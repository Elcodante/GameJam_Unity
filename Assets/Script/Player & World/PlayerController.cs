using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    // ===== Events (untuk Animator / VFX / SFX tambahan) =====
    public event Action<int> OnLaneChanged;   // arah: -1 kiri, +1 kanan
    public event Action OnJumped;
    public event Action<float> OnLanded;      // kecepatan jatuh (positif)
    public event Action OnSlideStarted;
    public event Action OnSlideEnded;

    [Header("Animations")]
    [Tooltip("Komponen Animator pada model karakter")]
    public Animator animator;

    [Header("Lane Settings")]
    public float laneDistance = 2.5f;
    [Tooltip("Kegesitan pindah jalur. Makin besar makin cepat. 14-18 terasa seperti Subway Surfers.\nLogika jalur berpindah seketika saat input, dan badan langsung meninggalkan jalur lama (SYBO: selisih milidetik terasa).")]
    public float laneChangeSharpness = 16f;
    [Tooltip("Kecepatan minimum (m/s) saat pindah jalur supaya tidak ada ekor lambat di ujung perpindahan")]
    public float laneChangeMinSpeed = 6f;
    private int currentLane = 1;
    private float laneVelocity;

    [Header("Movement Speeds")]
    public float baseRunSpeed = 12f;
    [HideInInspector] public float currentRunSpeed;
    [Tooltip("Seberapa cepat kecepatan mengikuti target (buff / debuff / stumble)")]
    public float speedResponse = 6f;
    [Tooltip("Seberapa cepat player berhenti saat menang / kalah")]
    public float stopResponse = 7f;
    [Tooltip("Kecepatan awal sebagai pecahan dari kecepatan dasar. Start mendadak di kecepatan penuh adalah salah satu penyebab mual.")]
    [Range(0f, 1f)] public float startSpeedFraction = 0.3f;
    [Tooltip("Seberapa cepat kecepatan naik di awal lari (lebih landai dari Speed Response)")]
    public float launchResponse = 2.5f;
    private float speedMultiplier = 1f;
    private bool launched = false;

    [Header("Snappy Jump & Gravity")]
    public float jumpForce = 9.5f;
    public float gravity = 28f;
    [Tooltip("Gravitasi tambahan saat jatuh agar lompatan terasa berbobot seperti Subway Surfers")]
    public float fallMultiplier = 1.8f;
    [Tooltip("Tombol lompat yang ditekan sedikit sebelum mendarat tetap dihitung (detik)")]
    public float jumpBufferTime = 0.15f;
    private float verticalVelocity;
    private float jumpBufferedAt = -999f;
    private bool wasGrounded = true;

    [Header("Slide Settings")]
    public float slideDuration = 0.65f;
    public float fastFallSlideForce = 20f; // Membanting ke tanah seketika saat slide di udara (jump-cancel)
    public float slideHeight = 0.85f;
    private bool isSliding = false;
    private bool slideQueued = false;      // slide ditekan di udara, mulai begitu mendarat
    private Coroutine slideRoutine;
    private Vector3 originalCenter;
    private float originalHeight;

    [Header("Stumble Penalty")]
    public float stumbleSlowMultiplier = 0.5f;
    public float stumbleDuration = 0.8f;
    [Tooltip("Seperti Subway Surfers: tersandung lagi saat masih tersandung = game over. Mati secara default karena game ini sudah punya penalti waktu.")]
    public bool secondStumbleIsFatal = false;
    private bool isStumbling = false;

    [Header("Procedural Visuals (opsional)")]
    [Tooltip("Seret model karakter (harus CHILD dari player, bukan objek player-nya sendiri). Kosong = efek visual mati.")]
    public Transform visualRoot;
    public bool useProceduralVisuals = true;
    [Tooltip("Derajat miring badan saat pindah jalur")]
    public float leanRollAngle = 14f;
    [Tooltip("Derajat badan menoleh ke arah pindah jalur")]
    public float leanYawAngle = 10f;
    [Tooltip("Derajat badan condong ke belakang saat slide")]
    public float slideTiltAngle = 30f;
    public float visualResponse = 14f;
    [Tooltip("Kuat gepeng saat mendarat dan memanjang saat lompat")]
    public float squashStrength = 0.45f;

    [Header("Controls Flags")]
    public bool invertControls = false;
    public bool isInvincible = false;
    public bool canControl = true;

    // ===== Info publik untuk kamera & VFX =====
    public bool IsSliding => isSliding;
    public bool IsGrounded => controller != null && controller.isGrounded;
    public float LateralVelocity => laneVelocity;
    public float VerticalVelocity => verticalVelocity;

    private CharacterController controller;

    // visual state
    private Vector3 visualBasePos;
    private Quaternion visualBaseRot;
    private Vector3 visualBaseScale;
    private float visualRoll, visualYaw, visualPitch;
    private float squash, squashVel;
    private float bumpOffset, bumpVel;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        controller = GetComponent<CharacterController>();
        originalCenter = controller.center;
        originalHeight = controller.height;
        currentRunSpeed = baseRunSpeed * Mathf.Clamp01(startSpeedFraction);

        if (visualRoot == transform)
        {
            Debug.LogWarning("[PlayerController] Visual Root harus child dari player, bukan player itu sendiri. Efek visual dimatikan.");
            visualRoot = null;
        }
        if (visualRoot != null)
        {
            visualBasePos = visualRoot.localPosition;
            visualBaseRot = visualRoot.localRotation;
            visualBaseScale = visualRoot.localScale;
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (canControl)
        {
            HandleInput();
            TryConsumeBufferedJump();
        }

        UpdateSpeed(dt);
        ApplyMovementAndGravity(dt);
        UpdateVisuals(dt);

        if (animator != null)
        {
            animator.SetBool("IsGrounded", controller.isGrounded);

            // Menyesuaikan kecepatan animasi saat Speed Boost atau Kram Perut
            float speedRatio = baseRunSpeed > 0 ? (currentRunSpeed / baseRunSpeed) : 1f;
            animator.SetFloat("RunSpeed", speedRatio);
        }
    }

    // =====================================================
    // INPUT
    // =====================================================
    private void HandleInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        bool leftInput = keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame;
        bool rightInput = keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame;

        if (invertControls)
        {
            bool temp = leftInput;
            leftInput = rightInput;
            rightInput = temp;
        }

        if (leftInput) TryChangeLane(-1);
        else if (rightInput) TryChangeLane(1);

        // Jump (di-buffer, jadi tekan sedikit sebelum mendarat tetap lompat)
        bool jumpInput = keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame;
        if (jumpInput) jumpBufferedAt = Time.time;

        // Slide
        bool slideInput = keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame;
        if (slideInput) TrySlide();
    }

    private void TryConsumeBufferedJump()
    {
        if (Time.time - jumpBufferedAt > jumpBufferTime) return;
        if (!controller.isGrounded) return;

        jumpBufferedAt = -999f;
        DoJump();
    }

    private void DoJump()
    {
        // Lompat membatalkan slide (seperti Subway Surfers)
        if (isSliding) CancelSlide();
        slideQueued = false;

        verticalVelocity = jumpForce;
        squashVel -= 5f * squashStrength * 2f; // meregang saat lepas landas

        AudioManager.Instance?.PlayJump();
        OnJumped?.Invoke();

        // Trigger animasi lompat
        if (animator != null)
        {
            animator.ResetTrigger("Jump");
            animator.SetTrigger("Jump");
        }
    }

    private void TrySlide()
    {
        if (!controller.isGrounded)
        {
            // Jump-cancel: banting ke tanah dulu, slide dimulai begitu mendarat supaya durasinya penuh
            verticalVelocity = -fastFallSlideForce;
            slideQueued = true;
            return;
        }

        if (!isSliding) StartSlide();
    }

    // =====================================================
    // LANE CHANGE
    // =====================================================
    private void TryChangeLane(int direction)
    {
        int targetLane = Mathf.Clamp(currentLane + direction, 0, 2);
        if (targetLane == currentLane) return;

        // Ada rintangan menghalangi jalur tujuan? -> tersandung ke samping, tetap di jalur lama
        if (!isInvincible && IsLaneBlocked(targetLane))
        {
            TriggerStumble(direction);
            return;
        }

        currentLane = targetLane;
        OnLaneChanged?.Invoke(direction);
    }

    private bool IsLaneBlocked(int targetLane)
    {
        float targetX = (targetLane - 1) * laneDistance;
        float radius = controller.radius;

        float laneChangeTime = 3f / Mathf.Max(1f, laneChangeSharpness);
        float lookAhead = currentRunSpeed * laneChangeTime;

        Vector3 capsuleCenter = transform.position + controller.center;
        float halfHeight = Mathf.Max(0.1f, controller.height * 0.5f - 0.1f);

        Vector3 boxCenter = new Vector3(targetX, capsuleCenter.y, capsuleCenter.z + (lookAhead - radius) * 0.5f);
        Vector3 boxHalf = new Vector3(radius * 0.8f, halfHeight, (lookAhead + radius) * 0.5f);

        Collider[] hits = Physics.OverlapBox(boxCenter, boxHalf, Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].GetComponentInParent<Obstacle>() != null) return true;
        }
        return false;
    }

    // =====================================================
    // SPEED, MOVEMENT & GRAVITY
    // =====================================================
    private void UpdateSpeed(float dt)
    {
        float targetSpeed;
        float response;

        if (canControl)
        {
            targetSpeed = baseRunSpeed * speedMultiplier;
            if (isStumbling) targetSpeed *= stumbleSlowMultiplier;

            response = launched ? speedResponse : launchResponse;
            if (!launched && currentRunSpeed >= baseRunSpeed * 0.97f) launched = true;
        }
        else
        {
            targetSpeed = 0f;
            response = stopResponse;
        }

        currentRunSpeed = Mathf.Lerp(currentRunSpeed, targetSpeed, 1f - Mathf.Exp(-response * dt));
        if (!canControl && currentRunSpeed < 0.05f) currentRunSpeed = 0f;
    }

    private void ApplyMovementAndGravity(float dt)
    {
        float x = transform.position.x;
        float targetX = (currentLane - 1) * laneDistance;
        float distance = Mathf.Abs(targetX - x);

        float newX = x;
        if (distance > 0.0005f)
        {
            float step = Mathf.Max(distance * (1f - Mathf.Exp(-laneChangeSharpness * dt)), laneChangeMinSpeed * dt);
            newX = Mathf.MoveTowards(x, targetX, step);
        }
        float deltaX = newX - x;
        laneVelocity = Mathf.Lerp(laneVelocity, deltaX / Mathf.Max(dt, 0.0001f), 1f - Mathf.Exp(-25f * dt));

        float velocityThisFrame;
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
            velocityThisFrame = verticalVelocity;
        }
        else
        {
            float appliedGravity = gravity;
            if (verticalVelocity < 0f) appliedGravity *= fallMultiplier;
            verticalVelocity -= appliedGravity * dt;
            velocityThisFrame = verticalVelocity;
        }

        controller.Move(new Vector3(deltaX, velocityThisFrame * dt, 0f));

        bool groundedNow = controller.isGrounded;
        if (groundedNow && !wasGrounded)
        {
            HandleLanded(-velocityThisFrame);
        }
        wasGrounded = groundedNow;
    }

    private void HandleLanded(float impactSpeed)
    {
        if (impactSpeed > 4f)
        {
            squashVel += Mathf.Clamp(impactSpeed * 0.4f, 0f, 7f) * squashStrength * 2f;
            ProceduralCamera.Instance?.OnPlayerLanded(impactSpeed);
            OnLanded?.Invoke(impactSpeed);
        }

        if (slideQueued && canControl)
        {
            slideQueued = false;
            StartSlide();
        }
    }

    // =====================================================
    // SLIDE
    // =====================================================
    private void StartSlide()
    {
        if (slideRoutine != null) StopCoroutine(slideRoutine);
        slideRoutine = StartCoroutine(SlideRoutine());

        AudioManager.Instance?.PlaySlide();
        OnSlideStarted?.Invoke();

        if (animator != null)
        {
            animator.SetBool("IsSliding", true);
        }
    }

    private IEnumerator SlideRoutine()
    {
        isSliding = true;
        ApplySlideCollider();

        yield return new WaitForSeconds(slideDuration);

        EndSlide();
    }

    private void CancelSlide()
    {
        if (slideRoutine != null)
        {
            StopCoroutine(slideRoutine);
            slideRoutine = null;
        }
        if (isSliding) EndSlide();
    }

    private void EndSlide()
    {
        controller.height = originalHeight;
        controller.center = originalCenter;
        isSliding = false;
        slideRoutine = null;
        OnSlideEnded?.Invoke();

        if (animator != null)
        {
            animator.SetBool("IsSliding", false);
        }
    }

    private void ApplySlideCollider()
    {
        float bottom = originalCenter.y - originalHeight * 0.5f;
        controller.height = slideHeight;
        controller.center = new Vector3(originalCenter.x, bottom + slideHeight * 0.5f, originalCenter.z);
    }

    // =====================================================
    // STUMBLE
    // =====================================================
    public void TriggerStumble()
    {
        TriggerStumble(0);
    }

    public void TriggerStumble(int bumpDirection)
    {
        if (isInvincible) return;

        if (isStumbling)
        {
            if (secondStumbleIsFatal && GameManager.Instance != null)
            {
                GameManager.Instance.TriggerGameOver("TERSANDUNG DUA KALI! Celanamu tidak tertolong lagi...");
            }
            return;
        }

        bumpVel += bumpDirection * 6f;

        StartCoroutine(StumbleRoutine());
        AudioManager.Instance?.PlayStumble();
    }

    private IEnumerator StumbleRoutine()
    {
        isStumbling = true;
        ProceduralCamera.Instance?.TriggerShake(0.35f, 0.25f);
        yield return new WaitForSeconds(stumbleDuration);
        isStumbling = false;
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
    }

    // =====================================================
    // PROCEDURAL VISUALS
    // =====================================================
    private void UpdateVisuals(float dt)
    {
        if (visualRoot == null || !useProceduralVisuals) return;

        float lean = Mathf.Clamp(laneVelocity / 30f, -1f, 1f);
        float slide01 = isSliding ? 1f : 0f;

        float follow = 1f - Mathf.Exp(-visualResponse * dt);
        visualRoll = Mathf.Lerp(visualRoll, -lean * leanRollAngle, follow);
        visualYaw = Mathf.Lerp(visualYaw, lean * leanYawAngle, follow);
        visualPitch = Mathf.Lerp(visualPitch, -slide01 * slideTiltAngle, follow);

        float accel = -220f * squash - 18f * squashVel;
        squashVel += accel * dt;
        squash = Mathf.Clamp(squash + squashVel * dt, -0.35f, 0.4f);

        float bumpAccel = -300f * bumpOffset - 20f * bumpVel;
        bumpVel += bumpAccel * dt;
        bumpOffset += bumpVel * dt;

        visualRoot.localRotation = visualBaseRot * Quaternion.Euler(visualPitch, visualYaw, visualRoll);
        visualRoot.localScale = new Vector3(
            visualBaseScale.x * (1f + squash * 0.5f),
            visualBaseScale.y * (1f - squash),
            visualBaseScale.z * (1f + squash * 0.5f));
        visualRoot.localPosition = visualBasePos + new Vector3(bumpOffset, 0f, 0f);
    }
}