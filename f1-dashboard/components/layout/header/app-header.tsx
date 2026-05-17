"use client";

import { GlobalBlockingLoader } from "@/components/shared/GlobalBlockingLoader/global-blocking-loader";
import { selectColorMode } from "@/features/store/app/appSelectors";
import { toggleColorMode } from "@/features/store/app/appUiSlice";
import {
  useBootstrapReplayMutation,
  useStartReplayMutation,
} from "@/features/store/race-state/raceStateApi";
import { useMemo, useState } from "react";
import { useDispatch, useSelector } from "react-redux";
import {
  Logo,
  StyledHeader,
  StyledHeaderBrand,
  StyledHeaderControls,
  StyledHeaderInner,
  StyledLogoSubtitle,
  StyledLogoTexts,
  StyledLogoTitle,
  StyledReplayButton,
  StyledReplayControls,
  StyledReplayStatus,
  StyledSwitch,
  StyledToggleLabel,
  StyledToggleRow,
} from "./app-header.styles";
export function AppHeader() {
  const dispatch = useDispatch();
  const colorMode = useSelector(selectColorMode);
  const isDark = colorMode === "dark";
  const [bootstrapReplay, bootstrapReplayRequest] = useBootstrapReplayMutation();
  const [startReplay, startReplayRequest] = useStartReplayMutation();
  const [replayStatus, setReplayStatus] = useState<string>("REPLAY IDLE");

  const isBusy = bootstrapReplayRequest.isLoading || startReplayRequest.isLoading;
  const statusLabel = useMemo(() => {
    if (bootstrapReplayRequest.isLoading) {
      return "BOOTSTRAPPING REPLAY...";
    }

    if (startReplayRequest.isLoading) {
      return "STARTING REPLAY...";
    }

    return replayStatus;
  }, [bootstrapReplayRequest.isLoading, replayStatus, startReplayRequest.isLoading]);

  const handleBootstrapReplay = async () => {
    try {
      await bootstrapReplay().unwrap();
      setReplayStatus("BOOTSTRAP COMPLETE");
    } catch {
      setReplayStatus("BOOTSTRAP FAILED");
    }
  };

  const handleStartReplay = async () => {
    try {
      await startReplay().unwrap();
      setReplayStatus("REPLAY STARTED");
    } catch {
      setReplayStatus("START FAILED");
    }
  };

  return (
    <>
      <StyledHeader>
        <StyledHeaderInner>
          <StyledHeaderBrand>
            <Logo />
            <StyledLogoTexts>
              <StyledLogoTitle>RACE DASHBOARD</StyledLogoTitle>
              <StyledLogoSubtitle>F1 IOT SIMULATION PLATFORM</StyledLogoSubtitle>
            </StyledLogoTexts>
          </StyledHeaderBrand>

          <StyledHeaderControls>
            <StyledReplayControls>
              <StyledReplayButton
                onClick={handleBootstrapReplay}
                disabled={isBusy}
                variant="outlined"
              >
                BOOTSTRAP REPLAY
              </StyledReplayButton>
              <StyledReplayButton
                onClick={handleStartReplay}
                disabled={isBusy}
                variant="outlined"
              >
                START REPLAY
              </StyledReplayButton>
              <StyledReplayStatus>{statusLabel}</StyledReplayStatus>
            </StyledReplayControls>

            <StyledToggleRow>
              <StyledToggleLabel $active={!isDark}>LIGHT</StyledToggleLabel>
              <StyledSwitch
                checked={isDark}
                onChange={() => dispatch(toggleColorMode())}
                size="small"
              />
              <StyledToggleLabel $active={isDark}>DARK</StyledToggleLabel>
            </StyledToggleRow>
          </StyledHeaderControls>
        </StyledHeaderInner>
      </StyledHeader>

      <GlobalBlockingLoader open={isBusy} label={statusLabel} />
    </>
  );
}
