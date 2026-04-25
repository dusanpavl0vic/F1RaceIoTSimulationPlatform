class AnalyticsStateStore {
  constructor() {
    this.session = {
      sessionId: "",
      meetingName: null,
      sessionName: null,
      sessionStatus: null,
      trackStatusCode: null,
      trackStatusLabel: null,
      currentLap: null,
      totalLaps: null,
      updatedAt: null,
      drivers: new Map()
    };
    this.lastAppliedEventVersions = new Map();
  }

  snapshot() {
    return {
      sessionId: this.session.sessionId,
      meetingName: this.session.meetingName,
      sessionName: this.session.sessionName,
      sessionStatus: this.session.sessionStatus,
      trackStatusCode: this.session.trackStatusCode,
      trackStatusLabel: this.session.trackStatusLabel,
      currentLap: this.session.currentLap,
      totalLaps: this.session.totalLaps,
      updatedAt: this.session.updatedAt
    };
  }

  apply(canonicalEvent) {
    const eventTime = parseDate(canonicalEvent.eventTime);
    this.session.sessionId = canonicalEvent.sessionId;
    this.session.updatedAt = eventTime;
    const stateKey = resolveStateKey(canonicalEvent);

    if (stateKey && isLateEventForStateKey(this.lastAppliedEventVersions, stateKey, eventTime, canonicalEvent.sequence)) {
      return emptyOutcome();
    }

    const result = (() => {
      switch (canonicalEvent.eventType) {
      case "session.info.updated":
        return this.applySessionInfo(canonicalEvent, eventTime);
      case "session.status.updated":
        return this.applySessionStatus(canonicalEvent, eventTime);
      case "track.status.updated":
        return this.applyTrackStatus(canonicalEvent, eventTime);
      case "lap.count.updated":
        return this.applyLapCount(canonicalEvent, eventTime);
      case "driver.list.updated":
        return this.applyDriverMetadata(canonicalEvent, eventTime);
      case "timing.driver.updated":
        return this.applyTiming(canonicalEvent, eventTime);
      case "timing.app.updated":
        return this.applyTimingApp(canonicalEvent, eventTime);
      case "lap.series.updated":
        return this.applyLapSeries(canonicalEvent, eventTime);
      case "tyres.current.updated":
        return this.applyCurrentTyres(canonicalEvent, eventTime);
      case "tyres.stint.updated":
        return this.applyTyreStints(canonicalEvent, eventTime);
      case "car.telemetry.updated":
        return this.applyTelemetry(canonicalEvent, eventTime);
      default:
        return emptyOutcome();
      }
    })();

    if (stateKey && result.applied) {
      this.lastAppliedEventVersions.set(stateKey, {
        eventTime,
        sequence: canonicalEvent.sequence
      });
    }

    return result;
  }

  applySessionInfo(canonicalEvent) {
    const data = canonicalEvent.payload?.data;
    this.session.meetingName = firstNonEmpty(
      resolveText(data, "Meeting", "Name"),
      resolveText(data, "MeetingName"),
      this.session.meetingName
    );
    this.session.sessionName = firstNonEmpty(
      resolveText(data, "Name"),
      resolveText(data, "Session", "Name"),
      resolveText(data, "OfficialName"),
      this.session.sessionName
    );

    return outcome({ applied: true, sessionChanged: true });
  }

  applySessionStatus(canonicalEvent) {
    const data = canonicalEvent.payload?.data;
    this.session.sessionStatus = firstNonEmpty(
      asString(data?.Status),
      asString(data?.status),
      this.session.sessionStatus
    );

    return outcome({ applied: true, sessionChanged: true });
  }

  applyTrackStatus(canonicalEvent) {
    const data = canonicalEvent.payload?.data;
    this.session.trackStatusCode = firstNonEmpty(asString(data?.Status), this.session.trackStatusCode);
    this.session.trackStatusLabel = firstNonEmpty(
      resolveTrackStatusLabel(this.session.trackStatusCode, asString(data?.Message)),
      this.session.trackStatusLabel
    );

    return outcome({ applied: true, sessionChanged: true });
  }

  applyLapCount(canonicalEvent) {
    const data = canonicalEvent.payload?.data;
    this.session.currentLap = firstNonNull(tryParseInt(data?.CurrentLap), this.session.currentLap);
    const incomingTotalLaps = tryParseInt(data?.TotalLaps);
    this.session.totalLaps = firstNonNull(this.session.totalLaps, incomingTotalLaps) ?? incomingTotalLaps;

    return outcome({ applied: true, sessionChanged: true });
  }

  applyDriverMetadata(canonicalEvent, eventTime) {
    if (!Number.isInteger(canonicalEvent.driverNumber)) {
      return emptyOutcome();
    }

    const driver = this.getOrCreateDriver(canonicalEvent.driverNumber);
    const payload = canonicalEvent.payload?.driver;
    driver.tla = firstNonEmpty(asString(payload?.Tla), driver.tla);
    driver.broadcastName = firstNonEmpty(asString(payload?.BroadcastName), driver.broadcastName);
    driver.fullName = firstNonEmpty(
      asString(payload?.FullName),
      buildFullName(asString(payload?.FirstName), asString(payload?.LastName)),
      driver.fullName
    );
    driver.teamName = firstNonEmpty(asString(payload?.TeamName), driver.teamName);
    driver.teamColor = firstNonEmpty(normalizeColor(asString(payload?.TeamColour)), driver.teamColor);
    driver.lastUpdateTimestamp = eventTime;

    return outcome({ applied: true, driverChanged: true, driver });
  }

  applyTiming(canonicalEvent, eventTime) {
    if (!Number.isInteger(canonicalEvent.driverNumber)) {
      return emptyOutcome();
    }

    const driver = this.getOrCreateDriver(canonicalEvent.driverNumber);
    const timing = canonicalEvent.payload?.timing;
    const previousTimingCompletedLaps = driver.timingCompletedLaps ?? 0;
    const incomingCompletedLaps = tryParseInt(timing?.NumberOfLaps);

    driver.lastUpdateTimestamp = eventTime;

    if (Number.isInteger(incomingCompletedLaps)) {
      driver.timingCompletedLaps = Math.max(previousTimingCompletedLaps, incomingCompletedLaps);
      driver.completedLaps = Math.max(driver.completedLaps ?? 0, incomingCompletedLaps);
      driver.activeLapNumber = Math.max(resolveActiveLapNumber(driver), incomingCompletedLaps + 1);
    }

    return outcome({ applied: true, driverChanged: true, driver });
  }

  applyTimingApp(canonicalEvent, eventTime) {
    if (!Number.isInteger(canonicalEvent.driverNumber)) {
      return emptyOutcome();
    }

    const driver = this.getOrCreateDriver(canonicalEvent.driverNumber);
    const payload = canonicalEvent.payload?.timingApp;
    driver.gridPosition = firstNonNull(tryParseInt(payload?.GridPos), driver.gridPosition);
    driver.lastUpdateTimestamp = eventTime;

    const stintSummary = payload?.Stints ? applyStints(driver, payload.Stints, eventTime) : null;

    return outcome({
      applied: true,
      driverChanged: true,
      stintChanged: Boolean(stintSummary),
      driver,
      currentStint: stintSummary
    });
  }

  applyLapSeries(canonicalEvent, eventTime) {
    if (!Number.isInteger(canonicalEvent.driverNumber)) {
      return emptyOutcome();
    }

    const driver = this.getOrCreateDriver(canonicalEvent.driverNumber);
    const resolved = resolveLapSeriesPosition(canonicalEvent.payload?.lapSeries?.LapPosition);
    if (Number.isInteger(resolved.lapNumber)) {
      driver.completedLaps = Math.max(driver.completedLaps ?? 0, resolved.lapNumber);
      driver.activeLapNumber = Math.max(resolveActiveLapNumber(driver), resolved.lapNumber + 1);
    }

    driver.lastUpdateTimestamp = eventTime;

    return outcome({ applied: true, driverChanged: true, driver });
  }

  applyCurrentTyres(canonicalEvent, eventTime) {
    if (!Number.isInteger(canonicalEvent.driverNumber)) {
      return emptyOutcome();
    }

    const driver = this.getOrCreateDriver(canonicalEvent.driverNumber);
    const tyres = canonicalEvent.payload?.tyres;
    driver.currentCompound = firstNonEmpty(asString(tyres?.Compound), driver.currentCompound);
    driver.tyreIsNew = tryParseBool(tyres?.New) ?? driver.tyreIsNew;
    driver.lastUpdateTimestamp = eventTime;

    return outcome({ applied: true, driverChanged: true, driver });
  }

  applyTyreStints(canonicalEvent, eventTime) {
    if (!Number.isInteger(canonicalEvent.driverNumber) || !isObject(canonicalEvent.payload?.stints)) {
      return emptyOutcome();
    }

    const driver = this.getOrCreateDriver(canonicalEvent.driverNumber);
    driver.lastUpdateTimestamp = eventTime;
    const stintSummary = applyStints(driver, canonicalEvent.payload.stints, eventTime);

    return outcome({
      applied: true,
      driverChanged: true,
      stintChanged: Boolean(stintSummary),
      driver,
      currentStint: stintSummary
    });
  }

  applyTelemetry(canonicalEvent, eventTime) {
    if (!Number.isInteger(canonicalEvent.driverNumber)) {
      return emptyOutcome();
    }

    const driver = this.getOrCreateDriver(canonicalEvent.driverNumber);
    const telemetry = canonicalEvent.payload?.telemetry;
    const lapNumber = resolveActiveLapNumber(driver);
    const sampleIndex = (driver.nextTelemetrySampleIndexByLap.get(lapNumber) ?? 0) + 1;
    driver.nextTelemetrySampleIndexByLap.set(lapNumber, sampleIndex);

    const speed = tryParseInt(telemetry?.speed);
    const rpm = tryParseInt(telemetry?.rpm);
    const gear = tryParseInt(telemetry?.gear);
    const throttlePct = tryParseInt(telemetry?.throttlePct);
    const rawThrottle = tryParseInt(telemetry?.rawThrottle);
    const brakeApplied = tryParseBool(telemetry?.brakeApplied);
    const rawBrake = tryParseInt(telemetry?.rawBrake);
    const drsEnabled = tryParseBool(telemetry?.drsEnabled);

    driver.lastUpdateTimestamp = eventTime;

    const tyreContext = resolveLapTyreContext(driver, lapNumber);

    return outcome({
      applied: true,
      telemetrySample: {
        sessionId: canonicalEvent.sessionId,
        driverNumber: canonicalEvent.driverNumber,
        lapNumber,
        stintNumber: tyreContext?.stintNumber ?? Math.max(1, driver.stintNumber),
        sampleIndex,
        timestamp: eventTime,
        speed,
        rpm,
        gear,
        throttlePct,
        rawThrottle,
        brakeApplied,
        rawBrake,
        drsEnabled
      }
    });
  }

  getOrCreateDriver(driverNumber) {
    let driver = this.session.drivers.get(driverNumber);
    if (!driver) {
      driver = createDriverState(driverNumber);
      this.session.drivers.set(driverNumber, driver);
    }

    return driver;
  }
}

