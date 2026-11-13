using UnityEngine;
using System.Collections;

public class SettingsMenuJuicer : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Objek utama Panel Settings (yang memiliki background gelap)")]
    public GameObject settingsPanelRoot;

    [Tooltip("Bagian kotak yang berisi tombol & slider (agar background gelap tidak ikut memantul)")]
    public Transform windowVisual;

    private void Start()
    {
        // Pastikan panel mati saat game baru mulai
        if (settingsPanelRoot != null) settingsPanelRoot.SetActive(false);
    }

    // ==========================================
    // DIPANGGIL OLEH TOMBOL "SETTINGS" (Contoh: di Main Menu / Pause)
    // ==========================================
    public void OpenSettings()
    {
        settingsPanelRoot.SetActive(true);
        AudioManager.Instance?.PlaySFX(SoundType.ButtonPressed);

        StartCoroutine(OpenAnim());
    }

    // ==========================================
    // DIPANGGIL OLEH TOMBOL "X" / "CLOSE" DI DALAM SETTINGS
    // ==========================================
    public void CloseSettings()
    {
        AudioManager.Instance?.PlaySFX(SoundType.ButtonPressed);

        // Jangan langsung SetActive(false)! Mainkan animasi tutup dulu
        StartCoroutine(CloseAnim());
    }

    private IEnumerator OpenAnim()
    {
        // Mulai dari ukuran 0
        windowVisual.localScale = Vector3.zero;

        // Fase 1: Melenting membesar melampaui ukuran asli (0 ke 1.1)
        float timer = 0f;
        while (timer < 0.2f)
        {
            timer += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(0f, 1.1f, timer / 0.2f);
            windowVisual.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        // Fase 2: Mengempis pas ke ukuran asli (1.1 ke 1.0)
        timer = 0f;
        while (timer < 0.15f)
        {
            timer += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(1.1f, 1f, timer / 0.15f);
            windowVisual.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        windowVisual.localScale = Vector3.one;
    }

    private IEnumerator CloseAnim()
    {
        // Fase 1: Antisipasi, membesar sedikit sebelum menghilang (1.0 ke 1.1)
        float timer = 0f;
        while (timer < 0.15f)
        {
            timer += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(1f, 1.1f, timer / 0.15f);
            windowVisual.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        // Fase 2: Tersedot cepat menghilang (1.1 ke 0)
        timer = 0f;
        while (timer < 0.15f)
        {
            timer += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(1.1f, 0f, timer / 0.15f);
            windowVisual.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        // Setelah kotak visualnya hilang 100%, baru matikan panel utamanya
        settingsPanelRoot.SetActive(false);
    }
}