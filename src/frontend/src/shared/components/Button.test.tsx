import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Button } from './Button';

describe('Button', () => {
  it('renders with base btn class only when no variant/size/block', () => {
    render(<Button>Click me</Button>);

    const button = screen.getByRole('button', { name: 'Click me' });
    expect(button).toHaveClass('btn');
    expect(button.className).toBe('btn');
  });

  it('applies variant class for primary', () => {
    render(<Button variant="primary">Save</Button>);

    expect(screen.getByRole('button')).toHaveClass('btn', 'btn-primary');
  });

  it('applies variant class for danger', () => {
    render(<Button variant="danger">Delete</Button>);

    expect(screen.getByRole('button')).toHaveClass('btn', 'btn-danger');
  });

  it('applies variant class for ghost', () => {
    render(<Button variant="ghost">Cancel</Button>);

    expect(screen.getByRole('button')).toHaveClass('btn', 'btn-ghost');
  });

  it('applies size class for sm', () => {
    render(<Button size="sm">Small</Button>);

    expect(screen.getByRole('button')).toHaveClass('btn', 'btn-sm');
  });

  it('applies btn-block when block is true', () => {
    render(<Button block>Full width</Button>);

    expect(screen.getByRole('button')).toHaveClass('btn', 'btn-block');
  });

  it('composes all classes together', () => {
    render(
      <Button variant="primary" size="sm" block>
        All props
      </Button>,
    );

    const button = screen.getByRole('button');
    expect(button).toHaveClass('btn', 'btn-primary', 'btn-sm', 'btn-block');
  });

  it('merges external className', () => {
    render(<Button className="extra">Merged</Button>);

    const button = screen.getByRole('button');
    expect(button).toHaveClass('btn', 'extra');
  });

  it('sets aria-label from ariaLabel prop', () => {
    render(<Button ariaLabel="Close dialog">X</Button>);

    expect(screen.getByRole('button', { name: 'Close dialog' })).toBeInTheDocument();
  });

  it('does not render aria-label when ariaLabel is not provided', () => {
    render(<Button>Plain</Button>);

    expect(screen.getByRole('button')).not.toHaveAttribute('aria-label');
  });

  it('forwards onClick handler', async () => {
    const user = userEvent.setup();
    const onClick = vi.fn();

    render(<Button onClick={onClick}>Click</Button>);

    await user.click(screen.getByRole('button'));
    expect(onClick).toHaveBeenCalledOnce();
  });

  it('spreads native button attributes', () => {
    render(
      <Button type="submit" disabled>
        Submit
      </Button>,
    );

    const button = screen.getByRole('button');
    expect(button).toHaveAttribute('type', 'submit');
    expect(button).toBeDisabled();
  });
});