const createDriverState = (driverNumber) => ({
  driverNumber,
  tla: null,
  broadcastName: null,
  fullName: null,
  teamName: null,
  teamColor: null,
  gridPosition: null,
  completedLaps: null,
  currentCompound: null,
  tyreIsNew: null,
  stintNumber: 0,
  currentStintStartLap: null,
  stintHistory: new Map(),
  timingCompletedLaps: null,
  lastUpdateTimestamp: null,
  activeLapNumber: 1,
  nextTelemetrySampleIndexByLap: new Map()
});

const emptyOutcome = () => outcome({});

const outcome = ({
  applied = false,
  sessionChanged = false,
  driverChanged = false,
  stintChanged = false,
  driver = null,
  currentStint = null,
  telemetrySample = null
}) => ({
  applied,
  sessionChanged,
  driverChanged,
  stintChanged,
  driver,
  currentStint,
  telemetrySample
});

const applyStints = (driver, stints, updatedAt) => {
  const current = Object.entries(stints)
    .map(([index, data]) => ({ index: tryParseInt(index), data: isObject(data) ? data : null }))
    .filter((item) => Number.isInteger(item.index) && item.data)
    .sort((left, right) => right.index - left.index)[0];

  if (!current) {
    return null;
  }

  const stintNumber = current.index + 1;
  const previousStint = stintNumber > 1 ? driver.stintHistory.get(stintNumber - 1) ?? null : null;
  const existingStint = driver.stintHistory.get(stintNumber) ?? null;
  const startLaps = tryParseInt(current.data.StartLaps);
  const totalLaps = tryParseInt(current.data.TotalLaps);
  const initialTyreLaps = firstNonNull(existingStint?.initialTyreLaps, startLaps);
  const normalizedStartLap = resolveStintStartLap(
    stintNumber,
    startLaps,
    existingStint,
    previousStint,
    driver
  );
  const normalizedLapCount = resolveRaceLapCount(totalLaps, initialTyreLaps, existingStint);
  const normalizedCompound = firstNonEmpty(
    asString(current.data.Compound),
    existingStint?.compound,
    driver.currentCompound
  );
  const normalizedTyreIsNew =
    tryParseBool(current.data.New) ?? existingStint?.tyreIsNew ?? driver.tyreIsNew ?? null;
  const normalizedEndLap =
    Number.isInteger(normalizedStartLap) && Number.isInteger(normalizedLapCount) && normalizedLapCount > 0
      ? normalizedStartLap + normalizedLapCount - 1
      : null;

  driver.stintNumber = stintNumber;
  driver.currentCompound = normalizedCompound;
  driver.tyreIsNew = normalizedTyreIsNew;
  driver.currentStintStartLap = firstNonNull(normalizedStartLap, driver.currentStintStartLap);

  driver.stintHistory.set(stintNumber, {
    stintNumber,
    compound: normalizedCompound,
    tyreIsNew: normalizedTyreIsNew,
    startLap: normalizedStartLap,
    lapCount: normalizedLapCount,
    endLap: normalizedEndLap,
    initialTyreLaps
  });

  return {
    sessionId: "",
    driverNumber: driver.driverNumber,
    stintNumber,
    compound: normalizedCompound,
    tyreIsNew: normalizedTyreIsNew,
    startLap: normalizedStartLap,
    endLap: normalizedEndLap,
    lapCount: normalizedLapCount,
    updatedAt
  };
};

