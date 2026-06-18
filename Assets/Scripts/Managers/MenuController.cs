using UnityEngine;
using UnityEngine.SceneManagement;

namespace PizzaGame.Infrastructure
{
    public class MenuController : MonoBehaviour
    {
        [Header("Scene Settings")]
        [SerializeField] private string gameSceneName = "Micha³ 2";

        [Header("UI Elements")]
        [SerializeField] private GameObject howToPlayPanel;

        public void StartGame()
        {
            SceneManager.LoadScene(gameSceneName); ;
        }

        public void OpenHowToPlay()
        {
            if (howToPlayPanel != null)
            {
                howToPlayPanel.SetActive(true);
            }
        }

        public void CloseHowToPlay()
        {
            if (howToPlayPanel != null)
            {
                howToPlayPanel.SetActive(false);
            }
        }

        public void ExitGame()
        {
            Application.Quit();
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; 
            #endif
        }
    }
}