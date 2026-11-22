using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace RunnerForex.Castle
{
    /// <summary>
    /// Executes the 30 distinct movement animations for candlestick patterns.
    /// Provides continuous subtle ambient animation loops and high-impact burst animations on click or quiz answer.
    /// </summary>
    public class ForexPatternMovementAnimator : MonoBehaviour
    {
        [Header("Animation Target & Configuration")]
        public PatternAnimationType animationType = PatternAnimationType.Meditate;
        public RectTransform cardTransform;
        public Image cardImage;

        [Header("Visual FX Elements (Procedurally Created if null)")]
        public Image auraGlowImage;
        public Image shockwaveRing;
        public Image projectileImage;
        public Image ghostCloneLeft;
        public Image ghostCloneRight;

        private Vector3 initialPosition;
        private Vector3 initialScale;
        private Quaternion initialRotation;
        private Coroutine activeBurstRoutine;

        private void Awake()
        {
            if (cardTransform == null) cardTransform = GetComponent<RectTransform>();
            if (cardImage == null) cardImage = GetComponent<Image>();

            if (cardTransform != null)
            {
                initialPosition = cardTransform.localPosition;
                initialScale = cardTransform.localScale;
                initialRotation = cardTransform.localRotation;
            }

            EnsureVfxElements();
        }

        private void OnEnable()
        {
            ResetVisualState();
        }

        private void OnDisable()
        {
            if (activeBurstRoutine != null)
            {
                StopCoroutine(activeBurstRoutine);
                activeBurstRoutine = null;
            }
            ResetVisualState();
        }

        private void ResetVisualState()
        {
            if (cardTransform != null)
            {
                cardTransform.localPosition = initialPosition;
                cardTransform.localScale = initialScale;
                cardTransform.localRotation = initialRotation;
            }
            if (auraGlowImage != null) auraGlowImage.gameObject.SetActive(false);
            if (shockwaveRing != null) shockwaveRing.gameObject.SetActive(false);
            if (projectileImage != null) projectileImage.gameObject.SetActive(false);
            if (ghostCloneLeft != null) ghostCloneLeft.gameObject.SetActive(false);
            if (ghostCloneRight != null) ghostCloneRight.gameObject.SetActive(false);
            if (cardImage != null) cardImage.color = Color.white;
        }

        private void Update()
        {
            if (activeBurstRoutine != null) return; // Burst in progress
            UpdateAmbientIdleAnimation();
        }

        /// <summary>
        /// Plays ambient looping animation specific to each of the 30 moves
        /// </summary>
        private void UpdateAmbientIdleAnimation()
        {
            if (cardTransform == null) return;
            float t = Time.time;

            switch (animationType)
            {
                // 1. Meditate: Gentle floating hover bob & breathing scale
                case PatternAnimationType.Meditate:
                    float hover = Mathf.Sin(t * 2.2f) * 4.5f;
                    cardTransform.localPosition = initialPosition + new Vector3(0f, hover, 0f);
                    float breath = 1f + Mathf.Sin(t * 1.8f) * 0.025f;
                    cardTransform.localScale = initialScale * breath;
                    break;

                // 2. Ground Slam: Subtle energetic trembling
                case PatternAnimationType.GroundSlam:
                    float slamPulse = Mathf.Sin(t * 3.5f);
                    cardTransform.localPosition = initialPosition + new Vector3(0f, Mathf.Max(0f, slamPulse * 3f), 0f);
                    break;

                // 3. Air Drop: Floating high then settling
                case PatternAnimationType.AirDrop:
                    float airFloat = Mathf.Sin(t * 2.8f) * 5f;
                    cardTransform.localPosition = initialPosition + new Vector3(0f, airFloat, 0f);
                    break;

                // 4. Energy Shot: Pulsing glow with slight horizontal recoil
                case PatternAnimationType.EnergyShot:
                    float charge = Mathf.Sin(t * 4f) * 2f;
                    cardTransform.localPosition = initialPosition + new Vector3(-Mathf.Abs(charge), 0f, 0f);
                    break;

                // 5. Rising Dash: Diagonal upward float
                case PatternAnimationType.RisingDash:
                    float rise = Mathf.Sin(t * 2.5f) * 4f;
                    cardTransform.localPosition = initialPosition + new Vector3(rise * 0.5f, rise, 0f);
                    break;

                // 6. Power Dash: Forward forward forward power vibration
                case PatternAnimationType.PowerDash:
                    float vib = Mathf.Sin(t * 18f) * 1.5f;
                    cardTransform.localPosition = initialPosition + new Vector3(vib, 0f, 0f);
                    break;

                // 7. Shadow Strike: Opacity pulsing between dark and light
                case PatternAnimationType.ShadowStrike:
                    if (cardImage != null)
                    {
                        float alpha = 0.75f + Mathf.Sin(t * 3f) * 0.25f;
                        cardImage.color = new Color(1f, 1f, 1f, alpha);
                    }
                    break;

                // 8. Rebirth Heal: Soft upward breathing expansion
                case PatternAnimationType.RebirthHeal:
                    float healScale = 1f + Mathf.Sin(t * 2.0f) * 0.035f;
                    cardTransform.localScale = initialScale * healScale;
                    break;

                // 9. Dark Pulse: Periodic outward pulse
                case PatternAnimationType.DarkPulse:
                    float dp = Mathf.PingPong(t * 1.5f, 1f);
                    cardTransform.localScale = initialScale * (1f + dp * 0.04f);
                    break;

                // 10. Super Sprint: High-speed vibration
                case PatternAnimationType.SuperSprint:
                    float sprintShake = Mathf.Sin(t * 25f) * 1.8f;
                    cardTransform.localPosition = initialPosition + new Vector3(sprintShake, 0f, 0f);
                    break;

                // 11. Spin Move: Continuous subtle rotation wobble
                case PatternAnimationType.SpinMove:
                    float spinWobble = Mathf.Sin(t * 4f) * 3f;
                    cardTransform.localRotation = initialRotation * Quaternion.Euler(0f, 0f, spinWobble);
                    break;

                // 12. Quick Step: Horizontal side-to-side stutter step
                case PatternAnimationType.QuickStep:
                    float step = Mathf.Sin(t * 5f) * 4f;
                    cardTransform.localPosition = initialPosition + new Vector3(step, 0f, 0f);
                    break;

                // 13. Backstep: Subtle backward rocking
                case PatternAnimationType.Backstep:
                    float rock = Mathf.Sin(t * 3f) * 3f;
                    cardTransform.localPosition = initialPosition + new Vector3(-Mathf.Max(0f, rock), 0f, 0f);
                    break;

                // 14. Breakthrough: Forward pressure pulse
                case PatternAnimationType.Breakthrough:
                    float bt = Mathf.PingPong(t * 2f, 1f) * 3.5f;
                    cardTransform.localPosition = initialPosition + new Vector3(bt, 0f, 0f);
                    break;

                // 15. Vision Block: Smoky alpha breathing
                case PatternAnimationType.VisionBlock:
                    if (cardImage != null)
                    {
                        float vAlpha = 0.82f + Mathf.Sin(t * 2.5f) * 0.18f;
                        cardImage.color = new Color(0.9f, 0.9f, 1f, vAlpha);
                    }
                    break;

                // 16. Double Strike: Quick 1-2 scale twitch
                case PatternAnimationType.DoubleStrike:
                    float ds = Mathf.Abs(Mathf.Sin(t * 4f)) * 0.04f;
                    cardTransform.localScale = initialScale * (1f + ds);
                    break;

                // 17. Dual Jump: Vertical bouncing hop
                case PatternAnimationType.DualJump:
                    float dj = Mathf.Abs(Mathf.Sin(t * 3.5f)) * 5f;
                    cardTransform.localPosition = initialPosition + new Vector3(0f, dj, 0f);
                    break;

                // 18. Triple Boost: 3-tier stepped forward position
                case PatternAnimationType.TripleBoost:
                    float tb = (Mathf.Floor(Mathf.PingPong(t * 3f, 3f)) * 1.5f);
                    cardTransform.localPosition = initialPosition + new Vector3(tb, 0f, 0f);
                    break;

                // 19. Triple Slash: Slanted tilt vibration
                case PatternAnimationType.TripleSlash:
                    float slashTilt = Mathf.Sin(t * 6f) * 2f;
                    cardTransform.localRotation = initialRotation * Quaternion.Euler(0f, 0f, slashTilt);
                    break;

                // 20. Blink Teleport: Periodic glitch flash
                case PatternAnimationType.BlinkTeleport:
                    if (cardImage != null)
                    {
                        bool blink = (Mathf.FloorToInt(t * 6f) % 8 == 0);
                        cardImage.color = blink ? new Color(0.5f, 1f, 1f, 0.4f) : Color.white;
                    }
                    break;

                // 21. Air Combo: Juggling vertical float
                case PatternAnimationType.AirCombo:
                    float comboY = Mathf.Sin(t * 4f) * 4f;
                    cardTransform.localPosition = initialPosition + new Vector3(0f, comboY, 0f);
                    break;

                // 22. Forward Rush: Constant forward lean
                case PatternAnimationType.ForwardRush:
                    float rush = Mathf.Sin(t * 4.5f) * 2.5f;
                    cardTransform.localPosition = initialPosition + new Vector3(rush, 0f, 0f);
                    break;

                // 23. Shield: Protective hexagonal pulsation
                case PatternAnimationType.Shield:
                    float shScale = 1f + Mathf.Sin(t * 2.5f) * 0.03f;
                    cardTransform.localScale = initialScale * shScale;
                    break;

                // 24. Explosion: Energy expansion buildup
                case PatternAnimationType.Explosion:
                    float expPulse = 1f + Mathf.PingPong(t * 2f, 0.05f);
                    cardTransform.localScale = initialScale * expPulse;
                    break;

                // 25. Speed Surge: Horizontal streak wobble
                case PatternAnimationType.SpeedSurge:
                    float surge = Mathf.Sin(t * 12f) * 2f;
                    cardTransform.localPosition = initialPosition + new Vector3(surge, 0f, 0f);
                    break;

                // 26. Slide Attack: Low position with forward tilt
                case PatternAnimationType.SlideAttack:
                    cardTransform.localPosition = initialPosition + new Vector3(Mathf.Sin(t * 4f) * 3f, -2f, 0f);
                    break;

                // 27. Climb Boost: Ascending steps
                case PatternAnimationType.ClimbBoost:
                    float climb = Mathf.PingPong(t * 2.5f, 6f);
                    cardTransform.localPosition = initialPosition + new Vector3(0f, climb, 0f);
                    break;

                // 28. Slide Drop: Downward glide
                case PatternAnimationType.SlideDrop:
                    float drop = -Mathf.PingPong(t * 2.5f, 5f);
                    cardTransform.localPosition = initialPosition + new Vector3(0f, drop, 0f);
                    break;

                // 29. Clone Strike: Twin heartbeat pulse
                case PatternAnimationType.CloneStrike:
                    float cloneP = 1f + Mathf.Abs(Mathf.Sin(t * 3f)) * 0.04f;
                    cardTransform.localScale = initialScale * cloneP;
                    break;

                // 30. Revive Wave: Smooth expanding and contracting wave
                case PatternAnimationType.ReviveWave:
                    float wave = 1f + Mathf.Sin(t * 2f) * 0.05f;
                    cardTransform.localScale = initialScale * wave;
                    break;
            }
        }

        /// <summary>
        /// Triggers full, high-impact movement animation sequence (used on card tap or quiz correct answer)
        /// </summary>
        public void PlayBurstAnimation(System.Action onComplete = null)
        {
            if (activeBurstRoutine != null)
            {
                StopCoroutine(activeBurstRoutine);
            }
            activeBurstRoutine = StartCoroutine(BurstAnimationRoutine(onComplete));
        }

        private IEnumerator BurstAnimationRoutine(System.Action onComplete)
        {
            float duration = 0.85f;
            float elapsed = 0f;

            if (auraGlowImage != null) auraGlowImage.gameObject.SetActive(true);

            switch (animationType)
            {
                // 1. Meditate: Ascends higher, halo pulses, serene expansion
                case PatternAnimationType.Meditate:
                    while (elapsed < duration)
                    {
                        elapsed += Time.deltaTime;
                        float p = elapsed / duration;
                        float floatY = Mathf.Sin(p * Mathf.PI) * 18f;
                        cardTransform.localPosition = initialPosition + new Vector3(0f, floatY, 0f);
                        cardTransform.localScale = initialScale * (1f + Mathf.Sin(p * Mathf.PI) * 0.12f);
                        if (auraGlowImage != null) auraGlowImage.color = new Color(0.2f, 0.8f, 1f, Mathf.Sin(p * Mathf.PI) * 0.8f);
                        yield return null;
                    }
                    break;

                // 2. Ground Slam: Jumps UP, then SLAMS down, shockwave explodes outward!
                case PatternAnimationType.GroundSlam:
                    // Phase 1: Rapid jump
                    float jumpTime = 0.25f;
                    float jumpElapsed = 0f;
                    while (jumpElapsed < jumpTime)
                    {
                        jumpElapsed += Time.deltaTime;
                        float t = jumpElapsed / jumpTime;
                        cardTransform.localPosition = initialPosition + new Vector3(0f, Mathf.Sin(t * Mathf.PI * 0.5f) * 28f, 0f);
                        yield return null;
                    }
                    // Phase 2: High speed slam down
                    float slamTime = 0.12f;
                    float slamElapsed = 0f;
                    while (slamElapsed < slamTime)
                    {
                        slamElapsed += Time.deltaTime;
                        float t = slamElapsed / slamTime;
                        cardTransform.localPosition = initialPosition + new Vector3(0f, (1f - t) * 28f - 4f, 0f);
                        yield return null;
                    }
                    // Phase 3: Shockwave ring expansion
                    if (shockwaveRing != null)
                    {
                        shockwaveRing.gameObject.SetActive(true);
                        StartCoroutine(AnimateShockwaveRing(0.45f, new Color(0f, 1f, 0.9f)));
                    }
                    // Settle with slight shake
                    float settleTime = 0.25f;
                    float settleElapsed = 0f;
                    while (settleElapsed < settleTime)
                    {
                        settleElapsed += Time.deltaTime;
                        float t = settleElapsed / settleTime;
                        float shake = Mathf.Sin(t * 30f) * (1f - t) * 4f;
                        cardTransform.localPosition = initialPosition + new Vector3(shake, 0f, 0f);
                        yield return null;
                    }
                    break;

                // 4. Energy Shot: Recoil + Laser projectile beam
                case PatternAnimationType.EnergyShot:
                    if (projectileImage != null)
                    {
                        projectileImage.gameObject.SetActive(true);
                        StartCoroutine(AnimateProjectileShot(0.5f));
                    }
                    // Recoil
                    while (elapsed < duration)
                    {
                        elapsed += Time.deltaTime;
                        float p = elapsed / duration;
                        float recoil = -Mathf.Sin(p * Mathf.PI) * 14f;
                        cardTransform.localPosition = initialPosition + new Vector3(recoil, 0f, 0f);
                        yield return null;
                    }
                    break;

                // 11. Spin Move: Full 360-degree whirlwind spin
                case PatternAnimationType.SpinMove:
                    while (elapsed < duration)
                    {
                        elapsed += Time.deltaTime;
                        float p = elapsed / duration;
                        float rotAngle = p * 720f;
                        cardTransform.localRotation = initialRotation * Quaternion.Euler(0f, 0f, rotAngle);
                        cardTransform.localScale = initialScale * (1f + Mathf.Sin(p * Mathf.PI) * 0.15f);
                        yield return null;
                    }
                    cardTransform.localRotation = initialRotation;
                    break;

                // 20. Blink Teleport: Disappear -> Glitch -> Reappear
                case PatternAnimationType.BlinkTeleport:
                    if (cardImage != null) cardImage.color = new Color(0.2f, 1f, 1f, 0.15f);
                    yield return new WaitForSeconds(0.18f);
                    cardTransform.localPosition = initialPosition + new Vector3(30f, 0f, 0f);
                    yield return new WaitForSeconds(0.1f);
                    if (cardImage != null) cardImage.color = Color.white;
                    if (shockwaveRing != null)
                    {
                        shockwaveRing.gameObject.SetActive(true);
                        StartCoroutine(AnimateShockwaveRing(0.4f, new Color(0.2f, 1f, 1f)));
                    }
                    yield return new WaitForSeconds(0.25f);
                    cardTransform.localPosition = initialPosition;
                    break;

                // 24. Explosion: Blinding flash + massive radial shockwave
                case PatternAnimationType.Explosion:
                    if (shockwaveRing != null)
                    {
                        shockwaveRing.gameObject.SetActive(true);
                        StartCoroutine(AnimateShockwaveRing(0.6f, new Color(1f, 0.85f, 0.1f)));
                    }
                    while (elapsed < duration)
                    {
                        elapsed += Time.deltaTime;
                        float p = elapsed / duration;
                        float pop = 1f + Mathf.Sin(p * Mathf.PI) * 0.25f;
                        cardTransform.localScale = initialScale * pop;
                        float shake = (1f - p) * Mathf.Sin(elapsed * 45f) * 6f;
                        cardTransform.localPosition = initialPosition + new Vector3(shake, shake, 0f);
                        yield return null;
                    }
                    break;

                // Default burst: Punchy power surge + glow
                default:
                    while (elapsed < duration)
                    {
                        elapsed += Time.deltaTime;
                        float p = elapsed / duration;
                        float pulse = 1f + Mathf.Sin(p * Mathf.PI) * 0.16f;
                        cardTransform.localScale = initialScale * pulse;
                        if (auraGlowImage != null)
                        {
                            auraGlowImage.color = new Color(1f, 1f, 1f, Mathf.Sin(p * Mathf.PI) * 0.8f);
                        }
                        yield return null;
                    }
                    break;
            }

            ResetVisualState();
            activeBurstRoutine = null;
            onComplete?.Invoke();
        }

        private IEnumerator AnimateShockwaveRing(float duration, Color color)
        {
            if (shockwaveRing == null) yield break;
            RectTransform srt = shockwaveRing.GetComponent<RectTransform>();
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                srt.localScale = Vector3.one * (0.2f + t * 2.2f);
                shockwaveRing.color = new Color(color.r, color.g, color.b, (1f - t) * 0.9f);
                yield return null;
            }

            shockwaveRing.gameObject.SetActive(false);
        }

        private IEnumerator AnimateProjectileShot(float duration)
        {
            if (projectileImage == null) yield break;
            RectTransform prt = projectileImage.GetComponent<RectTransform>();
            float elapsed = 0f;
            Vector3 startP = new Vector3(-20f, 0f, 0f);
            Vector3 endP = new Vector3(80f, 0f, 0f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                prt.localPosition = Vector3.Lerp(startP, endP, t);
                projectileImage.color = new Color(0f, 1f, 1f, (1f - t * 0.5f));
                yield return null;
            }

            projectileImage.gameObject.SetActive(false);
        }

        /// <summary>
        /// Automatically generates glowing aura and shockwave helper objects if not present in prefab
        /// </summary>
        private void EnsureVfxElements()
        {
            if (cardTransform == null) return;

            if (auraGlowImage == null)
            {
                GameObject auraObj = new GameObject("AuraGlow", typeof(RectTransform), typeof(Image));
                auraObj.transform.SetParent(cardTransform, false);
                auraObj.transform.SetAsFirstSibling();
                RectTransform art = auraObj.GetComponent<RectTransform>();
                art.anchorMin = Vector2.zero;
                art.anchorMax = Vector2.one;
                art.sizeDelta = new Vector2(16, 16);
                auraGlowImage = auraObj.GetComponent<Image>();
                auraGlowImage.color = new Color(0.2f, 0.8f, 1f, 0f);
                auraObj.SetActive(false);
            }

            if (shockwaveRing == null)
            {
                GameObject ringObj = new GameObject("ShockwaveRing", typeof(RectTransform), typeof(Image));
                ringObj.transform.SetParent(cardTransform, false);
                RectTransform rrt = ringObj.GetComponent<RectTransform>();
                rrt.sizeDelta = new Vector2(60, 60);
                shockwaveRing = ringObj.GetComponent<Image>();
                shockwaveRing.color = new Color(0f, 1f, 1f, 0f);
                ringObj.SetActive(false);
            }

            if (projectileImage == null)
            {
                GameObject projObj = new GameObject("ProjectileBeam", typeof(RectTransform), typeof(Image));
                projObj.transform.SetParent(cardTransform, false);
                RectTransform prt = projObj.GetComponent<RectTransform>();
                prt.sizeDelta = new Vector2(24, 6);
                projectileImage = projObj.GetComponent<Image>();
                projectileImage.color = new Color(0.2f, 1f, 1f, 0f);
                projObj.SetActive(false);
            }
        }
    }
}
