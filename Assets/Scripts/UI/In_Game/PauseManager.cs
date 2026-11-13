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
        if (pausePanel != null) pausePanel.SetActive(false);
        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (pauseButton != null) pauseButton.SetActive(true);
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            if (!isCountingDown && !isPaused)
            {
                TogglePause();
            }
        }
    }

    public void TogglePause()
    {
        if (isCountingDown) return;

        if (GameManager.Instance != null && GameManager.Instance.currentState != GameState.Playing)
            return;

        if (isPaused) StartResumeSequence();
        else PauseGame();
    }

    // ==========================================
    // FASE 1: BUKA PAUSE
    // ==========================================
    public void PauseGame()
    {
        if (isCountingDown) return;

        isPaused = true;
        Time.timeScale = 0f;

        if (pauseButton != null) pauseButton.SetActive(false);

        pausePanel.SetActive(true);
        StartCoroutine(OpenPanelAnim());
    }

    private IEnumerator OpenPanelAnim()
    {
        // 1. Membesar cepat melewati batas normal (0 ke 1.15)
        float timer = 0f;
        while (timer < 0.2f)
        {
            timer += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(0f, 1.15f, timer / 0.2f);
            pausePanel.transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        // 2. Mengempis santai ke ukuran normal (1.15 ke 1.0)
        timer = 0f;
        while (timer < 0.15f)
        {
            timer += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(1.15f, 1f, timer / 0.15f);
            pausePanel.transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        pausePanel.transform.localScale = Vector3.one;
    }

    // ==========================================
    // FASE 2: TUTUP PAUSE & MULAI HITUNG MUNDUR
    // ==========================================
    public void StartResumeSequence()
    {
        // Jangan langsung dimatikan, jalankan animasi tutup dulu!
        StartCoroutine(ClosePanelAndCountdown());
    }

    private IEnumerator ClosePanelAndCountdown()
    {
        isCountingDown = true; // Kunci input agar tidak bisa di-spam

        // 1. Animasi Tutup Panel: Membesar sedikit untuk antisipasi (1.0 ke 1.15)
        float timer = 0f;
        while (timer < 0.15f)
        {
            timer += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(1f, 1.15f, timer / 0.15f);
            pausePanel.transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        // 2. Animasi Tutup Panel: Mengecil tajam menghilang (1.15 ke 0)
        timer = 0f;
        while (timer < 0.2f)
        {
            timer += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(1.15f, 0f, timer / 0.2f);
            pausePanel.transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        pausePanel.SetActive(false); // Baru matikan setelah animasi 0 selesai

        // ==========================================
        // 3. MULAI COUNTDOWN HEBOH SETELAH PANEL HILANG
        // ==========================================
        countdownText.gameObject.SetActive(true);
        int count = 3;

        while (count > 0)
        {
            countdownText.text = count.ToString();
            AudioManager.Instance?.PlaySFX(SoundType.ButtonPressed);

            // Jalankan animasi heboh selama 1 detik (menggantikan WaitForSeconds)
            yield return StartCoroutine(HebohTextAnim());
            count--;
        }

        // Teks START!
        countdownText.text = "START!";
        AudioManager.Instance?.PlaySFX(SoundType.Jump);
        yield return StartCoroutine(HebohTextAnim()); // START! juga ikut animasi heboh

        countdownText.gameObject.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
        isCountingDown = false;

        if (pauseButton != null) pauseButton.SetActive(true);
    }

    // ==========================================
    // ANIMASI TEKS: JANTUNG BERDEBAR (HEBOH)
    // ==========================================
    private IEnumerator HebohTextAnim()
    {
        // Durasi total harus genap 1 Detik agar sinkron dengan timer 3-2-1

        // A. Meledak dari ukuran kecil ke super besar (0.5 ke 1.5)
        float timer = 0f;
        while (timer < 0.15f)
        {
            timer += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(0.5f, 1.5f, timer / 0.15f);
            countdownText.transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        // B. Mengempis cepat ke agak kecil (1.5 ke 0.8)
        timer = 0f;
        while (timer < 0.2f)
        {
            timer += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(1.5f, 0.8f, timer / 0.2f);
            countdownText.transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        // C. Mengayun perlahan kembali ke normal (0.8 ke 1.0) menghabiskan sisa waktu 1 detik
        timer = 0f;
        while (timer < 0.65f) // 0.15 + 0.2 + 0.65 = Pas 1.0 Detik!
        {
            timer += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(0.8f, 1f, timer / 0.65f);
            countdownText.transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
    }
}