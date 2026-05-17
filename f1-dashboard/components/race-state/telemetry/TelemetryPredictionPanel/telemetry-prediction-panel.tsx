"use client";

import type {
  DriverPredictionFeaturesResponse,
  NextLapPredictionCycle,
  PredictionComparisonEntry,
  PredictionWorkflowStatus,
} from "@/features/store/race-state/raceStateTypes";
import { Box } from "@mui/material";
import {
  StyledPredictionActionCard,
  StyledPredictionActionText,
  StyledPredictionActionTitle,
  StyledPredictionBadge,
  StyledPredictionBody,
  StyledPredictionButton,
  StyledPredictionCycleGrid,
  StyledPredictionError,
  StyledPredictionEyebrow,
  StyledPredictionHeadline,
  StyledPredictionHero,
  StyledPredictionHistoryCell,
  StyledPredictionHistoryHeader,
  StyledPredictionHistoryList,
  StyledPredictionHistoryRow,
  StyledPredictionMetricCard,
  StyledPredictionMetricGrid,
  StyledPredictionMetricLabel,
  StyledPredictionMetricValue,
  StyledPredictionPanel,
  StyledPredictionPanelGrid,
  StyledPredictionPanelHeader,
  StyledPredictionPanelSubtitle,
  StyledPredictionPanelTitle,
  StyledPredictionPlaceholder,
  StyledPredictionShell,
  StyledPredictionStage,
  StyledPredictionStatusRow,
  StyledPredictionTimelineCard,
  StyledPredictionTimelineText,
  StyledPredictionTimelineTitle,
} from "./telemetry-prediction-panel.styles";

