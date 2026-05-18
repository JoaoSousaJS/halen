import SparkLine from './SparkLine';

interface KpiCardProps {
  label: string;
  value: string;
  deltaPct: number;
  sparkline: number[];
}

export default function KpiCard({ label, value, deltaPct, sparkline }: KpiCardProps) {
  const deltaClass =
    deltaPct > 0 ? 'positive' : deltaPct < 0 ? 'negative' : undefined;

  const deltaText =
    deltaPct > 0 ? `+${deltaPct}%` : `${deltaPct}%`;

  return (
    <div className="analytics-kpi-card">
      <span className="analytics-kpi-label">{label}</span>
      <span className="analytics-kpi-value">{value}</span>
      <span className={deltaClass}>{deltaText}</span>
      <SparkLine data={sparkline} />
    </div>
  );
}
