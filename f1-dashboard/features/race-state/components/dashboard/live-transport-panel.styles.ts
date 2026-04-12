"use client";

import styled from "styled-components";
import { t } from "@/theme/styled-helpers";
import { appColors } from "@/theme/colors";

export const StyledTransportPaper = styled.div`
  border-radius: 4px;
  border: 1px solid ${({ theme }) => t(theme).colors.border};
  background-color: ${({ theme }) => t(theme).colors.panel};
  overflow: hidden;
`;

export const StyledPanelHeader = styled.div`
  padding: 8px 16px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  border-bottom: 1px solid ${({ theme }) => t(theme).colors.border};
  border-left: 4px solid ${appColors.formulaRed};
  background-color: ${({ theme }) => t(theme).colors.panel};
`;

export const StyledPanelTitle = styled.span`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-weight: 700;
  font-size: 0.72rem;
  letter-spacing: 0.12em;
  color: ${({ theme }) => t(theme).colors.textPrimary};
`;

export const StyledLiveDot = styled.span<{ $live: boolean }>`
  display: block;
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background-color: ${({ $live }) => ($live ? appColors.sectorGreen : "#555")};
`;

export const StyledStatsList = styled.div`
  padding: 2px 16px;
`;

export const StyledStatRow = styled.div`
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 0;
  border-bottom: 1px solid ${({ theme }) => t(theme).colors.border};

  &:last-child {
    border-bottom: 0;
  }
`;

export const StyledStatLabel = styled.span`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.60rem;
  letter-spacing: 0.08em;
  color: ${({ theme }) => t(theme).colors.textMuted};
`;

export const StyledStatTimeValue = styled.span`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.72rem;
  font-weight: 700;
  font-variant-numeric: tabular-nums;
  color: ${({ theme }) => t(theme).colors.textPrimary};
`;
