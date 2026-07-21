using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    public enum EyeTrackingSource { Unavailable = 0, EyeGaze = 1, HeadGazeFallback = 2 }
    public enum EyeTrackingRuntimeStatus
    {
        Unsupported = 0, PermissionRequired = 1, PermissionDenied = 2,
        TrackingUnavailable = 3, Tracking = 4, HeadGazeFallback = 5
    }
    public static class EyeTrackingDevicePolicy
    {
        public static bool IsKnownHeadGazeOnlyQuest(string deviceModel)
        {
            string model = (deviceModel ?? string.Empty).Trim().ToLowerInvariant();
            return model.Contains("quest 2") || model.Contains("quest 3") ||
                   model.Contains("quest 3s");
        }

        public static bool RequiresLiveEyeTracking(string deviceModel)
        {
#if QUEST_PRO_RESEARCH
            return !IsKnownHeadGazeOnlyQuest(deviceModel);
#else
            return false;
#endif
        }
    }

    public enum GazeAoiCategory
    {
        None = 0, FocalStudentEyes = 1, FocalStudentFace = 2,
        FocalStudentMouth = 3, FocalStudentTorso = 4, FocalStudentHands = 5,
        FocalStudentDesk = 6, PeerStudent = 7, Hud = 8,
        SpeechBubble = 9, ResponseOptions = 10, ElectronicBoard = 11
    }

    [Serializable]
    public sealed class GazeAoiDwell
    {
        public GazeAoiCategory category;
        public int milliseconds;
    }

    [Serializable]
    public sealed class TeacherGazeSummary
    {
        public string actionId = string.Empty;
        public EyeTrackingSource trackingSource;
        [Range(0f, 1f)] public float validSampleRatio;
        public int responseLatencyMilliseconds;
        public int firstRelevantFixationMilliseconds = -1;
        public int focalStudentDwellMilliseconds;
        public int faceAndEyesDwellMilliseconds;
        public int handsDwellMilliseconds;
        public int peerDwellMilliseconds;
        public int hudDwellMilliseconds;
        public int boardDwellMilliseconds;
        public int mutualGazeMilliseconds;
        public int fixationCount;
        public int revisitCount;
        public int transitionCount;
        public bool missedRelevantCue = true;
        public GazeAoiDwell[] dwellByAoi = Array.Empty<GazeAoiDwell>();
    }

    [Serializable]
    public sealed class GazeResearchSample
    {
        public int schemaVersion = 1;
        public string sessionId = string.Empty;
        public string timestampUtc = string.Empty;
        public double monotonicSeconds;
        public string scenarioId = string.Empty;
        public int beatIndex;
        public EyeTrackingSource trackingSource;
        public bool trackingValid;
        public Vector3 origin;
        public Vector3 direction;
        public string aoiId = string.Empty;
        public string actorId = string.Empty;
        public GazeAoiCategory category;
        public float hitDistance;
        public AffectVector studentAffect;
        public BehaviorGesture studentGesture;
        public bool studentLookingAtTeacher;
    }

    public sealed class GazeMetricAccumulator
    {
        private readonly int minimumFixationMilliseconds;
        private readonly float maximumAngularVelocity;
        private readonly Dictionary<GazeAoiCategory, double> dwell = new Dictionary<GazeAoiCategory, double>();
        private readonly HashSet<GazeAoiCategory> visited = new HashSet<GazeAoiCategory>();
        private double cueSeconds;
        private GazeResearchSample previous;
        private GazeAoiCategory runCategory;
        private double runStart;
        private bool runCounted;
        private int totalSamples;
        private int validSamples;
        private int fixations;
        private int revisits;
        private int transitions;
        private int firstRelevantMilliseconds = -1;
        private double mutualSeconds;
        private EyeTrackingSource source;

        public GazeMetricAccumulator(int minimumFixationMilliseconds, float maximumAngularVelocity)
        {
            this.minimumFixationMilliseconds = Mathf.Max(50, minimumFixationMilliseconds);
            this.maximumAngularVelocity = Mathf.Max(10f, maximumAngularVelocity);
        }

        public void BeginCue(double monotonicSeconds)
        {
            dwell.Clear();
            visited.Clear();
            cueSeconds = monotonicSeconds;
            previous = null;
            runCategory = GazeAoiCategory.None;
            runStart = monotonicSeconds;
            runCounted = false;
            totalSamples = 0;
            validSamples = 0;
            fixations = 0;
            revisits = 0;
            transitions = 0;
            firstRelevantMilliseconds = -1;
            mutualSeconds = 0d;
            source = EyeTrackingSource.Unavailable;
        }

        public void AddSample(GazeResearchSample sample)
        {
            if (sample == null) return;
            totalSamples++;
            if (!sample.trackingValid)
            {
                FlushPrevious(sample.monotonicSeconds);
                previous = sample;
                runCategory = GazeAoiCategory.None;
                return;
            }

            validSamples++;
            source = sample.trackingSource;
            if (previous != null)
            {
                FlushPrevious(sample.monotonicSeconds);
                double dt = Math.Max(0.0001d, sample.monotonicSeconds - previous.monotonicSeconds);
                float velocity = Vector3.Angle(previous.direction, sample.direction) / (float)dt;
                bool stable = velocity <= maximumAngularVelocity;
                if (!stable || sample.category != runCategory)
                {
                    StartRun(sample);
                }
                else
                {
                    CountFixationIfReady(sample.monotonicSeconds);
                }
            }
            else
            {
                StartRun(sample);
            }

            previous = sample;
        }

        public TeacherGazeSummary Complete(string actionId, double monotonicSeconds)
        {
            FlushPrevious(monotonicSeconds);
            CountFixationIfReady(monotonicSeconds);
            var entries = new List<GazeAoiDwell>();
            foreach (KeyValuePair<GazeAoiCategory, double> pair in dwell)
            {
                entries.Add(new GazeAoiDwell
                {
                    category = pair.Key,
                    milliseconds = ToMilliseconds(pair.Value)
                });
            }
            entries.Sort((left, right) => left.category.CompareTo(right.category));

            int focal = Sum(
                GazeAoiCategory.FocalStudentEyes, GazeAoiCategory.FocalStudentFace,
                GazeAoiCategory.FocalStudentMouth, GazeAoiCategory.FocalStudentTorso,
                GazeAoiCategory.FocalStudentHands, GazeAoiCategory.FocalStudentDesk);
            return new TeacherGazeSummary
            {
                actionId = actionId ?? string.Empty,
                trackingSource = source,
                validSampleRatio = totalSamples == 0 ? 0f : validSamples / (float)totalSamples,
                responseLatencyMilliseconds = ToMilliseconds(monotonicSeconds - cueSeconds),
                firstRelevantFixationMilliseconds = firstRelevantMilliseconds,
                focalStudentDwellMilliseconds = focal,
                faceAndEyesDwellMilliseconds = Sum(
                    GazeAoiCategory.FocalStudentEyes, GazeAoiCategory.FocalStudentFace,
                    GazeAoiCategory.FocalStudentMouth),
                handsDwellMilliseconds = Sum(GazeAoiCategory.FocalStudentHands),
                peerDwellMilliseconds = Sum(GazeAoiCategory.PeerStudent),
                hudDwellMilliseconds = Sum(
                    GazeAoiCategory.Hud, GazeAoiCategory.SpeechBubble, GazeAoiCategory.ResponseOptions),
                boardDwellMilliseconds = Sum(GazeAoiCategory.ElectronicBoard),
                mutualGazeMilliseconds = ToMilliseconds(mutualSeconds),
                fixationCount = fixations,
                revisitCount = revisits,
                transitionCount = transitions,
                missedRelevantCue = firstRelevantMilliseconds < 0,
                dwellByAoi = entries.ToArray()
            };
        }

        private void StartRun(GazeResearchSample sample)
        {
            if (runCategory != GazeAoiCategory.None && sample.category != runCategory)
            {
                transitions++;
                if (visited.Contains(sample.category)) revisits++;
            }
            runCategory = sample.category;
            runStart = sample.monotonicSeconds;
            runCounted = false;
            visited.Add(sample.category);
        }

        private void CountFixationIfReady(double now)
        {
            if (runCounted || runCategory == GazeAoiCategory.None) return;
            if (ToMilliseconds(now - runStart) < minimumFixationMilliseconds) return;
            runCounted = true;
            fixations++;
            if (firstRelevantMilliseconds < 0 && IsFocal(runCategory))
            {
                firstRelevantMilliseconds = ToMilliseconds(runStart - cueSeconds);
            }
        }

        private void FlushPrevious(double now)
        {
            if (previous == null || !previous.trackingValid) return;
            double seconds = Math.Min(0.1d, Math.Max(0d, now - previous.monotonicSeconds));
            if (!dwell.ContainsKey(previous.category)) dwell[previous.category] = 0d;
            dwell[previous.category] += seconds;
            if (previous.studentLookingAtTeacher && IsFace(previous.category)) mutualSeconds += seconds;
        }

        private int Sum(params GazeAoiCategory[] categories)
        {
            double seconds = 0d;
            foreach (GazeAoiCategory category in categories)
            {
                if (dwell.TryGetValue(category, out double value)) seconds += value;
            }
            return ToMilliseconds(seconds);
        }

        private static bool IsFocal(GazeAoiCategory category) =>
            category >= GazeAoiCategory.FocalStudentEyes &&
            category <= GazeAoiCategory.FocalStudentDesk;

        private static bool IsFace(GazeAoiCategory category) =>
            category == GazeAoiCategory.FocalStudentEyes ||
            category == GazeAoiCategory.FocalStudentFace ||
            category == GazeAoiCategory.FocalStudentMouth;

        private static int ToMilliseconds(double seconds) =>
            Mathf.Max(0, Mathf.RoundToInt((float)(seconds * 1000d)));
    }

    [CreateAssetMenu(fileName = "QuestProEyeTrackingResearchSettings",
        menuName = "Teacher Training/Quest Pro Eye Tracking Settings", order = 35)]
    public sealed class EyeTrackingResearchSettings : ScriptableObject
    {
        public const string DefaultResourcePath = "Training/Research/QuestProEyeTrackingResearchSettings";
        [Range(10, 90)] public int sampleRateHz = 30;
        [Min(1f)] public float maximumRayDistance = 20f;
        [Range(50, 500)] public int minimumFixationMilliseconds = 100;
        [Range(10f, 500f)] public float maximumFixationAngularVelocity = 120f;
        public bool persistRawGaze = true;
        public bool allowHeadGazeFallback = true;
        public LayerMask gazeLayerMask = ~0;

        public static bool RequireLiveEyeTracking =>
            EyeTrackingDevicePolicy.RequiresLiveEyeTracking(SystemInfo.deviceModel);

        public static EyeTrackingResearchSettings LoadDefault()
        {
            EyeTrackingResearchSettings settings =
                Resources.Load<EyeTrackingResearchSettings>(DefaultResourcePath);
            if (settings != null) return settings;
            settings = CreateInstance<EyeTrackingResearchSettings>();
            settings.hideFlags = HideFlags.DontSave;
            return settings;
        }
    }
}


