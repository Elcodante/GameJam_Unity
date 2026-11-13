using System;
using UnityEngine;

public enum GameState { Ready, Playing, Won, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Lucky Number - Initial Distance (Meters)")]
    public float minDistance = 300f;
    public float maxDistance = 600f;
    [Tooltip("Buffer ekstra waktu (1.6x = 60% waktu cadangan untuk antisipasi debuff/stumble)")]
    public float timeBufferMultiplier = 1.6f;

    [Header("Live Game Data")]
    public GameState currentState = GameState.Ready;
    public float initialDistance { get; private set; }
    public float remainingDistance { get; private set; }
    public float totalTimer { get; private set; }
    public float remainingTimer { get; private set; }

    [Header("Endgame Flags")]
    public float toiletSpawnThreshold = 15f; // Jarak sisa saat toilet mulai di-spawn
    [HideInInspector] public bool isToiletSpawned = false;

    // Events untuk UI & Audio
    public event Action<float, float> OnStatsUpdated; // (remainingDist, remainingTime)
    public event Action OnGameWon;
    public event Action<string> OnGameOver; // string: alasan kalah

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        StartNewGame();
    }

    public void StartNewGame()
    {
        // 1. ROLL LUCKY NUMBER: Mengundi jarak ke toilet
        initialDistance = Mathf.Round(UnityEngine.Random.Range(minDistance, maxDistance));
        remainingDistance = initialDistance;

        // 2. KALKULASI TIMER: Waktu tempuh bersih + Safety Buffer
        float baseSpeed = PlayerController.Instance != null ? PlayerController.Instance.baseRunSpeed : 12f;
        float cleanRunTime = initialDistance / baseSpeed;
        totalTimer = Mathf.Round(cleanRunTime * timeBufferMultiplier);
        remainingTimer = totalTimer;

        isToiletSpawned = false;
        currentState = GameState.Ready;

        Debug.Log($"<color=cyan>[Lucky Number]</color> Jarak: {initialDistance}m | Waktu Kebelet: {totalTimer}s");
    }

    public void BeginPlaying()
    {
        currentState = GameState.Playing;
        Debug.Log("<color=green>[START]</color> Lari!!!");
    }

    private void Update()
    {
        if (currentState != GameState.Playing) return;

        UpdateDistanceAndTimer();
    }

    private void UpdateDistanceAndTimer()
    {
        float currentSpeed = PlayerController.Instance != null ? PlayerController.Instance.currentRunSpeed : 0f;

        // Jarak berkurang berdasarkan kecepatan riil player
        remainingDistance -= currentSpeed * Time.deltaTime;
        remainingDistance = Mathf.Max(0, remainingDistance);

        // Timer kebelet berkurang
        remainingTimer -= Time.deltaTime;
        remainingTimer = Mathf.Max(0, remainingTimer);

        // Kirim event update ke UI
        OnStatsUpdated?.Invoke(remainingDistance, remainingTimer);

        // Cek Kondisi Kalah: Waktu Kebelet Habis (BAB di celana)
        if (remainingTimer <= 0)
        {
            TriggerGameOver("WAKTU HABIS! Kamu tidak sempat sampai ke toilet...");
            return;
        }

        // Cek Trigger Menang (ketika sisa jarak = 0)
        // Catatan: Jika ada prefab toilet, player menang saat menabrak pintu toilet.
        // Ini adalah fallback jika toilet belum dipasang di Batch 3.
        if (remainingDistance <= 0 && !isToiletSpawned)
        {
            TriggerWin();
        }
    }

    public void AddUrgencyTime(float seconds)
    {
        remainingTimer += seconds;
        Debug.Log($"<color=green>[Buff]</color> Nafas Lega! Waktu bertambah +{seconds}s");
    }

    public void TriggerGameOver(string reason)
    {
        if (currentState != GameState.Playing) return;

        currentState = GameState.GameOver;
        if (PlayerController.Instance != null) PlayerController.Instance.canControl = false;

        AudioManager.Instance?.PlaySFX(SoundType.Lose);
        Debug.Log($"<color=red>[GAME OVER]</color> {reason}");
        OnGameOver?.Invoke(reason);
    }

    public void TriggerWin()
    {
        if (currentState != GameState.Playing) return;

        currentState = GameState.Won;
        if (PlayerController.Instance != null) PlayerController.Instance.canControl = false;

        AudioManager.Instance?.PlaySFX(SoundType.Win);
        Debug.Log("<color=yellow>[VICTORY]</color> Haaaah... Tepat waktu sampai di toilet!");
        OnGameWon?.Invoke();
    }
}