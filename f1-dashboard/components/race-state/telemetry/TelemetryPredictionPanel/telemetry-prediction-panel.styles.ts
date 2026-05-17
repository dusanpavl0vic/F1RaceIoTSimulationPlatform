"use client";

import { Box, Button, Typography } from "@mui/material";
import styled from "styled-components";

export const StyledPredictionShell = styled(Box)`
  display: flex;
  flex-direction: column;
  gap: 16px;
`;

export const StyledPredictionHero = styled(Box)`
  display: grid;
  grid-template-columns: minmax(0, 1.3fr) minmax(280px, 360px);
  gap: 14px;

  @media (max-width: 1120px) {
    grid-template-columns: 1fr;
  }
`;

export const StyledPredictionStage = styled(Box)`
  border-radius: 18px;
  border: 1px solid ${({ theme }) => theme.colors.border};
  background:
    linear-gradient(135deg, ${({ theme }) => theme.colors.panel} 0%, ${({ theme }) => theme.colors.backgroundSoft} 100%);
  padding: 18px;
  display: flex;
  flex-direction: column;
  gap: 14px;
`;

export const StyledPredictionStatusRow = styled(Box)`
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
  flex-wrap: wrap;
`;

export const StyledPredictionEyebrow = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 10px;
  color: ${({ theme }) => theme.colors.textMuted};
`;

export const StyledPredictionHeadline = styled(Typography)`
  margin-top: 8px;
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 15px;
  color: ${({ theme }) => theme.colors.textPrimary};
`;

export const StyledPredictionBadge = styled(Box)<{ $tone: "neutral" | "active" | "success" | "warning" | "danger" }>`
  padding: 8px 10px;
  border-radius: 999px;
  border: 1px solid
    ${({ $tone, theme }) =>
      $tone === "success"
        ? theme.colors.formulaRed
        : $tone === "active"
          ? theme.colors.textPrimary
          : theme.colors.border};
  background-color:
    ${({ $tone, theme }) =>
      $tone === "danger"
        ? "rgba(179, 0, 0, 0.1)"
        : $tone === "success"
          ? "rgba(225, 6, 0, 0.12)"
          : theme.colors.panel};
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 9px;
  color:
    ${({ $tone, theme }) =>
      $tone === "danger"
        ? "#b30000"
        : $tone === "success"
          ? theme.colors.formulaRed
          : theme.colors.textPrimary};
