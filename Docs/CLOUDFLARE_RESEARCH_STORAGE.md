# Cloudflare research storage

## Current implementation

The Meta Quest research pipeline is local-first and cloud-backed:

1. Quest Pro eye-gaze and Quest 2/3/3S head-gaze proxy samples are written as session NDJSON under `Application.persistentDataPath/eye-tracking` only after explicit raw-gaze consent; every record preserves `trackingSource`.
2. Completed sessions and pause/shutdown checkpoints are serialized under `Application.persistentDataPath/research-upload-queue`.
3. `ResearchCloudSyncClient` sends the pseudonymous session, telemetry batch, raw gaze file, and debrief report to the Worker.
4. The local queue manifest is deleted only after all four server operations return a successful status.
5. D1 stores searchable research records; R2 stores the raw gaze object; D1 retains its object key, SHA-256, sample count, byte size, and time range.

## Provisioned Cloudflare resource

- D1 database: `teacher-training-research`
- D1 region: WNAM
- D1 schema: `study_sessions`, `training_events`, `raw_gaze_objects`
- Worker target: `teacher-training-collector`
- Worker URL: `https://teacher-training-collector.jewoong-moon.workers.dev`
- R2 bucket: `teacher-training-eye-raw`
- Unity endpoint asset: `Assets/Resources/Training/Research/ResearchCloudSyncSettings.asset`

D1, R2, and the Worker are live. On 2026-07-20, a synthetic end-to-end session successfully wrote its session, event, completion report, and raw NDJSON gaze object.

## Automatic Quest authorization

When the app opens, `ResearchAutomaticSessionBootstrap` creates or reuses a random install-scoped ID, derives a stable pseudonymous participant code, requests a 24-hour upload token from `POST /v1/auth/quest-session`, and immediately registers an `active` D1 session when the network is available. The token remains in memory and is never serialized into a scene, Resources asset, PlayerPrefs, repository file, or Quest APK. Pending offline sessions resume automatically after authorization on the next app launch or resume.

Meta account identity, email, username, and device serial number are not collected. Aggregate interaction and eye-attention telemetry is enabled automatically. Raw gaze vectors remain disabled until `ResearchConsentPreferences.RawGazeConsent` has been explicitly set to `true` by the consent interface.

Quest 2/3/3S uploads are labeled `HeadGazeFallback`. These records are head-direction proxies, not eye-tracking observations. D1 debrief aggregates count fallback actions separately, and live-eye validity, dwell, mutual-gaze, and fixation summaries include only `EyeGaze` records.

`ConfigureResearchSession(...)` remains available for coordinator-controlled or externally authenticated sessions. The automatic research token is deliberately not forwarded to the LLM relay. The prototype administrator ingest token and local signing secret are stored only in gitignored `Cloudflare/ResearchCollector/.dev.vars` and as Cloudflare Worker secrets.

## Research exports

D1 supports session/event queries and joins with `raw_gaze_objects`. Raw NDJSON is downloaded from R2 only for approved analysis. Never forward gaze vectors to OpenRouter or another model provider.
