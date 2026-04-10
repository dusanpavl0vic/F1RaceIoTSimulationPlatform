import { Paper, Stack, Typography } from "@mui/material";
import type { RaceMapPosition } from "@/features/race-state/types/race-state";

type TrackPositionPanelProps = {
  positions: RaceMapPosition[];
};

export function TrackPositionPanel({ positions }: TrackPositionPanelProps) {
  return (
    <Paper sx={{ p: 2.5, borderRadius: 4 }}>
      <Typography variant="h5" gutterBottom>
        Track positions
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Pocetni pregled top 8 pozicija sa koordinatama iz `mapPositions`.
      </Typography>

      <Stack spacing={1.25}>
        {positions.slice(0, 8).map((position) => (
          <Stack
            key={position.driverNumber}
            direction="row"
            justifyContent="space-between"
            spacing={1}
          >
            <Typography fontWeight={700}>
              P{position.position ?? "-"} {position.driverName}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              x:{position.x} y:{position.y}
            </Typography>
          </Stack>
        ))}
      </Stack>
    </Paper>
  );
}
