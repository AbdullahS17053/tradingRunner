using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(CharacterController))]
public class RunnerController : MonoBehaviour
{
    public static RunnerController Instance { get; private set; }

    [Header("References")]
    public Animator animator;
    public ParticleSystem deathParticles;

    [Header("Lane Settings")]
    public int numberOfLanes = 3;
    public float laneSpacing = 2.5f;

    [Header("Movement Settings")]
    public float forwardSpeed = 15f;
    public float laneSwitchSpeed = 12f;

    [Header("Jump & Slide Settings")]
    public float jumpForce = 8f;
    public float gravity = -20f;
    public float slideDuration = 1.0f;
    [Tooltip("How fast the character slams down when sliding mid-air")]
    public float fastFallVelocity = -20f;

    [Header("Labyrinth Level Settings")]
    [Tooltip("Enable 90-degree directional turning ONLY for Labyrinth World")]
    public bool enableLabyrinthTurns = false;

    private CharacterController controller;
    private int currentLane;
    private Vector3 velocity;
    private float targetXPosition;
    private Vector3 pathOrigin;
    private Quaternion pathRotation = Quaternion.identity;
    private float groundedGraceTimer = 0f;

    public Vector3 PathForward => pathRotation * Vector3.forward;

    private float originalHeight;
    private Vector3 originalCenter;
    private bool isSliding = false;
    private float slideTimer;

    // Added flag to prevent input/movement after dying
    private bool isDead = false;

    private bool IsPlayerGrounded => controller.isGrounded || groundedGraceTimer > 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        originalHeight = controller.height;
        originalCenter = controller.center;

        if (animator == null) animator = GetComponentInChildren<Animator>();

        pathOrigin = new Vector3(0f, transform.position.y, transform.position.z);
        pathRotation = transform.rotation;

        currentLane = numberOfLanes / 2;
        targetXPosition = GetLaneXPosition(currentLane);

        Vector3 startPos = transform.position;
        startPos.x = targetXPosition;
        transform.position = startPos;
    }

    private void Update()
    {
        if (isDead) return;

        if (controller.isGrounded)
            groundedGraceTimer = 0.12f;
        else if (groundedGraceTimer > 0f)
            groundedGraceTimer -= Time.deltaTime;

        HandleKeyboardInput();
        MovePlayer();
        UpdateAnimations();
        HandleSlidingTimer();
    }

    private void HandleKeyboardInput()
    {
        if (GameManager.Instance.isTradingChallengeActive) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            MoveLane(-1);
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            MoveLane(1);

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space))
            TriggerJump();
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            TriggerSlide();
    }

    public void MoveLane(int direction)
    {
        int desiredLane = Mathf.Clamp(currentLane + direction, 0, numberOfLanes - 1);

        if (desiredLane != currentLane)
        {
            if (IsPlayerGrounded && !isSliding)
            {
                if (desiredLane < currentLane)
                    animator.SetTrigger("DodgeLeft");
                else
                    animator.SetTrigger("DodgeRight");
            }

            currentLane = desiredLane;
            targetXPosition = GetLaneXPosition(currentLane);
        }
    }

    public void TriggerJump()
    {
        if (IsPlayerGrounded)
        {
            if (isSliding) StopSliding();

            groundedGraceTimer = 0f;
            velocity.y = jumpForce;
            animator.ResetTrigger("Slide");
            animator.SetTrigger("Jump");
        }
    }

    public void TriggerSlide()
    {
        if (IsPlayerGrounded)
        {
            isSliding = true;
            slideTimer = slideDuration;

            controller.height = originalHeight / 2f;
            controller.center = new Vector3(originalCenter.x, originalCenter.y / 2f, originalCenter.z);

            animator.ResetTrigger("Jump");
            animator.SetTrigger("Slide");
        }
        else
        {
            velocity.y = fastFallVelocity;
        }
    }

    private void HandleSlidingTimer()
    {
        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0)
            {
                StopSliding();
            }
        }
    }

    private void StopSliding()
    {
        isSliding = false;
        controller.height = originalHeight;
        controller.center = originalCenter;
    }

    private void MovePlayer()
    {
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;

        float currentSpeedMultiplier = GameManager.Instance != null ? GameManager.Instance.currentSpeedMultiplier : 1f;
        float currentForwardSpeed = forwardSpeed * currentSpeedMultiplier;

        if (!enableLabyrinthTurns)
        {
            float newX = Mathf.Lerp(transform.position.x, targetXPosition, laneSwitchSpeed * Time.deltaTime);
            float moveX = newX - transform.position.x;

            Vector3 linearMoveVector = new Vector3(moveX, velocity.y * Time.deltaTime, currentForwardSpeed * Time.deltaTime);
            controller.Move(linearMoveVector);
            return;
        }

        // Rotate BEFORE controller.Move so CharacterController.isGrounded is never invalidated after Move()
        if (Quaternion.Angle(transform.rotation, pathRotation) > 0.1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, pathRotation, 16f * Time.deltaTime);
        }
        else if (transform.rotation != pathRotation)
        {
            transform.rotation = pathRotation;
        }

        Vector3 forwardDir = pathRotation * Vector3.forward;
        Vector3 rightDir = pathRotation * Vector3.right;

        float currentLateral = Vector3.Dot(transform.position - pathOrigin, rightDir);
        float moveLateral = Mathf.Lerp(currentLateral, targetXPosition, laneSwitchSpeed * Time.deltaTime) - currentLateral;

        Vector3 moveVector = (forwardDir * (currentForwardSpeed * Time.deltaTime))
                           + (rightDir * moveLateral)
                           + (Vector3.up * (velocity.y * Time.deltaTime));

        controller.Move(moveVector);
        if (controller.isGrounded)
            groundedGraceTimer = 0.12f;
    }

    public void ExecuteCornerTurn(Vector3 newOrigin, Quaternion newRotation, float turnAngle)
    {
        if (!enableLabyrinthTurns) return;

        pathOrigin = new Vector3(newOrigin.x, transform.position.y, newOrigin.z);
        pathRotation = newRotation;

        currentLane = numberOfLanes / 2;
        targetXPosition = GetLaneXPosition(currentLane);
    }

    private void UpdateAnimations()
    {
        if (animator != null)
        {
            animator.SetBool("Grounded", IsPlayerGrounded);
        }
    }

    private float GetLaneXPosition(int laneIndex)
    {
        float leftMostOffset = -(numberOfLanes - 1) * laneSpacing / 2f;
        return leftMostOffset + (laneIndex * laneSpacing);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.ShakeCamera(0.25f, 0.2f);
        }

        SoundManager.Instance.StopMusic();
        SoundManager.Instance.PlaySFX(SoundManager.Instance.crashSound);
        SoundManager.Instance.PlaySFX(SoundManager.Instance.gameOverSound);

        TradingChallenge[] activeChallenges = FindObjectsOfType<TradingChallenge>();
        foreach (TradingChallenge challenge in activeChallenges)
        {
            challenge.ForceCancelChallenge();
        }

        if (deathParticles != null) deathParticles.Play();
        if (animator != null) animator.SetTrigger("Die");

        GameManager.Instance.isGameOver = true;
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(2.0f);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver();
        }
    }
}
