using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RunnerForex.Castle
{
    /// <summary>
    /// Procedural UI Candlestick Pattern badge.
    /// Renders all 30 candlestick patterns from trader data using sharp UI bars, wicks, and trendlines.
    /// Eliminates dependency on missing external PNG sprites and allows hierarchy tweaking.
    /// </summary>
    [ExecuteAlways]
    public class CandlestickBadge : MonoBehaviour
    {
        public struct CandleElement
        {
            public string name;
            public Vector2 pos;
            public Vector2 size;
            public Color color;
            public float rotationZ;

            public CandleElement(string name, Vector2 pos, Vector2 size, Color color, float rotationZ = 0f)
            {
                this.name = name;
                this.pos = pos;
                this.size = size;
                this.color = color;
                this.rotationZ = rotationZ;
            }
        }

        [Header("Pattern ID (1 - 30)")]
        [Range(1, 30)]
        public int patternId = 1;

        [Header("Trader Theme Colors")]
        public Color bullishColor = new Color(0.05f, 0.95f, 0.35f, 1f); // Vibrant Neon Green
        public Color bearishColor = new Color(1.00f, 0.20f, 0.25f, 1f); // Vibrant Neon Red
        public Color neutralColor = new Color(0.15f, 0.90f, 1.00f, 1f); // Vibrant Neon Cyan
        public Color lineColor = new Color(1.00f, 0.85f, 0.20f, 0.95f); // Gold Trendline
        public Color whiteLineColor = new Color(1.00f, 1.00f, 1.00f, 0.70f); // Soft White Level

        private int lastBuiltPatternId = -1;

        private void Awake()
        {
            if (transform.childCount == 0 && patternId > 0)
            {
                Build(patternId);
            }
        }

        private void OnValidate()
        {
            if (patternId != lastBuiltPatternId)
            {
                Build(patternId);
            }
        }

        /// <summary>
        /// Builds or updates all bars, wicks, and trendlines for the specified pattern ID.
        /// </summary>
        public void Build(int id)
        {
            patternId = Mathf.Clamp(id, 1, 30);
            lastBuiltPatternId = patternId;

            // Clear existing elements
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }

            List<CandleElement> elements = GetPatternElements(patternId, bullishColor, bearishColor, neutralColor, lineColor, whiteLineColor);
            RectTransform parentRt = GetComponent<RectTransform>();
            if (parentRt == null) parentRt = gameObject.AddComponent<RectTransform>();

            foreach (var el in elements)
            {
                GameObject obj = new GameObject(el.name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                obj.transform.SetParent(transform, false);

                RectTransform rt = obj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = el.pos;
                rt.sizeDelta = el.size;
                if (Mathf.Abs(el.rotationZ) > 0.01f)
                {
                    rt.localRotation = Quaternion.Euler(0, 0, el.rotationZ);
                }

                Image img = obj.GetComponent<Image>();
                img.color = el.color;
                img.raycastTarget = false;
            }
        }

        /// <summary>
        /// Returns the exact candlestick geometries for each of the 30 patterns.
        /// </summary>
        public static List<CandleElement> GetPatternElements(int id, Color bull, Color bear, Color neutral, Color line, Color whiteLine)
        {
            List<CandleElement> list = new List<CandleElement>();

            switch (id)
            {
                // 1. Doji
                case 1:
                    list.Add(new CandleElement("Wick", new Vector2(0, 0), new Vector2(2.5f, 44), bull));
                    list.Add(new CandleElement("Body", new Vector2(0, 0), new Vector2(16, 3), bull));
                    break;

                // 2. Hammer
                case 2:
                    list.Add(new CandleElement("UpperWick", new Vector2(0, 16), new Vector2(2.5f, 4), bull));
                    list.Add(new CandleElement("Body", new Vector2(0, 9), new Vector2(14, 12), bull));
                    list.Add(new CandleElement("LowerWick", new Vector2(0, -11), new Vector2(2.5f, 28), bull));
                    break;

                // 3. Hanging Man
                case 3:
                    list.Add(new CandleElement("UpperWick", new Vector2(0, 16), new Vector2(2.5f, 4), bear));
                    list.Add(new CandleElement("Body", new Vector2(0, 9), new Vector2(14, 12), bear));
                    list.Add(new CandleElement("LowerWick", new Vector2(0, -11), new Vector2(2.5f, 28), bear));
                    break;

                // 4. Shooting Star
                case 4:
                    list.Add(new CandleElement("UpperWick", new Vector2(0, 11), new Vector2(2.5f, 28), bear));
                    list.Add(new CandleElement("Body", new Vector2(0, -9), new Vector2(14, 12), bear));
                    list.Add(new CandleElement("LowerWick", new Vector2(0, -16), new Vector2(2.5f, 4), bear));
                    break;

                // 5. Inverted Hammer
                case 5:
                    list.Add(new CandleElement("UpperWick", new Vector2(0, 11), new Vector2(2.5f, 28), bull));
                    list.Add(new CandleElement("Body", new Vector2(0, -9), new Vector2(14, 12), bull));
                    list.Add(new CandleElement("LowerWick", new Vector2(0, -16), new Vector2(2.5f, 4), bull));
                    break;

                // 6. Bullish Engulfing
                case 6:
                    list.Add(new CandleElement("RedWick", new Vector2(-10, 0), new Vector2(2, 24), bear));
                    list.Add(new CandleElement("RedBody", new Vector2(-10, 0), new Vector2(10, 14), bear));
                    list.Add(new CandleElement("GreenWick", new Vector2(10, 1), new Vector2(2.5f, 42), bull));
                    list.Add(new CandleElement("GreenBody", new Vector2(10, 1), new Vector2(14, 32), bull));
                    break;

                // 7. Bearish Engulfing
                case 7:
                    list.Add(new CandleElement("GreenWick", new Vector2(-10, 0), new Vector2(2, 24), bull));
                    list.Add(new CandleElement("GreenBody", new Vector2(-10, 0), new Vector2(10, 14), bull));
                    list.Add(new CandleElement("RedWick", new Vector2(10, 1), new Vector2(2.5f, 42), bear));
                    list.Add(new CandleElement("RedBody", new Vector2(10, 1), new Vector2(14, 32), bear));
                    break;

                // 8. Morning Star
                case 8:
                    list.Add(new CandleElement("RedWick", new Vector2(-18, 5), new Vector2(2, 34), bear));
                    list.Add(new CandleElement("RedBody", new Vector2(-18, 5), new Vector2(10, 24), bear));
                    list.Add(new CandleElement("StarWick", new Vector2(0, -15), new Vector2(2, 14), bull));
                    list.Add(new CandleElement("StarBody", new Vector2(0, -15), new Vector2(8, 4), bull));
                    list.Add(new CandleElement("GreenWick", new Vector2(18, 2), new Vector2(2.5f, 34), bull));
                    list.Add(new CandleElement("GreenBody", new Vector2(18, 2), new Vector2(10, 22), bull));
                    break;

                // 9. Evening Star
                case 9:
                    list.Add(new CandleElement("GreenWick", new Vector2(-18, -2), new Vector2(2.5f, 34), bull));
                    list.Add(new CandleElement("GreenBody", new Vector2(-18, -2), new Vector2(10, 22), bull));
                    list.Add(new CandleElement("StarWick", new Vector2(0, 15), new Vector2(2, 14), bear));
                    list.Add(new CandleElement("StarBody", new Vector2(0, 15), new Vector2(8, 4), bear));
                    list.Add(new CandleElement("RedWick", new Vector2(18, -5), new Vector2(2, 34), bear));
                    list.Add(new CandleElement("RedBody", new Vector2(18, -5), new Vector2(10, 24), bear));
                    break;

                // 10. Marubozu
                case 10:
                    list.Add(new CandleElement("RedMarubozu", new Vector2(-10, 0), new Vector2(12, 38), bear));
                    list.Add(new CandleElement("GreenMarubozu", new Vector2(10, 0), new Vector2(12, 38), bull));
                    break;

                // 11. Spinning Top
                case 11:
                    list.Add(new CandleElement("Wick", new Vector2(0, 0), new Vector2(2.5f, 44), bull));
                    list.Add(new CandleElement("Body", new Vector2(0, 0), new Vector2(14, 10), bull));
                    break;

                // 12. Bullish Harami
                case 12:
                    list.Add(new CandleElement("MotherWick", new Vector2(-10, 0), new Vector2(2, 40), bear));
                    list.Add(new CandleElement("MotherBody", new Vector2(-10, 0), new Vector2(12, 32), bear));
                    list.Add(new CandleElement("BabyWick", new Vector2(10, 0), new Vector2(2, 18), bull));
                    list.Add(new CandleElement("BabyBody", new Vector2(10, 0), new Vector2(9, 12), bull));
                    break;

                // 13. Bearish Harami
                case 13:
                    list.Add(new CandleElement("MotherWick", new Vector2(-10, 0), new Vector2(2.5f, 40), bull));
                    list.Add(new CandleElement("MotherBody", new Vector2(-10, 0), new Vector2(12, 32), bull));
                    list.Add(new CandleElement("BabyWick", new Vector2(10, 0), new Vector2(2, 18), bear));
                    list.Add(new CandleElement("BabyBody", new Vector2(10, 0), new Vector2(9, 12), bear));
                    break;

                // 14. Piercing Line
                case 14:
                    list.Add(new CandleElement("RedWick", new Vector2(-10, 4), new Vector2(2, 38), bear));
                    list.Add(new CandleElement("RedBody", new Vector2(-10, 4), new Vector2(11, 28), bear));
                    list.Add(new CandleElement("GreenWick", new Vector2(10, -2), new Vector2(2.5f, 38), bull));
                    list.Add(new CandleElement("GreenBody", new Vector2(10, -2), new Vector2(11, 26), bull));
                    break;

                // 15. Dark Cloud Cover
                case 15:
                    list.Add(new CandleElement("GreenWick", new Vector2(-10, -2), new Vector2(2.5f, 38), bull));
                    list.Add(new CandleElement("GreenBody", new Vector2(-10, -2), new Vector2(11, 26), bull));
                    list.Add(new CandleElement("RedWick", new Vector2(10, 4), new Vector2(2, 38), bear));
                    list.Add(new CandleElement("RedBody", new Vector2(10, 4), new Vector2(11, 28), bear));
                    break;

                // 16. Tweezer Top
                case 16:
                    list.Add(new CandleElement("ResistanceLine", new Vector2(0, 19), new Vector2(36, 2), whiteLine));
                    list.Add(new CandleElement("GreenWick", new Vector2(-9, 0), new Vector2(2, 38), bull));
                    list.Add(new CandleElement("GreenBody", new Vector2(-9, -3), new Vector2(11, 22), bull));
                    list.Add(new CandleElement("RedWick", new Vector2(9, 0), new Vector2(2, 38), bear));
                    list.Add(new CandleElement("RedBody", new Vector2(9, -3), new Vector2(11, 22), bear));
                    break;

                // 17. Tweezer Bottom
                case 17:
                    list.Add(new CandleElement("SupportLine", new Vector2(0, -19), new Vector2(36, 2), whiteLine));
                    list.Add(new CandleElement("RedWick", new Vector2(-9, 0), new Vector2(2, 38), bear));
                    list.Add(new CandleElement("RedBody", new Vector2(-9, 3), new Vector2(11, 22), bear));
                    list.Add(new CandleElement("GreenWick", new Vector2(9, 0), new Vector2(2, 38), bull));
                    list.Add(new CandleElement("GreenBody", new Vector2(9, 3), new Vector2(11, 22), bull));
                    break;

                // 18. Three White Soldiers
                case 18:
                    list.Add(new CandleElement("Soldier1Wick", new Vector2(-18, -10), new Vector2(2, 22), bull));
                    list.Add(new CandleElement("Soldier1Body", new Vector2(-18, -10), new Vector2(10, 16), bull));
                    list.Add(new CandleElement("Soldier2Wick", new Vector2(0, 0), new Vector2(2, 24), bull));
                    list.Add(new CandleElement("Soldier2Body", new Vector2(0, 0), new Vector2(10, 18), bull));
                    list.Add(new CandleElement("Soldier3Wick", new Vector2(18, 10), new Vector2(2, 26), bull));
                    list.Add(new CandleElement("Soldier3Body", new Vector2(18, 10), new Vector2(10, 20), bull));
                    break;

                // 19. Three Black Crows
                case 19:
                    list.Add(new CandleElement("Crow1Wick", new Vector2(-18, 10), new Vector2(2, 26), bear));
                    list.Add(new CandleElement("Crow1Body", new Vector2(-18, 10), new Vector2(10, 20), bear));
                    list.Add(new CandleElement("Crow2Wick", new Vector2(0, 0), new Vector2(2, 24), bear));
                    list.Add(new CandleElement("Crow2Body", new Vector2(0, 0), new Vector2(10, 18), bear));
                    list.Add(new CandleElement("Crow3Wick", new Vector2(18, -10), new Vector2(2, 22), bear));
                    list.Add(new CandleElement("Crow3Body", new Vector2(18, -10), new Vector2(10, 16), bear));
                    break;

                // 20. Tri-Star
                case 20:
                    list.Add(new CandleElement("Star1Wick", new Vector2(-18, 4), new Vector2(2, 24), bull));
                    list.Add(new CandleElement("Star1Body", new Vector2(-18, 4), new Vector2(12, 3), bull));
                    list.Add(new CandleElement("Star2Wick", new Vector2(0, -10), new Vector2(2, 24), neutral));
                    list.Add(new CandleElement("Star2Body", new Vector2(0, -10), new Vector2(12, 3), neutral));
                    list.Add(new CandleElement("Star3Wick", new Vector2(18, 4), new Vector2(2, 24), bull));
                    list.Add(new CandleElement("Star3Body", new Vector2(18, 4), new Vector2(12, 3), bull));
                    break;

                // 21. Falling Three
                case 21:
                    list.Add(new CandleElement("Red1Wick", new Vector2(-22, 2), new Vector2(2, 40), bear));
                    list.Add(new CandleElement("Red1Body", new Vector2(-22, 2), new Vector2(9, 32), bear));
                    list.Add(new CandleElement("Green2", new Vector2(-10, -8), new Vector2(6, 8), bull));
                    list.Add(new CandleElement("Green3", new Vector2(-1, 0), new Vector2(6, 8), bull));
                    list.Add(new CandleElement("Green4", new Vector2(8, 8), new Vector2(6, 8), bull));
                    list.Add(new CandleElement("Red5Wick", new Vector2(20, -4), new Vector2(2, 40), bear));
                    list.Add(new CandleElement("Red5Body", new Vector2(20, -4), new Vector2(9, 32), bear));
                    break;

                // 22. Rising Three
                case 22:
                    list.Add(new CandleElement("Green1Wick", new Vector2(-22, -2), new Vector2(2, 40), bull));
                    list.Add(new CandleElement("Green1Body", new Vector2(-22, -2), new Vector2(9, 32), bull));
                    list.Add(new CandleElement("Red2", new Vector2(-10, 8), new Vector2(6, 8), bear));
                    list.Add(new CandleElement("Red3", new Vector2(-1, 0), new Vector2(6, 8), bear));
                    list.Add(new CandleElement("Red4", new Vector2(8, -8), new Vector2(6, 8), bear));
                    list.Add(new CandleElement("Green5Wick", new Vector2(20, 4), new Vector2(2, 40), bull));
                    list.Add(new CandleElement("Green5Body", new Vector2(20, 4), new Vector2(9, 32), bull));
                    break;

                // 23. Inside Bar
                case 23:
                    list.Add(new CandleElement("MotherWick", new Vector2(-10, 0), new Vector2(2.5f, 42), bull));
                    list.Add(new CandleElement("MotherBody", new Vector2(-10, 0), new Vector2(12, 32), bull));
                    list.Add(new CandleElement("InsideWick", new Vector2(10, 0), new Vector2(2, 20), bear));
                    list.Add(new CandleElement("InsideBody", new Vector2(10, 0), new Vector2(9, 14), bear));
                    break;

                // 24. Outside Bar
                case 24:
                    list.Add(new CandleElement("PriorWick", new Vector2(-10, 0), new Vector2(2, 20), bear));
                    list.Add(new CandleElement("PriorBody", new Vector2(-10, 0), new Vector2(9, 14), bear));
                    list.Add(new CandleElement("OutsideWick", new Vector2(10, 0), new Vector2(2.5f, 44), bull));
                    list.Add(new CandleElement("OutsideBody", new Vector2(10, 0), new Vector2(13, 34), bull));
                    break;

                // 25. Bullish Flag
                case 25:
                    list.Add(new CandleElement("Pole", new Vector2(-20, 0), new Vector2(6, 36), bull));
                    list.Add(new CandleElement("TopFlagLine", new Vector2(5, 12), new Vector2(30, 2), line, -22f));
                    list.Add(new CandleElement("BottomFlagLine", new Vector2(5, -6), new Vector2(30, 2), line, -22f));
                    list.Add(new CandleElement("FlagCandle1", new Vector2(-6, 7), new Vector2(5, 12), bear));
                    list.Add(new CandleElement("FlagCandle2", new Vector2(3, 2), new Vector2(5, 10), bull));
                    list.Add(new CandleElement("FlagCandle3", new Vector2(12, -3), new Vector2(5, 8), bear));
                    list.Add(new CandleElement("Breakout", new Vector2(22, 14), new Vector2(6, 14), bull));
                    break;

                // 26. Bearish Flag
                case 26:
                    list.Add(new CandleElement("Pole", new Vector2(-20, 0), new Vector2(6, 36), bear));
                    list.Add(new CandleElement("TopFlagLine", new Vector2(5, 6), new Vector2(30, 2), line, 22f));
                    list.Add(new CandleElement("BottomFlagLine", new Vector2(5, -12), new Vector2(30, 2), line, 22f));
                    list.Add(new CandleElement("FlagCandle1", new Vector2(-6, -7), new Vector2(5, 12), bull));
                    list.Add(new CandleElement("FlagCandle2", new Vector2(3, -2), new Vector2(5, 10), bear));
                    list.Add(new CandleElement("FlagCandle3", new Vector2(12, 3), new Vector2(5, 8), bull));
                    list.Add(new CandleElement("Breakdown", new Vector2(22, -14), new Vector2(6, 14), bear));
                    break;

                // 27. Ascending Triangle
                case 27:
                    list.Add(new CandleElement("ResistanceLine", new Vector2(0, 18), new Vector2(48, 2.5f), line));
                    list.Add(new CandleElement("SupportTrendline", new Vector2(0, -5), new Vector2(50, 2.5f), line, 26f));
                    list.Add(new CandleElement("Candle1", new Vector2(-16, 0), new Vector2(6, 26), bull));
                    list.Add(new CandleElement("Candle2", new Vector2(-6, 4), new Vector2(6, 20), bear));
                    list.Add(new CandleElement("Candle3", new Vector2(4, 8), new Vector2(6, 14), bull));
                    list.Add(new CandleElement("Breakout", new Vector2(16, 18), new Vector2(6, 16), bull));
                    break;

                // 28. Descending Triangle
                case 28:
                    list.Add(new CandleElement("SupportLine", new Vector2(0, -18), new Vector2(48, 2.5f), line));
                    list.Add(new CandleElement("ResistanceTrendline", new Vector2(0, 5), new Vector2(50, 2.5f), line, -26f));
                    list.Add(new CandleElement("Candle1", new Vector2(-16, 0), new Vector2(6, 26), bear));
                    list.Add(new CandleElement("Candle2", new Vector2(-6, -4), new Vector2(6, 20), bull));
                    list.Add(new CandleElement("Candle3", new Vector2(4, -8), new Vector2(6, 14), bear));
                    list.Add(new CandleElement("Breakdown", new Vector2(16, -18), new Vector2(6, 16), bear));
                    break;

                // 29. Double Top
                case 29:
                    list.Add(new CandleElement("ResistanceLine", new Vector2(0, 18), new Vector2(48, 2), line));
                    list.Add(new CandleElement("Neckline", new Vector2(0, -12), new Vector2(44, 1.5f), whiteLine));
                    list.Add(new CandleElement("Peak1_Green", new Vector2(-16, 4), new Vector2(6, 24), bull));
                    list.Add(new CandleElement("Peak1_Red", new Vector2(-8, -2), new Vector2(6, 18), bear));
                    list.Add(new CandleElement("Peak2_Green", new Vector2(8, 4), new Vector2(6, 24), bull));
                    list.Add(new CandleElement("Peak2_Red", new Vector2(16, -12), new Vector2(6, 22), bear));
                    break;

                // 30. Double Bottom
                case 30:
                    list.Add(new CandleElement("SupportLine", new Vector2(0, -18), new Vector2(48, 2), line));
                    list.Add(new CandleElement("Neckline", new Vector2(0, 12), new Vector2(44, 1.5f), whiteLine));
                    list.Add(new CandleElement("Trough1_Red", new Vector2(-16, -4), new Vector2(6, 24), bear));
                    list.Add(new CandleElement("Trough1_Green", new Vector2(-8, 2), new Vector2(6, 18), bull));
                    list.Add(new CandleElement("Trough2_Red", new Vector2(8, -4), new Vector2(6, 24), bear));
                    list.Add(new CandleElement("Trough2_Green", new Vector2(16, 12), new Vector2(6, 22), bull));
                    break;
            }

            return list;
        }
    }
}
