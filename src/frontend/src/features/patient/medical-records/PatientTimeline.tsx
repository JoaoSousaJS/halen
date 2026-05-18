import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Chip, Button } from '../../../shared/components';
import { getPatientTimeline } from '../../../shared/api/medical-records';
import type { TimelineEntryDto } from '../../../shared/api/medical-records';

const TIMELINE_ENTRY_TYPES = [
  'Condition',
  'Allergy',
  'Vital',
  'Medication',
  'FamilyHistory',
  'Document',
] as const;

type TimelineEntryType = (typeof TIMELINE_ENTRY_TYPES)[number];

const TYPE_VARIANT: Record<string, 'good' | 'warn' | 'danger' | undefined> = {
  Condition: 'warn',
  Allergy: 'danger',
  Vital: 'good',
  Medication: undefined,
  FamilyHistory: undefined,
  Document: undefined,
};

const PAGE_SIZE = 10;

interface PatientTimelineProps {
  patientProfileId: string;
}

function TimelineEntry({ entry }: { entry: TimelineEntryDto }) {
  const formattedDate = new Date(entry.occurredAt).toLocaleDateString();

  return (
    <article className="timeline-entry" aria-label={entry.title}>
      <div className="timeline-entry-rail">
        <div className={`timeline-type-badge timeline-type-${entry.type.toLowerCase()}`}>
          <Chip status={entry.type} variant={TYPE_VARIANT[entry.type]} />
        </div>
        <div className="timeline-rail-line" />
      </div>
      <div className="timeline-card">
        <div className="timeline-card-header">
          <h3>{entry.title}</h3>
          <time className="timeline-date" dateTime={entry.occurredAt}>{formattedDate}</time>
        </div>
        {entry.subtitle && <p className="timeline-card-subtitle">{entry.subtitle}</p>}
        {entry.addedBy && <p className="text-dim timeline-card-added-by">{entry.addedBy}</p>}
      </div>
    </article>
  );
}

export default function PatientTimeline({
  patientProfileId,
}: PatientTimelineProps) {
  const [page, setPage] = useState(1);
  const [typeFilters, setTypeFilters] = useState<Set<TimelineEntryType>>(
    new Set(TIMELINE_ENTRY_TYPES),
  );

  const timeline = useQuery({
    queryKey: [
      'patient-timeline',
      patientProfileId,
      page,
      Array.from(typeFilters).sort().join(','),
    ],
    queryFn: () =>
      getPatientTimeline(patientProfileId, {
        page,
        pageSize: PAGE_SIZE,
        filterTypes: Array.from(typeFilters),
      }),
  });

  function toggleTypeFilter(type: TimelineEntryType) {
    setTypeFilters((prev) => {
      const next = new Set(prev);
      if (next.has(type)) {
        next.delete(type);
      } else {
        next.add(type);
      }
      return next;
    });
    setPage(1);
  }

  if (timeline.isLoading) {
    return <p role="status">Loading timeline...</p>;
  }

  if (timeline.isError) {
    return <p className="auth-error">Failed to load timeline.</p>;
  }

  const { entries, totalCount } = timeline.data ?? { entries: [], totalCount: 0 };
  const totalPages = Math.ceil(totalCount / PAGE_SIZE);

  return (
    <section className="timeline" aria-label="Patient timeline">
      <fieldset className="timeline-filters" role="group" aria-label="Filter by type">
        <legend>Filter by type</legend>
        {TIMELINE_ENTRY_TYPES.map((type) => (
          <label key={type} className={`timeline-filter-item${typeFilters.has(type) ? ' timeline-filter-active' : ''}`}>
            <input
              type="checkbox"
              checked={typeFilters.has(type)}
              onChange={() => toggleTypeFilter(type)}
            />
            {type}
          </label>
        ))}
      </fieldset>

      {entries.length === 0 ? (
        <p className="timeline-empty text-dim">No medical events recorded yet.</p>
      ) : (
        <div className="timeline-list" role="list" aria-label="Timeline entries">
          {entries.map((entry) => (
            <div key={entry.id} role="listitem">
              <TimelineEntry entry={entry} />
            </div>
          ))}
        </div>
      )}

      {totalPages > 1 && (
        <nav className="timeline-pagination" aria-label="Pagination">
          <Button
            size="sm"
            disabled={page <= 1}
            onClick={() => setPage((p) => p - 1)}
            ariaLabel="Previous page"
          >
            Previous
          </Button>
          <span className="timeline-pagination-text">
            Page {page} of {totalPages}
          </span>
          <Button
            size="sm"
            disabled={page >= totalPages}
            onClick={() => setPage((p) => p + 1)}
            ariaLabel="Next page"
          >
            Next
          </Button>
        </nav>
      )}
    </section>
  );
}
