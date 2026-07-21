import { Hono } from "hono"
import { ZodError } from "zod"
import {
  AiProviderError,
  type AiRuntime,
  type AiRuntimeBindings,
  AiRuntimeConfigurationError,
  ManagedAiRuntime,
  RubricEnvelopeSchema,
  SpeechSynthesisRequestSchema,
  StudentEnvelopeSchema,
} from "./ai-runtime"
import { CloudflareResearchStore } from "./cloudflare-research-store"
import {
  CompleteSessionRequestSchema,
  EventBatchRequestSchema,
  parseRawGazeMetadata,
  SessionIdSchema,
  StartSessionRequestSchema,
  StoredSessionSchema,
} from "./contracts"
import {
  RawGazeConsentRequiredError,
  type ResearchStore,
  ResearchStoreConfigurationError,
} from "./research-store"
import {
  issueQuestSessionToken,
  QuestSessionTokenRequestSchema,
  type ResearchAuthorization,
  timingSafeStringEquals,
  validateResearchAuthorization,
} from "./session-authorization"

type Bindings = AiRuntimeBindings & {
  readonly RESEARCH_INGEST_TOKEN: string
  readonly RESEARCH_SESSION_SIGNING_SECRET: string
  readonly RESEARCH_REGISTRATION_KEY?: string
  readonly DB?: D1Database
  readonly RAW_GAZE?: R2Bucket
}

type Variables = {
  readonly researchStore: ResearchStore
  readonly researchAuthorization: ResearchAuthorization
}

const MAX_RAW_GAZE_BYTES = 50 * 1024 * 1024

