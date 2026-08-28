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
    [Tooltip("The player transform used to determine when to spawn/despawn.")]
    public Transform player;
    
    [Header("Chunk Configuration")]
    [Tooltip("The very first chunk to spawn (optional, usually a safe zone).")]
    public TrackChunk startChunkPrefab;
    public List<WeightedChunk> availableChunks = new List<WeightedChunk>();

    [Header("Spawn Settings")]
    [Tooltip("How many chunks to keep alive ahead of the player.")]
    public int concurrentChunks = 5;
    [Tooltip("Distance behind the player before a chunk is returned to the pool.")]
    public float despawnDistance = 30f;
    private Queue<TrackChunk> activeChunks = new Queue<TrackChunk>();
    private Transform currentConnectionPoint;
    private float totalWeight;

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
        totalWeight = 0;
        foreach (var chunk in availableChunks)
        {
            totalWeight += chunk.weight;
            
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
        float randomValue = Random.Range(0, totalWeight);
        float currentWeight = 0;

        foreach (var chunk in availableChunks)
        {
            currentWeight += chunk.weight;
            if (randomValue <= currentWeight)
            {
                SpawnChunk(chunk.prefab, chunk.pool);
                return;
            }
        }
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