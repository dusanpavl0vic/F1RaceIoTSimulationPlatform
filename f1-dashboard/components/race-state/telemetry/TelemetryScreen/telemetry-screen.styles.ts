"use client";

import { Box, FormControl, MenuItem, Select, Tab, Tabs, Typography } from "@mui/material";
import styled from "styled-components";

export const StyledTelemetryShell = styled(Box)`
  width: 100%;
  padding: 20px 0 56px;
  display: flex;
  flex-direction: column;
  gap: 14px;
`;

export const StyledTelemetryTabsShell = styled(Box)`
  border-radius: 16px;
  border: 1px solid ${({ theme }) => theme.colors.border};
  background-color: ${({ theme }) => theme.colors.panel};
  overflow: hidden;
`;

export const StyledTelemetryPageTabs = styled(Tabs)`
  min-height: 0 !important;
  padding: 8px 10px 0 !important;
  background-color: ${({ theme }) => theme.colors.panel};

  .MuiTabs-indicator {
    height: 2px;
    background-color: ${({ theme }) => theme.colors.formulaRed};
  }
`;

export const StyledTelemetryPageTab = styled(Tab)`
  min-height: 0 !important;
  min-width: 0 !important;
  padding: 10px 14px !important;
  font-family: var(--font-silkscreen), "Silkscreen", monospace !important;
  font-size: 0.58rem !important;
  letter-spacing: 0.08em !important;
  color: ${({ theme }) => theme.colors.textMuted} !important;

  &.Mui-selected {
    color: ${({ theme }) => theme.colors.textPrimary} !important;
  }
`;

export const StyledTelemetryTabPanel = styled(Box)`
  padding: 18px;
  display: flex;
  flex-direction: column;
  gap: 16px;
`;

export const StyledTelemetryPanelIntro = styled(Box)`
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 14px;
  flex-wrap: wrap;
`;

export const StyledTelemetryPanelTitle = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.84rem;
  font-weight: 700;
  letter-spacing: 0.10em;
  color: ${({ theme }) => theme.colors.textPrimary};
`;

export const StyledTelemetryPanelCaption = styled(Typography)`
  margin-top: 4px !important;
  max-width: 760px;
  font-size: 0.78rem;
  line-height: 1.6;
  color: ${({ theme }) => theme.colors.textMuted};
`;

export const StyledTelemetryFilters = styled(Box)`
  display: flex;
  flex-direction: column;
  gap: 14px;
`;

export const StyledTelemetryErrorText = styled(Typography)`
  font-size: 0.78rem;
  color: #b30000;
`;

export const StyledTelemetryDriverTabs = styled(Tabs)`
  min-height: 0 !important;
  border: 1px solid ${({ theme }) => theme.colors.border};
  border-radius: 14px;
  padding: 4px 8px !important;
  background-color: ${({ theme }) => theme.colors.backgroundSoft};

  .MuiTabs-indicator {
    height: 100%;
    border-radius: 10px;
    background-color: ${({ theme }) => theme.colors.panel};
    border: 1px solid ${({ theme }) => theme.colors.border};
    z-index: 0;
  }

  .MuiTabs-flexContainer {
    gap: 8px;
  }
`;

export const StyledTelemetryDriverTab = styled(Tab)`
  min-height: 0 !important;
  min-width: 0 !important;
  padding: 10px 14px !important;
  border-radius: 10px !important;
  font-family: var(--font-silkscreen), "Silkscreen", monospace !important;
  font-size: 0.58rem !important;
  letter-spacing: 0.08em !important;
  color: ${({ theme }) => theme.colors.textMuted} !important;
  z-index: 1;

  &.Mui-selected {
    color: ${({ theme }) => theme.colors.textPrimary} !important;
  }
`;

export const StyledTelemetryControlGrid = styled(Box)`
  display: grid;
  grid-template-columns: minmax(0, 1.3fr) repeat(2, minmax(180px, 240px));
  gap: 12px;

  @media (max-width: 1100px) {
    grid-template-columns: 1fr;
  }
`;

export const StyledTelemetryControlCard = styled(Box)`
  padding: 14px 16px;
  border-radius: 14px;
  border: 1px solid ${({ theme }) => theme.colors.border};
  background-color: ${({ theme }) => theme.colors.backgroundSoft};
  display: flex;
  flex-direction: column;
  gap: 8px;
`;

export const StyledTelemetryControlLabel = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.54rem;
  letter-spacing: 0.10em;
  color: ${({ theme }) => theme.colors.textMuted};
`;

export const StyledTelemetryControlValue = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.82rem;
  font-weight: 700;
  color: ${({ theme }) => theme.colors.textPrimary};
`;

export const StyledTelemetrySelect = styled(Select)`
  min-width: 0;
  border-radius: 10px !important;
  background-color: ${({ theme }) => theme.colors.panel};

  .MuiOutlinedInput-notchedOutline {
    border-color: ${({ theme }) => theme.colors.border};
  }

  .MuiSelect-select {
    font-family: var(--font-silkscreen), "Silkscreen", monospace;
    font-size: 0.70rem;
    letter-spacing: 0.06em;
    color: ${({ theme }) => theme.colors.textPrimary};
    padding: 12px 14px;
  }

  .MuiSvgIcon-root {
    color: ${({ theme }) => theme.colors.textMuted};
  }
`;

export const StyledTelemetryFormControl = styled(FormControl)`
  width: 100%;
`;

export const StyledTelemetryMenuItem = styled(MenuItem)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace !important;
  font-size: 0.68rem !important;
  letter-spacing: 0.06em !important;
`;

