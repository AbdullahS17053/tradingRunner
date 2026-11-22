using System;
using System.Collections.Generic;
using UnityEngine;

namespace RunnerForex.Castle
{
    public enum TradeAction
    {
        BUY,
        SELL,
        WAIT
    }

    [Serializable]
    public struct CandleData
    {
        public float open;
        public float high;
        public float low;
        public float close;

        public CandleData(float open, float high, float low, float close)
        {
            this.open = open;
            this.high = high;
            this.low = low;
            this.close = close;
        }

        public bool IsBullish => close >= open;
    }

    [Serializable]
    public class CastleChallenge
    {
        public int id;
        public int patternId = 1;
        public string patternName = "Doji";
        public string moveName = "Meditate";
        public PatternAnimationType animationType = PatternAnimationType.Meditate;
        public string pair;
        public string timeframe;
        public string scenario;
        public TradeAction answer;
        public string explanation;

        public float supportLevel;
        public float resistanceLevel;
        public float sweepLevel;
        public List<CandleData> candles = new List<CandleData>();
        public List<float> rsiData = new List<float>();

        public bool isFormingCandle = false; // Challenge 15
        public bool showNewsBanner = false;  // Challenge 24
        public bool showRiskWarning = false; // Challenge 27
    }
}
