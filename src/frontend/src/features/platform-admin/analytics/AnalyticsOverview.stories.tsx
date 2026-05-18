import type { Meta, StoryObj } from '@storybook/react';
import AnalyticsOverview from './AnalyticsOverview';
import type { AnalyticsOverviewDto } from '../../../shared/api/analytics';

const meta: Meta<typeof AnalyticsOverview> = {
  title: 'Analytics/AnalyticsOverview',
  component: AnalyticsOverview,
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
type Story = StoryObj<typeof AnalyticsOverview>;

const mockData: AnalyticsOverviewDto = {
  appointmentKpi: { total: 1847, deltaPct: 12.5, sparkline: [40, 42, 38, 45, 50, 48, 52, 55, 58, 60, 57, 62, 65, 68] },
  revenueKpi: { value: 156000, deltaPct: 8.3, sparkline: [4000, 4200, 3800, 4500, 5000, 4800, 5200] },
  activeUsersKpi: { total: 892, deltaPct: -2.1, sparkline: [30, 28, 32, 25, 30, 27, 29] },
  noShowKpi: { rate: 6.8, deltaPct: -1.5, sparkline: [8, 7.5, 7, 6.8, 7.2, 6.5, 6.8] },
  appointmentSeries: {
    labels: ['May 1', 'May 2', 'May 3', 'May 4', 'May 5', 'May 6', 'May 7'],
    current: [45, 52, 38, 61, 55, 48, 50],
    previous: [40, 48, 35, 55, 50, 42, 45],
  },
  revenueSeries: {
    labels: ['W14', 'W15', 'W16', 'W17', 'W18', 'W19', 'W20'],
    values: [18000, 22000, 19500, 24000, 21000, 25000, 23000],
  },
  funnel: [
    { label: 'Booked', value: 1200 },
    { label: 'Scheduled', value: 1020 },
    { label: 'Completed', value: 850 },
    { label: 'Paid', value: 720 },
  ],
  activeUsers: { dau: 127, wau: 456, mau: 892, dauDelta: 5.2, wauDelta: -1.3, mauDelta: -2.1, stickiness: 14.2 },
  clinicBreakdown: [
    { name: 'Lisbon Sul', value: 520 },
    { name: 'Porto Centro', value: 380 },
    { name: 'Faro', value: 210 },
    { name: 'Coimbra', value: 180 },
  ],
  specialtyMix: [
    { label: 'General', value: 620 },
    { label: 'Cardiology', value: 450 },
    { label: 'Dermatology', value: 320 },
    { label: 'Mental health', value: 280 },
    { label: 'Pediatrics', value: 177 },
  ],
};

const emptyData: AnalyticsOverviewDto = {
  appointmentKpi: { total: 0, deltaPct: 0, sparkline: [] },
  revenueKpi: { value: 0, deltaPct: 0, sparkline: [] },
  activeUsersKpi: { total: 0, deltaPct: 0, sparkline: [] },
  noShowKpi: { rate: 0, deltaPct: 0, sparkline: [] },
  appointmentSeries: { labels: [], current: [], previous: [] },
  revenueSeries: { labels: [], values: [] },
  funnel: [],
  activeUsers: { dau: 0, wau: 0, mau: 0, dauDelta: 0, wauDelta: 0, mauDelta: 0, stickiness: 0 },
  clinicBreakdown: [],
  specialtyMix: [],
};

export const Default: Story = {
  args: {
    data: mockData,
    onNavigate: () => {},
  },
};

export const Empty: Story = {
  args: {
    data: emptyData,
    onNavigate: () => {},
  },
};
