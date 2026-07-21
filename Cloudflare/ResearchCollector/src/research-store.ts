import type {
  CompleteSessionRequest,
  EventBatchRequest,
  RawGazeMetadata,
  SessionId,
  StoredSession,
} from "./contracts"

export class RawGazeConsentRequiredError extends Error {
  readonly name = "RawGazeConsentRequiredError"

  constructor(readonly sessionId: SessionId) {
    super("Raw gaze upload requires explicit session consent.")
  }
}

export class ResearchStoreConfigurationError extends Error {
  readonly name = "ResearchStoreConfigurationError"

  constructor() {
    super("D1 and R2 bindings are required.")
  }
}

export interface ResearchStore {
  createSession(session: StoredSession): Promise<void>
  appendEvents(sessionId: SessionId, batch: EventBatchRequest): Promise<number>
  putRawGaze(metadata: RawGazeMetadata, body: ArrayBuffer): Promise<string>
  completeSession(sessionId: SessionId, completion: CompleteSessionRequest): Promise<void>
}
