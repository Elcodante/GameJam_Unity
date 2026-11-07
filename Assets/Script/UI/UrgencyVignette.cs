using UnityEngine;

public class UrgencyVignette : MonoBehaviour
{
    public static UrgencyVignette Instance { get; private set; }

    [Header("Urgency Trigger Threshold")]
    [Tooltip("Persentase sisa waktu untuk mulai memicu denyut panik (0.25 = 25% waktu sisa)")]
    public float criticalThreshold = 0.25f;

    private Texture2D vignetteTexture;
    private float pulseAlpha = 0f;
    private bool forcePanic = false; // Dipicu oleh debuff kram / tremor

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        GenerateVignetteTexture();
    }

    private void GenerateVignetteTexture()
    {
        // Membuat tekstur gradasi lingkaran (pinggir gelap/merah, tengah transparan)
        int size = 128;
        vignetteTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float factor = Mathf.Clamp01(dist / maxDist);
                // Semakin ke pinggir semakin tebal
                float alpha = Mathf.Pow(factor, 2.5f);
                vignetteTexture.SetPixel(x, y, new Color(0.8f, 0.1f, 0.1f, alpha));
            }
        }
        vignetteTexture.Apply();
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameState.Playing)
        {
            pulseAlpha = 0f;
            return;
        }

        float timeRatio = GameManager.Instance.remainingTimer / GameManager.Instance.totalTimer;

        // Denyut jantung (heartbeat pulse) saat waktu kritis atau saat kena debuff
        if (timeRatio <= criticalThreshold || forcePanic)
        {
            float pulseSpeed = (timeRatio <= 0.1f) ? 10f : 5f; // Makin kritis makin cepat berdenyut
            pulseAlpha = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f * 0.6f;
        }
        else
        {
            pulseAlpha = Mathf.Lerp(pulseAlpha, 0f, 5f * Time.deltaTime);
        }
    }

    public void SetPanicState(bool active)
    {
        forcePanic = active;
    }

    private void OnGUI()
    {
        if (pulseAlpha > 0.01f && vignetteTexture != null)
        {
            GUI.color = new Color(1f, 1f, 1f, pulseAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), vignetteTexture);
            GUI.color = Color.white;
        }
    }
}