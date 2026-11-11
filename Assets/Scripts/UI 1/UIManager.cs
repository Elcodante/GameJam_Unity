using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

// Struct untuk mendaftarkan gambar di Inspector
[System.Serializable]
public struct GachaVisual
{
    public EffectType type;
    public Sprite cardSprite;
}

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD References")]
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI timerText;

    [Header("Gacha UI References")]
    public GameObject gachaCardPanel;
    public Image gachaCardImage; // Tambahan komponen Image untuk mengganti gambar
    public TextMeshProUGUI gachaDurationText;

    [Header("Gacha Assets Library")]
    public GachaVisual[] gachaVisuals;
    private Dictionary<EffectType, Sprite> gachaDictionary;

    [Header("Endgame UI References")]
    public GameObject winPanel;
    public GameObject gameOverPanel;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Inisialisasi Dictionary untuk pencarian gambar O(1)
        gachaDictionary = new Dictionary<EffectType, Sprite>();
        foreach (var visual in gachaVisuals)
        {
            if (!gachaDictionary.ContainsKey(visual.type))
            {
                gachaDictionary.Add(visual.type, visual.cardSprite);
            }
        }
    }

    private void Start()
    {
        if (gachaCardPanel != null) gachaCardPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStatsUpdated += HandleStatsUpdate;
            GameManager.Instance.OnGameWon += HandleGameWon;
            GameManager.Instance.OnGameOver += HandleGameOver;
        }

        if (EffectManager.Instance != null)
        {
            // Perhatikan perubahan parameter di sini menjadi EffectType
            EffectManager.Instance.OnEffectStarted += HandleEffectStarted;
            EffectManager.Instance.OnEffectEnded += HandleEffectEnded;
        }
    }

    private void HandleStatsUpdate(float dist, float time)
    {
        if (distanceText != null) distanceText.text = $"{Mathf.CeilToInt(dist)} m";
        if (timerText != null) timerText.text = $"{Mathf.CeilToInt(time)}s";
    }

    // Fungsi ini menangkap EffectType dari EffectManager
    private void HandleEffectStarted(EffectType effectType, int duration, bool isPositive)
    {
        if (gachaCardPanel != null && gachaCardImage != null)
        {
            // Cari gambar yang sesuai di Dictionary
            if (gachaDictionary.TryGetValue(effectType, out Sprite newSprite))
            {
                // Ganti gambar panel dengan aset yang sesuai
                gachaCardImage.sprite = newSprite;
            }

            gachaCardPanel.SetActive(true);
            if (gachaDurationText != null) gachaDurationText.text = $"Durasi: {duration} Detik";
        }
    }

    private void HandleEffectEnded()
    {
        if (gachaCardPanel != null) gachaCardPanel.SetActive(false);
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
        }
    }

    private void OnDestroy()
    {
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