# Teacher Training Research Collector

Cloudflare Worker ingestion service for the Unity and Meta Quest teacher-response simulation. The deployed collector is available at `https://teacher-training-collector.jewoong-moon.workers.dev`.

## Storage layout

- D1 `teacher-training-research`: pseudonymous sessions, versioned telemetry events, debrief reports, and searchable metadata for raw gaze objects.
- R2 `teacher-training-eye-raw`: session-scoped NDJSON eye-gaze samples captured at the configured Quest Pro sampling rate.
- Unity local queue: `Application.persistentDataPath/research-upload-queue` remains the source of truth until the Worker acknowledges every upload.

The Quest client never receives Cloudflare account credentials. It calls the Worker with an in-memory bearer token. The Worker accesses D1 and R2 through bindings.

## API

| Route | Purpose |
| --- | --- |
| `POST /v1/auth/quest-session` | Issue a 24-hour token for an install-scoped pseudonymous Quest participant |
| `POST /v1/student-turn` | Generate a schema-validated Korean student turn through OpenRouter |
| `POST /v1/teacher-rubric` | Evaluate the teacher utterance against the six ECD competency dimensions |
| `POST /v1/transcribe` | Transcribe bounded Korean WAV microphone input without retaining audio |
| `POST /v1/speech` | Return WAV student speech using affect-derived prosody |
| `POST /v1/sessions` | Register pseudonymous participant, scenario, device, build, and raw-gaze consent |
| `POST /v1/sessions/:id/events` | Store a versioned batch of training telemetry |
| `PUT /v1/sessions/:id/raw-gaze` | Stream NDJSON gaze samples to R2 and index the object in D1 |
| `POST /v1/sessions/:id/complete` | Store completion status and ECD/debrief report |
| `GET /health` | Public service health check |

Every write requires either the administrator ingest token or a signed Quest session token plus `X-Client-Id: teacher-training-quest`. Session creation and raw uploads also require an `Idempotency-Key`. The token-issuance response uses `Cache-Control: no-store`.

## Development

```powershell
npm install
npm run check
npx wrangler d1 migrations apply teacher-training-research --remote
npx wrangler r2 bucket create teacher-training-eye-raw
npx wrangler secret put RESEARCH_INGEST_TOKEN
npx wrangler secret put RESEARCH_SESSION_SIGNING_SECRET
npx wrangler secret put OPENROUTER_API_KEY

npx wrangler deploy
```

The production D1 database, R2 bucket, Worker deployment, administrator bearer secret, and Quest session signing secret are provisioned. LLM, STT, and TTS routes all use the single `OPENROUTER_API_KEY` secret and fail closed with typed `503` responses until it is configured. Provider failures return `502` plus a bounded, credential-redacted upstream diagnostic. The local prototype token belongs only in gitignored `.dev.vars`. Do not commit environment files, bearer tokens, participant names, or Cloudflare credentials.

## Consent and privacy

Aggregate training telemetry starts automatically when the Quest app opens, and the active session is registered in D1 immediately after token issuance. Raw gaze is recorded and uploaded only when the Unity runtime has a persisted explicit consent choice via `ResearchConsentPreferences.RawGazeConsent=true`. Participant codes are pseudonymous and limited to letters, digits, underscore, and hyphen. Teacher and student utterances remain represented by length/hash fields in telemetry unless a separately approved research protocol adds text retention.
