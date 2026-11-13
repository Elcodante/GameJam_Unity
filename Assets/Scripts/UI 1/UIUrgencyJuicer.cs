using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class UIUrgencyJuicer : MonoBehaviour
{
    [Header("Urgency Settings")]
    public float panicThreshold = 15f; // Mulai panik di bawah 15 detik
    public float heartbeatSpeed = 8f;
    public float maxScaleMultiplier = 1.2f;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color panicColor = Color.red;

    private TextMeshProUGUI myText;
    private Vector3 baseScale;

    private void Start()
    {
        myText = GetComponent<TextMeshProUGUI>();
        baseScale = transform.localScale;
    }

    private void Update()
    {
        // Membaca data langsung dari GameManager
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameState.Playing) return;

        float timeLeft = GameManager.Instance.remainingTimer;

        if (timeLeft <= panicThreshold)
        {
            // Menghasilkan detak jantung yang semakin cepat saat waktu makin habis
            float speedMultiplier = Mathf.Lerp(2f, 1f, timeLeft / panicThreshold);
            float throb = (Mathf.Sin(Time.time * heartbeatSpeed * speedMultiplier) + 1f) / 2f;

            // Efek membesar/mengecil
            float currentScale = Mathf.Lerp(1f, maxScaleMultiplier, throb);
            transform.localScale = baseScale * currentScale;

            // Efek kedip warna (merah ke putih)
            myText.color = Color.Lerp(normalColor, panicColor, throb);
        }
        else
        {
            // Tenang kembali
            transform.localScale = baseScale;
            myText.color = normalColor;
        }
    }
}