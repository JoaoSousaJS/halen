import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Field } from './Field';

describe('Field', () => {
  it('renders label text and children', () => {
    render(
      <Field label="Email">
        <input data-testid="email-input" />
      </Field>,
    );

    expect(screen.getByText('Email')).toBeInTheDocument();
    expect(screen.getByTestId('email-input')).toBeInTheDocument();
  });

  it('wraps in a label element with field class', () => {
    const { container } = render(
      <Field label="Name">
        <input />
      </Field>,
    );

    const label = container.querySelector('label');
    expect(label).toHaveClass('field');
    expect(label?.className).toBe('field');
  });

  it('renders label span before children in default mode', () => {
    const { container } = render(
      <Field label="Name">
        <input data-testid="input" />
      </Field>,
    );

    const label = container.querySelector('label')!;
    const span = label.querySelector('span');
    const input = label.querySelector('[data-testid="input"]');
    // span should come before input in DOM order
    expect(span!.compareDocumentPosition(input!)).toBe(
      Node.DOCUMENT_POSITION_FOLLOWING,
    );
  });

  it('adds field-inline class when inline is true', () => {
    const { container } = render(
      <Field label="Remember me" inline>
        <input type="checkbox" />
      </Field>,
    );

    const label = container.querySelector('label');
    expect(label).toHaveClass('field', 'field-inline');
  });

  it('renders children before label span when inline', () => {
    const { container } = render(
      <Field label="Remember me" inline>
        <input data-testid="checkbox" type="checkbox" />
      </Field>,
    );

    const label = container.querySelector('label')!;
    const span = label.querySelector('span');
    const input = label.querySelector('[data-testid="checkbox"]');
    // children should come before span in inline mode
    expect(input!.compareDocumentPosition(span!)).toBe(
      Node.DOCUMENT_POSITION_FOLLOWING,
    );
  });

  it('renders a div with field-row class when row is true', () => {
    const { container } = render(
      <Field label="Group" row>
        <button>A</button>
        <button>B</button>
      </Field>,
    );

    const div = container.querySelector('.field-row');
    expect(div).toBeInTheDocument();
    expect(div?.tagName).toBe('DIV');
    // should not render a label element
    expect(container.querySelector('label')).toBeNull();
  });

  it('renders hint text when hint is provided', () => {
    render(
      <Field label="Password" hint="At least 8 characters">
        <input type="password" />
      </Field>,
    );

    const hint = screen.getByText('At least 8 characters');
    expect(hint).toBeInTheDocument();
    expect(hint).toHaveClass('field-hint');
  });

  it('does not render hint span when hint is not provided', () => {
    const { container } = render(
      <Field label="Email">
        <input />
      </Field>,
    );

    expect(container.querySelector('.field-hint')).toBeNull();
  });
});
