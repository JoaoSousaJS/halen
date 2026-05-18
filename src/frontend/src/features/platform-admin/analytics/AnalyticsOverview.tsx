import {
  AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
  BarChart, Bar, PieChart, Pie, Cell,
} from 'recharts';
import type { AnalyticsOverviewDto } from '../../../shared/api/analytics';
import type { AnalyticsView } from './AnalyticsPage';
import KpiCard from './components/KpiCard';
import AnalyticsCard from './components/AnalyticsCard';
import FunnelChart from './components/FunnelChart';
import { SPECIALTY_COLORS, DEFAULT_COLOR } from './chartColors';

interface AnalyticsOverviewProps {
  data: AnalyticsOverviewDto;
  onNavigate: (view: AnalyticsView) => void;
}

function formatNumber(n: number): string {
  if (n >= 1_000_000) return `${(n / 1_000_000).toFixed(1)}M`;
  if (n >= 1_000) return `${(n / 1_000).toFixed(1)}K`;
  return n.toLocaleString();
}

function formatCurrency(n: number): string {
  if (n >= 1_000_000) return `$${(n / 1_000_000).toFixed(1)}M`;
  if (n >= 1_000) return `$${(n / 1_000).toFixed(1)}K`;
  return `$${n}`;
}

export default function AnalyticsOverview({ data, onNavigate }: AnalyticsOverviewProps) {
  const dailyChartData = data.appointmentSeries.labels.map((label, i) => ({
    name: label,
    current: data.appointmentSeries.current[i],
    previous: data.appointmentSeries.previous[i],
  }));

  const revenueChartData = data.revenueSeries.labels.map((label, i) => ({
    name: label,
    value: data.revenueSeries.values[i],
  }));

  const clinicChartData = data.clinicBreakdown.map((c) => ({
    name: c.name,
    value: c.value,
  }));

  const specialtyChartData = data.specialtyMix.map((s) => ({
    name: s.label,
    value: s.value,
  }));

  return (
    <div className="analytics-content">
      <div className="analytics-kpi-grid">
        <KpiCard
          label="Appointments"
          value={formatNumber(data.appointmentKpi.total)}
          deltaPct={data.appointmentKpi.deltaPct}
          sparkline={data.appointmentKpi.sparkline}
        />
        <KpiCard
          label="Revenue"
          value={formatCurrency(data.revenueKpi.value)}
          deltaPct={data.revenueKpi.deltaPct}
          sparkline={data.revenueKpi.sparkline}
        />
        <KpiCard
          label="Active Users"
          value={formatNumber(data.activeUsersKpi.total)}
          deltaPct={data.activeUsersKpi.deltaPct}
          sparkline={data.activeUsersKpi.sparkline}
        />
        <KpiCard
          label="No-Show Rate"
          value={`${data.noShowKpi.rate}%`}
          deltaPct={data.noShowKpi.deltaPct}
          sparkline={data.noShowKpi.sparkline}
        />
      </div>

      <div className="analytics-chart-grid">
        <AnalyticsCard
          title="Daily Appointments"
          action={{ label: 'View details', onClick: () => onNavigate('appointments') }}
        >
          <ResponsiveContainer width="100%" height={260}>
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

        <AnalyticsCard
          title="Revenue (Weekly)"
          action={{ label: 'View details', onClick: () => onNavigate('revenue') }}
        >
          <ResponsiveContainer width="100%" height={260}>
            <BarChart data={revenueChartData}>
              <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
              <XAxis dataKey="name" stroke="var(--text-muted)" fontSize={11} />
              <YAxis stroke="var(--text-muted)" fontSize={11} />
              <Tooltip />
              <Bar dataKey="value" fill="var(--accent)" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </AnalyticsCard>
      </div>

      <div className="analytics-chart-grid">
        <AnalyticsCard title="Booking Funnel">
          <FunnelChart stages={data.funnel} />
        </AnalyticsCard>

        <AnalyticsCard title="Active Users">
          <div className="analytics-active-users">
            <div className="analytics-active-users-metric">
              <span className="analytics-kpi-label">DAU</span>
              <span className="analytics-kpi-value">{data.activeUsers.dau}</span>
              <DeltaBadge value={data.activeUsers.dauDelta} />
            </div>
            <div className="analytics-active-users-metric">
              <span className="analytics-kpi-label">WAU</span>
              <span className="analytics-kpi-value">{data.activeUsers.wau}</span>
              <DeltaBadge value={data.activeUsers.wauDelta} />
            </div>
            <div className="analytics-active-users-metric">
              <span className="analytics-kpi-label">MAU</span>
              <span className="analytics-kpi-value">{data.activeUsers.mau}</span>
              <DeltaBadge value={data.activeUsers.mauDelta} />
            </div>
            <div className="analytics-active-users-metric">
              <span className="analytics-kpi-label">Stickiness</span>
              <span className="analytics-kpi-value">{data.activeUsers.stickiness}%</span>
            </div>
          </div>
        </AnalyticsCard>
      </div>

      <div className="analytics-chart-grid">
        <AnalyticsCard title="Clinic Breakdown">
          <ResponsiveContainer width="100%" height={260}>
            <BarChart data={clinicChartData} layout="vertical">
              <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
              <XAxis type="number" stroke="var(--text-muted)" fontSize={11} />
              <YAxis dataKey="name" type="category" stroke="var(--text-muted)" fontSize={11} width={100} />
              <Tooltip />
              <Bar dataKey="value" fill="var(--accent)" radius={[0, 4, 4, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </AnalyticsCard>

        <AnalyticsCard title="Specialty Mix">
          <ResponsiveContainer width="100%" height={260}>
            <PieChart>
              <Pie
                data={specialtyChartData}
                cx="50%"
                cy="50%"
                innerRadius={60}
                outerRadius={100}
                paddingAngle={2}
                dataKey="value"
                nameKey="name"
              >
                {specialtyChartData.map((entry) => (
                  <Cell key={entry.name} fill={SPECIALTY_COLORS[entry.name] ?? DEFAULT_COLOR} />
                ))}
              </Pie>
              <Tooltip />
            </PieChart>
          </ResponsiveContainer>
        </AnalyticsCard>
      </div>
    </div>
  );
}

function DeltaBadge({ value }: { value: number }) {
  const cls = value > 0 ? 'positive' : value < 0 ? 'negative' : '';
  return (
    <span className={`analytics-kpi-delta ${cls}`}>
      {value > 0 ? '+' : ''}{value}%
    </span>
  );
}
