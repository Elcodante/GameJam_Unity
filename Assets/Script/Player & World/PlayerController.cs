using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Lane Settings")]
    public float laneDistance = 2.5f; 
    public float laneChangeSpeed = 18f; // Ditingkatkan agar lebih gesit
    private int currentLane = 1;

    [Header("Movement Speeds")]
    public float baseRunSpeed = 12f;
    [HideInInspector] public float currentRunSpeed;
    private float speedMultiplier = 1f;

    [Header("Snappy Jump & Gravity")]
    public float jumpForce = 9.5f;
    public float gravity = 28f;
    [Tooltip("Gravitasi tambahan saat jatuh agar lompatan terasa berbobot seperti Subway Surfers")]
    public float fallMultiplier = 1.8f;
    private float verticalVelocity;

    [Header("Slide Settings")]
    public float slideDuration = 0.65f; // Durasi lebih padat
    public float fastFallSlideForce = 20f; // Membanting ke tanah seketika saat slide di udara
    private bool isSliding = false;
    private Vector3 originalCenter;
    private float originalHeight;
    public float slideHeight = 0.85f;

    [Header("Stumble Penalty")]
    public float stumbleSlowMultiplier = 0.5f;
    public float stumbleDuration = 0.8f;
    private bool isStumbling = false;

    [Header("Controls Flags")]
    public bool invertControls = false;
    public bool isInvincible = false;
    public bool canControl = true;

    private CharacterController controller;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        controller = GetComponent<CharacterController>();
        originalCenter = controller.center;
        originalHeight = controller.height;
        currentRunSpeed = baseRunSpeed;
    }

    private void Update()
    {
        if (!canControl) return;

        HandleInput();
        ApplyMovementAndGravity();
    }

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

        // Jump
        bool jumpInput = keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame;
        if (jumpInput && controller.isGrounded)
        {
            verticalVelocity = jumpForce;
            AudioManager.Instance?.PlayJump();
        }

        // Slide
        bool slideInput = keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame;
        if (slideInput)
        {
            // Jika ditekan di udara: Fast-Fall
            if (!controller.isGrounded)
            {
                verticalVelocity = -fastFallSlideForce;
            }

            if (!isSliding)
            {
                AudioManager.Instance?.PlaySlide(); // <-- DIPINDAH KE SINI
                StartCoroutine(SlideRoutine());
            }
        }
    }

    private void TryChangeLane(int direction)
    {
        int targetLane = Mathf.Clamp(currentLane + direction, 0, 2);
        if (targetLane == currentLane) return;

        float checkDistance = laneDistance;
        Vector3 rayDir = direction > 0 ? transform.right : -transform.right;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;

        if (Physics.Raycast(rayOrigin, rayDir, out RaycastHit hit, checkDistance))
        {
            if (hit.collider.CompareTag("Obstacle"))
            {
                TriggerStumble();
                return;
            }
        }

        currentLane = targetLane;
    }

    private void ApplyMovementAndGravity()
    {
        float targetSpeed = baseRunSpeed * speedMultiplier;
        if (isStumbling) targetSpeed *= stumbleSlowMultiplier;
        currentRunSpeed = Mathf.Lerp(currentRunSpeed, targetSpeed, 10f * Time.deltaTime);

        // Perpindahan jalur horizontal X yang halus
        float targetX = (currentLane - 1) * laneDistance;
        float deltaX = (targetX - transform.position.x) * laneChangeSpeed;

        // Snappy Gravity Curve (Jatuh lebih cepat dari naik)
        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -1f;
        }
        else
        {
            float appliedGravity = gravity;
            if (verticalVelocity < 0) appliedGravity *= fallMultiplier; // Fast-falling
            verticalVelocity -= appliedGravity * Time.deltaTime;
        }

        Vector3 finalMovement = new Vector3(deltaX, verticalVelocity, 0);
        controller.Move(finalMovement * Time.deltaTime);
    }

    private IEnumerator SlideRoutine()
    {
        isSliding = true;
        controller.height = slideHeight;
        controller.center = new Vector3(originalCenter.x, slideHeight / 2f, originalCenter.z);

        yield return new WaitForSeconds(slideDuration);

        controller.height = originalHeight;
        controller.center = originalCenter;
        isSliding = false;
    }

    public void TriggerStumble()
    {
        if (isStumbling || isInvincible) return;
        StartCoroutine(StumbleRoutine());
        AudioManager.Instance?.PlayStumble();
    }

    private IEnumerator StumbleRoutine()
    {
        isStumbling = true;
        ProceduralCamera.Instance?.TriggerShake(0.35f, 0.25f); // Getar kamera saat tersandung
        yield return new WaitForSeconds(stumbleDuration);
        isStumbling = false;
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
    }
}