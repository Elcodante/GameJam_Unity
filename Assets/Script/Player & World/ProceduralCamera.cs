using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ProceduralCamera : MonoBehaviour
{
    public static ProceduralCamera Instance { get; private set; }

    [Header("Follow Settings")]
    public Transform target; // Objek Player
    public Vector3 offset = new Vector3(0f, 3.5f, -5.5f);
    public float smoothSpeedX = 10f;
    public float smoothSpeedY = 4f; // Diredam agar kamera tidak pusing saat player lompat

    [Header("Dynamic Speed VFX")]
    public float baseFOV = 60f;
    public float maxSpeedFOV = 74f;
    public float fovLerpSpeed = 5f;
    public float maxPullbackZ = -1.2f; // Mundur sedikit saat ngebut

    [Header("Shake State")]
    private float shakeDuration = 0f;
    private float shakeIntensity = 0f;

    [Header("White Flash (Win VFX)")]
    private float whiteFadeAlpha = 0f;
    private Texture2D whiteTexture;

    private Camera cam;
    private Vector3 currentVelocity;
    private Vector3 currentPos;

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

        if (target != null)
        {
            currentPos = target.position + offset;
            transform.position = currentPos;
        }

        // Sambungkan ke event Kemenangan GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameWon += TriggerWhiteFlash;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // 1. Smooth Follow X & Damped Y
        float targetX = Mathf.Lerp(currentPos.x, target.position.x + offset.x, smoothSpeedX * Time.deltaTime);
        float targetY = Mathf.Lerp(currentPos.y, target.position.y + offset.y, smoothSpeedY * Time.deltaTime);

        // 2. Dynamic Cam Pullback saat lari cepat
        float speedRatio = 1f;
        if (PlayerController.Instance != null)
        {
            speedRatio = PlayerController.Instance.currentRunSpeed / PlayerController.Instance.baseRunSpeed;
        }
        float speedPullback = Mathf.Lerp(0f, maxPullbackZ, Mathf.Clamp01(speedRatio - 1f));
        float targetZ = offset.z + speedPullback;

        currentPos = new Vector3(targetX, targetY, targetZ);

        // 3. Camera Shake (Perlin Noise)
        Vector3 shakeOffset = Vector3.zero;
        if (shakeDuration > 0)
        {
            shakeOffset.x = (Mathf.PerlinNoise(Time.time * 25f, 0f) - 0.5f) * 2f * shakeIntensity;
            shakeOffset.y = (Mathf.PerlinNoise(0f, Time.time * 25f) - 0.5f) * 2f * shakeIntensity;
            shakeDuration -= Time.deltaTime;
        }

        transform.position = currentPos + shakeOffset;
        transform.LookAt(target.position + Vector3.up * 1.5f);

        // 4. Dynamic Speed FOV
        float targetFOV = Mathf.Lerp(baseFOV, maxSpeedFOV, Mathf.Clamp01(speedRatio - 1f));
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, fovLerpSpeed * Time.deltaTime);
    }

    public void TriggerShake(float duration, float intensity)
    {
        shakeDuration = duration;
        shakeIntensity = intensity;
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
            GameManager.Instance.OnGameWon -= TriggerWhiteFlash;
        }
    }
}