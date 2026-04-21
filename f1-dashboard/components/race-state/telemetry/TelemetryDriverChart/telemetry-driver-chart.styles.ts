"use client";

import { Box, Tab, Tabs, Typography } from "@mui/material";
import styled from "styled-components";

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
  font-size: 13px;
  font-weight: 700;
  color: ${({ theme }) => theme.colors.textPrimary};
`;

export const StyledDriverTeam = styled(Typography)`
  font-size: 12px;
  color: ${({ theme }) => theme.colors.textMuted};
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
  font-size: 9px !important;
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
  font-size: 9px;
  color: ${({ theme }) => theme.colors.textMuted};
`;

export const StyledDriverMetaValue = styled(Typography)`
  margin-top: 6px !important;
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 13px;
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
  font-size: 10px;
  text-align: center;
  color: ${({ theme }) => theme.colors.textMuted};
`;
