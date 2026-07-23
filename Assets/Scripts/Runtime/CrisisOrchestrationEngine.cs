using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    /// <summary>
    /// Pure domain rules for a teacher-led crisis response. The engine never
    /// scores a teacher for feeling distressed; it evaluates observable safety,
    /// help-seeking, handoff, documentation, and recovery actions.
    /// </summary>
    public sealed class CrisisOrchestrationEngine
    {
        private readonly CrisisOrchestrationPolicy policy;
        private CrisisOrchestrationState state;

        public CrisisOrchestrationState Current => state.Copy();

        public CrisisOrchestrationEngine(
            CrisisOrchestrationState initialState,
            CrisisOrchestrationPolicy authoredPolicy = null)
        {
            if (initialState == null) throw new ArgumentNullException(nameof(initialState));
            policy = authoredPolicy != null
                ? authoredPolicy
                : CrisisOrchestrationPolicy.LoadDefault();
            state = initialState.Copy();
            Normalize(state);
        }

        public CrisisOrchestrationResolution Apply(CrisisOrchestrationAction action)
        {
            CrisisOrchestrationState before = state.Copy();
            var evidence = new List<CrisisOrchestrationEvidenceId>();
            if (state.phase == CrisisOrchestrationPhase.Complete)
            {
                return Rejected(before, "이미 완료된 위기대응 세션입니다.", evidence);
            }

            string feedback;
            bool accepted = action switch
            {
                CrisisOrchestrationAction.PauseAndRegulate => PauseAndRegulate(evidence, out feedback),
                CrisisOrchestrationAction.AddressStudent => AddressStudent(evidence, out feedback),
                CrisisOrchestrationAction.MovePeersToSafety => MovePeers(evidence, out feedback),
                CrisisOrchestrationAction.RequestColleagueSupport => RequestColleague(evidence, out feedback),
                CrisisOrchestrationAction.RequestAdministratorSupport => RequestAdministrator(evidence, out feedback),
                CrisisOrchestrationAction.ConfirmSupportArrival => ConfirmSupportArrival(out feedback),
                CrisisOrchestrationAction.HandoffWithBriefing => Handoff(evidence, out feedback),
                CrisisOrchestrationAction.RecordObjectiveFacts => RecordFacts(evidence, out feedback),
                CrisisOrchestrationAction.RequestTeacherRecoverySupport => PlanRecovery(evidence, out feedback),
                _ => RejectUnknown(out feedback)
            };

            Normalize(state);
            return new CrisisOrchestrationResolution
            {
                accepted = accepted,
                feedback = feedback,
                before = before,
                after = state.Copy(),
                evidence = evidence.ToArray()
            };
        }

        private bool PauseAndRegulate(
            ICollection<CrisisOrchestrationEvidenceId> evidence,
            out string feedback)
        {
            state.teacher.arousal -= 0.24f;
            state.teacher.regulationCapacity += 0.22f;
            evidence.Add(CrisisOrchestrationEvidenceId.TeacherStateAcknowledged);
            evidence.Add(CrisisOrchestrationEvidenceId.RegulatedBeforeIntervention);
            feedback = "느낀 감정을 평가하지 않고, 다음 행동을 안전하게 선택할 여유를 확보했습니다.";
            return true;
        }

        private bool AddressStudent(
            ICollection<CrisisOrchestrationEvidenceId> evidence,
            out string feedback)
        {
            bool overloaded = state.teacher.arousal >= policy.HighTeacherArousal &&
                              state.classroom.focalStudentRisk >= policy.HighStudentRisk;
            if (overloaded)
            {
                state.classroom.focalStudentRisk += 0.08f;
                state.teacher.physicalSafety -= 0.08f;
                evidence.Add(CrisisOrchestrationEvidenceId.PrematureDirectIntervention);
                feedback = "교사의 각성과 학생 위험이 모두 높은 상태에서 직접 개입하여 상호 격화 가능성이 커졌습니다.";
                return true;
            }

            state.classroom.focalStudentRisk -= 0.24f;
            state.teacher.responseConfidence += 0.08f;
            AdvanceTo(CrisisOrchestrationPhase.Stabilize);
            feedback = "현재 조절 가능 범위에서 짧고 예측 가능한 직접 개입을 실시했습니다.";
            return true;
        }

        private bool MovePeers(
            ICollection<CrisisOrchestrationEvidenceId> evidence,
            out string feedback)
        {
            state.classroom.peersInUnsafeArea = 0;
            state.classroom.peerDistress -= 0.24f;
            state.classroom.noise -= 0.15f;
            state.classroom.learningContinuity -= 0.05f;
            AdvanceTo(CrisisOrchestrationPhase.Stabilize);
            evidence.Add(CrisisOrchestrationEvidenceId.PeerSafetyPrioritized);
            feedback = "수업 지속보다 주변 학생의 안전과 자극 감소를 먼저 확보했습니다.";
            return true;
        }

        private bool RequestColleague(
            ICollection<CrisisOrchestrationEvidenceId> evidence,
            out string feedback)
        {
            if (state.support.colleagueResponse == SupportResponseState.EnRoute ||
                state.support.colleagueResponse == SupportResponseState.Present)
            {
                feedback = "동료 지원 요청이 이미 진행 중입니다.";
                return false;
            }

            state.support.colleagueResponse = state.support.colleagueAvailable
                ? SupportResponseState.EnRoute
                : SupportResponseState.Unavailable;
            if (!state.support.colleagueAvailable)
            {
                feedback = "현재 동료 지원이 불가능합니다. 관리자 지원 등 다음 자원을 선택해야 합니다.";
                return true;
            }

            state.teacher.perceivedSupport += 0.18f;
            state.phase = CrisisOrchestrationPhase.AwaitSupport;
            if (HelpIsIndicated()) evidence.Add(CrisisOrchestrationEvidenceId.TimelyHelpSeeking);
            feedback = "동료에게 위치, 위험, 필요한 역할을 포함한 지원 요청을 보냈습니다.";
            return true;
        }

        private bool RequestAdministrator(
            ICollection<CrisisOrchestrationEvidenceId> evidence,
            out string feedback)
        {
            if (state.support.administratorResponse == SupportResponseState.EnRoute ||
                state.support.administratorResponse == SupportResponseState.Present)
            {
                feedback = "관리자 지원 요청이 이미 진행 중입니다.";
                return false;
            }

            state.support.administratorResponse = state.support.administratorAvailable
                ? SupportResponseState.EnRoute
                : SupportResponseState.Unavailable;
            if (!state.support.administratorAvailable)
            {
                feedback = "관리자도 즉시 대응할 수 없습니다. 학생·학급 안전을 유지하며 대체 지원체계를 사용해야 합니다.";
                return true;
            }

            state.teacher.perceivedSupport += 0.16f;
            state.phase = CrisisOrchestrationPhase.AwaitSupport;
            evidence.Add(CrisisOrchestrationEvidenceId.EscalatedSupportRequest);
            feedback = "동료 지원의 한계를 확인하고 관리자에게 구체적인 역할을 요청했습니다.";
            return true;
        }

        private bool ConfirmSupportArrival(out string feedback)
        {
            if (state.support.colleagueResponse == SupportResponseState.EnRoute)
            {
                state.support.colleagueResponse = SupportResponseState.Present;
                feedback = "동료 교사가 도착했습니다. 역할과 관찰 사실을 짧게 인계할 수 있습니다.";
                return true;
            }
            if (state.support.administratorResponse == SupportResponseState.EnRoute)
            {
                state.support.administratorResponse = SupportResponseState.Present;
                feedback = "관리자가 도착했습니다. 현재 위험과 필요한 조치를 구체적으로 인계할 수 있습니다.";
                return true;
            }
            feedback = "도착 예정인 지원인력이 없습니다.";
            return false;
        }

        private bool Handoff(
            ICollection<CrisisOrchestrationEvidenceId> evidence,
            out string feedback)
        {
            bool colleaguePresent = state.support.colleagueResponse == SupportResponseState.Present;
            bool administratorPresent = state.support.administratorResponse == SupportResponseState.Present;
            if (!colleaguePresent && !administratorPresent)
            {
                evidence.Add(CrisisOrchestrationEvidenceId.UnsupportedHandoffAttempt);
                feedback = "지원인력이 도착하기 전에는 현장을 일방적으로 떠날 수 없습니다.";
                return false;
            }

            if (colleaguePresent) state.support.colleagueResponse = SupportResponseState.HandoffComplete;
            if (administratorPresent) state.support.administratorResponse = SupportResponseState.HandoffComplete;
            state.classroom.focalStudentRisk -= 0.24f;
            state.teacher.physicalSafety += 0.22f;
            state.teacher.perceivedSupport += 0.2f;
            state.phase = CrisisOrchestrationPhase.Handoff;
            evidence.Add(CrisisOrchestrationEvidenceId.ClearHandoff);
            feedback = "관찰 사실, 현재 위험, 실시한 조치와 다음 역할을 구분해 인계했습니다.";
            return true;
        }

        private bool RecordFacts(
            ICollection<CrisisOrchestrationEvidenceId> evidence,
            out string feedback)
        {
            bool safeTiming = state.classroom.focalStudentRisk <= policy.SafeForDocumentationRisk &&
                              state.classroom.peersInUnsafeArea == 0 &&
                              (int)state.phase >= (int)CrisisOrchestrationPhase.Handoff;
            if (!safeTiming)
            {
                evidence.Add(CrisisOrchestrationEvidenceId.UnsafeDocumentationTiming);
                feedback = "현재는 기록보다 안전 확보와 인계가 우선입니다.";
                return false;
            }

            state.objectiveRecordCompleted = true;
            state.phase = CrisisOrchestrationPhase.Documentation;
            evidence.Add(CrisisOrchestrationEvidenceId.ObjectiveDocumentation);
            feedback = "진단이나 비난 표현 없이 관찰 사실, 시간, 조치와 인계 내용을 기록했습니다.";
            return true;
        }

        private bool PlanRecovery(
            ICollection<CrisisOrchestrationEvidenceId> evidence,
            out string feedback)
        {
            if (!state.objectiveRecordCompleted)
            {
                feedback = "사건의 객관적 기록과 인계를 먼저 완료해야 합니다.";
                return false;
            }

            state.recoveryPlanCompleted = true;
            state.teacher.perceivedSupport += 0.22f;
            state.teacher.responseConfidence += 0.08f;
            state.phase = CrisisOrchestrationPhase.Complete;
            evidence.Add(CrisisOrchestrationEvidenceId.TeacherRecoveryPlanned);
            feedback = "사건 이후 동료 디브리핑과 필요한 회복 지원을 계획했습니다.";
            return true;
        }

        private bool HelpIsIndicated()
        {
            return state.classroom.focalStudentRisk >= policy.HighStudentRisk ||
                   state.teacher.physicalSafety <= policy.LowPhysicalSafety ||
                   state.classroom.peersInUnsafeArea > 0;
        }

        private void AdvanceTo(CrisisOrchestrationPhase next)
        {
            if ((int)state.phase < (int)next) state.phase = next;
        }

        private CrisisOrchestrationResolution Rejected(
            CrisisOrchestrationState before,
            string feedback,
            ICollection<CrisisOrchestrationEvidenceId> evidence)
        {
            return new CrisisOrchestrationResolution
            {
                accepted = false,
                feedback = feedback,
                before = before,
                after = state.Copy(),
                evidence = new List<CrisisOrchestrationEvidenceId>(evidence).ToArray()
            };
        }

        private static bool RejectUnknown(out string feedback)
        {
            feedback = "지원되지 않는 위기대응 행동입니다.";
            return false;
        }

        private static void Normalize(CrisisOrchestrationState value)
        {
            value.teacher ??= new TeacherOperationalState();
            value.classroom ??= new ClassroomOperationalState();
            value.support ??= new SupportResourceState();
            value.teacher.arousal = Mathf.Clamp01(value.teacher.arousal);
            value.teacher.regulationCapacity = Mathf.Clamp01(value.teacher.regulationCapacity);
            value.teacher.physicalSafety = Mathf.Clamp01(value.teacher.physicalSafety);
            value.teacher.perceivedSupport = Mathf.Clamp01(value.teacher.perceivedSupport);
            value.teacher.responseConfidence = Mathf.Clamp01(value.teacher.responseConfidence);
            value.classroom.focalStudentRisk = Mathf.Clamp01(value.classroom.focalStudentRisk);
            value.classroom.peerDistress = Mathf.Clamp01(value.classroom.peerDistress);
            value.classroom.noise = Mathf.Clamp01(value.classroom.noise);
            value.classroom.peersInUnsafeArea = Math.Max(0, value.classroom.peersInUnsafeArea);
            value.classroom.learningContinuity = Mathf.Clamp01(value.classroom.learningContinuity);
        }
    }
}
