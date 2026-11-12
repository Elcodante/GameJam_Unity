using System.Collections;
using UnityEngine;

/// <summary>
/// Kamera runner yang dirancang agar tidak membuat mual.
///
/// Prinsip (SYBO: "smooth horizon, no sharp turns"; prinsip kamera follow secara umum):
///  1. Kamera BERGESER ke samping (translasi), tidak memutar pandangan saat pindah jalur.
///  2. Gerak vertikal diredam lebih besar dari horizontal, supaya layar tidak naik-turun tiap lompat.
///  3. Awal dan akhir gerak selalu halus (SmoothDamp), tidak pernah menempel kaku ke target.
///  4. Kamera melihat lintasan di depan player (look-ahead), tapi titik pandangnya tidak berayun.
///  5. Efek (FOV, shake, roll) dibatasi oleh Comfort Mode dan dipisah dari gerak mengikuti.
/// </summary>
[RequireComponent(typeof(Camera))]
public class ProceduralCamera : MonoBehaviour
{
    public static ProceduralCamera Instance { get; private set; }

    [Header("Follow Settings")]
    public Transform target; // Objek Player
    public Vector3 offset = new Vector3(0f, 3.5f, -5.5f);
    [Tooltip("Seberapa banyak kamera ikut bergeser ke samping saat player pindah jalur (1 = penuh, 0 = kamera diam)")]
    [Range(0f, 1f)] public float lateralFollow = 0.5f;
    [Tooltip("Seberapa banyak kamera ikut naik saat player melompat")]
    [Range(0f, 1f)] public float verticalFollow = 0.3f;
    [Tooltip("Tinggi permukaan tanah (Y)")]
    public float groundLevelY = 0f;
    public float smoothTimeX = 0.18f;
    [Tooltip("Lebih besar dari X dengan sengaja: damping vertikal adalah pengaruh terbesar pada mual di game lompat")]
    public float smoothTimeY = 0.35f;
    public float smoothTimeZ = 0.5f;

    [Header("Look Ahead (kamera melihat ke arah lintasan, bukan ke punggung player)")]
    [Tooltip("Titik pandang di depan player (meter). 0 = menatap player seperti versi lama")]
    public float lookAhead = 7f;
    public float lookAheadBoost = 2f;   // tambahan saat ngebut
    public float lookHeight = 1.5f;
    [Tooltip("Samakan dengan Lateral Follow supaya arah pandang TETAP LURUS saat pindah jalur (hanya bergeser, tidak menyapu layar)")]
    [Range(0f, 1f)] public float lookLateralFollow = 0.5f;
    [Tooltip("Samakan dengan Vertical Follow supaya kemiringan pandangan tetap saat melompat")]
    [Range(0f, 1f)] public float lookVerticalFollow = 0.3f;

    [Header("Dynamic Speed VFX")]
    public float baseFOV = 60f;
    public float maxSpeedFOV = 68f;
    public float fovLerpSpeed = 3f;
    public float maxPullbackZ = -1.2f;      // Mundur sedikit saat ngebut
    [Tooltip("Rasio kecepatan (current/base) yang dianggap boost penuh. Speed Boost = 1.4")]
    public float boostRatioForMax = 1.4f;
    [Tooltip("Rasio kecepatan yang dianggap slow penuh. Kram Perut = 0.65")]
    public float slowRatioForMax = 0.65f;
    public float slowFOVReduction = 3f;

    [Header("Slide & Landing")]
    [Tooltip("Kamera turun sedikit saat player slide")]
    public float slideCameraDip = -0.2f;
    [Tooltip("Dorongan kamera ke bawah saat player mendarat dari lompatan")]
    public float landingKickStrength = 0.015f;
    public float maxLandingKick = 0.3f;

    [Header("Bank Roll (kamera miring ke arah belokan)")]
    [Tooltip("0 = mati. Roll kamera adalah pemicu mual yang kuat; Subway Surfers tidak memakainya.")]
    public float maxBankRoll = 0f;
    public float bankVelocityReference = 5f;
    public float bankResponse = 8f;

