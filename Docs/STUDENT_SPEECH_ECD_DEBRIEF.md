# Student Speech, ECD, and Research Debrief

## Implemented flow

A resolved student turn now follows one connected research pipeline:

1. The student agent returns Korean dialogue, affect values, a gesture, and Facial Action Unit directives.
2. `StudentSpeechProsodyPlanner` converts valence, arousal, dominance, punctuation, and text length into a speaking-rate, pitch, volume, and pause plan.
3. `StudentSpeechSynthesizer` requests a synthetic Korean voice on Windows when an environment-scoped OpenAI key is available.
4. `NpcSpeechPerformance` plays the clip from the student's position and drives AU25 (lips part) and AU26 (jaw drop) from the live audio amplitude.
5. If speech synthesis is unavailable, the same prosody plan drives pause-aware lip movement so the facial performance remains coherent.
6. Telemetry records the provider route and prosody plan without storing the raw teacher or student utterance.
7. At completion, `EcdAssessmentEngine` builds the research debrief report from the same event stream.

The HUD discloses that the student voice is synthetic.

## Student voice configuration

### Windows desktop

The runtime checks these environment variables:

| Variable | Required | Default |
|---|---:|---|
| `OPENAI_API_KEY` | For high-quality Korean TTS | none |
| `OPENAI_TTS_MODEL` | No | `gpt-4o-mini-tts` |
| `OPENAI_TTS_VOICE` | No | `coral` |

No provider key is serialized into a Unity scene, ScriptableObject, build, log, screenshot, or telemetry event.

The fallback order is:

1. OpenAI Audio API using the process environment on Windows;
2. installed Windows Korean speech voice;
3. silent, pause-aware lip-sync animation.

The current reference workstation has no Korean Windows SAPI voice installed, so reproducible Korean voice QA uses the environment-scoped OpenAI path.

### Meta Quest

A provider key must never be packaged in the APK. Quest uses the existing secure-proxy trust boundary. The client sends text plus the prosody plan to an authenticated server route, receives short-lived audio bytes, and passes the returned clip to the same `NpcSpeechPerformance` amplitude-driven lip-sync path. The current APK still requires deployment of that server-side TTS route before headset speech QA.

## Prosody and lip-sync starting values

The values below are research-prototype starting values, not validated child-speech norms:

- speaking rate: 0.72 to 1.20, increased mainly by arousal;
- pitch: -3.5 to +3.5 semitones, influenced by valence and arousal;
- volume: 0.66 to 0.94 from low to high arousal;
- comma pause: 250 to 105 milliseconds;
- sentence pause: 520 to 240 milliseconds;
- spatial audio blend: 0.82, with a 0.7 m minimum distance and 10 m maximum distance.

Validation should compare the synthesized output with ratings from Korean elementary teachers and speech experts. Recommended checks are perceived emotional fit, naturalness, intelligibility, excessive dramatization, pause appropriateness, and lip closure/jaw timing.

## Editable ECD assessment model

The canonical asset is:

`Assets/Resources/Training/ECD/TeacherResponseEcdModel.asset`

Researchers can edit it in the Unity Inspector without changing C# code. The model contains:

- competency identifier, Korean label, description, and score weight;
- one or more observable behaviors per competency;
- evidence-ID matching rules;
- expected scores for each observable behavior;
- missed-signal feedback;
- score bands and interpretations.

The runtime chain is:

`Competency → Observable behavior → Telemetry evidence → Weighted score → Feedback`

Use **Teacher Training > ECD > Create or Refresh Default Model** only when intentionally resetting the asset to repository defaults. Refreshing overwrites edits in the default asset, so study-specific versions should be duplicated and assigned to the scene controller.

## Research debrief dashboard

The debrief mode now displays:

- a compact in-class summary and a dedicated full-screen dashboard;
- overview, competency/evidence, and intervention-timeline tabs;
- valence and arousal trajectories across the session;
- the teacher-intervention timeline with pre/post valence and privacy-safe speech summaries;
- teacher-speech coaching with an LLM rubric suggestion when available, or a deterministic competency-based alternative;
- competency scores and evidence counts;
- missed-signal prompts linked to the highest-arousal beat;
- a same-scenario retry control;
- anonymized JSON and competency CSV export.

Exports are written under:

`Application.persistentDataPath/research-debrief/`

Raw teacher and student utterances are not exported. Session IDs, model/version identifiers, competency values, evidence counts, affect states, intervention sequences, and speech-prosody metadata remain available for research analysis.

The dashboard also does not retain or display the raw teacher utterance. It shows a privacy-safe summary derived from action source, text length, and observed competency evidence. LLM rubric improvement suggestions are stored separately as coaching feedback and are escaped before TextMeshPro rendering.

## Verification

The July 19, 2026 implementation pass verified:

- Unity EditMode: 102 tests passed, 0 failed;
- Windows player build: succeeded;
- continuous Windows autoplay: classroom view, direct dialogue and eye contact, six intervention beats, and research dashboard capture completed;
- runtime log: no dashboard exception after the graph-component fix;
- reference captures: `Assets/Reference/ResearchDebriefDashboard.png` and `Assets/Reference/ResearchDashboardSpeechCoaching.png`.

Before a study release, complete Korean voice perceptual review, physical Meta Quest audio-spatialization QA, secure TTS proxy deployment, and psychometric validation of the authored ECD model.
