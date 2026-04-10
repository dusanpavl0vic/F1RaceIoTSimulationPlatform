"use client";

import { Stack, Switch, Typography } from "@mui/material";
import { selectColorMode } from "@/features/app/store/selectors";
import { toggleColorMode } from "@/features/app/store/app-ui-slice";
import {
  StyledAppHeader,
  StyledAppHeaderInner,
  StyledAppLogo,
  StyledAppLogoMark,
  StyledAppLogoText,
  StyledHeaderActions,
} from "@/components/layout/header/app-header.styles";
import { useAppDispatch, useAppSelector } from "@/store/hooks";

export function AppHeader() {
  const dispatch = useAppDispatch();
  const colorMode = useAppSelector(selectColorMode);

  return (
    <StyledAppHeader $backgroundColor="background" $borderColor="border">
      <StyledAppHeaderInner>
        <StyledAppLogo href="/" $textColor="textPrimary">
          <StyledAppLogoMark aria-hidden="true" />
          <StyledAppLogoText>F1-Dashboard</StyledAppLogoText>
        </StyledAppLogo>

        <StyledHeaderActions>
          <Stack direction="row" spacing={1} alignItems="center">
            <Typography variant="body2">Dark mode</Typography>
            <Switch
              checked={colorMode === "dark"}
              onChange={() => dispatch(toggleColorMode())}
            />
          </Stack>
        </StyledHeaderActions>
      </StyledAppHeaderInner>
    </StyledAppHeader>
  );
}
