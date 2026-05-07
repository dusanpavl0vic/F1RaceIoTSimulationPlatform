"use client";

import type {
  PredictionComparisonEntry,
  RaceNextLapPredictions,
} from "@/features/store/race-state/raceStateTypes";
import { Box, CircularProgress, Typography } from "@mui/material";
import {
  StyledPredictionCard,
  StyledPredictionCell,
  StyledPredictionHeaderRow,
  StyledPredictionHistoryGrid,
  StyledPredictionLabel,
  StyledPredictionPanel,
  StyledPredictionPanelHeader,
  StyledPredictionRow,
  StyledPredictionShell,
  StyledPredictionSubtitle,
  StyledPredictionSummaryGrid,
  StyledPredictionTable,
  StyledPredictionTitle,
  StyledPredictionValue,
} from "./telemetry-prediction-panel.styles";

type TelemetryPredictionPanelProps = {
  predictionSnapshot: RaceNextLapPredictions | undefined;
  isLoading: boolean;
  errorMessage: string | null;
  comparisonHistory: PredictionComparisonEntry[];
};

const formatLapTime = (value: number | null | undefined) => {
  if (value === null || value === undefined || Number.isNaN(value)) {
    return "—";
  }

  const minutes = Math.floor(value / 60);
  const seconds = value - minutes * 60;
  return `${minutes}:${seconds.toFixed(3).padStart(6, "0")}`;
};

const formatDelta = (value: number) => `${value >= 0 ? "+" : ""}${value.toFixed(3)}s`;

