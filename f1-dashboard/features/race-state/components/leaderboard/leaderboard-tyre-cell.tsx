import { Chip, Stack, Typography } from "@mui/material";
import type { RaceDashboardDriverRow } from "@/features/race-state/types/race-state";

type LeaderboardTyreCellProps = {
  row: RaceDashboardDriverRow;
};

const resolveTyreColor = (compound: string) => {
  switch (compound.toLowerCase()) {
    case "soft":
    case "c5":
    case "c4":
      return "error" as const;
    case "medium":
    case "c3":
      return "warning" as const;
    case "hard":
    case "c2":
    case "c1":
      return "default" as const;
    case "intermediate":
    case "inter":
      return "success" as const;
    case "wet":
      return "info" as const;
    default:
      return "default" as const;
  }
};

export function LeaderboardTyreCell({ row }: LeaderboardTyreCellProps) {
  const tyreCompound = row.tyreCompound ?? "-";

  return (
    <Stack direction="row" spacing={1} alignItems="center">
      <Chip
        size="small"
        label={tyreCompound}
        color={resolveTyreColor(tyreCompound)}
        variant={row.tyreIsNew ? "filled" : "outlined"}
      />
      {row.currentStintLapCount ? (
        <Typography variant="caption" sx={{ color: "rgba(255,255,255,0.6)" }}>
          {row.currentStintLapCount} laps
        </Typography>
      ) : null}
      {row.pitFlag !== "-" ? (
        <Typography variant="caption" sx={{ color: "rgba(255,255,255,0.6)" }}>
          PIT {row.pitFlag}
        </Typography>
      ) : null}
    </Stack>
  );
}
