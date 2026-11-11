using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class SceneController : MonoBehaviour
{
    [Header("Pengaturan Transisi")]
    public Image fadeImage;
    public float fadeDuration = 1f;

    private void Start()
    {
        if (fadeImage != null)
        {
            if(AudioManager.Instance != null)
            {
                AudioManager.Instance.FadeBGM(isFadingOut: false, fadeDuration);
            }

            StartCoroutine(FadeSequence(1f, 0f)); 
        }
    }
    public void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        StartCoroutine(FadeOutAndLoad(sceneName));
    }

    private IEnumerator FadeOutAndLoad(string sceneName)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.FadeBGM(isFadingOut: true, fadeDuration);
        }
        yield return StartCoroutine(FadeSequence(0f, 1f)); 
        SceneManager.LoadSceneAsync(sceneName);
    }

    private IEnumerator FadeSequence(float startAlpha, float targetAlpha)
    {
        fadeImage.gameObject.SetActive(true);
        fadeImage.raycastTarget = true; 

        Color fadeColor = fadeImage.color;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            fadeColor.a = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
            fadeImage.color = fadeColor;
            yield return null; // Tunggu frame berikutnya
        }

        // Pastikan nilai akhir presisi
        fadeColor.a = targetAlpha;
        fadeImage.color = fadeColor;

        // Matikan image jika sudah transparan agar tidak memblokir tombol UI di belakangnya
        if (targetAlpha == 0f)
        {
            fadeImage.raycastTarget = false;
            fadeImage.gameObject.SetActive(false);
        }
    }

    public void RestartCurrentScene()
    {
        Time.timeScale = 1f;
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}