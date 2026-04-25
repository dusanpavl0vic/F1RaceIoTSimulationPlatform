const http = require("http");
const mqtt = require("mqtt");
const config = require("./config");
const { AnalyticsStateStore } = require("./analytics-state-store");
const { PostgresAnalyticsRepository } = require("./postgres-repository");
const { InfluxTelemetryClient } = require("./influx-client");

const relevantEventTypes = new Set([
  "session.info.updated",
  "session.status.updated",
  "track.status.updated",
  "lap.count.updated",
  "driver.list.updated",
  "timing.driver.updated",
  "timing.app.updated",
  "lap.series.updated",
  "tyres.current.updated",
  "tyres.stint.updated",
  "car.telemetry.updated"
]);

const status = {
  startedAt: new Date().toISOString(),
  mqttConnected: false,
  mqttSubscribed: false,
  lastMessageAt: null,
  lastProcessedEventAt: null,
  lastProcessedEventType: null,
  processedMessages: 0,
  failedMessages: 0
};

const analyticsStateStore = new AnalyticsStateStore();
const analyticsRepository = new PostgresAnalyticsRepository(config.postgres.connectionString);
const influxTelemetryClient = new InfluxTelemetryClient(config.influx);

let mqttClient = null;
let server = null;
let processingQueue = Promise.resolve();
let shuttingDown = false;

const start = async () => {
  await analyticsRepository.initialize();

  server = createHealthServer();
  server.listen(config.servicePort, () => {
    console.log(`[telemetry-database] Health endpoint listening on port ${config.servicePort}.`);
  });

  if (!config.mqtt.enabled) {
    console.log("[telemetry-database] MQTT ingestion is disabled.");
    return;
  }

  mqttClient = mqtt.connect({
    host: config.mqtt.host,
    port: config.mqtt.port,
    protocol: "mqtt",
    clientId: config.mqtt.clientId,
    keepalive: config.mqtt.keepAliveSeconds,
    reconnectPeriod: 1000,
    clean: true
  });

  mqttClient.on("connect", () => {
    status.mqttConnected = true;
    mqttClient.subscribe(config.mqtt.topics, (error) => {
      if (error) {
        console.error("[telemetry-database] MQTT subscribe failed.", error);
        return;
      }

      status.mqttSubscribed = true;
      console.log(
        `[telemetry-database] Subscribed to ${config.mqtt.topics.length} topic(s) on ${config.mqtt.host}:${config.mqtt.port}: ${config.mqtt.topics.join(", ")}.`
      );
    });
  });

  mqttClient.on("reconnect", () => {
    status.mqttConnected = false;
    status.mqttSubscribed = false;
  });

  mqttClient.on("close", () => {
    status.mqttConnected = false;
    status.mqttSubscribed = false;
  });

  mqttClient.on("error", (error) => {
    console.error("[telemetry-database] MQTT client error.", error);
  });

  mqttClient.on("message", (_topic, payload) => {
    processingQueue = processingQueue
      .then(() => processMessage(payload))
      .catch((error) => {
        status.failedMessages += 1;
        console.error("[telemetry-database] Canonical event processing failed.", error);
      });
  });
};

const processMessage = async (payload) => {
  status.lastMessageAt = new Date().toISOString();

  const canonicalEvent = JSON.parse(payload.toString("utf-8"));
  if (!relevantEventTypes.has(canonicalEvent?.eventType)) {
    return;
  }

  const outcome = analyticsStateStore.apply(canonicalEvent);
  if (!outcome.applied) {
    return;
  }

  if (outcome.sessionChanged) {
    await analyticsRepository.upsertSession(analyticsStateStore.snapshot());
  }

  if (outcome.driverChanged && outcome.driver && shouldPersistDriverMetadata(canonicalEvent.eventType, outcome)) {
    await analyticsRepository.upsertDriver(canonicalEvent.sessionId, outcome.driver);
  }

  if (outcome.currentStint) {
    await analyticsRepository.upsertStintSummary({
      ...outcome.currentStint,
      sessionId: canonicalEvent.sessionId
    });
  }

  if (outcome.telemetrySample) {
    await influxTelemetryClient.writeTelemetrySample(outcome.telemetrySample);
  }

  status.processedMessages += 1;
  status.lastProcessedEventType = canonicalEvent.eventType;
  status.lastProcessedEventAt = canonicalEvent.eventTime ?? new Date().toISOString();
};

const shouldPersistDriverMetadata = (eventType, outcome) =>
  eventType === "driver.list.updated" || eventType === "timing.app.updated" || Boolean(outcome?.currentStint);

const createHealthServer = () =>
  http.createServer((req, res) => {
    const body =
      req.url === "/health"
        ? {
            status: status.mqttConnected || !config.mqtt.enabled ? "ok" : "degraded",
            ...status
          }
        : {
            service: "telemetry-database-service",
            mqtt: config.mqtt.enabled,
            health: "/health"
          };

    res.writeHead(200, { "content-type": "application/json; charset=utf-8" });
    res.end(JSON.stringify(body));
  });

const shutdown = async () => {
  if (shuttingDown) {
    return;
  }

  shuttingDown = true;

  if (mqttClient) {
    await new Promise((resolve) => mqttClient.end(true, {}, resolve));
  }

  try {
    await processingQueue;
  } catch (_error) {
  }

  await analyticsRepository.close();

  if (server) {
    await new Promise((resolve, reject) => {
      server.close((error) => {
        if (error) {
          reject(error);
          return;
        }

        resolve();
      });
    });
  }
};

process.on("SIGINT", async () => {
  await shutdown();
  process.exit(0);
});

process.on("SIGTERM", async () => {
  await shutdown();
  process.exit(0);
});

start().catch(async (error) => {
  console.error("[telemetry-database] Service failed to start.", error);
  await shutdown();
  process.exit(1);
});
