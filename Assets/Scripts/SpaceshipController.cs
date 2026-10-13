using UnityEngine;

[RequireComponent(typeof(RunnerController))]
public class SpaceshipController : MonoBehaviour
{
    [Header("Ship Visual Reference")]
    public Transform shipVisual;
    public GameObject thrusterFXRoot;

    [Header("Hover Settings")]
    public float baseHoverHeight = 0.85f;
    public float slideHoverHeight = 0.35f;
    public float hoverBobAmplitude = 0.08f;
    public float hoverBobFrequency = 2.8f;

    [Header("Banking & Pitch Settings")]
    public float maxBankRollAngle = 28f;
    public float maxBankYawAngle = 9f;
    public float maxPitchAngle = 18f;
    public float rotationSmoothSpeed = 12f;
    public float altitudeSmoothSpeed = 14f;

    private RunnerController runner;
    private float currentHoverY;
    private bool wasDead = false;

    private void Awake()
    {
        runner = GetComponent<RunnerController>();
        currentHoverY = baseHoverHeight;
    }

    private void Start()
    {
        if (shipVisual == null)
        {
            Transform found = transform.Find("SpaceshipVisual");
            if (found != null) shipVisual = found;
        }
    }

    private void LateUpdate()
    {
        if (runner == null || shipVisual == null) return;

        if (runner.IsDead)
        {
            if (!wasDead)
            {
                wasDead = true;
                if (thrusterFXRoot != null) thrusterFXRoot.SetActive(false);
            }
            Quaternion crashRot = Quaternion.Euler(22f, 12f, 38f);
            shipVisual.localRotation = Quaternion.Slerp(shipVisual.localRotation, crashRot, 6f * Time.deltaTime);
            return;
        }

        // 1. Hover & Dive (Slide) Altitude
        float targetY = runner.IsSliding ? slideHoverHeight : baseHoverHeight;
        float bobOffset = Mathf.Sin(Time.time * hoverBobFrequency) * hoverBobAmplitude;
        currentHoverY = Mathf.Lerp(currentHoverY, targetY, altitudeSmoothSpeed * Time.deltaTime);

        Vector3 localPos = shipVisual.localPosition;
        localPos.y = currentHoverY + bobOffset;
        shipVisual.localPosition = localPos;

        // 2. Banking Roll & Yaw when changing lanes, plus Pitch when boosting/diving
        float lateralDelta = Mathf.Clamp(runner.TargetXPosition - transform.position.x, -2.5f, 2.5f) / 2.5f;
        float targetRoll = -lateralDelta * maxBankRollAngle;
        float targetYaw = lateralDelta * maxBankYawAngle;

        float verticalNorm = Mathf.Clamp(runner.VerticalVelocity / Mathf.Max(1f, runner.jumpForce), -1f, 1f);
        float targetPitch = -verticalNorm * maxPitchAngle;
        if (runner.IsSliding && Mathf.Abs(verticalNorm) < 0.2f)
        {
            targetPitch = 10f; // Slight nose-down dive姿态 while sliding under lasers/meteors
        }

        Quaternion targetLocalRot = Quaternion.Euler(targetPitch, targetYaw, targetRoll);
        shipVisual.localRotation = Quaternion.Slerp(shipVisual.localRotation, targetLocalRot, rotationSmoothSpeed * Time.deltaTime);
    }
}
