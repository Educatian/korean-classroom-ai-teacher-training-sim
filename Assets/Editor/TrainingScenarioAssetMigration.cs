using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AdieLab.TeacherTraining;
using UnityEditor;
using UnityEngine;

namespace AdieLab.TeacherTraining.Editor
{
    public static class TrainingScenarioAssetMigration
    {
        private const string Root = "Assets/Resources/Training";
        private const string PersonaFolder = Root + "/Personas";
        private const string ScenarioFolder = Root + "/Scenarios";
        private const string CatalogPath = Root + "/ScenarioCatalog.asset";

        private static readonly CrisisStage[] GeneralStages =
        {
            CrisisStage.Trigger,
            CrisisStage.Escalation,
            CrisisStage.Peak,
            CrisisStage.Escalation,
            CrisisStage.Deescalation,
            CrisisStage.InstructionalReentry
        };

        private static readonly CrisisStage[] CircleStages =
        {
            CrisisStage.Trigger,
            CrisisStage.Escalation,
            CrisisStage.Peak,
            CrisisStage.Peak,
            CrisisStage.Reconnection,
            CrisisStage.InstructionalReentry
        };

        [MenuItem("Tools/Teacher Training/Migrate Scenarios To Assets")]
        public static void MigrateFromMenu()
        {
            Migrate();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<TrainingScenarioCatalog>(CatalogPath);
        }

        public static void MigrateFromCommandLine()
        {
            try
            {
                Migrate();
                Debug.Log("TRAINING_SCENARIO_ASSET_MIGRATION_OK");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void Migrate()
        {
            Directory.CreateDirectory(PersonaFolder);
            Directory.CreateDirectory(ScenarioFolder);

            Dictionary<string, StudentPersonaAsset> personas = BuildPersonaAssets();
            TrainingScenarioAsset general = BuildScenarioAsset(
                "GeneralClassroomResponse",
                "?? ?? ????? ?? ??",
                TrainingSceneId.GeneralClassroom,
                TrainingScenarioLibrary.BuildDefaultScenario(),
                GeneralStages,
                personas);
            TrainingScenarioAsset circle = BuildScenarioAsset(
                "CircleDiscussionResponse",
                "?? ????? ?? ??",
                TrainingSceneId.CircleDiscussion,
                TrainingScenarioLibrary.BuildCircleDiscussionScenario(),
                CircleStages,
                personas);

            TrainingScenarioCatalog catalog = LoadOrCreate<TrainingScenarioCatalog>(CatalogPath);
            catalog.ConfigureForEditor(
                personas.Values.OrderBy(item => item.PersonaId).ToArray(),
                new[] { general, circle });
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static Dictionary<string, StudentPersonaAsset> BuildPersonaAssets()
        {
            var assets = new Dictionary<string, StudentPersonaAsset>(StringComparer.Ordinal);
            foreach (StudentPersonaProfile profile in TrainingResearchCatalog.BuildPersonas())
            {
                string path = $"{PersonaFolder}/{profile.id}.asset";
                StudentPersonaAsset asset = LoadOrCreate<StudentPersonaAsset>(path);
                asset.ConfigureForEditor(profile);
                EditorUtility.SetDirty(asset);
                assets.Add(profile.id, asset);
            }

            return assets;
        }

        private static TrainingScenarioAsset BuildScenarioAsset(
            string scenarioId,
            string displayName,
            TrainingSceneId sceneId,
            ScenarioBeat[] runtimeBeats,
            CrisisStage[] stages,
            IReadOnlyDictionary<string, StudentPersonaAsset> personas)
        {
            var authoredBeats = new ScenarioBeatAuthoringData[runtimeBeats.Length];
            for (int index = 0; index < runtimeBeats.Length; index++)
            {
                CrisisScenarioProfile profile = TrainingResearchCatalog.ForBeat(sceneId, index);
                var authored = new ScenarioBeatAuthoringData();
                authored.ConfigureForEditor(new ScenarioBeatAuthoringSeed
                {
                    scenarioId = $"{sceneId}.{profile.id}.{index + 1:D2}",
                    trigger = runtimeBeats[index].title,
                    stage = stages[index],
                    studentPersona = personas[profile.personaId],
                    crisisType = profile.crisisType,
                    teacherGoals = profile.evidenceTargets,
                    safetyFlags = profile.safetyFlags,
                    peerAttention = profile.peerAttention,
                    presentationAvoidance = profile.presentationAvoidance,
                    beat = runtimeBeats[index]
                });
                authoredBeats[index] = authored;
            }

            string path = $"{ScenarioFolder}/{scenarioId}.asset";
            TrainingScenarioAsset asset = LoadOrCreate<TrainingScenarioAsset>(path);
            asset.ConfigureForEditor(
                scenarioId,
                displayName,
                sceneId,
                "???? ?? ??, ?? ??, ?? ??? ?? ??? Inspector?? ???? ???????.",
                authoredBeats);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}