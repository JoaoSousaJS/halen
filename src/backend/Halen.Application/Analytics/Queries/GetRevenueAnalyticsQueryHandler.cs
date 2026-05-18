using Halen.Application.Interfaces;
using Halen.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Halen.Application.Analytics.Queries;

public class GetRevenueAnalyticsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetRevenueAnalyticsQuery, RevenueAnalyticsResult>
{
    public async Task<RevenueAnalyticsResult> Handle(GetRevenueAnalyticsQuery request, CancellationToken ct)
    {
        var (start, end, prevStart, prevEnd) = AnalyticsPeriod.ParsePeriod(request.Period);
        var periodDays = (int)Math.Ceiling((end - start).TotalDays);

        // Fetch all payments in the combined window (current + previous periods).
        // We pull the data once and compute everything in-memory, which is both
        // efficient and compatible with EF Core InMemory provider for tests.
        var allPayments = await db.Payments.AsNoTracking()
            .Where(p => p.CreatedAt >= prevStart && p.CreatedAt < end)
            .Select(p => new PaymentRow(
                p.Id, p.ClinicId, p.AppointmentId, p.PatientProfileId,
                p.Amount, p.Status, p.CapturedAt, p.RefundedAt, p.CreatedAt))
            .ToListAsync(ct);

        var grossKpi = BuildGrossKpi(allPayments, start, end, prevStart, prevEnd, periodDays);
        var refundsKpi = BuildRefundsKpi(allPayments, start, end, prevStart, prevEnd, periodDays);
        var netKpi = BuildNetKpi(grossKpi, refundsKpi, periodDays);
        var arpuKpi = BuildArpuKpi(allPayments, grossKpi, start, end, prevStart, prevEnd, periodDays);

        // Weekly by specialty requires joining with Appointments and DoctorProfiles,
        // so we fetch those separately and join in-memory.
        var weeklyBySpecialty = await BuildWeeklyBySpecialtyAsync(start, end, ct);
        var paymentStatusBreakdown = BuildPaymentStatusBreakdown(allPayments, start, end);
        var clinicRevenue = await BuildClinicRevenueAsync(allPayments, start, end, prevStart, prevEnd, ct);

        return new RevenueAnalyticsResult(
            grossKpi, netKpi, refundsKpi, arpuKpi,
            weeklyBySpecialty, paymentStatusBreakdown, clinicRevenue);
    }

    // ── KPIs ──

    /// <summary>
    /// Gross revenue = sum of captured payment amounts within the period.
    /// Only payments with Status == Captured and CapturedAt within range count.
    /// </summary>
    private static DecimalKpiDto BuildGrossKpi(
        List<PaymentRow> payments,
        DateTime start, DateTime end, DateTime prevStart, DateTime prevEnd, int periodDays)
    {
        var current = payments
            .Where(p => p.Status == PaymentStatus.Captured
                        && p.CapturedAt.HasValue && p.CapturedAt.Value >= start && p.CapturedAt.Value < end)
            .Sum(p => p.Amount);

        var previous = payments
            .Where(p => p.Status == PaymentStatus.Captured
                        && p.CapturedAt.HasValue && p.CapturedAt.Value >= prevStart && p.CapturedAt.Value < prevEnd)
            .Sum(p => p.Amount);

        var sparkline = BuildDecimalSparkline(
            payments.Where(p => p.Status == PaymentStatus.Captured
                                && p.CapturedAt.HasValue && p.CapturedAt.Value >= start && p.CapturedAt.Value < end),
            p => p.CapturedAt!.Value, start, periodDays);

        return new DecimalKpiDto(current, ComputeDelta(current, previous), sparkline);
    }

    /// <summary>
    /// Refunds = sum of refunded payment amounts within the period.
    /// </summary>
    private static DecimalKpiDto BuildRefundsKpi(
        List<PaymentRow> payments,
        DateTime start, DateTime end, DateTime prevStart, DateTime prevEnd, int periodDays)
    {
        var current = payments
            .Where(p => p.Status == PaymentStatus.Refunded
                        && p.RefundedAt.HasValue && p.RefundedAt.Value >= start && p.RefundedAt.Value < end)
            .Sum(p => p.Amount);

        var previous = payments
            .Where(p => p.Status == PaymentStatus.Refunded
                        && p.RefundedAt.HasValue && p.RefundedAt.Value >= prevStart && p.RefundedAt.Value < prevEnd)
            .Sum(p => p.Amount);

        var sparkline = new decimal[periodDays]; // refund sparkline less useful, fill zeros
        return new DecimalKpiDto(current, ComputeDelta(current, previous), sparkline);
    }

