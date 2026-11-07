using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Modular Mode")]
    [Tooltip("Jika TRUE: UI digambar otomatis via code. Jika FALSE: Menggunakan komponen UI dari Artist 2D di bawah.")]
    public bool usePrototypeUI = true;

    [Header("Custom 2D Artist UI (Digunakan jika usePrototypeUI = FALSE)")]
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI timerText;
    public Slider timerSlider;
    public GameObject gachaCardPanel;
    public TextMeshProUGUI gachaEffectText;
    public TextMeshProUGUI gachaDurationText;
    public GameObject winPanel;
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverReasonText;

    // Prototype State Cache
    private float displayDist;
    private float displayTime;
    private string gachaTitle = "";
    private int gachaDuration = 0;
    private bool gachaIsPositive = true;
    private float gachaNotificationTimer = 0f;
    private bool isWon = false;
    private bool isGameOver = false;
    private string loseReason = "";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Hubungkan ke GameManager & EffectManager
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

    private void Update()
    {
        if (gachaNotificationTimer > 0)
        {
            gachaNotificationTimer -= Time.deltaTime;
        }
    }

    private void HandleStatsUpdate(float dist, float time)
    {
        displayDist = dist;
        displayTime = time;

        if (!usePrototypeUI)
        {
            if (distanceText != null) distanceText.text = $"{Mathf.CeilToInt(dist)} m";
            if (timerText != null) timerText.text = $"{Mathf.CeilToInt(time)}s";
            if (timerSlider != null && GameManager.Instance != null)
            {
                timerSlider.maxValue = GameManager.Instance.totalTimer;
                timerSlider.value = time;
            }
        }
    }

    private void HandleEffectStarted(string effectName, int duration, bool isPositive)
    {
        gachaTitle = effectName;
        gachaDuration = duration;
        gachaIsPositive = isPositive;
        gachaNotificationTimer = 2.5f;

        if (!usePrototypeUI && gachaCardPanel != null)
        {
            gachaCardPanel.SetActive(true);
            if (gachaEffectText != null) gachaEffectText.text = effectName;
            if (gachaDurationText != null) gachaDurationText.text = $"Angka Hoki: {duration} Detik!";
        }
    }

    private void HandleEffectEnded()
    {
        if (!usePrototypeUI && gachaCardPanel != null)
        {
            gachaCardPanel.SetActive(false);
        }
    }

    private void HandleGameWon()
    {
        isWon = true;
        if (!usePrototypeUI && winPanel != null) winPanel.SetActive(true);
    }

    private void HandleGameOver(string reason)
    {
        isGameOver = true;
        loseReason = reason;
        if (!usePrototypeUI && gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (gameOverReasonText != null) gameOverReasonText.text = reason;
        }
    }

    // ==========================================
    // PROTOTYPE UI SYSTEM (ZERO ASSET / CODE-BASED)
    // ==========================================
    private void OnGUI()
    {
        if (!usePrototypeUI) return;

        GUI.skin.label.fontSize = 20;
        GUI.skin.button.fontSize = 18;

        // 1. HUD Jarak & Urgensi Timer
        GUI.Box(new Rect(20, 20, 260, 90), "");
        GUI.color = Color.cyan;
        GUI.Label(new Rect(30, 25, 240, 30), $"🏁 Jarak Toilet: {Mathf.CeilToInt(displayDist)} M");

        GUI.color = displayTime <= 10f ? Color.red : Color.yellow;
        GUI.Label(new Rect(30, 55, 240, 30), $"🚽 Tahan BAB: {Mathf.CeilToInt(displayTime)} Detik!");
        GUI.color = Color.white;

        // 2. Gacha Pop-up Card Notifikasi
        if (gachaNotificationTimer > 0)
        {
            Rect cardRect = new Rect((Screen.width - 360) / 2f, 40, 360, 80);
            GUI.Box(cardRect, "");
            GUI.color = gachaIsPositive ? Color.green : new Color(1f, 0.4f, 0.4f);
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(cardRect.x, cardRect.y + 10, cardRect.width, 30), $"🎲 ROLL: {gachaTitle}");
            GUI.Label(new Rect(cardRect.x, cardRect.y + 40, cardRect.width, 30), $"Durasi Angka Hoki: {gachaDuration} Detik!");
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        // 3. Victory Screen
        if (isWon)
        {
            Rect winBox = new Rect((Screen.width - 400) / 2f, (Screen.height - 220) / 2f, 400, 220);
            GUI.color = Color.black;
            GUI.Box(winBox, "");
            GUI.color = Color.yellow;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.skin.label.fontSize = 26;
            GUI.Label(new Rect(winBox.x, winBox.y + 20, winBox.width, 40), "✨ SELAMAT! ✨");
            GUI.skin.label.fontSize = 18;
            GUI.color = Color.white;
            GUI.Label(new Rect(winBox.x + 20, winBox.y + 70, winBox.width - 40, 50), "Kamu berhasil sampai di toilet tepat waktu!\nPerutmu kembali damai.");

            if (GUI.Button(new Rect(winBox.x + 100, winBox.y + 140, 200, 50), "Main Lagi"))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
        }

        // 4. Game Over Screen
        if (isGameOver)
        {
            Rect loseBox = new Rect((Screen.width - 420) / 2f, (Screen.height - 240) / 2f, 420, 240);
            GUI.Box(loseBox, "");
            GUI.color = Color.red;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.skin.label.fontSize = 26;
            GUI.Label(new Rect(loseBox.x, loseBox.y + 20, loseBox.width, 40), "💀 GAME OVER 💀");
            GUI.skin.label.fontSize = 16;
            GUI.color = Color.white;
            GUI.Label(new Rect(loseBox.x + 20, loseBox.y + 70, loseBox.width - 40, 60), loseReason);

            if (GUI.Button(new Rect(loseBox.x + 110, loseBox.y + 150, 200, 50), "Coba Lagi"))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
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
    }
}