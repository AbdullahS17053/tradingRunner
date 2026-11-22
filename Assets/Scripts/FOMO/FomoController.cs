using System.Collections;
using UnityEngine;

/// <summary>
/// Unified FOMO Controller.
/// Provides a state selection toggle (Inspector dropdown) to switch between:
/// 1. NormalFOMO: The original FOMO that chases behind the player and catches them on failure.
/// 2. EnemyBeamBot: The runner obstacle enemy that beams the player with red screen warning & dodge window.
/// </summary>
public class FOMOController : MonoBehaviour
{
    public enum FOMOMode
    {
        NormalFOMO,     // Original FOMO bot from the original (punishes mistakes, catches player when run fails)
        EnemyBeamBot    // FOMO bot for enemy (spawns ahead, red screen warning, beam attack, dodgeable)
    }

    [Header("FOMO Role Selection")]
    [Tooltip("Select whether this FOMO acts as the original chaser or the runner enemy bot")]
    public FOMOMode fomoMode = FOMOMode.NormalFOMO;

    [Header("Targeting References")]
    public Transform player;
    public Animator playerAnimator;
    protected RunnerController runnerController;

    [Header("Normal FOMO Mechanics (Original Chaser)")]
    public float currentDistanceBehind = 3.5f;
    public float penaltyDistance = 1.0f;
    public float catchUpSpeed = 10f;
    public float maxDistanceBehind = 3.5f;
    public float recoveryDelay = 4.0f;
    public float recoveryRate = 0.5f;

    [Header("Cinematic Flight Settings (Original)")]
    public float ascendHeight = 15f;
    public float ascendDuration = 2.0f;
    public float dropHeight = 15f;
    public float descendDuration = 2.0f;
    public float hoverForwardDistance = 4.0f;
    public float hoverLevitationHeight = 2.5f;
    public float captureToLaserDelay = 1.5f;
    public float laserFadeDuration = 0.2f;

    [Header("Enemy Beam Attack Settings (Runner Enemy)")]
    public float telegraphDistance = 28f;
    public float beamChargeDuration = 1.6f; // 1-2 second dodge window
    public float beamFireDuration = 0.3f;
    public float beamWidth = 0.35f;
    public float beamReach = 45f;
    public LineRenderer targetingLaser;
    public Light telegraphLight;

    [Header("Visual Effects (Laser & Animations)")]
    public LineRenderer fomoLaser;
    public Transform fomoLaserOrigin;
    public Animator fomoAnimator;

    [Header("Audio SFX")]
    public AudioSource fomoAudioSource;
    public AudioClip ascendSound;
    public AudioClip descendSound;
    public AudioClip attackWindupSound;
    public AudioClip laserShootSound;

    // Internal State
    protected float timeSinceLastMistake = 0f;
    protected bool isCatching = false;
    protected bool hasTriggeredAttack = false;
    protected bool isEnemyAttacking = false;
    protected int lockedTargetLane = 0;
    protected float lockedTargetLaneX = 0f;

    protected virtual void Awake()
    {
        // If this is a base FOMOController component but a FomoBot subclass is also on the same GameObject,
        // destroy this duplicate base component immediately to avoid duplicate updates and conflicts.
        if (GetType() == typeof(FOMOController))
        {
            Component botSubclass = GetComponent("FomoBot");
            if (botSubclass != null)
            {
                Destroy(this);
                return;
            }
        }

        if (fomoLaserOrigin == null)
        {
            fomoLaserOrigin = FindChildRecursive(transform, "LaserOrigin");
        }
        if (fomoLaser == null && fomoLaserOrigin != null)
        {
            fomoLaser = fomoLaserOrigin.GetComponent<LineRenderer>();
        }
        if (fomoAnimator == null)
        {
            fomoAnimator = GetComponentInChildren<Animator>();
        }
    }

    protected virtual void Start()
    {
        FindPlayerReferences();

        if (fomoLaser != null) fomoLaser.enabled = false;
        if (targetingLaser != null) targetingLaser.enabled = false;
        if (telegraphLight != null) telegraphLight.enabled = false;

        if (fomoMode == FOMOMode.EnemyBeamBot)
        {
            SetupEnemyVisualsIfMissing();
        }
    }

    protected void FindPlayerReferences()
    {
        if (runnerController == null && RunnerController.Instance != null)
        {
            runnerController = RunnerController.Instance;
        }

        if (player == null && runnerController != null)
        {
            player = runnerController.transform;
        }

        if (playerAnimator == null && runnerController != null)
        {
            playerAnimator = runnerController.GetComponentInChildren<Animator>();
        }
    }

    protected virtual void Update()
    {
        if (player == null || runnerController == null)
        {
            FindPlayerReferences();
            if (player == null) return;
        }

        if (fomoMode == FOMOMode.NormalFOMO)
        {
            UpdateNormalFOMO();
        }
        else
        {
            UpdateEnemyBeamBot();
        }
    }

