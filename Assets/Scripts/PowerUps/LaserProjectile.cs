using UnityEngine;

public class LaserProjectile : MonoBehaviour
{
    private Vector3 direction = Vector3.forward;
    private float speed = 65f;
    private float lifeTime = 1.8f;

    public void Initialize(Vector3 dir, float moveSpeed)
    {
        direction = dir.normalized;
        speed = moveSpeed;

        // Visual setup for the laser bolt
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.transform.SetParent(transform, false);
        cylinder.transform.localScale = new Vector3(0.12f, 0.9f, 0.12f);
        cylinder.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        Collider primCol = cylinder.GetComponent<Collider>();
        if (primCol != null) Destroy(primCol);

        // Glowing red/neon cyan material
        Renderer rend = cylinder.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            mat.color = new Color(1f, 0.2f, 0.1f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(2.5f, 0.4f, 0.2f));
            rend.material = mat;
        }

        // Add point light
        Light lt = gameObject.AddComponent<Light>();
        lt.type = LightType.Point;
        lt.range = 5f;
        lt.intensity = 4f;
        lt.color = new Color(1f, 0.3f, 0.1f);

        // Add sphere collider trigger
        SphereCollider col = gameObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.65f;

        Rigidbody rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        float step = speed * Time.deltaTime;
        transform.position += direction * step;

        // Perform raycast forward to ensure fast collisions are never missed
        Ray ray = new Ray(transform.position, direction);
        if (Physics.Raycast(ray, out RaycastHit hit, step + 0.5f))
        {
            if (hit.collider.CompareTag("Obstacle"))
            {
                HitObstacle(hit.collider.gameObject, hit.point);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Obstacle"))
        {
            HitObstacle(other.gameObject, transform.position);
        }
    }

    private void HitObstacle(GameObject obstacle, Vector3 hitPos)
    {
        // Destroy or disable obstacle
        obstacle.SetActive(false);

        // Create impact burst
        GameObject impact = new GameObject("LaserImpact");
        impact.transform.position = hitPos;
        Light light = impact.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 6f;
        light.intensity = 5f;
        light.color = new Color(1f, 0.4f, 0.1f);
        Destroy(impact, 0.25f);

        if (SoundManager.Instance != null && SoundManager.Instance.crashSound != null)
        {
            SoundManager.Instance.PlaySFX(SoundManager.Instance.crashSound);
        }

        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.ShakeCamera(0.12f, 0.08f);
        }

        Destroy(gameObject);
    }
}
