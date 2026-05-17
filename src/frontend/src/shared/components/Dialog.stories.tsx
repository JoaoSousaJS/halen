import type { Meta, StoryObj } from '@storybook/react';
import { Dialog, DialogActions } from './Dialog';
import { Button } from './Button';
import { Field } from './Field';
import { Input } from './Input';
import { Select } from './Select';
import { fn } from 'storybook/test';

const meta: Meta<typeof Dialog> = {
  title: 'Shared/Dialog',
  component: Dialog,
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

type Story = StoryObj<typeof Dialog>;

export const Default: Story = {
  args: {
    title: 'Confirm action',
    onClose: fn(),
    children: <p style={{ padding: '0 24px' }}>Are you sure you want to proceed?</p>,
  },
};

export const WithSubtitle: Story = {
  args: {
    title: 'Book appointment',
    subtitle: 'Select a date and time for your consultation',
    onClose: fn(),
    children: <p style={{ padding: '0 24px' }}>Appointment booking form goes here.</p>,
  },
};

export const Wide: Story = {
  args: {
    title: 'Patient records',
    subtitle: 'Full medical history overview',
    wide: true,
    onClose: fn(),
    children: (
      <div style={{ padding: '0 24px' }}>
        <p>This dialog uses the wide variant for content that needs more horizontal space.</p>
        <table style={{ width: '100%', marginTop: 12 }}>
          <thead>
            <tr>
              <th style={{ textAlign: 'left' }}>Date</th>
              <th style={{ textAlign: 'left' }}>Doctor</th>
              <th style={{ textAlign: 'left' }}>Notes</th>
            </tr>
          </thead>
          <tbody>
            <tr><td>2026-05-10</td><td>Dr. House</td><td>Follow-up visit</td></tr>
            <tr><td>2026-04-22</td><td>Dr. Wilson</td><td>Initial consultation</td></tr>
          </tbody>
        </table>
      </div>
    ),
  },
};

export const WithActions: Story = {
  args: {
    title: 'Delete appointment',
    onClose: fn(),
    children: (
      <>
        <p style={{ padding: '0 24px' }}>
          This will permanently cancel your appointment with Dr. House on May 15, 2026.
          This action cannot be undone.
        </p>
        <DialogActions>
          <Button variant="ghost">Cancel</Button>
          <Button variant="danger">Delete</Button>
        </DialogActions>
      </>
    ),
  },
};

export const WithFormContent: Story = {
  args: {
    title: 'Create user',
    subtitle: 'Add a new user to your clinic',
    onClose: fn(),
    children: (
      <>
        <div style={{ padding: '0 24px', display: 'flex', flexDirection: 'column', gap: 12 }}>
          <Field label="Full name">
            <Input placeholder="Maya Chen" />
          </Field>
          <Field label="Email">
            <Input type="email" placeholder="maya@clinic.com" />
          </Field>
          <Field label="Role">
            <Select
              options={[
                { value: 'patient', label: 'Patient' },
                { value: 'doctor', label: 'Doctor' },
              ]}
              placeholder="Select role..."
            />
          </Field>
        </div>
        <DialogActions>
          <Button variant="ghost">Cancel</Button>
          <Button variant="primary">Create user</Button>
        </DialogActions>
      </>
    ),
  },
};

export const MinimalContent: Story = {
  args: {
    title: 'Notice',
    onClose: fn(),
    children: <p style={{ padding: '0 24px 24px' }}>Session will expire in 5 minutes.</p>,
  },
};
