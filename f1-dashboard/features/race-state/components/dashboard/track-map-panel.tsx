"use client";

import { Paper, Stack, Typography } from "@mui/material";
import type {
  RaceDashboardDriverRow,
  RaceMapPosition,
} from "@/features/race-state/types/race-state";

type TrackMapPanelProps = {
  rows: RaceDashboardDriverRow[];
  positions: RaceMapPosition[];
};

type PlotPoint = {
  driverNumber: number;
  label: string;
  teamColor: string;
  position: number | null;
  x: number;
  y: number;
  isEstimated: boolean;
};

const VIEWBOX_SIZE = 100;
const PADDING = 8;

const buildPlotPoints = (
  rows: RaceDashboardDriverRow[],
  positions: RaceMapPosition[]
): PlotPoint[] => {
  const rowByDriverNumber = new Map(rows.map((row) => [row.driverNumber, row]));
  const validPositions = positions.filter(
    (position) => Number.isFinite(position.x) && Number.isFinite(position.y)
  );

  if (validPositions.length === 0) {
    return [];
  }

  const minX = Math.min(...validPositions.map((position) => position.x));
  const maxX = Math.max(...validPositions.map((position) => position.x));
  const minY = Math.min(...validPositions.map((position) => position.y));
  const maxY = Math.max(...validPositions.map((position) => position.y));

  const xRange = Math.max(maxX - minX, 1);
  const yRange = Math.max(maxY - minY, 1);

  return validPositions.map((position) => {
    const row = rowByDriverNumber.get(position.driverNumber);
    const normalizedX =
      PADDING + ((position.x - minX) / xRange) * (VIEWBOX_SIZE - PADDING * 2);
    const normalizedY =
      VIEWBOX_SIZE -
      (PADDING + ((position.y - minY) / yRange) * (VIEWBOX_SIZE - PADDING * 2));

    return {
      driverNumber: position.driverNumber,
      label: row?.driverLabel ?? position.driverName,
      teamColor: row?.teamColor ?? "#f5f5f5",
      position: row?.line ?? row?.position ?? row?.gridPosition ?? position.position,
      x: normalizedX,
      y: normalizedY,
      isEstimated: position.isEstimated,
    };
  });
};

export function TrackMapPanel({ rows, positions }: TrackMapPanelProps) {
  const plotPoints = buildPlotPoints(rows, positions);

  return (
    <Paper
      sx={{
        p: 2.25,
        borderRadius: 3,
        backgroundColor: "#050505",
        border: "1px solid rgba(255,255,255,0.08)",
      }}
    >
      <Typography variant="h6" sx={{ color: "#fff", fontWeight: 900, mb: 0.5 }}>
        Track Map
      </Typography>
      <Typography variant="body2" sx={{ color: "rgba(255,255,255,0.58)", mb: 2 }}>
        Vizuelni raspored bolida na osnovu `Position.z` koordinata.
      </Typography>

      <Stack spacing={2}>
        <svg
          viewBox={`0 0 ${VIEWBOX_SIZE} ${VIEWBOX_SIZE}`}
          width="100%"
          height="320"
          role="img"
          aria-label="Track map positions"
          style={{
            display: "block",
            borderRadius: 18,
            background:
              "radial-gradient(circle at center, rgba(225,6,0,0.08), rgba(255,255,255,0.02) 45%, rgba(255,255,255,0.01) 100%)",
            border: "1px solid rgba(255,255,255,0.08)",
          }}
        >
          {plotPoints.length > 1 ? (
            <polyline
              fill="none"
              stroke="rgba(255,255,255,0.12)"
              strokeWidth="0.8"
              points={plotPoints.map((point) => `${point.x},${point.y}`).join(" ")}
            />
          ) : null}

          {plotPoints.map((point) => (
            <g key={point.driverNumber}>
              <circle
                cx={point.x}
                cy={point.y}
                r="2.8"
                fill={point.teamColor}
                stroke={point.isEstimated ? "rgba(255,255,255,0.7)" : "#050505"}
                strokeWidth="0.7"
              />
              <text
                x={point.x + 3.8}
                y={point.y - 3.2}
                fill={point.teamColor}
                fontSize="4.4"
                fontWeight="700"
              >
                {point.label}
              </text>
            </g>
          ))}
        </svg>

        <Stack spacing={0.8}>
          {plotPoints.slice(0, 8).map((point) => (
            <Stack
              key={point.driverNumber}
              direction="row"
              justifyContent="space-between"
              alignItems="center"
              sx={{
                px: 1.1,
                py: 0.75,
                borderRadius: 2,
                backgroundColor: "rgba(255,255,255,0.03)",
              }}
            >
              <Stack direction="row" spacing={1} alignItems="center">
                <Typography sx={{ color: "#fff", fontWeight: 800, minWidth: 24 }}>
                  {point.position ?? "-"}
                </Typography>
                <Typography sx={{ color: point.teamColor, fontWeight: 800 }}>
                  {point.label}
                </Typography>
              </Stack>
              <Typography sx={{ color: "rgba(255,255,255,0.52)", fontSize: 12 }}>
                {point.isEstimated ? "estimated" : "live"}
              </Typography>
            </Stack>
          ))}
        </Stack>
      </Stack>
    </Paper>
  );
}
