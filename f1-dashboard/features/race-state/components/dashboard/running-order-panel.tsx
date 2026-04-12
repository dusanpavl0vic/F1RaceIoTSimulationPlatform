"use client";

import type { RaceDashboardDriverRow } from "@/features/race-state/types/race-state";
import {
  StyledRunningGap,
  StyledRunningHeader,
  StyledRunningLeft,
  StyledRunningList,
  StyledRunningName,
  StyledRunningPaper,
  StyledRunningPos,
  StyledRunningRight,
  StyledRunningRow,
  StyledRunningStatus,
  StyledRunningTeam,
  StyledRunningTitle,
} from "./running-order-panel.styles";

type RunningOrderPanelProps = {
  rows: RaceDashboardDriverRow[];
};

export function RunningOrderPanel({ rows }: RunningOrderPanelProps) {
  return (
    <StyledRunningPaper>
      <StyledRunningHeader>
        <StyledRunningTitle>RUNNING ORDER</StyledRunningTitle>
      </StyledRunningHeader>

      <StyledRunningList>
        {rows.map((row) => {
          const teamColor = row.teamColor ? `#${row.teamColor.replace(/^#/, "")}` : "#888";
          const pos = row.position ?? row.gridPosition ?? row.line;
          const isDimmed = row.retired || row.didNotStart || row.stopped;

          return (
            <StyledRunningRow key={row.driverNumber} $dimmed={isDimmed} $color={teamColor}>
              <StyledRunningLeft>
                <StyledRunningPos $isLeader={pos === 1}>{pos ?? "—"}</StyledRunningPos>
                <div>
                  <StyledRunningName $color={teamColor}>
                    {row.tla ?? row.driverLabel}
                  </StyledRunningName>
                  <StyledRunningTeam>{row.displayTeamName}</StyledRunningTeam>
                </div>
              </StyledRunningLeft>

              <StyledRunningRight>
                <StyledRunningGap>
                  {row.gapToLeader === "leader" ? "LEAD" : (row.gapToLeader ?? "—")}
                </StyledRunningGap>
                <StyledRunningStatus $status={row.statusLabel}>
                  {row.statusLabel}
                </StyledRunningStatus>
              </StyledRunningRight>
            </StyledRunningRow>
          );
        })}
      </StyledRunningList>
    </StyledRunningPaper>
  );
}