const resolveStintStartLap = (stintNumber, rawStartLap, existingStint, previousStint, driver) => {
  if (stintNumber === 1) {
    return 1;
  }

  if (Number.isInteger(existingStint?.startLap) && existingStint.startLap > 0) {
    return existingStint.startLap;
  }

  if (Number.isInteger(rawStartLap) && rawStartLap > 0) {
    return rawStartLap;
  }

  if (previousStint) {
    if (
      Number.isInteger(previousStint.startLap) &&
      previousStint.startLap > 0 &&
      Number.isInteger(previousStint.lapCount) &&
      previousStint.lapCount >= 0
    ) {
      return previousStint.startLap + Math.max(1, previousStint.lapCount);
    }

    if (Number.isInteger(previousStint.endLap) && previousStint.endLap > 0) {
      return previousStint.endLap + 1;
    }
  }

  if (Number.isInteger(driver.completedLaps) && driver.completedLaps >= 0) {
    return driver.completedLaps + 1;
  }

  return null;
};

const resolveRaceLapCount = (rawTotalLaps, initialTyreLaps, existingStint) => {
  if (!Number.isInteger(rawTotalLaps)) {
    return existingStint?.lapCount ?? null;
  }

  if (!Number.isInteger(initialTyreLaps)) {
    return rawTotalLaps;
  }

  return Math.max(0, rawTotalLaps - initialTyreLaps);
};

