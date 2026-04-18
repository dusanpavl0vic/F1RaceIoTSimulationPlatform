import type { RaceStateUiView } from "@/features/store/race-state/raceStateTypes";
import { Chip } from "@mui/material";
import {
  StyledLiveDot,
  StyledPanelHeader,
  StyledPanelTitle,
  StyledStatLabel,
  StyledStatRow,
  StyledStatsList,
  StyledStatTimeValue,
  StyledTransportPaper,
} from "./live-transport-panel.styles";

type LiveTransportPanelProps = {
  wsUi: RaceStateUiView;
  isFetching: boolean;
};

const resolveWsColor = (status: string) => {
  switch (status) {
    case "open":
      return "success" as const;
    case "connecting":
      return "warning" as const;
    case "error":
      return "error" as const;
    default:
      return "default" as const;
  }
};

const formatTime = (iso: string | null) => {
  if (!iso) return "—";
  return new Date(iso).toLocaleTimeString(undefined, {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hour12: false,
  });
};

export function LiveTransportPanel({
  wsUi,
  isFetching,
}: LiveTransportPanelProps) {
  const isLive = wsUi.wsStatus === "open";

  return (
    <StyledTransportPaper>
      <StyledPanelHeader>
        <StyledPanelTitle>TRANSPORT</StyledPanelTitle>
        <StyledLiveDot $live={isLive} />
      </StyledPanelHeader>

      <StyledStatsList>
        <StyledStatRow>
          <StyledStatLabel>WEBSOCKET</StyledStatLabel>
          <Chip
            size="small"
            label={wsUi.wsStatus.toUpperCase()}
            color={resolveWsColor(wsUi.wsStatus)}
            variant={isLive ? "filled" : "outlined"}
          />
        </StyledStatRow>

        <StyledStatRow>
          <StyledStatLabel>LAST MSG</StyledStatLabel>
          <StyledStatTimeValue>
            {formatTime(wsUi.lastWsMessageReceivedAt)}
          </StyledStatTimeValue>
        </StyledStatRow>
      </StyledStatsList>
    </StyledTransportPaper>
  );
}
