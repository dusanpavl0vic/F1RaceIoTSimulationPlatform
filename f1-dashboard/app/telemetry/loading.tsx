import { GlobalBlockingLoader } from "@/components/shared/GlobalBlockingLoader/global-blocking-loader";

export default function TelemetryLoading() {
  return <GlobalBlockingLoader open label="OPENING TELEMETRY..." />;
}
