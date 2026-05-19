import { useState } from 'react';
import type { Meta, StoryObj } from '@storybook/react';
import { SearchFilterBar } from './SearchFilterBar';
import type { Filter } from './SearchFilterBar';

const meta: Meta<typeof SearchFilterBar> = {
  title: 'Shared/SearchFilterBar',
  component: SearchFilterBar,
  parameters: { layout: 'padded' },
  decorators: [
    (Story) => (
      <div style={{ padding: 24, maxWidth: 720, background: '#0b0e0c' }}>
        <Story />
      </div>
    ),
  ],
};
export default meta;

type Story = StoryObj<typeof SearchFilterBar>;

function StatefulBar(props: {
  searchPlaceholder: string;
  filters: Filter[];
  resultCount?: number;
  resultLabel?: string;
}) {
  const [search, setSearch] = useState('');
  const initial: Record<string, string> = {};
  for (const f of props.filters) initial[f.key] = f.value;
  const [values, setValues] = useState(initial);

  const filters = props.filters.map((f) => ({
    ...f,
    value: values[f.key] ?? f.value,
    onChange: (v: string) => setValues((prev) => ({ ...prev, [f.key]: v })),
  })) as Filter[];

  return (
    <SearchFilterBar
      searchPlaceholder={props.searchPlaceholder}
      searchValue={search}
      onSearchChange={setSearch}
      filters={filters}
      resultCount={props.resultCount}
      resultLabel={props.resultLabel}
    />
  );
}

export const DoctorSearchConfig: Story = {
  render: () => (
    <StatefulBar
      searchPlaceholder="Search doctors by name..."
      filters={[
        { type: 'dropdown', key: 'specialty', label: 'All specialties', options: [
          { value: 'cardiology', label: 'Cardiology' },
          { value: 'dermatology', label: 'Dermatology' },
          { value: 'neurology', label: 'Neurology' },
          { value: 'orthopedics', label: 'Orthopedics' },
          { value: 'pediatrics', label: 'Pediatrics' },
        ], value: '', onChange: () => {} },
        { type: 'dropdown', key: 'sort', label: 'Sort by', options: [
          { value: 'name', label: 'Name' },
          { value: 'fee_asc', label: 'Fee (low to high)' },
          { value: 'fee_desc', label: 'Fee (high to low)' },
          { value: 'experience', label: 'Experience' },
        ], value: '', onChange: () => {} },
      ]}
      resultCount={42}
      resultLabel="doctors"
    />
  ),
};

export const AuditLogConfig: Story = {
  render: () => (
    <StatefulBar
      searchPlaceholder="Filter by action…"
      filters={[
        { type: 'text', key: 'target', placeholder: 'Target ID…', value: '', onChange: () => {} },
        { type: 'date', key: 'from', label: 'From', value: '', onChange: () => {} },
        { type: 'date', key: 'to', label: 'To', value: '', onChange: () => {} },
      ]}
      resultCount={1204}
      resultLabel="logs"
    />
  ),
};

export const AdminUsersConfig: Story = {
  render: () => (
    <StatefulBar
      searchPlaceholder="Search by name or email…"
      filters={[
        { type: 'dropdown', key: 'role', label: 'All roles', options: [
          { value: 'patient', label: 'Patient' },
          { value: 'doctor', label: 'Doctor' },
          { value: 'flagged', label: 'Needs review' },
        ], value: '', onChange: () => {} },
      ]}
      resultCount={156}
      resultLabel="users"
    />
  ),
};

export const WithActiveFilters: Story = {
  render: () => (
    <StatefulBar
      searchPlaceholder="Search doctors by name..."
      filters={[
        { type: 'dropdown', key: 'specialty', label: 'All specialties', options: [
          { value: 'cardiology', label: 'Cardiology' },
          { value: 'dermatology', label: 'Dermatology' },
        ], value: 'cardiology', onChange: () => {} },
        { type: 'dropdown', key: 'sort', label: 'Sort by', options: [
          { value: 'name', label: 'Name' },
          { value: 'fee_asc', label: 'Fee (low to high)' },
        ], value: 'fee_asc', onChange: () => {} },
      ]}
      resultCount={8}
      resultLabel="doctors"
    />
  ),
};

export const EmptyState: Story = {
  render: () => (
    <StatefulBar
      searchPlaceholder="Search..."
      filters={[
        { type: 'dropdown', key: 'category', label: 'Category', options: [
          { value: 'a', label: 'Option A' },
        ], value: '', onChange: () => {} },
      ]}
      resultCount={0}
      resultLabel="results"
    />
  ),
};

export const NoResultCount: Story = {
  render: () => (
    <StatefulBar
      searchPlaceholder="Search..."
      filters={[
        { type: 'dropdown', key: 'category', label: 'Category', options: [
          { value: 'a', label: 'Option A' },
        ], value: '', onChange: () => {} },
      ]}
    />
  ),
};

export const ManyFilters: Story = {
  render: () => (
    <StatefulBar
      searchPlaceholder="Search..."
      filters={[
        { type: 'dropdown', key: 'a', label: 'Filter A', options: [{ value: '1', label: 'One' }], value: '', onChange: () => {} },
        { type: 'dropdown', key: 'b', label: 'Filter B', options: [{ value: '1', label: 'One' }], value: '', onChange: () => {} },
        { type: 'dropdown', key: 'c', label: 'Filter C', options: [{ value: '1', label: 'One' }], value: '', onChange: () => {} },
        { type: 'text', key: 'd', placeholder: 'Text filter...', value: '', onChange: () => {} },
        { type: 'date', key: 'e', label: 'Start', value: '', onChange: () => {} },
        { type: 'date', key: 'f', label: 'End', value: '', onChange: () => {} },
      ]}
      resultCount={999}
      resultLabel="items"
    />
  ),
};

export const AllFilterTypes: Story = {
  render: () => (
    <StatefulBar
      searchPlaceholder="Search all types..."
      filters={[
        { type: 'dropdown', key: 'dd', label: 'Dropdown', options: [
          { value: 'x', label: 'Option X' },
          { value: 'y', label: 'Option Y' },
        ], value: '', onChange: () => {} },
        { type: 'text', key: 'txt', placeholder: 'Text input…', value: '', onChange: () => {} },
        { type: 'date', key: 'dt', label: 'Date', value: '', onChange: () => {} },
      ]}
      resultCount={50}
      resultLabel="items"
    />
  ),
};
