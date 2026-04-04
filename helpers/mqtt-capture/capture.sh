#!/bin/sh
set -eu

mkdir -p /capture

mosquitto_sub -h mosquitto -p 1883 -t 'f1/raw/#' -v | while IFS= read -r line
do
  topic=${line%% *}
  payload=${line#* }
  safe_topic=$(printf '%s' "$topic" | tr '/:' '__')

  printf '%s\n' "$payload" >> "/capture/${safe_topic}.jsonl"
  printf '%s\n' "$line" >> /capture/all-topics.log
done
