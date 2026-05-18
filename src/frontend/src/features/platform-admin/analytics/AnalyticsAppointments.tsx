import {
  AreaChart, Area, BarChart, Bar,
  XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
} from 'recharts';
import type { AppointmentAnalyticsDto } from '../../../shared/api/analytics';
import KpiCard from './components/KpiCard';
import AnalyticsCard from './components/AnalyticsCard';

interface AnalyticsAppointmentsProps {
  data: AppointmentAnalyticsDto;
}

function formatNumber(n: number): string {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
  if (n >= 1_000) return `${(n / 1_000).toFixed(1)}K`;
  return n.toLocaleString();
}

export default function AnalyticsAppointments({ data }: AnalyticsAppointmentsProps) {
  const dailyChartData = data.dailySeries.labels.map((label, i) => ({
    name: label,
    current: data.dailySeries.current[i],
    previous: data.dailySeries.previous[i],
  }));

  const dayOfWeekData = data.byDayOfWeek.map((d) => ({
    day: d.day,
    ratio: d.ratio,
  }));

  const hourOfDayData = data.byHourOfDay.map((h) => ({
    hour: `${h.hour}:00`,
    count: h.count,
  }));

  return (
    <div className="analytics-content">
      <div className="analytics-kpi-grid">
        <KpiCard
          label="Booked"
          value={formatNumber(data.bookedKpi.total)}
          deltaPct={data.bookedKpi.deltaPct}
          sparkline={data.bookedKpi.sparkline}
        />
        <KpiCard
          label="Completed"
          value={formatNumber(data.completedKpi.total)}
          deltaPct={data.completedKpi.deltaPct}
          sparkline={data.completedKpi.sparkline}
        />
        <KpiCard
          label="Cancelled"
          value={formatNumber(data.cancelledKpi.total)}
          deltaPct={data.cancelledKpi.deltaPct}
          sparkline={data.cancelledKpi.sparkline}
        />
        <KpiCard
          label="Avg Lead Time"
          value={`${data.avgLeadTimeKpi.value} days`}
          deltaPct={data.avgLeadTimeKpi.deltaPct}
          sparkline={data.avgLeadTimeKpi.sparkline}
        />
      </div>

      <AnalyticsCard title="Daily Appointments (90-day)">
        <ResponsiveContainer width="100%" height={300}>
          <AreaChart data={dailyChartData}>
            <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
            <XAxis dataKey="name" stroke="var(--text-muted)" fontSize={11} />
            <YAxis stroke="var(--text-muted)" fontSize={11} />
            <Tooltip />
            <Area type="monotone" dataKey="previous" stroke="var(--text-dim)" fill="var(--text-dim)" fillOpacity={0.1} />
            <Area type="monotone" dataKey="current" stroke="var(--accent)" fill="var(--accent)" fillOpacity={0.15} />
          </AreaChart>
        </ResponsiveContainer>
      </AnalyticsCard>

      <div className="analytics-chart-grid">
        <AnalyticsCard title="By Day of Week">
          <ResponsiveContainer width="100%" height={260}>
            <BarChart data={dayOfWeekData} layout="vertical">
              <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
              <XAxis type="number" stroke="var(--text-muted)" fontSize={11} />
              <YAxis dataKey="day" type="category" stroke="var(--text-muted)" fontSize={11} width={40} />
              <Tooltip />
              <Bar dataKey="ratio" fill="var(--accent)" radius={[0, 4, 4, 0]} />
            </BarChart>
          </ResponsiveContainer>
          <ul className="sr-only" aria-label="Day of week ratios">
            {dayOfWeekData.map((d) => (
              <li key={d.day}>{d.day}: {d.ratio}</li>
            ))}
          </ul>
        </AnalyticsCard>

        <AnalyticsCard title="By Hour of Day">
          <ResponsiveContainer width="100%" height={260}>
            <BarChart data={hourOfDayData}>
              <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
              <XAxis dataKey="hour" stroke="var(--text-muted)" fontSize={11} />
              <YAxis stroke="var(--text-muted)" fontSize={11} />
              <Tooltip />
              <Bar dataKey="count" fill="var(--accent)" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
          <ul className="sr-only" aria-label="Hour of day counts">
            {hourOfDayData.map((h) => (
              <li key={h.hour}>{h.hour}: {h.count}</li>
            ))}
          </ul>
        </AnalyticsCard>
      </div>
    </div>
  );
}
