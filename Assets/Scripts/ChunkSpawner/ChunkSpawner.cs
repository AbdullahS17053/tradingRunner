using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ChunkSpawner : MonoBehaviour
{
    [System.Serializable]
    public class WeightedChunk
    {
        public TrackChunk prefab;
        public float weight = 1f;

        [HideInInspector]
        public ObjectPool<TrackChunk> pool;
    }

    [Header("References")]
    public Transform player;

    [Header("Chunk Configuration")]
    public TrackChunk startChunkPrefab;
    public List<WeightedChunk> availableChunks = new List<WeightedChunk>();

    [Header("Spawn Settings")]
    public int concurrentChunks = 5;
    public float despawnDistance = 30f;

    [Header("Labyrinth Level Settings")]
    [Tooltip("Enable 90-degree directional chunk loop ONLY for Labyrinth World")]
    public bool enableLabyrinthTurns = false;
    [Tooltip("2-Way T-Junction Chunk Prefab for Labyrinth branching")]
    public TrackChunk tJunctionChunkPrefab;

    [Header("Power-Up & Portal Settings")]
    public bool enablePowerUpSpawning = true;
    public int powerUpInterval = 4;
    public int initialChunkGracePeriod = 1;
    public bool enablePortalSpawning = true;
    public int portalInterval = 14;
    public TrackChunk portalChunkPrefab;

    [Header("Active Power-Up Prefabs")]
    public GameObject shieldPowerUpPrefab;
    public GameObject speedBoostPowerUpPrefab;
    public GameObject coinMagnetPowerUpPrefab;
    public GameObject doubleTPPowerUpPrefab;
    public GameObject weaponUpgradePowerUpPrefab;
    public GameObject slowMotionPowerUpPrefab;
    public GameObject invincibilityPowerUpPrefab;

    [Header("Power-Up Placement Settings")]
    [Tooltip("Radius around the power-up that must be completely free of coins and obstacles")]
    public float powerUpClearanceRadius = 2.2f;

    [Header("Enemy Spawning Settings")]
    public bool enableEnemySpawning = true;
    public int enemyInterval = 3;
    public int enemyStartChunk = 2;
    public GameObject enemyPrefab;

    private int totalSpawnedChunks = 0;
    private int nextPowerUpTypeIndex = 0;
    private float powerUpTimer = 4.0f;
    private ObjectPool<TrackChunk> portalChunkPool;

    private Queue<TrackChunk> activeChunks = new Queue<TrackChunk>();
    private Transform currentConnectionPoint;

    private TrackChunk lastSpawnedPrefab;
    private float currentNetHeading = 0f;
    private bool waitingForBranchDecision = false;
    private TrackChunk activeBranchingChunk = null;
    private bool branchContinuationPending = false;

    public void Start()
    {
        if (enableLabyrinthTurns && tJunctionChunkPrefab != null)
        {
            bool exists = false;
            foreach (var c in availableChunks)
            {
                if (c != null && c.prefab == tJunctionChunkPrefab) { exists = true; break; }
            }
            if (!exists)
            {
                availableChunks.Add(new WeightedChunk { prefab = tJunctionChunkPrefab, weight = 2.0f });
            }
        }

        if (enableLabyrinthTurns && concurrentChunks < 6)
        {
            concurrentChunks = 6;
        }

        InitializePools();

        if (enablePowerUpSpawning)
        {
            PowerUpManager.GetOrCreate();
        }

        GameObject startPoint = new GameObject("StartPoint");
        startPoint.transform.position = transform.position;
        currentConnectionPoint = startPoint.transform;
        currentNetHeading = 0f;
        waitingForBranchDecision = false;
        activeBranchingChunk = null;
        branchContinuationPending = false;

        if (startChunkPrefab != null)
        {
            if (enableLabyrinthTurns)
            {
                lastSpawnedPrefab = startChunkPrefab;
                if (!startChunkPrefab.isBranchingChunk && startChunkPrefab.connectionPoint != null)
                {
                    currentNetHeading += Mathf.DeltaAngle(0f, startChunkPrefab.connectionPoint.localEulerAngles.y);
                }
            }
            SpawnChunk(startChunkPrefab, null);
        }

        while (activeChunks.Count < concurrentChunks && !waitingForBranchDecision)
        {
            SpawnRandomChunk();
        }
    }

    public void Update()
    {
        powerUpTimer += Time.deltaTime;

        // Safety net: never let a T-junction dead-end the level. If the junction chunk
        // went away (recycled/never spawned) or the runner is already far past it, the
        // spawner must resume instead of waiting forever for a branch decision.
        if (enableLabyrinthTurns && waitingForBranchDecision)
        {
            bool branchGone = activeBranchingChunk == null
                || activeBranchingChunk.gameObject == null
                || !activeBranchingChunk.gameObject.activeInHierarchy;

            float distPastJunction = 999f;
            if (!branchGone && player != null)
            {
                distPastJunction = Vector3.Dot(player.position - activeBranchingChunk.transform.position, activeBranchingChunk.transform.forward);
            }

            if (branchGone || distPastJunction > 58f)
            {
                waitingForBranchDecision = false;
                activeBranchingChunk = null;
                branchContinuationPending = false;
                currentNetHeading = 0f;
                lastSpawnedPrefab = null;
            }
        }

        if (activeChunks.Count == 0) return;

        TrackChunk oldestChunk = activeChunks.Peek();

        bool shouldDespawn;
        if (!enableLabyrinthTurns)
        {
            shouldDespawn = (player.position.z - oldestChunk.transform.position.z) > despawnDistance;
        }
        else
        {
            if (oldestChunk.isBranchingChunk)
            {
                // Never despawn the branching chunk while still awaiting the player's branch decision
                if (waitingForBranchDecision && oldestChunk == activeBranchingChunk)
                {
                    shouldDespawn = false;
                }
                else
                {
                    float distFromCenter = Vector3.Distance(player.position, oldestChunk.transform.position);
                    float forwardOffset = Vector3.Dot(player.position - oldestChunk.transform.position, oldestChunk.transform.forward);
                    shouldDespawn = (distFromCenter > 48f && forwardOffset > 10f);
                }
            }
            else
            {
                Vector3 exitPos = oldestChunk.connectionPoint != null ? oldestChunk.connectionPoint.position : oldestChunk.transform.position;
                Vector3 exitForward = oldestChunk.connectionPoint != null ? oldestChunk.connectionPoint.forward : oldestChunk.transform.forward;
                float distPastExit = Vector3.Dot(player.position - exitPos, exitForward);
                float distFromChunk = Vector3.Distance(player.position, oldestChunk.transform.position);
                float forwardFromChunk = Vector3.Dot(player.position - oldestChunk.transform.position, oldestChunk.transform.forward);

                // Despawn if runner passed the exit forward, OR if runner is far away in another corridor
                shouldDespawn = (distPastExit > 14f) || (distFromChunk > 48f && forwardFromChunk > 14f);
            }
        }

        if (shouldDespawn)
        {
            TrackChunk despawningChunk = activeChunks.Dequeue();

            // If the junction itself was recycled while still awaiting a decision,
            // drop the wait so spawning does not stop permanently.
            if (despawningChunk == activeBranchingChunk)
            {
                waitingForBranchDecision = false;
                activeBranchingChunk = null;
                branchContinuationPending = false;
            }

            RecycleChunk(despawningChunk);

            while (activeChunks.Count < concurrentChunks && !waitingForBranchDecision)
            {
                SpawnRandomChunk();
            }
        }
    }

    private void RecycleChunk(TrackChunk chunk)
    {
        if (chunk == null) return;

        bool returnedToPool = false;

        if (portalChunkPrefab != null && portalChunkPool != null && chunk.name.StartsWith(portalChunkPrefab.name))
        {
            portalChunkPool.Release(chunk);
            returnedToPool = true;
        }
        else
        {
            foreach (var chunkConfig in availableChunks)
            {
                if (chunkConfig == null || chunkConfig.prefab == null || chunkConfig.pool == null) continue;

                if (chunk.name.StartsWith(chunkConfig.prefab.name))
                {
                    chunkConfig.pool.Release(chunk);
                    returnedToPool = true;
                    break;
                }
            }
        }

        if (!returnedToPool)
        {
            Destroy(chunk.gameObject);
        }
    }

    private void InitializePools()
    {
        foreach (var chunk in availableChunks)
        {
            if (chunk == null || chunk.prefab == null) continue;

            chunk.pool = new ObjectPool<TrackChunk>(
                createFunc: () => Instantiate(chunk.prefab, transform),
                actionOnGet: (obj) => obj.gameObject.SetActive(true),
                actionOnRelease: (obj) => obj.gameObject.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj.gameObject),
                defaultCapacity: concurrentChunks,
                maxSize: concurrentChunks * 2
            );
        }

        if (portalChunkPrefab != null)
        {
            portalChunkPool = new ObjectPool<TrackChunk>(
                createFunc: () => Instantiate(portalChunkPrefab, transform),
                actionOnGet: (obj) => obj.gameObject.SetActive(true),
                actionOnRelease: (obj) => obj.gameObject.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj.gameObject),
                defaultCapacity: 2,
                maxSize: 4
            );
        }
    }

    private void SpawnRandomChunk()
    {
        if (waitingForBranchDecision) return;

        if (branchContinuationPending)
        {
            if (SpawnBranchContinuationChunk())
            {
                return;
            }

            Debug.LogError("[ChunkSpawner] Could not find a straight chunk for the selected labyrinth branch.");
            branchContinuationPending = false;
        }

        if (!branchContinuationPending && enablePortalSpawning && portalChunkPrefab != null && (totalSpawnedChunks + 1) >= portalInterval && ((totalSpawnedChunks + 1) % portalInterval == 0))
        {
            // If in Labyrinth mode, ensure we are heading forward before spawning linear portal chunk
            if (!enableLabyrinthTurns || Mathf.Abs(currentNetHeading) < 10f)
            {
                lastSpawnedPrefab = portalChunkPrefab;
                SpawnChunk(portalChunkPrefab, portalChunkPool);
                return;
            }
        }

        if (availableChunks.Count == 0) return;

        float lastTurn = (lastSpawnedPrefab != null && !lastSpawnedPrefab.isBranchingChunk && lastSpawnedPrefab.connectionPoint != null)
            ? Mathf.DeltaAngle(0f, lastSpawnedPrefab.connectionPoint.localEulerAngles.y)
            : 0f;
        bool lastWasBranch = (lastSpawnedPrefab != null && lastSpawnedPrefab.isBranchingChunk);

        float currentTotalWeight = 0f;
        List<WeightedChunk> validChunks = new List<WeightedChunk>();

        foreach (var chunk in availableChunks)
        {
            if (chunk == null || chunk.prefab == null) continue;

            if (availableChunks.Count > 1 && chunk.prefab == lastSpawnedPrefab)
            {
                continue;
            }

            if (enableLabyrinthTurns)
            {
                bool isBranch = chunk.prefab.isBranchingChunk;
                float chunkTurn = 0f;
                if (!isBranch && chunk.prefab.connectionPoint != null)
                {
                    chunkTurn = Mathf.DeltaAngle(0f, chunk.prefab.connectionPoint.localEulerAngles.y);
                }

                if (currentNetHeading < -45f)
                {
                    // Currently moving Left (-90): next chunk must turn Right (+90) to return to forward heading
                    if (isBranch || chunkTurn <= 10f) continue;
                }
                else if (currentNetHeading > 45f)
                {
                    // Currently moving Right (+90): next chunk must turn Left (-90) to return to forward heading
                    if (isBranch || chunkTurn >= -10f) continue;
                }
                else
                {
                    // Currently moving Forward (0): alternate between Linear chunks and Turn chunks
                    if (Mathf.Abs(lastTurn) > 10f || lastWasBranch)
                    {
                        // Just finished a Left/Right turn pair or branch -> spawn a Linear chunk next
                        if (isBranch || Mathf.Abs(chunkTurn) > 10f) continue;
                    }
                    else
                    {
                        // Just finished a Linear chunk -> spawn a Left, Right, or T-Junction turn chunk next
                        if (!isBranch && Mathf.Abs(chunkTurn) <= 10f) continue;
                    }
                }
            }

            validChunks.Add(chunk);
            currentTotalWeight += chunk.weight;
        }

        if (validChunks.Count == 0)
        {
            // If the filtered list is empty (e.g. lastSpawnedPrefab check excluded the only counter-turn),
            // search availableChunks for the best match ignoring lastSpawnedPrefab
            foreach (var chunk in availableChunks)
            {
                if (chunk == null || chunk.prefab == null) continue;
                bool isBranch = chunk.prefab.isBranchingChunk;
                float chunkTurn = (!isBranch && chunk.prefab.connectionPoint != null)
                    ? Mathf.DeltaAngle(0f, chunk.prefab.connectionPoint.localEulerAngles.y)
                    : 0f;

                if (currentNetHeading < -45f)
                {
                    if (!isBranch && chunkTurn > 10f) { validChunks.Add(chunk); currentTotalWeight += chunk.weight; }
                }
                else if (currentNetHeading > 45f)
                {
                    if (!isBranch && chunkTurn < -10f) { validChunks.Add(chunk); currentTotalWeight += chunk.weight; }
                }
                else
                {
                    if (!isBranch && Mathf.Abs(chunkTurn) <= 10f) { validChunks.Add(chunk); currentTotalWeight += chunk.weight; }
                }
            }

            if (validChunks.Count == 0 && availableChunks.Count > 0)
            {
                validChunks.Add(availableChunks[0]);
                currentTotalWeight = availableChunks[0].weight;
            }
        }

        float randomValue = Random.Range(0, currentTotalWeight);
        float currentWeight = 0;
        WeightedChunk selectedChunk = validChunks[0];

        foreach (var chunk in validChunks)
        {
            currentWeight += chunk.weight;
            if (randomValue <= currentWeight)
            {
                selectedChunk = chunk;
                break;
            }
        }

        lastSpawnedPrefab = selectedChunk.prefab;
        if (!selectedChunk.prefab.isBranchingChunk && selectedChunk.prefab.connectionPoint != null)
        {
            currentNetHeading += Mathf.DeltaAngle(0f, selectedChunk.prefab.connectionPoint.localEulerAngles.y);
        }

        SpawnChunk(selectedChunk.prefab, selectedChunk.pool);
    }

    private bool SpawnBranchContinuationChunk()
    {
        WeightedChunk selectedChunk = null;
        float totalWeight = 0f;

        foreach (var chunk in availableChunks)
        {
            if (chunk == null || chunk.prefab == null || chunk.prefab.isBranchingChunk)
            {
                continue;
            }

            float turn = chunk.prefab.connectionPoint == null
                ? 0f
                : Mathf.Abs(Mathf.DeltaAngle(0f, chunk.prefab.connectionPoint.localEulerAngles.y));

            if (turn <= 10f)
            {
                totalWeight += Mathf.Max(0f, chunk.weight);
            }
        }

        if (totalWeight <= 0f)
        {
            return false;
        }

        float randomValue = Random.Range(0f, totalWeight);
        foreach (var chunk in availableChunks)
        {
            if (chunk == null || chunk.prefab == null || chunk.prefab.isBranchingChunk)
            {
                continue;
            }

            float turn = chunk.prefab.connectionPoint == null
                ? 0f
                : Mathf.Abs(Mathf.DeltaAngle(0f, chunk.prefab.connectionPoint.localEulerAngles.y));

            if (turn > 10f)
            {
                continue;
            }

            randomValue -= Mathf.Max(0f, chunk.weight);
            if (randomValue <= 0f)
            {
                selectedChunk = chunk;
                break;
            }
        }

        if (selectedChunk == null)
        {
            return false;
        }

        branchContinuationPending = false;
        lastSpawnedPrefab = selectedChunk.prefab;
        SpawnChunk(selectedChunk.prefab, selectedChunk.pool);
        return true;
    }

    private void SpawnChunk(TrackChunk prefab, ObjectPool<TrackChunk> pool)
    {
        TrackChunk newChunk;

        if (pool != null)
        {
            newChunk = pool.Get();
            newChunk.name = prefab.name + "_Pooled";
        }
        else
        {
            newChunk = Instantiate(prefab, transform);
        }

        if (enableLabyrinthTurns)
        {
            newChunk.EnsureBranchingSetup();
        }

        newChunk.transform.position = currentConnectionPoint.position;
        newChunk.transform.rotation = currentConnectionPoint.rotation;

        activeChunks.Enqueue(newChunk);
        totalSpawnedChunks++;

        if (enableLabyrinthTurns && newChunk.isBranchingChunk)
        {
            waitingForBranchDecision = true;
            activeBranchingChunk = newChunk;
            lastSpawnedPrefab = prefab;
            return;
        }

        currentConnectionPoint = newChunk.connectionPoint;

        bool isLinearChunk = (prefab.connectionPoint == null || Mathf.Abs(Mathf.DeltaAngle(0f, prefab.connectionPoint.localEulerAngles.y)) < 15f);

        // Fallback procedural portal creation only if no dedicated portalChunkPrefab was configured
        if (portalChunkPrefab == null && enablePortalSpawning && totalSpawnedChunks >= portalInterval && (totalSpawnedChunks % portalInterval == 0) && isLinearChunk)
        {
            Vector3 gatePos = new Vector3(0f, 0.05f, 15f);
            PortalTrigger.CreatePortalGate(newChunk.transform, gatePos);
        }
        else if (enablePowerUpSpawning && (totalSpawnedChunks > initialChunkGracePeriod) && (totalSpawnedChunks == 2 || powerUpTimer >= 5.0f))
        {
            if (PowerUpManager.Instance != null && PowerUpManager.Instance.CanSpawnPowerUp)
            {
                if (SpawnPowerUpOnChunk(newChunk))
                {
                    powerUpTimer = 0f;
                }
            }
        }

        // Procedural enemy spawning on linear chunks
        if (enableEnemySpawning && totalSpawnedChunks >= enemyStartChunk && (totalSpawnedChunks % enemyInterval == 0) && isLinearChunk)
        {
            SpawnEnemyOnChunk(newChunk);
        }
    }

    private void SpawnEnemyOnChunk(TrackChunk chunk)
    {
        // In Space level, only Spaceship enemy obstacles are allowed, no humanoid FOMO bot enemies
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower();
        if (currentScene.Contains("space"))
        {
            return;
        }

        GameObject prefabToSpawn = enemyPrefab;
        if (prefabToSpawn == null)
        {
            prefabToSpawn = Resources.Load<GameObject>("Enemies/LabyrinthGuard_FomoBot");
            if (prefabToSpawn == null)
            {
                prefabToSpawn = Resources.Load<GameObject>("Enemies/FOMO_Cyberpunk");
            }
        }

        if (prefabToSpawn == null) return;

        Quaternion spawnWorldRot = chunk.transform.rotation * Quaternion.Euler(0f, 180f, 0f); // Face towards oncoming player

        // Spawn enemy directly in the oncoming player's lane
        float playerLaneX = 0f;
        if (RunnerController.Instance != null)
        {
            playerLaneX = RunnerController.Instance.TargetXPosition;
        }
        else
        {
            RunnerController runner = Object.FindFirstObjectByType<RunnerController>();
            if (runner != null)
            {
                playerLaneX = runner.TargetXPosition;
            }
        }

        float groundY = chunk.transform.position.y;
        if (chunk.coinsParent != null && chunk.coinsParent.childCount > 0)
        {
            groundY = Mathf.Max(groundY, chunk.coinsParent.GetChild(0).position.y - 0.35f);
        }

        // Spawn 16m forward on the chunk in the player's exact oncoming lane
        Vector3 spawnWorldPos = chunk.transform.TransformPoint(new Vector3(playerLaneX, 0f, 16f));
        spawnWorldPos.y = groundY;

        // If any coins are placed in the center lane at this position, deactivate them so they don't clip through the enemy
        if (chunk.coinsParent != null)
        {
            foreach (Transform coin in chunk.coinsParent)
            {
                if (coin != null && Vector3.Distance(coin.position, spawnWorldPos) < 2.5f)
                {
                    coin.gameObject.SetActive(false);
                }
            }
        }

        GameObject enemyObj = Instantiate(prefabToSpawn, chunk.transform);
        enemyObj.transform.position = spawnWorldPos;
        enemyObj.transform.rotation = spawnWorldRot;
        enemyObj.SetActive(true);
    }

    private bool SpawnPowerUpOnChunk(TrackChunk chunk)
    {
        if (chunk == null) return false;

        // In Labyrinth level, only spawn on linear chunks so power-ups never appear awkwardly in 90-degree turn sections
        bool isLinear = (chunk.connectionPoint == null || Mathf.Abs(Mathf.DeltaAngle(0f, chunk.connectionPoint.localEulerAngles.y)) < 15f);
        if (enableLabyrinthTurns && !isLinear)
        {
            return false;
        }

        // Pick next power-up type from available types, prioritizing variety (WeaponUpgrade disabled)
        List<PowerUpType> candidates = new List<PowerUpType>
        {
            PowerUpType.SpeedBoost,
            PowerUpType.Shield,
            PowerUpType.CoinMagnet,
            PowerUpType.DoubleTP,
            PowerUpType.SlowMotion,
            PowerUpType.Invincibility
        };
        if (candidates.Contains(PowerUpManager.lastActivatedType) && candidates.Count > 1)
        {
            candidates.Remove(PowerUpManager.lastActivatedType);
        }
        PowerUpType targetType = candidates[Random.Range(0, candidates.Count)];

        // Retrieve level-specific lane settings
        int numLanes = 3;
        float laneSpacing = 2.5f;

        if (RunnerController.Instance != null)
        {
            numLanes = RunnerController.Instance.numberOfLanes;
            laneSpacing = RunnerController.Instance.laneSpacing;
        }
        else if (player != null)
        {
            RunnerController rc = player.GetComponent<RunnerController>();
            if (rc != null)
            {
                numLanes = rc.numberOfLanes;
                laneSpacing = rc.laneSpacing;
            }
        }
        else
        {
            RunnerController rc = Object.FindAnyObjectByType<RunnerController>();
            if (rc != null)
            {
                numLanes = rc.numberOfLanes;
                laneSpacing = rc.laneSpacing;
            }
        }

        // Calculate clearance radius capped slightly below lane spacing so adjacent lanes don't block each other
        float effectiveRadius = Mathf.Min(powerUpClearanceRadius, laneSpacing * 0.88f);
        effectiveRadius = Mathf.Max(1.8f, effectiveRadius);

        // Determine forward distance range on this chunk
        float minZ = 7f;
        float maxZ = 24f;

        if (chunk.connectionPoint != null)
        {
            float chunkLength = chunk.connectionPoint.localPosition.z;
            if (chunkLength > 16f)
            {
                minZ = 6f;
                maxZ = Mathf.Min(chunkLength - 6f, 26f);
            }
        }

        // 1. Randomize the candidate lanes
        List<int> candidateLanes = new List<int>();
        for (int i = 0; i < numLanes; i++)
        {
            candidateLanes.Add(i);
        }
        for (int i = candidateLanes.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            int tmp = candidateLanes[i];
            candidateLanes[i] = candidateLanes[rnd];
            candidateLanes[rnd] = tmp;
        }

        // 2. Generate and shuffle candidate forward distances
        List<float> candidateZOffsets = new List<float>();
        for (float z = minZ; z <= maxZ; z += 2.5f)
        {
            candidateZOffsets.Add(z);
        }
        for (int i = candidateZOffsets.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            float tmp = candidateZOffsets[i];
            candidateZOffsets[i] = candidateZOffsets[rnd];
            candidateZOffsets[rnd] = tmp;
        }

        float leftMostOffset = -(numLanes - 1) * laneSpacing / 2f;

        // Search for a clear spot across candidate lanes and forward distances
        if (TryFindAndSpawnSpot(chunk, candidateLanes, candidateZOffsets, leftMostOffset, laneSpacing, effectiveRadius, targetType))
        {
            return true;
        }

        // Fallback pass with slightly tighter clearance before skipping this chunk
        float fallbackRadius = 1.8f;
        if (effectiveRadius > fallbackRadius)
        {
            if (TryFindAndSpawnSpot(chunk, candidateLanes, candidateZOffsets, leftMostOffset, laneSpacing, fallbackRadius, targetType))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryFindAndSpawnSpot(TrackChunk chunk, List<int> lanes, List<float> zOffsets, float leftMostOffset, float laneSpacing, float radius, PowerUpType targetType)
    {
        foreach (int laneIndex in lanes)
        {
            float laneOffset = leftMostOffset + (laneIndex * laneSpacing);

            foreach (float zOffset in zOffsets)
            {
                Vector3 rayOrigin = chunk.transform.position
                    + (chunk.transform.right * laneOffset)
                    + (chunk.transform.forward * zOffset)
                    + (chunk.transform.up * 3.0f);

                Vector3 candidatePos;

                // Cast ray down to find exact ground surface
                if (Physics.Raycast(rayOrigin, -chunk.transform.up, out RaycastHit groundHit, 6.0f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (IsObstacleCollider(groundHit.collider))
                    {
                        continue;
                    }
                    candidatePos = groundHit.point + chunk.transform.up * 1.0f;
                }
                else
                {
                    candidatePos = chunk.transform.position
                        + (chunk.transform.right * laneOffset)
                        + (chunk.transform.forward * zOffset)
                        + (chunk.transform.up * 1.0f);
                }

                if (IsSpotClear(candidatePos, radius, chunk))
                {
                    SpawnPowerUpAt(candidatePos, chunk.transform.rotation, chunk, targetType);
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsSpotClear(Vector3 candidatePos, float radius, TrackChunk chunk)
    {
        Physics.SyncTransforms();

        // 1. Direct check against coinsParent children in the chunk
        if (chunk.coinsParent != null)
        {
            for (int i = 0; i < chunk.coinsParent.childCount; i++)
            {
                Transform coinTr = chunk.coinsParent.GetChild(i);
                if (coinTr != null && coinTr.gameObject.activeSelf)
                {
                    if (Vector3.Distance(candidatePos, coinTr.position) < radius)
                    {
                        return false;
                    }
                }
            }
        }

        // 2. Direct check against all Collectibles in the chunk
        Collectible[] collectibles = chunk.GetComponentsInChildren<Collectible>(false);
        foreach (var coin in collectibles)
        {
            if (coin != null && coin.gameObject.activeSelf)
            {
                if (Vector3.Distance(candidatePos, coin.transform.position) < radius)
                {
                    return false;
                }
            }
        }

        // 3. Direct check against all obstacle colliders in the chunk
        Collider[] chunkColliders = chunk.GetComponentsInChildren<Collider>(false);
        foreach (var col in chunkColliders)
        {
            if (col == null || !col.gameObject.activeSelf) continue;

            if (IsObstacleCollider(col))
            {
                Vector3 closest = col.ClosestPoint(candidatePos);
                if (Vector3.Distance(candidatePos, closest) < radius)
                {
                    return false;
                }
            }
        }

        // 4. OverlapSphere check in world space to catch any nearby obstacles, coins, or special triggers
        Collider[] hits = Physics.OverlapSphere(candidatePos, radius, ~0, QueryTriggerInteraction.Collide);
        foreach (var hit in hits)
        {
            if (hit == null || !hit.gameObject.activeSelf) continue;

            if (IsTrackEnvironment(hit)) continue;

            if (IsObstacleCollider(hit))
            {
                return false;
            }

            if (IsCoinCollider(hit))
            {
                return false;
            }

            if (hit.GetComponentInParent<PortalTrigger>() != null
                || hit.GetComponentInParent<LabyrinthTurnZone>() != null
                || hit.GetComponentInParent<PowerUpPickup>() != null)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsObstacleCollider(Collider col)
    {
        if (col == null) return false;

        if (col.CompareTag("Obstacle")) return true;

        Transform tr = col.transform;
        while (tr != null && tr != transform)
        {
            if (tr.CompareTag("Obstacle")) return true;
            string name = tr.name;
            if (name.IndexOf("obstacle", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("barrier", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("laser", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("meteor", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
            tr = tr.parent;
        }

        return false;
    }

    private bool IsCoinCollider(Collider col)
    {
        if (col == null) return false;

        if (col.CompareTag("Coin")) return true;
        if (col.GetComponentInParent<Collectible>() != null) return true;

        Transform tr = col.transform;
        while (tr != null && tr != transform)
        {
            if (tr.CompareTag("Coin") || tr.name.IndexOf("coin", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
            tr = tr.parent;
        }

        return false;
    }

    private bool IsTrackEnvironment(Collider col)
    {
        if (col == null) return true;

        if (col.CompareTag("Obstacle") || col.CompareTag("Coin")) return false;
        if (col.GetComponentInParent<Collectible>() != null) return false;
        if (col.GetComponentInParent<PowerUpPickup>() != null) return false;
        if (col.GetComponentInParent<PortalTrigger>() != null) return false;
        if (col.GetComponentInParent<LabyrinthTurnZone>() != null) return false;

        string colName = col.name.ToLower();
        if (colName.Contains("road") || colName.Contains("track") || colName.Contains("ground")
            || colName.Contains("floor") || colName.Contains("basechunk") || colName.Contains("plane")
            || colName.Contains("tile") || colName.Contains("tunnel") || colName.Contains("street"))
        {
            return true;
        }

        if (!col.isTrigger && !IsObstacleCollider(col))
        {
            if (col.bounds.max.y <= 0.35f)
            {
                return true;
            }
        }

        return false;
    }

    private void SpawnPowerUpAt(Vector3 spawnWorldPos, Quaternion spawnWorldRot, TrackChunk chunk, PowerUpType targetType)
    {
        if (targetType == PowerUpType.WeaponUpgrade)
        {
            return;
        }

        GameObject prefabToSpawn = null;
        switch (targetType)
        {
            case PowerUpType.SpeedBoost: prefabToSpawn = speedBoostPowerUpPrefab; break;
            case PowerUpType.Shield: prefabToSpawn = shieldPowerUpPrefab; break;
            case PowerUpType.CoinMagnet: prefabToSpawn = coinMagnetPowerUpPrefab; break;
            case PowerUpType.DoubleTP: prefabToSpawn = doubleTPPowerUpPrefab; break;
            case PowerUpType.WeaponUpgrade: prefabToSpawn = weaponUpgradePowerUpPrefab; break;
            case PowerUpType.SlowMotion: prefabToSpawn = slowMotionPowerUpPrefab; break;
            case PowerUpType.Invincibility: prefabToSpawn = invincibilityPowerUpPrefab; break;
        }

        if (prefabToSpawn == null)
        {
            string prefabPath = "PowerUps/PowerUp_" + targetType.ToString();
            prefabToSpawn = Resources.Load<GameObject>(prefabPath);
        }

        if (prefabToSpawn != null)
        {
            GameObject powerUpObj = Instantiate(prefabToSpawn, chunk.transform);
            powerUpObj.transform.position = spawnWorldPos;
            powerUpObj.transform.rotation = spawnWorldRot;
        }
        else
        {
            Vector3 localPos = chunk.transform.InverseTransformPoint(spawnWorldPos);
            PowerUpPickup.CreatePowerUp(chunk.transform, localPos, targetType);
        }
    }

    public void OnLabyrinthBranchChosen(float finalAngle, Transform branchPivot)
    {
        OnLabyrinthBranchChosen(null, finalAngle, branchPivot);
    }

    public void OnLabyrinthBranchChosen(TrackChunk branchingChunk, float finalAngle, Transform branchPivot)
    {
        if (!enableLabyrinthTurns) return;

        // If a specific branching chunk was passed, verify it matches activeBranchingChunk if one is pending
        if (branchingChunk != null && activeBranchingChunk != null && branchingChunk != activeBranchingChunk)
        {
            return;
        }

        TrackChunk targetChunk = branchingChunk != null ? branchingChunk : activeBranchingChunk;
        if (targetChunk == null)
        {
            foreach (var ch in activeChunks)
            {
                if (ch != null && ch.isBranchingChunk)
                {
                    targetChunk = ch;
                    break;
                }
            }
        }

        if (targetChunk == null) return;

        targetChunk.EnsureBranchingSetup();
        Transform chosenExit = targetChunk.GetConnectionPoint(finalAngle);
        if (chosenExit != null)
        {
            currentConnectionPoint = chosenExit;
        }
        else if (branchPivot != null)
        {
            currentConnectionPoint = branchPivot;
        }

        currentNetHeading += finalAngle;
        lastSpawnedPrefab = targetChunk;
        waitingForBranchDecision = false;
        activeBranchingChunk = null;
        branchContinuationPending = true;

        // 1. Spawn continuation chunk along the chosen branch
        SpawnBranchContinuationChunk();

        // 2. Ensure a healthy runway of chunks is spawned along the chosen branch
        for (int i = 0; i < 3 && !waitingForBranchDecision; i++)
        {
            SpawnRandomChunk();
        }
    }
}
