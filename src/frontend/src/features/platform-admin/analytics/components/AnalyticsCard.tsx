import type { ReactNode } from 'react';

interface AnalyticsCardProps {
  title: string;
  action?: { label: string; onClick: () => void };
  children: ReactNode;
}

export default function AnalyticsCard({ title, action, children }: AnalyticsCardProps) {
  return (
    <div className="analytics-card">
      <div className="analytics-card-header">
        <h3 className="analytics-card-title">{title}</h3>
        {action && (
          <button
            type="button"
            className="analytics-card-action"
            onClick={action.onClick}
          >
            {action.label}
          </button>
        )}
      </div>
      {children}
    </div>
  );
}
