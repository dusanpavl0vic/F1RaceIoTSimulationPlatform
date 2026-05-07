#!/bin/sh
set -eu

ROOT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)
CANONICAL_DIR="$ROOT_DIR/docker/mqtt-capture/canonical"
REPLAY_DATA_DIR="$ROOT_DIR/docker/replay-data"
REPLAY_BASE_URL=${REPLAY_BASE_URL:-http://localhost:8080}

mkdir -p "$CANONICAL_DIR"
rm -f "$CANONICAL_DIR"/*.jsonl "$CANONICAL_DIR"/all-topics.log

SESSION_CONFIG=$(find "$REPLAY_DATA_DIR" -mindepth 2 -maxdepth 2 -type f -name 'replay-config.generated.json' | sort | head -n 1)

if [ -n "${SESSION_CONFIG:-}" ]; then
  SESSION_DIR=$(dirname "$SESSION_CONFIG")
  if [ -d "$SESSION_DIR/feeds" ]; then
    rm -rf "$REPLAY_DATA_DIR/feeds"
    cp -R "$SESSION_DIR/feeds" "$REPLAY_DATA_DIR/feeds"
    cp "$SESSION_CONFIG" "$REPLAY_DATA_DIR/replay-config.generated.json"
    if [ -f "$SESSION_DIR/Index.json" ]; then
      cp "$SESSION_DIR/Index.json" "$REPLAY_DATA_DIR/Index.json"
    fi
  fi
fi

cd "$ROOT_DIR"

docker compose up -d \
  mosquitto \
  event-normalizer-service \
  canonical-training-data-capture \
  f1-feed-replay-service

echo "Waiting for replay service at $REPLAY_BASE_URL..."
until curl -fsS "$REPLAY_BASE_URL/health" >/dev/null 2>&1
do
  sleep 2
done

echo "Stopping any previous replay run..."
curl -fsS -X POST "$REPLAY_BASE_URL/api/replay/stop" >/dev/null 2>&1 || true

echo "Loading configured replay data..."
curl -fsS -X POST "$REPLAY_BASE_URL/api/replay/load" \
  -H "Content-Type: application/json" \
  -d '{}' >/dev/null

echo "Starting replay..."
curl -fsS -X POST "$REPLAY_BASE_URL/api/replay/start" \
  -H "Content-Type: application/json" \
  -d '{}' >/dev/null

echo "Canonical training capture is active."
echo "Files will be written into: $CANONICAL_DIR"
echo "Watch progress with: ls $CANONICAL_DIR | head"
