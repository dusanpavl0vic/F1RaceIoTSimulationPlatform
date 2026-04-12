"use client";

import styled, { keyframes, css } from "styled-components";
import { TableCell, type TableCellProps, TableRow, type TableRowProps } from "@mui/material";
import { t } from "@/theme/styled-helpers";
import { appColors } from "@/theme/colors";

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

type DriverTableRowProps = TableRowProps & { $dimmed: boolean; $flash: "gained" | "lost" | null };
export const StyledDriverTableRow = styled(TableRow)<DriverTableRowProps>`
  opacity: ${({ $dimmed }) => ($dimmed ? 0.42 : 1)};

  & td {
    border-bottom: 1px solid ${({ theme }) => t(theme).colors.border} !important;
    background-color: ${({ theme }) => t(theme).isDark ? "transparent" : t(theme).colors.backgroundSoft};
    color: ${({ theme }) => t(theme).colors.textPrimary};
    font-family: var(--font-silkscreen), "Silkscreen", monospace;
    font-size: 0.75rem;
  }

  ${({ $flash }) => $flash === "gained" && css`
    & td {
      animation: ${flashGained} 1.6s ease-out forwards;
    }
  `}

  ${({ $flash }) => $flash === "lost" && css`
    & td {
      animation: ${flashLost} 1.6s ease-out forwards;
    }
  `}

  &:hover td {
    background-color: ${({ theme }) =>
      t(theme).isDark ? "rgba(255,255,255,0.04)" : "rgba(5,14,60,0.07)"} !important;
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
  font-size: 1rem !important;
  color: ${({ $isLeader }) =>
    $isLeader ? `${appColors.formulaRed} !important` : "inherit"};
`;

type DriverCellProps = TableCellProps & { $color: string };
export const StyledDriverCell = styled(TableCell)<DriverCellProps>`
  min-width: 130px;
  border-left: 3px solid ${({ $color }) => $color} !important;
  padding-left: 12px !important;
`;

export const StyledDriverTla = styled.span<{ $color: string }>`
  display: block;
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-weight: 700;
  font-size: 0.78rem;
  letter-spacing: 0.04em;
  color: ${({ $color }) => $color};
`;

export const StyledDriverNum = styled.span`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.60rem;
  color: ${({ theme }) => t(theme).colors.textMuted};
  margin-left: 4px;
`;

export const StyledMutedCell = styled(TableCell)`
  color: ${({ theme }) => t(theme).colors.textMuted} !important;
  font-size: 0.68rem !important;
`;

export const StyledDataCell = styled(TableCell)`
  font-variant-numeric: tabular-nums;
`;

export const StyledLeaderLabel = styled.span`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.60rem;
  color: ${appColors.formulaRed};
  font-weight: 700;
  letter-spacing: 0.04em;
`;

export const StyledLastCell = styled(TableCell)`
  padding-right: 16px !important;
`;

export const StyledPosArrow = styled.span<{ $direction: "gained" | "lost" }>`
  display: inline-block;
  margin-left: 4px;
  font-size: 0.55rem;
  line-height: 1;
  vertical-align: middle;
  color: ${({ $direction }) =>
    $direction === "gained" ? appColors.sectorGreen : appColors.formulaRed};
`;
