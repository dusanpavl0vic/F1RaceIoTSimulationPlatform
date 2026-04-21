import LeaderboardDriverRow from "@/components/race-state/leaderboard/LeaderboardDriverRow/leaderboard-driver-row";
import type { RaceDashboardDriverRow } from "@/features/store/race-state/raceStateTypes";
import {
  Table,
  TableBody,
  TableContainer,
  TableHead,
  TableRow,
} from "@mui/material";
import { useMemo, useRef } from "react";
import {
  StyledHeadCell,
  StyledLastHeadCell,
  StyledPosHeadCell,
  StyledTableCount,
  StyledTableTitle,
  StyledTableTopBar,
  StyledTableWrapper,
  StyledWideHeadCell,
} from "./leaderboard-table.styles";

type LeaderboardTableProps = {
  rows: RaceDashboardDriverRow[];
  isMobile: boolean;
  isTablet: boolean;
};

const resolveRowPosition = (row: RaceDashboardDriverRow) =>
  row.position ?? row.gridPosition ?? row.line;

function LeaderboardTable({
  rows,
  isMobile,
  isTablet,
}: LeaderboardTableProps) {
  const prevPositions = useRef<Map<number, number>>(new Map());
  const positionChanges = useMemo(() => {
    const changes = new Map<number, "gained" | "lost">();

    for (const row of rows) {
      const position = resolveRowPosition(row);
      const previousPosition = prevPositions.current.get(row.driverNumber);

      if (
        previousPosition !== undefined &&
        position !== null &&
        position !== undefined &&
        previousPosition !== position
      ) {
        changes.set(
          row.driverNumber,
          position < previousPosition ? "gained" : "lost",
        );
      }
    }

    prevPositions.current = new Map(
      rows.map((row) => [row.driverNumber, resolveRowPosition(row) ?? 0]),
    );

    return changes;
  }, [rows]);

  return (
    <StyledTableWrapper>
      <StyledTableTopBar>
        <StyledTableTitle>LIVE TIMING</StyledTableTitle>
        <StyledTableCount>{rows.length} DRIVERS</StyledTableCount>
      </StyledTableTopBar>

      <TableContainer>
        <Table size="small">
          <TableHead>
            <TableRow>
              <StyledPosHeadCell>POS</StyledPosHeadCell>
              <StyledWideHeadCell>DRIVER</StyledWideHeadCell>
              {!isMobile && <StyledHeadCell>TEAM</StyledHeadCell>}
              <StyledHeadCell>GAP</StyledHeadCell>
              {!isTablet && <StyledHeadCell>INT</StyledHeadCell>}
              {!isMobile && <StyledHeadCell>TYRE</StyledHeadCell>}
              <StyledHeadCell>LAST</StyledHeadCell>
              {!isTablet && <StyledHeadCell>BEST</StyledHeadCell>}
              <StyledLastHeadCell>STATUS</StyledLastHeadCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {rows.map((row) => (
              <LeaderboardDriverRow
                key={row.driverNumber}
                row={row}
                isMobile={isMobile}
                isTablet={isTablet}
                positionChange={positionChanges.get(row.driverNumber) ?? null}
              />
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </StyledTableWrapper>
  );
}

export default LeaderboardTable;
