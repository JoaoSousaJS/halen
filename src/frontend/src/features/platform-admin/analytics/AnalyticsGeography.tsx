import type { GeographyAnalyticsDto } from '../../../shared/api/analytics';
import AnalyticsCard from './components/AnalyticsCard';
import IberiaMap from './components/IberiaMap';
import CohortMatrix from './components/CohortMatrix';

interface AnalyticsGeographyProps {
  data: GeographyAnalyticsDto;
}

export default function AnalyticsGeography({ data }: AnalyticsGeographyProps) {
  return (
    <div className="analytics-content">
      <div className="analytics-chart-grid">
        <AnalyticsCard title="Geography">
          <IberiaMap regions={data.regions} />
        </AnalyticsCard>

        <AnalyticsCard title="Regions">
          <div className="analytics-region-list">
            {data.regions.map((region) => (
              <div key={region.name} className="analytics-region-item">
                <span className="analytics-region-name">{region.name}</span>
                <span className="analytics-region-consults">{region.consults}</span>
                <span
                  className={`analytics-region-delta ${region.deltaPct >= 0 ? 'positive' : 'negative'}`}
                >
                  {region.deltaPct > 0 ? '+' : ''}
                  {region.deltaPct}%
                </span>
                {region.isTop && (
                  <span className="analytics-region-top">Top</span>
                )}
              </div>
            ))}
          </div>
        </AnalyticsCard>
      </div>

      <AnalyticsCard title="Cohort Retention">
        <CohortMatrix cohorts={data.retention.cohorts} />
      </AnalyticsCard>
    </div>
  );
}
