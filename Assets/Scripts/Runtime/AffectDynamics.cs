using UnityEngine;

namespace AdieLab.TeacherTraining
{
    public static class AffectDynamics
    {
        public static AffectVector ConstrainTurn(AffectVector current, AffectVector proposed, float maxShift)
        {
            return new AffectVector(
                Mathf.MoveTowards(current.valence, proposed.valence, maxShift),
                Mathf.MoveTowards(current.arousal, proposed.arousal, maxShift),
                Mathf.MoveTowards(current.dominance, proposed.dominance, maxShift));
        }

        public static AffectVector Step(AffectVector current, AffectVector target, float responseRate, float deltaTime)
        {
            float blend = 1f - Mathf.Exp(-Mathf.Max(0.01f, responseRate) * Mathf.Max(0f, deltaTime));
            return new AffectVector(
                Mathf.Lerp(current.valence, target.valence, blend),
                Mathf.Lerp(current.arousal, target.arousal, blend),
                Mathf.Lerp(current.dominance, target.dominance, blend));
        }
    }
}
