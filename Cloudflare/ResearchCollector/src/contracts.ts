import { z } from "zod"

export const SessionIdSchema = z
  .string()
  .regex(/^[a-f0-9]{32}$/)
  .brand("SessionId")
export type SessionId = z.infer<typeof SessionIdSchema>

export const StartSessionRequestSchema = z.object({
  schemaVersion: z.literal(1),
  sessionId: SessionIdSchema,
  participantCode: z
    .string()
    .min(1)
    .max(64)
    .regex(/^[A-Za-z0-9_-]+$/),
  scenarioId: z.string().min(1).max(128),
  startedAtUtc: z.iso.datetime({ offset: true }),
  deviceModel: z.string().min(1).max(128),
  buildVersion: z.string().min(1).max(64),
  rawGazeConsent: z.boolean(),
})
export type StartSessionRequest = z.infer<typeof StartSessionRequestSchema>

export const StoredSessionSchema = StartSessionRequestSchema.extend({
  idempotencyKey: z.string().min(8).max(128),
})
export type StoredSession = z.infer<typeof StoredSessionSchema>

export const TelemetryEventSchema = z
  .object({
    schemaVersion: z.number().int().positive(),
    eventId: z.string().min(1).max(128),
    sessionId: SessionIdSchema,
    sequence: z.number().int().nonnegative(),
    timestampUtc: z.iso.datetime({ offset: true }),
    scenarioId: z.string().max(128),
    beatIndex: z.number().int(),
    kind: z.number().int().nonnegative(),
  })
  .catchall(z.unknown())
export type TelemetryEvent = z.infer<typeof TelemetryEventSchema>

export const EventBatchRequestSchema = z.object({
  schemaVersion: z.literal(1),
  requestId: z.string().min(8).max(128),
  events: z.array(TelemetryEventSchema).min(1).max(250),
})
export type EventBatchRequest = z.infer<typeof EventBatchRequestSchema>

export const CompleteSessionRequestSchema = z.object({
  schemaVersion: z.literal(1),
  completedAtUtc: z.iso.datetime({ offset: true }),
  status: z.enum(["completed", "aborted"]),
  report: z.record(z.string(), z.unknown()),
})
export type CompleteSessionRequest = z.infer<typeof CompleteSessionRequestSchema>

export const RawGazeMetadataSchema = z.object({
  sessionId: SessionIdSchema,
  idempotencyKey: z.string().min(8).max(128),
  sha256: z.string().regex(/^[a-f0-9]{64}$/),
  sampleCount: z.number().int().positive(),
  startedAtUtc: z.iso.datetime({ offset: true }),
  endedAtUtc: z.iso.datetime({ offset: true }),
  contentType: z.enum(["application/x-ndjson", "application/gzip"]),
})
export type RawGazeMetadata = z.infer<typeof RawGazeMetadataSchema>

export type StoredRawGazeObject = RawGazeMetadata & {
  readonly objectKey: string
  readonly byteLength: number
}

export function parseRawGazeMetadata(sessionId: string, headers: Headers): RawGazeMetadata {
  return RawGazeMetadataSchema.parse({
    sessionId,
    idempotencyKey: headers.get("Idempotency-Key"),
    sha256: headers.get("X-Content-Sha256"),
    sampleCount: Number(headers.get("X-Gaze-Sample-Count")),
    startedAtUtc: headers.get("X-Gaze-Started-At"),
    endedAtUtc: headers.get("X-Gaze-Ended-At"),
    contentType: headers.get("Content-Type"),
  })
}
