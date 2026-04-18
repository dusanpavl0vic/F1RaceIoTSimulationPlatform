"use client";
import LogoImage from "@/assets/svg/arcticons_formula-1.svg";

import { appColors } from "@/theme/colors";
import { Box, Switch, Typography } from "@mui/material";
import styled from "styled-components";

export const StyledHeader = styled(Box)`
  display: flex;
  flex-direction: row;
  justify-content: center;
  align-items: center;
  position: sticky;
  top: 0;
  z-index: 1000;
  background-color: ${({ theme }) => (theme.isDark ? appColors.navyDeep : appColors.panel)};
  border-bottom: 1px solid ${({ theme }) => theme.colors.border};
`;

export const StyledHeaderInner = styled(Box)`
  height: 56px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  padding: 0px 30px;
`;

export const Logo = styled(LogoImage)`
  width: 42px;
  height: 42px;
`;

export const StyledLogoTexts = styled(Box)`
  display: flex;
  flex-direction: column;
`;

export const StyledLogoTitle = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-weight: 700;
  font-size: 0.80rem;
  letter-spacing: 0.12em;
  color: ${({ theme }) => theme.colors.textPrimary};
  line-height: 1.2;
`;

export const StyledLogoSubtitle = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.55rem;
  letter-spacing: 0.10em;
  color: ${({ theme }) => theme.colors.textMuted};
  line-height: 1;
  margin-top: 3px;

  ${({ theme }) => theme.breakpoints.down("sm")} {
    display: none;
  }
`;

export const StyledToggleRow = styled(Box)`
  display: flex;
  align-items: center;
  gap: 6px;
`;

export const StyledToggleLabel = styled(Typography) <{ $active: boolean }>`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 0.55rem;
  letter-spacing: 0.10em;
  font-weight: ${({ $active }) => ($active ? 700 : 400)};
  color: ${({ $active, theme }) => ($active ? appColors.formulaRed : theme.colors.textMuted)};
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
    theme.isDark ? `${appColors.navyRoyal} !important` : "rgba(5,14,60,0.20) !important"};
    opacity: 1 !important;
  }
`;
