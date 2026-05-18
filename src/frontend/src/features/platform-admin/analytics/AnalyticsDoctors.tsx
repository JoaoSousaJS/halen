import { useState, useMemo } from 'react';
import type { DoctorAnalyticsDto } from '../../../shared/api/analytics';
import AnalyticsCard from './components/AnalyticsCard';
import SparkLine from './components/SparkLine';

interface AnalyticsDoctorsProps {
  data: DoctorAnalyticsDto;
}

function formatCurrency(n: number): string {
  if (n >= 1_000_000) return `$${(n / 1_000_000).toFixed(1)}M`;
  if (n >= 1_000) return `$${(n / 1_000).toFixed(1)}K`;
  return `$${n}`;
}

export default function AnalyticsDoctors({ data }: AnalyticsDoctorsProps) {
  const [selectedSpecialty, setSelectedSpecialty] = useState('All');

  // Extract unique specialties from ranked doctors for the filter pills
  const specialties = useMemo(() => {
    const unique = Array.from(new Set(data.ranked.map((d) => d.specialty)));
    return ['All', ...unique];
  }, [data.ranked]);

  // Filter ranked doctors based on selected specialty
  const filteredRanked = useMemo(() => {
    if (selectedSpecialty === 'All') return data.ranked;
    return data.ranked.filter((d) => d.specialty === selectedSpecialty);
  }, [data.ranked, selectedSpecialty]);

  return (
    <div className="analytics-content">
      {/* Specialty filter pills */}
      <div className="analytics-filter-pills">
        {specialties.map((specialty) => (
          <button
            key={specialty}
            type="button"
            className={`analytics-filter-pill${selectedSpecialty === specialty ? ' active' : ''}`}
            onClick={() => setSelectedSpecialty(specialty)}
          >
            {specialty}
          </button>
        ))}
      </div>

      {/* Leaderboard table */}
      <div className="analytics-leaderboard">
        {filteredRanked.map((doctor, index) => (
          <div key={doctor.name} className="analytics-leaderboard-row">
            <span className="analytics-leaderboard-rank">#{index + 1}</span>
            <span className="analytics-leaderboard-name">{doctor.name}</span>
            <span className="analytics-leaderboard-specialty">{doctor.specialty}</span>
            <span className="analytics-leaderboard-consults">{doctor.consults}</span>
            <span className="analytics-leaderboard-completion">{doctor.completionPct}%</span>
            <span className="analytics-leaderboard-rating">{doctor.rating}</span>
            <span className="analytics-leaderboard-revenue">{formatCurrency(doctor.revenue)}</span>
            <SparkLine data={doctor.trend} />
            {doctor.badge && (
              <span className="analytics-leaderboard-badge">{doctor.badge}</span>
            )}
          </div>
        ))}
      </div>

      {/* Top Rated */}
      <AnalyticsCard title="Top Rated">
        <div className="analytics-top-rated">
          {data.topRated.map((doctor) => (
            <div key={doctor.name} className="analytics-top-rated-card">
              <span className="analytics-top-rated-name">{doctor.name}</span>
              <span className="analytics-top-rated-rating">{doctor.rating}</span>
              <span className="analytics-top-rated-reviews">{doctor.reviewCount} reviews</span>
              <span className="analytics-top-rated-specialty">{doctor.specialty}</span>
            </div>
          ))}
        </div>
      </AnalyticsCard>

      {/* Needs Attention */}
      <AnalyticsCard title="Needs Attention">
        <div className="analytics-alerts">
          {data.needsAttention.map((item) => (
            <div key={item.name} className={`analytics-alert ${item.severity}`}>
              <span className="analytics-alert-name">{item.name}</span>
              <span className="analytics-alert-message">{item.message}</span>
            </div>
          ))}
        </div>
      </AnalyticsCard>
    </div>
  );
}
