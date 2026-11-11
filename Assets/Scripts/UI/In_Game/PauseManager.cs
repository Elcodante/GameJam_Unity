using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using TMPro;
public class PauseManager : MonoBehaviour
{
    [Header("UI Pause Settings")]
    public GameObject pausePanel;
    public TextMeshProUGUI countdownText;
    public GameObject pauseButton;

    private bool isPaused = false;
    private bool isCountingDown = false;

    private void Start()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
        if (pauseButton != null)
        {
            pauseButton.SetActive(true);
        }
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if(keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            if (!isCountingDown)
            {
                if (!isPaused)
                {
                    TogglePause();
                }
            }
        }
    }

    public void TogglePause()
    {
        if (isCountingDown)
        {
            return;
        }

        if(GameManager.Instance != null && GameManager.Instance.currentState != GameState.Playing)
        {
            return;
        }

        if(isPaused)
        {
            StartResumeSequence();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        if (isCountingDown) return;

        isPaused = true;
        pausePanel.SetActive(true);
        if (pauseButton != null)
        {
            pauseButton.SetActive(false);
        }
        Time.timeScale = 0f;
    }
    public void StartResumeSequence()
    {
        pausePanel.SetActive(false);
        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        isCountingDown = true;
        countdownText.gameObject.SetActive(true);

        int count = 3;
        while (count > 0)
        {
            countdownText.text = count.ToString();

            yield return new WaitForSecondsRealtime(1f);
            count--;
        }

        countdownText.text = "START!";
        yield return new WaitForSecondsRealtime(0.5f);

        countdownText.gameObject.SetActive(false);

        Time.timeScale = 1f; 
        isPaused = false;
        isCountingDown = false;
        pauseButton.SetActive(true);
    }
}
