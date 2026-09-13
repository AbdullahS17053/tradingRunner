using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SciFiUI.Effects
{
    /// <summary>
    /// Production-ready, unified Cyberpunk UI Effects component.
    /// Combines:
    /// 1. Glass Shine Sweep (isolated mask container, zero corner bleed, zero child text culling).
    /// 2. Rotating Border Glow (rotating arc tracer, full glow ring, custom bracket sprites with additive neon glow).
    /// 3. Subtle Button Scale Pulse (breathing idle scale animation).
    /// All 3 features can be toggled on/off independently via simple Inspector checkboxes.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class CyberpunkUIEffects : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        public enum BorderGlowStyle
        {
            [Tooltip("An energetic neon light arc/tracer that sweeps continuously around the sprite border.")]
            RotatingArcTracer,

            [Tooltip("A continuous full glowing border ring with luminous highlights.")]
            FullGlowRing,

            [Tooltip("Uses a custom border/ring sprite (such as tech brackets or segmented rings).")]
            CustomSprite
        }

        #region Inspector Settings

        [Header("1. Glass Shine Sweep Effect")]
        [Tooltip("Enable the diagonal glass sheen sweep animation across this UI element.")]
        [SerializeField] private bool enableShine = true;

        [SerializeField] private float sweepDuration = 0.65f;
        [SerializeField] private float shineInterval = 3.2f;
        [SerializeField] private float shineInitialDelay = 0.5f;
        [SerializeField] private Color shineColor = new Color(1f, 1f, 1f, 0.45f);
        [SerializeField] private float shineWidth = 65f;
        [SerializeField] private float shineAngle = -25f;
        [SerializeField] private Sprite customShineSprite;
        [SerializeField] private bool autoLoopShine = true;
        [SerializeField] private bool triggerShineOnHover = true;
        [SerializeField] private bool triggerShineOnClick = true;

        [Header("2. Rotating Border Glow Effect")]
        [Tooltip("Enable the rotating glowing border / neon light tracer around this sprite.")]
        [SerializeField] private bool enableBorderGlow = true;

        [SerializeField] private BorderGlowStyle borderGlowStyle = BorderGlowStyle.RotatingArcTracer;
        [SerializeField] private Color borderGlowColor = new Color(0f, 0.95f, 1f, 0.85f);
        [SerializeField] private Sprite customBorderSprite;
        [Range(0.8f, 1.3f)] [SerializeField] private float borderScale = 1.0f;
        [Tooltip("Rotation speed in degrees per second. Negative = Clockwise, Positive = Counter-Clockwise.")]
        [SerializeField] private float borderRotationSpeed = -50f;
        [Range(0.05f, 1.0f)] [SerializeField] private float arcFillAmount = 0.32f;
        [SerializeField] private bool arcClockwise = true;
        [SerializeField] private bool enableGlowPulse = true;
        [Range(0f, 0.5f)] [SerializeField] private float glowPulseAmount = 0.18f;
        [SerializeField] private float glowPulseSpeed = 2.4f;

        [Header("3. Button Scale Pulse")]
        [Tooltip("Enable a subtle breathing scale pulse on this UI element.")]
        [SerializeField] private bool enableScalePulse = false;

        [Range(0f, 0.1f)] [SerializeField] private float scalePulseAmount = 0.03f;
        [SerializeField] private float scalePulseSpeed = 2.5f;

        #endregion

        #region Private Fields

        private RectTransform _rectTransform;
        private Vector3 _initialScale;

        // Shine members
        private GameObject _shineContainer;
        private RectTransform _shineRect;
        private Image _shineImage;
        private Coroutine _shineLoopCoroutine;
        private Coroutine _shineSweepCoroutine;
        private static Sprite _cachedShineSprite;

        // Border Glow members
        private GameObject _glowGo;
        private RectTransform _glowRect;
        private Image _glowImage;
        private Material _runtimeAdditiveMaterial;
        private static Sprite _cachedRingSprite;
        private float _glowPulseTimer;
        private float _scalePulseTimer;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _initialScale = _rectTransform.localScale;

            if (enableShine) InitShine();
            if (enableBorderGlow) InitBorderGlow();
        }

        private void OnEnable()
        {
            if (enableShine && autoLoopShine)
            {
                if (_shineLoopCoroutine != null) StopCoroutine(_shineLoopCoroutine);
                _shineLoopCoroutine = StartCoroutine(ShineLoopRoutine());
            }

            if (_glowImage != null && enableBorderGlow)
            {
                _glowImage.enabled = true;
            }
        }

        private void OnDisable()
        {
            if (_shineLoopCoroutine != null)
            {
                StopCoroutine(_shineLoopCoroutine);
                _shineLoopCoroutine = null;
            }

            if (_shineSweepCoroutine != null)
            {
                StopCoroutine(_shineSweepCoroutine);
                _shineSweepCoroutine = null;
            }

            if (_shineImage != null) _shineImage.enabled = false;
            if (_glowImage != null) _glowImage.enabled = false;

            if (_rectTransform != null) _rectTransform.localScale = _initialScale;
        }

        private void OnDestroy()
        {
            if (_runtimeAdditiveMaterial != null)
            {
                SafeDestroy(_runtimeAdditiveMaterial);
                _runtimeAdditiveMaterial = null;
            }

            if (_shineContainer != null) SafeDestroy(_shineContainer);
            if (_glowGo != null) SafeDestroy(_glowGo);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            // 1. Rotating Border Glow update
            if (enableBorderGlow && _glowRect != null && _glowImage != null)
            {
                _glowRect.Rotate(0f, 0f, borderRotationSpeed * dt);

                if (enableGlowPulse)
                {
                    _glowPulseTimer += dt * glowPulseSpeed;
                    float sinVal = Mathf.Sin(_glowPulseTimer);
                    float alphaMod = 1f + sinVal * glowPulseAmount;
                    Color c = borderGlowColor;
                    c.a = Mathf.Clamp01(borderGlowColor.a * alphaMod);
                    _glowImage.color = c;
                }
            }

            // 2. Button Scale Pulse update
            if (enableScalePulse && _rectTransform != null)
            {
                _scalePulseTimer += dt * scalePulseSpeed;
                float sinVal = Mathf.Sin(_scalePulseTimer);
                float pulse = Mathf.Max(0f, sinVal) * scalePulseAmount;
                _rectTransform.localScale = _initialScale * (1f + pulse);
            }
        }

        #endregion

        #region Shine Implementation

        private void InitShine()
        {
            // Dedicated container ensures child text/icons are NEVER masked out
            Transform existing = transform.Find("UI_Shine_Container");
            if (existing != null)
            {
                _shineContainer = existing.gameObject;
            }
            else
            {
                _shineContainer = new GameObject("UI_Shine_Container");
                _shineContainer.transform.SetParent(transform, false);
                _shineContainer.transform.SetAsLastSibling();
            }

            RectTransform containerRect = _shineContainer.GetComponent<RectTransform>();
            if (containerRect == null) containerRect = _shineContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.sizeDelta = Vector2.zero;
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.localScale = Vector3.one;

            Image maskImg = _shineContainer.GetComponent<Image>();
            if (maskImg == null) maskImg = _shineContainer.AddComponent<Image>();
            maskImg.raycastTarget = false;

            Image parentImg = GetComponent<Image>();
            if (parentImg != null)
            {
                maskImg.sprite = parentImg.sprite;
                maskImg.type = parentImg.type;
                maskImg.preserveAspect = parentImg.preserveAspect;
            }

            Mask mask = _shineContainer.GetComponent<Mask>();
            if (mask == null) mask = _shineContainer.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Shine bar child inside container
            Transform existingShine = _shineContainer.transform.Find("UI_Shine_Bar");
            GameObject shineGo = existingShine != null ? existingShine.gameObject : new GameObject("UI_Shine_Bar");
            shineGo.transform.SetParent(_shineContainer.transform, false);

            _shineRect = shineGo.GetComponent<RectTransform>();
            if (_shineRect == null) _shineRect = shineGo.AddComponent<RectTransform>();

            _shineImage = shineGo.GetComponent<Image>();
            if (_shineImage == null) _shineImage = shineGo.AddComponent<Image>();

            _shineImage.raycastTarget = false;
            _shineImage.color = shineColor;
            _shineImage.sprite = customShineSprite != null ? customShineSprite : GetOrCreateShineSprite();

            _shineRect.localRotation = Quaternion.Euler(0, 0, shineAngle);
            _shineImage.enabled = false;
        }

        private IEnumerator ShineLoopRoutine()
        {
            yield return new WaitForSeconds(shineInitialDelay);

            while (true)
            {
                yield return StartCoroutine(SweepRoutine());
                yield return new WaitForSeconds(shineInterval);
            }
        }

        public void TriggerShine()
        {
            if (!gameObject.activeInHierarchy || !enableShine) return;
            if (_shineSweepCoroutine != null) StopCoroutine(_shineSweepCoroutine);
            _shineSweepCoroutine = StartCoroutine(SweepRoutine());
        }

        private IEnumerator SweepRoutine()
        {
            if (_shineRect == null || _shineImage == null) yield break;

            float parentW = _rectTransform.rect.width;
            float parentH = _rectTransform.rect.height;

            float diagonal = Mathf.Sqrt(parentW * parentW + parentH * parentH);
            _shineRect.sizeDelta = new Vector2(shineWidth, diagonal * 1.5f);

            float startX = -parentW * 0.5f - shineWidth;
            float endX = parentW * 0.5f + shineWidth;

            _shineImage.enabled = true;
            _shineImage.color = shineColor;

            float elapsed = 0f;
            while (elapsed < sweepDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / sweepDuration);
                float easedT = t * t * (3f - 2f * t);

                float currentX = Mathf.Lerp(startX, endX, easedT);
                _shineRect.anchoredPosition = new Vector2(currentX, 0f);

                yield return null;
            }

            _shineImage.enabled = false;
            _shineSweepCoroutine = null;
        }

        private static Sprite GetOrCreateShineSprite()
        {
            if (_cachedShineSprite != null) return _cachedShineSprite;

            int width = 64;
            int height = 8;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int x = 0; x < width; x++)
            {
                float t = (float)x / (width - 1);
                float alpha = Mathf.Pow(Mathf.Sin(t * Mathf.PI), 1.8f);
                Color col = new Color(1f, 1f, 1f, alpha);
                for (int y = 0; y < height; y++) tex.SetPixel(x, y, col);
            }
            tex.Apply();

            _cachedShineSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
            return _cachedShineSprite;
        }

        #endregion

        #region Border Glow Implementation

        private void InitBorderGlow()
        {
            Transform existing = transform.Find("UI_Border_Glow");
            _glowGo = existing != null ? existing.gameObject : new GameObject("UI_Border_Glow");
            _glowGo.transform.SetParent(transform, false);
            _glowGo.transform.SetAsLastSibling();

            _glowRect = _glowGo.GetComponent<RectTransform>();
            if (_glowRect == null) _glowRect = _glowGo.AddComponent<RectTransform>();

            _glowImage = _glowGo.GetComponent<Image>();
            if (_glowImage == null) _glowImage = _glowGo.AddComponent<Image>();

            _glowImage.raycastTarget = false;
            _glowImage.maskable = false; // Immune to stencil masks
            _glowImage.color = borderGlowColor;

            Shader additiveShader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (additiveShader == null) additiveShader = Shader.Find("UI/Default");

            if (additiveShader != null)
            {
                _runtimeAdditiveMaterial = new Material(additiveShader) { hideFlags = HideFlags.DontSave };
                _glowImage.material = _runtimeAdditiveMaterial;
            }

            _glowRect.anchorMin = new Vector2(0.5f, 0.5f);
            _glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            _glowRect.pivot = new Vector2(0.5f, 0.5f);
            _glowRect.anchoredPosition = Vector2.zero;
            _glowRect.sizeDelta = _rectTransform.rect.size * borderScale;

            Sprite targetSprite = customBorderSprite != null ? customBorderSprite : GetOrCreateProceduralRingSprite();
            _glowImage.sprite = targetSprite;

            switch (borderGlowStyle)
            {
                case BorderGlowStyle.RotatingArcTracer:
                    _glowImage.type = Image.Type.Filled;
                    _glowImage.fillMethod = Image.FillMethod.Radial360;
                    _glowImage.fillOrigin = (int)Image.Origin360.Top;
                    _glowImage.fillAmount = arcFillAmount;
                    _glowImage.fillClockwise = arcClockwise;
                    break;

                case BorderGlowStyle.FullGlowRing:
                case BorderGlowStyle.CustomSprite:
                    _glowImage.type = Image.Type.Simple;
                    break;
            }

            _glowImage.enabled = true;
        }

        private static Sprite GetOrCreateProceduralRingSprite()
        {
            if (_cachedRingSprite != null) return _cachedRingSprite;

            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float outerR = size * 0.48f;
            float innerR = size * 0.38f;
            float midR = (outerR + innerR) * 0.5f;
            float halfThick = (outerR - innerR) * 0.5f;

            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = 0f;

                    if (dist >= innerR && dist <= outerR)
                    {
                        float dFromMid = Mathf.Abs(dist - midR);
                        float norm = 1f - (dFromMid / halfThick);
                        alpha = Mathf.SmoothStep(0f, 1f, norm);
                    }

                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            _cachedRingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return _cachedRingSprite;
        }

        #endregion

        #region Event Handlers & Public API

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (triggerShineOnHover) TriggerShine();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (triggerShineOnClick) TriggerShine();
        }

        public void SetShineEnabled(bool enabled)
        {
            enableShine = enabled;
            if (_shineContainer != null) _shineContainer.SetActive(enabled);
        }

        public void SetBorderGlowEnabled(bool enabled)
        {
            enableBorderGlow = enabled;
            if (_glowGo != null) _glowGo.SetActive(enabled);
        }

        public void SetScalePulseEnabled(bool enabled)
        {
            enableScalePulse = enabled;
            if (!enabled && _rectTransform != null) _rectTransform.localScale = _initialScale;
        }

        private static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        #endregion
    }
}
