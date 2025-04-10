using System.Collections;
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

    public GameObject WinScreen;
    public Sound BGM;
    public Sound WinSound;
    public Sound TickSound;

    public Text scoreText;
    public LevelManager levelManager;

    void Start()
    {
        initialTime = timeRemaining;
        InvokeRepeating("PlayTickSound", 1, 1);
    }

    private void PlayTickSound()
    {
        SoundController.Instance?.PlaySoundRandomPitch(TickSound, 0.01f);
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
                CheckForWin();
            }
        }

        scoreText.text = PlayerController.GoalTilesLeft.ToString() + "/5";
        if (PlayerController.GoalTilesLeft == 0) scoreText.color = Color.green;
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

    public void CheckForWin()
    {
        if (PlayerController.GoalTilesLeft == 0)
        {
            StartCoroutine(TimerFinished());
        } else
        {
            levelManager.DoGameOverGoalFailed();
        }
    }

    private IEnumerator TimerFinished()
    {
        SoundController.Instance?.PlaySound(WinSound);
        SoundController.Instance?.PauseSound(BGM);
        Time.timeScale = 0;
        WinScreen.SetActive(true);
        yield return new WaitForSecondsRealtime(3f);
        Time.timeScale = 1;
        SceneManager.LoadScene("Title Screen");
    }
}



