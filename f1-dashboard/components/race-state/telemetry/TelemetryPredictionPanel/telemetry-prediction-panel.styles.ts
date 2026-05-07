"use client";

import { Box, Typography } from "@mui/material";
import styled from "styled-components";

export const StyledPredictionShell = styled(Box)`
  display: flex;
  flex-direction: column;
  gap: 16px;
`;

export const StyledPredictionSummaryGrid = styled(Box)`
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px;

  @media (max-width: 1100px) {
    grid-template-columns: 1fr;
  }
`;

export const StyledPredictionCard = styled(Box)`
  padding: 14px 16px;
  border-radius: 14px;
  border: 1px solid ${({ theme }) => theme.colors.border};
  background-color: ${({ theme }) => theme.colors.backgroundSoft};
  display: flex;
  flex-direction: column;
  gap: 8px;
`;

export const StyledPredictionLabel = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 9px;
  color: ${({ theme }) => theme.colors.textMuted};
`;

export const StyledPredictionValue = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 13px;
  font-weight: 700;
  color: ${({ theme }) => theme.colors.textPrimary};
`;

export const StyledPredictionPanel = styled(Box)`
  border-radius: 16px;
  border: 1px solid ${({ theme }) => theme.colors.border};
  background-color: ${({ theme }) => theme.colors.panel};
  overflow: hidden;
`;

export const StyledPredictionPanelHeader = styled(Box)`
  padding: 12px 16px;
  border-bottom: 1px solid ${({ theme }) => theme.colors.border};
  background-color: ${({ theme }) => theme.colors.backgroundSoft};
`;

export const StyledPredictionTitle = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 12px;
  font-weight: 700;
  color: ${({ theme }) => theme.colors.textPrimary};
`;

export const StyledPredictionSubtitle = styled(Typography)`
  margin-top: 6px;
  font-size: 11px;
  color: ${({ theme }) => theme.colors.textMuted};
`;

export const StyledPredictionTable = styled(Box)`
  display: flex;
  flex-direction: column;
`;

export const StyledPredictionRow = styled(Box)<{ $active?: boolean }>`
  display: grid;
  grid-template-columns: 56px minmax(0, 1.4fr) minmax(0, 1fr) minmax(0, 1fr);
  gap: 12px;
  align-items: center;
  padding: 12px 16px;
  border-bottom: 1px solid ${({ theme }) => theme.colors.border};
  background-color: ${({ $active, theme }) =>
    $active ? theme.colors.backgroundSoft : theme.colors.panel};
  cursor: pointer;

  &:last-child {
    border-bottom: 0;
  }

  @media (max-width: 860px) {
    grid-template-columns: 44px minmax(0, 1fr);
  }
`;

export const StyledPredictionHeaderRow = styled(StyledPredictionRow)`
  cursor: default;
  background-color: ${({ theme }) => theme.colors.panel};
`;

export const StyledPredictionCell = styled(Typography)<{ $muted?: boolean; $highlight?: boolean }>`
  min-width: 0;
  font-size: 11px;
  color: ${({ $highlight, $muted, theme }) =>
    $highlight
      ? theme.colors.formulaRed
      : $muted
        ? theme.colors.textMuted
        : theme.colors.textPrimary};
  font-family: ${({ $highlight }) =>
    $highlight ? 'var(--font-silkscreen), "Silkscreen", monospace' : "inherit"};
  font-weight: ${({ $highlight }) => ($highlight ? 700 : 500)};
`;

export const StyledPredictionHistoryGrid = styled(Box)`
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
  gap: 12px;

  @media (max-width: 1100px) {
    grid-template-columns: 1fr;
  }
`;
