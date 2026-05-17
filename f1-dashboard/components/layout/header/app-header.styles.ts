"use client";
import LogoImage from "@/assets/svg/arcticons_formula-1.svg";

import { appColors } from "@/theme/colors";
import { Box, Button, Switch, Typography } from "@mui/material";
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
  gap: 18px;

  ${({ theme }) => theme.breakpoints.down("md")} {
    height: auto;
    padding: 12px 16px;
    flex-wrap: wrap;
  }
`;

export const StyledHeaderBrand = styled(Box)`
  display: flex;
  align-items: center;
  gap: 12px;
  min-width: 0;
`;

export const Logo = styled(LogoImage)`
  width: 42px;
  height: 42px;
`;

export const StyledLogoTexts = styled(Box)`
  display: flex;
  flex-direction: column;
  min-width: 0;
`;

export const StyledLogoTitle = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-weight: 700;
  font-size: 13px;
  color: ${({ theme }) => theme.colors.textPrimary};
  line-height: 1.2;
`;

export const StyledLogoSubtitle = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 9px;
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

export const StyledHeaderControls = styled(Box)`
  display: flex;
  align-items: center;
  gap: 12px;
  margin-left: auto;

  ${({ theme }) => theme.breakpoints.down("md")} {
    width: 100%;
    justify-content: space-between;
    margin-left: 0;
  }
`;

export const StyledReplayControls = styled(Box)`
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
`;

export const StyledReplayButton = styled(Button)`
  && {
    min-width: 0;
    padding: 7px 12px;
    border-radius: 10px;
    border: 1px solid ${({ theme }) => theme.colors.border};
    background: ${({ theme }) => (theme.isDark ? appColors.navyRoyal : "rgba(5,14,60,0.05)")};
    color: ${({ theme }) => theme.colors.textPrimary};
    font-family: var(--font-silkscreen), "Silkscreen", monospace;
    font-size: 9px;
    line-height: 1;
    white-space: nowrap;
  }

  &&:hover {
    border-color: ${appColors.formulaRed};
    background: ${({ theme }) => (theme.isDark ? "rgba(226, 28, 55, 0.14)" : "rgba(226, 28, 55, 0.08)")};
  }
`;

export const StyledReplayStatus = styled(Typography)`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 9px;
  color: ${({ theme }) => theme.colors.textMuted};
  white-space: nowrap;
`;

export const StyledToggleLabel = styled(Typography) <{ $active: boolean }>`
  font-family: var(--font-silkscreen), "Silkscreen", monospace;
  font-size: 9px;
  font-weight: ${({ $active }) => ($active ? 700 : 400)};
  color: ${({ $active, theme }) => ($active ? appColors.formulaRed : theme.colors.textMuted)};
`;

export const StyledSwitch = styled(Switch)`
  width: 42px;
  height: 24px;
  padding: 0;

  & .MuiSwitch-switchBase {
    padding: 3px;
    transition-duration: 200ms;
  }

  & .MuiSwitch-thumb {
    width: 18px;
    height: 18px;
    box-shadow: none;
  }

  & .MuiSwitch-switchBase.Mui-checked {
    transform: translateX(18px);
  }

  & .MuiSwitch-switchBase.Mui-checked {
    color: ${appColors.formulaRed};
  }

  & .MuiSwitch-switchBase.Mui-checked + .MuiSwitch-track {
    background-color: ${appColors.formulaRed};
    opacity: 0.72;
  }

  & .MuiSwitch-track {
    border-radius: 999px;
    background-color: ${({ theme }) =>
    theme.isDark ? `${appColors.navyRoyal} !important` : "rgba(5,14,60,0.20) !important"};
    opacity: 1 !important;
  }
`;
