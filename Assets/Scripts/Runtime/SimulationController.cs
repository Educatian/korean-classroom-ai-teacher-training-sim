using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    [DefaultExecutionOrder(-1000)]
    public sealed class SimulationController : MonoBehaviour
    {
        [SerializeField] private TrainingHud hud;
        [SerializeField] private NpcPerformance focalStudent;
        [SerializeField] private NpcPerformance[] classmates;
        [SerializeField] private GenerativeAiCoach aiCoach;
        [SerializeField] private TeacherCameraController teacherCamera;
        [SerializeField] private TrainingModeNavigator modeNavigator;
        [SerializeField] private bool circleDiscussionScenario;

        private ScenarioBeat[] beats;
        private string sessionId;
        private int beatIndex;
        private int score;
        private int dialogueRequestId;
        private int telemetrySequence;
        private float studentTrust = 0.2f;
        private float studentEngagement = 0.2f;
        private TrainingTurnCoordinator turnCoordinator;
        private readonly ConversationMemory conversationMemory = new ConversationMemory(5);
        private readonly List<TeacherResponseOption> selectedResponses = new List<TeacherResponseOption>();
        private readonly List<TrainingTelemetryEvent> telemetryEvents = new List<TrainingTelemetryEvent>();
        private NpcSpeechPerformance speechPerformance;

        private bool awaitingContinue =>
            turnCoordinator != null && turnCoordinator.Phase == TrainingPhase.ReviewingFeedback;
        private bool sessionComplete =>
            turnCoordinator != null &&
            (turnCoordinator.Phase == TrainingPhase.Complete || turnCoordinator.Phase == TrainingPhase.Aborted);
        private TrainingSceneId ActiveSceneId => circleDiscussionScenario
            ? TrainingSceneId.CircleDiscussion
            : TrainingSceneId.GeneralClassroom;

        private void Awake()
        {
            sessionId = Guid.NewGuid().ToString("N");
            beats = circleDiscussionScenario
                ? TrainingScenarioLibrary.BuildCircleDiscussionScenario()
                : TrainingScenarioLibrary.BuildDefaultScenario();
            turnCoordinator = new TrainingTurnCoordinator(beats.Length);
            speechPerformance = focalStudent.GetComponent<NpcSpeechPerformance>();
            if (speechPerformance == null)
            {
                speechPerformance = focalStudent.gameObject.AddComponent<NpcSpeechPerformance>();
            }
            hud.OptionSelected += HandleOptionSelected;
            hud.ContinueSelected += HandleContinue;
            hud.TeacherUtteranceSubmitted += HandleTeacherUtterance;
        }

        private void Start()
        {
            focalStudent.SetAffect(StudentAffect.Distressed, true);
            focalStudent.SetGesture(BehaviorGesture.Listen, 0.42f);
            BehaviorGesture[] attentiveGestures =
            {
                BehaviorGesture.Neutral,
                BehaviorGesture.Listen,
                BehaviorGesture.Recover,
                BehaviorGesture.Protest,
                BehaviorGesture.Point,
                BehaviorGesture.Defiant,
                BehaviorGesture.PushAway
            };
            int attentiveIndex = 0;
            for (int i = 0; i < classmates.Length; i++)
            {
                NpcPerformance classmate = classmates[i];
                classmate.SetAffect(i % 3 == 0 ? StudentAffect.Uneasy : StudentAffect.Calm, true);
                float initialHold = 5.2f + (i % 7) * 0.37f;
                StudentGazeController gaze = classmate.GetComponent<StudentGazeController>();
                BehaviorGesture ambientGesture = attentiveGestures[attentiveIndex++ % attentiveGestures.Length];
                float gestureIntensity = ambientGesture == BehaviorGesture.AvoidGaze
                    ? 0.58f
                    : 0.16f + (i % 4) * 0.08f;
                classmate.SetAmbientGesture(
                    ambientGesture,
                    gestureIntensity,
                    initialHold);
            }

            PresentBeat();
            turnCoordinator.Start();
            RecordLifecycleEvent(
                TrainingEventKind.SessionStarted,
                TrainingPhase.PresentingScenario,
                turnCoordinator.Phase);
            hud.SetDialogueState(false, aiCoach == null ? "로컬 대화 모드" : aiCoach.ConfigurationLabel);
        }

        private void OnDestroy()
        {
            speechPerformance?.StopSpeaking();
            if (turnCoordinator != null && !sessionComplete)
            {
                TrainingPhase previous = turnCoordinator.Phase;
                if (turnCoordinator.TryAbort())
                {
                    RecordLifecycleEvent(
                        TrainingEventKind.SessionAborted,
                        previous,
                        TrainingPhase.Aborted);
                }
            }

            if (hud == null)
            {
                return;
            }

            hud.OptionSelected -= HandleOptionSelected;
            hud.ContinueSelected -= HandleContinue;
            hud.TeacherUtteranceSubmitted -= HandleTeacherUtterance;
        }

        private void HandleTeacherUtterance(string utterance)
        {
            if (sessionComplete ||
                !turnCoordinator.TrySubmit(
                    TeacherAction.FromUtterance(utterance),
                    out TrainingRequestToken token))
            {
                return;
            }

            StudentStateSnapshot stateBefore = CaptureStudentState();
            DateTime requestStartedUtc = DateTime.UtcNow;

            teacherCamera?.EnterConversationFocus();
            focalStudent.SetGesture(BehaviorGesture.Listen, 0.55f);
            hud.SetDialogueState(true, aiCoach != null && aiCoach.IsConfigured ? "학생이 답을 생각하고 있습니다…" : "로컬 학생 반응 생성 중…");
            if (aiCoach != null && aiCoach.IsConfigured)
            {
                int requestId = ++dialogueRequestId;
                StartCoroutine(aiCoach.RequestStudentTurn(
                    utterance,
                    conversationMemory.BuildContext(),
                    focalStudent.CurrentVector,
                    turn =>
                    {
                        if (requestId == dialogueRequestId && turnCoordinator.TryResolve(token))
                        {
                            ApplyStudentTurn(
                                utterance,
                                turn,
                                true,
                                stateBefore,
                                token,
                                requestStartedUtc);
                        }
                    },
                    error =>
                    {
                        if (requestId == dialogueRequestId && turnCoordinator.TryResolve(token))
                        {
                            ApplyStudentTurn(
                                utterance,
                                BuildLocalTurn(utterance),
                                false,
                                stateBefore,
                                token,
                                requestStartedUtc,
                                error);
                        }
                    }));
                return;
            }

            if (turnCoordinator.TryResolve(token))
            {
                ApplyStudentTurn(
                    utterance,
                    BuildLocalTurn(utterance),
                    false,
                    stateBefore,
                    token,
                    requestStartedUtc);
            }
        }

        private void ApplyStudentTurn(
            string utterance,
            StudentAgentTurn turn,
            bool fromLlm,
            StudentStateSnapshot stateBefore,
            TrainingRequestToken token,
            DateTime requestStartedUtc,
            string error = null)
        {
            TeacherResponseOption assessment = TeacherActionEvidenceEvaluator.ForUtterance(utterance);
            BehaviorGesture gesture = Enum.TryParse(turn.gesture, true, out BehaviorGesture parsed)
                ? parsed
                : BehaviorGesture.Fidget;
            AffectVector affect = new AffectVector(turn.valence, turn.arousal, turn.dominance);
            focalStudent.SetAffectVector(affect, gesture);
            selectedResponses.Add(assessment);
            score = ResponseScorer.AddResponse(score, assessment);
            UpdateResearchProgress(assessment.quality);
            foreach (NpcPerformance classmate in classmates)
            {
                classmate.SetAffect(
                    assessment.quality >= 2 ? StudentAffect.Recovering : StudentAffect.Uneasy);
            }
            conversationMemory.Add(utterance, turn.studentReply);
            speechPerformance.Speak(turn.studentReply, turn.actionUnits);
            bool conversationalEyeContact = gesture == BehaviorGesture.Recover || gesture == BehaviorGesture.Listen;
            focalStudent.SetUprightEyeContact(conversationalEyeContact);
            teacherCamera?.SetUprightFocus(conversationalEyeContact);
            hud.SetSpeechBubbleAvoidsFace(conversationalEyeContact);
            hud.ShowStudentTurn(utterance, turn.studentReply, affect);
            string source = fromLlm ? "LLM 학생 응답 · 계속 말하기 가능 · F: 교실 시점" : "로컬 학생 응답 · 계속 말하기 가능 · F: 교실 시점";
            if (!string.IsNullOrWhiteSpace(error))
            {
                source += " · LLM 연결 실패 후 대체됨";
                Debug.LogWarning($"Student LLM fallback: {error}");
            }

            hud.SetDialogueState(false, source);
            hud.ShowFeedback(assessment, score, beatIndex + 1);
            RecordResolvedTurn(
                TrainingActionSource.TeacherUtterance,
                utterance,
                turn.studentReply,
                stateBefore,
                CaptureStudentState(),
                assessment.competencyEvidence,
                token,
                fromLlm,
                requestStartedUtc,
                error);
            AppendRecord(-1, assessment);
        }

        private StudentAgentTurn BuildLocalTurn(string utterance)
        {
            string normalized = utterance.ToLowerInvariant();
            bool supportive = normalized.Contains("괜찮") || normalized.Contains("쉬") ||
                              normalized.Contains("천천히") || normalized.Contains("선택") ||
                              normalized.Contains("도와") || normalized.Contains("알겠");
            AffectVector current = focalStudent.CurrentVector;
            return supportive
                ? new StudentAgentTurn
                {
                    studentReply = "…네. 잠깐만 시간을 주시면 다시 이야기해 볼게요.",
                    valence = Mathf.MoveTowards(current.valence, 0.18f, 0.35f),
                    arousal = Mathf.MoveTowards(current.arousal, 0.28f, 0.34f),
                    dominance = Mathf.MoveTowards(current.dominance, 0.05f, 0.24f),
                    gesture = BehaviorGesture.Recover.ToString()
                }
                : new StudentAgentTurn
                {
                    studentReply = "지금은 계속 묻지 말아 주세요. 아직 너무 답답해요.",
                    valence = Mathf.MoveTowards(current.valence, -0.72f, 0.18f),
                    arousal = Mathf.MoveTowards(current.arousal, 0.82f, 0.16f),
                    dominance = Mathf.MoveTowards(current.dominance, 0.24f, 0.14f),
                    gesture = BehaviorGesture.Protest.ToString()
                };
        }

        private void HandleOptionSelected(int optionIndex)
        {
            if (optionIndex < 0 ||
                optionIndex >= beats[beatIndex].options.Length ||
                !turnCoordinator.TrySubmit(
                    TeacherAction.FromChoice(optionIndex),
                    out TrainingRequestToken token))
            {
                return;
            }

            StudentStateSnapshot stateBefore = CaptureStudentState();
            if (!turnCoordinator.TryResolve(token))
            {
                return;
            }

            TeacherResponseOption option = beats[beatIndex].options[optionIndex];
            CrisisScenarioProfile scenario = TrainingResearchCatalog.ForBeat(ActiveSceneId, beatIndex);
            option.competencyEvidence = TeacherActionEvidenceEvaluator.ForChoice(scenario, option);
            selectedResponses.Add(option);
            score = ResponseScorer.AddResponse(score, option);
            UpdateResearchProgress(option.quality);
            focalStudent.SetAffect(option.resultingAffect);
            foreach (NpcPerformance classmate in classmates)
            {
                classmate.SetAffect(option.quality >= 2 ? StudentAffect.Recovering : StudentAffect.Uneasy);
            }

            hud.ShowFeedback(option, score, beatIndex + 1);
            RecordResolvedTurn(
                TrainingActionSource.TeacherChoice,
                option.spokenResponse,
                beats[beatIndex].studentLine,
                stateBefore,
                CaptureStudentState(),
                option.competencyEvidence,
                token,
                false,
                DateTime.UtcNow,
                null);
            AppendRecord(optionIndex, option);
            if (aiCoach != null && aiCoach.IsConfigured)
            {
                int requestBeat = beatIndex;
                StartCoroutine(aiCoach.RequestFeedback(beats[requestBeat], option, feedback =>
                {
                    if (beatIndex == requestBeat && awaitingContinue && !sessionComplete)
                    {
                        hud.AppendAiCoachFeedback(feedback);
                    }
                }));
            }
        }

        private void HandleContinue()
        {
            if (!turnCoordinator.TryAdvance(out bool complete))
            {
                return;
            }

            dialogueRequestId++;
            speechPerformance?.StopSpeaking();
            beatIndex = turnCoordinator.BeatIndex;
            if (complete)
            {
                focalStudent.SetAffect(StudentAffect.Recovering);
                focalStudent.SetUprightEyeContact(true);
                teacherCamera?.SetUprightFocus(true);
                hud.ShowCompletion(score, beats.Length * 3);
                RecordLifecycleEvent(
                    TrainingEventKind.SessionCompleted,
                    TrainingPhase.ReviewingFeedback,
                    TrainingPhase.Complete);
                modeNavigator?.ShowDebrief(TeacherRubricEvaluator.Evaluate(telemetryEvents));
                return;
            }

            PresentBeat();
            RecordLifecycleEvent(
                TrainingEventKind.BeatPresented,
                TrainingPhase.PresentingScenario,
                TrainingPhase.AwaitingTeacherAction);
            hud.SetDialogueState(false, aiCoach == null ? "로컬 대화 모드" : aiCoach.ConfigurationLabel);
        }

        private void PresentBeat()
        {
            ScenarioBeat beat = beats[beatIndex];
            if (beat.gestureIntensity > 0f)
            {
                focalStudent.SetGesture(beat.entryGesture, beat.gestureIntensity);
            }
            hud.ShowBeat(beatIndex, beats.Length, beat, score, beatIndex);
        }

        private void UpdateResearchProgress(int quality)
        {
            float normalized = Mathf.Clamp(quality, 0, 3) / 3f;
            studentTrust = Mathf.Clamp01(studentTrust + Mathf.Lerp(-0.08f, 0.12f, normalized));
            studentEngagement = Mathf.Clamp01(
                studentEngagement + Mathf.Lerp(-0.06f, 0.10f, normalized));
        }

        private StudentStateSnapshot CaptureStudentState()
        {
            if (focalStudent == null)
            {
                return new StudentStateSnapshot
                {
                    engagement = studentEngagement,
                    trust = studentTrust
                };
            }

            bool gaze = focalStudent.UprightEyeContact ||
                        focalStudent.CurrentGesture == BehaviorGesture.Listen ||
                        focalStudent.CurrentGesture == BehaviorGesture.Recover;
            return new StudentStateSnapshot
            {
                affect = focalStudent.TargetVector,
                gesture = focalStudent.CurrentGesture,
                gestureIntensity = focalStudent.TargetVector.arousal,
                gazeContact = gaze ? 1f : 0.25f,
                engagement = studentEngagement,
                trust = studentTrust
            };
        }

        private void RecordResolvedTurn(
            TrainingActionSource teacherSource,
            string teacherText,
            string studentReply,
            StudentStateSnapshot stateBefore,
            StudentStateSnapshot stateAfter,
            CompetencyEvidence[] evidence,
            TrainingRequestToken token,
            bool fromLlm,
            DateTime requestStartedUtc,
            string error)
        {
            CrisisScenarioProfile scenario = TrainingResearchCatalog.ForBeat(ActiveSceneId, beatIndex);
            string teacherHash = TextHash(teacherText);
            string replyHash = TextHash(studentReply);
            var inference = new ModelPromptProvenance
            {
                modelId = aiCoach == null ? string.Empty : aiCoach.ModelId,
                promptTemplateId = nameof(GenerativeAiCoach),
                promptVersion = aiCoach == null ? 0 : aiCoach.PromptVersion,
                promptHash = TextHash(string.Concat(scenario.id, beatIndex.ToString(), teacherHash)),
                fallbackUsed = teacherSource == TrainingActionSource.TeacherUtterance && !fromLlm,
                fallbackReason = error ?? string.Empty,
                latencyMilliseconds = Math.Max(
                    0L,
                    (long)(DateTime.UtcNow - requestStartedUtc).TotalMilliseconds)
            };
            StudentSafetyCategory safetyCategory = StudentSafetyCategory.None;
            if (!string.IsNullOrWhiteSpace(error))
            {
                Enum.TryParse(error, true, out safetyCategory);
            }

            StudentTurnRoute route = fromLlm
                ? StudentTurnRoute.OpenRouter
                : teacherSource == TrainingActionSource.TeacherChoice
                    ? StudentTurnRoute.ScriptedScenario
                    : StudentTurnRoute.LocalFallback;
            StudentTurnOutcome outcome = safetyCategory != StudentSafetyCategory.None
                ? StudentTurnOutcome.Unsafe
                : string.IsNullOrWhiteSpace(error)
                    ? StudentTurnOutcome.Accepted
                    : StudentTurnOutcome.Malformed;
            AppendTelemetry(new TrainingTelemetryEvent
            {
                eventId = Guid.NewGuid().ToString(),
                sessionId = sessionId,
                sequence = telemetrySequence++,
                timestampUtc = DateTime.UtcNow.ToString(new string(new[] { 'O' })),
                scenarioId = scenario.id,
                beatIndex = beatIndex,
                kind = TrainingEventKind.TeacherAction,
                phaseBefore = TrainingPhase.AwaitingTeacherAction,
                phaseAfter = TrainingPhase.AwaitingStudentResponse,
                actionId = token.Value.ToString(),
                actionSource = teacherSource,
                teacherTextLength = teacherText == null ? 0 : teacherText.Length,
                teacherTextHash = teacherHash,
                studentReplyHash = string.Empty,
                turnRoute = route,
                turnOutcome = outcome,
                studentStateBefore = stateBefore,
                studentStateAfter = stateBefore,
                inference = inference,
                competencyEvidence = evidence ?? Array.Empty<CompetencyEvidence>()
            });
            AppendTelemetry(new TrainingTelemetryEvent
            {
                eventId = Guid.NewGuid().ToString(),
                sessionId = sessionId,
                sequence = telemetrySequence++,
                timestampUtc = DateTime.UtcNow.ToString(new string(new[] { 'O' })),
                scenarioId = scenario.id,
                beatIndex = beatIndex,
                kind = TrainingEventKind.StudentResponse,
                phaseBefore = TrainingPhase.AwaitingStudentResponse,
                phaseAfter = TrainingPhase.ReviewingFeedback,
                actionId = token.Value.ToString(),
                actionSource = fromLlm
                    ? TrainingActionSource.GenerativeModel
                    : teacherSource == TrainingActionSource.TeacherChoice
                        ? TrainingActionSource.ScriptedScenario
                        : TrainingActionSource.LocalFallback,
                teacherTextLength = teacherText == null ? 0 : teacherText.Length,
                teacherTextHash = teacherHash,
                studentReplyHash = replyHash,
                turnRoute = route,
                turnOutcome = outcome,
                studentStateBefore = stateBefore,
                studentStateAfter = stateAfter,
                inference = inference,
                competencyEvidence = Array.Empty<CompetencyEvidence>()
            });
        }

        private void RecordLifecycleEvent(
            TrainingEventKind kind,
            TrainingPhase phaseBefore,
            TrainingPhase phaseAfter)
        {
            StudentStateSnapshot state = CaptureStudentState();
            CrisisScenarioProfile scenario = TrainingResearchCatalog.ForBeat(ActiveSceneId, beatIndex);
            AppendTelemetry(new TrainingTelemetryEvent
            {
                eventId = Guid.NewGuid().ToString(),
                sessionId = sessionId,
                sequence = telemetrySequence++,
                timestampUtc = DateTime.UtcNow.ToString(new string(new[] { 'O' })),
                scenarioId = scenario.id,
                beatIndex = beatIndex,
                kind = kind,
                phaseBefore = phaseBefore,
                phaseAfter = phaseAfter,
                actionSource = TrainingActionSource.System,
                studentStateBefore = state,
                studentStateAfter = state
            });
        }

        private void AppendTelemetry(TrainingTelemetryEvent trainingEvent)
        {
            telemetryEvents.Add(trainingEvent);
#if !UNITY_WEBGL
            string fileName = new string(new[]
            {
                't', 'e', 'a', 'c', 'h', 'e', 'r', '_', 't', 'r', 'a', 'i', 'n', 'i', 'n', 'g', '_',
                't', 'e', 'l', 'e', 'm', 'e', 't', 'r', 'y', '.', 'j', 's', 'o', 'n', 'l'
            });
            string path = Path.Combine(Application.persistentDataPath, fileName);
            try
            {
                File.AppendAllText(path, JsonUtility.ToJson(trainingEvent) + Environment.NewLine);
            }
            catch (Exception exception) when (
                exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning(exception.GetType().Name);
            }
#endif
        }

        private static string TextHash(string value)
        {
            return Hash128.Compute(value ?? string.Empty).ToString();
        }

        private void AppendRecord(int selectedOption, TeacherResponseOption option)
        {
            TrainingRecord record = new TrainingRecord
            {
                sessionId = sessionId,
                timestampUtc = DateTime.UtcNow.ToString("O"),
                beatIndex = beatIndex,
                beatTitle = beats[beatIndex].title,
                selectedOption = selectedOption,
                quality = option.quality,
                cumulativeScore = score
            };

            string path = Path.Combine(Application.persistentDataPath, "teacher_training_sessions.jsonl");
            File.AppendAllText(path, JsonUtility.ToJson(record) + Environment.NewLine);
        }

        private static ScenarioBeat[] BuildScenario()
        {
            return new[]
            {
                new ScenarioBeat
                {
                    title = "초기 정서 신호 포착",
                    studentLine = "문제 너무 많아요. 더는 못 하겠어요.",
                    observation = "민준은 주먹을 쥐고 시선을 피합니다.\n호흡이 가빠지고 목소리가 커졌습니다.",
                    options = new[]
                    {
                        Option("낮은 목소리로 공간과 시간을 제공한다", "민준아, 지금 많이 벅찬 것 같아. 잠깐 멈추고 여기서 쉬거나 뒤쪽 자리로 이동해도 괜찮아.", 3, "행동을 지적하기 전에 정서 신호를 인정하고 자극을 낮췄습니다. 선택권은 통제감을 회복시키는 데 도움이 됩니다.", StudentAffect.Uneasy),
                        Option("즉시 과제 완료를 요구한다", "수업 중이야. 다른 학생들처럼 지금 끝내.", 0, "공개적 요구와 비교는 위협감을 높여 정서 강도를 키울 수 있습니다.", StudentAffect.Angry),
                        Option("왜 화가 났는지 여러 질문을 한다", "왜 그래? 무슨 일이야? 아침에 무슨 일 있었어?", 1, "관심은 전달되지만 높은 각성 상태에서 연속 질문은 인지 부담을 더할 수 있습니다.", StudentAffect.Distressed)
                    }
                },
                new ScenarioBeat
                {
                    title = "감정 인정과 선택권",
                    studentLine = "그냥 저 좀 내버려 두세요.",
                    observation = "민준의 목소리는 여전히 날카롭지만 교사를 바라보기 시작합니다. 대응의 길이를 짧게 유지해야 합니다.",
                    options = new[]
                    {
                        Option("감정을 인정하고 두 가지 안전한 선택을 제시한다", "알겠어. 지금은 말하지 않아도 돼. 여기서 2분 쉬거나 복도 앞 안정 공간에서 쉬는 것 중 골라도 돼.", 3, "감정을 논박하지 않고 선택 범위를 명확히 제시해 자율성과 안전을 함께 지켰습니다.", StudentAffect.Recovering),
                        Option("교실 밖으로 나가라고 지시한다", "그럴 거면 당장 나가.", 0, "배제처럼 들리는 지시는 수치심과 대립을 강화할 수 있습니다. 이동이 필요해도 지원 목적과 복귀 경로를 함께 말해야 합니다.", StudentAffect.Angry),
                        Option("반 친구들에게 이해해 달라고 설명한다", "얘들아, 민준이가 지금 힘드니까 이해해 줘.", 1, "선의가 있어도 학생의 상태를 공개적으로 드러내 사생활과 존엄을 해칠 수 있습니다.", StudentAffect.Distressed)
                    }
                },
                new ScenarioBeat
                {
                    title = "복귀와 후속 지원 연결",
                    studentLine = "조금 쉬면 다시 할 수 있을 것 같아요.",
                    observation = "어깨 긴장이 낮아지고 손이 펴졌습니다. 짧은 성공 경험과 이후의 비공개 점검이 필요합니다.",
                    options = new[]
                    {
                        Option("작은 복귀 과제와 비공개 후속 점검을 약속한다", "좋아. 돌아오면 첫 두 문제만 같이 시작하자. 수업 뒤에 네가 편한 방식도 잠깐 상의하자.", 3, "복귀 문턱을 낮추고 예측 가능한 후속 지원을 연결했습니다. 학생의 역량과 존엄을 모두 보존합니다.", StudentAffect.Recovering),
                        Option("진정했으니 원래 분량을 모두 하게 한다", "이제 괜찮아졌으니 남은 문제를 전부 해.", 1, "즉시 원래 요구량으로 돌아가면 회복 중인 학생에게 다시 과부하가 생길 수 있습니다.", StudentAffect.Uneasy),
                        Option("오늘 과제를 전부 면제한다", "오늘은 아무것도 안 해도 돼.", 2, "단기 자극은 낮추지만 복귀 구조가 없으면 회피를 강화할 수 있습니다. 작고 달성 가능한 참여 단계를 남기는 편이 좋습니다.", StudentAffect.Recovering)
                    }
                }
            };
        }

        private static TeacherResponseOption Option(
            string label,
            string spoken,
            int quality,
            string rationale,
            StudentAffect affect)
        {
            return new TeacherResponseOption
            {
                label = label,
                spokenResponse = spoken,
                quality = quality,
                rationale = rationale,
                resultingAffect = affect
            };
        }
    }
}
