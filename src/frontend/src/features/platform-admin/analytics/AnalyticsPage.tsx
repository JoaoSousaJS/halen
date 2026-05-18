import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import type { Period } from '../../../shared/api/analytics';
import {
  getAnalyticsOverview,
  getAppointmentAnalytics,
  getRevenueAnalytics,
  getHeatmapAnalytics,
  getDoctorAnalytics,
  getGeographyAnalytics,
} from '../../../shared/api/analytics';
import RangePills from './components/RangePills';
import AnalyticsOverview from './AnalyticsOverview';
import AnalyticsAppointments from './AnalyticsAppointments';
import AnalyticsRevenue from './AnalyticsRevenue';
import AnalyticsHeatmap from './AnalyticsHeatmap';
import AnalyticsDoctors from './AnalyticsDoctors';
import AnalyticsGeography from './AnalyticsGeography';

export type AnalyticsView = 'overview' | 'appointments' | 'revenue' | 'heatmap' | 'doctors' | 'geography';

interface AnalyticsPageProps {
  activeView: AnalyticsView;
  onNavigate: (view: AnalyticsView) => void;
}

const VIEW_TITLES: Record<Exclude<AnalyticsView, 'overview'>, string> = {
  appointments: 'Appointments',
  revenue: 'Revenue',
  heatmap: 'Heatmap',
  doctors: 'Doctors',
  geography: 'Geography',
};

export default function AnalyticsPage({ activeView, onNavigate }: AnalyticsPageProps) {
  const [period, setPeriod] = useState<Period>('30d');

  const overview = useQuery({
    queryKey: ['analytics', 'overview', period],
    queryFn: () => getAnalyticsOverview(period),
    staleTime: 30_000,
    enabled: activeView === 'overview',
  });

  const appointments = useQuery({
    queryKey: ['analytics', 'appointments', period],
    queryFn: () => getAppointmentAnalytics(period),
    staleTime: 30_000,
    enabled: activeView === 'appointments',
  });

  const revenue = useQuery({
    queryKey: ['analytics', 'revenue', period],
    queryFn: () => getRevenueAnalytics(period),
    staleTime: 30_000,
    enabled: activeView === 'revenue',
  });

  const heatmap = useQuery({
    queryKey: ['analytics', 'heatmap', period],
    queryFn: () => getHeatmapAnalytics(period),
    staleTime: 30_000,
    enabled: activeView === 'heatmap',
  });

  const doctors = useQuery({
    queryKey: ['analytics', 'doctors', period],
    queryFn: () => getDoctorAnalytics(period),
    staleTime: 30_000,
    enabled: activeView === 'doctors',
  });

  const geography = useQuery({
    queryKey: ['analytics', 'geography', period],
    queryFn: () => getGeographyAnalytics(period),
    staleTime: 30_000,
    enabled: activeView === 'geography',
  });

  return (
    <div className="analytics-page">
      <div className="analytics-header">
        {activeView !== 'overview' && (
          <nav className="analytics-breadcrumb">
            <a onClick={() => onNavigate('overview')}>Analytics</a>
            <span>/</span>
            <span>{VIEW_TITLES[activeView]}</span>
          </nav>
        )}
        <RangePills value={period} onSelect={setPeriod} />
      </div>

      {activeView === 'overview' && overview.data && (
        <AnalyticsOverview data={overview.data} onNavigate={onNavigate} />
      )}
      {activeView === 'overview' && overview.isLoading && (
        <div className="analytics-loading">Loading analytics...</div>
      )}

      {activeView === 'appointments' && appointments.data && (
        <AnalyticsAppointments data={appointments.data} />
      )}
      {activeView === 'appointments' && appointments.isLoading && (
        <div className="analytics-loading">Loading...</div>
      )}

      {activeView === 'revenue' && revenue.data && (
        <AnalyticsRevenue data={revenue.data} />
      )}
      {activeView === 'revenue' && revenue.isLoading && (
        <div className="analytics-loading">Loading...</div>
      )}

      {activeView === 'heatmap' && heatmap.data && (
        <AnalyticsHeatmap data={heatmap.data} />
      )}
      {activeView === 'heatmap' && heatmap.isLoading && (
        <div className="analytics-loading">Loading...</div>
      )}

      {activeView === 'doctors' && doctors.data && (
        <AnalyticsDoctors data={doctors.data} />
      )}
      {activeView === 'doctors' && doctors.isLoading && (
        <div className="analytics-loading">Loading...</div>
      )}

      {activeView === 'geography' && geography.data && (
        <AnalyticsGeography data={geography.data} />
      )}
      {activeView === 'geography' && geography.isLoading && (
        <div className="analytics-loading">Loading...</div>
      )}
    </div>
  );
}
