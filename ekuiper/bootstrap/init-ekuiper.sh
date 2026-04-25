#!/bin/sh
set -eu

API_URL="${EKUIPER_API_URL:-http://ekuiper:9081}"
STREAM_NAME="${EKUIPER_STREAM_NAME:-canonical_timing_gap_stream}"
RULE_NAME="${EKUIPER_RULE_NAME:-battle_gap_under_1s}"
BOOTSTRAP_DIR="${BOOTSTRAP_DIR:-/bootstrap}"
RULE_FILE="${BOOTSTRAP_DIR}/rule-battle-alert.json"
STREAM_FILE="${BOOTSTRAP_DIR}/stream-timing-gap.json"

post_json() {
  endpoint="$1"
  payload_file="$2"
  expected_code="$3"
  response_file="$(mktemp)"

  http_code="$(curl -sS -o "${response_file}" -w "%{http_code}" \
    -X POST \
    -H "Content-Type: application/json" \
    --data @"${payload_file}" \
    "${API_URL}${endpoint}")"

  if [ "${http_code}" != "${expected_code}" ]; then
    echo "Request to ${endpoint} failed with HTTP ${http_code}." >&2
    cat "${response_file}" >&2
    rm -f "${response_file}"
    exit 1
  fi

  rm -f "${response_file}"
}

wait_for_ekuiper() {
  echo "Waiting for eKuiper REST API at ${API_URL}..."
  attempts=0
  until curl -fsS "${API_URL}/streams" >/dev/null 2>&1; do
    attempts=$((attempts + 1))
    if [ "${attempts}" -ge 60 ]; then
      echo "eKuiper API did not become ready in time." >&2
      exit 1
    fi

    sleep 2
  done
}

ensure_stream() {
  if curl -fsS "${API_URL}/streams" | grep -q "${STREAM_NAME}"; then
    echo "Stream ${STREAM_NAME} already exists."
    return
  fi

  echo "Creating stream ${STREAM_NAME}..."
  post_json "/streams" "${STREAM_FILE}" "201"
}

validate_rule() {
  echo "Validating rule ${RULE_NAME}..."
  post_json "/rules/validate" "${RULE_FILE}" "200"
}

ensure_rule() {
  if curl -fsS "${API_URL}/rules" | grep -q "${RULE_NAME}"; then
    echo "Rule ${RULE_NAME} already exists."
    return
  fi

  echo "Creating rule ${RULE_NAME}..."
  post_json "/rules" "${RULE_FILE}" "201"
}

wait_for_ekuiper
ensure_stream
validate_rule
ensure_rule

echo "eKuiper bootstrap completed."
