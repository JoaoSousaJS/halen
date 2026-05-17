import type { Meta, StoryObj } from '@storybook/react';
import { VideoTile } from './VideoTile';

const meta: Meta<typeof VideoTile> = {
  title: 'VideoConsultation/VideoTile',
  component: VideoTile,
  parameters: { layout: 'centered' },
  decorators: [
    (Story) => (
      <div style={{ padding: 24, background: '#0b0e0c' }}>
        <Story />
      </div>
    ),
  ],
};
export default meta;

type Story = StoryObj<typeof VideoTile>;

export const Large: Story = {
  args: { name: 'Dr House', size: 'lg' },
};

export const Small: Story = {
  args: { name: 'Pat Ient', size: 'sm' },
};

export const PictureInPicture: Story = {
  args: { name: 'Pat Ient', size: 'pip' },
};

export const Muted: Story = {
  args: { name: 'Dr House', size: 'sm', isMuted: true },
};

export const SingleName: Story = {
  args: { name: 'Admin', size: 'sm' },
};

export const Grid: Story = {
  render: () => (
    <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12, width: 600 }}>
      <VideoTile name="Dr House" size="sm" />
      <VideoTile name="Pat Ient" size="sm" isMuted />
      <VideoTile name="Dr Wilson" size="sm" />
      <VideoTile name="Lisa Cuddy" size="sm" />
    </div>
  ),
};
