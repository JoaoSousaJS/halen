import { useState, useEffect } from 'react';
import { useQuery, keepPreviousData } from '@tanstack/react-query';
import { searchDoctors, listSpecialties } from '../../shared/api/doctors';
import type { DoctorSearchDto } from '../../shared/api/doctors';
import { SearchFilterBar } from '../../shared/components';
import type { Filter } from '../../shared/components';
import DoctorCard from './DoctorCard';

interface DoctorSearchProps {
  onSelect: (doctor: DoctorSearchDto) => void;
}

const SORT_OPTIONS = [
  { value: 'name', label: 'Name' },
  { value: 'fee_asc', label: 'Fee (low to high)' },
  { value: 'fee_desc', label: 'Fee (high to low)' },
  { value: 'experience', label: 'Experience' },
];

const PAGE_SIZE = 20;

export default function DoctorSearch({ onSelect }: DoctorSearchProps) {
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [specialty, setSpecialty] = useState('');
  const [sortBy, setSortBy] = useState('');
  const [page, setPage] = useState(1);

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(search);
      setPage(1);
    }, 300);
    return () => clearTimeout(timer);
  }, [search]);

  const { data: specialties } = useQuery({
    queryKey: ['specialties'],
    queryFn: listSpecialties,
  });

  const { data, isLoading, isError } = useQuery({
    queryKey: ['doctors', 'search', { search: debouncedSearch, specialty, sortBy, page }],
    queryFn: () =>
      searchDoctors({
        search: debouncedSearch || undefined,
        specialty: specialty || undefined,
        sortBy: (sortBy as 'name' | 'fee_asc' | 'fee_desc' | 'experience') || undefined,
        page,
        pageSize: PAGE_SIZE,
      }),
    placeholderData: keepPreviousData,
  });

  const specialtyOptions = (specialties ?? []).map((s) => ({ value: s, label: s }));

  const totalPages = data ? Math.ceil(data.totalCount / PAGE_SIZE) : 0;

  const filters: Filter[] = [
    {
      type: 'dropdown',
      key: 'specialty',
      label: 'All specialties',
      options: specialtyOptions,
      value: specialty,
      onChange: (v) => { setSpecialty(v); setPage(1); },
    },
    {
      type: 'dropdown',
      key: 'sort',
      label: 'Sort by',
      options: SORT_OPTIONS,
      value: sortBy,
      onChange: (v) => { setSortBy(v); setPage(1); },
    },
  ];

  return (
    <div className="doctor-search">
      <SearchFilterBar
        searchPlaceholder="Search doctors..."
        searchValue={search}
        onSearchChange={setSearch}
        filters={filters}
        resultCount={data?.totalCount}
        resultLabel="doctors"
      />

      {isLoading && !data && <p role="status">Loading doctors...</p>}

      {!isLoading && !isError && data && data.doctors.length === 0 && (
        <p className="doctor-search-empty" role="status">No doctors match your filters</p>
      )}

      {data && data.doctors.length > 0 && (
        <div className="doctor-search-results">
          {data.doctors.map((doctor) => (
            <DoctorCard key={doctor.id} doctor={doctor} onSelect={onSelect} />
          ))}
        </div>
      )}

      {totalPages > 1 && (
        <nav className="doctor-search-pagination" aria-label="Doctor search results pages">
          <button aria-label="Previous page" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
            Previous
          </button>
          <span aria-live="polite" aria-atomic="true">
            Page {page} of {totalPages}
          </span>
          <button aria-label="Next page" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>
            Next
          </button>
        </nav>
      )}
    </div>
  );
}
