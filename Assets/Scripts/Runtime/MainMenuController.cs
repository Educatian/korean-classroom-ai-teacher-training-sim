using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdieLab.TeacherTraining
{
    /// <summary>
    /// Front page of the published build: scene selection, research information,
    /// and quit. All wiring is created by the editor MainMenuBuilder.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup infoPanel;

        public void StartGeneralClassroom()
        {
            SceneManager.LoadScene("KoreanClassroomTraining", LoadSceneMode.Single);
        }

        public void StartCircleDiscussion()
        {
            SceneManager.LoadScene("KoreanClassroomCircleTraining", LoadSceneMode.Single);
        }

        public void ToggleResearchInfo()
        {
            if (infoPanel == null)
            {
                return;
            }

            bool show = infoPanel.alpha < 0.5f;
            infoPanel.alpha = show ? 1f : 0f;
            infoPanel.interactable = show;
            infoPanel.blocksRaycasts = show;
        }

        public void QuitApplication()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void SetInfoPanel(CanvasGroup panel)
        {
            infoPanel = panel;
        }
    }
}
