export const theme = {
  colors: {
    bg: '#1A1A2E',
    surface: '#16213E',
    primary: '#594AE2',
    secondary: '#FF4081',
    accent: '#00C9A7',
    text: '#FFFFFF',
    textMuted: '#B0B0C3',
    // Matches the baked-in background of flowhub-logo.png so the logo blends
    // seamlessly on title scenes (no visible rectangle around it).
    logoBg: '#091839',
  },
  fonts: {
    heading: '"Segoe UI", "Inter", system-ui, sans-serif',
    body: '"Segoe UI", "Inter", system-ui, sans-serif',
  },
} as const;
