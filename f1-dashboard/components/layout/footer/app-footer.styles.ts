"use client";

import { appColors } from "@/theme/colors";
import { Box, Button, Typography } from "@mui/material";
import styled from "styled-components";

export const StyledFooter = styled(Box)`
  margin-top: auto;
  border-top: 1px solid ${({ theme }) => theme.colors.border};
    background-color: ${({ theme }) => (theme.isDark ? appColors.navyDeep : appColors.panel)};

`;

export const StyledFooterInner = styled(Box)`
  margin: 0 auto;
  padding: 20px 30px;

  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  flex-wrap: wrap;

  ${({ theme }) => theme.breakpoints.down("sm")} {
    flex-direction: column;
    align-items: flex-start;
  }
`;

export const StyledFooterIdentity = styled(Box)`
  display: flex;
  flex-direction: column;
  gap: 3px;
`;

export const StyledFooterName = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-weight: 700;
  font-size: 0.68rem;
  letter-spacing: 0.10em;
  color: ${({ theme }) => theme.colors.textPrimary};
  line-height: 1.4;
`;

export const StyledFooterUniversity = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.55rem;
  letter-spacing: 0.08em;
  color: ${({ theme }) => theme.colors.textMuted};
`;

export const StyledGithubLink = styled(Button)`
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 0 12px;
  height: 30px;
  border: 1px solid ${appColors.formulaRed};
  color: ${appColors.formulaRed};
  text-decoration: none;
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-weight: 700;
  font-size: 0.60rem;
  letter-spacing: 0.10em;
  border-radius: 3px;
  transition: background-color 0.12s, color 0.12s;

  &:hover {
    background-color: ${appColors.formulaRed};
    color: #fff;
  }
`;