    [Header("Shake")]
    [Tooltip("Derajat roll per 1 meter intensitas shake")]
    public float shakeRollDegrees = 1.5f;
    public float shakeFrequency = 18f;
    [Tooltip("Lama pudar di akhir shake (detik)")]
    public float shakeFadeOut = 0.35f;
    [Tooltip("Getar otomatis saat game over")]
    public float gameOverShakeDuration = 0.45f;
    public float gameOverShakeIntensity = 0.5f;

    [Header("Comfort Mode (anti-mual)")]
    [Tooltip("Membatasi efek kamera agar tetap nyaman, apa pun nilai yang diisi di atas. Matikan hanya untuk efek yang sengaja ekstrem.")]
    public bool comfortMode = true;
    [Tooltip("Maksimal perubahan FOV dari Base FOV (derajat)")]
    public float comfortMaxFovDelta = 8f;
    [Tooltip("Pengali semua getaran kamera (termasuk debuff Tremor)")]
    [Range(0f, 1f)] public float comfortShakeScale = 0.6f;
    [Tooltip("Batas simpangan getaran (meter)")]
    public float comfortMaxShakeAmplitude = 0.3f;
    [Tooltip("Batas roll kamera (derajat)")]
    public float comfortMaxRoll = 1f;

    // Smoothed state
    private float camX, camY, camZ;
    private float lookX, lookY, lookZ;
    private float vCamX, vCamY, vCamZ;
    private float vLookX, vLookY, vLookZ;
    private float slideBlend;
    private float bankRoll;

    // Landing kick spring
    private float kickY, kickVelY;

    // Shake state
    private float shakeTimeLeft = 0f;
    private float shakeAmplitude = 0f;

    [Header("White Flash (Win VFX)")]
    private float whiteFadeAlpha = 0f;
    private Texture2D whiteTexture;

    private Camera cam;
    private bool snapped = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        cam = GetComponent<Camera>();
        cam.fieldOfView = baseFOV;

