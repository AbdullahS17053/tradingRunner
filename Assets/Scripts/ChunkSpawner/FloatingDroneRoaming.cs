using UnityEngine;

public class FloatingDroneRoaming : MonoBehaviour
{
    [Header("Roaming Settings")]
    public Vector3 roamRadius = new Vector3(4.5f, 1.2f, 5.5f);
    public float roamSpeed = 0.85f;
    public float bobAmplitude = 0.45f;
    public float bobFrequency = 1.6f;

    private Vector3 baseLocalPos;
    private float seedX;
    private float seedY;
    private float seedZ;
    private Vector3 lastLocalPos;

    private void Awake()
    {
        baseLocalPos = transform.localPosition;
        lastLocalPos = baseLocalPos;
        seedX = Random.Range(0f, 100f);
        seedY = Random.Range(0f, 100f);
        seedZ = Random.Range(0f, 100f);
        roamSpeed *= Random.Range(0.8f, 1.25f);
    }

    private void OnEnable()
    {
        lastLocalPos = transform.localPosition;
    }

    private void Update()
    {
        float t = Time.time * roamSpeed;

        float offsetX = Mathf.Sin(t + seedX) * roamRadius.x + Mathf.Cos(t * 0.63f + seedZ) * (roamRadius.x * 0.35f);
        float offsetY = Mathf.Sin(Time.time * bobFrequency + seedY) * bobAmplitude;
        float offsetZ = Mathf.Cos(t * 0.85f + seedZ) * roamRadius.z + Mathf.Sin(t * 0.51f + seedX) * (roamRadius.z * 0.35f);

        Vector3 targetLocalPos = baseLocalPos + new Vector3(offsetX, offsetY, offsetZ);
        Vector3 delta = targetLocalPos - lastLocalPos;

        transform.localPosition = targetLocalPos;

        if (delta.sqrMagnitude > 0.00001f)
        {
            Vector3 flatDelta = new Vector3(delta.x, delta.y * 0.25f, delta.z);
            if (flatDelta.sqrMagnitude > 0.00001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(flatDelta.normalized, Vector3.up);
                float bankAngle = Mathf.Clamp(-delta.x * 320f, -22f, 22f);
                targetRot *= Quaternion.Euler(0f, 0f, bankAngle);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, 5f * Time.deltaTime);
            }
        }

        lastLocalPos = targetLocalPos;
    }
}
