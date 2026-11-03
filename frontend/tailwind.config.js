/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/**/*.{html,ts}"
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
      },
      colors: {
        primary: {
          DEFAULT: '#0F766E',
          hover: '#115E59',
          soft: '#CCFBF1',
          dark: '#115E59',
        },
        accent: {
          DEFAULT: '#B45309',
          hover: '#92400E',
        },
        success: {
          DEFAULT: '#15803D',
          soft: '#DCFCE7',
          dark: '#166534',
        },
        warning: {
          DEFAULT: '#B45309',
          soft: '#FEF3C7',
          dark: '#92400E',
        },
        danger: {
          DEFAULT: '#B91C1C',
          soft: '#FEE2E2',
          dark: '#991B1B',
        },
        info: {
          DEFAULT: '#1D4ED8',
          soft: '#DBEAFE',
          dark: '#1E40AF',
        },
        surface: {
          DEFAULT: '#FFFFFF',
          bg: '#F8FAFC',
          card: '#FFFFFF',
          border: '#E2E8F0',
        },
        text: {
          main: '#0F172A',
          muted: '#475569',
          subtle: '#64748B',
        },
        background: '#F8FAFC',
        border: {
          DEFAULT: '#CBD5E1',
          input: '#64748B',
        },
      },
      borderRadius: {
        card: '12px',
        input: '8px',
        btn: '8px',
      },
      boxShadow: {
        card: '0 4px 6px -1px rgba(15, 23, 42, 0.05), 0 2px 4px -2px rgba(15, 23, 42, 0.05)',
      }
    },
  },
  plugins: [],
};
