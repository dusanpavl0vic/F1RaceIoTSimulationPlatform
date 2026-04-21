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
  font-size: 9px !important;
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
  font-size: 13px;
  font-weight: 700;
  color: ${({ theme }) => theme.colors.textPrimary};
`;

export const StyledTelemetryFilters = styled(Box)`
  display: flex;
  flex-direction: column;
  gap: 14px;
`;

export const StyledTelemetryErrorText = styled(Typography)`
  font-size: 12px;
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
  font-size: 9px !important;
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
  font-size: 9px;
  color: ${({ theme }) => theme.colors.textMuted};
`;

export const StyledTelemetryControlValue = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 13px;
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
    font-size: 11px;
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
  font-size: 11px !important;
`;
