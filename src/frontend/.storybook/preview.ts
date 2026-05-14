import type { Preview } from '@storybook/react-vite';
import { initialize, mswLoader } from 'msw-storybook-addon';
import '../src/index.css';

initialize();

const preview: Preview = {
  parameters: {
    backgrounds: {
      default: 'app',
      values: [
        { name: 'app', value: '#0f0f11' },
        { name: 'light', value: '#ffffff' },
      ],
    },
    layout: 'fullscreen',
  },
  loaders: [mswLoader],
};

export default preview;
