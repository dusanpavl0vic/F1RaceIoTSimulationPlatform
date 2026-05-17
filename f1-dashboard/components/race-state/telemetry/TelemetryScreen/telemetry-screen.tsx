"use client";

import { DashboardHero } from "@/components/race-state/dashboard/DashboardHero/dashboard-hero";
import TelemetryPredictionPanel from "@/components/race-state/telemetry/TelemetryPredictionPanel/telemetry-prediction-panel";
import { GlobalBlockingLoader } from "@/components/shared/GlobalBlockingLoader/global-blocking-loader";
import {
  useGetTyreStintStrategyQuery,
} from "@/features/store/race-state/raceStateApi";
import {
  selectRaceStateTelemetry,
  setRaceStateTelemetryLoading,
  setRaceStateTelemetryMetadata,
} from "@/features/store/race-state/raceStateTelemetrySlice";
import { selectRaceStateLiveCurrentState } from "@/features/store/race-state/raceStateLiveSlice";
import type {
  RaceDashboardDriverRow,
  RaceCurrentState,
} from "@/features/store/race-state/raceStateTypes";
import { buildTelemetryDriverRows } from "@/helpers/telemetryDrivers";
import {
  telemetryMetricOptions,
  type TelemetryMetricKey,
} from "@/helpers/telemetryStream";
import { useNextLapPredictions } from "@/hooks/useNextLapPredictions";
import { useTelemetryLapData } from "@/hooks/useTelemetryLapData";
import { skipToken } from "@reduxjs/toolkit/query";
import { Box, Typography } from "@mui/material";
import { useEffect, useMemo, useState } from "react";
import { useDispatch, useSelector } from "react-redux";
import TelemetryDriverChart from "../TelemetryDriverChart/telemetry-driver-chart";
import TelemetryTyreStrategyChart from "../TelemetryTyreStrategyChart/telemetry-tyre-strategy-chart";
import {
  StyledTelemetryControlCard,
  StyledTelemetryControlGrid,
  StyledTelemetryControlLabel,
  StyledTelemetryControlValue,
  StyledTelemetryDriverTab,
  StyledTelemetryDriverTabs,
  StyledTelemetryErrorText,
  StyledTelemetryFilters,
  StyledTelemetryFormControl,
  StyledTelemetryMenuItem,
  StyledTelemetryPageTab,
  StyledTelemetryPageTabs,
  StyledTelemetryPanelIntro,
  StyledTelemetryPanelTitle,
  StyledTelemetrySelect,
  StyledTelemetryShell,
  StyledTelemetryTabPanel,
  StyledTelemetryTabsShell,
} from "./telemetry-screen.styles";

const defaultMetric = "speed" as const;

