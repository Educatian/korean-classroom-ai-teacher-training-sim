using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdieLab.TeacherTraining
{
    [DefaultExecutionOrder(-1000)]
    public sealed class SimulationController : MonoBehaviour
    {
        [SerializeField] private TrainingHud hud;
        [SerializeField] private NpcPerformance focalStudent;
        [SerializeField] private NpcPerformance[] classmates;
        [SerializeField] private GenerativeAiCoach aiCoach;
        [SerializeField] private SecureProxyLlmGateway secureProxyCoach;
        [SerializeField] private TeacherCameraController teacherCamera;
        [SerializeField] private TrainingModeNavigator modeNavigator;
        [SerializeField] private bool circleDiscussionScenario;
        [SerializeField] private bool recoveryRoomScenario;
        [SerializeField] private EcdAssessmentModel ecdAssessmentModel;

        private ScenarioBeat[] beats;
        private TrainingScenarioAsset scenarioAsset;
        private string sessionId;
        private string sessionStartedAtUtc;
        private int beatIndex;
        private int score;
        private int dialogueRequestId;
        private int telemetrySequence;
        private float studentTrust = 0.2f;
        private float studentEngagement = 0.2f;
        private int turnsInCurrentBeat;
        private int pendingNextBeatIndex = -1;
        private ScenarioTransitionReason pendingTransitionReason = ScenarioTransitionReason.Hold;
        private CrisisStage[] crisisStages;
        private TrainingTurnCoordinator turnCoordinator;
        private readonly ConversationSessionState conversationState = new ConversationSessionState(8);
        private readonly List<TeacherResponseOption> selectedResponses = new List<TeacherResponseOption>();
        private PrebriefPanel prebriefPanel;
        private NpcPerformance dialogueTarget;
        private Transform focalSpeechAnchor;
        private readonly List<TrainingTelemetryEvent> telemetryEvents = new List<TrainingTelemetryEvent>();
        private NpcSpeechPerformance speechPerformance;
        private TeacherEyeTrackingRecorder eyeTrackingRecorder;
        private ResearchCloudSyncClient researchCloudSync;
        private ResearchAutomaticSessionBootstrap researchBootstrap;

        private bool awaitingContinue =>
            turnCoordinator != null && turnCoordinator.Phase == TrainingPhase.ReviewingFeedback;
        private bool sessionComplete =>
            turnCoordinator != null &&
            (turnCoordinator.Phase == TrainingPhase.Complete || turnCoordinator.Phase == TrainingPhase.Aborted);
        private TrainingSceneId ActiveSceneId => recoveryRoomScenario
            ? TrainingSceneId.RecoveryRoom
            : circleDiscussionScenario
                ? TrainingSceneId.CircleDiscussion
                : TrainingSceneId.GeneralClassroom;
        private ILlmGateway LlmGateway =>
            LlmDeploymentPolicy.TransportFor(Application.platform) == LlmTransportMode.SecureProxy
                ? secureProxyCoach
                : aiCoach;

        private void Awake()
        {
            sessionId = Guid.NewGuid().ToString("N");
            sessionStartedAtUtc = DateTime.UtcNow.ToString("O");
            if (secureProxyCoach == null)
            {
                secureProxyCoach = GetComponent<SecureProxyLlmGateway>();
                if (secureProxyCoach == null)
                {
                    secureProxyCoach = gameObject.AddComponent<SecureProxyLlmGateway>();
                }
            }
            researchCloudSync = GetComponent<ResearchCloudSyncClient>();
            if (researchCloudSync == null)
            {
                researchCloudSync = gameObject.AddComponent<ResearchCloudSyncClient>();
            }
            researchBootstrap = GetComponent<ResearchAutomaticSessionBootstrap>();
            if (researchBootstrap == null)
            {
                researchBootstrap = gameObject.AddComponent<ResearchAutomaticSessionBootstrap>();
            }
            if (ecdAssessmentModel == null)
            {
                ecdAssessmentModel = EcdAssessmentModel.LoadDefault();
            }
            scenarioAsset = TrainingScenarioCatalog
                .LoadDefault()
                .ScenarioFor(ActiveSceneId);
            beats = scenarioAsset.BuildRuntimeBeats();
            crisisStages = new CrisisStage[scenarioAsset.AuthoredBeats.Count];
            for (int index = 0; index < crisisStages.Length; index++)
            {
                crisisStages[index] = scenarioAsset.AuthoredBeats[index].Stage;
            }
            turnCoordinator = new TrainingTurnCoordinator(beats.Length);
            speechPerformance = focalStudent.GetComponent<NpcSpeechPerformance>();
            if (speechPerformance == null)
            {
                speechPerformance = focalStudent.gameObject.AddComponent<NpcSpeechPerformance>();
            }
            eyeTrackingRecorder = GetComponent<TeacherEyeTrackingRecorder>();
            if (eyeTrackingRecorder == null)
            {
                eyeTrackingRecorder = gameObject.AddComponent<TeacherEyeTrackingRecorder>();
            }
            eyeTrackingRecorder.Initialize(sessionId, EyeTrackingResearchSettings.LoadDefault(), CaptureStudentState);
            StudentGazeAoiInstaller.Install(focalStudent, "focal-student", true);
            for (int index = 0; index < classmates.Length; index++)
            {
                if (classmates[index] == null || !classmates[index].gameObject.activeInHierarchy)
                {
                    continue;
                }
                StudentGazeAoiInstaller.Install(classmates[index], "classmate-" + index, false);
            }
            StudentGazeAoiInstaller.InstallHud(hud.GetComponentInParent<Canvas>());
            StudentGazeAoiInstaller.InstallNamedSceneTargets();
            hud.OptionSelected += HandleOptionSelected;
            hud.ContinueSelected += HandleContinue;
            hud.TeacherUtteranceSubmitted += HandleTeacherUtterance;
            hud.OptionSelected += _ => prebriefPanel?.Dismiss();
            hud.TeacherUtteranceSubmitted += _ => prebriefPanel?.Dismiss();
        }

        public void ConfigureResearchSession(
            string bearerToken,
            string pseudonymousParticipantCode,
            bool consentToRawGaze)
        {
            secureProxyCoach?.SetSessionAuthorization(bearerToken);
            ConfigureResearchLoggingSession(
                bearerToken,
                pseudonymousParticipantCode,
                consentToRawGaze);
        }

        public void ConfigureResearchLoggingSession(
            string bearerToken,
            string pseudonymousParticipantCode,
            bool consentToRawGaze)
        {
            researchCloudSync?.SetSessionAuthorization(
                bearerToken,
                pseudonymousParticipantCode,
                consentToRawGaze);
            eyeTrackingRecorder?.SetRawGazeConsent(consentToRawGaze);
        }

        public void RegisterResearchLoggingSession()
        {
            CrisisScenarioProfile scenario = TrainingResearchCatalog.ForBeat(ActiveSceneId, 0);
            researchCloudSync?.RegisterActiveSession(
                sessionId,
                scenario.id,
                sessionStartedAtUtc);
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
                // Scene variants (recovery room) deactivate classmates; their Awake never
                // ran, so touching them would null-ref and abort the whole session start.
                if (classmate == null || !classmate.gameObject.activeInHierarchy)
                {
                    continue;
                }
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
            researchBootstrap?.Initialize(this);
            hud.SetDialogueState(false, LlmGateway == null ? "로컬 대화 모드" : LlmGateway.ConfigurationLabel);
            prebriefPanel = PrebriefPanel.Show(
                hud.RootCanvas,
                hud.PrimaryFont,
                TrainingResearchCatalog.ForBeat(ActiveSceneId, 0),
                null);
            HudPanelFolding.Install(hud.RootCanvas, hud.PrimaryFont);
            MinimapSystem.Install(
                hud.RootCanvas,
                hud.PrimaryFont,
                teacherCamera != null ? teacherCamera.GetComponent<Camera>() : Camera.main);
            dialogueTarget = focalStudent;
            focalSpeechAnchor = HeadOf(focalStudent);
            Camera sceneCamera = teacherCamera != null ? teacherCamera.GetComponent<Camera>() : Camera.main;
            StudentDialogueSelector selector = StudentDialogueSelector.Install(sceneCamera, focalStudent, classmates);
            if (selector != null)
            {
                selector.StudentClicked += HandleStudentClicked;
            }
        }

        private void HandleStudentClicked(NpcPerformance student)
        {
            if (student == null)
            {
                return;
            }

            prebriefPanel?.Dismiss();
            dialogueTarget = student;
            if (student == focalStudent)
            {
                if (focalSpeechAnchor != null)
                {
                    hud.SetSpeechTarget(focalSpeechAnchor);
                }
                hud.SetDialogueState(true, "집중 학생에게 말하기 · 대응이 평가에 반영됩니다");
                return;
            }

            hud.SetSpeechTarget(HeadOf(student));
            hud.SetDialogueState(
                true,
                AmbientPersonaChat.DisplayNameFor(student.gameObject.name) + "에게 말하기 · 자유 대화");
        }

        private static Transform HeadOf(NpcPerformance student)
        {
            if (student == null)
            {
                return null;
            }

            Animator animator = student.GetComponentInChildren<Animator>();
            Transform head = animator != null && animator.isHuman
                ? animator.GetBoneTransform(HumanBodyBones.Head)
                : null;
            return head != null ? head : student.transform;
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
                    QueueAbortedResearchSession();
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

        private void OnApplicationPause(bool paused)
        {
            if (paused && turnCoordinator != null && !sessionComplete)
            {
                QueueAbortedResearchSession();
            }
        }

        private void HandleTeacherUtterance(string utterance)
        {
            // A clicked classmate receives a persona-flavored ambient reply outside the
            // assessed crisis flow; only the focal student enters the scored pipeline.
            if (dialogueTarget != null && dialogueTarget != focalStudent)
            {
                string speakerName = AmbientPersonaChat.DisplayNameFor(dialogueTarget.gameObject.name);
                dialogueTarget.SetGesture(BehaviorGesture.Listen, 0.5f);
                hud.SetSpeechTarget(HeadOf(dialogueTarget));
                hud.ShowAmbientReply(speakerName, AmbientPersonaChat.ReplyFor(speakerName, utterance));
                return;
            }

            if (eyeTrackingRecorder.RequiresLiveEyeTracking && !eyeTrackingRecorder.ResearchReady)
            {
                hud.SetDialogueState(false, "Quest Pro eye tracking permission/calibration required");
                return;
            }

            if (sessionComplete ||
                !turnCoordinator.TrySubmit(
                    TeacherAction.FromUtterance(utterance),
                    out TrainingRequestToken token))
            {
                return;
            }

            StudentStateSnapshot stateBefore = CaptureStudentState();
            eyeTrackingRecorder.MarkTeacherAction(token.Value.ToString());
            DateTime requestStartedUtc = DateTime.UtcNow;

            teacherCamera?.EnterConversationFocus();
            focalStudent.SetGesture(BehaviorGesture.Listen, 0.55f);
            ILlmGateway gateway = LlmGateway;
            hud.SetDialogueState(true, gateway != null && gateway.IsConfigured ? "학생이 답을 생각하고 있습니다…" : "로컬 학생 반응 생성 중…");
            if (gateway != null && gateway.IsConfigured)
            {
                int requestId = ++dialogueRequestId;
                StartCoroutine(gateway.RequestStudentTurn(
                    new StudentTurnRequest
                    {
                        teacherUtterance = utterance,
                        conversationContext = conversationState.BuildPromptContext(),
                        scenarioContext = BuildScenarioContext(),
                        crisisStage = scenarioAsset.AuthoredBeats[beatIndex].Stage.ToString(),
                        personaId = scenarioAsset.AuthoredBeats[beatIndex].StudentPersona?.PersonaId ?? string.Empty,
                        currentAffect = focalStudent.CurrentVector
                    },
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
            StudentTurnPerformanceNormalizer.Normalize(turn);
            TeacherResponseOption assessment = TeacherActionEvidenceEvaluator.ForUtterance(utterance);
            DialogueSignals signals = LlmContractValidator.TryAcceptSignals(
                turn.dialogueSignals,
                out DialogueSignals acceptedSignals)
                ? acceptedSignals
                : DialogueSignals.Neutral;
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
            conversationState.AddTurn(utterance, turn.studentReply, signals);
            turnsInCurrentBeat++;
            ScenarioTransitionDecision decision = ScenarioTransitionEngine.Select(
                new ScenarioTransitionContext(beatIndex, turnsInCurrentBeat, crisisStages, signals));
            pendingNextBeatIndex = decision.Reason == ScenarioTransitionReason.Hold
                ? -1
                : decision.NextBeatIndex;
            pendingTransitionReason = decision.Reason;
            speechPerformance.Speak(turn.studentReply, turn.actionUnits, affect);
            bool conversationalEyeContact = gesture == BehaviorGesture.Recover || gesture == BehaviorGesture.Listen;
            focalStudent.SetUprightEyeContact(conversationalEyeContact);
            teacherCamera?.SetUprightFocus(conversationalEyeContact);
            hud.SetSpeechBubbleAvoidsFace(conversationalEyeContact);
            hud.ShowStudentTurn(utterance, turn.studentReply, affect);
            string source = fromLlm
                ? $"LLM 학생 응답 · 동적 전환 {TransitionLabel(pendingTransitionReason)} · F: 교실 시점"
                : $"로컬 학생 응답 · 동적 전환 {TransitionLabel(pendingTransitionReason)} · F: 교실 시점";
            if (!string.IsNullOrWhiteSpace(error))
            {
                source += " · LLM 연결 실패 후 대체됨";
                Debug.LogWarning($"Student LLM fallback: {error}");
            }

            source += " · " + StudentSpeechSynthesizer.VoiceDisclosure;
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
            if (fromLlm && LlmGateway != null && LlmGateway.IsConfigured)
            {
                RequestRubricEvaluation(utterance, turn.studentReply, assessment, beatIndex, token);
            }
        }

        private string BuildScenarioContext()
        {
            ScenarioBeatAuthoringData authored = scenarioAsset.AuthoredBeats[beatIndex];
            return string.Concat(
                beats[beatIndex].observation,
                "\ntrigger: ", authored.Trigger,
                "\nscene: ", ActiveSceneId,
                "\npeer_attention: ", authored.PeerAttention,
                "\npresentation_avoidance: ", authored.PresentationAvoidance);
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
                    gesture = BehaviorGesture.Recover.ToString(),
                    dialogueSignals = new DialogueSignals
                    {
                        feltHeard = 0.82f,
                        perceivedPressure = 0.12f,
                        choiceOffered = 0.62f,
                        readyForReentry = 0.68f
                    }
                }
                : new StudentAgentTurn
                {
                    studentReply = "지금은 계속 묻지 말아 주세요. 아직 너무 답답해요.",
                    valence = Mathf.MoveTowards(current.valence, -0.72f, 0.18f),
                    arousal = Mathf.MoveTowards(current.arousal, 0.82f, 0.16f),
                    dominance = Mathf.MoveTowards(current.dominance, 0.24f, 0.14f),
                    gesture = BehaviorGesture.Protest.ToString(),
                    dialogueSignals = new DialogueSignals
                    {
                        feltHeard = 0.12f,
                        perceivedPressure = 0.78f,
                        choiceOffered = 0.05f,
                        readyForReentry = 0.08f
                    }
                };
        }

        private void HandleOptionSelected(int optionIndex)
        {
            if (eyeTrackingRecorder.RequiresLiveEyeTracking && !eyeTrackingRecorder.ResearchReady)
            {
                hud.SetDialogueState(false, "Quest Pro eye tracking permission/calibration required");
                return;
            }

            if (optionIndex < 0 ||
                optionIndex >= beats[beatIndex].options.Length ||
                !turnCoordinator.TrySubmit(
                    TeacherAction.FromChoice(optionIndex),
                    out TrainingRequestToken token))
            {
                return;
            }
            eyeTrackingRecorder.MarkTeacherAction(token.Value.ToString());

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
            int previousBeatIndex = beatIndex;
            int targetBeatIndex = pendingNextBeatIndex >= 0
                ? pendingNextBeatIndex
                : beatIndex + 1;
            if (!turnCoordinator.TryAdvanceTo(targetBeatIndex, out bool complete))
            {
                return;
            }

            dialogueRequestId++;
            speechPerformance?.StopSpeaking();
            beatIndex = turnCoordinator.BeatIndex;
            pendingNextBeatIndex = -1;
            pendingTransitionReason = ScenarioTransitionReason.Hold;
            if (beatIndex != previousBeatIndex)
            {
                turnsInCurrentBeat = 0;
            }
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
                ShowResearchDebrief();
                return;
            }

            PresentBeat();
            RecordLifecycleEvent(
                TrainingEventKind.BeatPresented,
                TrainingPhase.PresentingScenario,
                TrainingPhase.AwaitingTeacherAction);
            hud.SetDialogueState(false, LlmGateway == null ? "로컬 대화 모드" : LlmGateway.ConfigurationLabel);
        }

        private void PresentBeat()
        {
            ScenarioBeat beat = beats[beatIndex];
            if (beat.gestureIntensity > 0f)
            {
                focalStudent.SetGesture(beat.entryGesture, beat.gestureIntensity);
            }
            CrisisScenarioProfile researchScenario = TrainingResearchCatalog.ForBeat(ActiveSceneId, beatIndex);
            eyeTrackingRecorder.BeginCue(researchScenario.id, beatIndex);
            hud.ShowBeat(beatIndex, beats.Length, beat, score, beatIndex);
        }

        private void RequestRubricEvaluation(
            string teacherUtterance,
            string studentReply,
            TeacherResponseOption provisionalAssessment,
            int requestBeatIndex,
            TrainingRequestToken token)
        {
            var request = new TeacherRubricRequest
            {
                teacherUtterance = teacherUtterance,
                studentReply = studentReply,
                scenarioContext = beats[requestBeatIndex].observation
            };
            StartCoroutine(LlmGateway.RequestTeacherRubric(
                request,
                rubric =>
                {
                    provisionalAssessment.competencyEvidence = rubric.ToEvidence();
                    provisionalAssessment.rationale = rubric.improvementSuggestion;
                    RecordRubricEvaluation(requestBeatIndex, token, rubric);
                    if (beatIndex == requestBeatIndex && awaitingContinue && !sessionComplete)
                    {
                        hud.AppendAiCoachFeedback(
                            $"LLM 루브릭(검증 신뢰도 {rubric.confidence:P0}) · {rubric.improvementSuggestion}");
                    }
                    // A rubric that arrives after session completion is recorded in telemetry
                    // only; re-running the debrief here would re-queue the upload bundle and
                    // re-open the dashboard from an async callback.
                },
                error => Debug.LogWarning($"Teacher rubric request failed: {error}")));
        }

        private void RecordRubricEvaluation(
            int requestBeatIndex,
            TrainingRequestToken token,
            TeacherRubricResult rubric)
        {
            CrisisScenarioProfile scenario = TrainingResearchCatalog.ForBeat(ActiveSceneId, requestBeatIndex);
            StudentStateSnapshot state = CaptureStudentState();
            AppendTelemetry(new TrainingTelemetryEvent
            {
                eventId = Guid.NewGuid().ToString(),
                sessionId = sessionId,
                sequence = telemetrySequence++,
                timestampUtc = DateTime.UtcNow.ToString(new string(new[] { 'O' })),
                scenarioId = scenario.id,
                beatIndex = requestBeatIndex,
                kind = TrainingEventKind.RubricEvaluation,
                phaseBefore = TrainingPhase.ReviewingFeedback,
                phaseAfter = TrainingPhase.ReviewingFeedback,
                actionId = token.Value.ToString(),
                actionSource = TrainingActionSource.GenerativeModel,
                turnRoute = StudentTurnRoute.OpenRouter,
                turnOutcome = StudentTurnOutcome.Accepted,
                coachSuggestion = rubric.improvementSuggestion ?? string.Empty,
                studentStateBefore = state,
                studentStateAfter = state,
                inference = new ModelPromptProvenance
                {
                    modelId = LlmGateway?.ModelId ?? string.Empty,
                    promptTemplateId = nameof(TeacherRubricResult),
                    promptVersion = LlmGateway?.PromptVersion ?? 0,
                    promptHash = TextHash(string.Concat(scenario.id, token.Value.ToString())),
                    fallbackUsed = false
                },
                competencyEvidence = rubric.ToEvidence()
            });
        }


        private void QueueAbortedResearchSession()
        {
            ResearchDebriefReport report = EcdAssessmentEngine.Evaluate(
                telemetryEvents,
                ecdAssessmentModel);
            CrisisScenarioProfile scenario = TrainingResearchCatalog.ForBeat(ActiveSceneId, 0);
            researchCloudSync?.QueueAbortedSession(
                sessionId,
                scenario.id,
                sessionStartedAtUtc,
                telemetryEvents,
                report,
                eyeTrackingRecorder != null ? eyeTrackingRecorder.RawDataPath : string.Empty);
        }

        private void ShowResearchDebrief()
        {
            ResearchDebriefReport report = EcdAssessmentEngine.Evaluate(
                telemetryEvents,
                ecdAssessmentModel);
            modeNavigator?.ShowResearchDebrief(report, RestartSession);
            CrisisScenarioProfile scenario = TrainingResearchCatalog.ForBeat(ActiveSceneId, 0);
            researchCloudSync?.QueueCompletedSession(
                sessionId,
                scenario.id,
                sessionStartedAtUtc,
                telemetryEvents,
                report,
                eyeTrackingRecorder != null ? eyeTrackingRecorder.RawDataPath : string.Empty);
        }

        private void RestartSession()
        {
            dialogueRequestId++;
            speechPerformance?.StopSpeaking();
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.buildIndex);
        }
        private static string TransitionLabel(ScenarioTransitionReason reason)
        {
            return reason switch
            {
                ScenarioTransitionReason.SafetyOverride => "안전 우선",
                ScenarioTransitionReason.SupportiveDeescalation => "진정 단계",
                ScenarioTransitionReason.PressureEscalation => "긴장 상승",
                ScenarioTransitionReason.ReadyForReentry => "수업 복귀",
                ScenarioTransitionReason.MaximumTurnsReached => "다음 단계",
                _ => "현재 단계 유지"
            };
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
            TeacherGazeSummary gazeSummary =
                eyeTrackingRecorder.TakeSummary(token.Value.ToString());
            var inference = new ModelPromptProvenance
            {
                modelId = LlmGateway == null ? string.Empty : LlmGateway.ModelId,
                promptTemplateId = nameof(GenerativeAiCoach),
                promptVersion = LlmGateway == null ? 0 : LlmGateway.PromptVersion,
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
                competencyEvidence = evidence ?? Array.Empty<CompetencyEvidence>(),
                gaze = gazeSummary
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
                competencyEvidence = Array.Empty<CompetencyEvidence>(),
                studentSpeech = speechPerformance?.CaptureTelemetry() ?? new StudentSpeechTelemetry(),
                gaze = gazeSummary
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
            try
            {
                File.AppendAllText(path, JsonUtility.ToJson(record) + Environment.NewLine);
            }
            catch (Exception exception) when (
                exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning(exception.GetType().Name);
            }
        }

    }
}
