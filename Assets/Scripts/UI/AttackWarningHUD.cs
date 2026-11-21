using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Universal Red Screen Warning & Dodge Telegraph HUD.
/// Automatically creates and renders across all worlds whenever an enemy (FOMO Bot or Spaceship) charges its beam attack.
/// </summary>
public class AttackWarningHUD : MonoBehaviour
{
    private static AttackWarningHUD _instance;
    public static AttackWarningHUD Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<AttackWarningHUD>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("AttackWarningHUD_Universal");
                    _instance = go.AddComponent<AttackWarningHUD>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [Header("UI Elements")]
    public Canvas warningCanvas;
    public Image redVignetteImage;
    public GameObject bannerPanel;
    public TextMeshProUGUI alertTitleText;
    public TextMeshProUGUI alertSubtitleText;
    public TextMeshProUGUI dodgedText;

    private Coroutine activeWarningCoroutine;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        BuildUIIfMissing();
    }

    private void BuildUIIfMissing()
    {
        if (warningCanvas != null) return;

        // 1. Root Canvas
        warningCanvas = gameObject.GetComponent<Canvas>();
        if (warningCanvas == null) warningCanvas = gameObject.AddComponent<Canvas>();
        warningCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        warningCanvas.sortingOrder = 999; // Top-most HUD layer

        CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        // Ensure no raycaster blocks player swipe touches
        GraphicRaycaster raycaster = gameObject.GetComponent<GraphicRaycaster>();
        if (raycaster != null) raycaster.enabled = false;

        // 2. Red Screen Overlay / Vignette
        GameObject vignetteObj = new GameObject("RedVignette", typeof(RectTransform), typeof(Image));
        vignetteObj.transform.SetParent(transform, false);
        RectTransform vigRt = vignetteObj.GetComponent<RectTransform>();
        vigRt.anchorMin = Vector2.zero;
        vigRt.anchorMax = Vector2.one;
        vigRt.sizeDelta = Vector2.zero;

        redVignetteImage = vignetteObj.GetComponent<Image>();
        redVignetteImage.color = new Color(1f, 0.05f, 0.05f, 0f);
        redVignetteImage.raycastTarget = false;

        // 3. Top Danger Alert Banner
        bannerPanel = new GameObject("AlertBanner", typeof(RectTransform), typeof(Image));
        bannerPanel.transform.SetParent(transform, false);
        RectTransform bannerRt = bannerPanel.GetComponent<RectTransform>();
        bannerRt.anchorMin = new Vector2(0.5f, 1f);
        bannerRt.anchorMax = new Vector2(0.5f, 1f);
        bannerRt.pivot = new Vector2(0.5f, 1f);
        bannerRt.anchoredPosition = new Vector2(0f, -140f);
        bannerRt.sizeDelta = new Vector2(880f, 140f);

        Image bannerBg = bannerPanel.GetComponent<Image>();
        bannerBg.color = new Color(0.12f, 0.02f, 0.02f, 0.88f);
        bannerBg.raycastTarget = false;

        // 4. Alert Title Text
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(bannerPanel.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 0.5f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -5f);
        titleRt.sizeDelta = new Vector2(-20f, 0f);

        alertTitleText = titleObj.GetComponent<TextMeshProUGUI>();
        alertTitleText.text = "<color=#FF2222>▲ WARNING: BEAM ATTACK INCOMING ▲</color>";
        alertTitleText.fontSize = 32f;
        alertTitleText.fontStyle = FontStyles.Bold;
        alertTitleText.alignment = TextAlignmentOptions.Center;
        alertTitleText.raycastTarget = false;

        // 5. Alert Subtitle Text (Dodge Instruction)
        GameObject subObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subObj.transform.SetParent(bannerPanel.transform, false);
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0f, 0f);
        subRt.anchorMax = new Vector2(1f, 0.5f);
        subRt.anchoredPosition = new Vector2(0f, 5f);
        subRt.sizeDelta = new Vector2(-20f, 0f);

        alertSubtitleText = subObj.GetComponent<TextMeshProUGUI>();
        alertSubtitleText.text = "◄ SWIPE LEFT OR RIGHT TO DODGE! ►";
        alertSubtitleText.fontSize = 24f;
        alertSubtitleText.fontStyle = FontStyles.Bold;
        alertSubtitleText.color = new Color(1f, 0.85f, 0.2f, 1f);
        alertSubtitleText.alignment = TextAlignmentOptions.Center;
        alertSubtitleText.raycastTarget = false;

        // 6. Dodged Feedback Text
        GameObject dodgedObj = new GameObject("DodgedText", typeof(RectTransform), typeof(TextMeshProUGUI));
        dodgedObj.transform.SetParent(transform, false);
        RectTransform dodgedRt = dodgedObj.GetComponent<RectTransform>();
        dodgedRt.anchorMin = new Vector2(0.5f, 0.5f);
        dodgedRt.anchorMax = new Vector2(0.5f, 0.5f);
        dodgedRt.anchoredPosition = new Vector2(0f, 180f);
        dodgedRt.sizeDelta = new Vector2(600f, 90f);

        dodgedText = dodgedObj.GetComponent<TextMeshProUGUI>();
        dodgedText.text = "<color=#00FFCC>DODGED!</color>";
        dodgedText.fontSize = 46f;
        dodgedText.fontStyle = FontStyles.Bold;
        dodgedText.alignment = TextAlignmentOptions.Center;
        dodgedText.raycastTarget = false;
        dodgedText.gameObject.SetActive(false);

        bannerPanel.SetActive(false);
    }

    /// <summary>
    /// Triggers the 1-2 second red screen warning animation when an attack is about to happen.
    /// </summary>
    public void TriggerWarning(float duration = 1.6f)
    {
        BuildUIIfMissing();

        if (activeWarningCoroutine != null)
        {
            StopCoroutine(activeWarningCoroutine);
        }

        activeWarningCoroutine = StartCoroutine(WarningRoutine(duration));
    }

    private IEnumerator WarningRoutine(float duration)
    {
        if (bannerPanel != null) bannerPanel.SetActive(true);
        if (dodgedText != null) dodgedText.gameObject.SetActive(false);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            // Pulsing red wave between 0.15 and 0.48 alpha at 5Hz
            float alpha = Mathf.Lerp(0.12f, 0.48f, Mathf.PingPong(elapsed * 5.0f, 1f));

            if (redVignetteImage != null)
            {
                redVignetteImage.color = new Color(1f, 0.05f, 0.05f, alpha);
            }

            // Pulse banner scale slightly for urgency
            if (bannerPanel != null)
            {
                float scale = 1.0f + 0.03f * Mathf.Sin(elapsed * 12.0f);
                bannerPanel.transform.localScale = new Vector3(scale, scale, 1f);
            }

            yield return null;
        }

        // Fade out warning
        float fade = 0.2f;
        float fadeElapsed = 0f;
        Color startColor = redVignetteImage != null ? redVignetteImage.color : Color.clear;

        while (fadeElapsed < fade)
        {
            fadeElapsed += Time.deltaTime;
            float t = fadeElapsed / fade;
            if (redVignetteImage != null)
            {
                redVignetteImage.color = Color.Lerp(startColor, new Color(1f, 0.05f, 0.05f, 0f), t);
            }
            yield return null;
        }

        if (bannerPanel != null) bannerPanel.SetActive(false);
        activeWarningCoroutine = null;
    }

    /// <summary>
    /// Dismisses active red screen warning without playing dodged animation.
    /// </summary>
    public void DismissWarning()
    {
        if (activeWarningCoroutine != null)
        {
            StopCoroutine(activeWarningCoroutine);
            activeWarningCoroutine = null;
        }

        if (bannerPanel != null) bannerPanel.SetActive(false);
        if (redVignetteImage != null) redVignetteImage.color = new Color(1f, 0.05f, 0.05f, 0f);
    }

    /// <summary>
    /// Displays a momentary positive feedback when player successfully swipes out of the targeted beam lane.
    /// </summary>
    public void ShowDodged()
    {
        BuildUIIfMissing();

        if (activeWarningCoroutine != null)
        {
            StopCoroutine(activeWarningCoroutine);
            activeWarningCoroutine = null;
        }

        if (bannerPanel != null) bannerPanel.SetActive(false);
        if (redVignetteImage != null) redVignetteImage.color = new Color(1f, 0.05f, 0.05f, 0f);

        StartCoroutine(DodgedRoutine());
    }

    private IEnumerator DodgedRoutine()
    {
        if (dodgedText != null)
        {
            dodgedText.gameObject.SetActive(true);
            dodgedText.alpha = 1f;

            float elapsed = 0f;
            float showTime = 0.7f;
            while (elapsed < showTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / showTime;
                dodgedText.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }

            dodgedText.gameObject.SetActive(false);
        }
    }

    public void Dismiss()
    {
        if (activeWarningCoroutine != null)
        {
            StopCoroutine(activeWarningCoroutine);
            activeWarningCoroutine = null;
        }

        if (bannerPanel != null) bannerPanel.SetActive(false);
        if (dodgedText != null) dodgedText.gameObject.SetActive(false);
        if (redVignetteImage != null) redVignetteImage.color = new Color(1f, 0.05f, 0.05f, 0f);
    }
}
