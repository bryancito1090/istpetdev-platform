/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/**/*.{html,ts}",
  ],
  theme: {
    extend: {
      colors: {
        // Paleta Oficial Expo Clean Ecuador 2026 (Ref: 03 Arquitectura/Sistema de diseno y colores.md)
        primary: {
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
        },
        secondary: {
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
        },
        accent: {
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
        },
        charcoal: {
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
        },
        surface: {
          ground: '#F8FAFC',
          card: '#FFFFFF',
          border: '#E2E8F0',
          dark: '#1E293B',
        },
        // Tokens de Sistema de Diseño Oficial Stripe (HDS)
        stripe: {
          indigo: '#635BFF',      // Botones primarios y acentos interactivos de Stripe
          indigoHover: '#534BE8', // Hover de botones Stripe
          hero: '#2D2564',        // Títulos de Hero (inspeccionado en DevTools)
          heading: '#061B31',     // Títulos de sección (inspeccionado en DevTools)
          muted: '#425466',       // Subtítulos y textos de soporte de Stripe
          dark: '#0A2540',        // Carbón de navegación y footer
        }
      },
      fontFamily: {
        sans: ['sohne-var', '"SF Pro Display"', '-apple-system', 'BlinkMacSystemFont', '"Segoe UI"', 'Roboto', 'sans-serif'],
        mono: ['"JetBrains Mono"', 'ui-monospace', 'SFMono-Regular', 'Menlo', 'Monaco', 'monospace'],
      },
      boxShadow: {
        'stripe-xs': '0 2px 5px -1px rgba(50, 50, 93, 0.08), 0 1px 3px -1px rgba(0, 0, 0, 0.04)',
        'stripe-sm': '0 4px 6px -1px rgba(50, 50, 93, 0.1), 0 2px 4px -2px rgba(0, 0, 0, 0.06)',
        'stripe-md': '0 8px 18px -4px rgba(50, 50, 93, 0.12), 0 4px 8px -4px rgba(0, 0, 0, 0.08)',
        'stripe-lg': '0 13px 27px -5px rgba(50, 50, 93, 0.15), 0 8px 16px -8px rgba(0, 0, 0, 0.1)',
        'stripe-btn': '0 4px 12px rgba(99, 91, 255, 0.35)',
      }
    },
  },
  plugins: [],
}
