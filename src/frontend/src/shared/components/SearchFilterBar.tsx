import { FilterPill } from './FilterPill';

export interface FilterOption {
  value: string;
  label: string;
}

export interface DropdownFilter {
  type: 'dropdown';
  key: string;
  label: string;
  options: FilterOption[];
  value: string;
  onChange: (value: string) => void;
}

export interface TextFilter {
  type: 'text';
  key: string;
  placeholder: string;
  value: string;
  onChange: (value: string) => void;
}

export interface DateFilter {
  type: 'date';
  key: string;
  label: string;
  value: string;
  onChange: (value: string) => void;
}

export type Filter = DropdownFilter | TextFilter | DateFilter;

interface SearchFilterBarProps {
  searchPlaceholder?: string;
  searchValue: string;
  onSearchChange: (value: string) => void;
  filters: Filter[];
  resultCount?: number;
  resultLabel?: string;
}

export function SearchFilterBar({
  searchPlaceholder,
  searchValue,
  onSearchChange,
  filters,
  resultCount,
  resultLabel,
}: SearchFilterBarProps) {
  return (
    <div className="sfb">
      <div className="sfb-search-row">
        <svg className="sfb-search-icon" viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="2">
          <circle cx="7" cy="7" r="5" />
          <line x1="11" y1="11" x2="14" y2="14" />
        </svg>
        <input
          className="sfb-search-input"
          type="search"
          placeholder={searchPlaceholder}
          value={searchValue}
          onChange={(e) => onSearchChange(e.target.value)}
        />
      </div>

      {(filters.length > 0 || resultCount !== undefined) && (
        <div className="sfb-pills-row">
          {filters.map((filter) => (
            <FilterPill key={filter.key} filter={filter} />
          ))}
          {resultCount !== undefined && (
            <span className="sfb-result-count" aria-live="polite">
              {resultCount.toLocaleString()} {resultLabel}
            </span>
          )}
        </div>
      )}
    </div>
  );
}
