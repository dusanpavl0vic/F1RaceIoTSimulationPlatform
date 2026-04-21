"use client";

import type {
  RaceDashboardDriverRow,
  RaceStateTelemetrySample,
} from "@/features/store/race-state/raceStateTypes";
import {
  buildTelemetryChartPoints,
  formatTelemetryAxisValue,
  formatTelemetryTimestamp,
  resolveTelemetryMetricDomain,
  resolveTelemetryMetricValue,
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
} from "./telemetry-driver-chart.styles";
import TelemetryChartTooltip from "../TelemetryChartTooltip/telemetry-chart-tooltip";

type TelemetryDriverChartProps = {
  driver: RaceDashboardDriverRow;
  samples: RaceStateTelemetrySample[];
  selectedMetric: TelemetryMetricKey;
  selectedLapNumber: number | null;
  onMetricChange: (metric: TelemetryMetricKey) => void;
};

function TelemetryDriverChart({
  driver,
  samples,
  selectedMetric,
  selectedLapNumber,
  onMetricChange,
}: TelemetryDriverChartProps) {
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
        onChange={(_, nextValue) =>
          onMetricChange(nextValue as TelemetryMetricKey)
        }
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
            <StyledDriverMetaLabel>LAP</StyledDriverMetaLabel>
            <StyledDriverMetaValue>
              {latestSample
                ? `L${selectedLapNumber ?? latestSample.lapNumber}`
                : selectedLapNumber
                  ? `L${selectedLapNumber}`
                  : "—"}
            </StyledDriverMetaValue>
          </StyledDriverMetaItem>
        </StyledDriverChartMeta>

        {chartPoints.length === 0 ? (
          <StyledChartEmptyState>
            <StyledChartEmptyText>
              No persisted telemetry samples were returned for the selected
              driver, lap and metric.
            </StyledChartEmptyText>
          </StyledChartEmptyState>
        ) : (
          <StyledChartShell>
            <ResponsiveContainer width="100%" height="100%">
              <LineChart
                data={chartPoints}
                margin={{ top: 8, right: 14, left: -10, bottom: 0 }}
              >
                <CartesianGrid
                  strokeDasharray="3 3"
                  stroke="rgba(61, 82, 133, 0.18)"
                />
                <XAxis
                  type="number"
                  dataKey="timestampMs"
                  domain={["dataMin", "dataMax"]}
                  scale="time"
                  tickFormatter={(value) =>
                    formatTelemetryTimestamp(
                      new Date(Number(value)).toISOString(),
                    )
                  }
                  tick={{ fontSize: 11, fill: "#3D5285" }}
                  axisLine={false}
                  tickLine={false}
                  minTickGap={28}
                />
                <YAxis
                  domain={resolveTelemetryMetricDomain(selectedMetric)}
                  tickFormatter={(value) =>
                    formatTelemetryAxisValue(selectedMetric, Number(value))
                  }
                  tick={{ fontSize: 11, fill: "#3D5285" }}
                  axisLine={false}
                  tickLine={false}
                  width={42}
                  allowDecimals={
                    selectedMetric === "speed" || selectedMetric === "rpm"
                  }
                />
                <Tooltip
                  cursor={{ stroke: "rgba(220, 0, 0, 0.14)", strokeWidth: 2 }}
                  content={
                    <TelemetryChartTooltip
                      metric={selectedMetric}
                      metricLabel={metricConfig.label}
                      metricUnit={metricConfig.unit}
                    />
                  }
                />
                <Line
                  type="monotone"
                  dataKey={(point: TelemetryChartPoint) =>
                    resolveTelemetryMetricValue(point, selectedMetric)
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
}

export default TelemetryDriverChart;
