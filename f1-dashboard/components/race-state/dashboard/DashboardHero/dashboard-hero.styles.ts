"use client";

import { Box, Typography } from "@mui/material";
import styled from "styled-components";

export const StyledHeroPaper = styled(Box)`
  padding: 16px 20px;
  border-radius: 16px;
  background-color: ${({ theme }) => theme.colors.panel};
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 16px;
`;

export const StyledHeroTitle = styled(Typography)`
  margin: 0;
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-weight: 700;
  font-size: 0.95rem;
  letter-spacing: 0.10em;
  color: ${({ theme }) => theme.colors.textPrimary};
  line-height: 1.3;

  ${({ theme }) => theme.breakpoints.down("sm")} {
    font-size: 0.82rem;
  }
`;

export const StyledHeroSubtitle = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.58rem;
  letter-spacing: 0.08em;
  color: ${({ theme }) => theme.colors.textMuted};
`;


