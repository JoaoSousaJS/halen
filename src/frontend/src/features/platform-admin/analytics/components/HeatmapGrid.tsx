interface HeatmapGridProps {
  grid: number[][]; // [7][24] — rows = Mon(0) to Sun(6), cols = hour 0-23
}

const DAY_LABELS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

export default function HeatmapGrid({ grid }: HeatmapGridProps) {
  const maxValue = Math.max(...grid.flat());

  function cellOpacity(value: number): number {
    if (maxValue === 0) return 0.05;
    return value === 0 ? 0.05 : value / maxValue;
  }

  return (
    <div className="analytics-heatmap">
      {/* Column header row: empty corner + 24 hour labels */}
      <div className="analytics-heatmap-label" />
      {Array.from({ length: 24 }, (_, h) => (
        <span key={`h-${h}`} className="analytics-heatmap-label">
          {h}
        </span>
      ))}

      {/* 7 day rows */}
      {grid.map((row, dayIndex) => (
        <div key={DAY_LABELS[dayIndex]} style={{ display: 'contents' }}>
          <span className="analytics-heatmap-label">{DAY_LABELS[dayIndex]}</span>
          {row.map((value, hour) => (
            <div
              key={`${dayIndex}-${hour}`}
              className="analytics-heatmap-cell"
              style={{ opacity: cellOpacity(value) }}
            />
          ))}
        </div>
      ))}
    </div>
  );
}
