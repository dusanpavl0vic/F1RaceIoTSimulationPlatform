import type { ColorMode } from "@/features/store/app/appUiSlice";
import { appColors } from "@/theme/colors";
import { breakpointValues } from "@/theme/ui-theme";
import { createTheme } from "@mui/material/styles";

export function createMuiTheme(colorMode: ColorMode) {
  const isDark = colorMode === "dark";

  return createTheme({
    breakpoints: { values: breakpointValues },
    palette: {
      mode: colorMode,
      primary: {
        main: appColors.formulaRed,
        light: appColors.formulaRedLight,
        dark: appColors.formulaRedDark,
        contrastText: "#FFFFFF",
      },
      secondary: {
        main: isDark ? appColors.navyRoyal : appColors.navyDeep,
        contrastText: "#FFFFFF",
      },
      background: {
        default: isDark ? appColors.backgroundDark : appColors.background,
        paper: isDark ? appColors.panelDark : appColors.panel,
      },
      text: {
        primary: isDark ? appColors.textPrimaryDark : appColors.textPrimary,
        secondary: isDark ? appColors.textMutedDark : appColors.textMuted,
      },
      divider: isDark ? appColors.borderDark : appColors.border,
      success: { main: appColors.sectorGreen, contrastText: "#000" },
      warning: { main: appColors.timingYellow, contrastText: "#000" },
      error: { main: appColors.formulaRed, light: appColors.formulaRedLight },
    },
    shape: { borderRadius: 4 },
    typography: {
      fontFamily: 'var(--font-silkscreen), "Silkscreen", monospace',
      fontSize: 12,
      h1: { fontWeight: 700, letterSpacing: "0.02em" },
      h2: { fontWeight: 700, letterSpacing: "0.02em" },
      h3: { fontWeight: 700, letterSpacing: "0.02em" },
      h4: { fontWeight: 700, letterSpacing: "0.02em" },
      h5: { fontWeight: 700, letterSpacing: "0.02em" },
      h6: { fontWeight: 700, letterSpacing: "0.02em" },
      body1: { fontSize: "0.82rem", lineHeight: 1.5 },
      body2: { fontSize: "0.75rem", lineHeight: 1.5 },
      caption: { fontSize: "0.68rem" },
      overline: { fontSize: "0.65rem", letterSpacing: "0.12em" },
      button: { textTransform: "uppercase", fontWeight: 700, letterSpacing: "0.08em" },
    },
    components: {
      MuiCssBaseline: {
        styleOverrides: { body: { margin: 0 } },
      },
      MuiPaper: {
        defaultProps: { elevation: 0 },
        styleOverrides: {
          root: {
            border: `1px solid ${isDark ? appColors.borderDark : appColors.border}`,
            backgroundImage: "none",
          },
        },
      },
      MuiTableCell: {
        styleOverrides: {
          root: {
            fontFamily: 'var(--font-silkscreen), "Silkscreen", monospace',
            fontSize: "0.75rem",
            borderColor: isDark ? appColors.borderDark : appColors.border,
            padding: "6px 10px",
          },
          head: {
            fontWeight: 700,
            letterSpacing: "0.10em",
            textTransform: "uppercase" as const,
            fontSize: "0.60rem",
            color: isDark ? appColors.textMutedDark : appColors.textMuted,
            backgroundColor: isDark ? appColors.backgroundDark : appColors.background,
            borderBottom: `1px solid ${isDark ? appColors.borderDark : appColors.border}`,
          },
        },
      },
      MuiTableRow: {
        styleOverrides: {
          root: { "&:last-child td": { borderBottom: 0 } },
        },
      },
      MuiChip: {
        styleOverrides: {
          root: {
            fontFamily: 'var(--font-silkscreen), "Silkscreen", monospace',
            fontWeight: 700,
            fontSize: "0.60rem",
            letterSpacing: "0.06em",
            borderRadius: 2,
            height: 20,
          },
          label: { padding: "0 7px" },
        },
      },
      MuiButton: {
        styleOverrides: {
          root: { borderRadius: 4, boxShadow: "none", "&:hover": { boxShadow: "none" } },
        },
      },
      MuiSwitch: {
        styleOverrides: {
          switchBase: {
            "&.Mui-checked": {
              color: appColors.formulaRed,
              "+ .MuiSwitch-track": {
                backgroundColor: appColors.formulaRed,
                opacity: 0.6,
              },
            },
          },
        },
      },
      MuiDivider: {
        styleOverrides: {
          root: { borderColor: isDark ? appColors.borderDark : appColors.border },
        },
      },
    },
  });
}
