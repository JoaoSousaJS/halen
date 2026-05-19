import { useState, useRef, useEffect, useCallback } from 'react';
import type { DropdownFilter, TextFilter, DateFilter, Filter } from './SearchFilterBar';

interface FilterPillProps {
  filter: Filter;
}

export function FilterPill({ filter }: FilterPillProps) {
  switch (filter.type) {
    case 'dropdown':
      return <DropdownPill filter={filter} />;
    case 'text':
      return <TextPill filter={filter} />;
    case 'date':
      return <DatePill filter={filter} />;
  }
}

function DropdownPill({ filter }: { filter: DropdownFilter }) {
  const [open, setOpen] = useState(false);
  const [highlightedIndex, setHighlightedIndex] = useState(-1);
  const containerRef = useRef<HTMLDivElement>(null);
  const buttonRef = useRef<HTMLButtonElement>(null);

  const selectedOption = filter.options.find((o) => o.value === filter.value);
  const isActive = filter.value !== '';

  const close = useCallback(() => {
    setOpen(false);
    setHighlightedIndex(-1);
  }, []);

  useEffect(() => {
    if (!open) return;
    function handleClickOutside(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        close();
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [open, close]);

  function handleKeyDown(e: React.KeyboardEvent) {
    if (!open) {
      if (e.key === 'Enter' || e.key === ' ' || e.key === 'ArrowDown') {
        e.preventDefault();
        setOpen(true);
        setHighlightedIndex(0);
      }
      return;
    }

    switch (e.key) {
      case 'Escape':
        e.preventDefault();
        close();
        break;
      case 'ArrowDown':
        e.preventDefault();
        setHighlightedIndex((i) => Math.min(i + 1, filter.options.length - 1));
        break;
      case 'ArrowUp':
        e.preventDefault();
        setHighlightedIndex((i) => Math.max(i - 1, 0));
        break;
      case 'Home':
        e.preventDefault();
        setHighlightedIndex(0);
        break;
      case 'End':
        e.preventDefault();
        setHighlightedIndex(filter.options.length - 1);
        break;
      case 'Enter':
        e.preventDefault();
        if (highlightedIndex >= 0 && highlightedIndex < filter.options.length) {
          filter.onChange(filter.options[highlightedIndex].value);
          close();
        }
        break;
    }
  }

  function selectOption(value: string) {
    filter.onChange(value);
    close();
  }

  const listboxId = `filter-pill-listbox-${filter.key}`;
  const activeDescendant = open && highlightedIndex >= 0
    ? `${listboxId}-opt-${highlightedIndex}`
    : undefined;

  return (
    <div className="filter-pill" ref={containerRef}>
      <button
        ref={buttonRef}
        type="button"
        className={`filter-pill-btn${isActive ? ' active' : ''}`}
        aria-expanded={open}
        aria-haspopup="listbox"
        aria-controls={open ? listboxId : undefined}
        aria-activedescendant={activeDescendant}
        aria-label={isActive ? selectedOption?.label : filter.label}
        onClick={() => setOpen(!open)}
        onKeyDown={handleKeyDown}
        disabled={filter.options.length === 0}
      >
        <span className="filter-pill-label">
          {isActive ? selectedOption?.label : filter.label}
        </span>
        {!isActive && (
          <svg className="filter-pill-chevron" viewBox="0 0 12 12" fill="none" stroke="currentColor" strokeWidth="2">
            <path d="M3 5l3 3 3-3" />
          </svg>
        )}
      </button>
      {isActive && (
        <button
          type="button"
          className="filter-pill-dismiss"
          aria-label={`Clear ${filter.key}`}
          onClick={() => filter.onChange('')}
        >
          ×
        </button>
      )}

      {open && (
        <ul
          id={listboxId}
          role="listbox"
          className="filter-pill-popover"
          aria-label={filter.label}
        >
          {filter.options.map((opt, i) => (
            <li
              key={opt.value}
              id={`${listboxId}-opt-${i}`}
              role="option"
              aria-selected={opt.value === filter.value}
              className={`filter-pill-option${i === highlightedIndex ? ' highlighted' : ''}`}
              onClick={() => selectOption(opt.value)}
              onMouseEnter={() => setHighlightedIndex(i)}
            >
              {opt.label}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

function TextPill({ filter }: { filter: TextFilter }) {
  return (
    <div className="filter-pill">
      <input
        type="text"
        className="filter-pill-input"
        placeholder={filter.placeholder}
        aria-label={filter.placeholder}
        value={filter.value}
        onChange={(e) => filter.onChange(e.target.value)}
      />
    </div>
  );
}

function DatePill({ filter }: { filter: DateFilter }) {
  return (
    <div className="filter-pill">
      <input
        type="date"
        className="filter-pill-input"
        aria-label={`${filter.label} date`}
        value={filter.value}
        onChange={(e) => filter.onChange(e.target.value)}
      />
    </div>
  );
}
