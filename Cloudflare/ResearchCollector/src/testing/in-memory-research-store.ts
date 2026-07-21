import type {
  CompleteSessionRequest,
  EventBatchRequest,
  RawGazeMetadata,
  SessionId,
  StoredRawGazeObject,
  StoredSession,
} from "../contracts"
import { RawGazeConsentRequiredError, type ResearchStore } from "../research-store"

export class InMemoryResearchStore implements ResearchStore {
  readonly sessions: StoredSession[] = []
  readonly eventBatches: EventBatchRequest[] = []
  readonly rawObjects: StoredRawGazeObject[] = []
  readonly objectBodies = new Map<string, string>()
  readonly completions = new Map<SessionId, CompleteSessionRequest>()

  async createSession(session: StoredSession): Promise<void> {
    if (!this.sessions.some((item) => item.sessionId === session.sessionId)) {
      this.sessions.push(session)
    }
  }

  async appendEvents(_sessionId: SessionId, batch: EventBatchRequest): Promise<number> {
    this.eventBatches.push(batch)
    return batch.events.length
  }

  async putRawGaze(metadata: RawGazeMetadata, body: ArrayBuffer): Promise<string> {
    const session = this.sessions.find((item) => item.sessionId === metadata.sessionId)
    if (session?.rawGazeConsent !== true) {
      throw new RawGazeConsentRequiredError(metadata.sessionId)
    }

    const extension = metadata.contentType === "application/gzip" ? "ndjson.gz" : "ndjson"
    const objectKey = `raw-gaze/${metadata.sessionId}/${metadata.idempotencyKey}.${extension}`
    this.rawObjects.push({ ...metadata, objectKey, byteLength: body.byteLength })
    this.objectBodies.set(objectKey, new TextDecoder().decode(body))
    return objectKey
  }

  async completeSession(sessionId: SessionId, completion: CompleteSessionRequest): Promise<void> {
    this.completions.set(sessionId, completion)
  }
}
