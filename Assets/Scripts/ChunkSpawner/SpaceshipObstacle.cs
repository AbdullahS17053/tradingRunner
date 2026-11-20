using System.Collections;
using UnityEngine;

public class SpaceshipObstacle : MonoBehaviour
{
    // Global tracker ensuring only ONE enemy spaceship is active on the track at any time
    public static SpaceshipObstacle activeSpaceship = null;

    [Header("Ship Visual")]
    public Transform visualMesh;

    [Header("Hover & Drift")]
    public float hoverSpeed = 2.4f;
    public float hoverAmplitude = 0.18f;
    public float bankAmplitude = 6.0f;
    public float bankSpeed = 1.6f;

    [Header("FOMO Enemy Transformation")]
    [Tooltip("Whether this spaceship behaves as a hostile FOMO Enemy Ship")]
    public bool isFomoEnemy = false;
    [Range(0f, 1f)] public float enemyChance = 0.45f;

    [Header("Beam Attack Settings")]
    public float telegraphDistance = 32f;
    public float beamChargeDuration = 1.6f; // 1-2s warning window
    public float beamFireDuration = 0.3f;
    public float beamWidth = 0.4f;
    public float beamReach = 45f;

    [Header("Laser References")]
    public LineRenderer shipLaser;
    public Transform shipLaserOrigin;
    public LineRenderer targetingLaser;

    [Header("Audio")]
    public AudioClip chargeSound;
    public AudioClip shootSound;

    private Vector3 initialMeshLocalPos;
    private Quaternion initialMeshLocalRot;
    private float seedOffset;

    private bool hasTriggeredAttack = false;
    private bool isAttacking = false;
    private RunnerController runnerController;
    private Transform playerTransform;

    private Light engineLight;

