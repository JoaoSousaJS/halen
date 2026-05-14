import type { Meta, StoryObj } from '@storybook/react';
import { ToastContainer } from './ToastContainer';
import type { Toast } from '../hooks/useNotifications';

const meta: Meta<typeof ToastContainer> = {
  title: 'Shared/ToastContainer',
  component: ToastContainer,
  parameters: { layout: 'fullscreen' },
  decorators: [
    (Story) => (
      <div style={{ padding: 24, minHeight: 200, background: '#0b0e0c' }}>
        <Story />
      </div>
    ),
  ],
};
export default meta;

type Story = StoryObj<typeof ToastContainer>;

const booked: Toast = {
  id: '1',
  message: 'New appointment with Maya Chen on May 15, 2026 at 14:00',
  type: 'appointment.booked',
  timestamp: Date.now(),
};

const cancelled: Toast = {
  id: '2',
  message: 'Appointment cancelled by Maya Chen (Patient)',
  type: 'appointment.cancelled',
  timestamp: Date.now(),
};

const completed: Toast = {
  id: '3',
  message: 'Your appointment with Dr. House has been marked as completed',
  type: 'appointment.completed',
  timestamp: Date.now(),
};

export const Single: Story = {
  args: {
    toasts: [booked],
    onDismiss: () => {},
  },
};

export const Multiple: Story = {
  args: {
    toasts: [booked, cancelled, completed],
    onDismiss: () => {},
  },
};

export const Empty: Story = {
  args: {
    toasts: [],
    onDismiss: () => {},
  },
};