const resolveActiveLapNumber = (driver) => Math.max(1, driver.activeLapNumber ?? (driver.completedLaps ?? 0) + 1);

const resolveLapTyreContext = (driver, lapNumber) => {
  const stints = [...driver.stintHistory.values()]
    .filter((stint) => Number.isInteger(stint.stintNumber))
    .sort((left, right) => left.stintNumber - right.stintNumber);

  const exactMatch = stints.find((stint) => {
    if (!Number.isInteger(stint.startLap) || stint.startLap <= 0) {
      return false;
    }

    if (Number.isInteger(stint.endLap) && stint.endLap > 0) {
      return lapNumber >= stint.startLap && lapNumber <= stint.endLap;
    }

    if (Number.isInteger(stint.lapCount) && stint.lapCount > 0) {
      return lapNumber >= stint.startLap && lapNumber <= stint.startLap + stint.lapCount - 1;
    }

    return lapNumber >= stint.startLap;
  });

  const currentMatch =
    exactMatch ??
    (Number.isInteger(driver.currentStintStartLap) && lapNumber >= driver.currentStintStartLap
      ? driver.stintHistory.get(driver.stintNumber) ?? null
      : null);

  if (!currentMatch) {
    return null;
  }

  const tyreLaps =
    Number.isInteger(currentMatch.startLap) && currentMatch.startLap > 0
      ? Math.max(0, lapNumber - currentMatch.startLap + 1)
      : currentMatch.lapCount ?? null;

  return {
    stintNumber: currentMatch.stintNumber,
    compound: currentMatch.compound,
    tyreIsNew: currentMatch.tyreIsNew,
    tyreLaps
  };
};

