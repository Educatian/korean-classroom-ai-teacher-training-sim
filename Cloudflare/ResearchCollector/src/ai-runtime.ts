import ky from "ky"
import { z } from "zod"

const Unit = z.number().min(0).max(1)
const Affect = z.object({
  valence: z.number().min(-1).max(1),
  arousal: Unit,
  dominance: z.number().min(-1).max(1),
})
const ActionUnits = z.object({
  au1: Unit,
  au2: Unit,
  au4: Unit,
  au5: Unit,
  au6: Unit,
  au7: Unit,
  au9: Unit,
  au12: Unit,
  au15: Unit,
  au17: Unit,
  au20: Unit,
  au23: Unit,
  au24: Unit,
  au25: Unit,
  au26: Unit,
})
const DialogueSignals = z.object({
  feltHeard: Unit,
  perceivedPressure: Unit,
  choiceOffered: Unit,
  safetyConcern: Unit,
  readyForReentry: Unit,
})
export const StudentTurnSchema = z.object({
  studentReply: z.string().min(1).max(800),
  valence: Affect.shape.valence,
  arousal: Affect.shape.arousal,
  dominance: Affect.shape.dominance,
  gesture: z.string().min(1).max(64),
  actionUnits: ActionUnits,
  dialogueSignals: DialogueSignals,
})
export const TeacherRubricSchema = z.object({
  schemaVersion: z.literal(1),
  confidence: Unit,
  dimensions: z
    .array(
      z.object({
        dimension: z.number().int().min(0).max(5),
        score: z.number().min(0).max(3),
        evidence: z.string().min(1).max(600),
      }),
    )
    .min(1)
    .max(6),
  improvementSuggestion: z.string().min(1).max(800),
})
export const StudentEnvelopeSchema = z.object({
  schemaVersion: z.literal(1),
  sessionId: z.string().min(1).max(256),
  requestId: z.string().min(8).max(128),
  promptVersion: z.number().int().positive(),
  studentTurn: z.object({
    teacherUtterance: z.string().min(1).max(2000),
    conversationContext: z.string().max(12000),
    scenarioContext: z.string().max(4000),
    crisisStage: z.string().max(128),
    personaId: z.string().max(128),
    currentAffect: Affect,
  }),
})
export const RubricEnvelopeSchema = z.object({
  schemaVersion: z.literal(1),
  sessionId: z.string().min(1).max(256),
  requestId: z.string().min(8).max(128),
  promptVersion: z.number().int().positive(),
  teacherRubric: z.object({
    teacherUtterance: z.string().min(1).max(2000),
    studentReply: z.string().min(1).max(2000),
    scenarioContext: z.string().max(4000),
  }),
})
export const SpeechSynthesisRequestSchema = z.object({
  schemaVersion: z.literal(1),
  text: z.string().min(1).max(1000),
  rate: z.number().min(0.65).max(1.35),
  pitchSemitones: z.number().min(-6).max(6),
  volume: Unit,
})
export type StudentEnvelope = z.infer<typeof StudentEnvelopeSchema>
export type RubricEnvelope = z.infer<typeof RubricEnvelopeSchema>
export type StudentTurn = z.infer<typeof StudentTurnSchema>
export type TeacherRubric = z.infer<typeof TeacherRubricSchema>
export type SpeechSynthesisRequest = z.infer<typeof SpeechSynthesisRequestSchema>

export interface AiRuntime {
  generateStudentTurn(request: StudentEnvelope): Promise<StudentTurn>
  evaluateTeacherRubric(request: RubricEnvelope): Promise<TeacherRubric>
  transcribe(audio: Uint8Array): Promise<string>
  synthesize(request: SpeechSynthesisRequest): Promise<Uint8Array>
}

export type AiRuntimeBindings = {
  readonly OPENROUTER_API_KEY?: string
  readonly OPENROUTER_MODEL?: string
  readonly OPENROUTER_STT_MODEL?: string
  readonly OPENROUTER_TTS_MODEL?: string
  readonly OPENROUTER_TTS_VOICE?: string
}

const OpenRouterResponseSchema = z.object({
  choices: z.array(z.object({ message: z.object({ content: z.string() }) })).min(1),
})
const TranscriptionResponseSchema = z.object({ text: z.string().min(1) })
export class AiRuntimeConfigurationError extends Error {
  constructor(readonly service: "llm" | "speech") {
    super(`${service}_not_configured`)
    this.name = "AiRuntimeConfigurationError"
  }
}
export function sanitizeProviderDetail(raw: string): string {
  return raw.replace(/sk-[A-Za-z0-9_-]+/g, "[redacted]").slice(0, 500)
}

export class AiProviderError extends Error {
  constructor(
    readonly service: "llm" | "speech",
    readonly statusCode: number,
    readonly detail?: string,
  ) {
    super(`${service}_provider_failed`)
    this.name = "AiProviderError"
  }
}

async function providerError(
  service: "llm" | "speech",
  response: Response,
): Promise<AiProviderError> {
  return new AiProviderError(
    service,
    response.status,
    sanitizeProviderDetail(await response.text()),
  )
}

