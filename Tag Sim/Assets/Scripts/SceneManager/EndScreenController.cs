using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndScreenController : MonoBehaviour
{
    public Text scoreText;

    void Start()
    {
        // Retrieve final score from PlayerPrefs
        int finalScore = PlayerPrefs.GetInt("FinalScore", 0);
        scoreText.text = $"Your Score: {finalScore}";
    }

    public void RetryLevel()
    {
        SceneManager.LoadScene("MainLevel");
    }

    public void LevelSelect()
    {
        SceneManager.LoadScene("TitleScreen");
    }
}
