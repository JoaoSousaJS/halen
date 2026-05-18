import type { Meta, StoryObj } from '@storybook/react';
import AnalyticsDoctors from './AnalyticsDoctors';
import type { DoctorAnalyticsDto } from '../../../shared/api/analytics';

const meta: Meta<typeof AnalyticsDoctors> = {
  title: 'Analytics/AnalyticsDoctors',
  component: AnalyticsDoctors,
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
type Story = StoryObj<typeof AnalyticsDoctors>;

const mockData: DoctorAnalyticsDto = {
  ranked: [
    { name: 'Dr. Ana Costa', specialty: 'Cardiology', consults: 85, completionPct: 96, rating: 4.8, revenue: 12750, trend: [18, 22, 20, 25], badge: 'Top Performer' },
    { name: 'Dr. Bruno Silva', specialty: 'General', consults: 72, completionPct: 91, rating: 4.5, revenue: 8640, trend: [15, 18, 20, 19], badge: null },
    { name: 'Dr. Clara Mendes', specialty: 'Dermatology', consults: 65, completionPct: 88, rating: 4.3, revenue: 9750, trend: [12, 15, 18, 20], badge: 'Rising Star' },
    { name: 'Dr. Diogo Ferreira', specialty: 'Cardiology', consults: 58, completionPct: 85, rating: 4.1, revenue: 8700, trend: [14, 13, 15, 16], badge: null },
    { name: 'Dr. Elena Santos', specialty: 'General', consults: 45, completionPct: 93, rating: 4.6, revenue: 5400, trend: [10, 11, 12, 12], badge: null },
  ],
  topRated: [
    { name: 'Dr. Ana Costa', rating: 4.8, reviewCount: 124, specialty: 'Cardiology' },
    { name: 'Dr. Elena Santos', rating: 4.6, reviewCount: 87, specialty: 'General' },
  ],
  needsAttention: [
    { name: 'Dr. Rui Oliveira', message: 'Completion rate 72% (below 85%)', severity: 'danger' },
    { name: 'Dr. Sofia Lima', message: 'Rating 3.2 (below 3.5)', severity: 'danger' },
  ],
};

export const Default: Story = {
  args: {
    data: mockData,
  },
};

export const NoNeedsAttention: Story = {
  args: {
    data: {
      ...mockData,
      needsAttention: [],
    },
  },
};

export const Empty: Story = {
  args: {
    data: {
      ranked: [],
      topRated: [],
      needsAttention: [],
    },
  },
};