function TelemetryScreen() {
  const dispatch = useDispatch();
  const [activeTab, setActiveTab] = useState("telemetry-history");
  const [selectedDriverNumber, setSelectedDriverNumber] = useState<
    number | null
  >(null);
  const [selectedLapNumber, setSelectedLapNumber] = useState<number | null>(
    null,
  );
  const [selectedMetric, setSelectedMetric] =
    useState<TelemetryMetricKey>(defaultMetric);
  const [selectedPredictionDriverNumber, setSelectedPredictionDriverNumber] =
    useState<number | null>(null);

  const {
    drivers,
    session,
    status: telemetryMetadataStatus,
    error: telemetryMetadataError,
  } = useSelector(selectRaceStateTelemetry);
  const liveCurrentState = useSelector(selectRaceStateLiveCurrentState);

  useEffect(() => {
    if (!liveCurrentState) {
      dispatch(setRaceStateTelemetryLoading());
      return;
    }

    dispatch(setRaceStateTelemetryMetadata(buildTelemetryMetadata(liveCurrentState)));
  }, [dispatch, liveCurrentState]);

  const leaderboardRows = useMemo(
    () => buildTelemetryDriverRows(drivers, liveCurrentState),
    [drivers, liveCurrentState],
  );
  const sessionId = session?.sessionId ?? null;
  const selectedDriver = useMemo(
    () =>
      leaderboardRows.find(
        (driver) => driver.driverNumber === selectedDriverNumber,
      ) ??
      leaderboardRows[0] ??
      null,
    [leaderboardRows, selectedDriverNumber],
  );
  const selectedMetricOption =
    telemetryMetricOptions.find((metric) => metric.key === selectedMetric) ??
    telemetryMetricOptions[0];
  const availableLaps = useMemo(() => {
    const totalLaps = session?.totalLaps ?? null;
    if (!totalLaps || totalLaps <= 0) {
      return [];
    }

    return Array.from({ length: totalLaps }, (_, index) => index + 1);
  }, [session?.totalLaps]);

  useEffect(() => {
    if (!selectedDriver && leaderboardRows.length > 0) {
      setSelectedDriverNumber(leaderboardRows[0].driverNumber);
      return;
    }

    if (
      selectedDriverNumber &&
      !leaderboardRows.some(
        (driver) => driver.driverNumber === selectedDriverNumber,
      )
    ) {
      setSelectedDriverNumber(leaderboardRows[0]?.driverNumber ?? null);
    }
  }, [leaderboardRows, selectedDriver, selectedDriverNumber]);

  useEffect(() => {
    if (!availableLaps.length) {
      setSelectedLapNumber(null);
      return;
    }

    if (!selectedLapNumber || !availableLaps.includes(selectedLapNumber)) {
      setSelectedLapNumber(1);
    }
  }, [availableLaps, selectedLapNumber]);

  const { samples, error } = useTelemetryLapData({
    sessionId,
    driverNumber: selectedDriver?.driverNumber ?? null,
    lapNumber: selectedLapNumber,
    metric: selectedMetric,
  });
  const {
    data: tyreStrategy,
    error: tyreStrategyError,
    isFetching: tyreStrategyLoading,
  } = useGetTyreStintStrategyQuery(
    activeTab === "tyre-strategy" && sessionId ? sessionId : skipToken,
  );
  const {
    basisSnapshot,
    pendingPrediction,
    comparisonHistory,
    predictionError,
    status: predictionStatus,
    statusMessage: predictionStatusMessage,
    isRefreshingBasis: predictionBasisLoading,
    isPredicting,
    loadBasisSnapshot,
    runPrediction,
  } = useNextLapPredictions({
    selectedDriverNumber: selectedPredictionDriverNumber,
    enabled: activeTab === "next-lap-prediction",
  });

  useEffect(() => {
    if (
      selectedPredictionDriverNumber &&
      leaderboardRows.some(
        (driver) => driver.driverNumber === selectedPredictionDriverNumber,
      )
    ) {
      return;
    }

    setSelectedPredictionDriverNumber(leaderboardRows[0]?.driverNumber ?? null);
  }, [leaderboardRows, selectedPredictionDriverNumber]);

  if (
    telemetryMetadataStatus === "loading" ||
    telemetryMetadataStatus === "idle"
  ) {
    return (
      <StyledTelemetryShell>
        <GlobalBlockingLoader open label="LOADING TELEMETRY HISTORY..." />
      </StyledTelemetryShell>
    );
  }

  if (telemetryMetadataStatus === "error" || !session) {
    return (
      <StyledTelemetryShell>
        <Typography variant="h4">Telemetry unavailable</Typography>
        <Typography color="text.secondary">
          {telemetryMetadataError ??
            "Telemetry page trenutno nema dostupnu sesiju."}
        </Typography>
      </StyledTelemetryShell>
    );
  }

  return (
    <StyledTelemetryShell>
      <GlobalBlockingLoader
        open={activeTab === "next-lap-prediction" && (predictionBasisLoading || isPredicting)}
        label={predictionBasisLoading ? "LOADING DRIVER PREDICTION DATA..." : "RUNNING NEXT LAP PREDICTION..."}
      />
      <DashboardHero session={session} />

      <StyledTelemetryTabsShell>
        <StyledTelemetryPageTabs
          value={activeTab}
          onChange={(_, nextValue) => setActiveTab(nextValue)}
        >
          <StyledTelemetryPageTab
            value="telemetry-history"
            label="TELEMETRY HISTORY"
          />
          <StyledTelemetryPageTab
            value="next-lap-prediction"
            label="NEXT LAP PREDICTION"
          />
          <StyledTelemetryPageTab
            value="tyre-strategy"
            label="TYRE STRATEGY"
          />
        </StyledTelemetryPageTabs>

        {activeTab === "telemetry-history" && (
          <StyledTelemetryTabPanel>
            <StyledTelemetryPanelIntro>
              <Box>
                <StyledTelemetryPanelTitle>
                  PERSISTED TELEMETRY HISTORY
                </StyledTelemetryPanelTitle>
              </Box>
            </StyledTelemetryPanelIntro>

            <StyledTelemetryFilters>
              <StyledTelemetryDriverTabs
                value={selectedDriver?.driverNumber ?? false}
                onChange={(_, nextValue) =>
                  setSelectedDriverNumber(Number(nextValue))
                }
                variant="scrollable"
                scrollButtons="auto"
              >
                {leaderboardRows.map((driver: RaceDashboardDriverRow) => (
                  <StyledTelemetryDriverTab
                    key={driver.driverNumber}
                    value={driver.driverNumber}
                    label={`${driver.tla ?? driver.driverLabel} #${driver.driverNumber}`}
                  />
                ))}
              </StyledTelemetryDriverTabs>

              <StyledTelemetryControlGrid>
                <StyledTelemetryControlCard>
                  <StyledTelemetryControlLabel>
                    ACTIVE DRIVER
                  </StyledTelemetryControlLabel>
                  <StyledTelemetryControlValue>
                    {selectedDriver
                      ? `${selectedDriver.fullName ?? selectedDriver.broadcastName ?? selectedDriver.tla ?? selectedDriver.driverLabel}`
                      : "No driver selected"}
                  </StyledTelemetryControlValue>
                  <StyledTelemetryControlLabel>
                    {selectedDriver?.displayTeamName ?? "No team available"}
                  </StyledTelemetryControlLabel>
                </StyledTelemetryControlCard>

                <StyledTelemetryControlCard>
                  <StyledTelemetryControlLabel>
                    SELECT LAP
                  </StyledTelemetryControlLabel>
                  <StyledTelemetryFormControl size="small">
                    <StyledTelemetrySelect
                      value={selectedLapNumber ?? ""}
                      onChange={(event) =>
                        setSelectedLapNumber(Number(event.target.value))
                      }
                      displayEmpty
                    >
                      {availableLaps.length === 0 ? (
                        <StyledTelemetryMenuItem value="" disabled>
                          NO LAPS AVAILABLE
                        </StyledTelemetryMenuItem>
                      ) : (
                        availableLaps.map((lapNumber) => (
                          <StyledTelemetryMenuItem
                            key={lapNumber}
                            value={lapNumber}
                          >
                            LAP {lapNumber}
                          </StyledTelemetryMenuItem>
                        ))
                      )}
                    </StyledTelemetrySelect>
                  </StyledTelemetryFormControl>
                </StyledTelemetryControlCard>

                <StyledTelemetryControlCard>
                  <StyledTelemetryControlLabel>
                    REQUESTED METRIC
                  </StyledTelemetryControlLabel>
                  <StyledTelemetryControlValue>
                    {selectedMetricOption.label}
                  </StyledTelemetryControlValue>
                  <StyledTelemetryControlLabel>
                    X axis follows timestamps for the selected lap.
                  </StyledTelemetryControlLabel>
                </StyledTelemetryControlCard>
              </StyledTelemetryControlGrid>

              {error ? (
                <StyledTelemetryErrorText>{error}</StyledTelemetryErrorText>
              ) : null}
            </StyledTelemetryFilters>

            {selectedDriver ? (
              <TelemetryDriverChart
                driver={selectedDriver}
                samples={samples}
                selectedMetric={selectedMetric}
                selectedLapNumber={selectedLapNumber}
                onMetricChange={setSelectedMetric}
              />
            ) : (
              <Box>
                <Typography color="text.secondary">
                  No telemetry driver is currently available.
                </Typography>
              </Box>
            )}
          </StyledTelemetryTabPanel>
        )}

        {activeTab === "next-lap-prediction" && (
          <StyledTelemetryTabPanel>
            <StyledTelemetryPanelIntro>
              <Box>
                <StyledTelemetryPanelTitle>
                  NEXT LAP FORECAST WORKBENCH
                </StyledTelemetryPanelTitle>
              </Box>
            </StyledTelemetryPanelIntro>

            <StyledTelemetryFilters>
              <StyledTelemetryDriverTabs
                value={selectedPredictionDriverNumber ?? false}
                onChange={(_, nextValue) =>
                  setSelectedPredictionDriverNumber(Number(nextValue))
                }
                variant="scrollable"
                scrollButtons="auto"
              >
                {leaderboardRows.map((driver: RaceDashboardDriverRow) => (
                  <StyledTelemetryDriverTab
                    key={driver.driverNumber}
                    value={driver.driverNumber}
                    label={`${driver.tla ?? driver.driverLabel} #${driver.driverNumber}`}
                  />
                ))}
              </StyledTelemetryDriverTabs>
            </StyledTelemetryFilters>

            <TelemetryPredictionPanel
              basisSnapshot={basisSnapshot}
              pendingPrediction={pendingPrediction}
              status={predictionStatus}
              statusMessage={predictionStatusMessage}
              isRefreshingBasis={predictionBasisLoading}
              isPredicting={isPredicting}
              errorMessage={predictionError}
              comparisonHistory={comparisonHistory}
              onLoadBasisSnapshot={loadBasisSnapshot}
              onRunPrediction={runPrediction}
            />
          </StyledTelemetryTabPanel>
        )}

        {activeTab === "tyre-strategy" && (
          <StyledTelemetryTabPanel>
            <StyledTelemetryPanelIntro>
              <Box>
                <StyledTelemetryPanelTitle>
                  POSTGRESQL TYRE STRATEGY
                </StyledTelemetryPanelTitle>
              </Box>
            </StyledTelemetryPanelIntro>

            <TelemetryTyreStrategyChart
              strategy={tyreStrategy}
              isLoading={tyreStrategyLoading}
              errorMessage={resolveRtkQueryError(
                tyreStrategyError,
                "Tyre strategy request failed.",
              )}
            />
          </StyledTelemetryTabPanel>
        )}
      </StyledTelemetryTabsShell>
    </StyledTelemetryShell>
  );
}

