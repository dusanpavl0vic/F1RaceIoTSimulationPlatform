"use client";

import { appColors } from "@/theme/colors";
import { Box, Typography } from "@mui/material";
import styled from "styled-components";

export const StyledTransportPaper = styled(Box)`
  border-radius: 16px;
  border: 1px solid ${({ theme }) => theme.colors.border};
  background-color: ${({ theme }) => theme.colors.panel};
  overflow: hidden;
`;

export const StyledPanelHeader = styled(Box)`
  padding: 8px 16px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  border-bottom: 1px solid ${({ theme }) => theme.colors.border};
  background-color: ${({ theme }) => theme.colors.panel};
`;

export const StyledPanelTitle = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-weight: 700;
  font-size: 12px;
  color: ${({ theme }) => theme.colors.textPrimary};
`;

export const StyledLiveDot = styled(Typography) <{ $live: boolean }>`
  display: block;
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background-color: ${({ $live }) => ($live ? appColors.sectorGreen : "#555")};
`;

export const StyledStatsList = styled(Box)`
  padding: 2px 16px;
`;

export const StyledStatRow = styled(Box)`
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 0;
  border-bottom: 1px solid ${({ theme }) => theme.colors.border};

  &:last-child {
    border-bottom: 0;
  }
`;

export const StyledStatLabel = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 10px;
  color: ${({ theme }) => theme.colors.textMuted};
`;

export const StyledStatusBadge = styled(Box)<{ $tone: "live" | "warning" | "error" | "idle" }>`
  min-width: 86px;
  height: 22px;
  padding: 0 8px;
  border-radius: 6px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border: 2px solid ${({ $tone, theme }) => resolveStatusColors($tone, theme.isDark).border};
  background-color: ${({ $tone, theme }) =>
    resolveStatusColors($tone, theme.isDark).background};
  color: ${({ $tone, theme }) => resolveStatusColors($tone, theme.isDark).color};
`;

export const StyledStatusBadgeLabel = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 9px;
  font-weight: 700;
  line-height: 1;
`;

export const StyledStatTimeValue = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 12px;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
  color: ${({ theme }) => theme.colors.textPrimary};
`;

const resolveStatusColors = (
  tone: "live" | "warning" | "error" | "idle",
  isDark: boolean
) => {
  switch (tone) {
    case "live":
      return {
        border: "rgba(0, 200, 83, 0.45)",
        background: isDark ? "#FFFFFF" : "rgba(0, 200, 83, 0.14)",
        color: "#00A94B",
      };
    case "warning":
      return {
        border: "rgba(255, 214, 0, 0.45)",
        background: isDark ? "#FFFFFF" : "rgba(255, 214, 0, 0.16)",
        color: "#8F6B00",
      };
    case "error":
      return {
        border: "rgba(220, 0, 0, 0.40)",
        background: isDark ? "#FFFFFF" : "rgba(220, 0, 0, 0.14)",
        color: "#B30000",
      };
    default:
      return {
        border: "rgba(5, 14, 60, 0.18)",
        background: isDark ? "#FFFFFF" : "rgba(5, 14, 60, 0.06)",
        color: "#4A5F91",
      };
  }
};
