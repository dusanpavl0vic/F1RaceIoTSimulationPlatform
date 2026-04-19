"use client";

import { Box, Button, Typography } from "@mui/material";
import styled from "styled-components";

export const StyledHeroPaper = styled(Box)`
  padding: 16px 20px;
  border-radius: 16px;
  background-color: ${({ theme }) => theme.colors.panel};
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 16px;
`;

export const StyledHeroTextBlock = styled(Box)`
  display: flex;
  flex-direction: column;
  gap: 4px;
`;

export const StyledHeroTitle = styled(Typography)`
  margin: 0;
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-weight: 700;
  font-size: 0.95rem;
  letter-spacing: 0.10em;
  color: ${({ theme }) => theme.colors.textPrimary};
  line-height: 1.3;

  ${({ theme }) => theme.breakpoints.down("sm")} {
    font-size: 0.82rem;
  }
`;

export const StyledHeroSubtitle = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.58rem;
  letter-spacing: 0.08em;
  color: ${({ theme }) => theme.colors.textMuted};
`;

export const StyledHeroActions = styled(Box)`
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
`;

export const StyledHeroActionButton = styled(Button)<{ $active?: boolean }>`
  min-width: 132px !important;
  border-radius: 999px !important;
  padding: 8px 14px !important;
  font-family: var(--font-silkscreen), "Silkscreen", monospace !important;
  font-size: 0.58rem !important;
  letter-spacing: 0.08em !important;
  border-color: ${({ theme }) => theme.colors.border} !important;
  color: ${({ $active, theme }) =>
    $active ? theme.colors.panel : theme.colors.textPrimary} !important;
  background-color: ${({ $active, theme }) =>
    $active ? theme.colors.formulaRed : "transparent"} !important;

  &:hover {
    border-color: ${({ theme }) => theme.colors.formulaRed} !important;
    background-color: ${({ $active, theme }) =>
      $active ? theme.colors.formulaRed : theme.colors.backgroundSoft} !important;
  }
`;

