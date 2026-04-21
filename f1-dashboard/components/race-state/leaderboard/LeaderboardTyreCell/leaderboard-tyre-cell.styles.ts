"use client";

import { Box, Typography } from "@mui/material";
import styled from "styled-components";

export const StyledTyreWrapper = styled(Box)`
  display: flex;
  align-items: center;
  gap: 6px;
`;

export const StyledTyreBadge = styled(Box)<{ $compound: string; $filled: boolean }>`
  min-width: 24px;
  height: 22px;
  padding: 0 8px;
  border-radius: 6px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border: 2px solid
    ${({ $compound, theme }) => resolveTyreAccent($compound, theme, 0.45)};
  background-color: ${({ $compound, $filled, theme }) =>
    theme.isDark
      ? "#FFFFFF"
      : $filled
        ? resolveTyreAccent($compound, theme, 0.92)
        : resolveTyreAccent($compound, theme, 0.10)};
  color: ${({ $compound, $filled, theme }) =>
    theme.isDark
      ? resolveTyreTextOnLight($compound)
      : $filled
        ? resolveTyreForeground($compound, theme)
        : resolveTyreAccent($compound, theme, 1)};
`;

export const StyledTyreBadgeLabel = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 9px;
  font-weight: 700;
  line-height: 1;
`;

export const StyledTyreLapCount = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 10px;
  color: ${({ theme }) => theme.colors.textMuted};
`;

const resolveTyreAccent = (
  compound: string,
  theme: { colors: { textPrimary: string } },
  alpha: number,
) => {
  const lowered = compound.toLowerCase();

  if (lowered === "soft" || lowered === "c5" || lowered === "c4") {
    return `rgba(220, 0, 0, ${alpha})`;
  }

  if (lowered === "medium" || lowered === "c3") {
    return `rgba(255, 214, 0, ${alpha})`;
  }

  if (lowered === "hard" || lowered === "c2" || lowered === "c1") {
    return `rgba(5, 14, 60, ${alpha})`;
  }

  if (lowered === "intermediate" || lowered === "inter") {
    return `rgba(0, 200, 83, ${alpha})`;
  }

  if (lowered === "wet") {
    return `rgba(0, 120, 255, ${alpha})`;
  }

  return `rgba(5, 14, 60, ${alpha})`;
};

const resolveTyreForeground = (
  compound: string,
  theme: { colors: { textPrimary: string; panel: string } },
) => {
  const lowered = compound.toLowerCase();

  if (lowered === "hard" || lowered === "c2" || lowered === "c1") {
    return theme.colors.panel;
  }

  return theme.colors.textPrimary;
};

const resolveTyreTextOnLight = (compound: string) => {
  const lowered = compound.toLowerCase();

  if (lowered === "soft" || lowered === "c5" || lowered === "c4") {
    return "#B30000";
  }

  if (lowered === "medium" || lowered === "c3") {
    return "#8F6B00";
  }

  if (lowered === "hard" || lowered === "c2" || lowered === "c1") {
    return "#213357";
  }

  if (lowered === "intermediate" || lowered === "inter") {
    return "#007A36";
  }

  if (lowered === "wet") {
    return "#005CC8";
  }

  return "#213357";
};
