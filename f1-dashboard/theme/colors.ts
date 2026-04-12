// ─── Brand palette ───────────────────────────────────────────────────────────
// #050E3C  deepest navy  — dark bg / light ink
// #002455  royal navy    — dark panel / light muted ink
// #DC0000  F1 red        — primary accent (both modes)
// #FF3838  bright red    — hover / lighter accent (both modes)

export const appColors = {
  // Core brand
  navyDeep: "#050E3C",
  navyRoyal: "#002455",
  formulaRed: "#DC0000",
  formulaRedLight: "#FF3838",

  // ── Dark mode ──────────────────────────────────────────────────────────────
  // Navies as backgrounds, light text on top
  backgroundDark: "#050E3C",
  backgroundSoftDark: "#001F4D",
  panelDark: "#002455",
  textPrimaryDark: "#E8EEFF",
  textMutedDark: "#6A85B8",
  borderDark: "rgba(255, 255, 255, 0.09)",
  shadowDark: "0 0 0 1px rgba(255,255,255,0.07), 0 8px 32px rgba(5,14,60,0.9)",

  // ── Light mode ─────────────────────────────────────────────────────────────
  // Inverted: light lavender bg, navy as ink — high contrast, same hue family
  background: "#EEF1FF",
  backgroundSoft: "#F6F8FF",
  panel: "#FFFFFF",
  textPrimary: "#050E3C",
  textMuted: "#3D5285",
  border: "rgba(5, 14, 60, 0.11)",
  shadow: "0 0 0 1px rgba(5,14,60,0.07), 0 4px 20px rgba(5,14,60,0.08)",

  // ── Semantic ───────────────────────────────────────────────────────────────
  sectorGreen: "#00C853",
  timingYellow: "#FFD600",
  white: "#FFFFFF",
  carbonBlack: "#050E3C",

  // Legacy aliases (keep for styled-components header)
  darkBlue: "#050E3C",
  darkBlueMuted: "#002455",
  formulaRedDark: "#A80000",
  accentShadow: "0 8px 24px rgba(220, 0, 0, 0.28)",
} as const;

export type AppColorToken = keyof typeof appColors;
