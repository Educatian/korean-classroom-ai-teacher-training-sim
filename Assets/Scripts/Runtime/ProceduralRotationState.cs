using UnityEngine;

namespace AdieLab.TeacherTraining
{
    public sealed class ProceduralRotationState
    {
        private bool hasSample;
        private Quaternion baseRotation;
        private Quaternion outputRotation;

        public Quaternion Apply(Quaternion currentRotation, Quaternion offset)
        {
            if (!hasSample || Quaternion.Angle(currentRotation, outputRotation) > 0.08f)
            {
                baseRotation = currentRotation;
            }

            outputRotation = offset * baseRotation;
            hasSample = true;
            return outputRotation;
        }

        public void Reset()
        {
            hasSample = false;
            baseRotation = Quaternion.identity;
            outputRotation = Quaternion.identity;
        }
    }
}
