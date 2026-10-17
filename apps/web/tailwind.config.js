const cleanCyan = {
  50: '#E6F7FA',
  100: '#C1EDF3',
  200: '#97E2EC',
  300: '#5CCFE0',
  400: '#26BFD5',
  500: '#00A7C4', // Clean Cyan oficial
  600: '#008CA5',
  700: '#026E82',
  800: '#085868',
  900: '#0C4956',
  950: '#04303B',
};

const expoLime = {
  50: '#F6FBD4',
  100: '#ECF89F',
  200: '#DFF366',
  300: '#CEEC2D',
  400: '#B6DD0D',
  500: '#9EC200', // Expo Lime oficial
  600: '#84A300',
  700: '#647B03',
  800: '#4F600A',
  900: '#42510D',
  950: '#222D02',
};

const actionOrange = {
  50: '#FEEFE8',
  100: '#FDDAC8',
  200: '#FAB491',
  300: '#F68953',
  400: '#F16F2C',
  500: '#EA601F', // Energy Orange oficial (CTA)
  600: '#D44F12',
  700: '#B03D0F',
  800: '#8D3213',
  900: '#732B13',
  950: '#3F1306',
};

const charcoalSlate = {
  50: '#F8FAFC',
  100: '#F1F5F9',
  200: '#E2E8F0',
  300: '#CBD5E1',
  400: '#94A3B8',
  500: '#64748B',
  600: '#475569',
  700: '#334155',
  800: '#2D3748', // Charcoal Slate tipográfico oficial
  900: '#1E293B',
  950: '#0F172A',
};

/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/**/*.{html,ts}",
  ],
  theme: {
    extend: {
      colors: {
        // Paleta Oficial Expo Clean Ecuador 2026 Refinada (Azul Eléctrico, Cyan, Menta)
        expo: {
          blue: '#0066FF',
          blueDark: '#0052FF',
          blueHover: '#0047E0',
          cyan: '#00C2FF',
          cyanLight: '#00E5FF',
          cyanDeep: '#00A7C4',
          mint: '#00E5A3',
          mintLight: '#00F5A0',
          mintDark: '#10B981',
          dark: '#0A1128',
          navy: '#061224',
          surface: '#F8FAFC',
        },
        // Paleta Oficial Expo Clean Ecuador 2026 (Ref: 03 Arquitectura/Sistema de diseno y colores.md)
        primary: cleanCyan,
        'clean-cyan': cleanCyan,
        secondary: expoLime,
        'expo-lime': expoLime,
        accent: actionOrange,
        'action-orange': actionOrange,
        charcoal: charcoalSlate,
        surface: {
          ground: '#F8FAFC',
          card: '#FFFFFF',
          border: '#E2E8F0',
          dark: '#1E293B',
        },
        // Compatibilidad con temas derivados hacia tokens oficiales de marca
        stripe: {
          indigo: '#0066FF',      // Mapeado a Azul Eléctrico Expo Clean
          indigoHover: '#0052FF', // Mapeado a Azul Hover
          hero: '#0A1128',        // Mapeado a Dark Navy
          heading: '#0A1128',     // Mapeado a Dark Navy
          muted: '#64748B',       // Mapeado a Charcoal 500
          dark: '#061224',        // Mapeado a Midnight Navy
        }
      },
      fontFamily: {
        sans: ['"Plus Jakarta Sans"', 'Inter', '"SF Pro Display"', '-apple-system', 'BlinkMacSystemFont', '"Segoe UI"', 'Roboto', 'sans-serif'],
        display: ['"Hina Mincho"', 'serif'],
        hina: ['"Hina Mincho"', 'serif'],
        zen: ['"Zen Kaku Gothic New"', 'sans-serif'],
        mono: ['"JetBrains Mono"', 'ui-monospace', 'SFMono-Regular', 'Menlo', 'Monaco', 'monospace'],
      },
      boxShadow: {
        '2xs': '0 1px 2px 0 rgba(0, 0, 0, 0.05)',
        'stripe-xs': '0 2px 5px -1px rgba(45, 55, 72, 0.08), 0 1px 3px -1px rgba(0, 0, 0, 0.04)',
        'stripe-sm': '0 4px 6px -1px rgba(45, 55, 72, 0.1), 0 2px 4px -2px rgba(0, 0, 0, 0.06)',
        'stripe-md': '0 8px 18px -4px rgba(45, 55, 72, 0.12), 0 4px 8px -4px rgba(0, 0, 0, 0.08)',
        'stripe-lg': '0 13px 27px -5px rgba(45, 55, 72, 0.15), 0 8px 16px -8px rgba(0, 0, 0, 0.1)',
        'stripe-btn': '0 4px 12px rgba(0, 167, 196, 0.35)',
        'brand-btn': '0 4px 12px rgba(0, 167, 196, 0.35)',
        'cta-btn': '0 4px 12px rgba(234, 96, 31, 0.35)',
      }
    },
  },
  plugins: [],
}
