using UnityEngine;

[ExecuteAlways]
public class WorldBendManager : MonoBehaviour
{
    [Header("Global Bend Settings")]
    [Range(-0.01f, 0.01f)] public float verticalBend = 0.002f;
    [Range(-0.01f, 0.01f)] public float horizontalBend = 0f;

    private static readonly int VerticalBendID = Shader.PropertyToID("_VerticalBend");
    private static readonly int HorizontalBendID = Shader.PropertyToID("_HorizontalBend");

    private void Update()
    {
        Shader.SetGlobalFloat(VerticalBendID, verticalBend);
        Shader.SetGlobalFloat(HorizontalBendID, horizontalBend);
    }
}
