import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { VideoTile } from '../components/VideoTile';

describe('VideoTile', () => {
  it('renders initials from full name', () => {
    render(<VideoTile name="Joao Lopes" size="lg" />);
    expect(screen.getByText('JL')).toBeDefined();
  });

  it('renders initials from single name', () => {
    render(<VideoTile name="Joao" size="lg" />);
    expect(screen.getByText('J')).toBeDefined();
  });

  it('applies lg size class', () => {
    const { container } = render(<VideoTile name="Test User" size="lg" />);
    expect(container.firstElementChild).toHaveClass('vc-tile-lg');
  });

  it('applies sm size class', () => {
    const { container } = render(<VideoTile name="Test User" size="sm" />);
    expect(container.firstElementChild).toHaveClass('vc-tile-sm');
  });

  it('applies pip size class', () => {
    const { container } = render(<VideoTile name="Test User" size="pip" />);
    expect(container.firstElementChild).toHaveClass('vc-tile-pip');
  });

  it('shows muted indicator when isMuted is true', () => {
    render(<VideoTile name="Test User" size="lg" isMuted />);
    expect(screen.getByLabelText('Muted')).toBeDefined();
  });

  it('does not show muted indicator by default', () => {
    render(<VideoTile name="Test User" size="lg" />);
    expect(screen.queryByLabelText('Muted')).toBeNull();
  });
});
