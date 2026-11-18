using System.Collections;
using UnityEngine;

public class FomoBot : MonoBehaviour
{
    public enum BotBehaviorType
    {
        FrontPatrolCharge,   // Spawns ahead, charges or patrols in lane, jump/slide/dodgeable
        RearChaser           // Comes from behind with warning indicator, overtakes in lane
    }

    [Header("Behavior & Variant")]
    public BotBehaviorType behaviorType = BotBehaviorType.FrontPatrolCharge;
    public string botVariantName = "FOMO Bot";

    [Header("Movement & Speeds")]
    public float movementSpeed = 6.0f;
    public float chargeMultiplier = 1.6f;
    public float rearApproachSpeed = 22.0f; // Faster than runner forward speed to overtake
    public float laneChangeInterval = 2.5f;

    [Header("Attack & Telegraph Settings")]
    public float telegraphDistance = 18f;
    public float attackRange = 12f;
    public GameObject warningIndicator; // Visual warning holographic marker or ground decal
    public LineRenderer targetingLaser;
    public Light telegraphLight;
    public Color warningColor = new Color(1f, 0.15f, 0.15f, 1f);

    [Header("Audio SFX")]
    public AudioClip windupSound;
    public AudioClip attackSound;
    public AudioClip destroyedSound;

    [Header("Visual Effects & Destruction")]
    public ParticleSystem warningParticles;
    public ParticleSystem destroyParticles;
    public Animator botAnimator;
    public Collider botCollider;

    private bool hasTelegraphed = false;
    private bool isAttacking = false;
    private bool isDestroyed = false;
    private float currentLaneX = 0f;
    private Transform playerTransform;