    private void Start()
    {
        // 1. Enforce ONLY ONE active spaceship at a time on the track
        if (activeSpaceship != null && activeSpaceship != this && activeSpaceship.gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
            return;
        }

        activeSpaceship = this;
        isFomoEnemy = true;

        // 2. Tag & Collider setup
        gameObject.tag = "Obstacle";

        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0f, 0.8f, 0f);
            col.size = new Vector3(2.4f, 1.6f, 3.2f);
        }

        if (visualMesh == null)
        {
            if (transform.childCount > 0)
                visualMesh = transform.GetChild(0);
            else
                visualMesh = transform;
        }

        initialMeshLocalPos = (visualMesh != transform) ? visualMesh.localPosition : Vector3.zero;
        initialMeshLocalRot = (visualMesh != transform) ? visualMesh.localRotation : Quaternion.identity;
        seedOffset = Random.Range(0f, 100f);

        // 3. Engine Glow Light
        GameObject lightObj = new GameObject("EngineGlow", typeof(Light));
        lightObj.transform.SetParent(transform, false);
        lightObj.transform.localPosition = new Vector3(0f, 0.5f, -1.2f); // At rear exhaust
        engineLight = lightObj.GetComponent<Light>();
        engineLight.type = LightType.Point;
        engineLight.range = 6f;
        engineLight.intensity = 4.5f;
        engineLight.color = new Color(1f, 0.15f, 0.15f);

        // 4. Setup Laser Cannon
        SetupShipLaserIfMissing();

        // Align into the middle track lane (X = 0) facing oncoming player
        Vector3 pos = transform.localPosition;
        pos.x = 0f; // Middle lane
        pos.y = Mathf.Max(pos.y, 0.85f);
        transform.localPosition = pos;

        // Ensure facing oncoming player (facing -Z relative to chunk track)
        if (transform.parent != null)
        {
            transform.rotation = transform.parent.rotation * Quaternion.Euler(0f, 180f, 0f);
        }

        if (RunnerController.Instance != null)
        {
            runnerController = RunnerController.Instance;
            playerTransform = runnerController.transform;
        }
    }

    private void SetupShipLaserIfMissing()
    {
        if (shipLaserOrigin == null)
        {
            GameObject originObj = new GameObject("ShipLaserOrigin");
            originObj.transform.SetParent(transform, false);
            originObj.transform.localPosition = new Vector3(0f, 0.4f, 1.6f); // Nose of the ship
            shipLaserOrigin = originObj.transform;
        }

        if (shipLaser == null)
        {
            shipLaser = shipLaserOrigin.gameObject.AddComponent<LineRenderer>();
            shipLaser.useWorldSpace = true;
            shipLaser.startWidth = beamWidth;
            shipLaser.endWidth = beamWidth;

            Material laserMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            laserMat.color = new Color(1f, 0.1f, 0.1f, 1f);
            shipLaser.material = laserMat;
            shipLaser.startColor = new Color(1f, 0.2f, 0.2f, 1f);
            shipLaser.endColor = new Color(1f, 0.1f, 0.1f, 0.8f);
            shipLaser.enabled = false;
        }

        if (targetingLaser == null)
        {
            GameObject targetObj = new GameObject("ShipTargetingGuide");
            targetObj.transform.SetParent(shipLaserOrigin, false);
            targetingLaser = targetObj.AddComponent<LineRenderer>();
            targetingLaser.useWorldSpace = true;
            targetingLaser.startWidth = 0.05f;
            targetingLaser.endWidth = 0.05f;

            Material guideMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            guideMat.color = new Color(1f, 0.3f, 0.3f, 0.5f);
            targetingLaser.material = guideMat;
            targetingLaser.startColor = new Color(1f, 0.3f, 0.3f, 0.6f);
            targetingLaser.endColor = new Color(1f, 0.1f, 0.1f, 0.1f);
            targetingLaser.enabled = false;
        }
    }

    private void Update()
    {
        // 1. Ambient Hover and Roll Banking
        if (visualMesh != null && visualMesh != transform)
        {
            float bob = Mathf.Sin((Time.time + seedOffset) * hoverSpeed) * hoverAmplitude;
            visualMesh.localPosition = initialMeshLocalPos + Vector3.up * bob;

            float roll = Mathf.Sin((Time.time + seedOffset) * bankSpeed) * bankAmplitude;
            visualMesh.localRotation = initialMeshLocalRot * Quaternion.Euler(0f, 0f, roll);
        }

        // 2. FOMO Enemy Beam Attack Behavior
        if (!isFomoEnemy) return;

        if (playerTransform == null)
        {
            if (RunnerController.Instance != null)
            {
                runnerController = RunnerController.Instance;
                playerTransform = runnerController.transform;
            }
            return;
        }

        Vector3 toShip = transform.position - playerTransform.position;
        Vector3 trackForward = (transform.parent != null) ? transform.parent.forward : -transform.forward;
        float distToPlayer = Vector3.Dot(toShip, trackForward);

        // Trigger beam telegraph when within range
        if (distToPlayer <= telegraphDistance && distToPlayer > 0f && !hasTriggeredAttack)
        {
            hasTriggeredAttack = true;
            StartCoroutine(SpaceshipBeamSequence());
        }

        // When player runs past spaceship, free active singleton and deactivate
        if (distToPlayer < -12f)
        {
            if (activeSpaceship == this)
            {
                activeSpaceship = null;
            }
            gameObject.SetActive(false);
        }
    }

    private IEnumerator SpaceshipBeamSequence()
    {
        isAttacking = true;

        // 1. Play Audio Charging SFX
        if (SoundManager.Instance != null)
        {
            if (chargeSound != null) SoundManager.Instance.PlaySFX(chargeSound);
            else if (SoundManager.Instance.fomoOpenGunSound != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.fomoOpenGunSound);
        }

        // 2. Aiming Guide Laser straight out of the ship (DOES NOT FOLLOW PLAYER)
        Vector3 origin = shipLaserOrigin != null ? shipLaserOrigin.position : (transform.position + Vector3.up * 0.4f);
        Vector3 targetFloor = origin + (transform.forward * beamReach);
        targetFloor.y = 0.5f;

        float chargeDuration = 1.8f;
        float chargeElapsed = 0f;
        bool warningShown = false;

        while (chargeElapsed < chargeDuration)
        {
            chargeElapsed += Time.deltaTime;

            if (targetingLaser != null)
            {
                targetingLaser.enabled = true;
                origin = shipLaserOrigin != null ? shipLaserOrigin.position : (transform.position + Vector3.up * 0.4f);
                targetFloor = origin + (transform.forward * beamReach);
                targetFloor.y = 0.5f;

                targetingLaser.SetPosition(0, origin);
                targetingLaser.SetPosition(1, targetFloor);
            }

            // Immediate check: If player is in the lane of the enemy, effect starts immediately (zero wait time)!
            if (playerTransform != null)
            {
                float shipX = transform.position.x;
                bool isPlayerInLane = Mathf.Abs(playerTransform.position.x - shipX) < 1.15f;
                Vector3 toPlayer = playerTransform.position - transform.position;
                float forwardDistance = Vector3.Dot(toPlayer, transform.forward);
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
                    // Player stepped out or swiped to another safe lane: dismiss warning and show dodged
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

        // 3. FIRE THE SHIP'S BEAM CANNON! (Straight in ship forward direction)
        if (SoundManager.Instance != null)
        {
            if (shootSound != null) SoundManager.Instance.PlaySFX(shootSound);
            else if (SoundManager.Instance.fomoLaserShotSound != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.fomoLaserShotSound);
        }

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.ShakeCamera(0.25f, 0.2f);
        }

        origin = shipLaserOrigin != null ? shipLaserOrigin.position : (transform.position + Vector3.up * 0.4f);
        Vector3 beamEnd = origin + (transform.forward * beamReach);
        beamEnd.y = 0.5f;

        if (shipLaser != null)
        {
            shipLaser.enabled = true;
            shipLaser.startWidth = beamWidth;
            shipLaser.endWidth = beamWidth;

            shipLaser.SetPosition(0, origin);
            shipLaser.SetPosition(1, beamEnd);
        }

        // 5. EVALUATE HIT OR DODGE
        // Beam is straight along the spaceship's position.
        // Hit occurs only if the player stays in that lane. If the player swiped left/right, they dodged!
        if (runnerController != null && !runnerController.IsDead)
        {
            float distanceXToBeam = Mathf.Abs(playerTransform.position.x - transform.position.x);
            bool isPlayerInBeamsLane = distanceXToBeam < 1.15f;

            Vector3 toPlayer = playerTransform.position - transform.position;
            float forwardDistance = Vector3.Dot(toPlayer, transform.forward);

            if (isPlayerInBeamsLane && forwardDistance > 0f && forwardDistance <= beamReach)
            {
                // Player stayed in the beam's direct path!
                if (PowerUpManager.Instance != null && PowerUpManager.Instance.IsInvincible)
                {
                    // Deflected
                }
                else if (PowerUpManager.Instance != null && PowerUpManager.Instance.ConsumeShield())
                {
                    // Shield absorbed
                }
                else
                {
                    runnerController.Die();
                }
            }
            else
            {
                // Player was not in the beam's lane when fired
                if (warningShown && AttackWarningHUD.Instance != null)
                {
                    AttackWarningHUD.Instance.ShowDodged();
                }
            }
        }

        // 6. Fade Out Laser Beam
        float fadeElapsed = 0f;
        while (fadeElapsed < beamFireDuration)
        {
            fadeElapsed += Time.deltaTime;
            float t = fadeElapsed / beamFireDuration;
            if (shipLaser != null)
            {
                float w = Mathf.Lerp(beamWidth, 0f, t);
                shipLaser.startWidth = w;
                shipLaser.endWidth = w;
            }
            yield return null;
        }

        if (shipLaser != null) shipLaser.enabled = false;
        isAttacking = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            RunnerController runner = other.GetComponent<RunnerController>();
            if (runner != null && !runner.IsDead)
            {
                if (PowerUpManager.Instance != null && PowerUpManager.Instance.IsInvincible)
                {
                    gameObject.SetActive(false);
                    return;
                }
                if (PowerUpManager.Instance != null && PowerUpManager.Instance.ConsumeShield())
                {
                    gameObject.SetActive(false);
                    return;
                }

                runner.Die();
            }
        }
    }

    private void OnDisable()
    {
        if (activeSpaceship == this)
        {
            activeSpaceship = null;
        }
    }

    private void OnDestroy()
    {
        if (activeSpaceship == this)
        {
            activeSpaceship = null;
        }
    }
}
