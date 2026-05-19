# Search Filter Bar Design — 2026-05-19

## Problem

The doctor search page has poorly styled filter controls (plain `<input>` and `<select>` elements in a flex row). The same pattern repeats across admin users and audit log pages with inconsistent implementations. Additionally, the login form's inline error causes a layout shift that feels jarring.

## Goals

1. Build a reusable `SearchFilterBar` shared component with the "Search + Filter Pills" pattern
2. Migrate 3 pages to use it: DoctorSearch, AdminUsersPage, AuditLogPage
3. Fix login/register form error display to reserve space and prevent layout shift

## Non-goals

- Redesigning the booking flow/wizard (separate feature)
- Redesigning doctor cards
- Changing any backend APIs

---

## Design: SearchFilterBar

### Visual pattern

Two rows:
1. **Search row**: Full-width input with a search icon (magnifying glass SVG), `border-radius: 12px`, dark surface background (`var(--surface)`), border `var(--border)`
2. **Pills row**: Horizontally scrolling/wrapping row of pill-shaped filter controls. Result count right-aligned.

### Pill states

- **Inactive dropdown**: `background: var(--surface)`, `border: 1px solid var(--border)`, `border-radius: var(--r-pill)`, `color: var(--text-dim)`. Shows chevron-down icon on right. Clicking opens a dropdown popover positioned below the pill.
- **Active filter**: `background: var(--accent-dim)`, `border: 1px solid rgba(196,255,61,0.2)`, `color: var(--accent)`. Shows the selected value label and a `×` dismiss button. Clicking `×` clears the filter and returns to inactive state.
- **Text input pill**: For free-text filters like "Target ID" on audit log. Renders as a pill-shaped text input. When focused, border brightens to `var(--border-bright)`.
- **Date pill**: Renders a native `<input type="date">` styled as a pill. Paired date pills (from/to) have a `→` separator between them.

### Dropdown popover

When an inactive dropdown pill is clicked, a popover appears below it:
- `background: var(--surface-2)`, `border: 1px solid var(--border)`, `border-radius: var(--r-md)`, `box-shadow: var(--shadow-dialog)`
- Options list with hover state: `background: var(--surface)` on hover
- Max height: 240px with overflow scroll
- Closes on: option select, click outside, Escape key
- Keyboard accessible: arrow keys navigate, Enter selects, Escape closes

### Component API

```tsx
interface FilterOption {
  value: string;
  label: string;
}

interface DropdownFilter {
  type: 'dropdown';
  key: string;
  label: string;          // shown when no value selected
  options: FilterOption[];
  value: string;
  onChange: (value: string) => void;
}

interface TextFilter {
  type: 'text';
  key: string;
  placeholder: string;
  value: string;
  onChange: (value: string) => void;
}

interface DateFilter {
  type: 'date';
  key: string;
  label: string;
  value: string;
  onChange: (value: string) => void;
}

type Filter = DropdownFilter | TextFilter | DateFilter;

interface SearchFilterBarProps {
  searchPlaceholder?: string;
  searchValue: string;
  onSearchChange: (value: string) => void;
  filters: Filter[];
  resultCount?: number;
  resultLabel?: string;  // e.g. "doctors", "users", "logs"
}
```

### File structure

```
src/shared/components/
  SearchFilterBar.tsx        # main component
  SearchFilterBar.test.tsx   # unit tests
  FilterPill.tsx             # pill sub-component (dropdown, text, date variants)
```

CSS added to `index.css` under a new `/* ── Search filter bar ── */` section.

Export from `src/shared/components/index.ts`.

---

## Page migrations

### DoctorSearch

Current: `<Input>` + two `<Select>` in `.doctor-search-controls`

New:
```tsx
<SearchFilterBar
  searchPlaceholder="Search doctors by name..."
  searchValue={search}
  onSearchChange={setSearch}
  filters={[
    { type: 'dropdown', key: 'specialty', label: 'All specialties',
      options: specialtyOptions, value: specialty, onChange: setSpecialty },
    { type: 'dropdown', key: 'sort', label: 'Sort by',
      options: SORT_OPTIONS, value: sortBy, onChange: setSortBy },
  ]}
  resultCount={data?.totalCount}
  resultLabel="doctors"
/>
```

Remove: `.doctor-search-controls` CSS and the inline `<Input>`/`<Select>` elements.

### AuditLogPage

Current: four separate `<input>` elements in `.admin-toolbar`

New:
```tsx
<SearchFilterBar
  searchPlaceholder="Filter by action..."
  searchValue={actionFilter}
  onSearchChange={setActionFilter}
  filters={[
    { type: 'text', key: 'target', placeholder: 'Target ID...',
      value: targetFilter, onChange: setTargetFilter },
    { type: 'date', key: 'from', label: 'From',
      value: fromFilter, onChange: setFromFilter },
    { type: 'date', key: 'to', label: 'To',
      value: toFilter, onChange: setToFilter },
  ]}
  resultCount={data?.totalCount}
  resultLabel="logs"
/>
```

### AdminUsersPage

Current: search `<input>` + role filter tabs + custom styling

New: Replace the search input and role tabs with `SearchFilterBar`. The role filter tabs become a `dropdown` filter with options: All, Patient, Doctor. The search input maps to `searchValue`.

---

## Login form error fix

### Problem

The `.auth-error` paragraph in `LoginPage.tsx` is conditionally rendered, causing the form to grow when it appears and shrink when it disappears.

### Solution

Add a fixed-height error region (`.auth-error-slot`) between the last input field and the submit button:

```css
.auth-error-slot {
  min-height: 24px;
  display: flex;
  align-items: center;
}
```

The error text renders inside this slot. When no error, the slot is empty but still takes up `min-height: 24px`. The error fades in with a CSS transition (`opacity 0 → 1`).

Affected files: `LoginPage.tsx`, `RegisterPage.tsx`, `index.css`.

---

## Testing strategy

- **SearchFilterBar unit tests**: render with various filter configs, verify pills render, test dropdown open/close, test active filter dismiss, test keyboard navigation, test search input debounce passthrough
- **FilterPill unit tests**: dropdown variant opens popover, text variant renders input, date variant renders date picker, active state shows dismiss button
- **Page migration tests**: update existing DoctorSearch, AuditLogPage, AdminUsersPage component tests to use new selectors (getByPlaceholder, getByRole for combobox)
- **Playwright e2e**: update existing e2e tests where filter selectors changed. Verify the visual appearance matches the design in Storybook stories.
- **Storybook stories**: SearchFilterBar with doctor search config, admin users config, audit log config, empty state, loading state

---

## Scope summary

| Item | Type | Effort |
|------|------|--------|
| SearchFilterBar component | New shared component | Medium |
| FilterPill sub-component | New shared component | Medium |
| DoctorSearch migration | Refactor | Small |
| AuditLogPage migration | Refactor | Small |
| AdminUsersPage migration | Refactor | Small |
| Login error spacing fix | CSS fix | Tiny |
| Tests + stories | Testing | Medium |
| CSS cleanup (remove old classes) | Cleanup | Small |
