PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS study_sessions (
  session_id TEXT PRIMARY KEY,
  participant_code TEXT NOT NULL,
  scenario_id TEXT NOT NULL,
  started_at_utc TEXT NOT NULL,
  completed_at_utc TEXT,
  device_model TEXT NOT NULL,
  build_version TEXT NOT NULL,
  raw_gaze_consent INTEGER NOT NULL CHECK (raw_gaze_consent IN (0, 1)),
  status TEXT NOT NULL CHECK (status IN ('active', 'completed', 'aborted')),
  idempotency_key TEXT NOT NULL UNIQUE,
  report_json TEXT
);

CREATE TABLE IF NOT EXISTS training_events (
  event_id TEXT PRIMARY KEY,
  session_id TEXT NOT NULL REFERENCES study_sessions(session_id) ON DELETE CASCADE,
  sequence_number INTEGER NOT NULL,
  timestamp_utc TEXT NOT NULL,
  scenario_id TEXT NOT NULL,
  beat_index INTEGER NOT NULL,
  event_kind INTEGER NOT NULL,
  event_json TEXT NOT NULL,
  request_id TEXT NOT NULL,
  UNIQUE (session_id, sequence_number)
);

CREATE TABLE IF NOT EXISTS raw_gaze_objects (
  raw_gaze_object_id INTEGER PRIMARY KEY AUTOINCREMENT,
  session_id TEXT NOT NULL REFERENCES study_sessions(session_id) ON DELETE CASCADE,
  object_key TEXT NOT NULL UNIQUE,
  sha256 TEXT NOT NULL,
  sample_count INTEGER NOT NULL CHECK (sample_count > 0),
  byte_length INTEGER NOT NULL CHECK (byte_length > 0),
  started_at_utc TEXT NOT NULL,
  ended_at_utc TEXT NOT NULL,
  content_type TEXT NOT NULL,
  idempotency_key TEXT NOT NULL,
  created_at_utc TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
  UNIQUE (session_id, idempotency_key)
);

CREATE INDEX IF NOT EXISTS idx_events_session_sequence
  ON training_events(session_id, sequence_number);
CREATE INDEX IF NOT EXISTS idx_events_scenario_kind
  ON training_events(scenario_id, event_kind);
CREATE INDEX IF NOT EXISTS idx_raw_gaze_session
  ON raw_gaze_objects(session_id);
