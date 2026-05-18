import type { Meta, StoryObj } from '@storybook/react';
import AnalyticsGeography from './AnalyticsGeography';
import type { GeographyAnalyticsDto } from '../../../shared/api/analytics';

const meta: Meta<typeof AnalyticsGeography> = {
  title: 'Analytics/AnalyticsGeography',
  component: AnalyticsGeography,
  parameters: { layout: 'padded' },
  decorators: [
    (Story) => (
      <div style={{ padding: 24, background: '#0b0e0c', minHeight: '100vh' }}>
        <Story />
      </div>
    ),
  ],
};
export default meta;
type Story = StoryObj<typeof AnalyticsGeography>;

const mockData: GeographyAnalyticsDto = {
  regions: [
    { name: 'Lisbon', consults: 520, deltaPct: 15.2, isTop: true },
    { name: 'Porto', consults: 380, deltaPct: 8.5, isTop: false },
    { name: 'Faro', consults: 120, deltaPct: -3.1, isTop: false },
    { name: 'Coimbra', consults: 95, deltaPct: 22.0, isTop: false },
  ],
  retention: {
    cohorts: [
      { cohortLabel: 'Apr 7', weeks: [100, 68, 45, 32, 28, 24, 20, 18] },
      { cohortLabel: 'Apr 14', weeks: [100, 62, 40, 30, 25, 22, 19] },
      { cohortLabel: 'Apr 21', weeks: [100, 71, 48, 35, 29, 25] },
      { cohortLabel: 'Apr 28', weeks: [100, 65, 42, 33, 28] },
      { cohortLabel: 'May 5', weeks: [100, 58, 38, 30] },
      { cohortLabel: 'May 12', weeks: [100, 72, 50] },
    ],
  },
};

const emptyData: GeographyAnalyticsDto = {
  regions: [],
  retention: { cohorts: [] },
};

export const Default: Story = {
  args: {
    data: mockData,
  },
};

export const Empty: Story = {
  args: {
    data: emptyData,
  },
};
