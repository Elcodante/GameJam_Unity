using System.Collections;
using UnityEngine;

public class UIPopupAnimator : MonoBehaviour
{
    [Header("Bounce Settings")]
    public float animationSpeed = 5f;
    public float overshootAmount = 1.15f; // Membesar sampai 115% dulu

    private Vector3 originalScale;

    private void Awake()
    {
        // Simpan ukuran asli panel
        originalScale = transform.localScale;
    }

    // Fungsi ini OTOMATIS terpanggil oleh Unity setiap kali SetActive(true) terjadi
    private void OnEnable()
    {
        transform.localScale = Vector3.zero; // Mulai dari ukuran 0
        StartCoroutine(BounceRoutine());
    }

    private IEnumerator BounceRoutine()
    {
        float timer = 0f;
        // Fase 1: Membesar dengan cepat melewati batas (Overshoot)
        while (timer < 1f)
        {
            timer += Time.unscaledDeltaTime * animationSpeed;
            transform.localScale = Vector3.Lerp(Vector3.zero, originalScale * overshootAmount, timer);
            yield return null;
        }

        timer = 0f;
        // Fase 2: Mengempis kembali ke ukuran normal dengan lebih halus
        while (timer < 1f)
        {
            timer += Time.unscaledDeltaTime * (animationSpeed * 1.5f);
            transform.localScale = Vector3.Lerp(originalScale * overshootAmount, originalScale, timer);
            yield return null;
        }

        transform.localScale = originalScale; // Pastikan presisi di akhir
    }
}