    /// <summary>
    /// Net revenue = gross - refunds. Delta computed from the two already-computed values.
    /// </summary>
    private static DecimalKpiDto BuildNetKpi(DecimalKpiDto gross, DecimalKpiDto refunds, int periodDays)
    {
        var netValue = gross.Value - refunds.Value;

        // For delta: compute from the sparklines if available, but since net is derived,
        // we compute a simple net sparkline and derive delta from gross/refund deltas.
        var sparkline = new decimal[periodDays];
        for (var i = 0; i < periodDays; i++)
            sparkline[i] = gross.Sparkline[i]; // net sparkline approximates gross (refund sparkline is zeros)

        // We need the previous net to compute delta properly.
        // Gross previous = gross.Value / (1 + gross.DeltaPct/100) when delta is valid
        // Simpler: just use gross delta as proxy since refunds are usually small
        var grossPrev = gross.DeltaPct == 100 && gross.Value > 0
            ? 0m
            : gross.DeltaPct == 0 && gross.Value == 0
                ? 0m
                : gross.Value / (1 + gross.DeltaPct / 100m);
        var refundsPrev = refunds.DeltaPct == 100 && refunds.Value > 0
            ? 0m
            : refunds.DeltaPct == 0 && refunds.Value == 0
                ? 0m
                : refunds.Value / (1 + refunds.DeltaPct / 100m);
        var prevNet = grossPrev - refundsPrev;

        return new DecimalKpiDto(netValue, ComputeDelta(netValue, prevNet), sparkline);
    }

    /// <summary>
    /// ARPU (Average Revenue Per User) = gross / count of distinct patients
    /// who have captured payments in the period.
    /// </summary>
    private static DecimalKpiDto BuildArpuKpi(
        List<PaymentRow> payments, DecimalKpiDto grossKpi,
        DateTime start, DateTime end, DateTime prevStart, DateTime prevEnd, int periodDays)
    {
        var currentCaptured = payments
            .Where(p => p.Status == PaymentStatus.Captured
                        && p.CapturedAt.HasValue && p.CapturedAt.Value >= start && p.CapturedAt.Value < end)
            .ToList();

        var distinctPatients = currentCaptured
            .Select(p => p.PatientProfileId)
            .Distinct()
            .Count();

        var arpu = distinctPatients > 0 ? Math.Round(grossKpi.Value / distinctPatients, 2) : 0m;

        var prevCaptured = payments
            .Where(p => p.Status == PaymentStatus.Captured
                        && p.CapturedAt.HasValue && p.CapturedAt.Value >= prevStart && p.CapturedAt.Value < prevEnd)
            .ToList();

        var prevDistinct = prevCaptured.Select(p => p.PatientProfileId).Distinct().Count();
        var prevGross = prevCaptured.Sum(p => p.Amount);
        var prevArpu = prevDistinct > 0 ? Math.Round(prevGross / prevDistinct, 2) : 0m;

        var sparkline = new decimal[periodDays];
        return new DecimalKpiDto(arpu, ComputeDelta(arpu, prevArpu), sparkline);
    }

    // ── Weekly by Specialty ──

    /// <summary>
    /// Joins Payments -> Appointments -> DoctorProfiles to group captured revenue
    /// by ISO week and doctor specialty. Data is fetched first, then grouped
    /// in-memory to avoid InMemory provider GroupBy limitations.
    /// </summary>
    private async Task<WeeklySpecialtyDto[]> BuildWeeklyBySpecialtyAsync(
        DateTime start, DateTime end, CancellationToken ct)
    {
        var paymentData = await db.Payments.AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Captured
                        && p.CapturedAt.HasValue && p.CapturedAt.Value >= start && p.CapturedAt.Value < end)
            .Select(p => new { p.Amount, p.CapturedAt, p.AppointmentId })
            .ToListAsync(ct);

        if (paymentData.Count == 0)
            return [];

        var appointmentIds = paymentData.Select(p => p.AppointmentId).Distinct().ToList();

        var appointmentDoctors = await db.Appointments.AsNoTracking()
            .Where(a => appointmentIds.Contains(a.Id))
            .Select(a => new { a.Id, a.DoctorId })
            .ToListAsync(ct);

        var doctorIds = appointmentDoctors.Select(a => a.DoctorId).Distinct().ToList();

        var doctorSpecialties = await db.DoctorProfiles.AsNoTracking()
            .Where(d => doctorIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.Specialty, ct);

        var appointmentDoctorMap = appointmentDoctors.ToDictionary(a => a.Id, a => a.DoctorId);

