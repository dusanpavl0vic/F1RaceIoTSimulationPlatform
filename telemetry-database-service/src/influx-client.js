class InfluxTelemetryClient {
  constructor(options) {
    this.options = options;
  }

  async writeTelemetrySample(sample) {
    if (!this.options.enabled) {
      return;
    }

    const line = formatLineProtocol(sample);
    const response = await this.sendWithOptionalAuthRetry((includeAuthorization) => {
      const url = new URL("/api/v2/write", this.options.baseUrl);
      url.searchParams.set("org", this.options.organization);
      url.searchParams.set("bucket", this.options.bucket);
      url.searchParams.set("precision", this.options.writePrecision);

      const headers = {
        "content-type": "text/plain; charset=utf-8"
      };

      if (includeAuthorization) {
        headers.authorization = `Token ${this.options.token}`;
      }

      return {
        url: url.toString(),
        init: {
          method: "POST",
          headers,
          body: line
        }
      };
    });

    if (!response.ok) {
      const body = await response.text();
      console.warn(
        `[telemetry-database] InfluxDB write failed with status ${response.status}. body=${body}`
      );
    }
  }

  async sendWithOptionalAuthRetry(requestFactory) {
    const includeAuthorization = Boolean(this.options.token);
    let response = await this.send(requestFactory(includeAuthorization));

    if (includeAuthorization && (response.status === 401 || response.status === 403)) {
      response = await this.send(requestFactory(false));
    }

    return response;
  }

  async send({ url, init }) {
    return fetch(url, init);
  }
}

const formatLineProtocol = (sample) => {
  const fields = [];

  if (Number.isInteger(sample.speed)) {
    fields.push(`speed=${sample.speed}i`);
  }

  if (Number.isInteger(sample.rpm)) {
    fields.push(`rpm=${sample.rpm}i`);
  }

  if (Number.isInteger(sample.gear)) {
    fields.push(`gear=${sample.gear}i`);
  }

  if (Number.isInteger(sample.throttlePct)) {
    fields.push(`throttle_pct=${sample.throttlePct}i`);
  }

  if (Number.isInteger(sample.rawThrottle)) {
    fields.push(`raw_throttle=${sample.rawThrottle}i`);
  }

  if (Number.isInteger(sample.rawBrake)) {
    fields.push(`brake_pct=${sample.rawBrake}i`);
    fields.push(`raw_brake=${sample.rawBrake}i`);
  } else if (typeof sample.brakeApplied === "boolean") {
    fields.push(`brake_pct=${sample.brakeApplied ? 100 : 0}i`);
  }

  if (typeof sample.drsEnabled === "boolean") {
    fields.push(`drs_enabled=${sample.drsEnabled ? "true" : "false"}`);
  }

  fields.push(`sample_index=${sample.sampleIndex}i`);

  const tags = [
    `session_id=${escapeTag(sample.sessionId)}`,
    `driver_number=${sample.driverNumber}`,
    `lap_number=${sample.lapNumber}`,
    `stint_number=${sample.stintNumber}`
  ].join(",");

  const timestamp = BigInt(sample.timestamp.getTime()) * 1000000n;
  return `telemetry_samples,${tags} ${fields.join(",")} ${timestamp.toString()}`;
};

const escapeTag = (value) =>
  String(value)
    .replaceAll("\\", "\\\\")
    .replaceAll(" ", "\\ ")
    .replaceAll(",", "\\,")
    .replaceAll("=", "\\=");

module.exports = {
  InfluxTelemetryClient
};
