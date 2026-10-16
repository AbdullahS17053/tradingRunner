using UnityEngine;

public class PlanetRotator : MonoBehaviour
{
    [Header("Rotation Settings")]
    public Vector3 rotationAxis = new Vector3(0.1f, 1f, 0.05f);
    public float rotationSpeed = 1.8f;

    [Header("Parallax Follow (Keeps planets in vista)")]
    public bool enableParallaxFollow = true;
    public float parallaxFactor = 0.95f; // Follows player Z at 95% speed so it stays in skybox

    private Transform playerTransform;
    private float initialZOffset;

    private void Start()
    {
        rotationAxis = rotationAxis.normalized;

        var runner = FindFirstObjectByType<RunnerController>();
        if (runner != null) playerTransform = runner.transform;
        else
        {
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) playerTransform = pObj.transform;
        }

        if (playerTransform != null)
        {
            initialZOffset = transform.position.z - playerTransform.position.z;
        }
    }

    private void Update()
    {
        // 1. Slow, majestic celestial rotation
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.World);

        // 2. Parallax follow so planet never disappears behind or passes away abruptly
        if (enableParallaxFollow && playerTransform != null)
        {
            Vector3 pos = transform.position;
            pos.z = playerTransform.position.z * parallaxFactor + initialZOffset;
            transform.position = pos;
        }
    }
}
