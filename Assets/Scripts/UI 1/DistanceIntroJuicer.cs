using UnityEngine;
using System.Collections;
using TMPro;

public class DistanceIntroJuicer : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform distancePanelRect;
    public TextMeshProUGUI distanceText;

    [Header("Juicing Settings")]
    public float centerScaleMultiplier = 2.5f;
    public float rollDuration = 1.8f;
    public float moveDuration = 0.5f;

    private Vector3 originalLocalPos;
    private Vector3 originalScale;
    private Vector3 centerLocalPos;

    // GANTI 'Start' MENJADI 'Awake' UNTUK KALKULASI AWAL
    private void Awake()
    {
        originalLocalPos = distancePanelRect.localPosition;
        originalScale = distancePanelRect.localScale;

        // Sembunyikan panel selagi menunggu layar terang
        distancePanelRect.localScale = Vector3.zero;

        RectTransform parentRect = distancePanelRect.parent as RectTransform;

        if (parentRect != null)
        {
            Vector2 parentCenter = parentRect.rect.center;

            Vector2 anchorCenter = new Vector2(
                Mathf.Lerp(parentRect.rect.xMin, parentRect.rect.xMax, (distancePanelRect.anchorMin.x + distancePanelRect.anchorMax.x) / 2f),
                Mathf.Lerp(parentRect.rect.yMin, parentRect.rect.yMax, (distancePanelRect.anchorMin.y + distancePanelRect.anchorMax.y) / 2f)
            );

            float scaledWidth = distancePanelRect.rect.width * centerScaleMultiplier;
            float scaledHeight = distancePanelRect.rect.height * centerScaleMultiplier;

            Vector2 pivotOffset = new Vector2(
                (distancePanelRect.pivot.x - 0.5f) * scaledWidth,
                (distancePanelRect.pivot.y - 0.5f) * scaledHeight
            );

            centerLocalPos = (Vector3)(parentCenter - anchorCenter + pivotOffset);
            centerLocalPos.z = originalLocalPos.z;
        }
        else
        {
            centerLocalPos = Vector3.zero;
        }
    }

    // DAFTARKAN EVENT SAAT SCRIPT AKTIF
    private void OnEnable()
    {
        SceneController.OnSceneFadeInComplete += BeginIntroAnim;
    }

    // CABUT EVENT SAAT SCRIPT MATI (PENTING AGAR TIDAK MEMORY LEAK)
    private void OnDisable()
    {
        SceneController.OnSceneFadeInComplete -= BeginIntroAnim;
    }

    // FUNGSI INI BARU AKAN TERPANGGIL SETELAH LAYAR BENING 100%
    private void BeginIntroAnim()
    {
        StartCoroutine(IntroSequence());
    }

    private IEnumerator IntroSequence()
    {
        distancePanelRect.localPosition = centerLocalPos;
        distancePanelRect.localScale = Vector3.zero;
        distanceText.text = "0 m";

        Vector3 targetCenterScale = originalScale * centerScaleMultiplier;

        // 1. POP-UP
        float timer = 0f;
        while (timer < 0.2f)
        {
            timer += Time.deltaTime;
            float s = Mathf.Lerp(0f, 1.15f, timer / 0.2f);
            distancePanelRect.localScale = targetCenterScale * s;
            yield return null;
        }

        timer = 0f;
        while (timer < 0.15f)
        {
            timer += Time.deltaTime;
            float s = Mathf.Lerp(1.15f, 1f, timer / 0.15f);
            distancePanelRect.localScale = targetCenterScale * s;
            yield return null;
        }
        distancePanelRect.localScale = targetCenterScale;

        yield return new WaitForSeconds(0.2f);

        // 2. ROLL ANGKA
        timer = 0f;
        float targetDistance = GameManager.Instance.initialDistance;

        while (timer < rollDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / rollDuration);
            float easeOutT = 1f - Mathf.Pow(1f - t, 3f);

            float currentDist = Mathf.Lerp(0f, targetDistance, easeOutT);
            distanceText.text = $"{Mathf.FloorToInt(currentDist)} m";

            float jitterAmount = 0.03f * (1f - easeOutT);
            float jitter = Random.Range(-jitterAmount, jitterAmount);
            distancePanelRect.localScale = targetCenterScale + new Vector3(jitter, jitter, 0f);

            yield return null;
        }

        distanceText.text = $"{targetDistance} m";
        distancePanelRect.localScale = targetCenterScale;

        AudioManager.Instance?.PlaySFX(SoundType.ButtonPressed);

        yield return new WaitForSeconds(0.6f);

        // 3. TERBANG KE POJOK
        timer = 0f;
        AudioManager.Instance?.PlaySFX(SoundType.Jump);

        while (timer < moveDuration)
        {
            timer += Time.deltaTime;
            float t = timer / moveDuration;
            float easeOutT = t * (2f - t);

            distancePanelRect.localPosition = Vector3.Lerp(centerLocalPos, originalLocalPos, easeOutT);
            distancePanelRect.localScale = Vector3.Lerp(targetCenterScale, originalScale, easeOutT);
            yield return null;
        }

        distancePanelRect.localPosition = originalLocalPos;
        distancePanelRect.localScale = originalScale;

        // 4. MULAI GAME!
        GameManager.Instance.BeginPlaying();
    }
}