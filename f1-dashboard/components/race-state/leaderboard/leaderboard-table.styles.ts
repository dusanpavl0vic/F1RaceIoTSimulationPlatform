"use client";

import { Box, TableCell, Typography } from "@mui/material";
import styled from "styled-components";

export const StyledTableWrapper = styled(Box)`
  border-radius: 16px;
  border: 1px solid ${({ theme }) => theme.colors.border};
  background-color: ${({ theme }) => theme.isDark ? theme.colors.panel : theme.colors.background};
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
  font-size: 0.72rem;
  letter-spacing: 0.12em;
  color: ${({ theme }) => theme.colors.textPrimary};
`;

export const StyledPosHeadCell = styled(TableCell)`
  padding-left: 16px !important;
  width: 40px !important;
` as typeof TableCell;

export const StyledWideHeadCell = styled(TableCell)`
  min-width: 130px !important;
` as typeof TableCell;

export const StyledLastHeadCell = styled(TableCell)`
  padding-right: 16px !important;
` as typeof TableCell;

export const StyledTableCount = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.58rem;
  letter-spacing: 0.06em;
  color: ${({ theme }) => theme.colors.textMuted};
`;
