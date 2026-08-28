using UnityEngine;

public class ChunkLaneVisualizer : MonoBehaviour
{
    [Header("Lane Configuration")]
    [Tooltip("Must match the player's lane settings")]
    [Range(1, 9)]
    public int numberOfLanes = 3;
    public float laneSpacing = 2.5f;

    [Header("Chunk Settings")]
    [Tooltip("How long this specific chunk is. Matches your prefab's physical length.")]
    public float chunkLength = 30f;

    [Header("Debug Visuals")]
    public Color normalLaneColor = Color.cyan;
    public Color centerLaneColor = Color.yellow;
    public bool drawInPlayMode = true;

    private void OnDrawGizmos()
    {
        DrawLaneLines(true);
    }

    private void Update()
    {
        if (drawInPlayMode && Application.isPlaying)
        {
            DrawLaneLines(false);
        }
    }

    private void DrawLaneLines(bool useGizmos)
    {
        float leftMostOffset = -(numberOfLanes - 1) * laneSpacing / 2f;

        for (int i = 0; i < numberOfLanes; i++)
        {
            float currentOffset = leftMostOffset + (i * laneSpacing);
            
            // Start at the chunk's origin, move locally right based on the offset
            Vector3 startPos = transform.position + (transform.right * currentOffset);
            // End at the chunk's forward length
            Vector3 endPos = startPos + (transform.forward * chunkLength);

            bool isCenter = (numberOfLanes % 2 != 0) && (i == numberOfLanes / 2);
            Color lineColor = isCenter ? centerLaneColor : normalLaneColor;

            if (useGizmos)
            {
                Gizmos.color = lineColor;
                Gizmos.DrawLine(startPos, endPos);
                Gizmos.DrawSphere(startPos, 0.15f); 
                Gizmos.DrawSphere(endPos, 0.15f); 
            }
            else
            {
                Debug.DrawLine(startPos, endPos, lineColor);
            }
        }
    }
}