const resolveRtkQueryError = (error: unknown, fallbackMessage: string) => {
  if (!error) {
    return null;
  }

  if (typeof error === "object" && error !== null && "status" in error) {
    return `${fallbackMessage} API returned ${String(error.status)}.`;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return fallbackMessage;
};

const buildTelemetryMetadata = (currentState: RaceCurrentState) => {
  const lapCount = currentState.session["lap.count.updated"] as
    | { currentLap?: number; totalLaps?: number; CurrentLap?: number; TotalLaps?: number }
    | undefined;

  return {
    session: {
      sessionId: currentState.sessionId,
      currentLap:
        lapCount?.currentLap ??
        lapCount?.CurrentLap ??
        null,
      totalLaps:
        lapCount?.totalLaps ??
        lapCount?.TotalLaps ??
        null,
      trackStatusCode: null,
      trackStatusMessage: null,
      lastProcessedEventTime: currentState.lastProcessedEventTime,
      lastProcessedSequence: currentState.lastProcessedSequence,
      updatedAt: currentState.updatedAt,
    },
    drivers: Object.values(currentState.drivers)
      .sort((left, right) => left.driverNumber - right.driverNumber)
      .map((driver) => ({
        driverNumber: driver.driverNumber,
        broadcastName: driver.broadcastName,
        fullName: driver.fullName,
        tla: driver.tla,
        teamName: driver.team.name,
        teamColor: driver.team.color,
        position: driver.leaderboard.position,
        gridPosition: driver.leaderboard.gridPosition,
      })),
  };
};

export default TelemetryScreen;
