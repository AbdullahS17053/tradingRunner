using System.Collections;
using UnityEngine;

/// <summary>
/// Enemy variant of FOMO Bot inheriting from FOMOController.
/// Uses the same FOMO character model and capabilities, but configured as a runner obstacle enemy.
/// </summary>
public class FomoBot : FOMOController
{
    public enum BotBehaviorType
    {
        EnemyBeamBot,       // Default: Beam attack with 1.6s red screen warning & dodge window
        FrontPatrolCharge,  // Charges / patrols down corridor
        RearChaser          // Approaches from behind to overtake
    }

    [Header("Enemy Bot Identity & Behavior")]
    public BotBehaviorType behaviorType = BotBehaviorType.EnemyBeamBot;
    public string botVariantName = "FOMO Bot";

    [Header("Movement Speeds")]
    public float movementSpeed = 4.5f;
    public float chargeMultiplier = 1.7f;
    public float rearApproachSpeed = 22.0f;
    public float laneChangeInterval = 2.5f;

    [Header("Dodge & Collision Settings")]
    public bool canAvoidBySliding = true;
    public bool canAvoidByJumping = true;
    public float jumpClearanceHeight = 1.1f;

    [Header("Components & Particle FX")]
    public Collider botCollider;
    public Animator botAnimator;
    public ParticleSystem warningParticles;
    public ParticleSystem slashParticles;
    public ParticleSystem destroyParticles;
    public AudioClip destroyedSound;

    private bool isDestroyed = false;
    private float currentLaneX = 0f;

    protected override void Awake()
    {
        // Enforce enemy beam mode
        fomoMode = FOMOMode.EnemyBeamBot;

        base.Awake();

        if (botCollider == null) botCollider = GetComponent<Collider>();
        if (botCollider != null) botCollider.isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        if (string.IsNullOrEmpty(gameObject.tag) || gameObject.CompareTag("Untagged"))
        {
            gameObject.tag = "Obstacle";
        }
    }

    protected override void Start()
    {
        base.Start();

        if (botCollider == null) botCollider = GetComponent<Collider>();
        if (botCollider != null) botCollider.isTrigger = true;
        if (botAnimator == null) botAnimator = fomoAnimator;

        currentLaneX = transform.position.x;
    }

    protected override void Update()
    {
        if (isDestroyed) return;

        if (player == null || runnerController == null)
        {
            FindPlayerReferences();
            if (player == null) return;
        }

        if (behaviorType == BotBehaviorType.RearChaser)
        {
            HandleRearChaser();
        }
        else
        {
            HandleFrontEnemy();
        }
    }

    private void HandleFrontEnemy()
    {
        Vector3 toBot = transform.position - player.position;
        Vector3 trackForward = (transform.parent != null && transform.parent.GetComponent<TrackChunk>() != null)
            ? transform.parent.forward
            : -transform.forward;

        float distToPlayer = Vector3.Dot(toBot, trackForward);

        // Advance along corridor only when not charging or firing beam
        if (distToPlayer > 0f && !isEnemyAttacking)
        {
            float speedMult = GameManager.Instance != null ? GameManager.Instance.currentSpeedMultiplier : 1.0f;
            transform.position += transform.forward * (movementSpeed * speedMult * 0.4f * Time.deltaTime);
        }

        // Trigger Beam Attack when within telegraph range
        if (distToPlayer <= telegraphDistance && distToPlayer > 0f && !hasTriggeredAttack)
        {
            hasTriggeredAttack = true;
            if (warningParticles != null) warningParticles.Play();
            StartCoroutine(EnemyBeamSequenceWrapper());
        }

        if (distToPlayer < -18f)
        {
            gameObject.SetActive(false);
        }
    }

    private IEnumerator EnemyBeamSequenceWrapper()
    {
        yield return StartCoroutine(EnemyBeamSequence());
        if (warningParticles != null) warningParticles.Stop();
    }

    private void HandleRearChaser()
    {
        Vector3 toPlayer = player.position - transform.position;
        Vector3 trackForward = (transform.parent != null && transform.parent.GetComponent<TrackChunk>() != null)
            ? transform.parent.forward
            : transform.forward;

        float distBehind = Vector3.Dot(toPlayer, trackForward);

        if (distBehind <= telegraphDistance && distBehind > 0f && !hasTriggeredAttack)
        {
            hasTriggeredAttack = true;
            if (AttackWarningHUD.Instance != null)
            {
                AttackWarningHUD.Instance.TriggerWarning(beamChargeDuration);
            }
        }

        float runnerSpeed = runnerController != null ? runnerController.forwardSpeed : 15f;
        float actualSpeed = runnerSpeed + (rearApproachSpeed * 0.4f);
        transform.position += transform.forward * (actualSpeed * Time.deltaTime);

        Vector3 pos = transform.position;
        pos.x = Mathf.MoveTowards(pos.x, currentLaneX, 8f * Time.deltaTime);
        transform.position = pos;

        if (distBehind < -12f)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDestroyed) return;

        if (other.CompareTag("Player"))
        {
            RunnerController runner = other.GetComponent<RunnerController>();
            if (runner != null && !runner.IsDead)
            {
                if (PowerUpManager.Instance != null && PowerUpManager.Instance.IsInvincible)
                {
                    DestroyBot();
                    return;
                }
                if (PowerUpManager.Instance != null && PowerUpManager.Instance.ConsumeShield())
                {
                    DestroyBot();
                    return;
                }
                if (canAvoidBySliding && runner.IsSliding)
                {
                    return;
                }
                if (canAvoidByJumping && (runner.transform.position.y - transform.position.y) >= jumpClearanceHeight)
                {
                    return;
                }

                runner.Die();
            }
        }
    }

    public void DestroyBot()
    {
        isDestroyed = true;
        if (fomoLaser != null) fomoLaser.enabled = false;
        if (targetingLaser != null) targetingLaser.enabled = false;
        if (botCollider != null) botCollider.enabled = false;

        if (destroyParticles != null)
        {
            destroyParticles.transform.position = transform.position;
            destroyParticles.Play();
        }

        if (SoundManager.Instance != null)
        {
            if (destroyedSound != null) SoundManager.Instance.PlaySFX(destroyedSound);
            else if (SoundManager.Instance.fomoDescendSound != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.fomoDescendSound);
            else if (SoundManager.Instance.crashSound != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.crashSound);
        }

        StartCoroutine(DisableAfterDestroy());
    }

    private IEnumerator DisableAfterDestroy()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            if (r != null && (destroyParticles == null || r != destroyParticles.GetComponent<Renderer>()))
            {
                r.enabled = false;
            }
        }

        yield return new WaitForSeconds(1.2f);

        foreach (var r in renderers)
        {
            if (r != null) r.enabled = true;
        }

        if (botCollider != null) botCollider.enabled = true;
        isDestroyed = false;
        hasTriggeredAttack = false;
        isEnemyAttacking = false;
        gameObject.SetActive(false);
    }
}
