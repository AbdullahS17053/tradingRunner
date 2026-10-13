using UnityEngine;

public class RandomizedTradingDecor : MonoBehaviour
{
    [Header("Materials")]
    public Material bullGreenMaterial;
    public Material bearRedMaterial;

    [Header("Candlestick Elements")]
    public Transform[] candleBodies;
    public Transform[] candleWicks;

    [Header("Randomization Settings")]
    public float minBodyHeight = 0.65f;
    public float maxBodyHeight = 1.65f;
    public float baseHeightY = 2.6f;
    public float maxTrendOffset = 1.1f;

    private Vector3[] initialLocalPositions;
    private bool initialized = false;

    private void Awake()
    {
        CacheInitialPositions();
    }

    private void OnEnable()
    {
        CacheInitialPositions();
        RandomizeChart();
    }

    private void CacheInitialPositions()
    {
        if (initialized || candleBodies == null) return;
        initialLocalPositions = new Vector3[candleBodies.Length];
        for (int i = 0; i < candleBodies.Length; i++)
        {
            if (candleBodies[i] != null)
                initialLocalPositions[i] = candleBodies[i].localPosition;
        }
        initialized = true;
    }

    public void RandomizeChart()
    {
        if (candleBodies == null || candleBodies.Length == 0) return;

        float runningTrendY = Random.Range(-0.4f, 0.4f);

        for (int i = 0; i < candleBodies.Length; i++)
        {
            Transform body = candleBodies[i];
            if (body == null) continue;

            // Reset trend at the halfway point (left side vs right side of street)
            if (i == candleBodies.Length / 2)
                runningTrendY = Random.Range(-0.4f, 0.4f);

            bool isBull = Random.value > 0.45f;
            float bodyHeight = Random.Range(minBodyHeight, maxBodyHeight);
            float stepDelta = (isBull ? 1f : -1f) * (bodyHeight * 0.28f);
            runningTrendY = Mathf.Clamp(runningTrendY + stepDelta, -maxTrendOffset, maxTrendOffset);

            Vector3 pos = initialLocalPositions != null && i < initialLocalPositions.Length
                ? initialLocalPositions[i]
                : body.localPosition;
            pos.y = baseHeightY + runningTrendY;
            body.localPosition = pos;
            body.localScale = new Vector3(0.26f, bodyHeight, 0.26f);

            Material targetMat = isBull ? bullGreenMaterial : bearRedMaterial;
            if (targetMat != null)
            {
                Renderer r = body.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = targetMat;
            }

            if (candleWicks != null && i < candleWicks.Length && candleWicks[i] != null)
            {
                Transform wick = candleWicks[i];
                wick.localScale = new Vector3(0.18f, Random.Range(1.35f, 1.75f), 0.18f);
                if (targetMat != null)
                {
                    Renderer wr = wick.GetComponent<Renderer>();
                    if (wr != null) wr.sharedMaterial = targetMat;
                }
            }
        }
    }
}