export function createCollectorApp(injectedStore?: ResearchStore, injectedAiRuntime?: AiRuntime) {
  const app = new Hono<{ Bindings: Bindings; Variables: Variables }>()

  app.get("/health", (context) =>
    context.json({ status: "ok", service: "teacher-training-research-collector" }),
  )

  app.post("/v1/auth/quest-session", async (context) => {
    const signingSecret = context.env.RESEARCH_SESSION_SIGNING_SECRET
    if (!signingSecret) {
      throw new ResearchStoreConfigurationError()
    }
    // When a registration key is configured, token minting requires the shared
    // client key; this closes the open-relay path where any caller could mint
    // a valid session token from a self-chosen installation id.
    const registrationKey = context.env.RESEARCH_REGISTRATION_KEY
    if (registrationKey) {
      const suppliedKey = context.req.header("X-Registration-Key") ?? ""
      if (!timingSafeStringEquals(registrationKey, suppliedKey)) {
        return context.json({ error: "unauthorized" }, 401)
      }
    }
    const request = QuestSessionTokenRequestSchema.parse(await context.req.json())
    const issued = await issueQuestSessionToken(request.installationId, signingSecret)
    context.header("Cache-Control", "no-store")
    return context.json({ schemaVersion: 1, ...issued })
  })

  app.use("/v1/*", async (context, next) => {
    if (context.req.path === "/v1/auth/quest-session") {
      await next()
      return
    }
    const supplied = context.req.header("Authorization")
    const bearerToken = supplied?.startsWith("Bearer ") ? supplied.slice(7) : ""
    const authorization = await validateResearchAuthorization(
      bearerToken,
      context.req.header("X-Client-Id"),
      context.env.RESEARCH_INGEST_TOKEN,
      context.env.RESEARCH_SESSION_SIGNING_SECRET,
    )
    if (!authorization) {
      return context.json({ error: "unauthorized" }, 401)
    }
    context.set("researchAuthorization", authorization)

    if (context.req.path.startsWith("/v1/sessions")) {
      if (injectedStore) {
        context.set("researchStore", injectedStore)
      } else {
        const database = context.env.DB
        const rawGazeBucket = context.env.RAW_GAZE
        if (!database || !rawGazeBucket) {
          throw new ResearchStoreConfigurationError()
        }
        context.set("researchStore", new CloudflareResearchStore(database, rawGazeBucket))
      }
    }
    await next()
  })

  app.post("/v1/student-turn", async (context) => {
    const envelope = StudentEnvelopeSchema.parse(await context.req.json())
    const runtime = injectedAiRuntime ?? new ManagedAiRuntime(context.env)
    const studentTurn = await runtime.generateStudentTurn(envelope)
    return context.json({ schemaVersion: 1, requestId: envelope.requestId, studentTurn })
  })

  app.post("/v1/teacher-rubric", async (context) => {
    const envelope = RubricEnvelopeSchema.parse(await context.req.json())
    const runtime = injectedAiRuntime ?? new ManagedAiRuntime(context.env)
    const teacherRubric = await runtime.evaluateTeacherRubric(envelope)
    return context.json({ schemaVersion: 1, requestId: envelope.requestId, teacherRubric })
  })

  app.post("/v1/transcribe", async (context) => {
    if (!context.req.header("Content-Type")?.startsWith("audio/wav")) {
      return context.json({ error: "unsupported_audio_type" }, 415)
    }
    const body = await context.req.arrayBuffer()
    if (body.byteLength === 0) return context.json({ error: "empty_audio" }, 400)
    if (body.byteLength > 5 * 1024 * 1024) return context.json({ error: "audio_too_large" }, 413)
    const runtime = injectedAiRuntime ?? new ManagedAiRuntime(context.env)
    const transcript = await runtime.transcribe(new Uint8Array(body))
    context.header("Cache-Control", "no-store")
    return context.json({ schemaVersion: 1, transcript })
  })

  app.post("/v1/speech", async (context) => {
    const speechRequest = SpeechSynthesisRequestSchema.parse(await context.req.json())
    const runtime = injectedAiRuntime ?? new ManagedAiRuntime(context.env)
    const wav = await runtime.synthesize(speechRequest)
    return new Response(Uint8Array.from(wav), {
      status: 200,
      headers: { "Cache-Control": "no-store", "Content-Type": "audio/wav" },
    })
  })
  app.post("/v1/sessions", async (context) => {
    const body = StartSessionRequestSchema.parse(await context.req.json())
    const session = StoredSessionSchema.parse({
      ...body,
      idempotencyKey: requireIdempotencyKey(context.req.header("Idempotency-Key")),
    })
    const authorization = context.get("researchAuthorization")
    if (
      authorization.participantCode &&
      authorization.participantCode !== session.participantCode
    ) {
      return context.json({ error: "participant_mismatch" }, 403)
    }
    await context.get("researchStore").createSession(session)
    return context.json({ stored: true, sessionId: session.sessionId }, 201)
  })

  app.post("/v1/sessions/:sessionId/events", async (context) => {
    const sessionId = SessionIdSchema.parse(context.req.param("sessionId"))
    const batch = EventBatchRequestSchema.parse(await context.req.json())
    if (batch.events.some((event) => event.sessionId !== sessionId)) {
      return context.json({ error: "event_session_mismatch" }, 409)
    }
    const storedCount = await context.get("researchStore").appendEvents(sessionId, batch)
    return context.json({ stored: true, storedCount }, 201)
  })

  app.put("/v1/sessions/:sessionId/raw-gaze", async (context) => {
    const metadata = parseRawGazeMetadata(context.req.param("sessionId"), context.req.raw.headers)
    const body = await context.req.arrayBuffer()
    if (body.byteLength === 0) {
      return context.json({ error: "empty_raw_gaze" }, 400)
    }
    if (body.byteLength > MAX_RAW_GAZE_BYTES) {
      return context.json({ error: "raw_gaze_too_large" }, 413)
    }
    const objectKey = await context.get("researchStore").putRawGaze(metadata, body)
    return context.json({ stored: true, objectKey, byteLength: body.byteLength }, 201)
  })

  app.post("/v1/sessions/:sessionId/complete", async (context) => {
    const sessionId = SessionIdSchema.parse(context.req.param("sessionId"))
    const completion = CompleteSessionRequestSchema.parse(await context.req.json())
    await context.get("researchStore").completeSession(sessionId, completion)
    return context.json({ stored: true, sessionId })
  })

  app.onError((error, context) => {
    if (error instanceof ZodError || error instanceof SyntaxError) {
      return context.json({ error: "invalid_request" }, 400)
    }
    if (error instanceof RawGazeConsentRequiredError) {
      return context.json({ error: "raw_gaze_consent_required" }, 409)
    }
    if (error instanceof AiRuntimeConfigurationError) {
      return context.json({ error: error.message }, 503)
    }
    if (error instanceof AiProviderError) {
      return context.json(
        { error: error.message, upstreamStatus: error.statusCode, upstreamDetail: error.detail },
        502,
      )
    }
    if (error instanceof ResearchStoreConfigurationError) {
      return context.json({ error: "storage_not_configured" }, 503)
    }
    console.error(
      JSON.stringify({
        level: "error",
        event: "collector_request_failed",
        errorName: error.name,
      }),
    )
    return context.json({ error: "internal_error" }, 500)
  })

  return app
}

function requireIdempotencyKey(value: string | undefined): string {
  if (!value || value.length < 8 || value.length > 128) {
    throw new ZodError([])
  }
  return value
}
