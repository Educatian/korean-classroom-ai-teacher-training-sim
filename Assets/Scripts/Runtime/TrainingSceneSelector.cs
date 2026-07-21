using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AdieLab.TeacherTraining
{
    [DisallowMultipleComponent]
    public sealed class TrainingSceneSelector : MonoBehaviour
    {
        [SerializeField] private Button sceneToggleButton;
        [SerializeField] private TMP_Text sceneToggleLabel;

        public TrainingSceneId CurrentSceneId =>
            TrainingSceneRegistry.FromSceneName(SceneManager.GetActiveScene().name);

        private void Awake()
        {
            sceneToggleButton?.onClick.AddListener(LoadOtherScene);
            RefreshLabel();
        }

        private void OnDestroy()
        {
            sceneToggleButton?.onClick.RemoveListener(LoadOtherScene);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                TryLoad(TrainingSceneId.GeneralClassroom);
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                TryLoad(TrainingSceneId.CircleDiscussion);
            }
            else if (Input.GetKeyDown(KeyCode.F3))
            {
                TryLoad(TrainingSceneId.RecoveryRoom);
            }
        }

        public void LoadOtherScene()
        {
            TryLoad(TrainingSceneRegistry.Other(CurrentSceneId));
        }

        public bool TryLoad(TrainingSceneId sceneId)
        {
            string sceneName = TrainingSceneRegistry.SceneName(sceneId);
            if (sceneId == CurrentSceneId)
            {
                RefreshLabel();
                return true;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                return false;
            }

            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            return true;
        }

        private void RefreshLabel()
        {
            if (sceneToggleLabel == null)
            {
                return;
            }

            sceneToggleLabel.text = CurrentSceneId switch
            {
                TrainingSceneId.CircleDiscussion => "서클 토론",
                TrainingSceneId.RecoveryRoom => "마음쉼터",
                _ => "일반 교실"
            };
        }
    }
}