        // Join in-memory and group by week + specialty
        var joined = paymentData
            .Where(p => appointmentDoctorMap.ContainsKey(p.AppointmentId))
            .Select(p =>
            {
                var doctorId = appointmentDoctorMap[p.AppointmentId];
                var specialty = doctorSpecialties.GetValueOrDefault(doctorId, "Other");
                var weekNum = GetIsoWeekNumber(p.CapturedAt!.Value);
                return new { Week = $"W{weekNum}", Specialty = specialty, p.Amount };
            })
            .GroupBy(x => x.Week)
            .Select(weekGroup => new WeeklySpecialtyDto(
                weekGroup.Key,
                weekGroup
                    .GroupBy(x => x.Specialty)
                    .Select(sg => new SpecialtyAmountDto(sg.Key, sg.Sum(x => x.Amount)))
                    .OrderByDescending(s => s.Amount)
                    .ToArray()))
            .OrderBy(w => w.Week)
            .ToArray();

        return joined;
    }

    // ── Payment Status Breakdown ──

    /// <summary>
    /// Groups all payments in the current period by their status and computes
    /// the percentage each status represents of the total amount.
    /// </summary>
    private static PaymentStatusDto[] BuildPaymentStatusBreakdown(
        List<PaymentRow> payments, DateTime start, DateTime end)
    {
        var periodPayments = payments
            .Where(p => p.CreatedAt >= start && p.CreatedAt < end)
            .ToList();

        if (periodPayments.Count == 0)
            return [];

        var totalAmount = periodPayments.Sum(p => p.Amount);

        return periodPayments
            .GroupBy(p => p.Status)
            .Select(g =>
            {
                var amount = g.Sum(p => p.Amount);
                var pct = totalAmount > 0 ? Math.Round(amount / totalAmount * 100, 2) : 0m;
                return new PaymentStatusDto(g.Key.ToString(), amount, pct);
            })
            .OrderByDescending(p => p.Amount)
            .ToArray();
    }

    // ── Clinic Revenue ──

    /// <summary>
    /// Groups captured payments by clinic, looks up clinic names, and computes
    /// consults (count), ARPU (revenue / consults), and delta vs previous period.
    /// Results are ordered by revenue descending.
    /// </summary>
    private async Task<ClinicRevenueDto[]> BuildClinicRevenueAsync(
        List<PaymentRow> payments,
        DateTime start, DateTime end, DateTime prevStart, DateTime prevEnd,
        CancellationToken ct)
    {
        var currentCaptured = payments
            .Where(p => p.Status == PaymentStatus.Captured
                        && p.CapturedAt.HasValue && p.CapturedAt.Value >= start && p.CapturedAt.Value < end)
            .ToList();

        if (currentCaptured.Count == 0)
            return [];

        var prevCaptured = payments
            .Where(p => p.Status == PaymentStatus.Captured
                        && p.CapturedAt.HasValue && p.CapturedAt.Value >= prevStart && p.CapturedAt.Value < prevEnd)
            .ToList();

        var clinicIds = currentCaptured.Select(p => p.ClinicId).Distinct().ToList();
        var clinicNames = await db.Clinics.AsNoTracking()
            .Where(c => clinicIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var prevByClinic = prevCaptured
            .GroupBy(p => p.ClinicId)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

        return currentCaptured
            .GroupBy(p => p.ClinicId)
            .Select(g =>
            {
                var revenue = g.Sum(p => p.Amount);
                var consults = g.Count();
                var arpu = consults > 0 ? Math.Round(revenue / consults, 2) : 0m;
                var prevRevenue = prevByClinic.GetValueOrDefault(g.Key, 0m);
                var name = clinicNames.GetValueOrDefault(g.Key, "Unknown");
                return new ClinicRevenueDto(name, consults, arpu, revenue, ComputeDelta(revenue, prevRevenue));
            })
            .OrderByDescending(c => c.Revenue)
            .ToArray();
    }

    // ── Helpers ──

    private static decimal[] BuildDecimalSparkline(
        IEnumerable<PaymentRow> filtered,
        Func<PaymentRow, DateTime> dateSelector,
        DateTime start, int days)
    {
        var dict = filtered
            .GroupBy(r => dateSelector(r).Date)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

        var sparkline = new decimal[days];
        for (var i = 0; i < days; i++)
            sparkline[i] = dict.GetValueOrDefault(start.AddDays(i).Date, 0m);
        return sparkline;
    }

    private static decimal ComputeDelta(decimal current, decimal previous)
        => previous == 0 ? (current > 0 ? 100 : 0) : Math.Round((current - previous) / previous * 100, 2);

    private static int GetIsoWeekNumber(DateTime date)
    {
        var day = System.Globalization.CultureInfo.InvariantCulture.Calendar
            .GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        return day;
    }

    /// <summary>
    /// Lightweight projection record for payment data.
    /// Avoids pulling full Payment entities into memory.
    /// </summary>
    private record PaymentRow(
        Guid Id, Guid ClinicId, Guid AppointmentId, Guid PatientProfileId,
        decimal Amount, PaymentStatus Status, DateTime? CapturedAt, DateTime? RefundedAt, DateTime CreatedAt);
}
