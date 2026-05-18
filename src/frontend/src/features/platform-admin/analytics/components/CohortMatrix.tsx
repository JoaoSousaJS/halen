interface CohortMatrixProps {
  cohorts: { cohortLabel: string; weeks: number[] }[];
}

export default function CohortMatrix({ cohorts }: CohortMatrixProps) {
  const maxWeeks = cohorts.reduce((max, c) => Math.max(max, c.weeks.length), 0);

  return (
    <div className="analytics-cohort">
      {/* Header row: empty corner + week columns */}
      <div className="analytics-cohort-header" />
      {Array.from({ length: maxWeeks }, (_, i) => (
        <span key={`w-${i}`} className="analytics-cohort-header">
          W{i}
        </span>
      ))}

      {/* Cohort rows */}
      {cohorts.map((cohort) => (
        <div key={cohort.cohortLabel} style={{ display: 'contents' }}>
          <span className="analytics-cohort-label">{cohort.cohortLabel}</span>
          {Array.from({ length: maxWeeks }, (_, weekIndex) => {
            const value = cohort.weeks[weekIndex];
            const hasValue = weekIndex < cohort.weeks.length;

            return (
              <span
                key={`${cohort.cohortLabel}-w${weekIndex}`}
                className="analytics-cohort-cell"
                style={{
                  backgroundColor: hasValue
                    ? `rgba(132, 204, 22, ${value / 100})`
                    : undefined,
                }}
              >
                {hasValue ? `${Math.round(value)}%` : ''}
              </span>
            );
          })}
        </div>
      ))}
    </div>
  );
}