export class ManagedAiRuntime implements AiRuntime {
  constructor(private readonly bindings: AiRuntimeBindings) {}
  async generateStudentTurn(request: StudentEnvelope): Promise<StudentTurn> {
    const result = await this.completeJson(
      "You are a Korean elementary student in an evidence-based teacher response simulation. Reply naturally in Korean and return only JSON matching: studentReply, valence(-1..1), arousal(0..1), dominance(-1..1), gesture, actionUnits(au1,au2,au4,au5,au6,au7,au9,au12,au15,au17,au20,au23,au24,au25,au26 each 0..1), dialogueSignals(feltHeard,perceivedPressure,choiceOffered,safetyConcern,readyForReentry each 0..1). Avoid stereotypes and diagnoses.",
      JSON.stringify(request.studentTurn),
    )
    return StudentTurnSchema.parse(result)
  }
  async evaluateTeacherRubric(request: RubricEnvelope): Promise<TeacherRubric> {
    const result = await this.completeJson(
      "Evaluate the teacher response for a Korean elementary classroom. Return only JSON: schemaVersion 1, confidence 0..1, dimensions array using numeric dimension IDs 0 StudentDignity, 1 LowStimulusResponse, 2 EmotionAcknowledgement, 3 StudentAgency, 4 Safety, 5 InstructionalReentry; score 0..3 and Korean evidence; improvementSuggestion in Korean.",
      JSON.stringify(request.teacherRubric),
    )
    return TeacherRubricSchema.parse(result)
  }
  async transcribe(audio: Uint8Array): Promise<string> {
    const key = this.bindings.OPENROUTER_API_KEY
    if (!key) throw new AiRuntimeConfigurationError("speech")
    const response = await ky.post("https://openrouter.ai/api/v1/audio/transcriptions", {
      headers: { Authorization: `Bearer ${key}`, "X-Title": "Teacher Response Training" },
      json: {
        model: this.bindings.OPENROUTER_STT_MODEL ?? "openai/gpt-4o-mini-transcribe",
        input_audio: { data: encodeBase64(audio), format: "wav" },
        language: "ko",
        temperature: 0,
      },
      timeout: 30_000,
      retry: 1,
      throwHttpErrors: false,
    })
    if (!response.ok) throw await providerError("speech", response)
    return TranscriptionResponseSchema.parse(await response.json()).text
  }
  async synthesize(request: SpeechSynthesisRequest): Promise<Uint8Array> {
    const key = this.bindings.OPENROUTER_API_KEY
    if (!key) throw new AiRuntimeConfigurationError("speech")
    const response = await ky.post("https://openrouter.ai/api/v1/audio/speech", {
      headers: { Authorization: `Bearer ${key}`, "X-Title": "Teacher Response Training" },
      json: {
        model: this.bindings.OPENROUTER_TTS_MODEL ?? "x-ai/grok-voice-tts-1.0",
        input: request.text,
        voice: this.bindings.OPENROUTER_TTS_VOICE ?? "Ara",
        response_format: "pcm",
        speed: request.rate,
      },
      timeout: 30_000,
      retry: 1,
      throwHttpErrors: false,
    })
    if (!response.ok) throw await providerError("speech", response)
    return wrapPcm16MonoAsWav(new Uint8Array(await response.arrayBuffer()), 24_000)
  }
  private async completeJson(system: string, user: string): Promise<unknown> {
    const key = this.bindings.OPENROUTER_API_KEY
    if (!key) throw new AiRuntimeConfigurationError("llm")
    const response = await ky.post("https://openrouter.ai/api/v1/chat/completions", {
      headers: { Authorization: `Bearer ${key}`, "X-Title": "Teacher Response Training" },
      json: {
        model: this.bindings.OPENROUTER_MODEL ?? "openai/gpt-4.1-mini",
        temperature: 0.35,
        response_format: { type: "json_object" },
        messages: [
          { role: "system", content: system },
          { role: "user", content: user },
        ],
      },
      timeout: 30_000,
      retry: 1,
      throwHttpErrors: false,
    })
    if (!response.ok) throw await providerError("llm", response)
    const parsed = OpenRouterResponseSchema.parse(await response.json())
    const first = parsed.choices[0]
    if (!first) throw new AiProviderError("llm", 502)
    return JSON.parse(first.message.content)
  }
}

export function encodeBase64(bytes: Uint8Array): string {
  const chunkSize = 32_768
  let binary = ""
  for (let offset = 0; offset < bytes.length; offset += chunkSize) {
    binary += String.fromCharCode(...bytes.subarray(offset, offset + chunkSize))
  }
  return btoa(binary)
}

export function wrapPcm16MonoAsWav(pcm: Uint8Array, sampleRate: number): Uint8Array {
  const wav = new Uint8Array(44 + pcm.byteLength)
  const view = new DataView(wav.buffer)
  writeAscii(wav, 0, "RIFF")
  view.setUint32(4, 36 + pcm.byteLength, true)
  writeAscii(wav, 8, "WAVE")
  writeAscii(wav, 12, "fmt ")
  view.setUint32(16, 16, true)
  view.setUint16(20, 1, true)
  view.setUint16(22, 1, true)
  view.setUint32(24, sampleRate, true)
  view.setUint32(28, sampleRate * 2, true)
  view.setUint16(32, 2, true)
  view.setUint16(34, 16, true)
  writeAscii(wav, 36, "data")
  view.setUint32(40, pcm.byteLength, true)
  wav.set(pcm, 44)
  return wav
}

function writeAscii(target: Uint8Array, offset: number, value: string): void {
  for (let index = 0; index < value.length; index++)
    target[offset + index] = value.charCodeAt(index)
}
