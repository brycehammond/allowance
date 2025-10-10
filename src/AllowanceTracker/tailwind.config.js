/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./**/*.{razor,html,cshtml}",
    "./Pages/**/*.razor",
    "./Shared/**/*.razor",
    "./Components/**/*.razor"
  ],
  theme: {
    extend: {
      colors: {
        // Primary Green Palette
        green: {
          50: '#f0f9f4',
          100: '#d1f0df',
          200: '#a3e1c0',
          300: '#72d0a0',
          400: '#4bb885',
          500: '#2da370',  // PRIMARY
          600: '#248c5f',
          700: '#1c6e4a',
          800: '#145537',
          900: '#0d3d27',
        },
        // Secondary Amber/Gold Palette
        amber: {
          50: '#fffbf0',
          100: '#fef3c7',
          200: '#fde68a',
          300: '#fcd34d',
          400: '#fbbf24',
          500: '#f59e0b',  // SECONDARY
          600: '#d97706',
          700: '#b45309',
          800: '#92400e',
          900: '#78350f',
        },
        // Chart Colors
        chart: {
          1: '#2da370',  // Green
          2: '#f59e0b',  // Amber
          3: '#3b82f6',  // Blue
          4: '#8b5cf6',  // Purple
          5: '#ec4899',  // Pink
          6: '#14b8a6',  // Teal
          7: '#f97316',  // Orange
          8: '#06b6d4',  // Cyan
        },
        // Semantic Colors
        success: {
          light: '#d1f0df',
          DEFAULT: '#2da370',
          dark: '#1c6e4a',
        },
        warning: {
          light: '#fef3c7',
          DEFAULT: '#f59e0b',
          dark: '#b45309',
        },
        error: {
          light: '#fee2e2',
          DEFAULT: '#dc2626',
          dark: '#991b1b',
        },
        info: {
          light: '#dbeafe',
          DEFAULT: '#3b82f6',
          dark: '#1e40af',
        },
      },
      fontFamily: {
        sans: [
          '-apple-system',
          'BlinkMacSystemFont',
          '"Segoe UI"',
          'Roboto',
          '"Helvetica Neue"',
          'Arial',
          'sans-serif',
        ],
        mono: [
          'ui-monospace',
          'SFMono-Regular',
          '"SF Mono"',
          'Menlo',
          'Consolas',
          '"Liberation Mono"',
          'monospace',
        ],
      },
      fontSize: {
        xs: ['0.75rem', { lineHeight: '1.5' }],      // 12px
        sm: ['0.875rem', { lineHeight: '1.5' }],     // 14px
        base: ['1rem', { lineHeight: '1.5' }],       // 16px
        lg: ['1.125rem', { lineHeight: '1.5' }],     // 18px
        xl: ['1.25rem', { lineHeight: '1.25' }],     // 20px
        '2xl': ['1.5rem', { lineHeight: '1.25' }],   // 24px
        '3xl': ['1.875rem', { lineHeight: '1.25' }], // 30px
        '4xl': ['2.25rem', { lineHeight: '1' }],     // 36px
        '5xl': ['3rem', { lineHeight: '1' }],        // 48px
        '6xl': ['3.75rem', { lineHeight: '1' }],     // 60px
      },
      spacing: {
        0: '0',
        1: '0.25rem',  // 4px
        2: '0.5rem',   // 8px - BASE
        3: '0.75rem',  // 12px
        4: '1rem',     // 16px - Default
        5: '1.25rem',  // 20px
        6: '1.5rem',   // 24px
        8: '2rem',     // 32px
        10: '2.5rem',  // 40px
        12: '3rem',    // 48px
        16: '4rem',    // 64px
        20: '5rem',    // 80px
        24: '6rem',    // 96px
      },
      borderRadius: {
        none: '0',
        sm: '0.25rem',   // 4px
        DEFAULT: '0.5rem', // 8px
        md: '0.5rem',    // 8px
        lg: '0.75rem',   // 12px
        xl: '1rem',      // 16px
        '2xl': '1.5rem', // 24px
        full: '9999px',
      },
      boxShadow: {
        sm: '0 1px 2px 0 rgba(0, 0, 0, 0.05)',
        DEFAULT: '0 1px 3px 0 rgba(0, 0, 0, 0.1), 0 1px 2px 0 rgba(0, 0, 0, 0.06)',
        md: '0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06)',
        lg: '0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05)',
        xl: '0 20px 25px -5px rgba(0, 0, 0, 0.1), 0 10px 10px -5px rgba(0, 0, 0, 0.04)',
      },
    },
  },
  plugins: [],
  darkMode: 'media', // Use 'media' for automatic dark mode based on system preference
}
