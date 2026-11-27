using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Power-up HUD. Shows the active power-up as a compact bottom-left card:
/// a rounded, color-coded icon tile with a depleting countdown ring, the
/// power-up name and a short "active" caption. Styled after Subway Surfers.
/// </summary>
public class PowerUpHUD : MonoBehaviour
{
    private static PowerUpHUD _instance;
    public static PowerUpHUD Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindAnyObjectByType<PowerUpHUD>(FindObjectsInactive.Include);
                if (_instance == null)
                {
                    Canvas canvas = FindGameCanvas();
                    if (canvas != null)
                    {
                        GameObject go = new GameObject("PowerUpHUD");
                        go.transform.SetParent(canvas.transform, false);
                        _instance = go.AddComponent<PowerUpHUD>();
                    }
                    else
                    {
                        GameObject go = new GameObject("PowerUpHUD");
                        _instance = go.AddComponent<PowerUpHUD>();
                        DontDestroyOnLoad(go);
                    }
                }
            }
            if (_instance != null && !_instance.gameObject.activeSelf)
            {
                _instance.gameObject.SetActive(true);
            }
            return _instance;
        }
        private set { _instance = value; }
    }

    [Header("Legacy Power Bar References (kept for scene compatibility, not used)")]
    public GameObject powerBarPanel;
    public Image powerFillBar;
    public TextMeshProUGUI powerBarText;
    public Image powerBarIcon;

    [Header("Bottom-Left Card Layout")]
    public Vector2 cardSize = new Vector2(430f, 132f);
    public Vector2 cardOffset = new Vector2(56f, 64f);

    // Card widgets
    private RectTransform cardRoot;
    private CanvasGroup cardGroup;
    private Image ringBg;
    private Image ringGlow;
    private Image ringFill;
    private Image tileOuter;
    private Image tileInner;
    private Image underline;
    private Image plate;
    private Image plateEdge;
    private TextMeshProUGUI numberText;
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI subtitleText;

    // Runtime state
    private PowerUpType activeType;
    private float activeRemaining;
    private float activeDuration = 1f;
    private bool shieldMode;
    private float animTime = 999f;
    private bool isBuilt;

    // Toast Pop-up Reference
    private GameObject activeToastObj;
    private Coroutine activeToastRoutine;
    private bool isSubscribed = false;

    // Cached art
    private static Sprite _roundedSprite;
    private static Sprite _ringSprite;
    private static TMP_FontAsset _hudFont;

    /// <summary>
    /// Clears statics that would otherwise survive a play session because the project
    /// disables domain reload on entering play mode.
    /// </summary>
    public static void ResetStaticsOnPlay()
    {
        _instance = null;
        _roundedSprite = null;
        _ringSprite = null;
        _hudFont = null;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        EnsureHud();
        SubscribeToManager();
    }

    private void Start()
    {
        if (cardRoot != null)
        {
            cardRoot.gameObject.SetActive(false);
        }

        SubscribeToManager();
    }

    private void SubscribeToManager()
    {
        if (!isSubscribed && PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.OnPowerUpUpdated += HandlePowerUpUpdated;
            PowerUpManager.Instance.OnPowerUpDeactivated += HandlePowerUpDeactivated;
            isSubscribed = true;
        }
    }

    private void OnDestroy()
    {
        if (isSubscribed && PowerUpManager.HasInstance)
        {
            PowerUpManager.Instance.OnPowerUpUpdated -= HandlePowerUpUpdated;
            PowerUpManager.Instance.OnPowerUpDeactivated -= HandlePowerUpDeactivated;
            isSubscribed = false;
        }
    }

    private void Update()
    {
        if (cardRoot == null || !cardRoot.gameObject.activeSelf) return;

        // Hide the card while the run is over so it does not sit behind the results screen.
        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
        {
            cardRoot.gameObject.SetActive(false);
            return;
        }

        float dt = Time.unscaledDeltaTime;

        // Pop-in + slide-in from the left
        if (animTime < 0.4f)
        {
            animTime += dt;
            float t = Mathf.Clamp01(animTime / 0.26f);
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            float overshoot = Mathf.Sin(t * Mathf.PI) * 0.07f;
            cardRoot.localScale = Vector3.one * (Mathf.Lerp(0.78f, 1f, ease) + overshoot);
            if (cardGroup != null) cardGroup.alpha = Mathf.Clamp01(t * 2.5f);
            cardRoot.anchoredPosition = new Vector2(cardOffset.x - (1f - ease) * 46f, cardOffset.y);
        }
        else
        {
            cardRoot.localScale = Vector3.one;
            if (cardGroup != null) cardGroup.alpha = 1f;
            cardRoot.anchoredPosition = cardOffset;
        }

        // Urgency pulse during the last 2 seconds
        bool urgent = !shieldMode && activeRemaining > 0f && activeRemaining <= 2f;
        if (urgent)
        {
            float pulse = (Mathf.Sin(Time.unscaledTime * 11f) + 1f) * 0.5f;
            float s = 1f + pulse * 0.035f;
            cardRoot.localScale = new Vector3(s, s, 1f);
            if (ringFill != null)
            {
                Color c = ringFill.color;
                c.a = Mathf.Lerp(0.55f, 1f, pulse);
                ringFill.color = c;
            }
        }
        else if (ringFill != null)
        {
            Color c = ringFill.color;
            c.a = 1f;
            ringFill.color = c;
        }
    }

    // ------------------------------------------------------------------
    // Construction
    // ------------------------------------------------------------------

    private void EnsureHud()
    {
        if (isBuilt) return;

        Canvas canvas = FindGameCanvas();
        if (canvas == null) return;

        HideLegacyBar();

        GameObject root = new GameObject("PowerUpHUD_Card", typeof(RectTransform), typeof(CanvasGroup));
        root.transform.SetParent(canvas.transform, false);
        root.transform.SetAsLastSibling();

        cardRoot = root.GetComponent<RectTransform>();
        cardRoot.anchorMin = new Vector2(0f, 0f);
        cardRoot.anchorMax = new Vector2(0f, 0f);
        cardRoot.pivot = new Vector2(0f, 0f);
        cardRoot.anchoredPosition = cardOffset;
        cardRoot.sizeDelta = cardSize;
        cardRoot.localScale = Vector3.one * 0.78f;

        cardGroup = root.GetComponent<CanvasGroup>();
        cardGroup.alpha = 0f;
        cardGroup.interactable = false;
        cardGroup.blocksRaycasts = false;

        Color darkTile = new Color(0.025f, 0.045f, 0.085f, 0.94f);
        Color plateColor = new Color(0.02f, 0.035f, 0.07f, 0.55f);
        Color captionColor = new Color(0.72f, 0.84f, 0.96f, 0.85f);

        // Soft colored halo behind the card
        plateEdge = CreateImage("CardHalo", cardRoot, GetRoundedSprite(), new Vector2(148f, 14f), new Vector2(288f, 116f), new Color(1f, 1f, 1f, 0.07f));

        // Dark plate behind the text column so it reads over the track
        plate = CreateImage("TextPlate", cardRoot, GetRoundedSprite(), new Vector2(148f, 14f), new Vector2(276f, 110f), plateColor);

        // Countdown ring (soft glow, dark track, then the draining fill)
        ringGlow = CreateImage("RingGlow", cardRoot, GetRingSprite(), new Vector2(-8f, -8f), new Vector2(148f, 148f), new Color(0.35f, 0.75f, 1f, 0.18f));

        ringBg = CreateImage("RingBG", cardRoot, GetRingSprite(), new Vector2(0f, 0f), new Vector2(132f, 132f), new Color(0.01f, 0.02f, 0.05f, 0.55f));

        ringFill = CreateImage("RingFill", cardRoot, GetRingSprite(), new Vector2(0f, 0f), new Vector2(132f, 132f), new Color(0.35f, 0.75f, 1f, 1f));
        ringFill.type = Image.Type.Filled;
        ringFill.fillMethod = Image.FillMethod.Radial360;
        ringFill.fillOrigin = (int)Image.Origin360.Top;
        ringFill.fillClockwise = true;
        ringFill.fillAmount = 1f;

        // Icon tile
        tileOuter = CreateImage("TileOuter", cardRoot, GetRoundedSprite(), new Vector2(20f, 20f), new Vector2(92f, 92f), new Color(0.35f, 0.75f, 1f, 0.95f));
        tileInner = CreateImage("TileInner", cardRoot, GetRoundedSprite(), new Vector2(25f, 25f), new Vector2(82f, 82f), darkTile);

        // Countdown number inside the tile
        numberText = CreateText("TimerText", cardRoot, new Vector2(25f, 25f), new Vector2(82f, 82f), 40f, TextAlignmentOptions.Center, Color.white);
        numberText.fontStyle = FontStyles.Bold;

        // Name + caption
        nameText = CreateText("NameText", cardRoot, new Vector2(158f, 76f), new Vector2(252f, 42f), 31f, TextAlignmentOptions.Left, Color.white);
        nameText.fontStyle = FontStyles.Bold;
        nameText.characterSpacing = 2f;

        underline = CreateImage("AccentLine", cardRoot, GetRoundedSprite(), new Vector2(158f, 66f), new Vector2(178f, 5f), new Color(0.35f, 0.75f, 1f, 1f));

        subtitleText = CreateText("CaptionText", cardRoot, new Vector2(158f, 34f), new Vector2(252f, 24f), 15f, TextAlignmentOptions.Left, captionColor);
        subtitleText.characterSpacing = 3f;

        root.SetActive(false);
        isBuilt = true;
    }

    private static Canvas FindGameCanvas()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].name == "GameUI" && canvases[i].isActiveAndEnabled) return canvases[i];
        }
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].renderMode == RenderMode.ScreenSpaceOverlay && canvases[i].isActiveAndEnabled) return canvases[i];
        }
        return null;
    }

    private void HideLegacyBar()
    {
        if (powerBarPanel != null)
        {
            powerBarPanel.SetActive(false);
        }

        GameObject[] all = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < all.Length; i++)
        {
            GameObject go = all[i];
            if (go == null || go.name != "PowerBar_Panel") continue;
            if (!go.scene.IsValid()) continue;
            go.SetActive(false);
        }
    }

    // ------------------------------------------------------------------
    // Updates from the manager
    // ------------------------------------------------------------------

    private void HandlePowerUpUpdated(PowerUpType type, float remaining, float duration)
    {
        EnsureHud();
        if (cardRoot == null) return;

        bool typeChanged = !cardRoot.gameObject.activeSelf || type != activeType;

        activeType = type;
        activeRemaining = Mathf.Max(0f, remaining);
        activeDuration = Mathf.Max(0.0001f, duration);
        shieldMode = (type == PowerUpType.Shield);

        if (!cardRoot.gameObject.activeSelf)
        {
            cardRoot.gameObject.SetActive(true);
        }

        ApplyVisuals();

        if (typeChanged)
        {
            animTime = 0f;
            cardRoot.localScale = Vector3.one * 0.78f;
            if (cardGroup != null) cardGroup.alpha = 0f;
        }
    }

    private void HandlePowerUpDeactivated(PowerUpType type)
    {
        if (cardRoot != null)
        {
            cardRoot.gameObject.SetActive(false);
        }
    }

    private void ApplyVisuals()
    {
        Color typeColor = PowerUpPickup.GetColorForType(activeType);

        if (ringFill != null)
        {
            ringFill.color = typeColor;
            ringFill.fillAmount = shieldMode ? 1f : Mathf.Clamp01(activeRemaining / activeDuration);
        }
        if (tileOuter != null) tileOuter.color = new Color(typeColor.r, typeColor.g, typeColor.b, 0.95f);
        if (underline != null) underline.color = typeColor;
        if (ringGlow != null) ringGlow.color = new Color(typeColor.r, typeColor.g, typeColor.b, 0.18f);
        if (plateEdge != null) plateEdge.color = new Color(typeColor.r, typeColor.g, typeColor.b, 0.10f);
        if (plate != null)
        {
            plate.color = Color.Lerp(new Color(0.02f, 0.035f, 0.07f, 0.62f),
                new Color(typeColor.r, typeColor.g, typeColor.b, 0.62f), 0.12f);
        }

        if (numberText != null)
        {
            numberText.text = shieldMode ? "1" : Mathf.CeilToInt(activeRemaining).ToString();
            numberText.color = Color.white;
        }
        if (nameText != null)
        {
            nameText.text = PowerUpPickup.GetDisplayName(activeType);
        }
        if (subtitleText != null)
        {
            subtitleText.text = shieldMode ? "1 HIT SHIELD" : "ACTIVE POWER-UP";
        }
    }

    // ------------------------------------------------------------------
    // UI factory helpers
    // ------------------------------------------------------------------

    private static Image CreateImage(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.alignment = align;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;

        TMP_FontAsset font = ResolveFont();
        if (font != null) tmp.font = font;

        return tmp;
    }

    private static TMP_FontAsset ResolveFont()
    {
        if (_hudFont != null) return _hudFont;

        TextMeshProUGUI[] texts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].font != null && texts[i].font.name.Contains("Exo"))
            {
                _hudFont = texts[i].font;
                return _hudFont;
            }
        }

        if (TMP_Settings.defaultFontAsset != null) _hudFont = TMP_Settings.defaultFontAsset;
        return _hudFont;
    }

    private static Sprite GetRoundedSprite()
    {
        if (_roundedSprite != null) return _roundedSprite;

        const int size = 96;
        const int radius = 26;
        float half = size * 0.5f;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = "PowerUpHUD_Rounded";
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float dx = Mathf.Abs(px - half) - (half - radius);
                float dy = Mathf.Abs(py - half) - (half - radius);
                float outside = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f));
                float inside = Mathf.Min(Mathf.Max(dx, dy), 0f);
                float dist = outside + inside - radius;
                float alpha = Mathf.Clamp01(0.5f - dist);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        _roundedSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        _roundedSprite.name = "PowerUpHUD_Rounded";
        return _roundedSprite;
    }

    private static Sprite GetRingSprite()
    {
        if (_ringSprite != null) return _ringSprite;

        const int size = 128;
        const float thickness = 9f;
        float half = size * 0.5f;
        float outer = half - 1f;
        float inner = outer - thickness;
        float mid = (outer + inner) * 0.5f;
        float halfThickness = thickness * 0.5f;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = "PowerUpHUD_Ring";
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float dist = Mathf.Sqrt((px - half) * (px - half) + (py - half) * (py - half));
                float alpha = Mathf.Clamp01(halfThickness - Mathf.Abs(dist - mid) + 0.5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        _ringSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        _ringSprite.name = "PowerUpHUD_Ring";
        return _ringSprite;
    }

    // ------------------------------------------------------------------
    // Announcement toast (unchanged behaviour)
    // ------------------------------------------------------------------

    public void ShowPowerUpAnnouncement(PowerUpType type)
    {
        string title = (type == PowerUpType.Shield)
            ? ">> SHIELD FORCEFIELD ACTIVATED! <<"
            : $">> {PowerUpPickup.GetDisplayName(type)} ACTIVATED! <<";
        Color col = PowerUpPickup.GetColorForType(type);
        CreateAnnouncementToast(title, col);
    }

    public void ShowPortalAnnouncement(string message)
    {
        string title = ">> " + message;
        Color col = new Color(0.2f, 0.9f, 1f);
        CreateAnnouncementToast(title, col);
    }

    private void CreateAnnouncementToast(string text, Color bannerColor)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        Canvas canvas = FindGameCanvas();
        if (canvas == null) return;

        if (activeToastObj != null)
        {
            Destroy(activeToastObj);
        }

        activeToastObj = new GameObject("AnnouncementToast", typeof(RectTransform), typeof(CanvasGroup));
        activeToastObj.transform.SetParent(canvas.transform, false);
        activeToastObj.SetActive(true);
        activeToastObj.transform.SetAsLastSibling();

        RectTransform rt = activeToastObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.82f);
        rt.anchorMax = new Vector2(0.5f, 0.82f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(560f, 48f);
        rt.anchoredPosition = Vector2.zero;

        // Dark sci-fi backplate
        Image bg = activeToastObj.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.07f, 0.12f, 0.92f);

        // Top accent line
        GameObject line = new GameObject("AccentLine", typeof(RectTransform), typeof(Image));
        line.transform.SetParent(activeToastObj.transform, false);
        RectTransform lrt = line.GetComponent<RectTransform>();
        lrt.anchorMin = new Vector2(0f, 1f);
        lrt.anchorMax = new Vector2(1f, 1f);
        lrt.pivot = new Vector2(0.5f, 1f);
        lrt.sizeDelta = new Vector2(0f, 3f);
        lrt.anchoredPosition = Vector2.zero;
        line.GetComponent<Image>().color = bannerColor;

        // Text
        GameObject txtObj = new GameObject("ToastText", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(activeToastObj.transform, false);
        RectTransform trt = txtObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.sizeDelta = Vector2.zero;
        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 24f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = bannerColor;
        TMP_FontAsset font = ResolveFont();
        if (font != null) tmp.font = font;

        // Run animation directly on active toast GameObject component to guarantee active coroutine execution
        ToastAnimationRunner runner = activeToastObj.AddComponent<ToastAnimationRunner>();
        runner.StartAnimation(activeToastObj.GetComponent<CanvasGroup>());
    }

    public class ToastAnimationRunner : MonoBehaviour
    {
        public void StartAnimation(CanvasGroup cg)
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(ToastAnimationRoutine(cg));
            }
        }

        private IEnumerator ToastAnimationRoutine(CanvasGroup cg)
        {
            Transform t = transform;

            // Punch pop-in
            t.localScale = Vector3.one * 1.3f;
            float elapsed = 0f;
            while (elapsed < 0.18f)
            {
                elapsed += Time.unscaledDeltaTime;
                t.localScale = Vector3.Lerp(Vector3.one * 1.3f, Vector3.one, elapsed / 0.18f);
                yield return null;
            }

            // Hold visible
            yield return new WaitForSecondsRealtime(1.8f);

            // Fade out
            elapsed = 0f;
            while (elapsed < 0.4f)
            {
                elapsed += Time.unscaledDeltaTime;
                if (cg != null) cg.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.4f);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
