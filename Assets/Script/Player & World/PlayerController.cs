using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Lane Settings")]
    [Tooltip("Jarak antar jalur")]
    public float laneDistance = 2.5f; 
    public float laneChangeSpeed = 15f;
    private int currentLane = 1; // 0: Kiri, 1: Tengah, 2: Kanan

    [Header("Movement Speeds")]
    public float baseRunSpeed = 12f;
    [HideInInspector] public float currentRunSpeed;
    private float speedMultiplier = 1f;

    [Header("Jump & Gravity")]
    public float jumpForce = 8f;
    public float gravity = 25f;
    private float verticalVelocity;

    [Header("Slide Settings")]
    public float slideDuration = 0.8f;
    private bool isSliding = false;
    private Vector3 originalCenter;
    private float originalHeight;
    public float slideHeight = 0.9f;

    [Header("Stumble / Side Hit Penalty")]
    public float stumbleSlowMultiplier = 0.5f;
    public float stumbleDuration = 1.0f;
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

        // Deteksi input geser jalur
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
        }

        // Slide
        bool slideInput = keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame;
        if (slideInput && !isSliding)
        {
            StartCoroutine(SlideRoutine());
        }
    }

    private void TryChangeLane(int direction)
    {
        int targetLane = Mathf.Clamp(currentLane + direction, 0, 2);
        if (targetLane == currentLane) return;

        // Cek Raycast samping untuk mendeteksi tabrakan obstacle
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
        // 1. Hitung Kecepatan Lari (Treadmill speed)
        float targetSpeed = baseRunSpeed * speedMultiplier;
        if (isStumbling) targetSpeed *= stumbleSlowMultiplier;
        currentRunSpeed = Mathf.Lerp(currentRunSpeed, targetSpeed, 10f * Time.deltaTime);

        // 2. Hitung Perpindahan Horizontal (X) via controller.Move
        float targetX = (currentLane - 1) * laneDistance;
        float deltaX = (targetX - transform.position.x) * laneChangeSpeed;

        // 3. Hitung Gravitasi & Lompat (Y)
        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -1f;
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        // 4. Eksekusi semua pergerakan sekaligus ke CharacterController
        Vector3 finalMovement = new Vector3(deltaX, verticalVelocity, 0);
        controller.Move(finalMovement * Time.deltaTime);
    }

    private IEnumerator SlideRoutine()
    {
        isSliding = true;
        controller.height = slideHeight;
        controller.center = new Vector3(originalCenter.x, slideHeight / 2f, originalCenter.z);

        if (!controller.isGrounded) verticalVelocity = -jumpForce;

        yield return new WaitForSeconds(slideDuration);

        controller.height = originalHeight;
        controller.center = originalCenter;
        isSliding = false;
    }

    public void TriggerStumble()
    {
        if (isStumbling || isInvincible) return;
        StartCoroutine(StumbleRoutine());
    }

    private IEnumerator StumbleRoutine()
    {
        isStumbling = true;
        yield return new WaitForSeconds(stumbleDuration);
        isStumbling = false;
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
    }
}