    #region Normal FOMO (Original Logic)

    private void UpdateNormalFOMO()
    {
        if (isCatching) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("<color=yellow>[DEBUG] Manually triggered FOMO Capture!</color>");
            currentDistanceBehind = 0f;
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("<color=yellow>[DEBUG] Manually triggered FOMO Catch-Up!</color>");
            PunishPlayer();
        }
#endif

        timeSinceLastMistake += Time.deltaTime;

        if (timeSinceLastMistake >= recoveryDelay && currentDistanceBehind < maxDistanceBehind)
        {
            currentDistanceBehind += recoveryRate * Time.deltaTime;
            currentDistanceBehind = Mathf.Min(currentDistanceBehind, maxDistanceBehind);
        }

        // Camera is at local y=2.467, z=-3.53 looking down 15 degrees.
        // When FOMO is close (distance <= 2.5m), we dynamically lift it so its head (at ~1.7m) is fully framed in the camera view as a warning!
        float warningLift = Mathf.Lerp(0.38f, 0f, Mathf.Clamp01((currentDistanceBehind - 1.5f) / 1.5f));
        float targetY = player.position.y + warningLift;

        if (runnerController != null && runnerController.enableLabyrinthTurns)
        {
            Vector3 targetPosition = player.position - (player.forward * currentDistanceBehind);
            targetPosition.y = targetY;
            transform.position = Vector3.Lerp(transform.position, targetPosition, catchUpSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, player.rotation, catchUpSpeed * Time.deltaTime);
        }
        else
        {
            Vector3 targetPosition = new Vector3(player.position.x, targetY, player.position.z - currentDistanceBehind);
            transform.position = Vector3.Lerp(transform.position, targetPosition, catchUpSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, player.rotation, catchUpSpeed * Time.deltaTime);
        }

