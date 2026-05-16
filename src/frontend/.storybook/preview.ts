import type { Preview } from '@storybook/react-vite';
import { initialize, mswLoader } from 'msw-storybook-addon';
import '../src/index.css';

initialize({
  serviceWorker: {
    // Vite injects BASE_URL at build time ("/halen/" in CI, "/" locally)
    url: import.meta.env.BASE_URL + 'mockServiceWorker.js',
  },
});

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
