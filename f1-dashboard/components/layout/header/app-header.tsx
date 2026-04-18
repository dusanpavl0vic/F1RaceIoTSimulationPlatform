"use client";

import { selectColorMode } from "@/features/store/app/appSelectors";
import { toggleColorMode } from "@/features/store/app/appUiSlice";
import { useDispatch, useSelector } from "react-redux";
import {
  Logo,
  StyledHeader,
  StyledHeaderInner,
  StyledLogoSubtitle,
  StyledLogoTexts,
  StyledLogoTitle,
  StyledSwitch,
  StyledToggleLabel,
  StyledToggleRow,
} from "./app-header.styles";
export function AppHeader() {
  const dispatch = useDispatch();
  const colorMode = useSelector(selectColorMode);
  const isDark = colorMode === "dark";

  return (
    <StyledHeader>
      <StyledHeaderInner>
        <Logo />
        <StyledLogoTexts>
          <StyledLogoTitle>RACE DASHBOARD</StyledLogoTitle>
          <StyledLogoSubtitle>F1 IOT SIMULATION PLATFORM</StyledLogoSubtitle>
        </StyledLogoTexts>

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
