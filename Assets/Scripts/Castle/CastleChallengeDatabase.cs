using System.Collections.Generic;
using UnityEngine;

namespace RunnerForex.Castle
{
    public static class CastleChallengeDatabase
    {
        private static List<CastleChallenge> _challenges;

        public static List<CastleChallenge> GetAllChallenges()
        {
            if (_challenges == null)
            {
                Initialize();
            }
            return _challenges;
        }

        private static void Initialize()
        {
            _challenges = new List<CastleChallenge>();

            _challenges.Add(CreateChallenge(1, "XAUUSD", "M15", "Price sweeps below the yellow support line. A bullish candle closes back above support. RSI rises from 27.", TradeAction.BUY, "The completed lower sweep and bullish close support a possible upward reversal.", 2000.0f, 2050.0f, 1990.0f, 27.0f, false, false, false));
            _challenges.Add(CreateChallenge(2, "EURUSD", "H1", "Price sweeps above resistance and closes back inside the range. RSI declines from 76.", TradeAction.SELL, "The upper sweep and bearish close support a possible downward reversal.", 1.08f, 1.095f, 1.1f, 76.0f, false, false, false));
            _challenges.Add(CreateChallenge(3, "GBPUSD", "M15", "RSI reaches 78, but price keeps rising without a completed liquidity sweep or bearish close.", TradeAction.WAIT, "Overbought RSI alone does not confirm a reversal.", 1.26f, 1.28f, 1.285f, 78.0f, false, false, false));
            _challenges.Add(CreateChallenge(4, "AUDUSD", "H1", "Price sweeps below support, closes back inside the range, and RSI recovers from 25.", TradeAction.BUY, "The completed lower sweep supports a possible bullish reversal.", 0.64f, 0.66f, 0.635f, 25.0f, false, false, false));
            _challenges.Add(CreateChallenge(5, "USDJPY", "M15", "Price sweeps above resistance, forms a long upper wick, closes below the yellow line, and RSI declines from 74.", TradeAction.SELL, "The upper sweep and bearish rejection support a possible downward reversal.", 150.0f, 155.0f, 156.0f, 74.0f, false, false, false));
            _challenges.Add(CreateChallenge(6, "XAUUSD", "H1", "Price approaches a previous low. RSI is 32, but no liquidity sweep has occurred.", TradeAction.WAIT, "The required setup is incomplete.", 2010.0f, 2060.0f, 2005.0f, 32.0f, false, false, false));
            _challenges.Add(CreateChallenge(7, "EURGBP", "M15", "Price sweeps below support, closes back above the yellow line, and RSI rises from 28.", TradeAction.BUY, "The lower sweep and bullish recovery support a potential reversal.", 0.85f, 0.865f, 0.846f, 28.0f, false, false, false));
            _challenges.Add(CreateChallenge(8, "USDCAD", "H1", "Price sweeps above a previous high, closes below resistance, and RSI falls from 73.", TradeAction.SELL, "The completed upper sweep supports a potential bearish reversal.", 1.34f, 1.36f, 1.365f, 73.0f, false, false, false));
            _challenges.Add(CreateChallenge(9, "NZDUSD", "M15", "Price moves sideways, RSI is 51, and no meaningful liquidity sweep occurs.", TradeAction.WAIT, "There is insufficient confirmation.", 0.59f, 0.61f, 0.595f, 51.0f, false, false, false));
            _challenges.Add(CreateChallenge(10, "XAUUSD", "H1", "Price sweeps below a previous low, closes above support, and RSI recovers from 29.", TradeAction.BUY, "The completed lower sweep supports a potential bullish reversal.", 2015.0f, 2070.0f, 2008.0f, 29.0f, false, false, false));
            _challenges.Add(CreateChallenge(11, "EURUSD", "M15", "Price sweeps above resistance, closes back below the yellow line, and RSI falls from 75.", TradeAction.SELL, "The completed upper sweep supports a possible bearish reversal.", 1.082f, 1.096f, 1.101f, 75.0f, false, false, false));
            _challenges.Add(CreateChallenge(12, "GBPJPY", "H1", "RSI is 24 and price continues falling below support without returning inside the range.", TradeAction.WAIT, "Oversold conditions can persist during a strong downtrend.", 188.0f, 193.0f, 187.0f, 24.0f, false, false, false));
            _challenges.Add(CreateChallenge(13, "AUDCAD", "M15", "Price sweeps below support, forms a long lower wick, closes above the yellow line, and RSI rises from 26.", TradeAction.BUY, "The confirmed lower sweep supports a potential bullish reversal.", 0.89f, 0.91f, 0.885f, 26.0f, false, false, false));
            _challenges.Add(CreateChallenge(14, "XAUUSD", "H1", "RSI reaches 79. Price sweeps above a previous high and closes below resistance.", TradeAction.SELL, "The upper sweep and bearish close support a potential downward reversal.", 2020.0f, 2080.0f, 2090.0f, 79.0f, false, false, false));
            _challenges.Add(CreateChallenge(15, "EURJPY", "M15", "Price moves above resistance, but the candle remains open. RSI is 72.", TradeAction.WAIT, "The strategy requires a completed candle for confirmation.", 160.0f, 164.0f, 164.8f, 72.0f, true, false, false));
            _challenges.Add(CreateChallenge(16, "GBPUSD", "H1", "Price sweeps below a previous low, closes above the yellow support line, and RSI recovers from 27.", TradeAction.BUY, "The completed lower sweep supports a possible upward reversal.", 1.255f, 1.275f, 1.25f, 27.0f, false, false, false));
            _challenges.Add(CreateChallenge(17, "USDCAD", "M15", "Price sweeps above resistance, closes back inside the range, and RSI falls from 77.", TradeAction.SELL, "The upper sweep and bearish return support a potential downward reversal.", 1.345f, 1.362f, 1.368f, 77.0f, false, false, false));
            _challenges.Add(CreateChallenge(18, "XAUUSD", "M15", "RSI is 28, but price only touches support without sweeping below it.", TradeAction.WAIT, "The required liquidity sweep has not occurred.", 2025.0f, 2075.0f, 2025.0f, 28.0f, false, false, false));
            _challenges.Add(CreateChallenge(19, "NZDUSD", "H1", "Price sweeps below support, closes back above the yellow line, and RSI rises from 25.", TradeAction.BUY, "The lower sweep and bullish confirmation support a potential reversal.", 0.588f, 0.608f, 0.583f, 25.0f, false, false, false));
            _challenges.Add(CreateChallenge(20, "EURAUD", "M15", "Price sweeps above resistance, rejects the breakout, closes inside the range, and RSI falls from 74.", TradeAction.SELL, "The completed upper sweep supports a possible bearish reversal.", 1.62f, 1.65f, 1.658f, 74.0f, false, false, false));
            _challenges.Add(CreateChallenge(21, "XAUUSD", "H1", "Price sweeps above resistance and closes below it, but the next candle breaks strongly above the swept high and closes there.", TradeAction.WAIT, "The original bearish reversal idea has been invalidated and requires reassessment.", 2030.0f, 2085.0f, 2095.0f, 65.0f, false, false, false));
            _challenges.Add(CreateChallenge(22, "EURUSD", "M15", "Price sweeps below a previous low, closes back inside the range, and RSI rises from 23 to 33.", TradeAction.BUY, "The lower sweep and bullish confirmation support a possible upward reversal.", 1.078f, 1.092f, 1.073f, 28.0f, false, false, false));
            _challenges.Add(CreateChallenge(23, "GBPJPY", "H1", "Price sweeps above resistance, forms a bearish rejection candle closing back inside the range, and RSI falls from 81.", TradeAction.SELL, "The upper sweep and bearish rejection support a potential downward reversal.", 189.0f, 194.0f, 195.2f, 81.0f, false, false, false));
            _challenges.Add(CreateChallenge(24, "XAUUSD", "M15", "Price sweeps below support, but major economic news approaches, spreads widen, and stable bullish confirmation is absent.", TradeAction.WAIT, "Execution risk is elevated and the setup is incomplete.", 2035.0f, 2090.0f, 2028.0f, 31.0f, false, true, false));
            _challenges.Add(CreateChallenge(25, "AUDUSD", "H1", "Price sweeps below support, closes back inside the range, RSI recovers from 29, and the next candle holds inside the range.", TradeAction.BUY, "The confirmed lower sweep supports a possible bullish reversal.", 0.638f, 0.658f, 0.632f, 29.0f, false, false, false));
            _challenges.Add(CreateChallenge(26, "EURGBP", "M15", "Price sweeps above resistance, closes back below the yellow line, forms a second bearish candle, and RSI declines from 78.", TradeAction.SELL, "The upper sweep and bearish follow-through support a potential downward reversal.", 0.852f, 0.868f, 0.873f, 78.0f, false, false, false));
            _challenges.Add(CreateChallenge(27, "XAUUSD", "H1", "Price sweeps below support and closes back inside the range, but the proposed stop-loss would require excessive account risk.", TradeAction.WAIT, "Risk management matters even when a technical setup appears promising.", 2040.0f, 2095.0f, 2032.0f, 27.0f, false, false, true));
            _challenges.Add(CreateChallenge(28, "USDCAD", "M15", "Price sweeps below support, forms a long lower wick, closes bullish, and RSI rises from 24 to 35.", TradeAction.BUY, "The completed lower sweep supports a possible bullish reversal.", 1.342f, 1.359f, 1.336f, 30.0f, false, false, false));
            _challenges.Add(CreateChallenge(29, "EURJPY", "H1", "Price sweeps above resistance, closes back inside the range, RSI falls from 77, and the following candle remains bearish.", TradeAction.SELL, "The upper sweep and bearish continuation support a potential downward reversal.", 161.0f, 165.0f, 165.9f, 77.0f, false, false, false));
            _challenges.Add(CreateChallenge(30, "XAUUSD", "M15", "Price breaks above resistance and closes above it. RSI is 76, but price has not returned inside the range.", TradeAction.WAIT, "A breakout remaining outside the range is not a confirmed liquidity-sweep reversal.", 2045.0f, 2100.0f, 2115.0f, 76.0f, false, false, false));
        }

