import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import RangePills from './RangePills';

describe('RangePills', () => {
  it('renders 4 pill buttons', () => {
    render(<RangePills value="7d" onSelect={vi.fn()} />);

    const pills = document.querySelectorAll('.analytics-filter-pill');
    expect(pills.length).toBe(4);
  });

  it('active pill has .active class', () => {
    render(<RangePills value="30d" onSelect={vi.fn()} />);

    const activeBtn = screen.getByRole('button', { name: '30d' });
    expect(activeBtn.classList.contains('active')).toBe(true);
  });

  it('clicking a pill calls onSelect with correct period value', async () => {
    const user = userEvent.setup();
    const onSelect = vi.fn();
    render(<RangePills value="7d" onSelect={onSelect} />);

    await user.click(screen.getByRole('button', { name: '90d' }));

    expect(onSelect).toHaveBeenCalledTimes(1);
    expect(onSelect).toHaveBeenCalledWith('90d');
  });

  it('only one pill has .active class at a time', () => {
    render(<RangePills value="ytd" onSelect={vi.fn()} />);

    const activePills = document.querySelectorAll('.analytics-filter-pill.active');
    expect(activePills.length).toBe(1);
  });
});
