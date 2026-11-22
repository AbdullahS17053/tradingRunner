using System;
using System.Collections.Generic;
using UnityEngine;

namespace RunnerForex.Castle
{
    public enum PatternAnimationType
    {
        Meditate = 1,
        GroundSlam = 2,
        AirDrop = 3,
        EnergyShot = 4,
        RisingDash = 5,
        PowerDash = 6,
        ShadowStrike = 7,
        RebirthHeal = 8,
        DarkPulse = 9,
        SuperSprint = 10,
        SpinMove = 11,
        QuickStep = 12,
        Backstep = 13,
        Breakthrough = 14,
        VisionBlock = 15,
        DoubleStrike = 16,
        DualJump = 17,
        TripleBoost = 18,
        TripleSlash = 19,
        BlinkTeleport = 20,
        AirCombo = 21,
        ForwardRush = 22,
        Shield = 23,
        Explosion = 24,
        SpeedSurge = 25,
        SlideAttack = 26,
        ClimbBoost = 27,
        SlideDrop = 28,
        CloneStrike = 29,
        ReviveWave = 30
    }

    public enum PatternBias
    {
        Bullish,
        Bearish,
        Neutral
    }

    public enum PatternCategory
    {
        Reversal,
        Continuation,
        Indecision,
        ChartPattern
    }

    [Serializable]
    public class ForexPatternInfo
    {
        public int id;
        public string patternName;
        public string moveName;
        public string moveDescription;
        public PatternBias bias;
        public PatternCategory category;
        public TradeAction signal;
        public string tradingRule;
        public PatternAnimationType animationType;

        public string DisplayTitle => $"{id}. {patternName}";
        public string SignalText => signal == TradeAction.BUY ? "BUY (BULLISH)" : (signal == TradeAction.SELL ? "SELL (BEARISH)" : "WAIT (NEUTRAL)");

        public Color BiasColor
        {
            get
            {
                switch (bias)
                {
                    case PatternBias.Bullish: return new Color(0.1f, 1f, 0.4f); // Vibrant Neon Green
                    case PatternBias.Bearish: return new Color(1f, 0.2f, 0.35f); // Vibrant Neon Red
                    default: return new Color(0.2f, 0.85f, 1f); // Vibrant Neon Cyan
                }
            }
        }

        public Sprite GetSprite()
        {
            Sprite s = Resources.Load<Sprite>($"ForexPatterns/Pattern_{id:02d}");
            if (s == null)
            {
                s = Resources.Load<Sprite>($"Pattern_{id:02d}");
            }
            return s;
        }
    }

    public static class ForexPatternDatabase
    {
        private static List<ForexPatternInfo> _patterns;

        public static List<ForexPatternInfo> GetAllPatterns()
        {
            if (_patterns == null)
            {
                Initialize();
            }
            return _patterns;
        }

        public static ForexPatternInfo GetPattern(int id)
        {
            var all = GetAllPatterns();
            int index = Mathf.Clamp(id - 1, 0, all.Count - 1);
            return all[index];
        }

