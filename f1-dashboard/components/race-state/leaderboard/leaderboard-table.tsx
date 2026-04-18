import type { RaceDashboardDriverRow } from "@/features/store/race-state/raceStateTypes";
import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
} from "@mui/material";
import { useRef } from "react";
import { LeaderboardDriverRow } from "./leaderboard-driver-row";
import {
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

export function LeaderboardTable({
  rows,
  isMobile,
  isTablet,
}: LeaderboardTableProps) {
  const prevPositions = useRef<Map<number, number>>(new Map());

  const positionChanges = new Map<number, "gained" | "lost">();
  for (const row of rows) {
    const pos = row.position ?? row.gridPosition ?? row.line;
    const prev = prevPositions.current.get(row.driverNumber);
    if (
      prev !== undefined &&
      pos !== null &&
      pos !== undefined &&
      prev !== pos
    ) {
      positionChanges.set(row.driverNumber, pos < prev ? "gained" : "lost");
    }
  }
  prevPositions.current = new Map(
    rows.map((r) => [
      r.driverNumber,
      r.position ?? r.gridPosition ?? r.line ?? 0,
    ]),
  );

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
              {!isMobile && <TableCell>TEAM</TableCell>}
              <TableCell>GAP</TableCell>
              {!isTablet && <TableCell>INT</TableCell>}
              {!isMobile && <TableCell>TYRE</TableCell>}
              <TableCell>LAST</TableCell>
              {!isTablet && <TableCell>BEST</TableCell>}
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
