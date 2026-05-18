import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import AnalyticsCard from './AnalyticsCard';

describe('AnalyticsCard', () => {
  it('renders title text', () => {
    render(
      <AnalyticsCard title="Appointments">
        <p>content</p>
      </AnalyticsCard>,
    );

    expect(screen.getByText('Appointments')).toBeDefined();
  });

  it('renders children', () => {
    render(
      <AnalyticsCard title="Revenue">
        <p>Some chart here</p>
      </AnalyticsCard>,
    );

    expect(screen.getByText('Some chart here')).toBeDefined();
  });

  it('renders action button when action prop provided', () => {
    render(
      <AnalyticsCard
        title="Users"
        action={{ label: 'View all', onClick: vi.fn() }}
      >
        <p>content</p>
      </AnalyticsCard>,
    );

    expect(screen.getByRole('button', { name: 'View all' })).toBeDefined();
  });

  it('clicking action button calls onClick', async () => {
    const user = userEvent.setup();
    const onClick = vi.fn();
    render(
      <AnalyticsCard
        title="Users"
        action={{ label: 'View all', onClick }}
      >
        <p>content</p>
      </AnalyticsCard>,
    );

    await user.click(screen.getByRole('button', { name: 'View all' }));

    expect(onClick).toHaveBeenCalledTimes(1);
  });

  it('does not render action when action prop is undefined', () => {
    render(
      <AnalyticsCard title="No Action">
        <p>content</p>
      </AnalyticsCard>,
    );

    const actionBtn = document.querySelector('.analytics-card-action');
    expect(actionBtn).toBeNull();
  });
});
