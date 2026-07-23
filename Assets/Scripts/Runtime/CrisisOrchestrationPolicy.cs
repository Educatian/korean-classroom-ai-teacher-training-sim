using UnityEngine;

namespace AdieLab.TeacherTraining
{
    [CreateAssetMenu(
        fileName = "CrisisOrchestrationPolicy",
        menuName = "Teacher Training/Crisis Orchestration Policy",
        order = 46)]
    public sealed class CrisisOrchestrationPolicy : ScriptableObject
    {
        public const string DefaultResourcePath = "Training/Orchestration/CrisisOrchestrationPolicy";

        [Header("Initial tuning values - validate with teacher and safety expert playtests")]
        [SerializeField, Range(0f, 1f)] private float highTeacherArousal = 0.7f;
        [SerializeField, Range(0f, 1f)] private float highStudentRisk = 0.7f;
        [SerializeField, Range(0f, 1f)] private float lowPhysicalSafety = 0.45f;
        [SerializeField, Range(0f, 1f)] private float safeForDocumentationRisk = 0.35f;

        public float HighTeacherArousal => highTeacherArousal;
        public float HighStudentRisk => highStudentRisk;
        public float LowPhysicalSafety => lowPhysicalSafety;
        public float SafeForDocumentationRisk => safeForDocumentationRisk;

        public static CrisisOrchestrationPolicy LoadDefault()
        {
            CrisisOrchestrationPolicy policy = Resources.Load<CrisisOrchestrationPolicy>(DefaultResourcePath);
            return policy != null ? policy : CreateRuntimeDefault();
        }

        public static CrisisOrchestrationPolicy CreateRuntimeDefault()
        {
            CrisisOrchestrationPolicy policy = CreateInstance<CrisisOrchestrationPolicy>();
            policy.hideFlags = HideFlags.DontSave;
            return policy;
        }
    }
}
