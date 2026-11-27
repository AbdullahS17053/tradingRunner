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

    [Header("Grounding & Input Feel")]
    [Tooltip("How long after leaving the ground jump/slide still count as grounded")]
    public float coyoteTime = 0.18f;
    [Tooltip("How long a jump press is remembered and replayed on landing")]
    public float jumpBufferTime = 0.18f;
    [Tooltip("How long a slide press is remembered and replayed on landing")]
    public float slideBufferTime = 0.18f;
    [Tooltip("Extra distance below the capsule that still counts as touching the ground")]
    public float groundProbeDistance = 0.35f;

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

    private float jumpBufferTimer = 0f;
    private float slideBufferTimer = 0f;
    private int groundProbeFrame = -1;
    private bool groundProbeResult = false;
    private readonly RaycastHit[] groundProbeHits = new RaycastHit[8];

    public Vector3 PathForward => pathRotation * Vector3.forward;

    private float originalHeight;
    private Vector3 originalCenter;
    private bool isSliding = false;
    private float slideTimer;

    // Added flag to prevent input/movement after dying
    private bool isDead = false;

    // Grounded means: the controller reports contact, we are still inside the coyote
    // window, or a short downward probe finds floor. The probe is what keeps jump and
    // slide alive on City's tiled road, where CharacterController.isGrounded flickers
    // between the 5x5 road tile colliders while the runner is visibly on the ground.
    private bool IsPlayerGrounded
    {
        get
        {
            if (velocity.y > 0.1f) return false; // never grounded while rising
            return controller.isGrounded || groundedGraceTimer > 0f || GroundProbe();
        }
    }

    private bool GroundProbe()
    {
        if (controller == null) return false;
        if (groundProbeFrame == Time.frameCount) return groundProbeResult;
        groundProbeFrame = Time.frameCount;
        groundProbeResult = false;

        float halfHeight = controller.height * 0.5f;
        Vector3 origin = transform.position + controller.center;
        float distance = halfHeight + groundProbeDistance;

        int count = Physics.RaycastNonAlloc(origin, Vector3.down, groundProbeHits, distance, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = groundProbeHits[i];
            Collider col = hit.collider;
            if (col == null || col == controller) continue;
            if (col.transform == transform || col.transform.IsChildOf(transform)) continue;
            if (hit.normal.y >= 0.55f)
            {
                groundProbeResult = true;
                break;
            }
        }
        return groundProbeResult;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        isWarping = false;
    }

    private void Start()
    {
        isWarping = false;
        initialScale = transform.localScale;
        transform.localScale = initialScale;
        controller = GetComponent<CharacterController>();
        originalHeight = controller.height;
        originalCenter = controller.center;

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        pathOrigin = new Vector3(0f, transform.position.y, transform.position.z);
        pathRotation = transform.rotation;

        currentLane = numberOfLanes / 2;
        targetXPosition = GetLaneXPosition(currentLane);

        if (transform.parent != null)
        {
            Vector3 startLocal = transform.localPosition;
            startLocal.x = targetXPosition;
            transform.localPosition = startLocal;
        }
        else
        {
            Vector3 startPos = transform.position;
            startPos.x = targetXPosition;
            transform.position = startPos;
        }

        if (Camera.main != null)
        {
            defaultCameraFOV = Camera.main.fieldOfView;
        }

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex != 1 && animator != null)
        {
            animator.Play("Sprint", 0, 0f);
        }
    }

    private float defaultCameraFOV = 60f;

    [Header("Testing Auto-Play Assist")]
    public bool enableAutoPilot = false;

    private void Update()
    {
        if (isDead || isWarping) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsGameStarted) return;

        float dt = Time.deltaTime;

        UpdateGroundedState(dt);
        HandleBufferedInput(dt);

        if (enableAutoPilot)
        {
            HandleAutoPilot();
        }

        HandleKeyboardInput();
        MovePlayer();
        UpdateAnimations();
        UpdateSpeedBoostEffects();
        HandleSlidingTimer();
    }

    private void UpdateGroundedState(float dt)
    {
        if (controller == null) return;

        if (controller.isGrounded || GroundProbe())
        {
            groundedGraceTimer = coyoteTime;
        }
        else
        {
            groundedGraceTimer = Mathf.Max(0f, groundedGraceTimer - dt);
        }
    }

    /// <summary>
    /// Replays a jump/slide press that arrived slightly too early (e.g. one frame
    /// before touchdown, or during a frame where the tiled road broke grounded
    /// detection). Without this the input was simply thrown away.
    /// </summary>
    private void HandleBufferedInput(float dt)
    {
        if (jumpBufferTimer > 0f)
        {
            jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - dt);
            if (IsPlayerGrounded)
            {
                jumpBufferTimer = 0f;
                DoJump();
            }
        }

        if (slideBufferTimer > 0f)
        {
            slideBufferTimer = Mathf.Max(0f, slideBufferTimer - dt);
            if (IsPlayerGrounded)
            {
                slideBufferTimer = 0f;
                DoSlide();
            }
        }

        if (turnBufferTimer > 0f)
        {
            turnBufferTimer = Mathf.Max(0f, turnBufferTimer - dt);
        }
    }

    private void HandleAutoPilot()
    {
        Vector3 origin = transform.position + Vector3.up * 0.4f;
        Vector3 forward = PathForward;

        if (Physics.SphereCast(origin, 0.5f, forward, out RaycastHit hit, 7.0f))
        {
            if (hit.collider.CompareTag("Obstacle"))
            {
                if (hit.point.y - transform.position.y < 1.35f)
                {
                    if (IsPlayerGrounded && !isSliding)
                    {
                        TriggerJump();
                    }
                }
                else
                {
                    if (IsPlayerGrounded && !isSliding)
                    {
                        TriggerSlide();
                    }
                }
            }
        }
    }

    private void UpdateSpeedBoostEffects()
    {
        bool isBoosting = PowerUpManager.Instance != null && PowerUpManager.Instance.IsSpeedBoostActive;

        Camera cam = Camera.main;
        if (cam != null)
        {
            float targetFOV = isBoosting ? (defaultCameraFOV + 15f) : defaultCameraFOV;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, 8f * Time.deltaTime);
        }

        if (animator != null)
        {
            animator.speed = isBoosting ? 1.35f : 1.0f;
        }
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

    public bool IsSliding => isSliding;
    public int CurrentLane => currentLane;
    public float TargetXPosition => targetXPosition;
    public float VerticalVelocity => velocity.y;
    public bool IsDead => isDead;
    public bool IsWarping => isWarping;

    private bool isWarping = false;
    private Vector3 initialScale = Vector3.one;

    public void FreezeForWarp()
    {
        isWarping = true;
        velocity = Vector3.zero;
        if (animator != null)
        {
            animator.speed = 0f;
        }
    }

    public void AnimateWarpIntoPortal(Vector3 portalCenter, float duration)
    {
        FreezeForWarp();
        StartCoroutine(WarpAnimationRoutine(portalCenter, duration));
    }

    private IEnumerator WarpAnimationRoutine(Vector3 portalCenter, float duration)
    {
        Vector3 startPos = transform.position;
        Vector3 startScale = transform.localScale;
        // Target is directly into the portal aperture
        Vector3 targetPos = portalCenter;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Ease in cubic for smooth pull into the vortex
            float tMove = t * t;
            transform.position = Vector3.Lerp(startPos, targetPos, tMove);

            // Scale down smoothly towards zero
            float tScale = Mathf.SmoothStep(0f, 1f, t);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, tScale);

            yield return null;
        }

        transform.localScale = Vector3.zero;
    }

    private LabyrinthTurnZone currentTurnZone;
    private float turnBufferTimer = 0f;
    private int bufferedTurnDirection = 0;

    public void RegisterActiveTurnZone(LabyrinthTurnZone zone)
    {
        currentTurnZone = zone;
        if (turnBufferTimer > 0f && zone != null && !zone.HasTriggered)
        {
            float requestedAngle = (bufferedTurnDirection < 0) ? -90f : 90f;
            if (zone.ExecuteTurn(this, requestedAngle))
            {
                turnBufferTimer = 0f;
            }
        }
    }

    public void UnregisterActiveTurnZone(LabyrinthTurnZone zone)
    {
        if (currentTurnZone == zone) currentTurnZone = null;
    }

    public void MoveLane(int direction)
    {
        if (enableLabyrinthTurns)
        {
            if (currentTurnZone != null && !currentTurnZone.HasTriggered)
            {
                float requestedAngle = (direction < 0) ? -90f : 90f;
                if (currentTurnZone.ExecuteTurn(this, requestedAngle))
                {
                    turnBufferTimer = 0f;
                    return; // Consumed as corner turn!
                }
                else
                {
                    // Swiped in an invalid turn direction for this turn zone (e.g. right in LeftOnly)
                    // Do not change lanes, keep player facing forward towards wall!
                    return;
                }
            }
            else
            {
                // Buffer turn input briefly in case player swiped right before entering zone
                turnBufferTimer = 0.3f;
                bufferedTurnDirection = direction;
            }
        }

        int desiredLane = Mathf.Clamp(currentLane + direction, 0, numberOfLanes - 1);

        if (desiredLane != currentLane)
        {
            if (IsPlayerGrounded && !isSliding && animator != null)
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
            DoJump();
        }
        else
        {
            // Remember the press and replay it as soon as we touch down.
            jumpBufferTimer = jumpBufferTime;
        }
    }

    private void DoJump()
    {
        jumpBufferTimer = 0f;
        slideBufferTimer = 0f;

        if (isSliding) StopSliding();

        groundedGraceTimer = 0f;
        velocity.y = jumpForce;
        if (animator != null)
        {
            animator.ResetTrigger("Slide");
            animator.SetTrigger("Jump");
        }
    }

    public void TriggerSlide()
    {
        if (IsPlayerGrounded)
        {
            DoSlide();
        }
        else
        {
            // Slam down, and still slide when we land.
            velocity.y = fastFallVelocity;
            slideBufferTimer = slideBufferTime;
        }
    }

    private void DoSlide()
    {
        jumpBufferTimer = 0f;
        slideBufferTimer = 0f;

        if (isSliding)
        {
            slideTimer = slideDuration;
            return;
        }

        isSliding = true;
        slideTimer = slideDuration;

        controller.height = originalHeight / 2f;
        controller.center = new Vector3(originalCenter.x, originalCenter.y / 2f, originalCenter.z);

        if (animator != null)
        {
            animator.ResetTrigger("Jump");
            animator.SetTrigger("Slide");
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
        if (IsPlayerGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;

        float currentSpeedMultiplier = GameManager.Instance != null ? GameManager.Instance.currentSpeedMultiplier : 1f;
        if (PowerUpManager.Instance != null && PowerUpManager.Instance.IsSpeedBoostActive)
        {
            currentSpeedMultiplier *= 2.0f;
        }
        float currentForwardSpeed = forwardSpeed * currentSpeedMultiplier;

        if (!enableLabyrinthTurns)
        {
            float currentX = transform.parent != null ? transform.localPosition.x : transform.position.x;
            float newX = Mathf.Lerp(currentX, targetXPosition, laneSwitchSpeed * Time.deltaTime);
            float moveX = newX - currentX;

            Vector3 localMoveVector = new Vector3(moveX, velocity.y * Time.deltaTime, currentForwardSpeed * Time.deltaTime);
            Vector3 worldMoveVector = transform.parent != null ? transform.parent.TransformDirection(localMoveVector) : localMoveVector;

            if (controller != null && controller.enabled)
            {
                controller.Move(worldMoveVector);
                if (controller.isGrounded) groundedGraceTimer = coyoteTime;
            }
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
                           + (Vector3.up * currentForwardSpeed * 0f + Vector3.up * (velocity.y * Time.deltaTime));

        if (controller != null && controller.enabled)
        {
            controller.Move(moveVector);
            if (controller.isGrounded)
                groundedGraceTimer = 0.12f;
        }
    }

    public void ExecuteCornerTurn(Vector3 newOrigin, Quaternion newRotation, float turnAngle)
    {
        if (!enableLabyrinthTurns) return;

        pathOrigin = new Vector3(newOrigin.x, transform.position.y, newOrigin.z);
        pathRotation = newRotation;

        currentLane = numberOfLanes / 2;
        targetXPosition = GetLaneXPosition(currentLane);

        // Center player cleanly on the new corridor
        Vector3 newRight = newRotation * Vector3.right;
        Vector3 newForward = newRotation * Vector3.forward;

        float lateralOffset = Vector3.Dot(transform.position - pathOrigin, newRight);
        float minLane = GetLaneXPosition(0);
        float maxLane = GetLaneXPosition(numberOfLanes - 1);
        lateralOffset = Mathf.Clamp(lateralOffset, minLane, maxLane);

        float forwardOffset = Mathf.Max(0f, Vector3.Dot(transform.position - pathOrigin, newForward));

        Vector3 correctedPos = pathOrigin + (newRight * lateralOffset) + (newForward * forwardOffset);
        correctedPos.y = transform.position.y;
        transform.position = correctedPos;
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
        if (isWarping || isDead) return;

        FomoBot bot = other.GetComponent<FomoBot>();
        bool isHazard = other.CompareTag("Obstacle") || bot != null;

        if (isHazard)
        {
            if (PowerUpManager.Instance != null)
            {
                if (PowerUpManager.Instance.IsInvincible)
                {
                    // Obliterate obstacle or bot
                    if (bot != null) bot.DestroyBot();
                    else other.gameObject.SetActive(false);

                    if (SoundManager.Instance != null && SoundManager.Instance.crashSound != null)
                    {
                        SoundManager.Instance.PlaySFX(SoundManager.Instance.crashSound);
                    }
                    if (CameraShake.Instance != null)
                    {
                        CameraShake.Instance.ShakeCamera(0.2f, 0.15f);
                    }
                    return;
                }
                else if (PowerUpManager.Instance.ConsumeShield())
                {
                    // Shield absorbed the hit and gave temporary grace
                    if (bot != null) bot.DestroyBot();
                    else other.gameObject.SetActive(false);

                    FOMOController fomoChaser = Object.FindFirstObjectByType<FOMOController>();
                    if (fomoChaser != null && fomoChaser.fomoMode == FOMOController.FOMOMode.NormalFOMO)
                    {
                        fomoChaser.PunishPlayer();
                    }
                    return;
                }
            }

            if (bot != null)
            {
                if (bot.canAvoidBySliding && isSliding)
                {
                    // Successfully slid under attack!
                    return;
                }
                if (bot.canAvoidByJumping && (transform.position.y - bot.transform.position.y) >= bot.jumpClearanceHeight)
                {
                    // Successfully jumped over attack!
                    return;
                }
            }

            FOMOController fomoWarning = Object.FindFirstObjectByType<FOMOController>();
            if (fomoWarning != null && fomoWarning.fomoMode == FOMOController.FOMOMode.NormalFOMO)
            {
                fomoWarning.PunishPlayer();
            }

            Die();
        }
    }

    public void RevivePlayer()
    {
        isDead = false;
        isSliding = false;
        if (controller != null)
        {
            controller.height = originalHeight;
            controller.center = originalCenter;
        }
        if (deathParticles != null) deathParticles.Stop();
        if (animator != null)
        {
            animator.Rebind();
            animator.Play("Sprint", 0, 0f);
            animator.SetBool("GameStarted", true);
        }
        enabled = true;
    }

    public void Die()
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
        yield return new WaitForSeconds(1.5f);

        if (GemReviveManager.Instance != null && GemReviveManager.Instance.TryPromptRevive())
        {
            yield break;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver();
        }
    }
}
