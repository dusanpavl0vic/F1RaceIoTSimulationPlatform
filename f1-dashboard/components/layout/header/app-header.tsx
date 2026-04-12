"use client";

import { selectColorMode } from "@/features/app/store/selectors";
import { toggleColorMode } from "@/features/app/store/app-ui-slice";
import { useAppDispatch, useAppSelector } from "@/store/hooks";
import {
  StyledHeader,
  StyledHeaderInner,
  StyledLogoImg,
  StyledLogoLink,
  StyledLogoSubtitle,
  StyledLogoTexts,
  StyledLogoTitle,
  StyledSwitch,
  StyledToggleLabel,
  StyledToggleRow,
} from "./app-header.styles";

export function AppHeader() {
  const dispatch = useAppDispatch();
  const colorMode = useAppSelector(selectColorMode);
  const isDark = colorMode === "dark";

  return (
    <StyledHeader>
      <StyledHeaderInner>
        <StyledLogoLink href="/">
          <StyledLogoImg src="/logo.svg" alt="F1 Dashboard Logo" />
          <StyledLogoTexts>
            <StyledLogoTitle>RACE DASHBOARD</StyledLogoTitle>
            <StyledLogoSubtitle>F1 IOT SIMULATION PLATFORM</StyledLogoSubtitle>
          </StyledLogoTexts>
        </StyledLogoLink>

        <StyledToggleRow>
          <StyledToggleLabel $active={!isDark}>LIGHT</StyledToggleLabel>
          <StyledSwitch
            checked={isDark}
            onChange={() => dispatch(toggleColorMode())}
            size="small"
          />
          <StyledToggleLabel $active={isDark}>DARK</StyledToggleLabel>
        </StyledToggleRow>
      </StyledHeaderInner>
    </StyledHeader>
  );
}
