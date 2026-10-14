const isBrowser = typeof window !== 'undefined';
const isLocalhost = isBrowser && (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1');

export const environment = {
  production: !isLocalhost,
  apiUrl: isLocalhost ? 'http://localhost:5000' : '',
  appName: 'IstpetDev Web',
  version: '1.0.0'
};
