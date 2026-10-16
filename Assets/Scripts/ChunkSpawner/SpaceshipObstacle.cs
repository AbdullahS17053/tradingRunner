using UnityEngine;

public class SpaceshipObstacle : MonoBehaviour
{
    [Header("Ship Visual")]
    public Transform visualMesh;

    [Header("Hover & Drift")]
    public float hoverSpeed = 2.4f;
    public float hoverAmplitude = 0.18f;
    public float bankAmplitude = 6.0f;
    public float bankSpeed = 1.6f;

    private Vector3 initialMeshLocalPos;
    private Quaternion initialMeshLocalRot;
    private float seedOffset;

    private void Start()
    {
        // Ensure Tag and Collider are set for runner collision detection
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

        // Add engine exhaust glow light
        GameObject lightObj = new GameObject("EngineGlow", typeof(Light));
        lightObj.transform.SetParent(transform, false);
        lightObj.transform.localPosition = new Vector3(0f, 0.5f, 1.2f);
        Light lt = lightObj.GetComponent<Light>();
        lt.type = LightType.Point;
        lt.range = 5f;
        lt.intensity = 3.5f;
        lt.color = new Color(1f, 0.35f, 0.1f);
    }

    private void Update()
    {
        if (visualMesh != null && visualMesh != transform)
        {
            // Hover bobbing
            float bob = Mathf.Sin((Time.time + seedOffset) * hoverSpeed) * hoverAmplitude;
            visualMesh.localPosition = initialMeshLocalPos + Vector3.up * bob;

            // Subtle roll banking
            float roll = Mathf.Sin((Time.time + seedOffset) * bankSpeed) * bankAmplitude;
            visualMesh.localRotation = initialMeshLocalRot * Quaternion.Euler(0f, 0f, roll);
        }
    }
}
