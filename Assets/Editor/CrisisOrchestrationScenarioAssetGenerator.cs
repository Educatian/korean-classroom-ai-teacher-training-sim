using System;
using System.IO;
using AdieLab.TeacherTraining;
using UnityEditor;
using UnityEngine;

public static class CrisisOrchestrationScenarioAssetGenerator
{
    private const string Folder = "Assets/Resources/Training/Orchestration";
    private const string AssetPath = Folder + "/TeacherDirectedAggression.asset";
    private const string AssessmentPath = Folder + "/CrisisOrchestrationAssessmentModel.asset";
    private const string PolicyPath = Folder + "/CrisisOrchestrationPolicy.asset";

    [MenuItem("Teacher Training/Content/Generate Crisis Orchestration Scenario")]
    public static void Generate()
    {
        Directory.CreateDirectory(Folder);
        EnsureAssessmentAndPolicyAssets();
        CrisisOrchestrationScenarioAsset asset =
            AssetDatabase.LoadAssetAtPath<CrisisOrchestrationScenarioAsset>(AssetPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<CrisisOrchestrationScenarioAsset>();
            AssetDatabase.CreateAsset(asset, AssetPath);
        }

        asset.ConfigureForEditor(
            "teacher-directed-aggression-v1",
            "교사 대상 욕설·물건 던지기와 팀 대응",
            "교사가 자신의 분노나 당황을 숨기거나 평가받지 않으면서 판단 여유를 확보하고, 학생·학급·교사의 안전을 함께 판단하여 지원 요청, 역할 인계, 기록과 회복까지 수행한다.",
            "4교시 개별 과제 시간. 과제가 어렵다고 말하던 학생이 교사의 재안내 직후 필통을 바닥 쪽으로 던지고 큰 목소리로 ‘선생님도 내 말 안 듣잖아요!’라고 말한다. 앞줄 학생 네 명이 멈춰서 상황을 보고 있고, 한 학생은 의자를 뒤로 밀며 통로 쪽으로 움직인다.",
            "이 시나리오는 교사가 학생을 신체적으로 제압하거나 혼자 강제 분리하는 연습이 아니다. 실제 학교의 승인된 안전 절차와 역할 체계를 우선하며, 즉각적 위험이 있으면 학습 목표보다 현장 안전 지침을 따른다.",
            InitialState(),
            Beats());
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("CRISIS_ORCHESTRATION_ASSET_OK " + AssetPath);
    }

    private static void EnsureAssessmentAndPolicyAssets()
    {
        if (AssetDatabase.LoadAssetAtPath<CrisisOrchestrationAssessmentModel>(AssessmentPath) == null)
        {
            CrisisOrchestrationAssessmentModel assessment =
                CrisisOrchestrationAssessmentModel.CreateRuntimeDefault();
            assessment.hideFlags = HideFlags.None;
            AssetDatabase.CreateAsset(assessment, AssessmentPath);
        }

        if (AssetDatabase.LoadAssetAtPath<CrisisOrchestrationPolicy>(PolicyPath) == null)
        {
            CrisisOrchestrationPolicy policy = CrisisOrchestrationPolicy.CreateRuntimeDefault();
            policy.hideFlags = HideFlags.None;
            AssetDatabase.CreateAsset(policy, PolicyPath);
        }
    }

    private static CrisisOrchestrationState InitialState()
    {
        return new CrisisOrchestrationState
        {
            phase = CrisisOrchestrationPhase.Assess,
            teacher = new TeacherOperationalState
            {
                arousal = 0.82f,
                regulationCapacity = 0.28f,
                physicalSafety = 0.42f,
                perceivedSupport = 0.12f,
                responseConfidence = 0.38f
            },
            classroom = new ClassroomOperationalState
            {
                focalStudentRisk = 0.82f,
                peerDistress = 0.68f,
                noise = 0.64f,
                peersInUnsafeArea = 4,
                learningContinuity = 0.4f
            },
            support = new SupportResourceState
            {
                colleagueAvailable = true,
                administratorAvailable = true,
                counselorAvailable = true
            }
        };
    }

