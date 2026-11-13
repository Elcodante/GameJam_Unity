using UnityEngine;
using System.Collections;

public class UIPopupJuicer : MonoBehaviour
{
    private Vector3 baseScale;
    private Quaternion baseRot;
    private Coroutine currentAnim;

    private void Awake()
    {
        // Simpan ukuran dan rotasi asli yang kamu atur di Inspector
        baseScale = transform.localScale;
        baseRot = transform.localRotation;
    }

    // Dipanggil saat efek dimulai
    public void Show()
    {
        gameObject.SetActive(true);
        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(ShowAnim());
    }

    // Dipanggil saat efek habis
    public void Hide()
    {
        if (!gameObject.activeInHierarchy) return;
        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(HideAnim());
    }

    private IEnumerator ShowAnim()
    {
        transform.localScale = Vector3.zero;
        transform.localRotation = baseRot; // Reset miring

        // Fase 1: Membesar cepat (0 ke 1.15)
        float timer = 0f;
        while (timer < 0.2f)
        {
            timer += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(0f, 1.15f, timer / 0.2f);
            transform.localScale = baseScale * s;
            yield return null;
        }

        // Fase 2: Kembali ke normal (1.15 ke 1)
        timer = 0f;
        while (timer < 0.15f)
        {
            timer += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(1.15f, 1f, timer / 0.15f);
            transform.localScale = baseScale * s;
            yield return null;
        }
        transform.localScale = baseScale;
    }

    private IEnumerator HideAnim()
    {
        // Fase 1: Antisipasi membesar sedikit (1 ke 1.1)
        float timer = 0f;
        while (timer < 0.1f)
        {
            timer += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(1f, 1.1f, timer / 0.1f);
            transform.localScale = baseScale * s;
            yield return null;
        }

        // Fase 2: TERSAPU ANGIN (Mengecil tajam ke 0 sambil miring 15 derajat)
        timer = 0f;
        while (timer < 0.2f)
        {
            timer += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(1.1f, 0f, timer / 0.2f);

            // Efek miring/terbuang
            float angle = Mathf.Lerp(0f, 15f, timer / 0.2f);

            transform.localScale = baseScale * s;
            transform.localRotation = baseRot * Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }

        // Setelah animasi miring dan mengecil selesai, BARU panel dimatikan
        gameObject.SetActive(false);
    }
}