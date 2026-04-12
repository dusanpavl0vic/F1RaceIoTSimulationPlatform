"use client";

import styled from "styled-components";
import { t } from "@/theme/styled-helpers";
import { appColors } from "@/theme/colors";

export const StyledHeroPaper = styled.div`
  padding: 16px 20px;
  border-radius: 4px;
  border: 1px solid ${({ theme }) => t(theme).colors.border};
  border-left: 4px solid ${appColors.formulaRed};
  background-color: ${({ theme }) => t(theme).colors.panel};
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 16px;
`;

export const StyledHeroTitle = styled.h2`
  margin: 0;
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-weight: 700;
  font-size: 0.95rem;
  letter-spacing: 0.10em;
  color: ${({ theme }) => t(theme).colors.textPrimary};
  line-height: 1.3;

  ${({ theme }) => t(theme).breakpoints.down("sm")} {
    font-size: 0.82rem;
  }
`;

export const StyledHeroSubtitle = styled.p`
  margin: 4px 0 0;
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.58rem;
  letter-spacing: 0.08em;
  color: ${({ theme }) => t(theme).colors.textMuted};
`;

export const StyledHeroStatus = styled.div`
  display: flex;
  align-items: center;
  gap: 8px;
`;

export const StyledLapBadge = styled.span`
  display: inline-flex;
  align-items: center;
  padding: 0 8px;
  height: 24px;
  border-radius: 3px;
  background-color: ${({ theme }) =>
    t(theme).isDark ? "rgba(255,255,255,0.07)" : "rgba(5,14,60,0.07)"};
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-weight: 700;
  font-size: 0.60rem;
  letter-spacing: 0.06em;
  color: ${({ theme }) => t(theme).colors.textPrimary};
`;

export const StyledWsBadge = styled.div<{ $live: boolean }>`
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 0 8px;
  height: 24px;
  border: 1px solid ${({ $live }) => ($live ? appColors.sectorGreen : "rgba(255,255,255,0.15)")};
  border-radius: 3px;
`;

export const StyledWsDot = styled.span<{ $live: boolean }>`
  display: block;
  width: 7px;
  height: 7px;
  border-radius: 50%;
  background-color: ${({ $live }) => ($live ? appColors.sectorGreen : "#555")};
`;

export const StyledWsLabel = styled.span<{ $live: boolean }>`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-weight: 700;
  font-size: 0.58rem;
  letter-spacing: 0.08em;
  color: ${({ $live, theme }) => ($live ? appColors.sectorGreen : t(theme).colors.textMuted)};
`;
