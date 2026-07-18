using System;
using System.IO;
using System.Linq;
using AdieLab.TeacherTraining;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdieLab.TeacherTraining.Editor
{
    public static partial class KoreanClassroomBuilder
    {
        private const string CircleScenePath = "Assets/Scenes/KoreanClassroomCircleTraining.unity";

        [MenuItem("Tools/Teacher Training/Build Circle Discussion Scene")]
        public static void BuildCircleSceneFromMenu()
        {
            BuildCircleScene();
        }

        public static void BuildCircleSceneFromCommandLine()
        {
            try
            {
                BuildAll();
                BuildCircleScene();
                Debug.Log("KOREAN_CLASSROOM_CIRCLE_SCENE_BUILD_OK");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void BuildCircleScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform furniture = GameObject.Find("00_ENVIRONMENT/Furniture").transform;
            Transform students = GameObject.Find("10_STUDENTS").transform;
            Transform camera = GameObject.Find("20_SYSTEMS/TeacherCamera").transform;
            SimulationController simulation = UnityEngine.Object.FindAnyObjectByType<SimulationController>();

            Transform[] desks = furniture.Cast<Transform>()
                .Where(item => item.name.StartsWith("StudentDesk_", StringComparison.Ordinal))
                .OrderBy(item => item.name)
                .ToArray();
            Transform[] participants = students.Cast<Transform>()
                .OrderBy(item => item.name == "FocalStudent_Minjun" ? 0 : 1)
                .ThenBy(item => item.name)
                .ToArray();

            const float radius = 3.25f;
            for (int i = 0; i < desks.Length; i++)
            {
                bool participates = i < participants.Length;
                desks[i].gameObject.SetActive(participates);
                if (!participates)
                {
                    continue;
                }

                float angle = 180f + i * (360f / participants.Length);
                Vector3 position = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;
                Quaternion inward = Quaternion.LookRotation(-position.normalized, Vector3.up);
                desks[i].SetPositionAndRotation(position, inward);
                Transform chair = desks[i].Find("Chair");
                if (chair == null)
                {
                    throw new InvalidOperationException($"{desks[i].name} is missing its Chair anchor.");
                }

                participants[i].SetPositionAndRotation(chair.position, chair.rotation);
            }

            camera.SetPositionAndRotation(
                new Vector3(-5.40f, 2.65f, 4.15f),
                Quaternion.LookRotation(new Vector3(0f, 0.90f, 0f) - new Vector3(-5.40f, 2.65f, 4.15f)));
            Camera sceneCamera = camera.GetComponent<Camera>();
            if (sceneCamera != null)
            {
                sceneCamera.fieldOfView = 60f;
            }

            SerializedObject simulationData = new SerializedObject(simulation);
            simulationData.FindProperty("circleDiscussionScenario").boolValue = true;
            simulationData.ApplyModifiedPropertiesWithoutUndo();

            TMP_Text title = UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(label => label.name == "AppTitle");
            if (title != null)
            {
                title.text = "정서·행동 지원 교사 대응 훈련 · 원형 토론/발표";
            }

            GameObject context = new GameObject("SCENE_02_CIRCLE_DISCUSSION");
            context.transform.position = Vector3.zero;
            Cylinder("DiscussionCenterMarker", context.transform, new Vector3(0f, 0.015f, 0f), new Vector3(1.15f, 0.018f, 1.15f), Quaternion.identity, Mat("M_WorkMint"));

            EditorSceneManager.SaveScene(scene, CircleScenePath);
            RegisterTrainingScenes();
            AssetDatabase.SaveAssets();
        }
    }
}
