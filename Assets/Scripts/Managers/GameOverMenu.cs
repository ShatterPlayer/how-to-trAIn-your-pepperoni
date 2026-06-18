using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

namespace PizzaGame.UI
{
    public class GameOverMenu : MonoBehaviour
    {
        [Header("UI Text Display")]
        [SerializeField] private TextMeshProUGUI finalScoreText;

        private void Start()
        {
            Time.timeScale = 1f;

            if (finalScoreText != null)
            {
                finalScoreText.text = $"Score: {EndGameTrigger.FinalScore}";
            }
        }

        public void RestartGame()
        {
            SceneManager.LoadScene("Micha³ 2");
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