"use client";

import {
  formatTelemetryMetricValue,
  formatTelemetryTimestamp,
  type TelemetryChartPoint,
  type TelemetryMetricKey,
} from "@/helpers/telemetryStream";
import {
  StyledChartTooltip,
  StyledChartTooltipTitle,
  StyledChartTooltipValue,
} from "./telemetry-chart-tooltip.styles";

type TelemetryChartTooltipProps = {
  active?: boolean;
  payload?: Array<{ payload: TelemetryChartPoint; value: number }>;
  metric: TelemetryMetricKey;
  metricLabel: string;
  metricUnit: string;
};

function TelemetryChartTooltip({
  active,
  payload,
  metric,
  metricLabel,
  metricUnit,
}: TelemetryChartTooltipProps) {
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
      <StyledChartTooltipTitle>
        {formatTelemetryTimestamp(point.timestamp)}
      </StyledChartTooltipTitle>
    </StyledChartTooltip>
  );
}

export default TelemetryChartTooltip;
