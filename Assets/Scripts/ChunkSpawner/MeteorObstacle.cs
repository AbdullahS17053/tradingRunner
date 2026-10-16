using UnityEngine;

public class MeteorObstacle : MonoBehaviour
{
    [Header("Meteor Visual")]
    public Transform visualMesh;

    [Header("Tumbling Rotation")]
    public Vector3 rotationSpeed = new Vector3(20f, 35f, 15f);

    [Header("Hovering & Bobbing")]
    public float hoverSpeed = 2.2f;
    public float hoverAmplitude = 0.22f;

    private Vector3 initialMeshLocalPos;
    private float seedOffset;

    private void Start()
    {
        if (visualMesh == null)
        {
            if (transform.childCount > 0)
            {
                visualMesh = transform.GetChild(0);
            }
            else
            {
                visualMesh = transform;
            }
        }

        initialMeshLocalPos = (visualMesh != transform) ? visualMesh.localPosition : Vector3.zero;
        seedOffset = Random.Range(0f, 100f);

        // Randomize tumbling rotation speed for natural celestial variation
        rotationSpeed = new Vector3(
            Random.Range(-35f, 35f),
            Random.Range(-45f, 45f),
            Random.Range(-30f, 30f)
        );
    }

    private void Update()
    {
        if (visualMesh != null)
        {
            // 1. Continuous gentle tumbling rotation in space
            visualMesh.Rotate(rotationSpeed * Time.deltaTime, Space.Self);

            // 2. Subtle hovering/bobbing in space
            float bob = Mathf.Sin((Time.time + seedOffset) * hoverSpeed) * hoverAmplitude;
            if (visualMesh != transform)
            {
                visualMesh.localPosition = initialMeshLocalPos + Vector3.up * bob;
            }
        }
    }
}
