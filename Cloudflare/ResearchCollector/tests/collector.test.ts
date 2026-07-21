import { describe, expect, it } from "vitest"
import { createCollectorApp } from "../src/app"
import { SessionIdSchema } from "../src/contracts"
import { InMemoryResearchStore } from "../src/testing/in-memory-research-store"

const AUTHORIZATION = "Bearer test-session-token"

describe("research collector", () => {
  it("stores a pseudonymous session when the bearer token is valid", async () => {
    // Given
    const store = new InMemoryResearchStore()
    const app = createCollectorApp(store)

    // When
    const response = await app.request(
      "/v1/sessions",
      {
        method: "POST",
        headers: {
          Authorization: AUTHORIZATION,
          "Content-Type": "application/json",
          "Idempotency-Key": "start-session-01",
        },
        body: JSON.stringify({
          schemaVersion: 1,
          sessionId: "a".repeat(32),
          participantCode: "P-014",
          scenarioId: "general-classroom",
          startedAtUtc: "2026-07-20T20:00:00.000Z",
          deviceModel: "Meta Quest Pro",
          buildVersion: "0.4.0",
          rawGazeConsent: true,
        }),
      },
      { RESEARCH_INGEST_TOKEN: "test-session-token" },
    )

    // Then
    expect(response.status).toBe(201)
    expect(store.sessions).toHaveLength(1)
    expect(store.sessions[0]?.participantCode).toBe("P-014")
  })

  it("rejects ingestion without a valid bearer token", async () => {
    // Given
    const store = new InMemoryResearchStore()
    const app = createCollectorApp(store)

    // When
    const response = await app.request(
      "/v1/sessions",
      { method: "POST" },
      { RESEARCH_INGEST_TOKEN: "test-session-token" },
    )

    // Then
    expect(response.status).toBe(401)
    expect(store.sessions).toHaveLength(0)
  })

  it("stores raw gaze bytes and searchable metadata", async () => {
    // Given
    const store = new InMemoryResearchStore()
    const app = createCollectorApp(store)
    const sessionId = SessionIdSchema.parse("b".repeat(32))
    await store.createSession({
      schemaVersion: 1,
      sessionId,
      participantCode: "P-015",
      scenarioId: "circle-discussion",
      startedAtUtc: "2026-07-20T20:00:00.000Z",
      deviceModel: "Meta Quest Pro",
      buildVersion: "0.4.0",
      rawGazeConsent: true,
      idempotencyKey: "seed-session",
    })
    const rawGaze = '{"sessionId":"bbbb","trackingValid":true}\n'

    // When
    const response = await app.request(
      `/v1/sessions/${sessionId}/raw-gaze`,
      {
        method: "PUT",
        headers: {
          Authorization: AUTHORIZATION,
          "Content-Type": "application/x-ndjson",
          "Idempotency-Key": "raw-gaze-01",
          "X-Content-Sha256": "c".repeat(64),
          "X-Gaze-Sample-Count": "1",
          "X-Gaze-Started-At": "2026-07-20T20:00:01.000Z",
          "X-Gaze-Ended-At": "2026-07-20T20:00:01.033Z",
        },
        body: rawGaze,
      },
      { RESEARCH_INGEST_TOKEN: "test-session-token" },
    )

    // Then
    expect(response.status).toBe(201)
    expect(store.rawObjects).toHaveLength(1)
    expect(store.rawObjects[0]?.sampleCount).toBe(1)
    expect(store.objectBodies.get(store.rawObjects[0]?.objectKey ?? "")).toBe(rawGaze)
  })

  it("refuses raw gaze when the session consent flag is false", async () => {
    // Given
    const store = new InMemoryResearchStore()
    const app = createCollectorApp(store)
    const sessionId = SessionIdSchema.parse("d".repeat(32))
    await store.createSession({
      schemaVersion: 1,
      sessionId,
      participantCode: "P-016",
      scenarioId: "general-classroom",
      startedAtUtc: "2026-07-20T20:00:00.000Z",
      deviceModel: "Meta Quest Pro",
      buildVersion: "0.4.0",
      rawGazeConsent: false,
      idempotencyKey: "seed-no-consent",
    })

    // When
    const response = await app.request(
      `/v1/sessions/${sessionId}/raw-gaze`,
      {
        method: "PUT",
        headers: {
          Authorization: AUTHORIZATION,
          "Content-Type": "application/x-ndjson",
          "Idempotency-Key": "raw-gaze-no-consent",
          "X-Content-Sha256": "e".repeat(64),
          "X-Gaze-Sample-Count": "1",
          "X-Gaze-Started-At": "2026-07-20T20:00:01.000Z",
          "X-Gaze-Ended-At": "2026-07-20T20:00:01.033Z",
        },
        body: "{}\n",
      },
      { RESEARCH_INGEST_TOKEN: "test-session-token" },
    )

    // Then
    expect(response.status).toBe(409)
    expect(store.rawObjects).toHaveLength(0)
  })
})
