using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD References")]
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI timerText;

    [Header("Gacha UI References")]
    public GameObject gachaCardPanel;
    public TextMeshProUGUI gachaEffectText;
    public TextMeshProUGUI gachaDurationText;

    [Header("Endgame UI References")]
    public GameObject winPanel;
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverReasonText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Keamanan tambahan: Pastikan panel popup tersembunyi saat game baru mulai
        if (gachaCardPanel != null) gachaCardPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Langganan Event ke Manager lain
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStatsUpdated += HandleStatsUpdate;
            GameManager.Instance.OnGameWon += HandleGameWon;
            GameManager.Instance.OnGameOver += HandleGameOver;
        }

        if (EffectManager.Instance != null)
        {
            EffectManager.Instance.OnEffectStarted += HandleEffectStarted;
            EffectManager.Instance.OnEffectEnded += HandleEffectEnded;
        }
    }

    private void HandleStatsUpdate(float dist, float time)
    {
        // Langsung update ke teks asli tanpa perantara variabel cache
        if (distanceText != null) distanceText.text = $"{Mathf.CeilToInt(dist)} m";
        if (timerText != null) timerText.text = $"{Mathf.CeilToInt(time)}s";
    }

    private void HandleEffectStarted(string effectName, int duration, bool isPositive)
    {
        if (gachaCardPanel != null)
        {
            gachaCardPanel.SetActive(true);
            if (gachaEffectText != null) gachaEffectText.text = effectName;

            // Catatan: Jika ingin efek warna berbeda (hijau/merah) untuk buff/debuff, 
            // kamu bisa mengubah gachaCardPanel.GetComponent<Image>().color di sini menggunakan parameter isPositive.
            if (gachaDurationText != null) gachaDurationText.text = $"Angka Hoki: {duration} Detik!";
        }
    }

    private void HandleEffectEnded()
    {
        if (gachaCardPanel != null)
        {
            gachaCardPanel.SetActive(false);
        }
    }

    private void HandleGameWon()
    {
        if (winPanel != null) winPanel.SetActive(true);
    }

    private void HandleGameOver(string reason)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (gameOverReasonText != null) gameOverReasonText.text = reason;
        }
    }

    private void OnDestroy()
    {
        // Berhenti langganan event saat scene berganti untuk mencegah Memory Leak
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStatsUpdated -= HandleStatsUpdate;
            GameManager.Instance.OnGameWon -= HandleGameWon;
            GameManager.Instance.OnGameOver -= HandleGameOver;
        }

        if (EffectManager.Instance != null)
        {
            EffectManager.Instance.OnEffectStarted -= HandleEffectStarted;
            EffectManager.Instance.OnEffectEnded -= HandleEffectEnded;
        }
    }
}