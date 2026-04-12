"use client";

import styled from "styled-components";
import { Switch } from "@mui/material";
import { t } from "@/theme/styled-helpers";
import { appColors } from "@/theme/colors";

export const StyledHeader = styled.header`
  position: sticky;
  top: 0;
  z-index: 100;
  background-color: ${({ theme }) => (t(theme).isDark ? appColors.navyDeep : appColors.panel)};
  border-bottom: 1px solid ${({ theme }) => t(theme).colors.border};
  box-shadow: inset 0 3px 0 0 ${appColors.formulaRed};
`;

export const StyledHeaderInner = styled.div`
  width: ${({ theme }) => t(theme).layout.contentWidth};
  margin: 0 auto;
  height: 56px;
  display: flex;
  align-items: center;
  justify-content: space-between;

  ${({ theme }) => t(theme).breakpoints.down("sm")} {
    width: ${({ theme }) => t(theme).layout.mobileContentWidth};
  }
`;

export const StyledLogoLink = styled.a`
  display: flex;
  align-items: center;
  gap: 12px;
  text-decoration: none;
`;

export const StyledLogoImg = styled.img`
  width: 42px;
  height: 42px;
  object-fit: contain;
  display: block;
`;

export const StyledLogoTexts = styled.div`
  display: flex;
  flex-direction: column;
`;

export const StyledLogoTitle = styled.span`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-weight: 700;
  font-size: 0.80rem;
  letter-spacing: 0.12em;
  color: ${({ theme }) => t(theme).colors.textPrimary};
  line-height: 1.2;
`;

export const StyledLogoSubtitle = styled.span`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.55rem;
  letter-spacing: 0.10em;
  color: ${({ theme }) => t(theme).colors.textMuted};
  line-height: 1;
  margin-top: 3px;

  ${({ theme }) => t(theme).breakpoints.down("sm")} {
    display: none;
  }
`;

export const StyledToggleRow = styled.div`
  display: flex;
  align-items: center;
  gap: 6px;
`;

export const StyledToggleLabel = styled.span<{ $active: boolean }>`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.55rem;
  letter-spacing: 0.10em;
  font-weight: ${({ $active }) => ($active ? 700 : 400)};
  color: ${({ $active, theme }) => ($active ? appColors.formulaRed : t(theme).colors.textMuted)};
`;

export const StyledSwitch = styled(Switch)`
  & .MuiSwitch-switchBase.Mui-checked {
    color: ${appColors.formulaRed};
  }
  & .MuiSwitch-switchBase.Mui-checked + .MuiSwitch-track {
    background-color: ${appColors.formulaRed};
    opacity: 0.55;
  }
  & .MuiSwitch-track {
    background-color: ${({ theme }) =>
      t(theme).isDark ? `${appColors.navyRoyal} !important` : "rgba(5,14,60,0.20) !important"};
    opacity: 1 !important;
  }
`;
