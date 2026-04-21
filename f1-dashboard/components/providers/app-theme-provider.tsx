"use client";

import { selectColorMode } from "@/features/store/app/appSelectors";
import { createAppUiTheme } from "@/theme/create-ui-theme";
import { GlobalStyles } from "@mui/material";
import { useMemo } from "react";
import { useSelector } from "react-redux";
import { ThemeProvider as StyledThemeProvider } from "styled-components";

export function AppThemeProvider({ children }: { children: React.ReactNode }) {
  const colorMode = useSelector(selectColorMode);
  const styledTheme = useMemo(() => createAppUiTheme(colorMode), [colorMode]);

  return (
    <StyledThemeProvider theme={styledTheme}>
      <GlobalStyles
        styles={{
          "*, *::before, *::after": {
            boxSizing: "border-box",
          },
          "html, body": {
            margin: 0,
            padding: 0,
            minHeight: "100%",
            background: styledTheme.colors.background,
            color: styledTheme.colors.textPrimary,
            fontFamily: 'var(--font-silkscreen), "Silkscreen", monospace',
          },
          body: {
            minHeight: "100vh",
          },
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
          ".MuiTypography-root, .MuiButton-root, .MuiChip-root, .MuiTableCell-root, .MuiInputBase-root, .MuiFormLabel-root":
            {
              fontFamily: 'var(--font-silkscreen), "Silkscreen", monospace',
            },
          ".MuiTableCell-root": {
            color: styledTheme.colors.textPrimary,
            borderColor: styledTheme.colors.border,
            fontSize: "12px",
            padding: "6px 10px",
          },
          ".MuiTableCell-head": {
            fontWeight: 700,
            letterSpacing: "0.10em",
            textTransform: "uppercase",
            fontSize: "10px",
            color: styledTheme.colors.textMuted,
            backgroundColor: styledTheme.isDark
              ? styledTheme.colors.background
              : styledTheme.colors.backgroundSoft,
            borderBottom: `1px solid ${styledTheme.colors.border}`,
          },
          ".MuiChip-root": {
            borderRadius: 4,
            fontWeight: 700,
            fontSize: "10px",
            letterSpacing: "0.06em",
            height: 20,
          },
          ".MuiChip-label": {
            padding: "0 7px",
          },
          ".MuiButton-root": {
            borderRadius: 4,
            boxShadow: "none",
          },
          ".MuiButton-root:hover": {
            boxShadow: "none",
          },
          ".MuiPaper-root": {
            border: `1px solid ${styledTheme.colors.border}`,
            backgroundImage: "none",
          },
        }}
      />
      {children}
    </StyledThemeProvider>
  );
}