export const StyledMetaBadge = styled(Box)<{ $tone?: "neutral" | "live" | "warning" | "error" }>`
  min-height: 30px;
  padding: 0 12px;
  border-radius: 8px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border: 2px solid
    ${({ $tone, theme }) =>
      $tone === "live"
        ? "rgba(0, 200, 83, 0.45)"
        : $tone === "warning"
          ? "rgba(255, 214, 0, 0.45)"
          : $tone === "error"
            ? "rgba(220, 0, 0, 0.40)"
            : theme.colors.border};
  background-color: ${({ $tone, theme }) =>
    theme.isDark
      ? "#FFFFFF"
      : $tone === "live"
        ? "rgba(0, 200, 83, 0.10)"
        : $tone === "warning"
          ? "rgba(255, 214, 0, 0.12)"
          : $tone === "error"
            ? "rgba(220, 0, 0, 0.10)"
            : theme.colors.backgroundSoft};
`;

export const StyledMetaBadgeLabel = styled(Typography)<{ $tone?: "neutral" | "live" | "warning" | "error" }>`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.56rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  color: ${({ $tone, theme }) =>
    $tone === "live"
      ? "#007A36"
      : $tone === "warning"
        ? "#8F6B00"
        : $tone === "error"
          ? "#B30000"
          : theme.colors.textPrimary};
`;

export const StyledTelemetryDriverList = styled(Box)`
  display: flex;
  flex-direction: column;
  gap: 16px;
`;

export const StyledDriverStreamCard = styled(Box)`
  border-radius: 16px;
  border: 1px solid ${({ theme }) => theme.colors.border};
  background-color: ${({ theme }) => theme.colors.panel};
  overflow: hidden;
`;

export const StyledDriverStreamHeader = styled(Box)`
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 14px;
  padding: 16px 18px 14px;
  border-bottom: 1px solid ${({ theme }) => theme.colors.border};
  background-color: ${({ theme }) => theme.colors.backgroundSoft};
  flex-wrap: wrap;
`;

export const StyledDriverIdentity = styled(Box)`
  display: flex;
  flex-direction: column;
  gap: 4px;
`;

export const StyledDriverName = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.80rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  color: ${({ theme }) => theme.colors.textPrimary};
`;

export const StyledDriverTeam = styled(Typography)`
  font-size: 0.74rem;
  color: ${({ theme }) => theme.colors.textMuted};
`;

export const StyledDriverHeaderStats = styled(Box)`
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
`;

export const StyledDriverMetricTabs = styled(Tabs)`
  min-height: 0 !important;
  padding: 0 12px !important;
  border-bottom: 1px solid ${({ theme }) => theme.colors.border};

  .MuiTabs-indicator {
    height: 2px;
    background-color: ${({ theme }) => theme.colors.formulaRed};
  }
`;

export const StyledDriverMetricTab = styled(Tab)`
  min-height: 0 !important;
  min-width: 0 !important;
  padding: 10px 12px !important;
  font-family: var(--font-silkscreen), "Silkscreen", monospace !important;
  font-size: 0.54rem !important;
  letter-spacing: 0.08em !important;
  color: ${({ theme }) => theme.colors.textMuted} !important;

  &.Mui-selected {
    color: ${({ theme }) => theme.colors.textPrimary} !important;
  }
`;

export const StyledDriverChartBody = styled(Box)`
  padding: 14px 18px 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
`;

export const StyledDriverChartMeta = styled(Box)`
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 10px;

  @media (max-width: 960px) {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  @media (max-width: 560px) {
    grid-template-columns: 1fr;
  }
`;

export const StyledDriverMetaItem = styled(Box)`
  padding: 12px 14px;
  border-radius: 12px;
  border: 1px solid ${({ theme }) => theme.colors.border};
  background-color: ${({ theme }) => theme.colors.backgroundSoft};
`;

export const StyledDriverMetaLabel = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.54rem;
  letter-spacing: 0.1em;
  color: ${({ theme }) => theme.colors.textMuted};
`;

export const StyledDriverMetaValue = styled(Typography)`
  margin-top: 6px !important;
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.84rem;
  font-weight: 700;
  color: ${({ theme }) => theme.colors.textPrimary};
`;

export const StyledChartShell = styled(Box)`
  width: 100%;
  height: 360px;
  padding: 6px 0 0;
`;

export const StyledChartEmptyState = styled(Box)`
  min-height: 220px;
  border-radius: 14px;
  border: 1px dashed ${({ theme }) => theme.colors.border};
  background-color: ${({ theme }) => theme.colors.backgroundSoft};
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 18px;
`;

export const StyledChartEmptyText = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.62rem;
  letter-spacing: 0.08em;
  text-align: center;
  color: ${({ theme }) => theme.colors.textMuted};
`;

export const StyledChartTooltip = styled(Box)`
  padding: 10px 12px;
  border-radius: 10px;
  border: 1px solid ${({ theme }) => theme.colors.border};
  background-color: ${({ theme }) => theme.colors.panel};
  box-shadow: ${({ theme }) => theme.colors.shadow};
  display: flex;
  flex-direction: column;
  gap: 6px;
`;

export const StyledChartTooltipTitle = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.56rem;
  letter-spacing: 0.08em;
  color: ${({ theme }) => theme.colors.textMuted};
`;

export const StyledChartTooltipValue = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.78rem;
  font-weight: 700;
  color: ${({ theme }) => theme.colors.textPrimary};
`;
