using UnityEngine;

public class SkyboxRotator : MonoBehaviour
{
    public float rotationSpeed = 1.5f;

    private float currentRotation;

    private void Start()
    {
        if (RenderSettings.skybox != null)
        {
            currentRotation = RenderSettings.skybox.GetFloat("_Rotation");
        }
    }

    private void Update()
    {
        if (RenderSettings.skybox != null)
        {
            currentRotation += rotationSpeed * Time.deltaTime;
            currentRotation %= 360f;
            RenderSettings.skybox.SetFloat("_Rotation", currentRotation);
        }
    }
}
