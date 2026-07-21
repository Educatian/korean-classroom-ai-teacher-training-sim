import { describe, expect, it } from "vitest"
import type { AiRuntime, SpeechSynthesisRequest } from "../src/ai-runtime"
import { encodeBase64, sanitizeProviderDetail, wrapPcm16MonoAsWav } from "../src/ai-runtime"
import { createCollectorApp } from "../src/app"

const ENV = {
  RESEARCH_INGEST_TOKEN: "admin-token",
  RESEARCH_SESSION_SIGNING_SECRET: "test-signing-secret-with-at-least-32-bytes",
}
class FakeAiRuntime implements AiRuntime {
  readonly audioSizes: number[] = []
  readonly spokenTexts: string[] = []
  async generateStudentTurn() {
    return {
      studentReply: "조금만 쉬고 다시 해볼게요.",
      valence: -0.1,
      arousal: 0.35,
      dominance: 0,
      gesture: "SmallNod",
      actionUnits: {
        au1: 0.1,
        au2: 0,
        au4: 0.1,
        au5: 0.1,
        au6: 0,
        au7: 0.1,
        au9: 0,
        au12: 0.08,
        au15: 0,
        au17: 0,
        au20: 0,
        au23: 0,
        au24: 0,
        au25: 0.1,
        au26: 0,
      },
      dialogueSignals: {
        feltHeard: 0.8,
        perceivedPressure: 0.15,
        choiceOffered: 0.7,
        safetyConcern: 0.1,
        readyForReentry: 0.65,
      },
    }
  }
  async evaluateTeacherRubric() {
    return {
      schemaVersion: 1 as const,
      confidence: 0.9,
      dimensions: [{ dimension: 0, score: 2.6, evidence: "학생의 감정을 먼저 확인했다." }],
      improvementSuggestion: "선택지를 한 가지 더 제시하세요.",
    }
  }
  async transcribe(audio: Uint8Array) {
    this.audioSizes.push(audio.byteLength)
    return "지금 많이 답답해 보이는구나."
  }
  async synthesize(request: SpeechSynthesisRequest) {
    this.spokenTexts.push(request.text)
    return new Uint8Array([82, 73, 70, 70, 0, 0, 0, 0, 87, 65, 86, 69])
  }
}
async function tokenFor(app: ReturnType<typeof createCollectorApp>): Promise<string> {
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
        deviceModel: "Meta Quest 2",
      }),
    },
    ENV,
  )
  const body: unknown = await response.json()
  if (
    typeof body !== "object" ||
    body === null ||
    !("token" in body) ||
    typeof body.token !== "string"
  )
    throw new TypeError("Missing token")
  return body.token
}
const headers = (token: string, contentType = "application/json") => ({
  Authorization: `Bearer ${token}`,
  "Content-Type": contentType,
  "X-Client-Id": "teacher-training-quest",
})
const envelope = {
  schemaVersion: 1,
  sessionId: "device-session",
  requestId: "request-student-001",
  promptVersion: 1,
  studentTurn: {
    teacherUtterance: "괜찮니? 잠깐 쉬어도 돼.",
    conversationContext: "학생이 과제를 거부했다.",
    scenarioContext: "한국 초등학교 교실",
    crisisStage: "Escalating",
    personaId: "withdrawn-student",
    currentAffect: { valence: -0.5, arousal: 0.7, dominance: -0.2 },
  },
}

describe("OpenRouter audio adaptation", () => {
  it("encodes WAV bytes for the OpenRouter transcription contract", () => {
    expect(encodeBase64(new Uint8Array([82, 73, 70, 70]))).toBe("UklGRg==")
  })

  it("removes provider credentials and bounds diagnostics", () => {
    const detail = sanitizeProviderDetail(
      `{"error":"invalid voice","token":"sk-demo-secret-value"}`,
    )
    expect(detail).toContain("invalid voice")
    expect(detail).not.toContain("secret-value")
    expect(detail.length).toBeLessThanOrEqual(500)
  })

  it("wraps 24 kHz PCM16 mono output as a Unity-loadable WAV", () => {
    const wav = wrapPcm16MonoAsWav(new Uint8Array([0, 0, 255, 127]), 24_000)
    expect(new TextDecoder().decode(wav.slice(0, 4))).toBe("RIFF")
    expect(new TextDecoder().decode(wav.slice(8, 12))).toBe("WAVE")
    expect(new DataView(wav.buffer).getUint32(24, true)).toBe(24_000)
    expect(new DataView(wav.buffer).getUint32(40, true)).toBe(4)
  })
})
describe("Quest secure runtime proxy", () => {
  it("returns Unity-compatible student and rubric envelopes", async () => {
    const app = createCollectorApp(undefined, new FakeAiRuntime())
    const token = await tokenFor(app)
    const student = await app.request(
      "/v1/student-turn",
      { method: "POST", headers: headers(token), body: JSON.stringify(envelope) },
      ENV,
    )
    const rubric = await app.request(
      "/v1/teacher-rubric",
      {
        method: "POST",
        headers: headers(token),
        body: JSON.stringify({
          ...envelope,
          requestId: "request-rubric-001",
          studentTurn: undefined,
          teacherRubric: {
            teacherUtterance: "괜찮니?",
            studentReply: "조금 쉴래요.",
            scenarioContext: "교실",
          },
        }),
      },
      ENV,
    )
    expect(student.status).toBe(200)
    expect(await student.json()).toMatchObject({
      schemaVersion: 1,
      requestId: "request-student-001",
      studentTurn: { studentReply: "조금만 쉬고 다시 해볼게요." },
    })
    expect(rubric.status).toBe(200)
    expect(await rubric.json()).toMatchObject({
      schemaVersion: 1,
      requestId: "request-rubric-001",
      teacherRubric: { confidence: 0.9 },
    })
  })
  it("proxies microphone WAV and synthesized WAV", async () => {
    const runtime = new FakeAiRuntime()
    const app = createCollectorApp(undefined, runtime)
    const token = await tokenFor(app)
    const wav = new Uint8Array([82, 73, 70, 70, 1, 2, 3, 4])
    const transcript = await app.request(
      "/v1/transcribe",
      { method: "POST", headers: headers(token, "audio/wav"), body: wav },
      ENV,
    )
    const speech = await app.request(
      "/v1/speech",
      {
        method: "POST",
        headers: headers(token),
        body: JSON.stringify({
          schemaVersion: 1,
          text: "조금만 쉬고 다시 할게요.",
          rate: 0.9,
          pitchSemitones: -0.5,
          volume: 0.8,
        }),
      },
      ENV,
    )
    const transcriptBody: unknown = await transcript.json()
    expect(transcriptBody).toEqual({ schemaVersion: 1, transcript: "지금 많이 답답해 보이는구나." })
    expect(runtime.audioSizes).toEqual([8])
    expect(speech.status).toBe(200)
    expect(speech.headers.get("Content-Type")).toContain("audio/wav")
    expect(runtime.spokenTexts).toEqual(["조금만 쉬고 다시 할게요."])
  })
  it("rejects missing authorization and oversized audio", async () => {
    const app = createCollectorApp(undefined, new FakeAiRuntime())
    const token = await tokenFor(app)
    const unauthorized = await app.request(
      "/v1/student-turn",
      {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(envelope),
      },
      ENV,
    )
    const oversized = await app.request(
      "/v1/transcribe",
      {
        method: "POST",
        headers: headers(token, "audio/wav"),
        body: new Uint8Array(5 * 1024 * 1024 + 1),
      },
      ENV,
    )
    expect(unauthorized.status).toBe(401)
    expect(oversized.status).toBe(413)
  })
})
