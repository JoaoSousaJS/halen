import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { vi, describe, it, expect } from 'vitest';
import { FilterPill } from './FilterPill';
import type { DropdownFilter, TextFilter, DateFilter } from './SearchFilterBar';

describe('FilterPill — dropdown variant', () => {
  const options = [
    { value: 'cardiology', label: 'Cardiology' },
    { value: 'dermatology', label: 'Dermatology' },
    { value: 'neurology', label: 'Neurology' },
  ];

  function makeDropdown(overrides: Partial<DropdownFilter> = {}): DropdownFilter {
    return {
      type: 'dropdown',
      key: 'specialty',
      label: 'All specialties',
      options,
      value: '',
      onChange: vi.fn(),
      ...overrides,
    };
  }

  it('renders inactive pill with label and chevron', () => {
    render(<FilterPill filter={makeDropdown()} />);
    const btn = screen.getByRole('button', { name: /all specialties/i });
    expect(btn).toBeInTheDocument();
    expect(btn).toHaveAttribute('aria-expanded', 'false');
  });

  it('opens popover on click', async () => {
    const user = userEvent.setup();
    render(<FilterPill filter={makeDropdown()} />);

    await user.click(screen.getByRole('button', { name: /all specialties/i }));

    const listbox = screen.getByRole('listbox');
    expect(listbox).toBeInTheDocument();
    expect(within(listbox).getAllByRole('option')).toHaveLength(3);
  });

  it('selects an option and calls onChange', async () => {
    const onChange = vi.fn();
    const user = userEvent.setup();
    render(<FilterPill filter={makeDropdown({ onChange })} />);

    await user.click(screen.getByRole('button', { name: /all specialties/i }));
    await user.click(screen.getByRole('option', { name: 'Cardiology' }));

    expect(onChange).toHaveBeenCalledWith('cardiology');
  });

  it('closes popover after selection', async () => {
    const user = userEvent.setup();
    render(<FilterPill filter={makeDropdown()} />);

    await user.click(screen.getByRole('button', { name: /all specialties/i }));
    await user.click(screen.getByRole('option', { name: 'Cardiology' }));

    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
  });

  it('renders active pill with selected label and dismiss button', () => {
    render(<FilterPill filter={makeDropdown({ value: 'cardiology' })} />);

    const btn = screen.getByRole('button', { name: /cardiology/i });
    expect(btn).toHaveClass('active');
    expect(screen.getByRole('button', { name: /clear specialty/i })).toBeInTheDocument();
  });

  it('dismiss button calls onChange with empty string', async () => {
    const onChange = vi.fn();
    const user = userEvent.setup();
    render(<FilterPill filter={makeDropdown({ value: 'cardiology', onChange })} />);

    await user.click(screen.getByRole('button', { name: /clear specialty/i }));

    expect(onChange).toHaveBeenCalledWith('');
  });

  it('closes popover on Escape', async () => {
    const user = userEvent.setup();
    render(<FilterPill filter={makeDropdown()} />);

    await user.click(screen.getByRole('button', { name: /all specialties/i }));
    expect(screen.getByRole('listbox')).toBeInTheDocument();

    await user.keyboard('{Escape}');
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
  });

  it('navigates options with arrow keys and selects with Enter', async () => {
    const onChange = vi.fn();
    const user = userEvent.setup();
    render(<FilterPill filter={makeDropdown({ onChange })} />);

    await user.click(screen.getByRole('button', { name: /all specialties/i }));
    await user.keyboard('{ArrowDown}');
    await user.keyboard('{ArrowDown}');
    await user.keyboard('{Enter}');

    expect(onChange).toHaveBeenCalledWith('dermatology');
  });

  it('closes popover on click outside', async () => {
    const user = userEvent.setup();
    render(
      <div>
        <FilterPill filter={makeDropdown()} />
        <button>Other</button>
      </div>,
    );

    await user.click(screen.getByRole('button', { name: /all specialties/i }));
    expect(screen.getByRole('listbox')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Other' }));
    expect(screen.queryByRole('listbox')).not.toBeInTheDocument();
  });

  it('renders disabled pill when options array is empty', () => {
    render(<FilterPill filter={makeDropdown({ options: [] })} />);
    const btn = screen.getByRole('button', { name: /all specialties/i });
    expect(btn).toBeDisabled();
  });
});

describe('FilterPill — text variant', () => {
  function makeText(overrides: Partial<TextFilter> = {}): TextFilter {
    return {
      type: 'text',
      key: 'target',
      placeholder: 'Target ID...',
      value: '',
      onChange: vi.fn(),
      ...overrides,
    };
  }

  it('renders a text input with placeholder', () => {
    render(<FilterPill filter={makeText()} />);
    expect(screen.getByPlaceholderText('Target ID...')).toBeInTheDocument();
  });

  it('calls onChange on input', async () => {
    const onChange = vi.fn();
    const user = userEvent.setup();
    render(<FilterPill filter={makeText({ onChange })} />);

    await user.type(screen.getByPlaceholderText('Target ID...'), 'abc');

    expect(onChange).toHaveBeenCalledTimes(3);
    expect(onChange).toHaveBeenNthCalledWith(1, 'a');
  });

  it('displays the controlled value', () => {
    render(<FilterPill filter={makeText({ value: 'test-id' })} />);
    expect(screen.getByDisplayValue('test-id')).toBeInTheDocument();
  });
});

describe('FilterPill — date variant', () => {
  function makeDate(overrides: Partial<DateFilter> = {}): DateFilter {
    return {
      type: 'date',
      key: 'from',
      label: 'From',
      value: '',
      onChange: vi.fn(),
      ...overrides,
    };
  }

  it('renders a date input with aria-label', () => {
    render(<FilterPill filter={makeDate()} />);
    expect(screen.getByLabelText('From date')).toBeInTheDocument();
  });

  it('calls onChange when date is selected', async () => {
    const onChange = vi.fn();
    const user = userEvent.setup();
    render(<FilterPill filter={makeDate({ onChange })} />);

    const input = screen.getByLabelText('From date');
    await user.clear(input);
    await user.type(input, '2026-05-19');

    expect(onChange).toHaveBeenCalled();
  });

  it('displays controlled value', () => {
    render(<FilterPill filter={makeDate({ value: '2026-05-19' })} />);
    expect(screen.getByDisplayValue('2026-05-19')).toBeInTheDocument();
  });
});