        if (currentDistanceBehind <= 0.8f)
        {
            StartCoroutine(CatchPlayerSequence());
        }
    }

    public void PunishPlayer()
    {
        if (isCatching) return;

        timeSinceLastMistake = 0f;
        // Bring FOMO bot to 1.85m directly behind the player so its head and shoulders are prominently framed in the camera view
        currentDistanceBehind = Mathf.Max(1.85f, currentDistanceBehind - 1.4f);

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.ShakeCamera(0.3f, 0.2f);
        }

        if (SoundManager.Instance != null && SoundManager.Instance.fomoDescendSound != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.fomoDescendSound);
        }
    }

    private IEnumerator CatchPlayerSequence()
    {
        isCatching = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.isCaptureSequenceActive = true;
        }

        if (runnerController != null)
        {
            runnerController.enabled = false;
        }

        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsCaptured", true);
            playerAnimator.Play("StandingIdle", 0, 0f);
        }

        if (SoundManager.Instance != null && SoundManager.Instance.fomoAscendSound != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.fomoAscendSound);
        }

        Vector3 startPos = transform.position;
        Vector3 skyPos = startPos + (Vector3.up * ascendHeight);

        float elapsed = 0f;
        while (elapsed < ascendDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / ascendDuration);
            transform.position = Vector3.Lerp(startPos, skyPos, t);
            yield return null;
        }

        Vector3 targetPos = player.position + (player.forward * hoverForwardDistance) + (Vector3.up * hoverLevitationHeight);
        Vector3 dropPos = targetPos + (Vector3.up * dropHeight);

        if (SoundManager.Instance != null && SoundManager.Instance.fomoDescendSound != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.fomoDescendSound);
        }

        transform.position = dropPos;
        elapsed = 0f;

        while (elapsed < descendDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / descendDuration);
            transform.position = Vector3.Lerp(dropPos, targetPos, t);
            transform.LookAt(player.position + (Vector3.up * 1f));
            yield return null;
        }

        transform.position = targetPos;
        transform.LookAt(player.position + (Vector3.up * 1f));

        if (SoundManager.Instance != null && SoundManager.Instance.fomoOpenGunSound != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.fomoOpenGunSound);
        }

        if (fomoAnimator != null)
        {
            fomoAnimator.SetTrigger("Attack");
        }

        yield return new WaitForSeconds(captureToLaserDelay);

        if (SoundManager.Instance != null && SoundManager.Instance.fomoLaserShotSound != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.fomoLaserShotSound);
        }

        if (fomoLaser != null && fomoLaserOrigin != null)
        {
            fomoLaser.gameObject.SetActive(true);
            fomoLaser.useWorldSpace = true;
            fomoLaser.enabled = true;
            fomoLaser.startWidth = 0.1f;
            fomoLaser.endWidth = 0.1f;
            fomoLaser.SetPosition(0, fomoLaserOrigin.position);
            fomoLaser.SetPosition(1, player.position + (Vector3.up * 1f));
        }

        if (playerAnimator != null)
        {
            playerAnimator.Play("Die", 0, 0f);
        }

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.ShakeCamera(0.2f, 0.2f);
        }

        float fadeElapsed = 0f;
        while (fadeElapsed < laserFadeDuration)
        {
            fadeElapsed += Time.deltaTime;
            if (fomoLaser != null)
            {
                float currentWidth = Mathf.Lerp(0.1f, 0f, fadeElapsed / laserFadeDuration);
                fomoLaser.startWidth = currentWidth;
                fomoLaser.endWidth = currentWidth;
            }
            yield return null;
        }

        if (fomoLaser != null)
        {
            fomoLaser.enabled = false;
        }

        yield return new WaitForSeconds(1.5f - laserFadeDuration);

        if (SoundManager.Instance != null && SoundManager.Instance.gameOverSound != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.gameOverSound);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver();
        }
    }

    #endregion

    #region Enemy Beam Bot Logic (Runner Enemy)

    private void SetupEnemyVisualsIfMissing()
    {
        if (fomoLaserOrigin == null)
        {
            fomoLaserOrigin = FindChildRecursive(transform, "LaserOrigin");
            if (fomoLaserOrigin == null)
            {
                GameObject originObj = new GameObject("LaserOrigin");
                originObj.transform.SetParent(transform, false);
                originObj.transform.localPosition = new Vector3(0f, 1.0f, 0.6f);
                fomoLaserOrigin = originObj.transform;
            }
        }

        if (fomoLaser == null && fomoLaserOrigin != null)
        {
            fomoLaser = fomoLaserOrigin.GetComponent<LineRenderer>();
            if (fomoLaser == null)
            {
                fomoLaser = fomoLaserOrigin.gameObject.AddComponent<LineRenderer>();
                fomoLaser.useWorldSpace = true;
                fomoLaser.startWidth = beamWidth;
                fomoLaser.endWidth = beamWidth;

                Material laserMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                laserMat.color = new Color(1f, 0.1f, 0.1f, 1f);
                fomoLaser.material = laserMat;
                fomoLaser.startColor = new Color(1f, 0.2f, 0.2f, 1f);
                fomoLaser.endColor = new Color(1f, 0.1f, 0.1f, 0.8f);
            }
        }

        if (targetingLaser == null && fomoLaserOrigin != null)
        {
            GameObject targetObj = new GameObject("TargetingLaserGuide");
            targetObj.transform.SetParent(fomoLaserOrigin, false);
            targetingLaser = targetObj.AddComponent<LineRenderer>();
            targetingLaser.useWorldSpace = true;
            targetingLaser.startWidth = 0.05f;
            targetingLaser.endWidth = 0.05f;

            Material guideMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            guideMat.color = new Color(1f, 0.4f, 0.4f, 0.6f);
            targetingLaser.material = guideMat;
            targetingLaser.startColor = new Color(1f, 0.4f, 0.4f, 0.7f);
            targetingLaser.endColor = new Color(1f, 0.1f, 0.1f, 0.1f);
        }
    }

    private void UpdateEnemyBeamBot()
    {
        Vector3 toBot = transform.position - player.position;
        Vector3 trackForward = (transform.parent != null && transform.parent.GetComponent<TrackChunk>() != null)
            ? transform.parent.forward
            : -transform.forward;

        float distToPlayer = Vector3.Dot(toBot, trackForward);

        if (distToPlayer <= telegraphDistance && distToPlayer > 0f && !hasTriggeredAttack)
        {
            hasTriggeredAttack = true;
            StartCoroutine(EnemyBeamSequence());
        }

        if (distToPlayer < -18f)
        {
            gameObject.SetActive(false);
        }
    }

    protected IEnumerator EnemyBeamSequence()
    {
        isEnemyAttacking = true;

        // 1. Play Attack Trigger on FOMO Animator
        if (fomoAnimator != null)
        {
            fomoAnimator.SetTrigger("Attack");
        }

        // 2. Audio windup charging sound
        if (SoundManager.Instance != null)
        {
            if (attackWindupSound != null) SoundManager.Instance.PlaySFX(attackWindupSound);
            else if (SoundManager.Instance.fomoOpenGunSound != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.fomoOpenGunSound);
        }

        if (telegraphLight != null) telegraphLight.enabled = true;

        // 3. Aiming Guide Laser straight out in bot forward direction (DOES NOT FOLLOW PLAYER)
        Vector3 origin = fomoLaserOrigin != null ? fomoLaserOrigin.position : (transform.position + Vector3.up * 1.0f);
        Vector3 targetFloor = origin + (transform.forward * beamReach);
        targetFloor.y = 0.5f;

        float chargeDuration = 1.8f;
        float chargeElapsed = 0f;
        bool warningShown = false;

        while (chargeElapsed < chargeDuration)
        {
            chargeElapsed += Time.deltaTime;

            origin = fomoLaserOrigin != null ? fomoLaserOrigin.position : (transform.position + Vector3.up * 1.0f);
            targetFloor = origin + (transform.forward * beamReach);
            targetFloor.y = 0.5f;

            if (targetingLaser != null)
            {
                targetingLaser.enabled = true;
                targetingLaser.SetPosition(0, origin);
                targetingLaser.SetPosition(1, targetFloor);
            }

            // Immediate check: If player is in the lane of the enemy, the effect starts immediately (zero wait time)!
            if (player != null)
            {
                Vector3 toPlayerInBotSpace = transform.InverseTransformPoint(player.position);
                float forwardDistance = toPlayerInBotSpace.z;
                float lateralOffset = Mathf.Abs(toPlayerInBotSpace.x);
                bool isPlayerInLane = lateralOffset < 1.15f;
                bool isPlayerInFront = isPlayerInLane && forwardDistance > 0f && forwardDistance <= beamReach;

                if (isPlayerInFront)
                {
                    if (!warningShown)
                    {
                        warningShown = true;
                        float remaining = Mathf.Max(0.5f, chargeDuration - chargeElapsed);
                        if (AttackWarningHUD.Instance != null)
                        {
                            AttackWarningHUD.Instance.TriggerWarning(remaining);
                        }
                    }
                }
                else
                {
                    // Player stepped out or swiped to another lane: dismiss warning and show dodged
                    if (warningShown)
                    {
                        warningShown = false;
                        if (AttackWarningHUD.Instance != null)
                        {
                            AttackWarningHUD.Instance.DismissWarning();
                            AttackWarningHUD.Instance.ShowDodged();
                        }
                    }
                }
            }

            yield return null;
        }

        if (targetingLaser != null) targetingLaser.enabled = false;
        if (telegraphLight != null) telegraphLight.enabled = false;

        // 4. Fire Beam Straight Ahead!
        if (SoundManager.Instance != null)
        {
            if (laserShootSound != null) SoundManager.Instance.PlaySFX(laserShootSound);
            else if (SoundManager.Instance.fomoLaserShotSound != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.fomoLaserShotSound);
        }

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.ShakeCamera(0.25f, 0.18f);
        }

        origin = fomoLaserOrigin != null ? fomoLaserOrigin.position : (transform.position + Vector3.up * 1.0f);
        Vector3 beamEnd = origin + (transform.forward * beamReach);
        beamEnd.y = 0.5f;

        if (fomoLaser != null)
        {
            fomoLaser.enabled = true;
            fomoLaser.startWidth = beamWidth;
            fomoLaser.endWidth = beamWidth;

            fomoLaser.SetPosition(0, origin);
            fomoLaser.SetPosition(1, beamEnd);
        }

        // 5. Evaluate Hit or Dodge
        // Beam shoots straight down the bot's lane. If player is in that lane, hit; otherwise dodged!
        if (runnerController != null && !runnerController.IsDead && player != null)
        {
            Vector3 toPlayerInBotSpace = transform.InverseTransformPoint(player.position);
            float forwardDistance = toPlayerInBotSpace.z;
            float lateralOffset = Mathf.Abs(toPlayerInBotSpace.x);
            bool isPlayerInLane = lateralOffset < 1.15f;
            bool isHit = isPlayerInLane && forwardDistance > 0f && forwardDistance <= beamReach;

            if (isHit)
            {
                if (PowerUpManager.Instance != null && PowerUpManager.Instance.IsInvincible)
                {
                    // Deflected
                }
                else if (PowerUpManager.Instance != null && PowerUpManager.Instance.ConsumeShield())
                {
                    // Absorbed
                }
                else
                {
                    runnerController.Die();
                }
            }
            else
            {
                if (warningShown && AttackWarningHUD.Instance != null)
                {
                    AttackWarningHUD.Instance.ShowDodged();
                }
            }
        }

        // 7. Fade out laser beam
        float fadeElapsed = 0f;
        while (fadeElapsed < beamFireDuration)
        {
            fadeElapsed += Time.deltaTime;
            float t = fadeElapsed / beamFireDuration;
            if (fomoLaser != null)
            {
                float w = Mathf.Lerp(beamWidth, 0f, t);
                fomoLaser.startWidth = w;
                fomoLaser.endWidth = w;
            }
            yield return null;
        }

        if (fomoLaser != null) fomoLaser.enabled = false;
        isEnemyAttacking = false;
    }

    #endregion

    protected Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursive(parent.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
