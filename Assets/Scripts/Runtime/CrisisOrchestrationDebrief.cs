using System;
using System.Collections.Generic;

namespace AdieLab.TeacherTraining
{
    [Serializable]
    public sealed class CrisisOrchestrationDebrief
    {
        public string headline = string.Empty;
        public string[] strengths = Array.Empty<string>();
        public string[] revisit = Array.Empty<string>();
        public string[] organizationalSupports = Array.Empty<string>();
        public string[] reflectionPrompts = Array.Empty<string>();
        public CrisisOrchestrationAssessmentReport assessment =
            new CrisisOrchestrationAssessmentReport();
    }

    public static class CrisisOrchestrationDebriefBuilder
    {
        public static CrisisOrchestrationDebrief Build(
            CrisisOrchestrationScenarioAsset scenario,
            IReadOnlyList<CrisisOrchestrationResolution> history,
            CrisisOrchestrationAssessmentModel assessmentModel = null)
        {
            CrisisOrchestrationAssessmentReport assessment =
                CrisisOrchestrationAssessmentEngine.Evaluate(history, assessmentModel);
            var positive = new HashSet<CrisisOrchestrationEvidenceId>();
            var adverse = new HashSet<CrisisOrchestrationEvidenceId>();
            if (assessment.competencies != null)
            {
                foreach (OrchestrationCompetencyResult result in assessment.competencies)
                {
                    if (result?.observedEvidence != null)
                        foreach (CrisisOrchestrationEvidenceId item in result.observedEvidence)
                            positive.Add(item);
                    if (result?.adverseEvidence != null)
                        foreach (CrisisOrchestrationEvidenceId item in result.adverseEvidence)
                            adverse.Add(item);
                }
            }

            return new CrisisOrchestrationDebrief
            {
                headline = "감정을 평가하지 않고, 안전을 위해 실제로 선택한 행동을 함께 검토합니다.",
                strengths = MapPositive(positive),
                revisit = MapAdverse(adverse),
                organizationalSupports = new[]
                {
                    "교실을 잠시 맡고 주변 학생을 이동시킬 수 있는 동료 역할을 사전에 지정합니다.",
                    "관리자·상담 인력 호출 경로와 응답 불가 시 대체 경로를 실제 학교 절차에 맞게 확인합니다.",
                    "사건 직후 교사의 안정 시간, 동료 디브리핑, 후속 수업 조정을 개인 호의가 아닌 조직 절차로 둡니다."
                },
                reflectionPrompts = Prompts(scenario),
                assessment = assessment
            };
        }

        private static string[] MapPositive(ISet<CrisisOrchestrationEvidenceId> evidence)
        {
            var lines = new List<string>();
            AddIf(evidence, CrisisOrchestrationEvidenceId.RegulatedBeforeIntervention, lines,
                "높아진 각성을 알아차리고 즉각적인 맞대응 전에 판단 여유를 확보했습니다.");
            AddIf(evidence, CrisisOrchestrationEvidenceId.PeerSafetyPrioritized, lines,
                "초점학생뿐 아니라 주변 학생과 통로의 안전을 우선했습니다.");
            AddIf(evidence, CrisisOrchestrationEvidenceId.TimelyHelpSeeking, lines,
                "혼자 감당하지 않고 위치·위험·필요 역할을 포함해 적시에 지원을 요청했습니다.");
            AddIf(evidence, CrisisOrchestrationEvidenceId.ClearHandoff, lines,
                "지원인력에게 관찰 사실과 다음 역할을 분리해 인계했습니다.");
            AddIf(evidence, CrisisOrchestrationEvidenceId.ObjectiveDocumentation, lines,
                "안정 이후 진단이나 비난 없이 시간순 관찰 기록을 남겼습니다.");
            AddIf(evidence, CrisisOrchestrationEvidenceId.TeacherRecoveryPlanned, lines,
                "교사 회복을 개인의 약점이 아닌 필요한 후속 지원으로 다뤘습니다.");
            if (lines.Count == 0)
                lines.Add("아직 충분한 관찰 근거가 없습니다. 같은 장면을 다시 시도해 행동 근거를 만들어 보세요.");
            return lines.ToArray();
        }

        private static string[] MapAdverse(ISet<CrisisOrchestrationEvidenceId> evidence)
        {
            var lines = new List<string>();
            AddIf(evidence, CrisisOrchestrationEvidenceId.PrematureDirectIntervention, lines,
                "교사와 학생의 각성이 모두 높은 시점의 직접 맞대응은 상호 격화 위험이 있었습니다.");
            AddIf(evidence, CrisisOrchestrationEvidenceId.UnsupportedHandoffAttempt, lines,
                "지원인력의 실제 도착과 역할 수락을 확인하기 전에 인계를 시도했습니다.");
            AddIf(evidence, CrisisOrchestrationEvidenceId.UnsafeDocumentationTiming, lines,
                "현장 위험이 남아 있을 때 기록을 우선하여 주변 변화 관찰이 약해질 수 있었습니다.");
            if (lines.Count == 0)
                lines.Add("치명적인 순서 오류는 관찰되지 않았습니다. 실제 학교의 안전 절차와 비교해 세부 발화를 검토하세요.");
            return lines.ToArray();
        }

        private static string[] Prompts(CrisisOrchestrationScenarioAsset scenario)
        {
            var prompts = new List<string>();
            if (scenario?.Beats != null)
            {
                for (int index = 0; index < scenario.Beats.Count; index++)
                {
                    string prompt = scenario.Beats[index]?.debriefPrompt;
                    if (!string.IsNullOrWhiteSpace(prompt)) prompts.Add(prompt);
                }
            }
            return prompts.ToArray();
        }

        private static void AddIf(
            ISet<CrisisOrchestrationEvidenceId> observed,
            CrisisOrchestrationEvidenceId id,
            ICollection<string> lines,
            string message)
        {
            if (observed.Contains(id)) lines.Add(message);
        }
    }
}
