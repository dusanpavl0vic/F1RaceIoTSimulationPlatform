const parseBoolean = (value, defaultValue) => {
  if (value === undefined || value === null || value === "") {
    return defaultValue;
  }

  const normalized = String(value).trim().toLowerCase();
  if (["1", "true", "yes", "on"].includes(normalized)) {
    return true;
  }

  if (["0", "false", "no", "off"].includes(normalized)) {
    return false;
  }

  return defaultValue;
};

const parseInteger = (value, defaultValue) => {
  const parsed = Number.parseInt(String(value ?? ""), 10);
  return Number.isFinite(parsed) ? parsed : defaultValue;
};

const parseCsv = (value) =>
  String(value ?? "")
    .split(",")
    .map((item) => item.trim())
    .filter(Boolean);

const defaultMqttTopics = [
  "f1/canonical/session/info/updated",
  "f1/canonical/session/status/updated",
  "f1/canonical/track/status/updated",
  "f1/canonical/lap/count/updated",
  "f1/canonical/driver/list/updated/+",
  "f1/canonical/timing/driver/updated/+",
  "f1/canonical/timing/app/updated/+",
  "f1/canonical/lap/series/updated/+",
  "f1/canonical/tyres/current/updated/+",
  "f1/canonical/tyres/stint/updated/+",
  "f1/canonical/car/telemetry/updated/+"
];

module.exports = {
  servicePort: parseInteger(process.env.SERVICE_PORT, 8090),
  mqtt: {
    enabled: parseBoolean(process.env.MQTT_ENABLED, true),
    host: process.env.MQTT_HOST || "localhost",
    port: parseInteger(process.env.MQTT_PORT, 1883),
    topics:
      parseCsv(process.env.MQTT_TOPICS).length > 0
        ? parseCsv(process.env.MQTT_TOPICS)
        : parseCsv(process.env.MQTT_TOPIC_FILTER).length > 0
          ? parseCsv(process.env.MQTT_TOPIC_FILTER)
          : defaultMqttTopics,
    clientId: process.env.MQTT_CLIENT_ID || "f1-telemetry-database-service",
    keepAliveSeconds: parseInteger(process.env.MQTT_KEEPALIVE_SECONDS, 30)
  },
  postgres: {
    connectionString:
      process.env.POSTGRES_CONNECTION_STRING ||
      "Host=localhost;Port=5432;Database=f1_telemetry;Username=f1;Password=f1_password"
  },
  influx: {
    enabled: parseBoolean(process.env.INFLUX_ENABLED, true),
    baseUrl: process.env.INFLUX_BASE_URL || "http://localhost:8086",
    organization: process.env.INFLUX_ORGANIZATION || "f1-platform",
    bucket: process.env.INFLUX_BUCKET || "f1-telemetry",
    token: process.env.INFLUX_TOKEN || "f1-super-token",
    writePrecision: process.env.INFLUX_WRITE_PRECISION || "ns"
  }
};
