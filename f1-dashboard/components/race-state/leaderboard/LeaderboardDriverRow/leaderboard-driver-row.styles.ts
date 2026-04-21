"use client";

import { appColors } from "@/theme/colors";
import {
  Box,
  TableCell,
  type TableCellProps,
  TableRow,
  type TableRowProps,
  Typography,
} from "@mui/material";
import styled, { css, keyframes } from "styled-components";

const flashGained = keyframes`
  0%   { box-shadow: inset 0 0 0 200px rgba(0, 200, 83, 0.30); }
  60%  { box-shadow: inset 0 0 0 200px rgba(0, 200, 83, 0.15); }
  100% { box-shadow: inset 0 0 0 200px rgba(0, 200, 83, 0); }
`;

const flashLost = keyframes`
  0%   { box-shadow: inset 0 0 0 200px rgba(220, 0, 0, 0.28); }
  60%  { box-shadow: inset 0 0 0 200px rgba(220, 0, 0, 0.12); }
  100% { box-shadow: inset 0 0 0 200px rgba(220, 0, 0, 0); }
`;

type DriverTableRowProps = TableRowProps & {
  $dimmed: boolean;
  $flash: "gained" | "lost" | null;
};

export const StyledDriverTableRow = styled(TableRow)<DriverTableRowProps>`
  opacity: ${({ $dimmed }) => ($dimmed ? 0.42 : 1)};

  & td {
    border-bottom: 1px solid ${({ theme }) => theme.colors.border} !important;
    background-color: ${({ theme }) =>
      theme.isDark ? "transparent" : theme.colors.backgroundSoft};
    color: ${({ theme }) => theme.colors.textPrimary};
    font-family: var(--font-silkscreen), "Silkscreen", monospace;
    font-size: 12px;
  }

  ${({ $flash }) =>
    $flash === "gained" &&
    css`
      & td {
        animation: ${flashGained} 1s ease-out forwards;
      }
    `}

  ${({ $flash }) =>
    $flash === "lost" &&
    css`
      & td {
        animation: ${flashLost} 1s ease-out forwards;
      }
    `}

  &:hover td {
    background-color: ${({ theme }) =>
      theme.isDark
        ? "rgba(255,255,255,0.04)"
        : "rgba(5,14,60,0.07)"} !important;
  }

  &:last-child td {
    border-bottom: 0 !important;
  }
`;

type PosCellProps = TableCellProps & { $isLeader: boolean };

export const StyledPosCell = styled(TableCell)<PosCellProps>`
  padding-left: 16px !important;
  width: 40px !important;
  font-weight: 700 !important;
  font-size: 16px !important;
  color: ${({ $isLeader }) =>
    $isLeader ? `${appColors.formulaRed} !important` : "inherit"};
`;

type DriverCellProps = TableCellProps & { $color: string };

export const StyledDriverCell = styled(TableCell)<DriverCellProps>`
  min-width: 130px;
  padding-left: 12px !important;
`;

export const StyledDriverTla = styled(Typography)<{ $color: string }>`
  display: block;
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-weight: 700;
  font-size: 12px;
  color: ${({ $color }) => $color};
`;

export const StyledDriverNum = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 10px;
  color: ${({ theme }) => theme.colors.textMuted};
  margin-left: 4px;
`;

export const StyledMutedCell = styled(TableCell)`
  color: ${({ theme }) => theme.colors.textMuted} !important;
  font-size: 11px !important;
`;

export const StyledDataCell = styled(TableCell)`
  font-variant-numeric: tabular-nums;
`;

export const StyledLeaderLabel = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 10px;
  color: ${appColors.formulaRed};
  font-weight: 700;
`;

export const StyledLastCell = styled(TableCell)`
  padding-right: 16px !important;
`;

export const StyledStatusStack = styled(Box)`
  display: flex;
  align-items: center;
  gap: 6px;
  flex-wrap: wrap;
`;

export const StyledStatusBadge = styled(Box)<{
  $tone: "neutral" | "success" | "warning" | "error";
  $filled: boolean;
}>`
  min-width: 24px;
  height: 22px;
  padding: 0 8px;
  border-radius: 6px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border: 2px solid ${({ $tone }) => resolveStatusAccent($tone, 0.45)};
  background-color: ${({ $tone, $filled, theme }) =>
    theme.isDark
      ? "#FFFFFF"
      : $filled
        ? resolveStatusAccent($tone, 0.92)
        : resolveStatusAccent($tone, 0.10)};
  color: ${({ $tone, $filled, theme }) =>
    theme.isDark
      ? resolveStatusTextOnLight($tone)
      : $filled
        ? resolveStatusForeground($tone, theme)
        : resolveStatusAccent($tone, 1)};
`;

export const StyledStatusBadgeLabel = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 9px;
  font-weight: 700;
  line-height: 1;
`;

export const StyledPosArrow = styled(Typography)<{
  $direction: "gained" | "lost";
}>`
  display: inline-block;
  margin-left: 4px;
  font-size: 9px;
  line-height: 1;
  vertical-align: middle;
  color: ${({ $direction }) =>
    $direction === "gained" ? appColors.sectorGreen : appColors.formulaRed};
`;

const resolveStatusAccent = (
  tone: "neutral" | "success" | "warning" | "error",
  alpha: number,
) => {
  switch (tone) {
    case "success":
      return `rgba(0, 200, 83, ${alpha})`;
    case "warning":
      return `rgba(255, 214, 0, ${alpha})`;
    case "error":
      return `rgba(220, 0, 0, ${alpha})`;
    default:
      return `rgba(5, 14, 60, ${alpha})`;
  }
};

const resolveStatusForeground = (
  tone: "neutral" | "success" | "warning" | "error",
  theme: { colors: { textPrimary: string; panel: string } },
) => {
  if (tone === "neutral") {
    return theme.colors.panel;
  }

  return theme.colors.textPrimary;
};

const resolveStatusTextOnLight = (
  tone: "neutral" | "success" | "warning" | "error",
) => {
  switch (tone) {
    case "success":
      return "#007A36";
    case "warning":
      return "#8F6B00";
    case "error":
      return "#B30000";
    default:
      return "#213357";
  }
};
