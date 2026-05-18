import type { Period } from '../../../../shared/api/analytics';

interface RangePillsProps {
  value: Period;
  onSelect: (period: Period) => void;
}

const periods: { value: Period; label: string }[] = [
  { value: '7d', label: '7d' },
  { value: '30d', label: '30d' },
  { value: '90d', label: '90d' },
  { value: 'ytd', label: 'YTD' },
];

export default function RangePills({ value, onSelect }: RangePillsProps) {
  return (
    <div className="analytics-filter-pills">
      {periods.map((p) => (
        <button
          key={p.value}
          type="button"
          className={`analytics-filter-pill${p.value === value ? ' active' : ''}`}
          onClick={() => onSelect(p.value)}
        >
          {p.label}
        </button>
      ))}
    </div>
  );
}
