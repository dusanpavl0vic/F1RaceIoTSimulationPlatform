"use client";

import styled from "styled-components";
import { TableCell } from "@mui/material";
import { t } from "@/theme/styled-helpers";
import { appColors } from "@/theme/colors";

export const StyledTableWrapper = styled.div`
  border-radius: 4px;
  border: 1px solid ${({ theme }) => t(theme).colors.border};
  background-color: ${({ theme }) => t(theme).isDark ? t(theme).colors.panel : t(theme).colors.background};
  overflow: hidden;
`;

export const StyledTableTopBar = styled.div`
  padding: 8px 16px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  border-bottom: 1px solid ${({ theme }) => t(theme).colors.border};
  border-left: 4px solid ${appColors.formulaRed};
  background-color: ${({ theme }) => t(theme).colors.panel};
`;

export const StyledTableTitle = styled.span`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-weight: 700;
  font-size: 0.72rem;
  letter-spacing: 0.12em;
  color: ${({ theme }) => t(theme).colors.textPrimary};
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

export const StyledTableCount = styled.span`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.58rem;
  letter-spacing: 0.06em;
  color: ${({ theme }) => t(theme).colors.textMuted};
`;
