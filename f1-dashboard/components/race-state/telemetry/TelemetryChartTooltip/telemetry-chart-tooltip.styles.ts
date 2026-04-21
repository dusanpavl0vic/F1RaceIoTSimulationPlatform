"use client";

import { Box, Typography } from "@mui/material";
import styled from "styled-components";

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
  font-size: 9px;
  color: ${({ theme }) => theme.colors.textMuted};
`;

export const StyledChartTooltipValue = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 12px;
  font-weight: 700;
  color: ${({ theme }) => theme.colors.textPrimary};
`;
