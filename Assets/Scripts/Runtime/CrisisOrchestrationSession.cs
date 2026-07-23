using System;
using System.Collections.Generic;

namespace AdieLab.TeacherTraining
{
    /// <summary>
    /// Runtime session coordinator. Presentation layers call SelectAction and
    /// subscribe to Changed; all training rules remain in the pure engine.
    /// </summary>
    public sealed class CrisisOrchestrationSession
    {
        private readonly CrisisOrchestrationScenarioAsset scenario;
        private readonly CrisisOrchestrationEngine engine;
        private readonly List<CrisisOrchestrationResolution> history = new();
        private int beatIndex;

        public event Action Changed;

        public CrisisOrchestrationScenarioAsset Scenario => scenario;
        public CrisisOrchestrationScenarioBeat CurrentBeat =>
            beatIndex < scenario.Beats.Count ? scenario.Beats[beatIndex] : null;
        public CrisisOrchestrationState State => engine.Current;
        public IReadOnlyList<CrisisOrchestrationResolution> History => history;
        public int BeatIndex => beatIndex;
        public bool IsComplete => beatIndex >= scenario.Beats.Count ||
                                  State.phase == CrisisOrchestrationPhase.Complete;

        public CrisisOrchestrationSession(
            CrisisOrchestrationScenarioAsset authoredScenario,
            CrisisOrchestrationPolicy policy = null)
        {
            scenario = authoredScenario != null
                ? authoredScenario
                : throw new ArgumentNullException(nameof(authoredScenario));
            CrisisOrchestrationState initial = scenario.InitialState;
            if (initial == null)
                throw new ArgumentException("Scenario requires an initial state.", nameof(authoredScenario));
            engine = new CrisisOrchestrationEngine(initial, policy);
        }

        public CrisisOrchestrationResolution SelectAction(
            CrisisOrchestrationAction action)
        {
            CrisisOrchestrationScenarioBeat beat = CurrentBeat;
            if (beat == null)
            {
                return new CrisisOrchestrationResolution
                {
                    accepted = false,
                    feedback = "이미 모든 학습 비트를 완료했습니다.",
                    before = State,
                    after = State
                };
            }

            if (!ContainsAction(beat, action))
            {
                return new CrisisOrchestrationResolution
                {
                    accepted = false,
                    feedback = "현재 장면에서 제공되지 않은 행동입니다.",
                    before = State,
                    after = State
                };
            }

            CrisisOrchestrationResolution resolution = engine.Apply(action);
            history.Add(resolution);
            if (resolution.accepted && action == beat.completionAction)
                beatIndex++;
            Changed?.Invoke();
            return resolution;
        }

        public CrisisOrchestrationAssessmentReport BuildAssessment(
            CrisisOrchestrationAssessmentModel model = null)
        {
            return CrisisOrchestrationAssessmentEngine.Evaluate(history, model);
        }

        private static bool ContainsAction(
            CrisisOrchestrationScenarioBeat beat,
            CrisisOrchestrationAction action)
        {
            if (beat.options == null) return false;
            for (int index = 0; index < beat.options.Length; index++)
                if (beat.options[index] != null && beat.options[index].action == action)
                    return true;
            return false;
        }
    }
}
