using UnityEngine;
using UnityEngine.UI;

namespace SciFiUI.Effects
{
    /// <summary>
    /// Creates and controls a rotating, glowing border effect around any UI Image sprite.
    /// Supports procedural glowing arc tracers, full glow rings, and custom bracket/ring sprites.
    /// Uses additive blending for vibrant cyberpunk neon light emission.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class UISpriteBorderGlow : MonoBehaviour
    {
        public enum GlowStyle
        {
            [Tooltip("An energetic neon light arc/tracer that sweeps continuously around the sprite border.")]
            RotatingArcTracer,

            [Tooltip("A continuous full glowing border ring with luminous highlights.")]
            FullGlowRing,

            [Tooltip("Uses a custom border/ring sprite (such as tech brackets or segmented rings) that rotates and glows.")]
            CustomSprite
        }

        [Header("Glow Style & Appearance")]
        [SerializeField] private GlowStyle glowStyle = GlowStyle.RotatingArcTracer;

        [Tooltip("Color and emission intensity of the border glow.")]
        [SerializeField] private Color glowColor = new Color(0f, 0.95f, 1f, 0.85f);

        [Tooltip("Custom sprite to use when style is CustomSprite or as a border overlay.")]
        [SerializeField] private Sprite customBorderSprite;

        [Tooltip("Scale multiplier relative to the parent sprite (1.0 = exact border, 1.05 = outer aura, 0.95 = inner rim).")]
        [Range(0.8f, 1.3f)] [SerializeField] private float borderScale = 1.0f;

        [Header("Rotation Settings")]
        [Tooltip("Rotation speed in degrees per second. Positive = Counter-Clockwise, Negative = Clockwise.")]
        [SerializeField] private float rotationSpeed = -50f;

        [Tooltip("Use unscaled time so the glow rotates smoothly even during pause.")]
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Arc Settings (for RotatingArcTracer)")]
        [Tooltip("Fraction of the perimeter covered by the glowing arc (0.15 = tight comet, 0.35 = medium arc, 0.6 = broad sweep).")]
        [Range(0.05f, 1.0f)] [SerializeField] private float arcFillAmount = 0.32f;

        [Tooltip("Softens the tail of the rotating arc tracer.")]
        [SerializeField] private bool clockwise = true;

        [Header("Pulsing / Breathing")]
        [Tooltip("Subtle breathing intensity pulse for the glow.")]
        [SerializeField] private bool enablePulse = true;

        [Range(0f, 0.5f)] [SerializeField] private float pulseAmount = 0.18f;
        [SerializeField] private float pulseSpeed = 2.4f;

        private RectTransform _parentRect;
        private RectTransform _glowRect;
        private Image _glowImage;
        private Material _runtimeAdditiveMaterial;
        private static Sprite _cachedProceduralRingSprite;
        private float _pulseTimer;

        private void Awake()
        {
            _parentRect = GetComponent<RectTransform>();
            CreateGlowObject();
        }

        private void OnEnable()
        {
            if (_glowImage != null)
            {
                _glowImage.enabled = true;
            }
        }

        private void OnDisable()
        {
            if (_glowImage != null)
            {
                _glowImage.enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (_runtimeAdditiveMaterial != null)
            {
                SafeDestroy(_runtimeAdditiveMaterial);
                _runtimeAdditiveMaterial = null;
            }

            if (_glowRect != null)
            {
                SafeDestroy(_glowRect.gameObject);
            }
        }

        private void Update()
        {
            if (_glowRect == null || _glowImage == null) return;

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            // Continuous rotation
            _glowRect.Rotate(0f, 0f, rotationSpeed * dt);

            // Subtle breathing glow pulse
            if (enablePulse)
            {
                _pulseTimer += dt * pulseSpeed;
                float sinVal = Mathf.Sin(_pulseTimer);
                float alphaMod = 1f + sinVal * pulseAmount;

                Color c = glowColor;
                c.a = Mathf.Clamp01(glowColor.a * alphaMod);
                _glowImage.color = c;
            }
        }

        private void CreateGlowObject()
        {
            Transform existing = transform.Find("UI_Sprite_Border_Glow");
            GameObject glowGo;

            if (existing != null)
            {
                glowGo = existing.gameObject;
            }
            else
            {
                glowGo = new GameObject("UI_Sprite_Border_Glow");
                glowGo.transform.SetParent(transform, false);
                glowGo.transform.SetAsLastSibling();
            }

            _glowRect = glowGo.GetComponent<RectTransform>();
            if (_glowRect == null) _glowRect = glowGo.AddComponent<RectTransform>();

            _glowImage = glowGo.GetComponent<Image>();
            if (_glowImage == null) _glowImage = glowGo.AddComponent<Image>();

            _glowImage.raycastTarget = false;
            _glowImage.maskable = false;
            _glowImage.color = glowColor;

            // Setup Additive material for glowing neon effect
            Shader additiveShader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (additiveShader == null) additiveShader = Shader.Find("UI/Default");

            if (additiveShader != null)
            {
                _runtimeAdditiveMaterial = new Material(additiveShader)
                {
                    hideFlags = HideFlags.DontSave
                };
                _glowImage.material = _runtimeAdditiveMaterial;
            }

            // Anchors stretch to fit parent
            _glowRect.anchorMin = new Vector2(0.5f, 0.5f);
            _glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            _glowRect.pivot = new Vector2(0.5f, 0.5f);
            _glowRect.anchoredPosition = Vector2.zero;
            UpdateGlowSize();

            // Configure sprite and fill type based on style
            ConfigureGlowVisuals();
        }

        public void UpdateGlowSize()
        {
            if (_glowRect == null || _parentRect == null) return;
            Vector2 parentSize = _parentRect.rect.size;
            _glowRect.sizeDelta = parentSize * borderScale;
        }

        public void ConfigureGlowVisuals()
        {
            if (_glowImage == null) return;

            Sprite targetSprite = customBorderSprite;
            if (targetSprite == null)
            {
                targetSprite = GetOrCreateProceduralRingSprite();
            }

            _glowImage.sprite = targetSprite;

            switch (glowStyle)
            {
                case GlowStyle.RotatingArcTracer:
                    _glowImage.type = Image.Type.Filled;
                    _glowImage.fillMethod = Image.FillMethod.Radial360;
                    _glowImage.fillOrigin = (int)Image.Origin360.Top;
                    _glowImage.fillAmount = arcFillAmount;
                    _glowImage.fillClockwise = clockwise;
                    break;

                case GlowStyle.FullGlowRing:
                case GlowStyle.CustomSprite:
                    _glowImage.type = Image.Type.Simple;
                    break;
            }

            _glowImage.enabled = true;
        }

        private Sprite GetOrCreateProceduralRingSprite()
        {
            if (_cachedProceduralRingSprite != null) return _cachedProceduralRingSprite;

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

            _cachedProceduralRingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return _cachedProceduralRingSprite;
        }

        private static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
        }
    }
}
