"use client";

import { DashboardHero } from "@/components/race-state/dashboard/DashboardHero/dashboard-hero";
import { useGetTyreStintStrategyQuery } from "@/features/store/race-state/raceStateApi";
import { selectRaceStateTelemetry } from "@/features/store/race-state/raceStateTelemetrySlice";
import type { RaceDashboardDriverRow } from "@/features/store/race-state/raceStateTypes";
import { buildTelemetryDriverRows } from "@/helpers/telemetryDrivers";
import {
  telemetryMetricOptions,
  type TelemetryMetricKey,
} from "@/helpers/telemetryStream";
import { useTelemetryLapData } from "@/hooks/useTelemetryLapData";
import { skipToken } from "@reduxjs/toolkit/query";
import { Box, CircularProgress, Typography } from "@mui/material";
import { useEffect, useMemo, useState } from "react";
import { useSelector } from "react-redux";
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
  const [activeTab, setActiveTab] = useState("telemetry-history");
  const [selectedDriverNumber, setSelectedDriverNumber] = useState<
    number | null
  >(null);
  const [selectedLapNumber, setSelectedLapNumber] = useState<number | null>(
    null,
  );
  const [selectedMetric, setSelectedMetric] =
    useState<TelemetryMetricKey>(defaultMetric);

  const {
    drivers,
    session,
    status: telemetryMetadataStatus,
    error: telemetryMetadataError,
  } = useSelector(selectRaceStateTelemetry);
  const leaderboardRows = useMemo(
    () => buildTelemetryDriverRows(drivers),
    [drivers],
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

  if (
    telemetryMetadataStatus === "loading" ||
    telemetryMetadataStatus === "idle"
  ) {
    return (
      <StyledTelemetryShell>
        <CircularProgress size={24} />
        <Typography>Loading telemetry history...</Typography>
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
              errorMessage={resolveRtkQueryError(tyreStrategyError)}
            />
          </StyledTelemetryTabPanel>
        )}
      </StyledTelemetryTabsShell>
    </StyledTelemetryShell>
  );
}

const resolveRtkQueryError = (error: unknown) => {
  if (!error) {
    return null;
  }

  if (typeof error === "object" && error !== null && "status" in error) {
    return `Tyre strategy API returned ${String(error.status)}.`;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return "Tyre strategy request failed.";
};

export default TelemetryScreen;
