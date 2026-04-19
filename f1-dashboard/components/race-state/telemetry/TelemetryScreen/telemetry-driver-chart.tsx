"use client";

import type {
  RaceDashboardDriverRow,
  RaceStateTelemetrySample,
} from "@/features/store/race-state/raceStateTypes";
import {
  buildTelemetryChartPoints,
  formatTelemetryMetricValue,
  formatTelemetryTimestamp,
  telemetryMetricOptions,
  type TelemetryChartPoint,
  type TelemetryMetricKey,
} from "@/helpers/telemetryStream";
import {
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import {
  StyledChartEmptyState,
  StyledChartEmptyText,
  StyledChartShell,
  StyledChartTooltip,
  StyledChartTooltipTitle,
  StyledChartTooltipValue,
  StyledDriverChartBody,
  StyledDriverChartMeta,
  StyledDriverIdentity,
  StyledDriverMetaItem,
  StyledDriverMetaLabel,
  StyledDriverMetaValue,
  StyledDriverMetricTab,
  StyledDriverMetricTabs,
  StyledDriverName,
  StyledDriverStreamCard,
  StyledDriverStreamHeader,
  StyledDriverTeam,
} from "./telemetry-screen.styles";

type TelemetryDriverChartProps = {
  driver: RaceDashboardDriverRow;
  samples: RaceStateTelemetrySample[];
  selectedMetric: TelemetryMetricKey;
  selectedLapNumber: number | null;
  onMetricChange: (metric: TelemetryMetricKey) => void;
};

const resolveMetricDomain = (metric: TelemetryMetricKey) => {
  switch (metric) {
    case "throttlePct":
    case "brakePct":
      return [0, 100] as const;
    case "gear":
      return [1, 8] as const;
    case "drsEnabled":
      return [0, 1] as const;
    default:
      return ["auto", "auto"] as const;
  }
};

const formatAxisValue = (metric: TelemetryMetricKey, value: number) => {
  if (metric === "drsEnabled") {
    return value > 0 ? "ON" : "OFF";
  }

  return `${Math.round(value)}`;
};

const resolveMetricValue = (
  sample: RaceStateTelemetrySample,
  metric: TelemetryMetricKey
) => {
  if (metric === "drsEnabled") {
    return sample.drsEnabled ? 1 : 0;
  }

  return sample[metric] ?? null;
};

const CustomTelemetryTooltip = ({
  active,
  payload,
  metric,
  metricLabel,
  metricUnit,
}: {
  active?: boolean;
  payload?: Array<{ payload: TelemetryChartPoint; value: number }>;
  metric: TelemetryMetricKey;
  metricLabel: string;
  metricUnit: string;
}) => {
  if (!active || !payload?.length) {
    return null;
  }

  const point = payload[0]?.payload;
  const value = payload[0]?.value;
  if (!point || value === undefined) {
    return null;
  }

  return (
    <StyledChartTooltip>
      <StyledChartTooltipTitle>
        SAMPLE {point.sampleIndex} · LAP {point.lapNumber}
      </StyledChartTooltipTitle>
      <StyledChartTooltipValue>
        {metricLabel} {formatTelemetryMetricValue(metric, value)}
        {metricUnit ? ` ${metricUnit}` : ""}
      </StyledChartTooltipValue>
      <StyledChartTooltipTitle>{formatTelemetryTimestamp(point.timestamp)}</StyledChartTooltipTitle>
    </StyledChartTooltip>
  );
};

export const TelemetryDriverChart = ({
  driver,
  samples,
  selectedMetric,
  selectedLapNumber,
  onMetricChange,
}: TelemetryDriverChartProps) => {
  const chartPoints = buildTelemetryChartPoints(samples);
  const metricConfig =
    telemetryMetricOptions.find((option) => option.key === selectedMetric) ??
    telemetryMetricOptions[0];
  const latestSample = samples.at(-1);
  const lineColor = metricConfig.color;

  return (
    <StyledDriverStreamCard>
      <StyledDriverStreamHeader>
        <StyledDriverIdentity>
          <StyledDriverName>
            P{driver.position ?? driver.gridPosition ?? driver.line ?? "—"} ·{" "}
            {driver.tla ?? driver.driverLabel}
          </StyledDriverName>
          <StyledDriverTeam>{driver.displayTeamName}</StyledDriverTeam>
        </StyledDriverIdentity>
      </StyledDriverStreamHeader>

      <StyledDriverMetricTabs
        value={selectedMetric}
        onChange={(_, nextValue) => onMetricChange(nextValue as TelemetryMetricKey)}
        variant="scrollable"
        scrollButtons="auto"
      >
        {telemetryMetricOptions.map((metric) => (
          <StyledDriverMetricTab
            key={metric.key}
            value={metric.key}
            label={metric.label}
          />
        ))}
      </StyledDriverMetricTabs>

      <StyledDriverChartBody>
        <StyledDriverChartMeta>
          <StyledDriverMetaItem>
            <StyledDriverMetaLabel>ACTIVE METRIC</StyledDriverMetaLabel>
            <StyledDriverMetaValue>{metricConfig.label}</StyledDriverMetaValue>
          </StyledDriverMetaItem>
          <StyledDriverMetaItem>
            <StyledDriverMetaLabel>LATEST VALUE</StyledDriverMetaLabel>
            <StyledDriverMetaValue>
              {formatTelemetryMetricValue(
                selectedMetric,
                latestSample
                  ? selectedMetric === "drsEnabled"
                    ? latestSample.drsEnabled
                    : latestSample[selectedMetric]
                  : null
              )}
              {metricConfig.unit ? ` ${metricConfig.unit}` : ""}
            </StyledDriverMetaValue>
          </StyledDriverMetaItem>
          <StyledDriverMetaItem>
            <StyledDriverMetaLabel>LAP / STINT</StyledDriverMetaLabel>
            <StyledDriverMetaValue>
              {latestSample
                ? `L${selectedLapNumber ?? latestSample.lapNumber} · S${latestSample.stintNumber}`
                : selectedLapNumber
                  ? `L${selectedLapNumber}`
                  : "—"}
            </StyledDriverMetaValue>
          </StyledDriverMetaItem>
          <StyledDriverMetaItem>
            <StyledDriverMetaLabel>LAST SAMPLE</StyledDriverMetaLabel>
            <StyledDriverMetaValue>
              {latestSample ? formatTelemetryTimestamp(latestSample.timestamp) : "—"}
            </StyledDriverMetaValue>
          </StyledDriverMetaItem>
        </StyledDriverChartMeta>

        {chartPoints.length === 0 ? (
          <StyledChartEmptyState>
            <StyledChartEmptyText>
              No persisted telemetry samples were returned for the selected driver, lap and metric.
            </StyledChartEmptyText>
          </StyledChartEmptyState>
        ) : (
          <StyledChartShell>
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={chartPoints} margin={{ top: 8, right: 14, left: -10, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="rgba(61, 82, 133, 0.18)" />
                <XAxis
                  type="number"
                  dataKey="timestampMs"
                  domain={["dataMin", "dataMax"]}
                  scale="time"
                  tickFormatter={(value) =>
                    formatTelemetryTimestamp(new Date(Number(value)).toISOString())
                  }
                  tick={{ fontSize: 11, fill: "#3D5285" }}
                  axisLine={false}
                  tickLine={false}
                  minTickGap={28}
                />
                <YAxis
                  domain={resolveMetricDomain(selectedMetric)}
                  tickFormatter={(value) => formatAxisValue(selectedMetric, Number(value))}
                  tick={{ fontSize: 11, fill: "#3D5285" }}
                  axisLine={false}
                  tickLine={false}
                  width={42}
                  allowDecimals={selectedMetric === "speed" || selectedMetric === "rpm"}
                />
                <Tooltip
                  cursor={{ stroke: "rgba(220, 0, 0, 0.14)", strokeWidth: 2 }}
                  content={
                    <CustomTelemetryTooltip
                      metric={selectedMetric}
                      metricLabel={metricConfig.label}
                      metricUnit={metricConfig.unit}
                    />
                  }
                />
                <Line
                  type="monotone"
                  dataKey={(point: TelemetryChartPoint) =>
                    resolveMetricValue(point, selectedMetric)
                  }
                  stroke={lineColor}
                  strokeWidth={2.5}
                  dot={false}
                  activeDot={{ r: 4, strokeWidth: 0 }}
                  isAnimationActive={false}
                />
              </LineChart>
            </ResponsiveContainer>
          </StyledChartShell>
        )}
      </StyledDriverChartBody>
    </StyledDriverStreamCard>
  );
};
