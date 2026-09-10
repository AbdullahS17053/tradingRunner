using UnityEngine;

[ExecuteAlways]
public class WorldBendController : MonoBehaviour
{
    [Header("Global Bend Settings")]
    [Range(-0.01f, 0.01f)] public float verticalBend = 0.002f;
    [Range(-0.01f, 0.01f)] public float horizontalBend = 0f;

    [Header("Dynamic Bending (Runtime Only)")]
    public bool randomizeHorizontal = true;
    [Tooltip("Maximum curve to the left")]
    public float minHorizontalBend = -0.004f;
    [Tooltip("Maximum curve to the right")]
    public float maxHorizontalBend = 0.004f;
    [Tooltip("How many seconds before picking a new direction")]
    public float changeInterval = 6f;
    [Tooltip("How smoothly the world shifts to the new curve")]
    public float bendTransitionSpeed = 0.5f;

    private float targetHorizontalBend;
    private float timer;

    private static readonly int VerticalBendID = Shader.PropertyToID("_VerticalBend");
    private static readonly int HorizontalBendID = Shader.PropertyToID("_HorizontalBend");

    private void Start()
    {
        if (Application.isPlaying)
        {
            targetHorizontalBend = horizontalBend;
            timer = changeInterval;
        }
    }

    private void Update()
    {
        if (Application.isPlaying && randomizeHorizontal)
        {
            timer += Time.deltaTime;

            if (timer >= changeInterval)
            {
                PickNewBendDirection();
                timer = 0f;
            }

            horizontalBend = Mathf.Lerp(horizontalBend, targetHorizontalBend, Time.deltaTime * bendTransitionSpeed);
        }

        Shader.SetGlobalFloat(VerticalBendID, verticalBend);
        Shader.SetGlobalFloat(HorizontalBendID, horizontalBend);
    }

    private void PickNewBendDirection()
    {
        int choice = Random.Range(0, 3);

        if (choice == 0)
            targetHorizontalBend = Random.Range(minHorizontalBend, -0.001f);
        else if (choice == 1)
            targetHorizontalBend = Random.Range(0.001f, maxHorizontalBend);
        else
            targetHorizontalBend = 0f;
    }
}
