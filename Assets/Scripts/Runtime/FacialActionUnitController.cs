using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    [DisallowMultipleComponent]
    public sealed class FacialActionUnitController : MonoBehaviour
    {
        private static readonly FacialActionUnit[] Units =
            (FacialActionUnit[])Enum.GetValues(typeof(FacialActionUnit));

        [SerializeField, Min(1f)] private float transitionRate = 130f;

        private readonly List<BlendShapeChannel> channels = new List<BlendShapeChannel>();
        private readonly Dictionary<FacialActionUnit, float> affectUnits = new Dictionary<FacialActionUnit, float>();
        private readonly Dictionary<FacialActionUnit, float> overrideUnits = new Dictionary<FacialActionUnit, float>();
        private readonly HashSet<FacialActionUnit> activeOverrides = new HashSet<FacialActionUnit>();
        private readonly Dictionary<int, Dictionary<FacialActionUnit, float>> sourcedOverrides =
            new Dictionary<int, Dictionary<FacialActionUnit, float>>();
        private readonly HashSet<FacialActionUnit> explicitActionUnits = new HashSet<FacialActionUnit>();
        private float blinkTimer;

        private sealed class BlendShapeChannel
        {
            public SkinnedMeshRenderer renderer;
            public int index;
            public string name;
            public float target;
        }

        public int ChannelCount => channels.Count;

        private void Awake()
        {
            CacheChannels();
            foreach (FacialActionUnit unit in Units)
            {
                affectUnits[unit] = 0f;
                overrideUnits[unit] = 0f;
            }

            blinkTimer = UnityEngine.Random.Range(2.2f, 4.8f);
        }

        private void Update()
        {
            blinkTimer -= Time.deltaTime;
            if (blinkTimer <= 0f)
            {
                StartCoroutine(Blink());
                blinkTimer = UnityEngine.Random.Range(2.4f, 5.5f);
            }

            for (int i = 0; i < channels.Count; i++)
            {
                BlendShapeChannel channel = channels[i];
                float current = channel.renderer.GetBlendShapeWeight(channel.index);
                channel.renderer.SetBlendShapeWeight(
                    channel.index,
                    Mathf.MoveTowards(current, channel.target, transitionRate * Time.deltaTime));
            }
        }

        public void SetActionUnit(FacialActionUnit unit, float intensity, bool immediate = false)
        {
            overrideUnits[unit] = Mathf.Clamp01(intensity);
            activeOverrides.Add(unit);
            RebuildTargets();
            if (immediate)
            {
                ApplyTargetsImmediately();
            }
        }

        public void SetActionUnit(
            FacialActionUnit unit,
            float intensity,
            int sourceId,
            bool immediate = false)
        {
            if (sourceId == 0)
            {
                SetActionUnit(unit, intensity, immediate);
                return;
            }

            if (!sourcedOverrides.TryGetValue(sourceId, out Dictionary<FacialActionUnit, float> source))
            {
                source = new Dictionary<FacialActionUnit, float>();
                sourcedOverrides[sourceId] = source;
            }

            source[unit] = Mathf.Clamp01(intensity);
            RebuildTargets();
            if (immediate)
            {
                ApplyTargetsImmediately();
            }
        }

        public float GetActionUnit(FacialActionUnit unit)
        {
            bool overridden = false;
            float strongest = 0f;
            if (activeOverrides.Contains(unit))
            {
                strongest = overrideUnits.TryGetValue(unit, out float defaultValue) ? defaultValue : 0f;
                overridden = true;
            }

            foreach (Dictionary<FacialActionUnit, float> source in sourcedOverrides.Values)
            {
                if (source.TryGetValue(unit, out float sourceValue))
                {
                    strongest = overridden ? Mathf.Max(strongest, sourceValue) : sourceValue;
                    overridden = true;
                }
            }

            return overridden
                ? strongest
                : affectUnits.TryGetValue(unit, out float affect) ? affect : 0f;
        }

        public void ReleaseActionUnit(FacialActionUnit unit, bool immediate = false)
        {
            activeOverrides.Remove(unit);
            RebuildTargets();
            if (immediate)
            {
                ApplyTargetsImmediately();
            }
        }

        public void ReleaseActionUnit(
            FacialActionUnit unit,
            int sourceId,
            bool immediate = false)
        {
            if (sourceId == 0)
            {
                ReleaseActionUnit(unit, immediate);
                return;
            }

            if (sourcedOverrides.TryGetValue(sourceId, out Dictionary<FacialActionUnit, float> source))
            {
                source.Remove(unit);
                if (source.Count == 0)
                {
                    sourcedOverrides.Remove(sourceId);
                }
            }

            RebuildTargets();
            if (immediate)
            {
                ApplyTargetsImmediately();
            }
        }

        public void ClearActionUnitOverrides(bool immediate = false)
        {
            activeOverrides.Clear();
            sourcedOverrides.Clear();
            RebuildTargets();
            if (immediate)
            {
                ApplyTargetsImmediately();
            }
        }

        public void ClearActionUnitOverrides(int sourceId, bool immediate = false)
        {
            if (sourceId == 0)
            {
                activeOverrides.Clear();
            }
            else
            {
                sourcedOverrides.Remove(sourceId);
            }

            RebuildTargets();
            if (immediate)
            {
                ApplyTargetsImmediately();
            }
        }

        public void ApplyAffect(AffectVector affect, bool immediate = false)
        {
            float negative = Mathf.Clamp01(-affect.valence);
            float positive = Mathf.Clamp01(affect.valence);
            float highArousal = Mathf.Clamp01(affect.arousal);
            float lowDominance = Mathf.Clamp01(-affect.dominance);
            float highDominance = Mathf.Clamp01(affect.dominance);

            affectUnits[FacialActionUnit.AU1InnerBrowRaiser] = Mathf.Max(negative * lowDominance, negative * 0.35f);
            affectUnits[FacialActionUnit.AU2OuterBrowRaiser] = negative * lowDominance * 0.55f;
            affectUnits[FacialActionUnit.AU4BrowLowerer] = negative * highDominance * 0.95f;
            affectUnits[FacialActionUnit.AU5UpperLidRaiser] = highArousal
                * (0.10f + negative * 0.30f)
                * (1f - highDominance * 0.75f);
            affectUnits[FacialActionUnit.AU6CheekRaiser] = positive * 0.35f;
            affectUnits[FacialActionUnit.AU7LidTightener] = negative * highArousal * 0.55f;
            affectUnits[FacialActionUnit.AU9NoseWrinkler] = negative * highDominance * 0.32f;
            affectUnits[FacialActionUnit.AU12LipCornerPuller] = positive * (0.45f + (1f - highArousal) * 0.45f);
            affectUnits[FacialActionUnit.AU15LipCornerDepressor] = negative * lowDominance * 0.7f;
            affectUnits[FacialActionUnit.AU17ChinRaiser] = negative * lowDominance * 0.42f;
            affectUnits[FacialActionUnit.AU20LipStretcher] = negative * highArousal * lowDominance * 0.45f;
            affectUnits[FacialActionUnit.AU23LipTightener] = negative * highDominance * 0.72f;
            affectUnits[FacialActionUnit.AU24LipPressor] = negative * highArousal * 0.65f;
            affectUnits[FacialActionUnit.AU25LipsPart] = highArousal * 0.18f;
            affectUnits[FacialActionUnit.AU26JawDrop] = highArousal * lowDominance * 0.12f;
            RebuildTargets();
            if (immediate)
            {
                ApplyTargetsImmediately();
            }
        }

        public float GetMaxWeight(params string[] tokens)
        {
            float max = 0f;
            for (int i = 0; i < channels.Count; i++)
            {
                if (Matches(channels[i].name, tokens))
                {
                    max = Mathf.Max(max, channels[i].renderer.GetBlendShapeWeight(channels[i].index));
                }
            }

            return max;
        }

        private System.Collections.IEnumerator Blink()
        {
            SetActionUnit(FacialActionUnit.AU45Blink, 1f, true);
            yield return new WaitForSeconds(0.09f);
            ReleaseActionUnit(FacialActionUnit.AU45Blink, true);
        }

        private void CacheChannels()
        {
            foreach (SkinnedMeshRenderer renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Mesh mesh = renderer.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                for (int i = 0; i < mesh.blendShapeCount; i++)
                {
                    string channelName = mesh.GetBlendShapeName(i).ToLowerInvariant();
                    channels.Add(new BlendShapeChannel
                    {
                        renderer = renderer,
                        index = i,
                        name = channelName,
                        target = 0f
                    });

                    foreach (FacialActionUnit unit in Units)
                    {
                        if (MatchesExplicitUnit(channelName, unit))
                        {
                            explicitActionUnits.Add(unit);
                        }
                    }
                }
            }
        }

        private void RebuildTargets()
        {
            for (int i = 0; i < channels.Count; i++)
            {
                BlendShapeChannel channel = channels[i];
                float strongest = 0f;
                foreach (FacialActionUnit unit in Units)
                {
                    bool matches = explicitActionUnits.Contains(unit)
                        ? MatchesExplicitUnit(channel.name, unit)
                        : MatchesFallbackUnit(channel.name, unit);
                    if (matches)
                    {
                        strongest = Mathf.Max(strongest, GetActionUnit(unit));
                    }
                }

                channel.target = strongest * 100f;
            }
        }

        private void ApplyTargetsImmediately()
        {
            for (int i = 0; i < channels.Count; i++)
            {
                channels[i].renderer.SetBlendShapeWeight(channels[i].index, channels[i].target);
            }
        }

        private static bool MatchesExplicitUnit(string name, FacialActionUnit unit)
        {
            return unit switch
            {
                FacialActionUnit.AU1InnerBrowRaiser => name.Contains("au_01_innerbrowraiser"),
                FacialActionUnit.AU2OuterBrowRaiser => name.Contains("au_02_outerbrowraiser"),
                FacialActionUnit.AU4BrowLowerer => name.Contains("au_04_browlowerer"),
                FacialActionUnit.AU5UpperLidRaiser => name.Contains("au_05_upperlidraiser"),
                FacialActionUnit.AU6CheekRaiser => name.Contains("au_06_cheekraiser"),
                FacialActionUnit.AU7LidTightener => name.Contains("au_07_lidtightener"),
                FacialActionUnit.AU9NoseWrinkler => name.Contains("au_09_nosewrinkler"),
                FacialActionUnit.AU12LipCornerPuller => name.Contains("au_12_lipcornerpuller"),
                FacialActionUnit.AU15LipCornerDepressor => name.Contains("au_15_lipcornerdepressor"),
                FacialActionUnit.AU17ChinRaiser => name.Contains("au_17_chinraiser"),
                FacialActionUnit.AU20LipStretcher => name.Contains("au_20_lipstretcher"),
                FacialActionUnit.AU23LipTightener => name.Contains("au_23_liptightener"),
                FacialActionUnit.AU24LipPressor => name.Contains("au_24_lippressor"),
                FacialActionUnit.AU25LipsPart => name.Contains("au_25_lipspart"),
                FacialActionUnit.AU26JawDrop => name.Contains("au_26_jawdrop"),
                FacialActionUnit.AU45Blink => name.Contains("au_45_blink"),
                _ => false
            };
        }

        private static bool MatchesFallbackUnit(string name, FacialActionUnit unit)
        {
            return unit switch
            {
                FacialActionUnit.AU1InnerBrowRaiser => Matches(name, "browinnerup", "innerbrow", "middleeyebrow", "browraise"),
                FacialActionUnit.AU2OuterBrowRaiser => Matches(name, "browouterup", "outereyebrow"),
                FacialActionUnit.AU4BrowLowerer => Matches(name, "browdown", "browlower", "brow_down"),
                FacialActionUnit.AU5UpperLidRaiser => Matches(name, "eyewide", "upperlidraise", "eye_wide"),
                FacialActionUnit.AU6CheekRaiser => Matches(name, "cheeksquint", "cheekraise", "cheek_squint"),
                FacialActionUnit.AU7LidTightener => Matches(name, "eyesquint", "lidtight", "eye_squint"),
                FacialActionUnit.AU9NoseWrinkler => Matches(name, "nosewrinkle", "nose_wrinkle"),
                FacialActionUnit.AU12LipCornerPuller => Matches(name, "mouthsmile", "lipcornerpull", "mouth_smile"),
                FacialActionUnit.AU15LipCornerDepressor => Matches(name, "mouthfrown", "lipcornerdepress", "mouth_frown"),
                FacialActionUnit.AU17ChinRaiser => Matches(name, "chinraise", "mouthshruglower"),
                FacialActionUnit.AU20LipStretcher => Matches(name, "mouthstretch", "lipstretch"),
                FacialActionUnit.AU23LipTightener => Matches(name, "mouthroll", "liptight"),
                FacialActionUnit.AU24LipPressor => Matches(name, "mouthpress", "lippress"),
                FacialActionUnit.AU25LipsPart => Matches(name, "mouthopen", "lipspart"),
                FacialActionUnit.AU26JawDrop => Matches(name, "jawopen", "jawdrop"),
                FacialActionUnit.AU45Blink => Matches(name, "eyeblink", "blinktop", "eye_blink"),
                _ => false
            };
        }

        private static bool Matches(string source, params string[] tokens)
        {
            for (int i = 0; i < tokens.Length; i++)
            {
                if (source.Contains(tokens[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
