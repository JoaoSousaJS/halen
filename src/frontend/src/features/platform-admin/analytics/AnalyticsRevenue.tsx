import {
  BarChart, Bar,
  XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend,
} from 'recharts';
import type { RevenueAnalyticsDto } from '../../../shared/api/analytics';
import KpiCard from './components/KpiCard';
import AnalyticsCard from './components/AnalyticsCard';
import { SPECIALTY_COLORS, DEFAULT_COLOR } from './chartColors';

interface AnalyticsRevenueProps {
  data: RevenueAnalyticsDto;
}

function formatCurrency(n: number): string {
  if (n >= 1_000_000) return `$${(n / 1_000_000).toFixed(1)}M`;
  if (n >= 1_000) return `$${(n / 1_000).toFixed(1)}K`;
  return `$${n}`;
}

export default function AnalyticsRevenue({ data }: AnalyticsRevenueProps) {
  // Collect all unique specialties across every week
  const allSpecialties = Array.from(
    new Set(data.weeklyBySpecialty.flatMap((w) => w.segments.map((s) => s.specialty))),
  );

  // Transform weekly data into flat records for Recharts stacked bars
  const weeklyChartData = data.weeklyBySpecialty.map((w) => {
    const record: Record<string, string | number> = { week: w.week };
    for (const seg of w.segments) {
      record[seg.specialty] = seg.amount;
    }
    return record;
  });

  return (
    <div className="analytics-content">
      <div className="analytics-kpi-grid">
        <KpiCard
          label="Gross Revenue"
          value={formatCurrency(data.grossKpi.value)}
          deltaPct={data.grossKpi.deltaPct}
          sparkline={data.grossKpi.sparkline}
        />
        <KpiCard
          label="Net Revenue"
          value={formatCurrency(data.netKpi.value)}
          deltaPct={data.netKpi.deltaPct}
          sparkline={data.netKpi.sparkline}
        />
        <KpiCard
          label="Refunds"
          value={formatCurrency(data.refundsKpi.value)}
          deltaPct={data.refundsKpi.deltaPct}
          sparkline={data.refundsKpi.sparkline}
        />
        <KpiCard
          label="ARPU"
          value={formatCurrency(data.arpuKpi.value)}
          deltaPct={data.arpuKpi.deltaPct}
          sparkline={data.arpuKpi.sparkline}
        />
      </div>

      <AnalyticsCard title="Revenue by Specialty (Weekly)">
        <ResponsiveContainer width="100%" height={300}>
          <BarChart data={weeklyChartData}>
            <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
            <XAxis dataKey="week" stroke="var(--text-muted)" fontSize={11} />
            <YAxis stroke="var(--text-muted)" fontSize={11} />
            <Tooltip />
            <Legend />
            {allSpecialties.map((specialty) => (
              <Bar
                key={specialty}
                dataKey={specialty}
                stackId="stack"
                fill={SPECIALTY_COLORS[specialty] ?? DEFAULT_COLOR}
              />
            ))}
          </BarChart>
        </ResponsiveContainer>
      </AnalyticsCard>

      <div className="analytics-chart-grid">
        <AnalyticsCard title="Payment Status">
          <ul className="analytics-payment-list">
            {data.paymentStatusBreakdown.map((ps) => (
              <li key={ps.label} className="analytics-payment-row">
                <span className="analytics-payment-label">{ps.label}</span>
                <span className="analytics-payment-amount">{formatCurrency(ps.amount)}</span>
                <span className="analytics-payment-pct">{ps.percentage}%</span>
              </li>
            ))}
          </ul>
        </AnalyticsCard>

        <AnalyticsCard title="Clinic Revenue">
          <table className="analytics-table">
            <thead>
              <tr>
                <th>Clinic</th>
                <th>Consults</th>
                <th>ARPU</th>
                <th>Revenue</th>
                <th>Trend</th>
              </tr>
            </thead>
            <tbody>
              {data.clinicRevenue.map((clinic) => (
                <tr key={clinic.name}>
                  <td>{clinic.name}</td>
                  <td>{clinic.consults}</td>
                  <td>{formatCurrency(clinic.arpu)}</td>
                  <td>{formatCurrency(clinic.revenue)}</td>
                  <td className={clinic.deltaPct >= 0 ? 'positive' : 'negative'}>
                    {clinic.deltaPct > 0 ? '+' : ''}{clinic.deltaPct}%
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </AnalyticsCard>
      </div>
    </div>
  );
}