    private void Awake()
    {
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

    private void Start()
    {
        if (botCollider == null) botCollider = GetComponent<Collider>();
        if (botCollider != null) botCollider.isTrigger = true;
        if (botAnimator == null) botAnimator = GetComponentInChildren<Animator>();

        if (RunnerController.Instance != null)
        {
            playerTransform = RunnerController.Instance.transform;
        }

        if (warningIndicator != null) warningIndicator.SetActive(false);
        if (targetingLaser != null) targetingLaser.enabled = false;
        if (telegraphLight != null) telegraphLight.enabled = false;

        currentLaneX = transform.position.x;
    }

    private Vector3 GetTrackForward()
    {
        if (RunnerController.Instance != null)
        {
            return RunnerController.Instance.transform.forward;
        }
        return Vector3.forward;
    }

    private void Update()
    {
        if (isDestroyed) return;

        if (playerTransform == null)
        {
            if (RunnerController.Instance != null) playerTransform = RunnerController.Instance.transform;
            return;
        }

        if (behaviorType == BotBehaviorType.FrontPatrolCharge)
        {
            HandleFrontBotLogic();
        }
        else
        {
            HandleRearChaserLogic();
        }
    }

    private void HandleFrontBotLogic()
    {
        Vector3 trackForward = GetTrackForward();
        Vector3 toBot = transform.position - playerTransform.position;
        float distToPlayer = Vector3.Dot(toBot, trackForward);

        // 1. Telegraph warning when player gets within range
        if (distToPlayer <= telegraphDistance && distToPlayer > 0 && !hasTelegraphed)
        {
            TriggerTelegraph();
        }

        // 2. Charge or move toward player once telegraphed
        if (distToPlayer <= attackRange && distToPlayer > 0)
        {
            if (!isAttacking)
            {
                isAttacking = true;
                if (botAnimator != null) botAnimator.SetTrigger("Attack");
                if (SoundManager.Instance != null && attackSound != null)
                {
                    SoundManager.Instance.PlaySFX(attackSound);
                }
            }

            transform.position -= trackForward * (movementSpeed * chargeMultiplier * Time.deltaTime);
        }
        else if (distToPlayer > 0)
        {
            // Idle hover or slow forward patrol
            transform.position -= trackForward * (movementSpeed * 0.5f * Time.deltaTime);
        }

        // If player has safely passed the bot, turn off warning
        if (distToPlayer < -2f && hasTelegraphed)
        {
            CleanupTelegraph();
        }

        // Deactivate if far behind player
        if (distToPlayer < -15f)
        {
            gameObject.SetActive(false);
        }
    }

    private void HandleRearChaserLogic()
    {
        Vector3 trackForward = GetTrackForward();
        Vector3 toPlayer = playerTransform.position - transform.position;
        float distBehindPlayer = Vector3.Dot(toPlayer, trackForward);

        // Telegraph warning on player HUD/behind when approaching
        if (distBehindPlayer <= telegraphDistance && distBehindPlayer > 0 && !hasTelegraphed)
        {
            TriggerTelegraph();
        }

        // Move forward faster than player forwardSpeed to catch up and overtake
        float runnerSpeed = RunnerController.Instance != null ? RunnerController.Instance.forwardSpeed : 15f;
        float actualSpeed = runnerSpeed + 5f;
        transform.position += trackForward * (actualSpeed * Time.deltaTime);

        // Keep in lane
        Vector3 pos = transform.position;
        pos.x = Mathf.MoveTowards(pos.x, currentLaneX, 8f * Time.deltaTime);
        transform.position = pos;

        // If chaser has passed the player safely
        if (distBehindPlayer < -10f)
        {
            CleanupTelegraph();
            gameObject.SetActive(false);
        }
    }

    private void TriggerTelegraph()
    {
        hasTelegraphed = true;

        if (warningIndicator != null) warningIndicator.SetActive(true);
        if (warningParticles != null) warningParticles.Play();

        if (telegraphLight != null)
        {
            telegraphLight.color = warningColor;
            telegraphLight.enabled = true;
        }

        if (targetingLaser != null && playerTransform != null)
        {
            targetingLaser.enabled = true;
            targetingLaser.SetPosition(0, transform.position + Vector3.up * 1f);
            targetingLaser.SetPosition(1, playerTransform.position + Vector3.up * 0.8f);
        }

        if (SoundManager.Instance != null)
        {
            if (windupSound != null) SoundManager.Instance.PlaySFX(windupSound);
            else if (SoundManager.Instance.fomoAscendSound != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.fomoAscendSound);
        }
    }

    private void CleanupTelegraph()
    {
        if (warningIndicator != null) warningIndicator.SetActive(false);
        if (warningParticles != null) warningParticles.Stop();
        if (targetingLaser != null) targetingLaser.enabled = false;
        if (telegraphLight != null) telegraphLight.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDestroyed) return;

        if (other.CompareTag("Player"))
        {
            // Check if player has invincible powerup or shield
            if (PowerUpManager.Instance != null)
            {
                if (PowerUpManager.Instance.IsInvincible)
                {
                    DestroyBot();
                    return;
                }
                else if (PowerUpManager.Instance.ConsumeShield())
                {
                    DestroyBot();
                    return;
                }
            }

            // Player hit by FOMO Bot!
            RunnerController runner = other.GetComponent<RunnerController>();
            if (runner != null)
            {
                runner.Die();
            }
        }
    }

    public void DestroyBot()
    {
        isDestroyed = true;
        CleanupTelegraph();

        if (botCollider != null) botCollider.enabled = false;

        if (destroyParticles != null)
        {
            destroyParticles.transform.position = transform.position;
            destroyParticles.Play();
        }

        if (SoundManager.Instance != null)
        {
            if (destroyedSound != null) SoundManager.Instance.PlaySFX(destroyedSound);
            else if (SoundManager.Instance.crashSound != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.crashSound);
        }

        StartCoroutine(DisableAfterDestroy());
    }

    private IEnumerator DisableAfterDestroy()
    {
        // Hide child meshes
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            if (r != null && r != (Renderer)destroyParticles?.GetComponent<Renderer>())
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
        hasTelegraphed = false;
        isAttacking = false;
        gameObject.SetActive(false);
    }
}
