"use client";

import { Box, TableCell, Typography } from "@mui/material";
import styled from "styled-components";

export const StyledHeadCell = styled(TableCell)`
  padding-top: 11px !important;
  padding-bottom: 11px !important;
  font-family: var(--font-silkscreen), "Silkscreen", monospace !important;
  font-weight: 700 !important;
  font-size: 10px !important;
  text-transform: uppercase !important;
  white-space: nowrap !important;
  color: ${({ theme }) => theme.colors.textMuted} !important;
  background-color: ${({ theme }) =>
    theme.isDark
      ? theme.colors.background
      : theme.colors.backgroundSoft} !important;
  border-bottom: 1px solid ${({ theme }) => theme.colors.border} !important;
`;

export const StyledTableWrapper = styled(Box)`
  border-radius: 16px;
  border: 1px solid ${({ theme }) => theme.colors.border};
  background-color: ${({ theme }) =>
    theme.isDark ? theme.colors.panel : theme.colors.background};
  overflow: hidden;
`;

export const StyledTableTopBar = styled(Box)`
  padding: 8px 16px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  border-bottom: 1px solid ${({ theme }) => theme.colors.border};
  background-color: ${({ theme }) => theme.colors.panel};
`;

export const StyledTableTitle = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-weight: 700;
  font-size: 12px;
  color: ${({ theme }) => theme.colors.textPrimary};
`;

export const StyledPosHeadCell = styled(StyledHeadCell)`
  padding-left: 16px !important;
  width: 40px !important;
` as typeof StyledHeadCell;

export const StyledWideHeadCell = styled(StyledHeadCell)`
  min-width: 130px !important;
` as typeof StyledHeadCell;

export const StyledLastHeadCell = styled(StyledHeadCell)`
  padding-right: 16px !important;
  text-align: right !important;
` as typeof StyledHeadCell;

export const StyledTableCount = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 9px;
  color: ${({ theme }) => theme.colors.textMuted};
`;
