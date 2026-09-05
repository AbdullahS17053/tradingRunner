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

    private Queue<TrackChunk> activeChunks = new Queue<TrackChunk>();
    private Transform currentConnectionPoint;

    private TrackChunk lastSpawnedPrefab;
    private int consecutiveSpawnCount = 0;

    private void Start()
    {
        InitializePools();

        GameObject startPoint = new GameObject("StartPoint");
        startPoint.transform.position = transform.position;
        currentConnectionPoint = startPoint.transform;

        if (startChunkPrefab != null)
        {
            SpawnChunk(startChunkPrefab, null);
        }

        for (int i = 0; i < concurrentChunks; i++)
        {
            SpawnRandomChunk();
        }
    }

    private void Update()
    {
        if (activeChunks.Count == 0) return;

        TrackChunk oldestChunk = activeChunks.Peek();

        if (player.position.z - oldestChunk.transform.position.z > despawnDistance)
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

        float currentTotalWeight = 0f;
        List<WeightedChunk> validChunks = new List<WeightedChunk>();

        foreach (var chunk in availableChunks)
        {
            if (availableChunks.Count > 1 && consecutiveSpawnCount >= 2 && chunk.prefab == lastSpawnedPrefab)
            {
                continue;
            }

            validChunks.Add(chunk);
            currentTotalWeight += chunk.weight;
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

        if (selectedChunk.prefab == lastSpawnedPrefab)
        {
            consecutiveSpawnCount++;
        }
        else
        {
            lastSpawnedPrefab = selectedChunk.prefab;
            consecutiveSpawnCount = 1;
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
    }
}
