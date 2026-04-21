import LeaderboardDriverRow from "@/components/race-state/leaderboard/LeaderboardDriverRow/leaderboard-driver-row";
import type { RaceDashboardDriverRow } from "@/features/store/race-state/raceStateTypes";
import {
  Table,
  TableBody,
  TableContainer,
  TableHead,
  TableRow,
} from "@mui/material";
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

function LeaderboardTable({
  rows,
  isMobile,
  isTablet,
}: LeaderboardTableProps) {
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
              />
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    </StyledTableWrapper>
  );
}

export default LeaderboardTable;
