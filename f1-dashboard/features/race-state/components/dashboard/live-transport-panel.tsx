import { Paper, Stack, Typography } from "@mui/material";
import type { RaceStateUiView } from "@/features/race-state/types/race-state";

type LiveTransportPanelProps = {
  wsUi: RaceStateUiView;
  isFetching: boolean;
};

export function LiveTransportPanel({
  wsUi,
  isFetching,
}: LiveTransportPanelProps) {
  return (
    <Paper sx={{ p: 2.5, borderRadius: 4 }}>
      <Typography variant="h5" gutterBottom>
        Live transport
      </Typography>
      <Stack spacing={1}>
        <Typography variant="body2" color="text.secondary">
          REST refresh: {isFetching ? "updating" : "idle"}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          WS status: {wsUi.wsStatus}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Last message:{" "}
          {wsUi.lastWsMessageReceivedAt
            ? new Date(wsUi.lastWsMessageReceivedAt).toLocaleTimeString()
            : "-"}
        </Typography>
        <Paper
          variant="outlined"
          sx={{
            p: 1.5,
            borderRadius: 3,
            backgroundColor: "background.default",
          }}
        >
          <Typography
            component="pre"
            variant="caption"
            sx={{ m: 0, whiteSpace: "pre-wrap", wordBreak: "break-word" }}
          >
            {wsUi.lastWsPayloadPreview}
          </Typography>
        </Paper>
      </Stack>
    </Paper>
  );
}
