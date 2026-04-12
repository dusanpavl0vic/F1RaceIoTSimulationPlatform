"use client";

import styled from "styled-components";
import { t } from "@/theme/styled-helpers";
import { appColors } from "@/theme/colors";

export const StyledRunningPaper = styled.div`
  border-radius: 4px;
  border: 1px solid ${({ theme }) => t(theme).colors.border};
  background-color: ${({ theme }) => t(theme).colors.panel};
  overflow: hidden;
`;

export const StyledRunningHeader = styled.div`
  padding: 8px 16px;
  display: flex;
  align-items: center;
  gap: 8px;
  border-bottom: 1px solid ${({ theme }) => t(theme).colors.border};
  border-left: 4px solid ${appColors.formulaRed};
  background-color: ${({ theme }) => t(theme).colors.panel};
`;

export const StyledRunningTitle = styled.span`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-weight: 700;
  font-size: 0.72rem;
  letter-spacing: 0.12em;
  color: ${({ theme }) => t(theme).colors.textPrimary};
`;

export const StyledRunningList = styled.div`
  padding: 4px 0;
`;

export const StyledRunningRow = styled.div<{ $dimmed: boolean; $color: string }>`
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 7px 16px;
  opacity: ${({ $dimmed }) => ($dimmed ? 0.4 : 1)};
  border-bottom: 1px solid ${({ theme }) => t(theme).colors.border};
  border-left: 3px solid ${({ $color }) => $color};

  &:last-child {
    border-bottom: 0;
  }

  &:hover {
    background-color: ${({ theme }) =>
      t(theme).isDark ? "rgba(255,255,255,0.04)" : "rgba(5,14,60,0.03)"};
  }
`;

export const StyledRunningLeft = styled.div`
  display: flex;
  align-items: center;
  gap: 12px;
`;

export const StyledRunningPos = styled.span<{ $isLeader: boolean }>`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-weight: 700;
  font-size: 0.75rem;
  min-width: 20px;
  color: ${({ $isLeader }) => ($isLeader ? appColors.formulaRed : "inherit")};
`;

export const StyledRunningName = styled.span<{ $color: string }>`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-weight: 700;
  font-size: 0.75rem;
  color: ${({ $color }) => $color};
`;

export const StyledRunningTeam = styled.span`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.58rem;
  color: ${({ theme }) => t(theme).colors.textMuted};
`;

export const StyledRunningRight = styled.div`
  text-align: right;
`;

export const StyledRunningGap = styled.span`
  display: block;
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-weight: 700;
  font-size: 0.72rem;
  font-variant-numeric: tabular-nums;
  color: ${({ theme }) => t(theme).colors.textPrimary};
`;

export const StyledRunningStatus = styled.span<{ $status: string }>`
  display: block;
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.58rem;
  color: ${({ $status }) =>
    $status === "RET" || $status === "STOP"
      ? appColors.formulaRed
      : $status === "PIT"
        ? appColors.timingYellow
        : "inherit"};
`;
