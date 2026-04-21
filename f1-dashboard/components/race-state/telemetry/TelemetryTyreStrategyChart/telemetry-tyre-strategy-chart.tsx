"use client";

import type { RaceTyreStintStrategy } from "@/features/store/race-state/raceStateTypes";
import { Fragment, type CSSProperties } from "react";
import {
  StyledTyreLegendItem,
  StyledTyreLegendText,
  StyledTyreStintBlock,
  StyledTyreStintLabel,
  StyledTyreStrategyAxis,
  StyledTyreStrategyAxisSpacer,
  StyledTyreStrategyAxisTick,
  StyledTyreStrategyBody,
  StyledTyreStrategyCard,
  StyledTyreStrategyDriver,
  StyledTyreStrategyDriverName,
  StyledTyreStrategyHeader,
  StyledTyreStrategyLegend,
  StyledTyreStrategyPlot,
  StyledTyreStrategyRow,
  StyledTyreStrategySpinner,
  StyledTyreStrategyStatus,
  StyledTyreStrategyStatusText,
  StyledTyreStrategySubtitle,
  StyledTyreStrategyTeam,
  StyledTyreStrategyTitle,
} from "./telemetry-tyre-strategy-chart.styles";

type TelemetryTyreStrategyChartProps = {
  strategy: RaceTyreStintStrategy | undefined;
  isLoading: boolean;
  errorMessage: string | null;
};

function TelemetryTyreStrategyChart({
  strategy,
  isLoading,
  errorMessage,
}: TelemetryTyreStrategyChartProps) {
  if (isLoading) {
    return (
      <StyledTyreStrategyCard>
        <StyledTyreStrategyStatus>
          <StyledTyreStrategySpinner size={24} />
          <StyledTyreStrategyStatusText>
            Loading tyre stint strategy...
          </StyledTyreStrategyStatusText>
        </StyledTyreStrategyStatus>
      </StyledTyreStrategyCard>
    );
  }

  if (errorMessage) {
    return (
      <StyledTyreStrategyCard>
        <StyledTyreStrategyStatus>
          <StyledTyreStrategyStatusText>
            {errorMessage}
          </StyledTyreStrategyStatusText>
        </StyledTyreStrategyStatus>
      </StyledTyreStrategyCard>
    );
  }

  if (!strategy || strategy.drivers.length === 0) {
    return (
      <StyledTyreStrategyCard>
        <StyledTyreStrategyStatus>
          <StyledTyreStrategyStatusText>
            No PostgreSQL tyre stint data is available for this session.
          </StyledTyreStrategyStatusText>
        </StyledTyreStrategyStatus>
      </StyledTyreStrategyCard>
    );
  }

  const totalLaps = Math.max(
    1,
    strategy.totalLaps,
    ...strategy.drivers.flatMap((driver) =>
      driver.stints.map((stint) => stint.endLap),
    ),
  );
  const lapTicks = Array.from({ length: totalLaps }, (_, index) => index + 1);
  const compounds = Array.from(
    new Set(
      strategy.drivers
        .flatMap((driver) => driver.stints)
        .map((stint) => normalizeCompound(stint.compound)),
    ),
  );

  return (
    <StyledTyreStrategyCard>
      <StyledTyreStrategyHeader>
        <StyledTyreStrategyTitle>TYRE STINT STRATEGY</StyledTyreStrategyTitle>
        <StyledTyreStrategySubtitle>
          PostgreSQL analytics view: drivers on the Y axis, laps on the X axis,
          coloured by compound.
        </StyledTyreStrategySubtitle>
      </StyledTyreStrategyHeader>

      <StyledTyreStrategyBody>
        <StyledTyreStrategyPlot
          style={{ "--lap-count": totalLaps } as CSSProperties}
        >
          <StyledTyreStrategyAxisSpacer />
          <StyledTyreStrategyAxis>
            {lapTicks.map((lap) => (
              <StyledTyreStrategyAxisTick
                key={lap}
                $highlight={lap === 1 || lap === totalLaps || lap % 5 === 0}
              >
                {lap === 1 || lap === totalLaps || lap % 5 === 0 ? lap : ""}
              </StyledTyreStrategyAxisTick>
            ))}
          </StyledTyreStrategyAxis>

          {strategy.drivers.map((driver) => (
            <Fragment key={driver.driverNumber}>
              <StyledTyreStrategyDriver>
                <StyledTyreStrategyDriverName>
                  {driver.driverName} #{driver.driverNumber}
                </StyledTyreStrategyDriverName>
                <StyledTyreStrategyTeam>
                  {driver.teamName ?? "No team"}
                </StyledTyreStrategyTeam>
              </StyledTyreStrategyDriver>

              <StyledTyreStrategyRow>
                {driver.stints.map((stint) => {
                  const startLap = Math.max(1, stint.startLap);
                  const lapCount = Math.max(
                    1,
                    stint.lapCount || stint.endLap - startLap + 1,
                  );

                  return (
                    <StyledTyreStintBlock
                      key={`${driver.driverNumber}-${stint.stintNumber}`}
                      $compound={normalizeCompound(stint.compound)}
                      $start={startLap}
                      $span={lapCount}
                      title={`${driver.driverName} ${normalizeCompound(stint.compound)} L${startLap}-L${stint.endLap}`}
                    >
                      <StyledTyreStintLabel>
                        {normalizeCompound(stint.compound)} {lapCount}L
                      </StyledTyreStintLabel>
                    </StyledTyreStintBlock>
                  );
                })}
              </StyledTyreStrategyRow>
            </Fragment>
          ))}
        </StyledTyreStrategyPlot>

        <StyledTyreStrategyLegend>
          {compounds.map((compound) => (
            <StyledTyreLegendItem key={compound} $compound={compound}>
              <StyledTyreLegendText>{compound}</StyledTyreLegendText>
            </StyledTyreLegendItem>
          ))}
        </StyledTyreStrategyLegend>
      </StyledTyreStrategyBody>
    </StyledTyreStrategyCard>
  );
}

const normalizeCompound = (compound: string | null | undefined) =>
  compound?.trim().toUpperCase() || "UNKNOWN";

export default TelemetryTyreStrategyChart;
