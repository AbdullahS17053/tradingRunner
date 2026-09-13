using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Production-ready, self-contained Cyberpunk UI Glitch & Holographic Projector effect for Unity UI (uGUI) / URP / Unity 6.
/// Features 5 selectable cyberpunk glitch algorithms:
/// 0. Holographic Projector (ArtStation shield style: mesh slicing, core bloom, chromatic desync)
/// 1. Digital Displacement (stepped horizontal jitter, RGB split, digital noise slice bars)
/// 2. Matrix Artifact Scan (scanline-interlaced raster distortion and phase offsets)
/// 3. Cyberpunk Flicker (strobe voltage drop, neon aura flaring, sudden signal loss)
/// 4. Chromatic Aberration Only (clean non-distorting red/cyan separation, optimal for text)
/// Requires NO Animator, NO Animation Clips, and NO manual keyframing.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class CyberpunkUIGlitch : BaseMeshEffect
{
    public enum GlitchState
    {
        Hidden,
        Intro,
        Idle,
        IdleGlitch,
        Outro
    }

    public enum GlitchType
    {
        [Tooltip("ArtStation shield style: Horizontal mesh-sliced tears, luminous core bloom, and lateral chromatic desync.")]
        HolographicProjector = 0,

        [Tooltip("Classic digital UI displacement with sharp stepped horizontal jitter, RGB split, and noise slice bars.")]
        DigitalDisplacement = 1,

        [Tooltip("High-frequency scanline matrix artifact tears with alternating interlacing dropouts and phase offsets.")]
        MatrixArtifactScan = 2,

        [Tooltip("Cyberpunk signal strobe with rapid voltage flickering, neon aura flaring, and sudden holographic twitch.")]
        CyberpunkFlicker = 3,

        [Tooltip("Clean, non-distorting red/cyan chromatic separation - ideal for typography, labels, and clean HUD elements.")]
        ChromaticAberrationOnly = 4
    }

    [Header("Glitch Style")]
    [Tooltip("Select the active glitch algorithm (can also be switched live via dropdown in demo).")]
    [SerializeField] private GlitchType glitchType = GlitchType.HolographicProjector;

    [Tooltip("Play the intro reveal glitch automatically when the GameObject is enabled.")]
    [SerializeField] private bool showOnEnable = true;

    [Tooltip("Automatically enter idle pulse and periodic glitch loop after showing.")]
    [SerializeField] private bool autoIdle = true;

    [Header("Holographic & Matrix Slicing Settings")]
    [Tooltip("Number of horizontal digital bands used to slice and tear the image in sliced modes.")]
    [Range(4, 32)] [SerializeField] private int horizontalBands = 18;

    [Tooltip("Maximum horizontal tear displacement across the digital bands.")]
    [Range(0f, 50f)] [SerializeField] private float bandDisplacement = 20f;

    [Tooltip("Keeps a persistent chromatic desync ghost offset in idle state (Holographic style).")]
    [SerializeField] private bool persistentDesyncGhost = true;

    [Tooltip("Horizontal and vertical offset for the chromatic desync ghost.")]
    [SerializeField] private Vector2 desyncOffset = new Vector2(16f, -2f);

    [Tooltip("Enables the bright additive white-cyan holographic core glow.")]
    [SerializeField] private bool enableCoreGlow = true;

    [Tooltip("Color and intensity of the inner holographic core glow.")]
    [SerializeField] private Color coreGlowColor = new Color(0.75f, 1f, 1f, 0.65f);

    [Header("Glitch Timing")]
    [Tooltip("Duration of the intro reveal animation in seconds.")]
    [SerializeField] private float introDuration = 0.32f;

    [Tooltip("Duration of the outro hide animation in seconds.")]
    [SerializeField] private float outroDuration = 0.22f;

    [Tooltip("Duration of a single glitch burst in seconds.")]
    [SerializeField] private float glitchDuration = 0.12f;

    [Tooltip("Minimum wait time between idle glitch bursts in seconds.")]
    [SerializeField] private float minimumIdleGlitchDelay = 2.5f;

    [Tooltip("Maximum wait time between idle glitch bursts in seconds.")]
    [SerializeField] private float maximumIdleGlitchDelay = 5.0f;

    [Header("Glitch Appearance")]
    [Tooltip("Overall intensity multiplier for displacement and jitter.")]
    [Range(0f, 2f)] [SerializeField] private float glitchIntensity = 1.0f;

    [Tooltip("Horizontal pixel separation between cyan and red chromatic aberration ghosts.")]
    [Range(0f, 40f)] [SerializeField] private float rgbSplitAmount = 12.0f;

    [Tooltip("Color tint for the cyan holographic ghost.")]
    [SerializeField] private Color cyanColor = new Color(0f, 0.95f, 1f, 0.7f);

    [Tooltip("Color tint for the red/magenta chromatic desync ghost.")]
    [SerializeField] private Color redColor = new Color(1f, 0.1f, 0.38f, 0.75f);

    [Header("Idle Pulse")]
    [Tooltip("Subtle breathing scale increment during idle (e.g., 0.02 = 1.02 scale).")]
    [Range(0f, 0.1f)] [SerializeField] private float idlePulseAmount = 0.02f;

    [Tooltip("Speed multiplier of the idle pulse loop.")]
    [SerializeField] private float idlePulseSpeed = 2.2f;

    // Component caches
    private RectTransform _rectTransform;
    private Image _mainImage;
    private CanvasGroup _canvasGroup;

    // Transform and visual caches
    private Vector2 _initialPosition;
    private Vector3 _initialScale;
    private float _initialAlpha = 1f;

    // Mesh glitch state
    private float _currentGlitchIntensity = 0f;
    private float _glitchSeed = 0f;
    private float _glitchTime = 0f;

    // Internal runtime visual elements
    private GameObject _cyanGhostObj;
    private RectTransform _cyanGhostRect;
    private Image _cyanGhostImage;

    private GameObject _redGhostObj;
    private RectTransform _redGhostRect;
    private Image _redGhostImage;

    private GameObject _coreGlowObj;
    private RectTransform _coreGlowRect;
    private Image _coreGlowImage;

    private const int SliceCount = 3;
    private GameObject[] _sliceObjs;
    private RectTransform[] _sliceRects;

    private Material _runtimeAdditiveMaterial;
    private Coroutine _activeAnimationCoroutine;
    private Coroutine _idleLoopCoroutine;
    private GlitchState _currentState = GlitchState.Hidden;
    private bool _isInitialized = false;

    // Public properties
    public GlitchState CurrentState => _currentState;
    public GlitchType Type
    {
        get => glitchType;
        set
        {
            glitchType = value;
            UpdateVisualsForCurrentState();
        }
    }
    public bool IsShowing => _currentState != GlitchState.Hidden && _currentState != GlitchState.Outro;

    protected override void Awake()
    {
        base.Awake();
        EnsureInitialized();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        EnsureInitialized();

        if (showOnEnable)
        {
            PlayShow();
        }
        else if (autoIdle)
        {
            PlayIdle();
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        StopAllRunningCoroutines();
        ResetToCleanState();

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = _initialAlpha;
        }

        if (_rectTransform != null)
        {
            _rectTransform.localScale = _initialScale;
            _rectTransform.anchoredPosition = _initialPosition;
        }

        _currentState = GlitchState.Hidden;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (_runtimeAdditiveMaterial != null)
        {
            SafeDestroy(_runtimeAdditiveMaterial);
            _runtimeAdditiveMaterial = null;
        }

        SafeDestroy(_cyanGhostObj);
        SafeDestroy(_redGhostObj);
        SafeDestroy(_coreGlowObj);

        if (_sliceObjs != null)
        {
            for (int i = 0; i < _sliceObjs.Length; i++)
            {
                SafeDestroy(_sliceObjs[i]);
            }
        }
    }

    #region Public API & Dropdown Integration

    /// <summary>
    /// Set glitch type by index (0: Holographic, 1: Digital, 2: Matrix Scan, 3: Flicker, 4: Chromatic Aberration).
    /// Hooks directly into TMP_Dropdown.onValueChanged.
    /// </summary>
    public void SetGlitchType(int index)
    {
        int max = System.Enum.GetValues(typeof(GlitchType)).Length - 1;
        glitchType = (GlitchType)Mathf.Clamp(index, 0, max);
        UpdateVisualsForCurrentState();
    }

    /// <summary>
    /// Set glitch type by enum.
    /// </summary>
    public void SetGlitchType(GlitchType type)
    {
        glitchType = type;
        UpdateVisualsForCurrentState();
    }

    /// <summary>
    /// Plays the holographic cyberpunk intro reveal glitch, ending in clean idle.
    /// </summary>
    public void PlayShow()
    {
        EnsureInitialized();

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        StopAllRunningCoroutines();
        _activeAnimationCoroutine = StartCoroutine(IntroCoroutine());
    }

    /// <summary>
    /// Plays the glitch collapse outro, optionally deactivating the GameObject afterwards.
    /// </summary>
    public void PlayHide(bool deactivate = false)
    {
        EnsureInitialized();

        if (!gameObject.activeInHierarchy)
        {
            if (deactivate) gameObject.SetActive(false);
            _currentState = GlitchState.Hidden;
            return;
        }

        StopAllRunningCoroutines();
        _activeAnimationCoroutine = StartCoroutine(OutroCoroutine(deactivate));
    }

    /// <summary>
    /// Triggers a single subtle cyberpunk glitch burst without hiding the UI.
    /// </summary>
    public void PlayGlitch()
    {
        EnsureInitialized();

        if (!gameObject.activeInHierarchy || _currentState == GlitchState.Hidden || _currentState == GlitchState.Outro)
        {
            return;
        }

        if (_activeAnimationCoroutine != null)
        {
            StopCoroutine(_activeAnimationCoroutine);
        }

        _activeAnimationCoroutine = StartCoroutine(GlitchBurstCoroutine(glitchDuration, 1.0f));
    }

    /// <summary>
    /// Immediately restores clean UI state and begins the subtle idle pulse / periodic glitch loop.
    /// </summary>
    public void PlayIdle()
    {
        EnsureInitialized();

        StopAllRunningCoroutines();
        ResetToCleanState();

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
        }

        if (autoIdle)
        {
            _idleLoopCoroutine = StartCoroutine(IdleLoopCoroutine());
        }
        else
        {
            _currentState = GlitchState.Idle;
            if (_rectTransform != null)
            {
                _rectTransform.localScale = _initialScale;
            }
            UpdateVisualsForCurrentState();
        }
    }

    /// <summary>
    /// Enables or disables the idle pulse and periodic glitch loop.
    /// </summary>
    public void SetIdle(bool enabled)
    {
        autoIdle = enabled;

        if (enabled)
        {
            if (_idleLoopCoroutine == null && IsShowing)
            {
                _idleLoopCoroutine = StartCoroutine(IdleLoopCoroutine());
            }
        }
        else
        {
            if (_idleLoopCoroutine != null)
            {
                StopCoroutine(_idleLoopCoroutine);
                _idleLoopCoroutine = null;
            }

            if (_rectTransform != null)
            {
                _rectTransform.localScale = _initialScale;
            }

            ResetToCleanState();
        }
    }

    #endregion

    #region Internal Setup

    private void EnsureInitialized()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        _rectTransform = GetComponent<RectTransform>();
        _mainImage = GetComponent<Image>();

        if (!TryGetComponent<CanvasGroup>(out _canvasGroup))
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        _initialAlpha = _canvasGroup.alpha;
        _initialPosition = _rectTransform.anchoredPosition;
        _initialScale = _rectTransform.localScale;

        CreateGlitchVisualElements();
    }

    private void CreateGlitchVisualElements()
    {
        Shader additiveShader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (additiveShader == null) additiveShader = Shader.Find("UI/Default");

        if (additiveShader != null)
        {
            _runtimeAdditiveMaterial = new Material(additiveShader)
            {
                hideFlags = HideFlags.DontSave
            };
        }

        // Cyan Hologram Ghost
        _cyanGhostObj = CreateGhostChild("__CyberpunkGlitch_Cyan", cyanColor, out _cyanGhostRect, out _cyanGhostImage);

        // Red / Magenta Chromatic Desync Ghost
        _redGhostObj = CreateGhostChild("__CyberpunkGlitch_Red", redColor, out _redGhostRect, out _redGhostImage);

        // Luminous Holographic Core Glow
        _coreGlowObj = CreateGhostChild("__CyberpunkGlitch_CoreGlow", coreGlowColor, out _coreGlowRect, out _coreGlowImage);

        // Horizontal digital noise slice lines
        _sliceObjs = new GameObject[SliceCount];
        _sliceRects = new RectTransform[SliceCount];

        for (int i = 0; i < SliceCount; i++)
        {
            GameObject slice = new GameObject($"__CyberpunkGlitch_Slice_{i}", typeof(RectTransform), typeof(Image));
            slice.hideFlags = HideFlags.DontSave;
            slice.transform.SetParent(transform, false);

            RectTransform rt = slice.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            Image img = slice.GetComponent<Image>();
            img.raycastTarget = false;
            img.maskable = false;
            img.color = new Color(0.25f, 0.95f, 1f, 0.4f);
            if (_runtimeAdditiveMaterial != null)
            {
                img.material = _runtimeAdditiveMaterial;
            }

            slice.SetActive(false);

            _sliceObjs[i] = slice;
            _sliceRects[i] = rt;
        }

        SetGlitchVisualsActive(false);
    }

    private GameObject CreateGhostChild(string name, Color tint, out RectTransform rect, out Image image)
    {
        GameObject ghost = new GameObject(name, typeof(RectTransform), typeof(Image));
        ghost.hideFlags = HideFlags.DontSave;
        ghost.transform.SetParent(transform, false);

        rect = ghost.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.pivot = _rectTransform.pivot;

        image = ghost.GetComponent<Image>();
        image.raycastTarget = false;
        image.maskable = false;
        image.color = tint;

        if (_runtimeAdditiveMaterial != null)
        {
            image.material = _runtimeAdditiveMaterial;
        }

        SyncGhostProperties(image);
        ghost.SetActive(false);
        return ghost;
    }

    private void SyncGhostProperties(Image ghost)
    {
        if (ghost == null) return;

        if (_mainImage != null)
        {
            ghost.sprite = _mainImage.sprite;
            ghost.overrideSprite = _mainImage.overrideSprite;
            ghost.type = _mainImage.type;
            ghost.preserveAspect = _mainImage.preserveAspect;
            ghost.fillCenter = _mainImage.fillCenter;
            ghost.fillMethod = _mainImage.fillMethod;
            ghost.fillAmount = _mainImage.fillAmount;
            ghost.fillClockwise = _mainImage.fillClockwise;
            ghost.fillOrigin = _mainImage.fillOrigin;
        }
        else
        {
            ghost.sprite = null;
        }
    }

    private void SetGlitchVisualsActive(bool active)
    {
        if (_cyanGhostObj != null && _cyanGhostObj.activeSelf != active)
        {
            _cyanGhostObj.SetActive(active);
        }

        if (_redGhostObj != null)
        {
            bool redActive = active || (glitchType == GlitchType.HolographicProjector && persistentDesyncGhost && IsShowing);
            if (_redGhostObj.activeSelf != redActive)
            {
                _redGhostObj.SetActive(redActive);
            }
        }

        if (_coreGlowObj != null)
        {
            bool coreActive = enableCoreGlow && glitchType == GlitchType.HolographicProjector && IsShowing;
            if (_coreGlowObj.activeSelf != coreActive)
            {
                _coreGlowObj.SetActive(coreActive);
            }
        }

        if (!active && _sliceObjs != null)
        {
            for (int i = 0; i < _sliceObjs.Length; i++)
            {
                if (_sliceObjs[i] != null && _sliceObjs[i].activeSelf)
                {
                    _sliceObjs[i].SetActive(false);
                }
            }
        }
    }

    private void UpdateVisualsForCurrentState()
    {
        if (_currentState == GlitchState.Idle)
        {
            ResetToCleanState();
        }
    }

    #endregion

    #region Animation Sequences

    private IEnumerator IntroCoroutine()
    {
        _currentState = GlitchState.Intro;
        float duration = Mathf.Max(0.05f, introDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float alpha = t < 0.25f
            ? Mathf.Lerp(0.15f, 0.75f, t / 0.25f)
            : Mathf.Lerp(0.75f, 1.0f, (t - 0.25f) / 0.75f);
            _canvasGroup.alpha = alpha;

            _rectTransform.localScale = Vector3.Lerp(_initialScale * 0.94f, _initialScale, Mathf.SmoothStep(0f, 1f, t));

            float intensity;
            float rgbFactor;

            if (t < 0.20f)
            {
                intensity = Mathf.Lerp(0.6f, 1.35f, t / 0.20f);
                rgbFactor = 1.35f;
            }
            else if (t < 0.50f)
            {
                intensity = Mathf.Lerp(1.35f, 0.6f, (t - 0.20f) / 0.30f);
                rgbFactor = 1.0f;
            }
            else if (t < 0.80f)
            {
                intensity = Mathf.Lerp(0.6f, 0.15f, (t - 0.50f) / 0.30f);
                rgbFactor = Mathf.Lerp(1.0f, 0.2f, (t - 0.50f) / 0.30f);
            }
            else
            {
                intensity = Mathf.Lerp(0.15f, 0f, (t - 0.80f) / 0.20f);
                rgbFactor = 0f;
            }

            ApplyGlitchFrame(intensity, rgbFactor);
            yield return null;
        }

        ResetToCleanState();
        _canvasGroup.alpha = 1f;
        _rectTransform.localScale = _initialScale;
        _rectTransform.anchoredPosition = _initialPosition;

        _currentState = GlitchState.Idle;
        _activeAnimationCoroutine = null;

        if (autoIdle)
        {
            _idleLoopCoroutine = StartCoroutine(IdleLoopCoroutine());
        }
    }

    private IEnumerator OutroCoroutine(bool deactivate)
    {
        _currentState = GlitchState.Outro;
        float duration = Mathf.Max(0.05f, outroDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float intensity = t < 0.45f
            ? Mathf.Lerp(0.1f, 1.3f, t / 0.45f)
            : Mathf.Lerp(1.3f, 0.2f, (t - 0.45f) / 0.55f);

            float rgbFactor = Mathf.Clamp01(t * 1.5f);
            float alpha = t < 0.4f ? 1.0f : Mathf.Lerp(1.0f, 0f, (t - 0.4f) / 0.6f);
            _canvasGroup.alpha = alpha;

            _rectTransform.localScale = Vector3.Lerp(_initialScale, _initialScale * 0.94f, t);

            ApplyGlitchFrame(intensity, rgbFactor);
            yield return null;
        }

        ResetToCleanState();
        _canvasGroup.alpha = 0f;
        _rectTransform.localScale = _initialScale;
        _rectTransform.anchoredPosition = _initialPosition;

        _currentState = GlitchState.Hidden;
        _activeAnimationCoroutine = null;

        if (deactivate)
        {
            gameObject.SetActive(false);
        }
    }

    private IEnumerator IdleLoopCoroutine()
    {
        _currentState = GlitchState.Idle;
        float nextGlitchTime = Time.unscaledTime + Random.Range(minimumIdleGlitchDelay, maximumIdleGlitchDelay);
        float pulseTimer = 0f;
        float burstElapsed = 0f;
        float burstDuration = 0f;
        bool inBurst = false;

        while (_currentState == GlitchState.Idle || _currentState == GlitchState.IdleGlitch)
        {
            float dt = Time.unscaledDeltaTime;

            // Continuous breathing scale pulse runs uninterrupted every single frame
            pulseTimer += dt * idlePulseSpeed;
            float sinVal = Mathf.Sin(pulseTimer);
            float pulseOffset = sinVal * idlePulseAmount;
            _rectTransform.localScale = _initialScale * (1f + pulseOffset);

            if (glitchType == GlitchType.HolographicProjector && enableCoreGlow && _coreGlowImage != null)
            {
                Color cg = coreGlowColor;
                cg.a = coreGlowColor.a * (0.75f + (sinVal * 0.5f + 0.5f) * 0.35f);
                _coreGlowImage.color = cg;
            }

            // Check if it's time to trigger an occasional subtle glitch burst
            if (!inBurst && _activeAnimationCoroutine == null && Time.unscaledTime >= nextGlitchTime && _currentState == GlitchState.Idle)
            {
                inBurst = true;
                burstElapsed = 0f;
                burstDuration = Mathf.Max(0.04f, glitchDuration);
                _currentState = GlitchState.IdleGlitch;
            }

            // Animate glitch burst concurrently with breathing pulse
            if (inBurst)
            {
                burstElapsed += dt;
                float progress = burstElapsed / burstDuration;

                if (progress >= 1f)
                {
                    inBurst = false;
                    ResetToCleanState();
                    _currentState = GlitchState.Idle;
                    nextGlitchTime = Time.unscaledTime + Random.Range(minimumIdleGlitchDelay, maximumIdleGlitchDelay);
                }
                else
                {
                    // Subtle burst factor: smooth sinusoidal envelope
                    float burstFactor = Mathf.Sin(progress * Mathf.PI) * 0.75f;
                    ApplyGlitchFrame(burstFactor, 0.85f);
                }
            }

            yield return null;
        }
    }

    private IEnumerator GlitchBurstCoroutine(float duration, float strengthMultiplier)
    {
        GlitchState previousState = _currentState;
        _currentState = GlitchState.IdleGlitch;

        float elapsed = 0f;
        float actualDuration = Mathf.Max(0.04f, duration);

        while (elapsed < actualDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / actualDuration;

            float burstFactor = Mathf.Sin(progress * Mathf.PI) * strengthMultiplier;
            ApplyGlitchFrame(burstFactor, 1.0f);
            yield return null;
        }

        ResetToCleanState();
        _currentState = previousState == GlitchState.Intro ? GlitchState.Idle : previousState;
    }

    #endregion

    #region Glitch Frame Synthesis

    private void ApplyGlitchFrame(float intensity, float rgbFactor)
    {
        _currentGlitchIntensity = intensity;
        _glitchTime += Time.unscaledDeltaTime * 18f;
        _glitchSeed = Random.value * 100f;

        if (intensity <= 0.005f)
        {
            ResetToCleanState();
            return;
        }

        SetGlitchVisualsActive(true);
        SyncGhostProperties(_cyanGhostImage);
        SyncGhostProperties(_redGhostImage);
        SyncGhostProperties(_coreGlowImage);

        // Calculate style-specific displacement & RGB separation
        float dispX = 0f;
        float dispY = 0f;
        float splitDistance = rgbSplitAmount * intensity * rgbFactor;

        switch (glitchType)
        {
            case GlitchType.HolographicProjector:
                // Subtle position drift while mesh strips tear horizontally
                dispX = (Random.value > 0.4f ? (Random.value * 2f - 1f) : 0f) * glitchIntensity * intensity * 8f;
                dispY = (Random.value > 0.7f ? (Random.value * 2f - 1f) : 0f) * glitchIntensity * intensity * 2f;
                break;

            case GlitchType.DigitalDisplacement:
                // High-frequency stepped digital jitter
                float rndStepX = (Random.value > 0.35f ? (Random.value * 2f - 1f) : 0f);
                float rndStepY = (Random.value > 0.75f ? (Random.value * 2f - 1f) : 0f);
                dispX = rndStepX * glitchIntensity * intensity * 16f;
                dispY = rndStepY * glitchIntensity * intensity * 4f;
                splitDistance *= 1.3f;
                break;

            case GlitchType.MatrixArtifactScan:
                // Fast alternating phase jumps
                float phase = (Mathf.Sin(_glitchTime * 20f) > 0f ? 1f : -1f);
                dispX = phase * glitchIntensity * intensity * 10f;
                dispY = (Random.value * 2f - 1f) * glitchIntensity * intensity * 2f;
                splitDistance *= 0.9f;
                break;

            case GlitchType.CyberpunkFlicker:
                // Voltage drop: rapid jitter + sudden position drop
                dispX = (Random.value > 0.5f ? (Random.value * 2f - 1f) : 0f) * glitchIntensity * intensity * 14f;
                dispY = (Random.value > 0.8f ? -6f : 0f) * glitchIntensity * intensity;
                splitDistance *= (Random.value > 0.3f ? 1.5f : 0.2f);
                break;

            case GlitchType.ChromaticAberrationOnly:
                // Clean: zero spatial displacement, pure RGB split
                dispX = 0f;
                dispY = 0f;
                break;
        }

        _rectTransform.anchoredPosition = _initialPosition + new Vector2(dispX, dispY);

        float splitJitter = (Random.value * 2f - 1f) * splitDistance * 0.25f;

        if (_cyanGhostRect != null)
        {
            _cyanGhostRect.anchoredPosition = new Vector2(splitDistance, splitJitter);
        }

        if (_redGhostRect != null)
        {
            Vector2 baseOffset = (glitchType == GlitchType.HolographicProjector && persistentDesyncGhost)
            ? desyncOffset
            : Vector2.zero;

            _redGhostRect.anchoredPosition = baseOffset + new Vector2(-splitDistance + (Random.value * 2f - 1f) * 6f * intensity, -splitJitter);
        }

        // Modulate ghost alphas
        if (_cyanGhostImage != null)
        {
            float targetA = cyanColor.a * Mathf.Clamp01(intensity * 1.15f);
            if (glitchType == GlitchType.CyberpunkFlicker && Random.value > 0.5f) targetA *= 1.4f;
            Color c = cyanColor;
            c.a = targetA;
            _cyanGhostImage.color = c;
        }

        if (_redGhostImage != null)
        {
            float targetA = (glitchType == GlitchType.HolographicProjector && persistentDesyncGhost)
            ? Mathf.Max(redColor.a * 0.6f, redColor.a * Mathf.Clamp01(intensity * 1.3f))
            : redColor.a * Mathf.Clamp01(intensity * 1.15f);
            if (glitchType == GlitchType.CyberpunkFlicker && Random.value > 0.5f) targetA *= 1.4f;
            Color r = redColor;
            r.a = targetA;
            _redGhostImage.color = r;
        }

        // Core glow for Holographic & Flicker modes
        if (_coreGlowImage != null && enableCoreGlow && (glitchType == GlitchType.HolographicProjector || glitchType == GlitchType.CyberpunkFlicker))
        {
            Color cg = coreGlowColor;
            cg.a = Mathf.Clamp01(coreGlowColor.a + intensity * 0.45f);
            _coreGlowImage.color = cg;
        }

        // Horizontal digital slices (for DigitalDisplacement and MatrixArtifactScan)
        if (_sliceObjs != null)
        {
            bool enableSlices = glitchType == GlitchType.DigitalDisplacement || glitchType == GlitchType.MatrixArtifactScan;
            float parentHeight = _rectTransform.rect.height;
            if (parentHeight <= 0.01f) parentHeight = 100f;

            for (int i = 0; i < _sliceObjs.Length; i++)
            {
                if (_sliceObjs[i] == null) continue;

                bool showSlice = enableSlices && (Random.value < (0.5f * intensity));
                _sliceObjs[i].SetActive(showSlice);

                if (showSlice && _sliceRects[i] != null)
                {
                    float sliceH = Random.Range(2f, 7f) * glitchIntensity;
                    float posY = Random.Range(-parentHeight * 0.45f, parentHeight * 0.45f);
                    float posX = (Random.value * 2f - 1f) * 14f * glitchIntensity;

                    _sliceRects[i].sizeDelta = new Vector2(0f, sliceH);
                    _sliceRects[i].anchoredPosition = new Vector2(posX, posY);
                }
            }
        }

        // Trigger mesh subdivision for sliced modes
        if (graphic != null && isActiveAndEnabled && (glitchType == GlitchType.HolographicProjector || glitchType == GlitchType.MatrixArtifactScan))
        {
            graphic.SetVerticesDirty();
        }
    }

    private void ResetToCleanState()
    {
        _currentGlitchIntensity = 0f;
        SetGlitchVisualsActive(false);

        if (_rectTransform != null)
        {
            _rectTransform.anchoredPosition = _initialPosition;
        }

        if (glitchType == GlitchType.HolographicProjector && IsShowing)
        {
            if (persistentDesyncGhost && _redGhostObj != null && _redGhostRect != null && _redGhostImage != null)
            {
                SyncGhostProperties(_redGhostImage);
                _redGhostObj.SetActive(true);
                _redGhostRect.anchoredPosition = desyncOffset;
                Color r = redColor;
                r.a = redColor.a * 0.6f;
                _redGhostImage.color = r;
            }

            if (enableCoreGlow && _coreGlowObj != null && _coreGlowImage != null)
            {
                SyncGhostProperties(_coreGlowImage);
                _coreGlowObj.SetActive(true);
                _coreGlowImage.color = coreGlowColor;
            }
        }

        if (graphic != null && isActiveAndEnabled)
        {
            graphic.SetVerticesDirty();
        }
    }

    private void StopAllRunningCoroutines()
    {
        if (_activeAnimationCoroutine != null)
        {
            StopCoroutine(_activeAnimationCoroutine);
            _activeAnimationCoroutine = null;
        }

        if (_idleLoopCoroutine != null)
        {
            StopCoroutine(_idleLoopCoroutine);
            _idleLoopCoroutine = null;
        }
    }

    #endregion

    #region BaseMeshEffect Implementation (Horizontal Digital Band Tears)

    public override void ModifyMesh(VertexHelper vh)
    {
        bool useMeshSlicing = (glitchType == GlitchType.HolographicProjector || glitchType == GlitchType.MatrixArtifactScan);

        if (!IsActive() || _currentGlitchIntensity <= 0.001f || !useMeshSlicing || horizontalBands <= 1)
        {
            return;
        }

        int bands = Mathf.Clamp(glitchType == GlitchType.MatrixArtifactScan ? horizontalBands * 2 : horizontalBands, 2, 36);

        if (vh.currentVertCount == 4)
        {
            UIVertex v0 = new UIVertex();
            UIVertex v1 = new UIVertex();
            UIVertex v2 = new UIVertex();
            UIVertex v3 = new UIVertex();

            vh.PopulateUIVertex(ref v0, 0);
            vh.PopulateUIVertex(ref v1, 1);
            vh.PopulateUIVertex(ref v2, 2);
            vh.PopulateUIVertex(ref v3, 3);

            vh.Clear();

            for (int i = 0; i < bands; i++)
            {
                float t0 = (float)i / bands;
                float t1 = (float)(i + 1) / bands;

                float y0 = Mathf.Lerp(v0.position.y, v1.position.y, t0);
                float y1 = Mathf.Lerp(v0.position.y, v1.position.y, t1);

                Vector2 uv0 = Vector2.Lerp(v0.uv0, v1.uv0, t0);
                Vector2 uv1 = Vector2.Lerp(v0.uv0, v1.uv0, t1);
                Vector2 uv2 = Vector2.Lerp(v3.uv0, v2.uv0, t1);
                Vector2 uv3 = Vector2.Lerp(v3.uv0, v2.uv0, t0);

                float dx;
                if (glitchType == GlitchType.MatrixArtifactScan)
                {
                    // Scanline alternating shift
                    dx = (i % 2 == 0 ? 1f : -1f) * bandDisplacement * _currentGlitchIntensity * glitchIntensity * 0.75f;
                }
                else
                {
                    // Holographic Perlin noise tear
                    float noiseVal = Mathf.PerlinNoise(i * 1.8f + _glitchTime, _glitchSeed);
                    float jitter = (noiseVal * 2f - 1f);
                    if ((i + Mathf.FloorToInt(_glitchTime)) % 3 == 0) jitter *= 1.6f;
                    dx = jitter * bandDisplacement * _currentGlitchIntensity * glitchIntensity;
                }

                Color32 c0 = v0.color;
                Color32 c1 = v1.color;
                Color32 c2 = v2.color;
                Color32 c3 = v3.color;

                if (_currentGlitchIntensity > 0.3f && (i % 2 == 1))
                {
                    byte dimA = (byte)(c0.a * 0.8f);
                    c0.a = dimA;
                    c1.a = dimA;
                    c2.a = dimA;
                    c3.a = dimA;
                }

                UIVertex b0 = v0; b0.position = new Vector3(v0.position.x + dx, y0, 0); b0.uv0 = uv0; b0.color = c0;
                UIVertex b1 = v1; b1.position = new Vector3(v1.position.x + dx, y1, 0); b1.uv0 = uv1; b1.color = c1;
                UIVertex b2 = v2; b2.position = new Vector3(v2.position.x + dx, y1, 0); b2.uv0 = uv2; b2.color = c2;
                UIVertex b3 = v3; b3.position = new Vector3(v3.position.x + dx, y0, 0); b3.uv0 = uv3; b3.color = c3;

                int vertStart = i * 4;
                vh.AddVert(b0);
                vh.AddVert(b1);
                vh.AddVert(b2);
                vh.AddVert(b3);

                vh.AddTriangle(vertStart + 0, vertStart + 1, vertStart + 2);
                vh.AddTriangle(vertStart + 2, vertStart + 3, vertStart + 0);
            }
        }
        else
        {
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                UIVertex vert = new UIVertex();
                vh.PopulateUIVertex(ref vert, i);

                float noise = Mathf.PerlinNoise(vert.position.y * 0.08f + _glitchTime, _glitchSeed);
                float dx = (noise * 2f - 1f) * bandDisplacement * _currentGlitchIntensity * glitchIntensity;
                vert.position.x += dx;

                vh.SetUIVertex(vert, i);
            }
        }
    }

    #endregion

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