        // Texture 1x1 putih polos untuk fade kemenangan
        whiteTexture = new Texture2D(1, 1);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();
    }

    private void Start()
    {
        if (target == null && PlayerController.Instance != null)
        {
            target = PlayerController.Instance.transform;
        }

        SnapToTarget();

        if (GameManager.Instance != null)
        {
            //GameManager.Instance.OnGameWon += TriggerWhiteFlash;
            GameManager.Instance.OnGameOver += HandleGameOver;
        }
    }

    // =====================================================
    // POSE
    // =====================================================
    private void ComputeDesired(float boost01, float slow01, out Vector3 pos, out Vector3 look)
    {
        Vector3 t = target.position;
        float height = t.y - groundLevelY;

        pos.x = t.x * lateralFollow + offset.x;
        pos.y = groundLevelY + offset.y + height * verticalFollow + slideBlend * slideCameraDip;
        pos.z = t.z + offset.z + Mathf.Lerp(0f, maxPullbackZ, boost01) + slow01 * 0.4f;

        look.x = t.x * lookLateralFollow;
        look.y = groundLevelY + lookHeight + height * lookVerticalFollow;
        look.z = t.z + lookAhead + lookAheadBoost * boost01;
    }

    private void SnapToTarget()
    {
        if (target == null || snapped) return;

        Vector3 pos, look;
        ComputeDesired(0f, 0f, out pos, out look);
        camX = pos.x; camY = pos.y; camZ = pos.z;
        lookX = look.x; lookY = look.y; lookZ = look.z;

        transform.position = pos;
        transform.rotation = Quaternion.LookRotation(look - pos, Vector3.up);
        snapped = true;
    }

    private void LateUpdate()
    {
        if (target == null) return;
        if (!snapped) SnapToTarget();

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        PlayerController player = PlayerController.Instance;

        // 1. Rasio kecepatan -> boost / slow (0..1)
        float speedRatio = 1f;
        if (player != null && player.baseRunSpeed > 0.01f)
        {
            speedRatio = player.currentRunSpeed / player.baseRunSpeed;
        }
        float boost01 = Mathf.InverseLerp(1f, boostRatioForMax, speedRatio);
        float slow01 = Mathf.InverseLerp(1f, slowRatioForMax, speedRatio);

        // 2. Slide: kamera turun halus
        float slideTarget = (player != null && player.IsSliding) ? 1f : 0f;
        slideBlend = Mathf.MoveTowards(slideBlend, slideTarget, dt * 4f);

        // 3. Smooth follow (SmoothDamp, aman di FPS berapa pun)
        Vector3 desiredPos, desiredLook;
        ComputeDesired(boost01, slow01, out desiredPos, out desiredLook);

        camX = Mathf.SmoothDamp(camX, desiredPos.x, ref vCamX, smoothTimeX, Mathf.Infinity, dt);
        camY = Mathf.SmoothDamp(camY, desiredPos.y, ref vCamY, smoothTimeY, Mathf.Infinity, dt);
        camZ = Mathf.SmoothDamp(camZ, desiredPos.z, ref vCamZ, smoothTimeZ, Mathf.Infinity, dt);

        lookX = Mathf.SmoothDamp(lookX, desiredLook.x, ref vLookX, smoothTimeX, Mathf.Infinity, dt);
        lookY = Mathf.SmoothDamp(lookY, desiredLook.y, ref vLookY, smoothTimeY, Mathf.Infinity, dt);
        lookZ = Mathf.SmoothDamp(lookZ, desiredLook.z, ref vLookZ, smoothTimeZ, Mathf.Infinity, dt);

        // 4. Landing kick (pegas kecil ke bawah lalu kembali)
        kickVelY += (-180f * kickY - 16f * kickVelY) * dt;
        kickY += kickVelY * dt;

        // 5. Shake (dibatasi Comfort Mode)
        Vector3 shakeOffset = Vector3.zero;
        float shakeRoll = 0f;
        if (shakeTimeLeft > 0f)
        {
            float amp = CurrentShakeAmplitude();
            float time = Time.time * shakeFrequency;
            float nx = (Mathf.PerlinNoise(time, 0.37f) - 0.5f) * 2f;
            float ny = (Mathf.PerlinNoise(11.7f, time) - 0.5f) * 2f;
            float nr = (Mathf.PerlinNoise(time, 53.1f) - 0.5f) * 2f;

            shakeOffset.x = nx * amp;
            shakeOffset.y = ny * amp;
            shakeRoll = nr * amp * shakeRollDegrees;

            shakeTimeLeft -= dt;
            if (shakeTimeLeft <= 0f) shakeAmplitude = 0f;
        }

        // 6. Bank roll mengikuti kecepatan geser kamera (default mati)
        float bankMax = comfortMode ? Mathf.Min(maxBankRoll, comfortMaxRoll) : maxBankRoll;
        float bankTarget = -Mathf.Clamp(vCamX / Mathf.Max(0.01f, bankVelocityReference), -1f, 1f) * bankMax;
        bankRoll = Mathf.Lerp(bankRoll, bankTarget, 1f - Mathf.Exp(-bankResponse * dt));

        float totalRoll = bankRoll + shakeRoll;
        if (comfortMode) totalRoll = Mathf.Clamp(totalRoll, -comfortMaxRoll, comfortMaxRoll);

        // 7. Terapkan pose
        Vector3 basePos = new Vector3(camX, camY, camZ);
        Vector3 lookPoint = new Vector3(lookX, lookY, lookZ);

        transform.position = new Vector3(camX, camY + kickY, camZ) + shakeOffset;
        Quaternion rotation = Quaternion.LookRotation(lookPoint - basePos, Vector3.up);
        transform.rotation = rotation * Quaternion.Euler(0f, 0f, totalRoll);

        // 8. Dynamic FOV (perubahan dibatasi Comfort Mode)
        float maxFov = comfortMode ? Mathf.Min(maxSpeedFOV, baseFOV + comfortMaxFovDelta) : maxSpeedFOV;
        float fovSpeed = comfortMode ? Mathf.Min(fovLerpSpeed, 3f) : fovLerpSpeed;
        float targetFOV = Mathf.Lerp(baseFOV, maxFov, boost01) - slowFOVReduction * slow01;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, 1f - Mathf.Exp(-fovSpeed * dt));
    }

    private float CurrentShakeAmplitude()
    {
        float fade = shakeFadeOut > 0.001f ? Mathf.Clamp01(shakeTimeLeft / shakeFadeOut) : 1f;
        fade = fade * fade * (3f - 2f * fade); // smoothstep

        float amp = shakeAmplitude * fade;
        if (comfortMode)
        {
            amp = Mathf.Min(amp * comfortShakeScale, comfortMaxShakeAmplitude);
        }
        return amp;
    }

    // =====================================================
    // PUBLIC API
    // =====================================================
    /// <summary>
    /// Getar kamera. Tetap penuh selama durasi, lalu memudar di akhir.
    /// Pemanggilan bertumpuk tidak memotong getaran yang lebih kuat.
    /// </summary>
    public void TriggerShake(float duration, float intensity)
    {
        float currentAmp = shakeAmplitude;
        if (shakeTimeLeft > 0f && shakeFadeOut > 0.001f)
        {
            float fade = Mathf.Clamp01(shakeTimeLeft / shakeFadeOut);
            currentAmp *= fade * fade * (3f - 2f * fade);
        }

        shakeAmplitude = Mathf.Max(currentAmp, intensity);
        shakeTimeLeft = Mathf.Max(shakeTimeLeft, duration);
    }

    /// <summary>Dipanggil PlayerController saat mendarat dari lompatan.</summary>
    public void OnPlayerLanded(float impactSpeed)
    {
        float kick = Mathf.Min(impactSpeed * landingKickStrength, maxLandingKick);
        kickVelY -= kick * 14f; // dorong ke bawah, pegas yang mengembalikan
    }

    private void HandleGameOver(string reason)
    {
        TriggerShake(gameOverShakeDuration, gameOverShakeIntensity);
    }

    public void TriggerWhiteFlash()
    {
        StartCoroutine(WhiteFlashRoutine());
    }

    private IEnumerator WhiteFlashRoutine()
    {
        float timer = 0f;
        float duration = 0.4f; // Cepat dan membutakan
        while (timer < duration)
        {
            timer += Time.deltaTime;
            whiteFadeAlpha = Mathf.Clamp01(timer / duration);
            yield return null;
        }
        whiteFadeAlpha = 1f;
    }

    /// <summary>
    /// Jika komponen ini sudah ada di scene dengan nilai lama yang tersimpan, Unity tidak memakai default baru.
    /// Klik kanan komponen > Apply Comfort Preset untuk menerapkan nilai anti-mual sekaligus.
    /// </summary>
    [ContextMenu("Apply Comfort Preset")]
    private void ApplyComfortPreset()
    {
        lateralFollow = 0.5f;
        verticalFollow = 0.3f;
        smoothTimeX = 0.18f;
        smoothTimeY = 0.35f;
        smoothTimeZ = 0.5f;
        lookLateralFollow = 0.5f;
        lookVerticalFollow = 0.3f;
        lookAheadBoost = 2f;
        maxSpeedFOV = Mathf.Min(maxSpeedFOV, baseFOV + comfortMaxFovDelta);
        fovLerpSpeed = Mathf.Min(fovLerpSpeed, 3f);
        slowFOVReduction = 3f;
        slideCameraDip = -0.2f;
        landingKickStrength = 0.015f;
        maxLandingKick = 0.3f;
        maxBankRoll = 0f;
        shakeRollDegrees = 1.5f;
        shakeFrequency = 18f;
        comfortMode = true;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log("[ProceduralCamera] Comfort preset diterapkan.");
    }

    // Menggambar layar putih instan tanpa perlu asset grafis
    private void OnGUI()
    {
        if (whiteFadeAlpha > 0)
        {
            GUI.color = new Color(1, 1, 1, whiteFadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTexture);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            //GameManager.Instance.OnGameWon -= TriggerWhiteFlash;
            GameManager.Instance.OnGameOver -= HandleGameOver;
        }
        if (whiteTexture != null) Destroy(whiteTexture);
    }
}