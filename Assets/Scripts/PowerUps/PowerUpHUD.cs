using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PowerUpHUD : MonoBehaviour
{
    private static PowerUpHUD _instance;
    public static PowerUpHUD Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindAnyObjectByType<PowerUpHUD>();
                if (_instance == null)
                {
                    Canvas canvas = Object.FindAnyObjectByType<Canvas>();
                    if (canvas != null)
                    {
                        GameObject go = new GameObject("PowerUpHUD");
                        go.transform.SetParent(canvas.transform, false);
                        _instance = go.AddComponent<PowerUpHUD>();
                    }
                }
            }
            return _instance;
        }
        private set { _instance = value; }
    }

    [Header("Power Bar Panel Reference")]
    public GameObject powerBarPanel;
    public Image powerFillBar;
    public TextMeshProUGUI powerBarText;
    public Image powerBarIcon;

    // Toast Pop-up Reference
    private GameObject activeToastObj;
    private Coroutine activeToastRoutine;
    private bool isSubscribed = false;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        EnsurePowerBarPanel();
        SubscribeToManager();
    }

    private void Start()
    {
        if (powerBarPanel != null)
        {
            powerBarPanel.SetActive(false);
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

    private void EnsurePowerBarPanel()
    {
        if (powerBarPanel == null)
        {
            GameObject found = GameObject.Find("PowerBar_Panel");
            if (found != null)
            {
                powerBarPanel = found;
            }
        }

        if (powerBarPanel == null)
        {
            GameObject distPanel = GameObject.Find("Distance_Panel");
            if (distPanel != null)
            {
                powerBarPanel = Instantiate(distPanel, distPanel.transform.parent);
                powerBarPanel.name = "PowerBar_Panel";

                RectTransform rt = powerBarPanel.GetComponent<RectTransform>();
                RectTransform distRt = distPanel.GetComponent<RectTransform>();
                rt.anchorMin = distRt.anchorMin;
                rt.anchorMax = distRt.anchorMax;
                rt.pivot = distRt.pivot;
                rt.sizeDelta = new Vector2(580f, 85f);
                rt.anchoredPosition = new Vector2(0f, distRt.anchoredPosition.y - 135f);

                // Rename text child
                Transform txt = powerBarPanel.transform.Find("DistanceText");
                if (txt != null)
                {
                    txt.name = "PowerBarText";
                    powerBarText = txt.GetComponent<TextMeshProUGUI>();
                    if (powerBarText != null)
                    {
                        powerBarText.text = "Power Bar";
                        powerBarText.fontSize = 38f;
                        powerBarText.alignment = TextAlignmentOptions.MidlineLeft;
                        RectTransform trt = txt.GetComponent<RectTransform>();
                        trt.anchoredPosition = new Vector2(160f, 8f);
                        trt.sizeDelta = new Vector2(380f, 60f);
                    }
                }

                // Add draining fill bar inside the duplicate panel
                GameObject fillObj = new GameObject("PowerFillBar", typeof(RectTransform), typeof(Image));
                fillObj.transform.SetParent(powerBarPanel.transform, false);
                RectTransform frt = fillObj.GetComponent<RectTransform>();
                frt.anchorMin = new Vector2(0.06f, 0.12f);
                frt.anchorMax = new Vector2(0.94f, 0.28f);
                frt.offsetMin = Vector2.zero;
                frt.offsetMax = Vector2.zero;

                powerFillBar = fillObj.GetComponent<Image>();
                powerFillBar.type = Image.Type.Filled;
                powerFillBar.fillMethod = Image.FillMethod.Horizontal;
                powerFillBar.fillOrigin = (int)Image.OriginHorizontal.Left;
                powerFillBar.fillAmount = 1f;
                powerFillBar.color = new Color(0f, 0.85f, 1f, 0.95f);
            }
        }

        if (powerBarPanel != null)
        {
            RectTransform pbrt = powerBarPanel.GetComponent<RectTransform>();
            if (pbrt != null)
            {
                pbrt.anchoredPosition = new Vector2(0f, pbrt.anchoredPosition.y);
                pbrt.sizeDelta = new Vector2(580f, 85f);
            }

            if (powerBarText == null)
            {
                Transform txt = powerBarPanel.transform.Find("PowerBarText");
                if (txt != null) powerBarText = txt.GetComponent<TextMeshProUGUI>();
            }

            if (powerFillBar == null)
            {
                Transform bar = powerBarPanel.transform.Find("PowerFillBar");
                if (bar != null) powerFillBar = bar.GetComponent<Image>();
            }

            if (powerBarIcon == null)
            {
                Transform icon = powerBarPanel.transform.Find("TPIcon");
                if (icon != null) powerBarIcon = icon.GetComponent<Image>();
            }
        }
    }

    private void HandlePowerUpUpdated(PowerUpType type, float remaining, float duration)
    {
        if (powerBarPanel == null)
        {
            EnsurePowerBarPanel();
        }

        if (powerBarPanel != null)
        {
            if (!powerBarPanel.activeSelf)
            {
                powerBarPanel.SetActive(true);
            }

            string nameString = (type == PowerUpType.SpeedBoost) ? "2X SPEED BOOST" : "SHIELD";
            if (powerBarText != null)
            {
                powerBarText.text = $"{nameString} ({remaining:F1}s)";
            }

            float fill = Mathf.Clamp01(remaining / duration);
            if (powerFillBar != null)
            {
                powerFillBar.fillAmount = fill;
                powerFillBar.color = (type == PowerUpType.SpeedBoost)
                    ? new Color(1f, 0.45f, 0.05f, 0.95f)
                    : new Color(0f, 0.85f, 1f, 0.95f);
            }

            if (powerBarIcon != null)
            {
                powerBarIcon.color = (type == PowerUpType.SpeedBoost)
                    ? new Color(1f, 0.65f, 0.15f)
                    : new Color(0.2f, 0.9f, 1f);
            }
        }
    }

    private void HandlePowerUpDeactivated(PowerUpType type)
    {
        if (powerBarPanel != null)
        {
            powerBarPanel.SetActive(false);
        }
    }

    public void ShowPowerUpAnnouncement(PowerUpType type)
    {
        string title = (type == PowerUpType.SpeedBoost) ? ">> 2X SPEED BOOST ACTIVATED! <<" : ">> SHIELD FORCEFIELD ACTIVATED! <<";
        Color col = (type == PowerUpType.SpeedBoost) ? new Color(1f, 0.5f, 0.05f) : new Color(0.1f, 0.85f, 1f);
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
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        if (activeToastObj != null)
        {
            Destroy(activeToastObj);
        }

        activeToastObj = new GameObject("AnnouncementToast", typeof(RectTransform), typeof(CanvasGroup));
        activeToastObj.transform.SetParent(canvas.transform, false);

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

        if (activeToastRoutine != null) StopCoroutine(activeToastRoutine);
        activeToastRoutine = StartCoroutine(ToastAnimationRoutine(activeToastObj));
    }

    private IEnumerator ToastAnimationRoutine(GameObject toast)
    {
        if (toast == null) yield break;

        CanvasGroup cg = toast.GetComponent<CanvasGroup>();
        Transform t = toast.transform;

        // Punch pop-in
        t.localScale = Vector3.one * 1.3f;
        float elapsed = 0f;
        while (elapsed < 0.18f)
        {
            if (toast == null) yield break;
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
            if (toast == null) yield break;
            elapsed += Time.unscaledDeltaTime;
            if (cg != null) cg.alpha = Mathf.Lerp(1f, 0f, elapsed / 0.4f);
            yield return null;
        }

        if (toast != null) Destroy(toast);
    }
}