type TelemetryPredictionPanelProps = {
  basisSnapshot: DriverPredictionFeaturesResponse | null;
  pendingPrediction: NextLapPredictionCycle | null;
  status: PredictionWorkflowStatus;
  statusMessage: string;
  isRefreshingBasis: boolean;
  isPredicting: boolean;
  errorMessage: string | null;
  comparisonHistory: PredictionComparisonEntry[];
  onLoadBasisSnapshot: () => Promise<void>;
  onRunPrediction: () => Promise<void>;
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

const resolveStatusTone = (status: PredictionWorkflowStatus) => {
  switch (status) {
    case "predicting":
    case "loading_basis":
      return "active" as const;
    case "waiting_for_actual":
      return "warning" as const;
    case "resolved":
    case "ready":
      return "success" as const;
    case "error":
      return "danger" as const;
    default:
      return "neutral" as const;
  }
};

const resolveStatusLabel = (status: PredictionWorkflowStatus) => {
  switch (status) {
    case "loading_basis":
      return "REFRESHING BASIS";
    case "ready":
      return "READY TO PREDICT";
    case "predicting":
      return "RUNNING MODEL";
    case "waiting_for_actual":
      return "WAITING FOR ACTUAL LAP";
    case "resolved":
      return "COMPARISON RESOLVED";
    case "error":
      return "MODEL ERROR";
    default:
      return "INSUFFICIENT DATA";
  }
};

export default function TelemetryPredictionPanel({
  basisSnapshot,
  pendingPrediction,
  status,
  statusMessage,
  isRefreshingBasis,
  isPredicting,
  errorMessage,
  comparisonHistory,
  onLoadBasisSnapshot,
  onRunPrediction,
}: TelemetryPredictionPanelProps) {
  const latestComparison = comparisonHistory[0] ?? null;
  const activeCycle = pendingPrediction ?? null;
  const canPredict =
    status === "ready" &&
    Boolean(basisSnapshot?.features) &&
    !isPredicting &&
    !isRefreshingBasis;
  const targetLapNumber =
    pendingPrediction?.predictedForLap ??
    (basisSnapshot?.lastCompletedLapNumber
      ? basisSnapshot.lastCompletedLapNumber + 1
      : null);
  const displayedBasisLapTime =
    activeCycle?.basisLapTime ?? latestComparison?.basisLapTime ?? basisSnapshot?.lastCompletedLapTimeSeconds ?? null;
  const displayedActualLapTime =
    activeCycle?.actualNextLapTime ?? latestComparison?.actualLapTime ?? null;
  const displayedDelta =
    activeCycle?.deltaToActual ?? latestComparison?.deltaToActual ?? null;
  const displayedAccuracy =
    activeCycle?.accuracyPercentage ?? latestComparison?.accuracyPercentage ?? null;

  return (
    <StyledPredictionShell>
      <StyledPredictionHero>
        <StyledPredictionStage>
          <StyledPredictionStatusRow>
            <Box>
              <StyledPredictionEyebrow>NEXT LAP FORECAST</StyledPredictionEyebrow>
              <StyledPredictionHeadline>
                {basisSnapshot
                  ? `${basisSnapshot.driverName} #${basisSnapshot.driverNumber}`
                  : "No prediction basis yet"}
              </StyledPredictionHeadline>
            </Box>

            <StyledPredictionBadge $tone={resolveStatusTone(status)}>
              {resolveStatusLabel(status)}
            </StyledPredictionBadge>
          </StyledPredictionStatusRow>

          <StyledPredictionMetricGrid>
            <StyledPredictionMetricCard>
              <StyledPredictionMetricLabel>BASIS LAP</StyledPredictionMetricLabel>
              <StyledPredictionMetricValue>
                {basisSnapshot?.lastCompletedLapNumber
                  ? `Lap ${basisSnapshot.lastCompletedLapNumber}`
                  : "—"}
              </StyledPredictionMetricValue>
            </StyledPredictionMetricCard>

            <StyledPredictionMetricCard>
              <StyledPredictionMetricLabel>TARGET LAP</StyledPredictionMetricLabel>
              <StyledPredictionMetricValue $accent>
                {targetLapNumber ? `Lap ${targetLapNumber}` : "—"}
              </StyledPredictionMetricValue>
            </StyledPredictionMetricCard>

            <StyledPredictionMetricCard>
              <StyledPredictionMetricLabel>LAST COMPLETED</StyledPredictionMetricLabel>
              <StyledPredictionMetricValue>
                {formatLapTime(basisSnapshot?.lastCompletedLapTimeSeconds)}
              </StyledPredictionMetricValue>
            </StyledPredictionMetricCard>

            <StyledPredictionMetricCard>
              <StyledPredictionMetricLabel>ACTIVE FORECAST</StyledPredictionMetricLabel>
              <StyledPredictionMetricValue $accent={Boolean(pendingPrediction)}>
                {formatLapTime(pendingPrediction?.predictedNextLapTime ?? null)}
              </StyledPredictionMetricValue>
            </StyledPredictionMetricCard>
          </StyledPredictionMetricGrid>

          <StyledPredictionCycleGrid>
            <StyledPredictionMetricCard>
              <StyledPredictionMetricLabel>BASIS TIME</StyledPredictionMetricLabel>
              <StyledPredictionMetricValue>
                {formatLapTime(displayedBasisLapTime)}
              </StyledPredictionMetricValue>
            </StyledPredictionMetricCard>

            <StyledPredictionMetricCard>
              <StyledPredictionMetricLabel>PREDICTED</StyledPredictionMetricLabel>
              <StyledPredictionMetricValue $accent>
                {formatLapTime(activeCycle?.predictedNextLapTime ?? latestComparison?.predictedLapTime ?? null)}
              </StyledPredictionMetricValue>
            </StyledPredictionMetricCard>

            <StyledPredictionMetricCard>
              <StyledPredictionMetricLabel>ACTUAL</StyledPredictionMetricLabel>
              <StyledPredictionMetricValue>
                {displayedActualLapTime === null ? "WAITING" : formatLapTime(displayedActualLapTime)}
              </StyledPredictionMetricValue>
            </StyledPredictionMetricCard>

            <StyledPredictionMetricCard>
              <StyledPredictionMetricLabel>DELTA</StyledPredictionMetricLabel>
              <StyledPredictionMetricValue $accent={displayedDelta !== null}>
                {displayedDelta === null ? "WAITING" : formatDelta(displayedDelta)}
              </StyledPredictionMetricValue>
            </StyledPredictionMetricCard>

            <StyledPredictionMetricCard>
              <StyledPredictionMetricLabel>ACCURACY</StyledPredictionMetricLabel>
              <StyledPredictionMetricValue $accent={displayedAccuracy !== null}>
                {displayedAccuracy === null ? "WAITING" : `${displayedAccuracy.toFixed(2)}%`}
              </StyledPredictionMetricValue>
            </StyledPredictionMetricCard>

            <StyledPredictionMetricCard>
              <StyledPredictionMetricLabel>STATUS</StyledPredictionMetricLabel>
              <StyledPredictionMetricValue>
                {pendingPrediction ? "PENDING" : latestComparison ? "RESOLVED" : "IDLE"}
              </StyledPredictionMetricValue>
            </StyledPredictionMetricCard>
          </StyledPredictionCycleGrid>
        </StyledPredictionStage>

        <StyledPredictionActionCard>
          <StyledPredictionActionTitle>RUN MODEL</StyledPredictionActionTitle>
          <StyledPredictionActionText>
            {pendingPrediction
              ? `The model already predicted Lap ${pendingPrediction.predictedForLap}. No second request is sent until that lap is completed.`
              : basisSnapshot?.lastCompletedLapNumber
                ? `The latest valid basis is Lap ${basisSnapshot.lastCompletedLapNumber}. Trigger the forecast when you want to estimate Lap ${basisSnapshot.lastCompletedLapNumber + 1}.`
                : "As soon as the selected driver records a valid completed lap, the basis snapshot will appear here."}
          </StyledPredictionActionText>

          <StyledPredictionButton
            variant="outlined"
            onClick={() => {
              void onLoadBasisSnapshot();
            }}
            disabled={isRefreshingBasis || isPredicting}
          >
            {isRefreshingBasis ? "LOADING..." : "LOAD DRIVER DATA"}
          </StyledPredictionButton>

          <StyledPredictionButton
            variant="outlined"
            onClick={() => {
              void onRunPrediction();
            }}
            disabled={!canPredict}
          >
            {isPredicting ? "RUNNING..." : "PREDICT NEXT LAP"}
          </StyledPredictionButton>

          {errorMessage ? <StyledPredictionError>{errorMessage}</StyledPredictionError> : null}
        </StyledPredictionActionCard>
      </StyledPredictionHero>

      <StyledPredictionPanelGrid>
        <StyledPredictionPanel>
          <StyledPredictionPanelHeader>
            <StyledPredictionPanelTitle>CURRENT CYCLE</StyledPredictionPanelTitle>
          </StyledPredictionPanelHeader>

          <StyledPredictionBody>
            <StyledPredictionTimelineCard>
              <StyledPredictionTimelineTitle>Basis snapshot from race state</StyledPredictionTimelineTitle>
              <StyledPredictionTimelineText>
                {pendingPrediction
                  ? `Prediction was created from Lap ${pendingPrediction.basisLapNumber} using ${formatLapTime(pendingPrediction.basisLapTime)} as the last completed lap time.`
                  : basisSnapshot
                    ? `The current basis is Lap ${basisSnapshot.lastCompletedLapNumber ?? "—"} with last lap ${formatLapTime(basisSnapshot.lastCompletedLapTimeSeconds)} and rolling averages ${formatLapTime(basisSnapshot.lapTimeAvgLast3)} / ${formatLapTime(basisSnapshot.lapTimeAvgLast5)}.`
                  : "No basis snapshot is available yet for the selected driver."}
              </StyledPredictionTimelineText>
            </StyledPredictionTimelineCard>

            <StyledPredictionTimelineCard>
              <StyledPredictionTimelineTitle>Model forecast</StyledPredictionTimelineTitle>
              <StyledPredictionTimelineText>
                {pendingPrediction
                  ? `Lap ${pendingPrediction.basisLapNumber} produced a forecast of ${formatLapTime(pendingPrediction.predictedNextLapTime)} for Lap ${pendingPrediction.predictedForLap}. The UI is now waiting for the driver to finish that target lap.`
                  : latestComparison
                    ? `The latest resolved comparison was for Lap ${latestComparison.lapNumber}: predicted ${formatLapTime(latestComparison.predictedLapTime)}, actual ${formatLapTime(latestComparison.actualLapTime)}, delta ${formatDelta(latestComparison.deltaToActual)}.`
                    : "No active forecast is waiting for resolution right now."}
              </StyledPredictionTimelineText>
            </StyledPredictionTimelineCard>
          </StyledPredictionBody>
        </StyledPredictionPanel>

        <StyledPredictionPanel>
          <StyledPredictionPanelHeader>
            <StyledPredictionPanelTitle>PREDICTION HISTORY</StyledPredictionPanelTitle>
          </StyledPredictionPanelHeader>

          {comparisonHistory.length === 0 ? (
            <StyledPredictionBody>
              <StyledPredictionPlaceholder>
                <StyledPredictionTimelineText>
                  No resolved comparison yet. Run a prediction after a valid completed lap, then
                  wait for the driver to close the next lap.
                </StyledPredictionTimelineText>
              </StyledPredictionPlaceholder>
            </StyledPredictionBody>
          ) : (
            <StyledPredictionHistoryList>
              <StyledPredictionHistoryHeader>
                <StyledPredictionHistoryCell $muted>DRIVER</StyledPredictionHistoryCell>
                <StyledPredictionHistoryCell $muted>LAP</StyledPredictionHistoryCell>
                <StyledPredictionHistoryCell $muted>PREDICTED</StyledPredictionHistoryCell>
                <StyledPredictionHistoryCell $muted>ACTUAL</StyledPredictionHistoryCell>
              </StyledPredictionHistoryHeader>

              {comparisonHistory.map((entry) => (
                <StyledPredictionHistoryRow key={`${entry.lapNumber}:${entry.resolvedAt}`}>
                  <StyledPredictionHistoryCell $accent>
                    {`${entry.driverName} #${entry.driverNumber}`}
                  </StyledPredictionHistoryCell>
                  <StyledPredictionHistoryCell $accent>
                    Lap {entry.lapNumber}
                  </StyledPredictionHistoryCell>
                  <StyledPredictionHistoryCell>
                    {formatLapTime(entry.predictedLapTime)}
                  </StyledPredictionHistoryCell>
                  <StyledPredictionHistoryCell>
                    {formatLapTime(entry.actualLapTime)}
                  </StyledPredictionHistoryCell>
                </StyledPredictionHistoryRow>
              ))}
            </StyledPredictionHistoryList>
          )}
        </StyledPredictionPanel>
      </StyledPredictionPanelGrid>
    </StyledPredictionShell>
  );
}
