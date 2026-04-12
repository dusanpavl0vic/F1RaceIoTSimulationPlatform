"use client";

import styled from "styled-components";
import { t } from "@/theme/styled-helpers";
import { appColors } from "@/theme/colors";

export const StyledFooter = styled.footer`
  margin-top: auto;
  background-color: ${({ theme }) => t(theme).colors.background};
  border-top: 1px solid ${({ theme }) => t(theme).colors.border};
  box-shadow: inset 0 3px 0 0 ${appColors.formulaRed};
`;

export const StyledFooterInner = styled.div`
  width: ${({ theme }) => t(theme).layout.contentWidth};
  margin: 0 auto;
  padding: 16px 0;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  flex-wrap: wrap;

  ${({ theme }) => t(theme).breakpoints.down("sm")} {
    width: ${({ theme }) => t(theme).layout.mobileContentWidth};
    flex-direction: column;
    align-items: flex-start;
  }
`;

export const StyledFooterIdentity = styled.div`
  display: flex;
  flex-direction: column;
  gap: 3px;
`;

export const StyledFooterName = styled.span`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-weight: 700;
  font-size: 0.68rem;
  letter-spacing: 0.10em;
  color: ${({ theme }) => t(theme).colors.textPrimary};
  line-height: 1.4;
`;

export const StyledFooterUniversity = styled.span`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.55rem;
  letter-spacing: 0.08em;
  color: ${({ theme }) => t(theme).colors.textMuted};
`;

export const StyledGithubLink = styled.a`
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
