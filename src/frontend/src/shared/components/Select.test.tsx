import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Select } from './Select';

const sampleOptions = [
  { value: 'dr-chen', label: 'Dr. Chen' },
  { value: 'dr-patel', label: 'Dr. Patel' },
  { value: 'dr-silva', label: 'Dr. Silva' },
];

describe('Select', () => {
  it('renders all options from the options array', () => {
    render(<Select options={sampleOptions} />);

    const options = screen.getAllByRole('option');
    expect(options).toHaveLength(3);
    expect(options[0]).toHaveTextContent('Dr. Chen');
    expect(options[1]).toHaveTextContent('Dr. Patel');
    expect(options[2]).toHaveTextContent('Dr. Silva');
  });

  it('sets correct value attributes on options', () => {
    render(<Select options={sampleOptions} />);

    const options = screen.getAllByRole('option');
    expect(options[0]).toHaveAttribute('value', 'dr-chen');
    expect(options[1]).toHaveAttribute('value', 'dr-patel');
    expect(options[2]).toHaveAttribute('value', 'dr-silva');
  });

  it('renders placeholder as first option with empty value', () => {
    render(<Select options={sampleOptions} placeholder="Select a doctor" />);

    const options = screen.getAllByRole('option');
    expect(options).toHaveLength(4);
    expect(options[0]).toHaveTextContent('Select a doctor');
    expect(options[0]).toHaveAttribute('value', '');
  });

  it('does not render placeholder option when placeholder is not provided', () => {
    render(<Select options={sampleOptions} />);

    const options = screen.getAllByRole('option');
    expect(options).toHaveLength(3);
    expect(options[0]).toHaveTextContent('Dr. Chen');
  });

  it('forwards onChange handler', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();

    render(<Select options={sampleOptions} onChange={onChange} />);

    await user.selectOptions(screen.getByRole('combobox'), 'dr-patel');
    expect(onChange).toHaveBeenCalled();
  });

  it('forwards native select attributes', () => {
    render(
      <Select
        options={sampleOptions}
        disabled
        aria-label="Doctor select"
      />,
    );

    const select = screen.getByRole('combobox');
    expect(select).toBeDisabled();
    expect(select).toHaveAttribute('aria-label', 'Doctor select');
  });

  it('applies className when provided', () => {
    render(<Select options={sampleOptions} className="custom-select" />);

    expect(screen.getByRole('combobox')).toHaveClass('custom-select');
  });

  it('renders with an empty options array', () => {
    render(<Select options={[]} placeholder="No options" />);

    const options = screen.getAllByRole('option');
    expect(options).toHaveLength(1);
    expect(options[0]).toHaveTextContent('No options');
  });
});
