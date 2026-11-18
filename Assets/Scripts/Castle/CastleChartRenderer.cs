using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RunnerForex.Castle
{
    public class CastleChartRenderer : MonoBehaviour
    {
        [Header("Chart Canvas Areas")]
        public RectTransform chartArea;
        public RectTransform rsiArea;

        [Header("Colors & Styling")]
        public Color bullishColor = new Color(0.08f, 0.88f, 0.42f, 1f); // Vibrant Green
        public Color bearishColor = new Color(0.98f, 0.22f, 0.32f, 1f); // Vibrant Red
        public Color yellowSweepColor = new Color(1f, 0.85f, 0.1f, 1f); // Yellow Sweep Line
        public Color supportColor = new Color(0.2f, 0.6f, 1f, 0.7f);
        public Color resistanceColor = new Color(1f, 0.3f, 0.7f, 0.7f);
        public Color rsiLineColor = new Color(0.6f, 0.3f, 1f, 1f);      // Purple RSI curve
        public Color gridColor = new Color(1f, 1f, 1f, 0.08f);

        [Header("Settings")]
        public float candleBodyWidth = 24f;
        public float candleWickWidth = 2.5f;

        private List<GameObject> spawnedChartElements = new List<GameObject>();
        private GameObject formingCandleBody;
        private bool isAnimatingFormingCandle = false;

        public void RenderChart(CastleChallenge challenge)
        {
            ClearChart();

            if (chartArea == null) chartArea = GetComponent<RectTransform>();
            if (challenge == null || challenge.candles == null || challenge.candles.Count == 0) return;

            float chartWidth = chartArea.rect.width > 0 ? chartArea.rect.width : 560f;
            float chartHeight = chartArea.rect.height > 0 ? chartArea.rect.height : 280f;

            // 1. Calculate price range
            float minPrice = float.MaxValue;
            float maxPrice = float.MinValue;

            foreach (var c in challenge.candles)
            {
                if (c.low < minPrice) minPrice = c.low;
                if (c.high > maxPrice) maxPrice = c.high;
            }

            if (challenge.sweepLevel < minPrice) minPrice = challenge.sweepLevel;
            if (challenge.sweepLevel > maxPrice) maxPrice = challenge.sweepLevel;
            if (challenge.supportLevel < minPrice) minPrice = challenge.supportLevel;
            if (challenge.resistanceLevel > maxPrice) maxPrice = challenge.resistanceLevel;

            // Add 10% padding on top & bottom
            float priceRange = maxPrice - minPrice;
            if (priceRange <= 0f) priceRange = 1f;
            minPrice -= priceRange * 0.08f;
            maxPrice += priceRange * 0.08f;
            priceRange = maxPrice - minPrice;

            // 2. Draw Support, Resistance & Yellow Liquidity Sweep Lines
            DrawHorizontalLine(chartArea, challenge.supportLevel, minPrice, priceRange, chartWidth, chartHeight, supportColor, $"Support {challenge.supportLevel:F2}");
            DrawHorizontalLine(chartArea, challenge.resistanceLevel, minPrice, priceRange, chartWidth, chartHeight, resistanceColor, $"Resistance {challenge.resistanceLevel:F2}");
            DrawHorizontalLine(chartArea, challenge.sweepLevel, minPrice, priceRange, chartWidth, chartHeight, yellowSweepColor, "Liquidity Sweep", 3f);

            // 3. Draw Candlesticks
            int count = challenge.candles.Count;
            float stepX = chartWidth / (count + 1);

            for (int i = 0; i < count; i++)
            {
                CandleData candle = challenge.candles[i];
                float posX = (i + 1) * stepX;

                float openY = Mathf.InverseLerp(minPrice, maxPrice, candle.open) * chartHeight;
                float closeY = Mathf.InverseLerp(minPrice, maxPrice, candle.close) * chartHeight;
                float highY = Mathf.InverseLerp(minPrice, maxPrice, candle.high) * chartHeight;
                float lowY = Mathf.InverseLerp(minPrice, maxPrice, candle.low) * chartHeight;

                Color cColor = candle.IsBullish ? bullishColor : bearishColor;

                // Wick (high to low)
                GameObject wick = CreateUIBar(chartArea, new Vector2(posX, lowY), new Vector2(candleWickWidth, highY - lowY), cColor);
                spawnedChartElements.Add(wick);

                // Body (open to close)
                float bodyBottom = Mathf.Min(openY, closeY);
                float bodyHeight = Mathf.Max(Mathf.Abs(closeY - openY), 3f); // Minimum 3px height
                GameObject body = CreateUIBar(chartArea, new Vector2(posX, bodyBottom), new Vector2(candleBodyWidth, bodyHeight), cColor);
                spawnedChartElements.Add(body);

                // If challenge 15 (last candle is still forming)
                if (i == count - 1 && challenge.isFormingCandle)
                {
                    formingCandleBody = body;
                    isAnimatingFormingCandle = true;
                }
            }

            // 4. Render RSI Panel
            if (rsiArea != null && challenge.rsiData != null && challenge.rsiData.Count > 0)
            {
                RenderRsi(challenge.rsiData);
            }
        }

        private void RenderRsi(List<float> rsiPoints)
        {
            float rsiWidth = rsiArea.rect.width > 0 ? rsiArea.rect.width : 560f;
            float rsiHeight = rsiArea.rect.height > 0 ? rsiArea.rect.height : 70f;

            // Threshold lines at 70 and 30
            float y70 = (70f / 100f) * rsiHeight;
            float y30 = (30f / 100f) * rsiHeight;

            DrawHorizontalLine(rsiArea, 70f, 0f, 100f, rsiWidth, rsiHeight, new Color(1f, 0.3f, 0.3f, 0.5f), "70");
            DrawHorizontalLine(rsiArea, 30f, 0f, 100f, rsiWidth, rsiHeight, new Color(0.3f, 0.8f, 1f, 0.5f), "30");

            // Connect RSI points with line segments
            int count = rsiPoints.Count;
            float stepX = rsiWidth / (count + 1);

            Vector2 prevPoint = Vector2.zero;
            for (int i = 0; i < count; i++)
            {
                float x = (i + 1) * stepX;
                float y = Mathf.Clamp01(rsiPoints[i] / 100f) * rsiHeight;
                Vector2 curPoint = new Vector2(x, y);

                if (i > 0)
                {
                    GameObject segment = CreateLineSegment(rsiArea, prevPoint, curPoint, 2.5f, rsiLineColor);
                    spawnedChartElements.Add(segment);
                }
                prevPoint = curPoint;
            }
        }

        private void DrawHorizontalLine(RectTransform parent, float price, float minP, float rangeP, float width, float height, Color color, string label, float thickness = 1.5f)
        {
            float y = Mathf.InverseLerp(minP, minP + rangeP, price) * height;

            GameObject lineObj = CreateUIBar(parent, new Vector2(width * 0.5f, y), new Vector2(width, thickness), color);
            spawnedChartElements.Add(lineObj);

            if (!string.IsNullOrEmpty(label))
            {
                GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObj.transform.SetParent(parent, false);
                RectTransform rt = labelObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.zero;
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(8f, y + 10f);
                rt.sizeDelta = new Vector2(160, 20);

                TextMeshProUGUI text = labelObj.GetComponent<TextMeshProUGUI>();
                text.text = label;
                text.fontSize = 11;
                text.fontStyle = FontStyles.Bold;
                text.color = color;

                spawnedChartElements.Add(labelObj);
            }
        }

        private GameObject CreateUIBar(RectTransform parent, Vector2 pos, Vector2 size, Color color)
        {
            GameObject bar = new GameObject("Bar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bar.transform.SetParent(parent, false);

            RectTransform rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = bar.GetComponent<Image>();
            img.color = color;

            return bar;
        }

        private GameObject CreateLineSegment(RectTransform parent, Vector2 pointA, Vector2 pointB, float thickness, Color color)
        {
            GameObject seg = new GameObject("RsiSegment", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            seg.transform.SetParent(parent, false);

            RectTransform rt = seg.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);

            Vector2 diff = pointB - pointA;
            rt.sizeDelta = new Vector2(diff.magnitude, thickness);
            rt.anchoredPosition = pointA + (diff * 0.5f);
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg);

            Image img = seg.GetComponent<Image>();
            img.color = color;

            return seg;
        }

        private void Update()
        {
            if (isAnimatingFormingCandle && formingCandleBody != null)
            {
                // Subtle breathing / forming motion on last candle height
                float scaleY = 1.0f + Mathf.Sin(Time.time * 6f) * 0.15f;
                formingCandleBody.transform.localScale = new Vector3(1f, scaleY, 1f);
            }
        }

        public void ClearChart()
        {
            isAnimatingFormingCandle = false;
            formingCandleBody = null;

            foreach (var elem in spawnedChartElements)
            {
                if (elem != null) Destroy(elem);
            }
            spawnedChartElements.Clear();
        }
    }
}
