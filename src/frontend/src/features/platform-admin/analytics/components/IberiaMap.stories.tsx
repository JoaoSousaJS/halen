import type { Meta, StoryObj } from '@storybook/react';
import IberiaMap from './IberiaMap';

const meta: Meta<typeof IberiaMap> = {
  title: 'Analytics/IberiaMap',
  component: IberiaMap,
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
type Story = StoryObj<typeof IberiaMap>;

export const Default: Story = {
  args: {
    regions: [
      { name: 'Lisbon', consults: 520, deltaPct: 15.2, isTop: true },
      { name: 'Porto', consults: 380, deltaPct: 8.5, isTop: false },
      { name: 'Faro', consults: 120, deltaPct: -3.1, isTop: false },
      { name: 'Coimbra', consults: 95, deltaPct: 22.0, isTop: false },
    ],
  },
};

export const SingleRegion: Story = {
  args: {
    regions: [
      { name: 'Lisbon', consults: 520, deltaPct: 15.2, isTop: true },
    ],
  },
};

export const Empty: Story = {
  args: {
    regions: [],
  },
};