`;

export const StyledPredictionMetricGrid = styled(Box)`
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 12px;

  @media (max-width: 960px) {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  @media (max-width: 640px) {
    grid-template-columns: 1fr;
  }
`;

export const StyledPredictionMetricCard = styled(Box)`
  min-width: 0;
  padding: 12px 14px;
  border-radius: 14px;
  border: 1px solid ${({ theme }) => theme.colors.border};
  background-color: ${({ theme }) => theme.colors.panel};
  display: flex;
  flex-direction: column;
  gap: 6px;
`;

export const StyledPredictionMetricLabel = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 10px;
  color: ${({ theme }) => theme.colors.textMuted};
`;

export const StyledPredictionMetricValue = styled(Typography)<{ $accent?: boolean }>`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 13px;
  color: ${({ $accent, theme }) =>
    $accent ? theme.colors.formulaRed : theme.colors.textPrimary};
`;

export const StyledPredictionActionCard = styled(Box)`
  border-radius: 18px;
  border: 1px solid ${({ theme }) => theme.colors.border};
  background-color: ${({ theme }) => theme.colors.panel};
  padding: 18px;
  display: flex;
  flex-direction: column;
  gap: 14px;
`;

export const StyledPredictionActionTitle = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 12px;
  color: ${({ theme }) => theme.colors.textPrimary};
`;

export const StyledPredictionActionText = styled(Typography)`
  font-size: 12px;
  line-height: 1.5;
  color: ${({ theme }) => theme.colors.textMuted};
`;

export const StyledPredictionButton = styled(Button)`
  align-self: flex-start;
  border-radius: 12px !important;
  padding: 10px 14px !important;
  font-family: var(--font-silkscreen), "Silkscreen", monospace !important;
  font-size: 10px !important;
  color: ${({ theme }) => theme.colors.textPrimary} !important;
  border-color: ${({ theme }) => theme.colors.border} !important;
  background-color: ${({ theme }) => theme.colors.backgroundSoft} !important;
`;

export const StyledPredictionError = styled(Typography)`
  font-size: 12px;
  color: #b30000;
`;

export const StyledPredictionPanelGrid = styled(Box)`
  display: grid;
  grid-template-columns: minmax(0, 1.05fr) minmax(0, 1fr);
  gap: 14px;

  @media (max-width: 1120px) {
    grid-template-columns: 1fr;
  }
`;

export const StyledPredictionPanel = styled(Box)`
  border-radius: 18px;
  border: 1px solid ${({ theme }) => theme.colors.border};
  background-color: ${({ theme }) => theme.colors.panel};
  overflow: hidden;
`;

export const StyledPredictionPanelHeader = styled(Box)`
  padding: 14px 16px;
  border-bottom: 1px solid ${({ theme }) => theme.colors.border};
  background-color: ${({ theme }) => theme.colors.backgroundSoft};
`;

export const StyledPredictionPanelTitle = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 12px;
  color: ${({ theme }) => theme.colors.textPrimary};
`;

export const StyledPredictionPanelSubtitle = styled(Typography)`
  margin-top: 6px;
  font-size: 12px;
  line-height: 1.6;
  color: ${({ theme }) => theme.colors.textMuted};
`;

export const StyledPredictionBody = styled(Box)`
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 14px;
`;

export const StyledPredictionTimelineCard = styled(Box)`
  border-radius: 14px;
  border: 1px solid ${({ theme }) => theme.colors.border};
  background-color: ${({ theme }) => theme.colors.backgroundSoft};
  padding: 14px;
  display: flex;
  flex-direction: column;
  gap: 8px;
`;

export const StyledPredictionTimelineTitle = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 11px;
  color: ${({ theme }) => theme.colors.textPrimary};
`;

export const StyledPredictionTimelineText = styled(Typography)`
  font-size: 12px;
  line-height: 1.6;
  color: ${({ theme }) => theme.colors.textMuted};
`;

export const StyledPredictionCycleGrid = styled(Box)`
  display: grid;
  grid-template-columns: repeat(6, minmax(0, 1fr));
  gap: 10px;

  @media (max-width: 1100px) {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }

  @media (max-width: 680px) {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
`;

export const StyledPredictionHistoryList = styled(Box)`
  display: flex;
  flex-direction: column;
`;

export const StyledPredictionHistoryRow = styled(Box)`
  display: grid;
  grid-template-columns: minmax(0, 1.1fr) 76px minmax(0, 1fr) minmax(0, 1fr);
  gap: 12px;
  align-items: center;
  padding: 12px 16px;
  border-bottom: 1px solid ${({ theme }) => theme.colors.border};

  &:last-child {
    border-bottom: 0;
  }

  @media (max-width: 760px) {
    grid-template-columns: 1fr 1fr;
  }
`;

export const StyledPredictionHistoryHeader = styled(StyledPredictionHistoryRow)`
  background-color: ${({ theme }) => theme.colors.backgroundSoft};
`;

export const StyledPredictionHistoryCell = styled(Typography)<{ $muted?: boolean; $accent?: boolean }>`
  min-width: 0;
  font-size: 11px;
  color: ${({ $accent, $muted, theme }) =>
    $accent
      ? theme.colors.formulaRed
      : $muted
        ? theme.colors.textMuted
        : theme.colors.textPrimary};
  font-family: ${({ $accent }) =>
    $accent ? 'var(--font-silkscreen), "Silkscreen", monospace' : "inherit"};
`;

export const StyledPredictionPlaceholder = styled(Box)`
  padding: 16px;
  border-radius: 14px;
  border: 1px dashed ${({ theme }) => theme.colors.border};
  background-color: ${({ theme }) => theme.colors.backgroundSoft};
`;
