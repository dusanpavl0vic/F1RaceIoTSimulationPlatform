"use client";

import { Box, CircularProgress, Typography } from "@mui/material";
import styled from "styled-components";

export const StyledTyreStrategyCard = styled(Box)`
  border-radius: 16px;
  border: 1px solid ${({ theme }) => theme.colors.border};
  background-color: ${({ theme }) => theme.colors.panel};
  overflow: hidden;
`;

export const StyledTyreStrategyHeader = styled(Box)`
  padding: 16px 18px 14px;
  border-bottom: 1px solid ${({ theme }) => theme.colors.border};
  background-color: ${({ theme }) => theme.colors.backgroundSoft};
`;

export const StyledTyreStrategyTitle = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 13px;
  font-weight: 700;
  color: ${({ theme }) => theme.colors.textPrimary};
`;

export const StyledTyreStrategySubtitle = styled(Typography)`
  margin-top: 6px !important;
  font-size: 12px;
  color: ${({ theme }) => theme.colors.textMuted};
`;

export const StyledTyreStrategyBody = styled(Box)`
  padding: 16px 18px 18px;
  overflow-x: auto;
`;

export const StyledTyreStrategyPlot = styled(Box)`
  min-width: 860px;
  display: grid;
  grid-template-columns: 96px minmax(640px, 1fr);
  gap: 10px 14px;
`;

export const StyledTyreStrategyAxisSpacer = styled(Box)``;

export const StyledTyreStrategyAxis = styled(Box)`
  display: grid;
  grid-template-columns: repeat(var(--lap-count), minmax(6px, 1fr));
  align-items: end;
  min-height: 24px;
  border-bottom: 1px solid ${({ theme }) => theme.colors.border};
`;

export const StyledTyreStrategyAxisTick = styled(Box)<{ $highlight: boolean }>`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 8px;
  line-height: 1;
  color: ${({ $highlight, theme }) =>
    $highlight ? theme.colors.textPrimary : theme.colors.textMuted};
  text-align: center;
  padding-bottom: 6px;
  opacity: ${({ $highlight }) => ($highlight ? 1 : 0.38)};
`;

export const StyledTyreStrategyDriver = styled(Box)`
  min-height: 32px;
  display: flex;
  flex-direction: column;
  justify-content: center;
  border-bottom: 1px solid ${({ theme }) => theme.colors.border};
`;

export const StyledTyreStrategyDriverName = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 10px;
  font-weight: 700;
  color: ${({ theme }) => theme.colors.textPrimary};
`;

export const StyledTyreStrategyTeam = styled(Typography)`
  font-size: 9px;
  color: ${({ theme }) => theme.colors.textMuted};
`;

export const StyledTyreStrategyRow = styled(Box)`
  min-height: 32px;
  display: grid;
  grid-template-columns: repeat(var(--lap-count), minmax(6px, 1fr));
  align-items: center;
  border-bottom: 1px solid ${({ theme }) => theme.colors.border};
  background-image: linear-gradient(
    to right,
    ${({ theme }) =>
        theme.isDark ? "rgba(255,255,255,0.06)" : "rgba(5,14,60,0.08)"}
      1px,
    transparent 1px
  );
  background-size: calc(100% / var(--lap-count)) 100%;
`;

export const StyledTyreStintBlock = styled(Box)<{
  $compound: string;
  $start: number;
  $span: number;
}>`
  grid-column: ${({ $start, $span }) => `${$start} / span ${$span}`};
  height: 20px;
  border-radius: 999px;
  background-color: ${({ $compound }) => resolveCompoundColor($compound)};
  border: 1px solid rgba(5, 14, 60, 0.22);
  box-shadow: 0 6px 14px rgba(5, 14, 60, 0.12);
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
`;

export const StyledTyreStintLabel = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 8px;
  font-weight: 700;
  line-height: 1;
  color: #050e3c;
  white-space: nowrap;
`;

export const StyledTyreStrategyLegend = styled(Box)`
  margin-top: 16px;
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
`;

export const StyledTyreLegendItem = styled(Box)<{ $compound: string }>`
  display: inline-flex;
  align-items: center;
  gap: 6px;
  border-radius: 999px;
  border: 1px solid ${({ theme }) => theme.colors.border};
  background-color: ${({ theme }) => theme.colors.backgroundSoft};
  padding: 6px 9px;

  &::before {
    content: "";
    width: 10px;
    height: 10px;
    border-radius: 999px;
    background-color: ${({ $compound }) => resolveCompoundColor($compound)};
  }
`;

export const StyledTyreLegendText = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 9px;
  color: ${({ theme }) => theme.colors.textPrimary};
`;

export const StyledTyreStrategyStatus = styled(Box)`
  min-height: 220px;
  border-radius: 14px;
  border: 1px dashed ${({ theme }) => theme.colors.border};
  background-color: ${({ theme }) => theme.colors.backgroundSoft};
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 10px;
  padding: 18px;
  text-align: center;
`;

export const StyledTyreStrategySpinner = styled(CircularProgress)`
  color: ${({ theme }) => theme.colors.formulaRed} !important;
`;

export const StyledTyreStrategyStatusText = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 10px;
  color: ${({ theme }) => theme.colors.textMuted};
`;

const resolveCompoundColor = (compound: string) => {
  switch (compound.toUpperCase()) {
    case "SOFT":
      return "#DC0000";
    case "MEDIUM":
      return "#FFD600";
    case "HARD":
      return "#F5F5F5";
    case "INTERMEDIATE":
      return "#00A650";
    case "WET":
      return "#0067C6";
    default:
      return "#94A3B8";
  }
};
