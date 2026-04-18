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
  font-size: 0.72rem;
  letter-spacing: 0.12em;
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
  font-size: 0.60rem;
  letter-spacing: 0.08em;
  color: ${({ theme }) => theme.colors.textMuted};
`;

export const StyledStatTimeValue = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.72rem;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
  color: ${({ theme }) => theme.colors.textPrimary};
`;