export default function TelemetryPredictionPanel({
  predictionSnapshot,
  isLoading,
  errorMessage,
  comparisonHistory,
}: TelemetryPredictionPanelProps) {
  const selectedDriver = predictionSnapshot?.drivers[0] ?? null;
  const latestResolvedPrediction = comparisonHistory[0] ?? null;

  if (isLoading && !predictionSnapshot) {
    return (
      <StyledPredictionShell>
        <CircularProgress size={24} />
        <Typography>Loading prediction snapshot...</Typography>
      </StyledPredictionShell>
    );
  }

  return (
    <StyledPredictionShell>
      <StyledPredictionSummaryGrid>
        <StyledPredictionCard>
          <StyledPredictionLabel>NEXT LAP</StyledPredictionLabel>
          <StyledPredictionValue>
            {selectedDriver?.predictedForLap ? `Lap ${selectedDriver.predictedForLap}` : "—"}
          </StyledPredictionValue>
          <StyledPredictionLabel>
            One request is sent only for the selected driver after that driver closes a lap.
          </StyledPredictionLabel>
        </StyledPredictionCard>

        <StyledPredictionCard>
          <StyledPredictionLabel>MODEL VERSION</StyledPredictionLabel>
          <StyledPredictionValue>
            {predictionSnapshot?.modelVersion ?? "unavailable"}
          </StyledPredictionValue>
          <StyledPredictionLabel>
            Direct source: telemetry-prediction-service
          </StyledPredictionLabel>
        </StyledPredictionCard>

        <StyledPredictionCard>
          <StyledPredictionLabel>SELECTED DRIVER</StyledPredictionLabel>
          <StyledPredictionValue>
            {selectedDriver?.position ? `P${selectedDriver.position}` : "—"}
          </StyledPredictionValue>
          <StyledPredictionLabel>
            {selectedDriver
              ? `${selectedDriver.driverName} #${selectedDriver.driverNumber}`
              : "No driver selected or no completed lap yet."}
          </StyledPredictionLabel>
        </StyledPredictionCard>
      </StyledPredictionSummaryGrid>

      {errorMessage ? <Typography color="error">{errorMessage}</Typography> : null}

      <StyledPredictionHistoryGrid>
        <StyledPredictionPanel>
          <StyledPredictionPanelHeader>
            <StyledPredictionTitle>NEXT LAP PREDICTION</StyledPredictionTitle>
            <StyledPredictionSubtitle>
              The selected driver is predicted for the next lap only.
            </StyledPredictionSubtitle>
          </StyledPredictionPanelHeader>

          {!selectedDriver ? (
            <Box sx={{ p: 2 }}>
              <Typography color="text.secondary">
                Prediction snapshot trenutno nema dovoljno podataka za izabranog vozača.
              </Typography>
            </Box>
          ) : (
            <StyledPredictionTable>
              <StyledPredictionHeaderRow>
                <StyledPredictionCell $muted>DRIVER</StyledPredictionCell>
                <StyledPredictionCell $muted>NEXT LAP</StyledPredictionCell>
                <StyledPredictionCell $muted>PREDICTED</StyledPredictionCell>
                <StyledPredictionCell $muted>BEST LAP</StyledPredictionCell>
              </StyledPredictionHeaderRow>

              <StyledPredictionRow $active>
                <StyledPredictionCell $highlight>
                  {selectedDriver.driverName} #{selectedDriver.driverNumber}
                </StyledPredictionCell>
                <StyledPredictionCell>
                  {selectedDriver.predictedForLap
                    ? `Lap ${selectedDriver.predictedForLap}`
                    : "—"}
                </StyledPredictionCell>
                <StyledPredictionCell>
                  {formatLapTime(selectedDriver.predictedNextLapTime)}
                </StyledPredictionCell>
                <StyledPredictionCell $muted>
                  {formatLapTime(selectedDriver.bestLapTimeActual)}
                </StyledPredictionCell>
              </StyledPredictionRow>
            </StyledPredictionTable>
          )}
        </StyledPredictionPanel>

        <StyledPredictionPanel>
          <StyledPredictionPanelHeader>
            <StyledPredictionTitle>
              {selectedDriver
                ? `ACTUAL VS PREDICTED: ${selectedDriver.driverName} #${selectedDriver.driverNumber}`
                : "COMPARE WITH ACTUALS"}
            </StyledPredictionTitle>
            <StyledPredictionSubtitle>
              When the next real lap is completed, the panel shows actual time, delta and prediction accuracy.
            </StyledPredictionSubtitle>
          </StyledPredictionPanelHeader>

          {selectedDriver ? (
            <>
              <StyledPredictionSummaryGrid>
                <StyledPredictionCard>
                  <StyledPredictionLabel>LAST RESOLVED LAP</StyledPredictionLabel>
                  <StyledPredictionValue>
                    {latestResolvedPrediction
                      ? `Lap ${latestResolvedPrediction.lapNumber}`
                      : "—"}
                  </StyledPredictionValue>
                </StyledPredictionCard>
                <StyledPredictionCard>
                  <StyledPredictionLabel>ACTUAL TIME</StyledPredictionLabel>
                  <StyledPredictionValue>
                    {formatLapTime(latestResolvedPrediction?.actualLapTime)}
                  </StyledPredictionValue>
                </StyledPredictionCard>
                <StyledPredictionCard>
                  <StyledPredictionLabel>ACCURACY</StyledPredictionLabel>
                  <StyledPredictionValue>
                    {latestResolvedPrediction
                      ? `${latestResolvedPrediction.accuracyPercentage.toFixed(2)}%`
                      : "—"}
                  </StyledPredictionValue>
                </StyledPredictionCard>
              </StyledPredictionSummaryGrid>

              <StyledPredictionTable>
                <StyledPredictionHeaderRow>
                  <StyledPredictionCell $muted>LAP</StyledPredictionCell>
                  <StyledPredictionCell $muted>PREDICTED</StyledPredictionCell>
                  <StyledPredictionCell $muted>ACTUAL</StyledPredictionCell>
                  <StyledPredictionCell $muted>DELTA / ACC</StyledPredictionCell>
                </StyledPredictionHeaderRow>

                {comparisonHistory.length === 0 ? (
                  <Box sx={{ p: 2 }}>
                    <Typography color="text.secondary">
                      Sačekaj da izabrani vozač završi sledeći krug da bi se videlo poređenje.
                    </Typography>
                  </Box>
                ) : (
                  comparisonHistory.map((entry) => (
                    <StyledPredictionRow key={`${selectedDriver.driverNumber}:${entry.lapNumber}`}>
                      <StyledPredictionCell $highlight>
                        Lap {entry.lapNumber}
                      </StyledPredictionCell>
                      <StyledPredictionCell>
                        {formatLapTime(entry.predictedLapTime)}
                      </StyledPredictionCell>
                      <StyledPredictionCell>
                        {formatLapTime(entry.actualLapTime)}
                      </StyledPredictionCell>
                      <StyledPredictionCell
                        $highlight={entry.accuracyPercentage >= 99}
                        $muted={entry.accuracyPercentage < 99}
                      >
                        {`${formatDelta(entry.deltaToActual)} / ${entry.accuracyPercentage.toFixed(2)}%`}
                      </StyledPredictionCell>
                    </StyledPredictionRow>
                  ))
                )}
              </StyledPredictionTable>
            </>
          ) : (
            <Box sx={{ p: 2 }}>
              <Typography color="text.secondary">
                No driver available for prediction comparison.
              </Typography>
            </Box>
          )}
        </StyledPredictionPanel>
      </StyledPredictionHistoryGrid>
    </StyledPredictionShell>
  );
}
