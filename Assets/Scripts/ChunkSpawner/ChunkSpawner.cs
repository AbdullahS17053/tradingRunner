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

    [Header("Power-Up & Portal Settings")]
    public bool enablePowerUpSpawning = true;
    public int powerUpInterval = 4;
    public int initialChunkGracePeriod = 1;
    public bool enablePortalSpawning = true;
    public int portalInterval = 14;

    [Header("Two Active Power-Ups")]
    public GameObject shieldPowerUpPrefab;
    public GameObject speedBoostPowerUpPrefab;

    private int totalSpawnedChunks = 0;
    private int nextPowerUpTypeIndex = 0;
    private float powerUpTimer = 4.0f;

    private Queue<TrackChunk> activeChunks = new Queue<TrackChunk>();
    private Transform currentConnectionPoint;

    private TrackChunk lastSpawnedPrefab;
    private float currentNetHeading = 0f;

    public void Start()
    {
        InitializePools();

        if (enablePowerUpSpawning)
        {
            PowerUpManager.GetOrCreate();
        }

        GameObject startPoint = new GameObject("StartPoint");
        startPoint.transform.position = transform.position;
        currentConnectionPoint = startPoint.transform;
        currentNetHeading = 0f;

        if (startChunkPrefab != null)
        {
            if (enableLabyrinthTurns)
            {
                lastSpawnedPrefab = startChunkPrefab;
                if (startChunkPrefab.connectionPoint != null)
                {
                    currentNetHeading += Mathf.DeltaAngle(0f, startChunkPrefab.connectionPoint.localEulerAngles.y);
                }
            }
            SpawnChunk(startChunkPrefab, null);
        }

        for (int i = 0; i < concurrentChunks; i++)
        {
            SpawnRandomChunk();
        }
    }

    public void Update()
    {
        powerUpTimer += Time.deltaTime;

        if (activeChunks.Count == 0) return;

        TrackChunk oldestChunk = activeChunks.Peek();

        bool shouldDespawn;
        if (!enableLabyrinthTurns)
        {
            shouldDespawn = (player.position.z - oldestChunk.transform.position.z) > despawnDistance;
        }
        else
        {
            Vector3 exitPos = oldestChunk.connectionPoint != null ? oldestChunk.connectionPoint.position : oldestChunk.transform.position;
            Vector3 exitForward = oldestChunk.connectionPoint != null ? oldestChunk.connectionPoint.forward : oldestChunk.transform.forward;
            float distPastExit = Vector3.Dot(player.position - exitPos, exitForward);
            shouldDespawn = distPastExit > 14f;
        }

        if (shouldDespawn)
        {
            bool returnedToPool = false;

            foreach (var chunkConfig in availableChunks)
            {
                if (oldestChunk.name.StartsWith(chunkConfig.prefab.name))
                {
                    chunkConfig.pool.Release(activeChunks.Dequeue());
                    returnedToPool = true;
                    break;
                }
            }

            if (!returnedToPool)
            {
                TrackChunk unpooledChunk = activeChunks.Dequeue();
                Destroy(unpooledChunk.gameObject);
            }

            SpawnRandomChunk();
        }
    }

    private void InitializePools()
    {
        foreach (var chunk in availableChunks)
        {
            chunk.pool = new ObjectPool<TrackChunk>(
                createFunc: () => Instantiate(chunk.prefab, transform),
                                                    actionOnGet: (obj) => obj.gameObject.SetActive(true),
                                                    actionOnRelease: (obj) => obj.gameObject.SetActive(false),
                                                    actionOnDestroy: (obj) => Destroy(obj.gameObject),
                                                    defaultCapacity: concurrentChunks,
                                                        maxSize: concurrentChunks * 2
            );
        }
    }

    private void SpawnRandomChunk()
    {
        if (availableChunks.Count == 0) return;

        float lastTurn = (lastSpawnedPrefab != null && lastSpawnedPrefab.connectionPoint != null)
            ? Mathf.DeltaAngle(0f, lastSpawnedPrefab.connectionPoint.localEulerAngles.y)
            : 0f;

        float currentTotalWeight = 0f;
        List<WeightedChunk> validChunks = new List<WeightedChunk>();

        foreach (var chunk in availableChunks)
        {
            if (availableChunks.Count > 1 && chunk.prefab == lastSpawnedPrefab)
            {
                continue;
            }

            if (enableLabyrinthTurns && chunk.prefab.connectionPoint != null)
            {
                float chunkTurn = Mathf.DeltaAngle(0f, chunk.prefab.connectionPoint.localEulerAngles.y);

                if (currentNetHeading < -45f)
                {
                    // Currently moving Left (-90): next chunk must turn Right (+90) to return to forward heading
                    if (chunkTurn <= 10f) continue;
                }
                else if (currentNetHeading > 45f)
                {
                    // Currently moving Right (+90): next chunk must turn Left (-90) to return to forward heading
                    if (chunkTurn >= -10f) continue;
                }
                else
                {
                    // Currently moving Forward (0): alternate between Linear chunks and Turn chunks
                    if (Mathf.Abs(lastTurn) > 10f)
                    {
                        // Just finished a Left/Right turn pair -> spawn a Linear chunk next
                        if (Mathf.Abs(chunkTurn) > 10f) continue;
                    }
                    else
                    {
                        // Just finished a Linear chunk -> spawn a Left or Right turn chunk next
                        if (Mathf.Abs(chunkTurn) <= 10f) continue;
                    }
                }
            }

            validChunks.Add(chunk);
            currentTotalWeight += chunk.weight;
        }

        if (validChunks.Count == 0)
        {
            validChunks.Add(availableChunks[0]);
            currentTotalWeight = availableChunks[0].weight;
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
        if (selectedChunk.prefab.connectionPoint != null)
        {
            currentNetHeading += Mathf.DeltaAngle(0f, selectedChunk.prefab.connectionPoint.localEulerAngles.y);
        }

        SpawnChunk(selectedChunk.prefab, selectedChunk.pool);
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

        newChunk.transform.position = currentConnectionPoint.position;
        newChunk.transform.rotation = currentConnectionPoint.rotation;

        activeChunks.Enqueue(newChunk);
        currentConnectionPoint = newChunk.connectionPoint;

        totalSpawnedChunks++;

        bool isLinearChunk = (prefab.connectionPoint == null || Mathf.Abs(Mathf.DeltaAngle(0f, prefab.connectionPoint.localEulerAngles.y)) < 15f);

        if (enablePortalSpawning && totalSpawnedChunks >= portalInterval && (totalSpawnedChunks % portalInterval == 0) && isLinearChunk)
        {
            Vector3 gatePos = new Vector3(0f, 0.05f, 15f);
            PortalTrigger.CreatePortalGate(newChunk.transform, gatePos);
        }
        else if (enablePowerUpSpawning && (totalSpawnedChunks == 2 || powerUpTimer >= 5.0f))
        {
            if (PowerUpManager.Instance != null && PowerUpManager.Instance.CanSpawnPowerUp)
            {
                SpawnPowerUpOnChunk(newChunk);
                powerUpTimer = 0f;
            }
        }
    }

    private void SpawnPowerUpOnChunk(TrackChunk chunk)
    {
        // Pick the opposite power-up from the last triggered one so new worlds/runs never repeat the same start
        PowerUpType targetType = (PowerUpManager.lastActivatedType == PowerUpType.SpeedBoost)
            ? PowerUpType.Shield
            : PowerUpType.SpeedBoost;

        GameObject prefabToSpawn = (targetType == PowerUpType.SpeedBoost) ? speedBoostPowerUpPrefab : shieldPowerUpPrefab;
        if (prefabToSpawn == null)
        {
            string prefabPath = (targetType == PowerUpType.SpeedBoost) ? "PowerUps/PowerUp_SpeedBoost" : "PowerUps/PowerUp_Shield";
            prefabToSpawn = Resources.Load<GameObject>(prefabPath);
        }

        Vector3 spawnWorldPos;
        Quaternion spawnWorldRot = chunk.transform.rotation;

        // Give the exact alignment as coins are available in their position
        if (chunk.coinsParent != null && chunk.coinsParent.childCount > 0)
        {
            int coinIndex = chunk.coinsParent.childCount / 2;
            Transform targetCoin = chunk.coinsParent.GetChild(coinIndex);
            if (targetCoin != null)
            {
                spawnWorldPos = targetCoin.position;
                spawnWorldRot = targetCoin.rotation;
                targetCoin.gameObject.SetActive(false); // Hide coin so power-up takes its exact place
            }
            else
            {
                spawnWorldPos = chunk.transform.position + chunk.transform.forward * 14f + Vector3.up * 1.0f;
            }
        }
        else
        {
            float[] laneOffsets = new float[] { -2.5f, 0f, 2.5f };
            float laneX = laneOffsets[Random.Range(0, laneOffsets.Length)];
            spawnWorldPos = chunk.transform.position + (chunk.transform.right * laneX) + (chunk.transform.forward * 14f) + (Vector3.up * 1.0f);
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
        // Safe no-op to preserve pre-spawned chunk integrity
    }
}
