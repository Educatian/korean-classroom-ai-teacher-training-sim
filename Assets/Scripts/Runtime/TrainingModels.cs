using System;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    public enum StudentAffect
    {
        Calm,
        Uneasy,
        Distressed,
        Angry,
        Recovering
    }

    public enum BehaviorGesture
    {
        Neutral,
        AvoidGaze,
        Fidget,
        Withdraw,
        Protest,
        Defiant,
        DeskTap,
        Shield,
        Point,
        PushAway,
        Listen,
        Recover
    }

    public enum FacialActionUnit
    {
        AU1InnerBrowRaiser,
        AU2OuterBrowRaiser,
        AU4BrowLowerer,
        AU5UpperLidRaiser,
        AU6CheekRaiser,
        AU7LidTightener,
        AU9NoseWrinkler,
        AU12LipCornerPuller,
        AU15LipCornerDepressor,
        AU17ChinRaiser,
        AU20LipStretcher,
        AU23LipTightener,
        AU24LipPressor,
        AU25LipsPart,
        AU26JawDrop,
        AU45Blink
    }

    [Serializable]
    public struct AffectVector
    {
        [Range(-1f, 1f)] public float valence;
        [Range(0f, 1f)] public float arousal;
        [Range(-1f, 1f)] public float dominance;

        public AffectVector(float valence, float arousal, float dominance)
        {
            this.valence = Mathf.Clamp(valence, -1f, 1f);
            this.arousal = Mathf.Clamp01(arousal);
            this.dominance = Mathf.Clamp(dominance, -1f, 1f);
        }

        public static AffectVector MoveTowards(AffectVector current, AffectVector target, float maxDelta)
        {
            return new AffectVector(
                Mathf.MoveTowards(current.valence, target.valence, maxDelta),
                Mathf.MoveTowards(current.arousal, target.arousal, maxDelta),
                Mathf.MoveTowards(current.dominance, target.dominance, maxDelta));
        }
    }

    [Serializable]
    public sealed class ActionUnitDirective
    {
        [Range(0f, 1f)] public float au1;
        [Range(0f, 1f)] public float au2;
        [Range(0f, 1f)] public float au4;
        [Range(0f, 1f)] public float au5;
        [Range(0f, 1f)] public float au6;
        [Range(0f, 1f)] public float au7;
        [Range(0f, 1f)] public float au9;
        [Range(0f, 1f)] public float au12;
        [Range(0f, 1f)] public float au15;
        [Range(0f, 1f)] public float au17;
        [Range(0f, 1f)] public float au20;
        [Range(0f, 1f)] public float au23;
        [Range(0f, 1f)] public float au24;
        [Range(0f, 1f)] public float au25;
        [Range(0f, 1f)] public float au26;
    }

    [Serializable]
    public sealed class StudentAgentTurn
    {
        public string studentReply;
        public float valence;
        public float arousal;
        public float dominance;
        public string gesture;
        public ActionUnitDirective actionUnits;
    }

    [Serializable]
    public sealed class TeacherResponseOption
    {
        public string label;
        public string spokenResponse;
        [Range(0, 3)] public int quality;
        public string rationale;
        public StudentAffect resultingAffect;
        public CompetencyEvidence[] competencyEvidence;
    }

    [Serializable]
    public sealed class ScenarioBeat
    {
        public string title;
        public string studentLine;
        public string observation;
        public BehaviorGesture entryGesture;
        [Range(0f, 1f)] public float gestureIntensity;
        public TeacherResponseOption[] options;
    }

    [Serializable]
    public sealed class TrainingRecord
    {
        public string sessionId;
        public string timestampUtc;
        public int beatIndex;
        public string beatTitle;
        public int selectedOption;
        public int quality;
        public int cumulativeScore;
    }
}