    private static CrisisOrchestrationScenarioBeat[] Beats()
    {
        return new[]
        {
            Beat("01.teacher-state", CrisisOrchestrationPhase.Assess,
                CrisisOrchestrationAction.PauseAndRegulate,
                "내 상태를 먼저 알아차리기",
                "학생의 말과 물건이 떨어지는 소리에 순간적으로 심장이 빨라지고 화가 올라온다.",
                "지금 느끼는 감정에는 점수가 없습니다. 이 상태에서 어떤 행동을 선택하면 학생과 나, 학급의 안전에 도움이 될까요?",
                "학생의 어깨가 올라가 있고 손에 힘이 들어가 있다. 주변 학생들이 말을 멈추고 쳐다본다.",
                "교사 감정을 억압하거나 도덕적으로 평가하지 않는다. 판단 여유를 확보하는 관찰 가능한 행동을 피드백한다.",
                "나는 어떤 신체 신호를 알아차렸고, 그것이 다음 판단에 어떤 영향을 주었는가?",
                Option(CrisisOrchestrationAction.PauseAndRegulate,
                    "한 호흡 멈추고 거리·출구·주변 학생을 확인한다",
                    "속으로 ‘화가 올라왔다’를 확인하고 한 걸음 옆으로 이동해 손, 통로, 주변 학생 위치를 본다.",
                    "감정을 없애는 것이 아니라 충동적인 맞대응 전에 판단할 시간을 확보한다."),
                Option(CrisisOrchestrationAction.AddressStudent,
                    "즉시 큰 목소리로 행동을 제지한다",
                    "‘지금 당장 그만해!’라고 반응한다.",
                    "교사와 학생의 각성이 모두 높을 때 즉각적인 맞대응은 상호 격화를 키울 수 있다.")),

            Beat("02.peer-safety", CrisisOrchestrationPhase.Stabilize,
                CrisisOrchestrationAction.MovePeersToSafety,
                "학급 전체의 안전 확보",
                "초점학생만 바라보는 동안 앞줄 학생 네 명이 가까운 위치에 남아 있다.",
                "수업을 계속하는 것과 주변 학생을 이동시키는 것 중 지금 무엇이 우선인가?",
                "한 학생이 의자를 밀고 통로로 나오며 다른 학생은 필통을 주우려 한다.",
                "초점학생을 공개적으로 둘러싸지 않으면서 또래의 안전과 구경 자극을 줄이는 선택을 다룬다.",
                "나는 초점학생 외에 누구의 안전을 확인했는가?",
                Option(CrisisOrchestrationAction.MovePeersToSafety,
                    "주변 학생에게 짧고 중립적인 이동 지시를 한다",
                    "‘앞줄은 선생님 책상 옆 모둠으로 조용히 이동해 주세요. 물건은 그대로 두세요.’",
                    "수업 지속보다 안전을 우선하고 초점학생을 구경하는 집단 자극을 줄인다."),
                Option(CrisisOrchestrationAction.RecordObjectiveFacts,
                    "현장에서 바로 기록을 시작한다",
                    "휴대기기를 꺼내 사건 내용을 입력한다.",
                    "위험이 남아 있는 동안 기록을 시작하면 주변 학생과 현장 변화를 놓칠 수 있다.")),

            Beat("03.help-request", CrisisOrchestrationPhase.AwaitSupport,
                CrisisOrchestrationAction.RequestColleagueSupport,
                "구체적으로 도움 요청하기",
                "학생과 학급을 동시에 관찰하기 어렵고 교사의 물리적 안전감도 낮다.",
                "혼자 해결하려는 것이 책임감인지, 지원 요청을 늦추는 것인지 판단해 보세요.",
                "학생은 교실 뒤쪽으로 움직이려 하고 주변 학생의 주의가 계속 쏠린다.",
                "도움 요청은 실패가 아니다. 위치, 위험, 필요한 역할을 짧고 구체적으로 전달하게 한다.",
                "나는 누구에게 무엇을 해 달라고 요청했는가?",
                Option(CrisisOrchestrationAction.RequestColleagueSupport,
                    "동료 교사에게 학급 안전 지원을 요청한다",
                    "‘4학년 2반입니다. 학생이 물건을 던졌고 주변 학생 이동이 필요합니다. 학급 학생을 인솔하고 통로를 확보해 주세요.’",
                    "막연한 ‘도와주세요’보다 위치, 현재 위험, 필요한 역할을 전달한다."),
                Option(CrisisOrchestrationAction.RequestAdministratorSupport,
                    "관리자에게 즉시 현장 지원을 요청한다",
                    "‘4학년 2반 위기상황입니다. 학생·교사 안전 확인과 현장 인계가 필요합니다.’",
                    "학교 역할 체계상 관리자 개입이 필요한 수준인지 판단한다.")),

            Beat("04.handoff", CrisisOrchestrationPhase.Handoff,
                CrisisOrchestrationAction.HandoffWithBriefing,
                "도착 확인과 역할 인계",
                "지원 요청을 받은 동료 교사가 교실 문 앞에 도착한다.",
                "도착했다고 가정하지 말고 상대가 실제로 역할을 인수했는지 확인하세요.",
                "동료는 상황을 파악하려고 교사와 학생을 번갈아 본다.",
                "인계에는 관찰 사실, 현재 위험, 이미 실시한 조치, 다음 역할이 포함되어야 한다.",
                "인계할 때 사실과 해석을 어떻게 구분했는가?",
                Option(CrisisOrchestrationAction.ConfirmSupportArrival,
                    "지원인력의 도착과 의사소통 가능 상태를 확인한다",
                    "눈을 맞추고 이름을 부른 뒤 역할을 전달할 준비가 되었는지 확인한다.",
                    "요청 전송과 실제 지원 도착은 다른 사건이다."),
                Option(CrisisOrchestrationAction.HandoffWithBriefing,
                    "관찰 사실과 다음 역할을 짧게 인계한다",
                    "‘필통을 바닥 쪽으로 한 번 던졌고 다친 학생은 없습니다. 앞줄 이동을 부탁드리고, 저는 학생과 거리를 두고 대화하겠습니다.’",
                    "과장이나 진단 없이 안전에 필요한 정보만 전달한다.")),

            Beat("05.low-stimulus", CrisisOrchestrationPhase.Handoff,
                CrisisOrchestrationAction.AddressStudent,
                "팀이 확보된 상태에서 학생에게 재접촉",
                "주변 학생이 이동했고 동료가 학급을 맡았다. 초점학생은 여전히 큰 숨을 쉬지만 손에 든 물건은 없다.",
                "지금 학생에게 필요한 말은 설명, 처벌, 질문 중 무엇인가?",
                "학생의 목소리는 낮아졌지만 시선을 피하고 출구 방향을 확인한다.",
                "안전망이 확보된 후 짧은 인정과 선택권을 제공하되 사건 조사는 안정 이후로 미룬다.",
                "내 발화가 학생에게 처리할 정보량을 줄였는가?",
                Option(CrisisOrchestrationAction.AddressStudent,
                    "짧게 감정을 확인하고 안전한 선택을 제시한다",
                    "‘많이 답답해 보인다. 여기서 조용히 기다릴지, 상담실 앞까지 함께 갈지 선택해도 돼.’",
                    "낮은 자극, 선택권, 안전한 동행을 결합한다.")),

            Beat("06.documentation", CrisisOrchestrationPhase.Documentation,
                CrisisOrchestrationAction.RecordObjectiveFacts,
                "사실 중심 사건 기록",
                "학생과 학급의 즉각적 위험이 낮아지고 지원인력에게 현장이 인계되었다.",
                "기록에서 직접 본 사실, 학생이 한 말, 교사의 해석을 구분하세요.",
                "현장 시간, 위치, 물건의 방향, 주변 학생 위치와 실시한 조치를 확인할 수 있다.",
                "‘폭력적 성향’ 같은 낙인 대신 관찰 가능한 행동과 시간순 조치를 기록한다.",
                "내 기록에 진단·비난·추측 표현이 섞이지 않았는가?",
                Option(CrisisOrchestrationAction.RecordObjectiveFacts,
                    "시간·관찰·조치·인계를 순서대로 기록한다",
                    "‘11:18 학생이 필통을 교실 앞 바닥 방향으로 던짐. 접촉·부상 없음. 11:19 앞줄 학생 이동. 11:20 동료 교사 도착 및 학급 인계.’",
                    "사후 협업과 보호자 소통에 사용할 수 있는 검증 가능한 기록을 남긴다.")),

            Beat("07.recovery", CrisisOrchestrationPhase.Recovery,
                CrisisOrchestrationAction.RequestTeacherRecoverySupport,
                "교사 회복과 다음 지원 계획",
                "사건은 정리되었지만 교사는 손이 떨리고, 학생의 말이 반복해서 떠오른다.",
                "이 반응을 개인의 실패로 판단하지 말고 필요한 회복과 업무 지원을 선택하세요.",
                "긴장이 남아 있고 다음 수업 준비에 집중하기 어렵다.",
                "회복 요청은 평가 감점 요소가 아니다. 동료 디브리핑, 상담, 일정 조정을 구체화한다.",
                "내가 통제할 수 있었던 것과 조직의 지원이 필요했던 것은 각각 무엇인가?",
                Option(CrisisOrchestrationAction.RequestTeacherRecoverySupport,
                    "동료 디브리핑과 필요한 회복 지원을 요청한다",
                    "‘오늘 사건을 사실 중심으로 함께 검토하고 싶습니다. 다음 수업 전 잠시 안정할 시간과 후속 역할 확인이 필요합니다.’",
                    "개인 책임으로만 돌리지 않고 회복과 재발 방지를 조직적 행동으로 연결한다."))
        };
    }

    private static CrisisOrchestrationScenarioBeat Beat(
        string id,
        CrisisOrchestrationPhase phase,
        CrisisOrchestrationAction completionAction,
        string title,
        string situation,
        string privatePrompt,
        string signals,
        string facilitator,
        string debrief,
        params CrisisOrchestrationActionOption[] options)
    {
        return new CrisisOrchestrationScenarioBeat
        {
            beatId = id,
            phase = phase,
            completionAction = completionAction,
            title = title,
            situation = situation,
            privateTeacherPrompt = privatePrompt,
            observableSignals = signals,
            facilitatorNote = facilitator,
            debriefPrompt = debrief,
            options = options ?? Array.Empty<CrisisOrchestrationActionOption>()
        };
    }

    private static CrisisOrchestrationActionOption Option(
        CrisisOrchestrationAction action,
        string label,
        string script,
        string rationale)
    {
        return new CrisisOrchestrationActionOption
        {
            action = action,
            label = label,
            operationalScript = script,
            rationale = rationale
        };
    }
}
