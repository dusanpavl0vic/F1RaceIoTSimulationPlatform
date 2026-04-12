"use client";

import styled from "styled-components";
import { t } from "@/theme/styled-helpers";
import { appColors } from "@/theme/colors";

export const StyledCardsGrid = styled.div`
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 12px;
  margin-top: 0;

  ${({ theme }) => t(theme).breakpoints.down("sm")} {
    grid-template-columns: repeat(2, 1fr);
  }
`;

export const StyledSessionCard = styled.div`
  padding: 10px 14px;
  border-radius: 4px;
  border: 1px solid ${({ theme }) => t(theme).colors.border};
  border-left: 4px solid ${appColors.formulaRed};
  background-color: ${({ theme }) => t(theme).colors.panel};
`;

export const StyledCardLabel = styled.span`
  display: block;
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.58rem;
  letter-spacing: 0.12em;
  color: ${({ theme }) => t(theme).colors.textMuted};
  margin-bottom: 6px;
`;

export const StyledCardValue = styled.span<{ $small?: boolean }>`
  display: block;
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-weight: 700;
  font-size: ${({ $small }) => ($small ? "0.72rem" : "1rem")};
  color: ${({ theme }) => t(theme).colors.textPrimary};
  word-break: break-word;
  line-height: 1.2;
`;
