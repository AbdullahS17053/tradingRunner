using UnityEngine;

public class SkyboxRotator : MonoBehaviour
{
    private static readonly int RotationProp = Shader.PropertyToID("_Rotation");

    public float rotationSpeed = 1.5f;

    private float currentRotation;

    private void Start()
    {
        if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty(RotationProp))
        {
            currentRotation = RenderSettings.skybox.GetFloat(RotationProp);
        }
        else
        {
            enabled = false;
        }
    }

    private void Update()
    {
        if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty(RotationProp))
        {
            currentRotation += rotationSpeed * Time.deltaTime;
            currentRotation %= 360f;
            RenderSettings.skybox.SetFloat(RotationProp, currentRotation);
        }
    }
}