        private static void Initialize()
        {
            _patterns = new List<ForexPatternInfo>
            {
                // 1. Doji
                new ForexPatternInfo
                {
                    id = 1,
                    patternName = "Doji",
                    moveName = "Meditate",
                    moveDescription = "Hover in mid-air with deep focus, channeling a protective aura.",
                    bias = PatternBias.Neutral,
                    category = PatternCategory.Indecision,
                    signal = TradeAction.WAIT,
                    tradingRule = "Open and close prices are nearly equal. Indicates market equilibrium and indecision. Wait for next candle breakout confirmation.",
                    animationType = PatternAnimationType.Meditate
                },
                // 2. Hammer
                new ForexPatternInfo
                {
                    id = 2,
                    patternName = "Hammer",
                    moveName = "Ground Slam",
                    moveDescription = "Leap high and slam downward with explosive kinetic shockwaves.",
                    bias = PatternBias.Bullish,
                    category = PatternCategory.Reversal,
                    signal = TradeAction.BUY,
                    tradingRule = "Small body at the top with a long lower shadow (2x body) at support. Buyers reject lower prices. Signal: BUY.",
                    animationType = PatternAnimationType.GroundSlam
                },
                // 3. Hanging Man
                new ForexPatternInfo
                {
                    id = 3,
                    patternName = "Hanging Man",
                    moveName = "Air Drop",
                    moveDescription = "Dive vertically from the stratosphere with blazing velocity.",
                    bias = PatternBias.Bearish,
                    category = PatternCategory.Reversal,
                    signal = TradeAction.SELL,
                    tradingRule = "Formed at the top of an uptrend with a long lower shadow. Indicates heavy selling pressure entering the market. Signal: SELL.",
                    animationType = PatternAnimationType.AirDrop
                },
                // 4. Shooting Star
                new ForexPatternInfo
                {
                    id = 4,
                    patternName = "Shooting Star",
                    moveName = "Energy Shot",
                    moveDescription = "Channel plasma energy and unleash a high-velocity forward blaster beam.",
                    bias = PatternBias.Bearish,
                    category = PatternCategory.Reversal,
                    signal = TradeAction.SELL,
                    tradingRule = "Small body near low with a long upper wick at resistance. Buyers tried to push higher but were rejected by bears. Signal: SELL.",
                    animationType = PatternAnimationType.EnergyShot
                },
                // 5. Inverted Hammer
                new ForexPatternInfo
                {
                    id = 5,
                    patternName = "Inverted Hammer",
                    moveName = "Rising Dash",
                    moveDescription = "Surge upward at a sharp angle leaving vibrant motion trails.",
                    bias = PatternBias.Bullish,
                    category = PatternCategory.Reversal,
                    signal = TradeAction.BUY,
                    tradingRule = "Forms at a downtrend low with a long upper wick. Indicates buying pressure is testing resistance for a bullish reversal. Signal: BUY.",
                    animationType = PatternAnimationType.RisingDash
                },
                // 6. Bullish Engulfing
                new ForexPatternInfo
                {
                    id = 6,
                    patternName = "Bullish Engulfing",
                    moveName = "Power Dash",
                    moveDescription = "Charge forward with overwhelming power, crushing obstacles in your path.",
                    bias = PatternBias.Bullish,
                    category = PatternCategory.Reversal,
                    signal = TradeAction.BUY,
                    tradingRule = "Large green candle completely engulfs the prior smaller red candle body at support. Strong momentum shift. Signal: BUY.",
                    animationType = PatternAnimationType.PowerDash
                },
                // 7. Bearish Engulfing
                new ForexPatternInfo
                {
                    id = 7,
                    patternName = "Bearish Engulfing",
                    moveName = "Shadow Strike",
                    moveDescription = "Phase into a shadow silhouette, blitz through space, and strike from behind.",
                    bias = PatternBias.Bearish,
                    category = PatternCategory.Reversal,
                    signal = TradeAction.SELL,
                    tradingRule = "Large red candle completely engulfs the prior green candle at resistance. Bears seize control. Signal: SELL.",
                    animationType = PatternAnimationType.ShadowStrike
                },
                // 8. Morning Star
                new ForexPatternInfo
                {
                    id = 8,
                    patternName = "Morning Star",
                    moveName = "Rebirth Heal",
                    moveDescription = "Summon a radiant spiral healing aura that revitalizes maximum vitality.",
                    bias = PatternBias.Bullish,
                    category = PatternCategory.Reversal,
                    signal = TradeAction.BUY,
                    tradingRule = "Three-candle pattern: large bearish, small star/doji, then large bullish candle closing above midpoint. Major bottom reversal. Signal: BUY.",
                    animationType = PatternAnimationType.RebirthHeal
                },
                // 9. Evening Star
                new ForexPatternInfo
                {
                    id = 9,
                    patternName = "Evening Star",
                    moveName = "Dark Pulse",
                    moveDescription = "Release an expanding dark gravitational shockwave that disrupts targets.",
                    bias = PatternBias.Bearish,
                    category = PatternCategory.Reversal,
                    signal = TradeAction.SELL,
                    tradingRule = "Three-candle pattern at top: large bullish, small star/doji, then large bearish candle closing deep into first candle. Signal: SELL.",
                    animationType = PatternAnimationType.DarkPulse
                },
                // 10. Marubozu
                new ForexPatternInfo
                {
                    id = 10,
                    patternName = "Marubozu",
                    moveName = "Super Sprint",
                    moveDescription = "Ignite hyper-thrusters and sprint with blazing afterimage streaks.",
                    bias = PatternBias.Bullish,
                    category = PatternCategory.Continuation,
                    signal = TradeAction.BUY,
                    tradingRule = "Full solid candle body with virtually no wicks. Demonstrates pure, unyielding directional dominance from open to close. Signal: BUY.",
                    animationType = PatternAnimationType.SuperSprint
                },
                // 11. Spinning Top
                new ForexPatternInfo
                {
                    id = 11,
                    patternName = "Spinning Top",
                    moveName = "Spin Move",
                    moveDescription = "Execute a rapid 360-degree whirlwind spin with glowing cyclone rings.",
                    bias = PatternBias.Neutral,
                    category = PatternCategory.Indecision,
                    signal = TradeAction.WAIT,
                    tradingRule = "Small real body centered between equal upper and lower shadows. Demonstrates hesitation; wait for a confirmed trend direction.",
                    animationType = PatternAnimationType.SpinMove
                },
                // 12. Bullish Harami
                new ForexPatternInfo
                {
                    id = 12,
                    patternName = "Bullish Harami",
                    moveName = "Quick Step",
                    moveDescription = "Perform lightning-fast stutter-steps to evade oncoming attacks with precision.",
                    bias = PatternBias.Bullish,
                    category = PatternCategory.Reversal,
                    signal = TradeAction.BUY,
                    tradingRule = "Small green candle completely contained within the preceding large red candle. Selling momentum halts, signaling upward turn. Signal: BUY.",
                    animationType = PatternAnimationType.QuickStep
                },
                // 13. Bearish Harami
                new ForexPatternInfo
                {
                    id = 13,
                    patternName = "Bearish Harami",
                    moveName = "Backstep",
                    moveDescription = "Swift evasive hop backward to maintain spacing and prepare retaliation.",
                    bias = PatternBias.Bearish,
                    category = PatternCategory.Reversal,
                    signal = TradeAction.SELL,
                    tradingRule = "Small red candle completely contained within the preceding large green candle at resistance. Buying exhausted. Signal: SELL.",
                    animationType = PatternAnimationType.Backstep
                },
                // 14. Piercing Line
                new ForexPatternInfo
                {
                    id = 14,
                    patternName = "Piercing Line",
                    moveName = "Breakthrough",
                    moveDescription = "Shatter solid energy barriers with an explosive forward impact.",
                    bias = PatternBias.Bullish,
                    category = PatternCategory.Reversal,
                    signal = TradeAction.BUY,
                    tradingRule = "Bullish candle opens below prior low and surges to close above 50% of the previous bearish candle's body. Strong buy reversal. Signal: BUY.",
                    animationType = PatternAnimationType.Breakthrough
                },
                // 15. Dark Cloud Cover
                new ForexPatternInfo
                {
                    id = 15,
                    patternName = "Dark Cloud Cover",
                    moveName = "Vision Block",
                    moveDescription = "Conjure a veil of dark cosmic mist that blinds and confuses enemies.",
                    bias = PatternBias.Bearish,
                    category = PatternCategory.Reversal,
                    signal = TradeAction.SELL,
                    tradingRule = "Bearish candle opens above prior high and plunges to close below 50% of the prior bullish candle. Dark cloud forms at top. Signal: SELL.",
                    animationType = PatternAnimationType.VisionBlock
                },
                // 16. Tweezer Top
                new ForexPatternInfo
                {
                    id = 16,
                    patternName = "Tweezer Top",
                    moveName = "Double Strike",
                    moveDescription = "Deliver a ferocious two-hit dual-blade cross slash with neon trails.",
                    bias = PatternBias.Bearish,
                    category = PatternCategory.Reversal,
                    signal = TradeAction.SELL,
                    tradingRule = "Two adjacent candles hitting the exact same high price with their upper wicks. Dual rejection at resistance. Signal: SELL.",
                    animationType = PatternAnimationType.DoubleStrike
                },
                // 17. Tweezer Bottom
                new ForexPatternInfo
                {
                    id = 17,
                    patternName = "Tweezer Bottom",
                    moveName = "Dual Jump",
                    moveDescription = "Acrobatic double-jump propelling into the air with twin apex pulses.",
                    bias = PatternBias.Bullish,
                    category = PatternCategory.Reversal,
                    signal = TradeAction.BUY,
                    tradingRule = "Two adjacent candles hitting the exact same low price with their lower wicks. Rock-solid double support established. Signal: BUY.",
                    animationType = PatternAnimationType.DualJump
                },
                // 18. Three White Soldiers
                new ForexPatternInfo
                {
                    id = 18,
                    patternName = "Three White Soldiers",
                    moveName = "Triple Boost",
                    moveDescription = "Trigger a 3-stage rocket booster sequence accelerating to supersonic speed.",
                    bias = PatternBias.Bullish,
                    category = PatternCategory.Continuation,
                    signal = TradeAction.BUY,
                    tradingRule = "Three consecutive long green candles closing progressively higher with small wicks. Unstoppable bullish breakout. Signal: BUY.",
                    animationType = PatternAnimationType.TripleBoost
                },
                // 19. Three Black Crows
                new ForexPatternInfo
                {
                    id = 19,
                    patternName = "Three Black Crows",
                    moveName = "Triple Slash",
                    moveDescription = "Unleash three rapid slashing arcs of crimson energy carving the air.",
                    bias = PatternBias.Bearish,
                    category = PatternCategory.Continuation,
                    signal = TradeAction.SELL,
                    tradingRule = "Three consecutive long red candles closing progressively lower with short wicks. Dominant bearish breakdown. Signal: SELL.",
                    animationType = PatternAnimationType.TripleSlash
                },
                // 20. Tri-Star
                new ForexPatternInfo
                {
                    id = 20,
                    patternName = "Tri-Star",
                    moveName = "Blink Teleport",
                    moveDescription = "Dematerialize in a flash of cyan light and instantly reappear ahead.",
                    bias = PatternBias.Bullish,
                    category = PatternCategory.Reversal,
                    signal = TradeAction.BUY,
                    tradingRule = "Extremely rare formation of three consecutive Doji candles, with middle Doji gapping lower. Indicates ultimate trend climax reversal. Signal: BUY.",
                    animationType = PatternAnimationType.BlinkTeleport
                },
                // 21. Falling Three
                new ForexPatternInfo
                {
                    id = 21,
                    patternName = "Falling Three",
                    moveName = "Air Combo",
                    moveDescription = "Launch an opponent into the air and execute a high-speed juggling combo.",
                    bias = PatternBias.Bearish,
                    category = PatternCategory.Continuation,
                    signal = TradeAction.SELL,
                    tradingRule = "Long red candle followed by three small green pause candles inside its range, then a massive red candle breaking lower. Bearish continuation. Signal: SELL.",
                    animationType = PatternAnimationType.AirCombo
                },
                // 22. Rising Three
                new ForexPatternInfo
                {
                    id = 22,
                    patternName = "Rising Three",
                    moveName = "Forward Rush",
                    moveDescription = "Aggressive forward bull-rush charging straight through opposition lines.",
                    bias = PatternBias.Bullish,
                    category = PatternCategory.Continuation,
                    signal = TradeAction.BUY,
                    tradingRule = "Long green candle followed by three small red consolidation candles, then a powerful green candle surging to new highs. Bullish continuation. Signal: BUY.",
                    animationType = PatternAnimationType.ForwardRush
                },
                // 23. Inside Bar
                new ForexPatternInfo
                {
                    id = 23,
                    patternName = "Inside Bar",
                    moveName = "Shield",
                    moveDescription = "Deploy a rotating hexagonal energy dome that absorbs all incoming impacts.",
                    bias = PatternBias.Neutral,
                    category = PatternCategory.Indecision,
                    signal = TradeAction.WAIT,
                    tradingRule = "Current candle's high and low are completely contained within the prior mother bar. Energy coil before breakout. Wait for breakout direction.",
                    animationType = PatternAnimationType.Shield
                },
                // 24. Outside Bar
                new ForexPatternInfo
                {
                    id = 24,
                    patternName = "Outside Bar",
                    moveName = "Explosion",
                    moveDescription = "Detonate a shockwave blast radiating outward in all directions.",
                    bias = PatternBias.Bullish,
                    category = PatternCategory.Reversal,
                    signal = TradeAction.BUY,
                    tradingRule = "Candle's high is higher and low is lower than the prior candle. High volatility expansion. If closing bullish at support, signal: BUY.",
                    animationType = PatternAnimationType.Explosion
                },
                // 25. Bullish Flag
                new ForexPatternInfo
                {
                    id = 25,
                    patternName = "Bullish Flag",
                    moveName = "Speed Surge",
                    moveDescription = "Instantaneous warp surge blasting forward with blinding neon light.",
                    bias = PatternBias.Bullish,
                    category = PatternCategory.Continuation,
                    signal = TradeAction.BUY,
                    tradingRule = "Strong vertical flagpole rally followed by downward sloping consolidation channel. Breakout above flag resistance signals massive continuation. Signal: BUY.",
                    animationType = PatternAnimationType.SpeedSurge
                },
                // 26. Bearish Flag
                new ForexPatternInfo
                {
                    id = 26,
                    patternName = "Bearish Flag",
                    moveName = "Slide Attack",
                    moveDescription = "Low sweeping slide kick across the ground showering sparks along the road.",
                    bias = PatternBias.Bearish,
                    category = PatternCategory.Continuation,
                    signal = TradeAction.SELL,
                    tradingRule = "Sharp downward flagpole drop followed by gentle upward sloping consolidation channel. Breakdown below flag support signals continued plunge. Signal: SELL.",
                    animationType = PatternAnimationType.SlideAttack
                },
                // 27. Ascending Triangle
                new ForexPatternInfo
                {
                    id = 27,
                    patternName = "Ascending Triangle",
                    moveName = "Climb Boost",
                    moveDescription = "Vertical parkour vault ascending upward with effortless acrobatic lift.",
                    bias = PatternBias.Bullish,
                    category = PatternCategory.ChartPattern,
                    signal = TradeAction.BUY,
                    tradingRule = "Horizontal resistance ceiling paired with rising higher lows trendline. Pressure builds until an upward breakout explosion occurs. Signal: BUY.",
                    animationType = PatternAnimationType.ClimbBoost
                },
                // 28. Descending Triangle
                new ForexPatternInfo
                {
                    id = 28,
                    patternName = "Descending Triangle",
                    moveName = "Slide Drop",
                    moveDescription = "Steep downward slide descending into a rapid ground drop.",
                    bias = PatternBias.Bearish,
                    category = PatternCategory.ChartPattern,
                    signal = TradeAction.SELL,
                    tradingRule = "Horizontal support floor paired with falling lower highs trendline. Bears relentlessly press down until support collapses. Signal: SELL.",
                    animationType = PatternAnimationType.SlideDrop
                },
                // 29. Double Top
                new ForexPatternInfo
                {
                    id = 29,
                    patternName = "Double Top",
                    moveName = "Clone Strike",
                    moveDescription = "Holographic twin clones split to left and right and execute a coordinated strike.",
                    bias = PatternBias.Bearish,
                    category = PatternCategory.ChartPattern,
                    signal = TradeAction.SELL,
                    tradingRule = "Classic 'M' shaped chart pattern. Price hits resistance twice and fails to break out. Breakdown below neckline triggers major trend reversal. Signal: SELL.",
                    animationType = PatternAnimationType.CloneStrike
                },
                // 30. Double Bottom
                new ForexPatternInfo
                {
                    id = 30,
                    patternName = "Double Bottom",
                    moveName = "Revive Wave",
                    moveDescription = "Emit a radiant golden-cyan resurrection shockwave expanding across the field.",
                    bias = PatternBias.Bullish,
                    category = PatternCategory.ChartPattern,
                    signal = TradeAction.BUY,
                    tradingRule = "Classic 'W' shaped chart pattern. Price hits support twice, confirming buyers defend the level. Breakout above neckline triggers major rally. Signal: BUY.",
                    animationType = PatternAnimationType.ReviveWave
                }
            };
        }
    }
}
