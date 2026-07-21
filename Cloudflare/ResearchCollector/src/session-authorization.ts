import { z } from "zod"

const ALLOWED_CLIENT_ID = "teacher-training-quest"
const TOKEN_TTL_SECONDS = 24 * 60 * 60

const QuestSessionTokenPayloadSchema = z.object({
  version: z.literal(1),
  clientId: z.literal(ALLOWED_CLIENT_ID),
  participantCode: z.string().regex(/^Q-[a-f0-9]{24}$/),
  issuedAt: z.number().int().nonnegative(),
  expiresAt: z.number().int().positive(),
})

export const QuestSessionTokenRequestSchema = z.object({
  schemaVersion: z.literal(1),
  clientId: z.literal(ALLOWED_CLIENT_ID),
  installationId: z.string().regex(/^[a-f0-9]{32}$/),
  buildVersion: z.string().min(1).max(64),
  deviceModel: z.string().min(1).max(128),
})

export type ResearchAuthorization = {
  readonly clientId: string
  readonly participantCode?: string
}

export async function issueQuestSessionToken(
  installationId: string,
  signingSecret: string,
  nowSeconds = Math.floor(Date.now() / 1000),
) {
  const participantCode = await deriveParticipantCode(installationId)
  const payload = QuestSessionTokenPayloadSchema.parse({
    version: 1,
    clientId: ALLOWED_CLIENT_ID,
    participantCode,
    issuedAt: nowSeconds,
    expiresAt: nowSeconds + TOKEN_TTL_SECONDS,
  })
  const encodedPayload = encodeBase64Url(new TextEncoder().encode(JSON.stringify(payload)))
  const signature = await sign(encodedPayload, signingSecret)
  return {
    token: `${encodedPayload}.${encodeBase64Url(signature)}`,
    participantCode,
    expiresAtUtc: new Date(payload.expiresAt * 1000).toISOString(),
  }
}

export function timingSafeStringEquals(expected: string, supplied: string): boolean {
  const encoder = new TextEncoder()
  return constantTimeEquals(encoder.encode(expected), encoder.encode(supplied))
}

export async function validateResearchAuthorization(
  bearerToken: string,
  clientId: string | undefined,
  staticToken: string | undefined,
  signingSecret: string | undefined,
): Promise<ResearchAuthorization | undefined> {
  if (!bearerToken) return undefined
  if (staticToken && timingSafeStringEquals(staticToken, bearerToken)) {
    return { clientId: clientId ?? "admin" }
  }
  if (!clientId || clientId !== ALLOWED_CLIENT_ID) return undefined
  if (!signingSecret) return undefined

  const parts = bearerToken.split(".")
  if (parts.length !== 2) return undefined
  const expected = await sign(parts[0] ?? "", signingSecret)
  const supplied = decodeBase64Url(parts[1] ?? "")
  if (!constantTimeEquals(expected, supplied)) return undefined

  try {
    const payload = QuestSessionTokenPayloadSchema.parse(
      JSON.parse(new TextDecoder().decode(decodeBase64Url(parts[0] ?? ""))),
    )
    if (payload.clientId !== clientId || payload.expiresAt <= Math.floor(Date.now() / 1000)) {
      return undefined
    }
    return { clientId, participantCode: payload.participantCode }
  } catch {
    return undefined
  }
}

export async function deriveParticipantCode(installationId: string): Promise<string> {
  const digest = await crypto.subtle.digest(
    "SHA-256",
    new TextEncoder().encode(`${ALLOWED_CLIENT_ID}:${installationId}`),
  )
  return `Q-${Array.from(new Uint8Array(digest))
    .map((value) => value.toString(16).padStart(2, "0"))
    .join("")
    .slice(0, 24)}`
}

async function sign(payload: string, signingSecret: string): Promise<Uint8Array> {
  const key = await crypto.subtle.importKey(
    "raw",
    new TextEncoder().encode(signingSecret),
    { name: "HMAC", hash: "SHA-256" },
    false,
    ["sign"],
  )
  return new Uint8Array(await crypto.subtle.sign("HMAC", key, new TextEncoder().encode(payload)))
}

function encodeBase64Url(bytes: Uint8Array): string {
  let binary = ""
  for (const byte of bytes) binary += String.fromCharCode(byte)
  return btoa(binary).replaceAll("+", "-").replaceAll("/", "_").replaceAll("=", "")
}

function decodeBase64Url(value: string): Uint8Array {
  const padded = value
    .replaceAll("-", "+")
    .replaceAll("_", "/")
    .padEnd(Math.ceil(value.length / 4) * 4, "=")
  const binary = atob(padded)
  return Uint8Array.from(binary, (character) => character.charCodeAt(0))
}

function constantTimeEquals(expected: Uint8Array, supplied: Uint8Array): boolean {
  if (expected.length !== supplied.length) return false
  let difference = 0
  for (let index = 0; index < expected.length; index++) {
    difference |= (expected[index] ?? 0) ^ (supplied[index] ?? 0)
  }
  return difference === 0
}
