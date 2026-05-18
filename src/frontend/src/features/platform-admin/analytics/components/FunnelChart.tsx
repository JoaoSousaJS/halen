interface FunnelChartProps {
  stages: { label: string; value: number }[];
}

export default function FunnelChart({ stages }: FunnelChartProps) {
  const maxValue = stages.length > 0 ? stages[0].value : 0;

  return (
    <div className="analytics-funnel">
      {stages.map((stage, index) => {
        const widthPercent =
          maxValue > 0 ? Math.round((stage.value / maxValue) * 100) : 0;

        return (
          <div key={stage.label}>
            <div className="analytics-funnel-bar">
              <div
                className="analytics-funnel-fill"
                style={{ width: `${widthPercent}%` }}
              />
            </div>
            <span className="analytics-funnel-label">
              {stage.label}: {stage.value}
            </span>

            {index < stages.length - 1 && (
              <div className="analytics-funnel-conversion">
                →{' '}
                {Math.round(
                  (stages[index + 1].value / stage.value) * 100,
                )}
                %
              </div>
            )}
          </div>
        );
      })}
    </div>
  );
}
