import { describe, expect, it } from "vitest"
import { createCollectorApp } from "../src/app"
import { InMemoryResearchStore } from "../src/testing/in-memory-research-store"

const ENV = {
  RESEARCH_INGEST_TOKEN: "admin-token",
  RESEARCH_SESSION_SIGNING_SECRET: "test-signing-secret-with-at-least-32-bytes",
}

async function issueToken(app: ReturnType<typeof createCollectorApp>) {
  const response = await app.request(
    "/v1/auth/quest-session",
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        schemaVersion: 1,
        clientId: "teacher-training-quest",
        installationId: "1".repeat(32),
        buildVersion: "0.4.0",
        deviceModel: "Meta Quest Pro",
      }),
    },
    ENV,
  )
  return {
    response,
    body: (await response.json()) as {
      token: string
      participantCode: string
      expiresAtUtc: string
    },
  }
}

describe("automatic Quest session authorization", () => {
  it("issues a scoped token that can create its pseudonymous session", async () => {
    const store = new InMemoryResearchStore()
    const app = createCollectorApp(store)
    const issued = await issueToken(app)

    expect(issued.response.status).toBe(200)
    expect(issued.response.headers.get("Cache-Control")).toBe("no-store")
    expect(issued.body.participantCode).toMatch(/^Q-[a-f0-9]{24}$/)

    const response = await app.request(
      "/v1/sessions",
      {
        method: "POST",
        headers: {
          Authorization: `Bearer ${issued.body.token}`,
          "Content-Type": "application/json",
          "Idempotency-Key": "auto-session-start",
          "X-Client-Id": "teacher-training-quest",
        },
        body: JSON.stringify({
          schemaVersion: 1,
          sessionId: "2".repeat(32),
          participantCode: issued.body.participantCode,
          scenarioId: "general-classroom",
          startedAtUtc: "2026-07-20T20:00:00.000Z",
          deviceModel: "Meta Quest Pro",
          buildVersion: "0.4.0",
          rawGazeConsent: false,
        }),
      },
      ENV,
    )

    expect(response.status).toBe(201)
    expect(store.sessions[0]?.participantCode).toBe(issued.body.participantCode)
  })

  it("rejects participant substitution and tampered tokens", async () => {
    const store = new InMemoryResearchStore()
    const app = createCollectorApp(store)
    const issued = await issueToken(app)
    const requestBody = {
      schemaVersion: 1,
      sessionId: "3".repeat(32),
      participantCode: `Q-${"f".repeat(24)}`,
      scenarioId: "general-classroom",
      startedAtUtc: "2026-07-20T20:00:00.000Z",
      deviceModel: "Meta Quest Pro",
      buildVersion: "0.4.0",
      rawGazeConsent: false,
    }

    const mismatch = await app.request(
      "/v1/sessions",
      {
        method: "POST",
        headers: {
          Authorization: `Bearer ${issued.body.token}`,
          "Content-Type": "application/json",
          "Idempotency-Key": "participant-mismatch",
          "X-Client-Id": "teacher-training-quest",
        },
        body: JSON.stringify(requestBody),
      },
      ENV,
    )
    const tampered = await app.request(
      "/v1/sessions",
      {
        method: "POST",
        headers: {
          Authorization: `Bearer ${issued.body.token.slice(0, -1)}x`,
          "Content-Type": "application/json",
          "Idempotency-Key": "tampered-token",
          "X-Client-Id": "teacher-training-quest",
        },
        body: JSON.stringify({ ...requestBody, participantCode: issued.body.participantCode }),
      },
      ENV,
    )

    expect(mismatch.status).toBe(403)
    expect(tampered.status).toBe(401)
    expect(store.sessions).toHaveLength(0)
  })

  it("gates token minting behind the registration key when one is configured", async () => {
    const app = createCollectorApp(new InMemoryResearchStore())
    const gatedEnv = { ...ENV, RESEARCH_REGISTRATION_KEY: "shared-registration-key" }
    const requestBody = JSON.stringify({
      schemaVersion: 1,
      clientId: "teacher-training-quest",
      installationId: "1".repeat(32),
      buildVersion: "0.4.0",
      deviceModel: "Meta Quest Pro",
    })
    const post = (headers: Record<string, string>) =>
      app.request(
        "/v1/auth/quest-session",
        {
          method: "POST",
          headers: { "Content-Type": "application/json", ...headers },
          body: requestBody,
        },
        gatedEnv,
      )

    const missingKey = await post({})
    const wrongKey = await post({ "X-Registration-Key": "not-the-key" })
    const correctKey = await post({ "X-Registration-Key": "shared-registration-key" })

    expect(missingKey.status).toBe(401)
    expect(wrongKey.status).toBe(401)
    expect(correctKey.status).toBe(200)
    const issued = (await correctKey.json()) as { participantCode: string }
    expect(issued.participantCode).toMatch(/^Q-[a-f0-9]{24}$/)
  })
})
