import { createTheme } from "@mui/material/styles";
import type { ColorMode } from "@/features/app/store/app-ui-slice";
import { appColors } from "@/theme/colors";
import { breakpointValues } from "@/theme/ui-theme";

export function createMuiTheme(colorMode: ColorMode) {
  const isDark = colorMode === "dark";

  return createTheme({
    breakpoints: {
      values: breakpointValues,
    },
    palette: {
      mode: colorMode,
      primary: {
        main: appColors.formulaRed,
        dark: appColors.formulaRedDark,
        contrastText: appColors.white,
      },
      secondary: {
        main: appColors.darkBlue,
        contrastText: appColors.white,
      },
      background: {
        default: isDark ? appColors.backgroundDark : appColors.background,
        paper: isDark ? appColors.backgroundSoftDark : appColors.backgroundSoft,
      },
      text: {
        primary: isDark ? appColors.textPrimaryDark : appColors.textPrimary,
        secondary: isDark ? appColors.textMutedDark : appColors.textMuted,
      },
      divider: isDark ? appColors.borderDark : appColors.border,
    },
    shape: {
      borderRadius: 18,
    },
    typography: {
      fontFamily: '"Nunito", sans-serif',
      h1: {
        fontWeight: 800,
        letterSpacing: "-0.04em",
      },
      h2: {
        fontWeight: 800,
        letterSpacing: "-0.03em",
      },
      button: {
        textTransform: "none",
        fontWeight: 700,
      },
    },
    components: {
      MuiCssBaseline: {
        styleOverrides: {
          body: {
            margin: 0,
          },
        },
      },
      MuiPaper: {
        defaultProps: {
          elevation: 0,
        },
        styleOverrides: {
          root: {
            border: `1px solid ${isDark ? appColors.borderDark : appColors.border}`,
          },
        },
      },
    },
  });
}
