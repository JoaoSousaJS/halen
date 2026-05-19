import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { vi, describe, it, expect } from 'vitest';
import { SearchFilterBar } from './SearchFilterBar';
import type { Filter } from './SearchFilterBar';

const specialtyFilter: Filter = {
  type: 'dropdown',
  key: 'specialty',
  label: 'All specialties',
  options: [
    { value: 'cardiology', label: 'Cardiology' },
    { value: 'dermatology', label: 'Dermatology' },
  ],
  value: '',
  onChange: vi.fn(),
};

const sortFilter: Filter = {
  type: 'dropdown',
  key: 'sort',
  label: 'Sort by',
  options: [
    { value: 'name', label: 'Name' },
    { value: 'fee_asc', label: 'Fee (low to high)' },
  ],
  value: '',
  onChange: vi.fn(),
};

describe('SearchFilterBar', () => {
  it('renders search input with placeholder', () => {
    render(
      <SearchFilterBar
        searchPlaceholder="Search doctors by name..."
        searchValue=""
        onSearchChange={vi.fn()}
        filters={[]}
      />,
    );
    expect(screen.getByPlaceholderText('Search doctors by name...')).toBeInTheDocument();
  });

  it('search input has type="search"', () => {
    render(
      <SearchFilterBar
        searchPlaceholder="Search..."
        searchValue=""
        onSearchChange={vi.fn()}
        filters={[]}
      />,
    );
    expect(screen.getByPlaceholderText('Search...')).toHaveAttribute('type', 'search');
  });

  it('calls onSearchChange when typing', async () => {
    const onSearchChange = vi.fn();
    const user = userEvent.setup();
    render(
      <SearchFilterBar
        searchPlaceholder="Search..."
        searchValue=""
        onSearchChange={onSearchChange}
        filters={[]}
      />,
    );

    await user.type(screen.getByPlaceholderText('Search...'), 'test');

    expect(onSearchChange).toHaveBeenCalledTimes(4);
  });

  it('renders the correct number of filter pills', () => {
    render(
      <SearchFilterBar
        searchPlaceholder="Search..."
        searchValue=""
        onSearchChange={vi.fn()}
        filters={[specialtyFilter, sortFilter]}
      />,
    );

    expect(screen.getByRole('button', { name: /all specialties/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /sort by/i })).toBeInTheDocument();
  });

  it('renders result count when provided', () => {
    render(
      <SearchFilterBar
        searchPlaceholder="Search..."
        searchValue=""
        onSearchChange={vi.fn()}
        filters={[]}
        resultCount={42}
        resultLabel="doctors"
      />,
    );

    expect(screen.getByText('42 doctors')).toBeInTheDocument();
  });

  it('formats large result count with locale separators', () => {
    render(
      <SearchFilterBar
        searchPlaceholder="Search..."
        searchValue=""
        onSearchChange={vi.fn()}
        filters={[]}
        resultCount={1204}
        resultLabel="logs"
      />,
    );

    expect(screen.getByText(/1.204 logs|1,204 logs/)).toBeInTheDocument();
  });

  it('does not render result count when undefined', () => {
    const { container } = render(
      <SearchFilterBar
        searchPlaceholder="Search..."
        searchValue=""
        onSearchChange={vi.fn()}
        filters={[]}
      />,
    );

    expect(container.querySelector('.sfb-result-count')).not.toBeInTheDocument();
  });

  it('renders text and date filter pills', () => {
    const filters: Filter[] = [
      { type: 'text', key: 'target', placeholder: 'Target ID...', value: '', onChange: vi.fn() },
      { type: 'date', key: 'from', label: 'From', value: '', onChange: vi.fn() },
      { type: 'date', key: 'to', label: 'To', value: '', onChange: vi.fn() },
    ];

    render(
      <SearchFilterBar
        searchPlaceholder="Filter by action..."
        searchValue=""
        onSearchChange={vi.fn()}
        filters={filters}
        resultCount={237}
        resultLabel="logs"
      />,
    );

    expect(screen.getByPlaceholderText('Target ID...')).toBeInTheDocument();
    expect(screen.getByLabelText('From date')).toBeInTheDocument();
    expect(screen.getByLabelText('To date')).toBeInTheDocument();
  });

  it('result count uses aria-live for accessibility', () => {
    const { container } = render(
      <SearchFilterBar
        searchPlaceholder="Search..."
        searchValue=""
        onSearchChange={vi.fn()}
        filters={[]}
        resultCount={5}
        resultLabel="results"
      />,
    );

    const countEl = container.querySelector('.sfb-result-count');
    expect(countEl).toHaveAttribute('aria-live', 'polite');
  });
});
