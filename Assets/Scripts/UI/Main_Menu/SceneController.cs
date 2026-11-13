using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using System; // Tambahkan ini untuk Action

public class SceneController : MonoBehaviour
{
    [Header("Pengaturan Transisi")]
    public Image fadeImage;
    public float fadeDuration = 1f;

    // EVENT STATIS: Semua script bisa mendaftar ke sini untuk menunggu layar terang
    public static event Action OnSceneFadeInComplete;

    private void Start()
    {
        if (fadeImage != null)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.FadeBGM(isFadingOut: false, fadeDuration);
            }

            // Ganti baris ini agar menggunakan Coroutine pembungkus
            StartCoroutine(FadeInRoutine());
        }
    }

    private IEnumerator FadeInRoutine()
    {
        // Jalankan animasi transisi layar
        yield return StartCoroutine(FadeSequence(1f, 0f));

        // Setelah animasi selesai dan layar 100% bening, panggil event ini!
        OnSceneFadeInComplete?.Invoke();
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
            yield return null;
        }

        fadeColor.a = targetAlpha;
        fadeImage.color = fadeColor;

        if (targetAlpha <= 0.01f) // Gunakan toleransi batas
        {
            fadeColor.a = 0f;
            fadeImage.color = fadeColor;
            fadeImage.raycastTarget = false;
            fadeImage.gameObject.SetActive(false);
        }
    }

    // Perbaikan sedikit untuk RestartCurrentScene agar melalui FadeOut yang halus
    public void RestartCurrentScene()
    {
        Time.timeScale = 1f;
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        // Panggil fungsi LoadScene buatan sendiri, JANGAN bawaan Unity, agar kena FadeOut
        LoadScene(SceneUtility.GetScenePathByBuildIndex(currentSceneIndex));
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}