        private static CastleChallenge CreateChallenge(int id, string pair, string tf, string scenario, TradeAction ans, string exp, float sup, float res, float sweep, float baseRsi, bool isForming, bool news, bool risk)
        {
            CastleChallenge c = new CastleChallenge
            {
                id = id,
                patternId = id,
                pair = pair,
                timeframe = tf,
                scenario = scenario,
                answer = ans,
                explanation = exp,
                supportLevel = sup,
                resistanceLevel = res,
                sweepLevel = sweep,
                isFormingCandle = isForming,
                showNewsBanner = news,
                showRiskWarning = risk
            };

            ForexPatternInfo pattern = ForexPatternDatabase.GetPattern(id);
            if (pattern != null)
            {
                c.patternName = pattern.patternName;
                c.moveName = pattern.moveName;
                c.animationType = pattern.animationType;
            }

            // Procedurally generate 12 candles leading up to the final pattern
            float range = res - sup;
            float mid = sup + range * 0.5f;
            float step = range / 12f;

            for (int i = 0; i < 11; i++)
            {
                float open = mid + Mathf.Sin(i * 0.8f) * (range * 0.25f);
                float close = mid + Mathf.Sin((i + 1) * 0.8f) * (range * 0.25f);
                float high = Mathf.Max(open, close) + step * 0.4f;
                float low = Mathf.Min(open, close) - step * 0.4f;
                c.candles.Add(new CandleData(open, high, low, close));
                c.rsiData.Add(Mathf.Clamp(50f + Mathf.Sin(i * 0.6f) * 15f, 30f, 70f));
            }

            // Final 12th candle embodies the specific trigger
            if (ans == TradeAction.BUY)
            {
                float finalOpen = sup + step * 0.2f;
                float finalLow = sweep; // Swept below support
                float finalClose = sup + step * 0.8f; // Closed back inside
                float finalHigh = finalClose + step * 0.2f;
                c.candles.Add(new CandleData(finalOpen, finalHigh, finalLow, finalClose));
                c.rsiData.Add(baseRsi);
            }
            else if (ans == TradeAction.SELL)
            {
                float finalOpen = res - step * 0.2f;
                float finalHigh = sweep; // Swept above resistance
                float finalClose = res - step * 0.8f; // Closed back inside
                float finalLow = finalClose - step * 0.2f;
                c.candles.Add(new CandleData(finalOpen, finalHigh, finalLow, finalClose));
                c.rsiData.Add(baseRsi);
            }
            else
            {
                // WAIT / Incomplete / Continuing
                float finalOpen = mid;
                float finalClose = (id == 15 || id == 30) ? res + step * 0.5f : mid + step * 0.2f;
                float finalHigh = Mathf.Max(finalOpen, finalClose) + step * 0.3f;
                float finalLow = Mathf.Min(finalOpen, finalClose) - step * 0.3f;
                c.candles.Add(new CandleData(finalOpen, finalHigh, finalLow, finalClose));
                c.rsiData.Add(baseRsi);
            }

            return c;
        }
    }
}
