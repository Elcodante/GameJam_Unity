using UnityEngine;
using TMPro; // Wajib jika Click To Play menggunakan TextMeshPro

public class MainMenuJuicer : MonoBehaviour
{
    [Header("Logo Settings (Denyut Jantung)")]
    public RectTransform logoRect;
    public float logoPulseSpeed = 6f;
    public float logoPulseAmount = 0.05f;
    private Vector3 baseLogoScale;

    [Header("Text Settings (Napas)")]
    public TextMeshProUGUI clickToPlayText;
    public float textFadeSpeed = 4f;

    private void Start()
    {
        if (logoRect != null) baseLogoScale = logoRect.localScale;
    }

    private void Update()
    {
        // 1. Animasi Logo Berdenyut
        if (logoRect != null)
        {
            // Menghasilkan nilai naik turun menggunakan gelombang Sinus
            float pulse = Mathf.Sin(Time.time * logoPulseSpeed) * logoPulseAmount;
            logoRect.localScale = baseLogoScale + new Vector3(pulse, pulse, 0f);
        }

        // 2. Animasi Teks Bernapas
        if (clickToPlayText != null)
        {
            // Normalisasi nilai Sin (dari -1 s/d 1) menjadi (0 s/d 1)
            float alpha = (Mathf.Sin(Time.time * textFadeSpeed) + 1f) / 2f;

            // Batasi agar tidak sampai hilang sepenuhnya (Lerp antara 0.3 dan 1.0)
            float clampedAlpha = Mathf.Lerp(0.2f, 1f, alpha);

            Color c = clickToPlayText.color;
            clickToPlayText.color = new Color(c.r, c.g, c.b, clampedAlpha);
        }
    }
}