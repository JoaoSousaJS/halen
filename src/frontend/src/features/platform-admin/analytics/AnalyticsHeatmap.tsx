import {
  LineChart, Line, BarChart, Bar, XAxis, YAxis,
  CartesianGrid, Tooltip, ResponsiveContainer, Legend,
} from 'recharts';
import type { HeatmapAnalyticsDto } from '../../../shared/api/analytics';
import AnalyticsCard from './components/AnalyticsCard';
import HeatmapGrid from './components/HeatmapGrid';
import { SPECIALTY_COLORS, DEFAULT_COLOR } from './chartColors';

interface AnalyticsHeatmapProps {
  data: HeatmapAnalyticsDto;
}

export default function AnalyticsHeatmap({ data }: AnalyticsHeatmapProps) {
  // Build chart data for the seasonality LineChart.
  // Each row is { month, [specialty]: count, ... } so Recharts can plot multiple Lines.
  const allMonths = data.specialtySeries[0]?.dataPoints.map((dp) => dp.month) ?? [];
  const seasonalityData = allMonths.map((month, i) => {
    const row: Record<string, string | number> = { month };
    for (const series of data.specialtySeries) {
      row[series.specialty] = series.dataPoints[i]?.count ?? 0;
    }
    return row;
  });

  // Build chart data for horizontal bar chart (avg wait by specialty)
  const waitData = data.avgWaitBySpecialty.map((entry) => ({
    specialty: entry.specialty,
    days: entry.days,
  }));

  return (
    <div className="analytics-content">
      <AnalyticsCard title="Booking Heatmap">
        <HeatmapGrid grid={data.grid} />
      </AnalyticsCard>

      <div className="analytics-chart-grid">
        <AnalyticsCard title="Specialty Seasonality">
          <ResponsiveContainer width="100%" height={260}>
            <LineChart data={seasonalityData}>
              <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
              <XAxis dataKey="month" stroke="var(--text-muted)" fontSize={11} />
              <YAxis stroke="var(--text-muted)" fontSize={11} />
              <Tooltip />
              <Legend />
              {data.specialtySeries.map((series) => (
                <Line
                  key={series.specialty}
                  type="monotone"
                  dataKey={series.specialty}
                  stroke={SPECIALTY_COLORS[series.specialty] ?? DEFAULT_COLOR}
                  strokeWidth={2}
                  dot={false}
                />
              ))}
            </LineChart>
          </ResponsiveContainer>
        </AnalyticsCard>

        <AnalyticsCard title="Avg Wait by Specialty">
          <ResponsiveContainer width="100%" height={260}>
            <BarChart data={waitData} layout="vertical">
              <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
              <XAxis type="number" stroke="var(--text-muted)" fontSize={11} />
              <YAxis
                dataKey="specialty"
                type="category"
                stroke="var(--text-muted)"
                fontSize={11}
                width={100}
              />
              <Tooltip />
              <Bar dataKey="days" fill="var(--accent)" radius={[0, 4, 4, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </AnalyticsCard>
      </div>
    </div>
  );
}
