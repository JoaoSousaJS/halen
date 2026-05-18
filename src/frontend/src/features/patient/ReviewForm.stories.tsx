import type { Meta, StoryObj } from '@storybook/react';
import { fn } from 'storybook/test';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import ReviewForm from './ReviewForm';

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false } },
});

const meta: Meta<typeof ReviewForm> = {
  title: 'Reviews/ReviewForm',
  component: ReviewForm,
  parameters: { layout: 'fullscreen' },
  decorators: [
    (Story) => (
      <QueryClientProvider client={queryClient}>
        <Story />
      </QueryClientProvider>
    ),
  ],
  args: {
    appointmentId: 'apt-1',
    patientFirstName: 'Maya',
    patientLastInitial: 'C',
    onClose: fn(),
    onSuccess: fn(),
  },
};
export default meta;

type Story = StoryObj<typeof ReviewForm>;

export const Default: Story = {
  parameters: {
    msw: {
      handlers: [
        http.post('*/api/v1/reviews', () =>
          HttpResponse.json({ reviewId: 'rev-new-1' }),
        ),
      ],
    },
  },
};

export const WithPrefilledData: Story = {
  args: {
    patientFirstName: 'Carlos',
    patientLastInitial: 'R',
    appointmentId: 'apt-2',
  },
  parameters: {
    msw: {
      handlers: [
        http.post('*/api/v1/reviews', () =>
          HttpResponse.json({ reviewId: 'rev-new-2' }),
        ),
      ],
    },
  },
};

export const AllTagsSelected: Story = {
  name: 'All Tags Selected',
  parameters: {
    docs: {
      description: {
        story: 'Shows the form when 6 tags are selected. Since tag selection is internal state, interact with the form to select tags.',
      },
    },
    msw: {
      handlers: [
        http.post('*/api/v1/reviews', () =>
          HttpResponse.json({ reviewId: 'rev-new-3' }),
        ),
      ],
    },
  },
};
