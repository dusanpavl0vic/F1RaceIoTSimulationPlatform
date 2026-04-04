#!/bin/sh
set -eu

CAPTURE_TOPIC_FILTER=${CAPTURE_TOPIC_FILTER:-f1/raw/#}
CAPTURE_OUTPUT_DIR=${CAPTURE_OUTPUT_DIR:-/capture}
CAPTURE_HOST=${CAPTURE_HOST:-mosquitto}
CAPTURE_PORT=${CAPTURE_PORT:-1883}

mkdir -p "$CAPTURE_OUTPUT_DIR"

mosquitto_sub -h "$CAPTURE_HOST" -p "$CAPTURE_PORT" -t "$CAPTURE_TOPIC_FILTER" -v | while IFS= read -r line
do
  topic=${line%% *}
  payload=${line#* }
  safe_topic=$(printf '%s' "$topic" | tr '/:' '__')

  printf '%s\n' "$payload" >> "${CAPTURE_OUTPUT_DIR}/${safe_topic}.jsonl"
  printf '%s\n' "$line" >> "${CAPTURE_OUTPUT_DIR}/all-topics.log"
done
