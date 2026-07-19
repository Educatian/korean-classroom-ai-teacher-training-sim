using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    [CreateAssetMenu(
        fileName = "StudentPersona",
        menuName = "Teacher Training/Student Persona",
        order = 10)]
    public sealed class StudentPersonaAsset : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string personaId;
        [SerializeField] private string displayName;
        [SerializeField] private StudentGradeBand gradeBand = StudentGradeBand.UpperElementary;

        [Header("Strengths and Support Needs")]
        [SerializeField] private StudentStrength[] strengths = Array.Empty<StudentStrength>();
        [SerializeField] private PersonaSupportNeed[] supportNeeds = Array.Empty<PersonaSupportNeed>();

        public string PersonaId => personaId;
        public string DisplayName => displayName;
        public StudentGradeBand GradeBand => gradeBand;
        public IReadOnlyList<StudentStrength> Strengths => strengths;
        public IReadOnlyList<PersonaSupportNeed> SupportNeeds => supportNeeds;

        public StudentPersonaProfile ToRuntimeProfile()
        {
            return new StudentPersonaProfile
            {
                id = personaId,
                displayName = displayName,
                gradeBand = gradeBand,
                strengths = (StudentStrength[])strengths.Clone(),
                supportNeeds = (PersonaSupportNeed[])supportNeeds.Clone()
            };
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(
            StudentPersonaProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            personaId = profile.id;
            displayName = profile.displayName;
            gradeBand = profile.gradeBand;
            strengths = profile.strengths != null
                ? (StudentStrength[])profile.strengths.Clone()
                : Array.Empty<StudentStrength>();
            supportNeeds = profile.supportNeeds != null
                ? (PersonaSupportNeed[])profile.supportNeeds.Clone()
                : Array.Empty<PersonaSupportNeed>();
        }
#endif
    }
}