const resolveText = (node, ...path) => {
  let current = node;
  for (const segment of path) {
    if (current === null || current === undefined) {
      return null;
    }

    current = current[segment];
  }

  return asString(current);
};

const resolveTrackStatusLabel = (code, message) => {
  switch (code) {
    case "1":
      return "GREEN";
    case "2":
      return "YELLOW";
    case "4":
      return "SAFETY_CAR";
    case "5":
      return "RED_FLAG";
    case "6":
      return "VIRTUAL_SAFETY_CAR";
    case "7":
      return "VSC_ENDING";
    default:
      return firstNonEmpty(message, "UNKNOWN");
  }
};

const resolveLapSeriesPosition = (lapPositionNode) => {
  if (isObject(lapPositionNode)) {
    const latest = Object.entries(lapPositionNode)
      .map(([lapNumber, position]) => ({
        lapNumber: tryParseInt(lapNumber),
        position: tryParseInt(position)
      }))
      .filter((item) => Number.isInteger(item.lapNumber) && Number.isInteger(item.position))
      .sort((left, right) => right.lapNumber - left.lapNumber)[0];

    return latest || { lapNumber: null, position: null };
  }

  if (Array.isArray(lapPositionNode)) {
    const values = lapPositionNode.map((value) => tryParseInt(value)).filter(Number.isInteger);
    return {
      lapNumber: null,
      position: values.length > 0 ? values[values.length - 1] : null
    };
  }

  return { lapNumber: null, position: null };
};

const tryParseInt = (value) => {
  if (value === null || value === undefined || value === "") {
    return null;
  }

  const parsed = Number.parseInt(String(value), 10);
  return Number.isFinite(parsed) ? parsed : null;
};

const tryParseBool = (value) => {
  if (value === null || value === undefined || value === "") {
    return null;
  }

  const normalized = String(value).trim().toLowerCase();
  if (normalized === "true") {
    return true;
  }

  if (normalized === "false") {
    return false;
  }

  return null;
};

const buildFullName = (firstName, lastName) => {
  const fullName = [firstName, lastName].filter((value) => value && value.trim()).join(" ");
  return fullName || null;
};

const normalizeColor = (rawColor) => {
  if (!rawColor || !rawColor.trim()) {
    return null;
  }

  const value = rawColor.trim();
  return value.startsWith("#") ? value : `#${value}`;
};

const firstNonEmpty = (...values) => values.find((value) => value !== null && value !== undefined && String(value).trim() !== "") ?? null;
const firstNonNull = (left, right) => (left ?? right);
const isObject = (value) => value !== null && typeof value === "object" && !Array.isArray(value);
const asString = (value) => {
  if (value === null || value === undefined) {
    return null;
  }

  return typeof value === "object" ? JSON.stringify(value) : String(value);
};
const parseDate = (value) => new Date(value);

const resolveStateKey = (canonicalEvent) => {
  if (!canonicalEvent?.eventType) {
    return null;
  }

  switch (canonicalEvent.eventType) {
    case "session.info.updated":
    case "session.status.updated":
    case "track.status.updated":
    case "lap.count.updated":
      return canonicalEvent.eventType;
    case "driver.list.updated":
    case "timing.driver.updated":
    case "timing.app.updated":
    case "lap.series.updated":
    case "tyres.current.updated":
    case "tyres.stint.updated":
    case "car.telemetry.updated":
      return Number.isInteger(canonicalEvent.driverNumber)
        ? `${canonicalEvent.eventType}:${canonicalEvent.driverNumber}`
        : canonicalEvent.eventType;
    default:
      return null;
  }
};

const isLateEventForStateKey = (versions, stateKey, eventTime, sequence) => {
  const current = versions.get(stateKey);
  if (!current) {
    return false;
  }

  const incomingTime = eventTime?.getTime?.() ?? Number.NaN;
  const currentTime = current.eventTime?.getTime?.() ?? Number.NaN;

  if (Number.isFinite(incomingTime) && Number.isFinite(currentTime) && incomingTime !== currentTime) {
    return incomingTime < currentTime;
  }

  return Number(sequence ?? 0) <= Number(current.sequence ?? 0);
};

module.exports = {
  AnalyticsStateStore
};
