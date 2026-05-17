import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Input } from './Input';

describe('Input', () => {
  it('renders an input with the base input class', () => {
    render(<Input data-testid="field" />);

    const input = screen.getByTestId('field');
    expect(input.tagName).toBe('INPUT');
    expect(input).toHaveClass('input');
    expect(input.className).toBe('input');
  });

  it('merges additional className with base input class', () => {
    render(<Input data-testid="field" className="extra" />);

    const input = screen.getByTestId('field');
    expect(input).toHaveClass('input', 'extra');
  });

  it('forwards native input attributes', () => {
    render(
      <Input
        type="email"
        placeholder="you@example.com"
        required
        data-testid="field"
      />,
    );

    const input = screen.getByTestId('field');
    expect(input).toHaveAttribute('type', 'email');
    expect(input).toHaveAttribute('placeholder', 'you@example.com');
    expect(input).toBeRequired();
  });

  it('forwards onChange handler', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();

    render(<Input data-testid="field" onChange={onChange} />);

    await user.type(screen.getByTestId('field'), 'hello');
    expect(onChange).toHaveBeenCalled();
  });

  it('works without any extra className', () => {
    render(<Input data-testid="field" />);

    expect(screen.getByTestId('field').className).toBe('input');
  });
});
