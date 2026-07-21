import type {
  CompleteSessionRequest,
  EventBatchRequest,
  RawGazeMetadata,
  SessionId,
  StoredSession,
} from "./contracts"
import { RawGazeConsentRequiredError, type ResearchStore } from "./research-store"

type ConsentRow = {
  readonly raw_gaze_consent: number
}

export class CloudflareResearchStore implements ResearchStore {
  constructor(
    private readonly database: D1Database,
    private readonly rawGazeBucket: R2Bucket,
  ) {}

  async createSession(session: StoredSession): Promise<void> {
    await this.database
      .prepare(
        `INSERT OR IGNORE INTO study_sessions (
          session_id, participant_code, scenario_id, started_at_utc, device_model,
          build_version, raw_gaze_consent, status, idempotency_key
        ) VALUES (?, ?, ?, ?, ?, ?, ?, 'active', ?)`,
      )
      .bind(
        session.sessionId,
        session.participantCode,
        session.scenarioId,
        session.startedAtUtc,
        session.deviceModel,
        session.buildVersion,
        session.rawGazeConsent ? 1 : 0,
        session.idempotencyKey,
      )
      .run()
  }

  async appendEvents(sessionId: SessionId, batch: EventBatchRequest): Promise<number> {
    const statements = batch.events.map((event) =>
      this.database
        .prepare(
          `INSERT OR IGNORE INTO training_events (
            event_id, session_id, sequence_number, timestamp_utc, scenario_id,
            beat_index, event_kind, event_json, request_id
          ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)`,
        )
        .bind(
          event.eventId,
          sessionId,
          event.sequence,
          event.timestampUtc,
          event.scenarioId,
          event.beatIndex,
          event.kind,
          JSON.stringify(event),
          batch.requestId,
        ),
    )
    await this.database.batch(statements)
    return statements.length
  }

  async putRawGaze(metadata: RawGazeMetadata, body: ArrayBuffer): Promise<string> {
    const consent = await this.database
      .prepare("SELECT raw_gaze_consent FROM study_sessions WHERE session_id = ?")
      .bind(metadata.sessionId)
      .first<ConsentRow>()
    if (consent?.raw_gaze_consent !== 1) {
      throw new RawGazeConsentRequiredError(metadata.sessionId)
    }

    const extension = metadata.contentType === "application/gzip" ? "ndjson.gz" : "ndjson"
    const objectKey =
      "raw-gaze/" +
      metadata.sessionId +
      "/" +
      encodeURIComponent(metadata.idempotencyKey) +
      "." +
      extension
    await this.rawGazeBucket.put(objectKey, body, {
      httpMetadata: { contentType: metadata.contentType },
      customMetadata: {
        sessionId: metadata.sessionId,
        sha256: metadata.sha256,
        sampleCount: String(metadata.sampleCount),
      },
    })
    await this.database
      .prepare(
        `INSERT OR IGNORE INTO raw_gaze_objects (
          session_id, object_key, sha256, sample_count, byte_length,
          started_at_utc, ended_at_utc, content_type, idempotency_key
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)`,
      )
      .bind(
        metadata.sessionId,
        objectKey,
        metadata.sha256,
        metadata.sampleCount,
        body.byteLength,
        metadata.startedAtUtc,
        metadata.endedAtUtc,
        metadata.contentType,
        metadata.idempotencyKey,
      )
      .run()
    return objectKey
  }

  async completeSession(sessionId: SessionId, completion: CompleteSessionRequest): Promise<void> {
    await this.database
      .prepare(
        `UPDATE study_sessions
         SET completed_at_utc = ?, status = ?, report_json = ?
         WHERE session_id = ?`,
      )
      .bind(
        completion.completedAtUtc,
        completion.status,
        JSON.stringify(completion.report),
        sessionId,
      )
      .run()
  }
}
