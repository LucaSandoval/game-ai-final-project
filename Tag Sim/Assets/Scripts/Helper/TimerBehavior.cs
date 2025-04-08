using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TimerBehavior : MonoBehaviour
{
    public Text timerText;
    public float timeRemaining = 120f; // your timer duration
    private float initialTime;

    public bool timerRunning = true;
    public bool useColorInterpolation = true;

    private Color darkGreen = new Color(0f, 0.5f, 0f);
    private Color red = Color.red;

    public PlayerController playerController; // Reference to PlayerController for score

    void Start()
    {
        initialTime = timeRemaining;
    }

    void Update()
    {
        if (timerRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                DisplayTime(timeRemaining);
                UpdateColor();
            }
            else
            {
                timeRemaining = 0;
                timerRunning = false;
                TimerFinished();
            }
        }
    }

    void DisplayTime(float timeToDisplay)
    {
        timeToDisplay += 1;

        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    void UpdateColor()
    {
        if (useColorInterpolation)
        {
            float interpolationFactor = timeRemaining / initialTime;
            timerText.color = Color.Lerp(darkGreen, red, interpolationFactor);
        }
        else
        {
            timerText.color = (timeRemaining <= 5f) ? darkGreen : red;
        }
    }

    void TimerFinished()
    {
        Debug.Log("Timer finished!");

        // Save player's final score using PlayerPrefs (no extra class required)
        PlayerPrefs.SetInt("FinalScore", playerController.GetScore());
        PlayerPrefs.Save();

        // Load your "YouWin" scene
        SceneManager.LoadScene("YouWinScreen");
    }
}



