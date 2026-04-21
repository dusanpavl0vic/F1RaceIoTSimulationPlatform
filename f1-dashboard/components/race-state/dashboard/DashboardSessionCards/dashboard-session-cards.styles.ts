"use client";

import { Box, Typography } from "@mui/material";
import styled from "styled-components";

export const StyledCardsShell = styled(Box)`
  display: flex;
  flex-direction: column;
  gap: 14px;
  align-items: stretch;

  width: 100%;
`;

export const StyledHeroCard = styled(Box)`
  position: relative;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  padding: 18px 20px;
  border-radius: 16px;
  background:
    radial-gradient(circle at top right, rgba(220, 0, 0, 0.10), transparent 34%),
    linear-gradient(
      135deg,
      ${({ theme }) => theme.colors.panel},
      ${({ theme }) => theme.colors.backgroundSoft}
    );
`;

export const StyledHeroHeader = styled(Box)`
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
`;

export const StyledHeroValue = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 20px;
  line-height: 1.1;
  color: ${({ theme }) => theme.colors.textPrimary};
`;

export const StyledCardsCluster = styled(Box)`
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px;

  @media (max-width: 680px) {
    grid-template-columns: 1fr;
  }
`;

export const StyledMetricCard = styled(Box)`
  display: flex;
  flex-direction: column;
  padding: 5px 8px;

`;

export const StyledSessionCard = styled(Box)`
  display: flex;
  gap: 10px;
  justify-content: center;
  align-items: center;
`;

export const StyledCardEyebrow = styled(Typography)`
  display: inline-flex;
  align-items: center;
  gap: 6px;
  margin-bottom: 8px;
  font-size: 15px;
  font-weight: 700;
  color: ${({ theme }) => theme.colors.formulaRed};
  text-transform: uppercase;
`;

export const StyledCardLabel = styled(Typography)`
  display: block;
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 20px;
  color: ${({ theme }) => theme.colors.textMuted};
`;

export const StyledCardValue = styled(Typography) <{ $small?: boolean }>`
  display: block;
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-weight: 700;
  font-size: 20px;
  color: ${({ theme }) => theme.colors.textPrimary};
  word-break: break-word;
  line-height: 1.25;
`;

export const StyledHeroValueContainer = styled(Box)`
  display: flex;
  justify-content: space-between;
  align-items: center;
  width: 100%;
`;