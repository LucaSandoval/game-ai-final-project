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

    //I have no idea if I am doing this right I am a little confused with where we are creating our levels
    //High key might not even use a restart button
    public void RetryLevel()
    {
        SceneManager.LoadScene("MainLevel");
    }

    public void LevelSelect()
    {
        SceneManager.LoadScene("TitleScreen");
    }
}
