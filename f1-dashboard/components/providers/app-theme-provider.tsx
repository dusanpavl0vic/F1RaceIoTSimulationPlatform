"use client";

import { CssBaseline, GlobalStyles, ThemeProvider } from "@mui/material";
import { useMemo } from "react";
import { ThemeProvider as StyledThemeProvider } from "styled-components";
import { selectColorMode } from "@/features/app/store/selectors";
import { useAppSelector } from "@/store/hooks";
import { createMuiTheme } from "@/theme/mui-theme";
import { createAppUiTheme } from "@/theme/create-ui-theme";

export function AppThemeProvider({ children }: { children: React.ReactNode }) {
  const colorMode = useAppSelector(selectColorMode);
  const styledTheme = useMemo(() => createAppUiTheme(colorMode), [colorMode]);
  const muiTheme = useMemo(() => createMuiTheme(colorMode), [colorMode]);

  return (
    <ThemeProvider theme={muiTheme}>
      <StyledThemeProvider theme={styledTheme}>
        <CssBaseline />
        <GlobalStyles
          styles={{
            ":root": {
              "--bg": styledTheme.colors.background,
              "--ink": styledTheme.colors.textPrimary,
              "--muted": styledTheme.colors.textMuted,
              "--panel": styledTheme.colors.panel,
              "--line": styledTheme.colors.border,
              "--accent": styledTheme.colors.formulaRed,
              "--accent-dark": styledTheme.colors.formulaRedDark,
              "--carbon": styledTheme.colors.carbonBlack,
              "--shadow": styledTheme.colors.shadow,
            },
          }}
        />
        {children}
      </StyledThemeProvider>
    </ThemeProvider>
  );
}
