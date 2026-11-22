using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GemReviveManager : MonoBehaviour
{
    public static GemReviveManager Instance { get; private set; }

    [Header("Gem Configuration")]
    [SerializeField] private int startingGems = 3500;
    private const string GEMS_KEY = "PlayerGems_Total";

    [Header("Revive Settings")]
    public int baseReviveCost = 1;
    public int maxRevivesPerRun = 3;
    public float invulnerabilityDuration = 2.5f;
    public float clearHazardRadius = 20f;
    public float countdownDuration = 6.0f;

    [Header("UI Dialog References (Optional - Auto-Built if null)")]
    public GameObject reviveDialogCanvas;
    public TextMeshProUGUI gemsBalanceText;
    public TextMeshProUGUI reviveCostText;
    public TextMeshProUGUI countdownText;
    public Button reviveButton;
    public Button endRunButton;
    public Image countdownFillImage;

    private int revivesUsedInCurrentRun = 0;
    private Coroutine countdownCoroutine;
    private bool isReviving = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (!PlayerPrefs.HasKey(GEMS_KEY))
            {
                PlayerPrefs.SetInt(GEMS_KEY, startingGems);
                PlayerPrefs.Save();
            }
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        ResolveHierarchyReferences();
    }

    public void ResolveHierarchyReferences()
    {
        if (reviveDialogCanvas == null)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.isLoaded)
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    var trs = root.GetComponentsInChildren<Transform>(true);
                    foreach (var t in trs)
                    {
                        if (t.name == "GemReviveDialog" || t.name == "ReviveDialog" || t.name == "RevivePanel")
                        {
                            reviveDialogCanvas = t.gameObject;
                            break;
                        }
                    }
                    if (reviveDialogCanvas != null) break;
                }
            }

            if (reviveDialogCanvas == null)
            {
                var go = GameObject.Find("GemReviveDialog") ?? GameObject.Find("ReviveDialog") ?? GameObject.Find("RevivePanel");
                if (go != null)
                {
                    reviveDialogCanvas = go;
                }
            }
        }

        if (reviveDialogCanvas != null)
        {
            var allTexts = reviveDialogCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var txt in allTexts)
            {
                if (txt.name == "GemsInfo" || (txt.transform.parent != null && txt.transform.parent.name == "GemsInfo"))
                    gemsBalanceText = txt;
                else if (txt.name == "Countdown" || (txt.transform.parent != null && txt.transform.parent.name == "Countdown"))
                    countdownText = txt;
                else if (txt.name == "Text" && txt.transform.parent != null && txt.transform.parent.name == "BtnRevive")
                    reviveCostText = txt;
            }

            var allButtons = reviveDialogCanvas.GetComponentsInChildren<Button>(true);
            foreach (var btn in allButtons)
            {
                if (btn.name == "BtnRevive")
                {
                    reviveButton = btn;
                    reviveButton.onClick.RemoveAllListeners();
                    reviveButton.onClick.AddListener(OnReviveClicked);
                }
                else if (btn.name == "BtnEndRun" || btn.name == "BtnCancel")
                {
                    endRunButton = btn;
                    endRunButton.onClick.RemoveAllListeners();
                    endRunButton.onClick.AddListener(OnEndRunClicked);
                }
            }

            reviveDialogCanvas.SetActive(false);
        }
    }

    public int GetGems()
    {
        return PlayerPrefs.GetInt(GEMS_KEY, startingGems);
    }

    public void AddGems(int amount)
    {
        int current = GetGems();
        current += amount;
        PlayerPrefs.SetInt(GEMS_KEY, current);
        PlayerPrefs.Save();
        UpdateUI();
    }

    public bool SpendGems(int amount)
    {
        int current = GetGems();
        if (current >= amount)
        {
            current -= amount;
            PlayerPrefs.SetInt(GEMS_KEY, current);
            PlayerPrefs.Save();
            UpdateUI();
            return true;
        }
        return false;
    }

    public int GetCurrentReviveCost()
    {
        // 1st revive = 1, 2nd = 2, 3rd = 4 (doubling)
        return baseReviveCost * (int)Mathf.Pow(2, revivesUsedInCurrentRun);
    }

    public bool CanRevive()
    {
        if (revivesUsedInCurrentRun >= maxRevivesPerRun) return false;
        return GetGems() >= GetCurrentReviveCost();
    }

    public void ResetRunRevives()
    {
        revivesUsedInCurrentRun = 0;
        isReviving = false;
        if (reviveDialogCanvas != null) reviveDialogCanvas.SetActive(false);
    }

    /// <summary>
    /// Called when the player dies. Intercepts Game Over to offer the Gem Revive.
    /// </summary>
    public bool TryPromptRevive()
    {
        if (revivesUsedInCurrentRun >= maxRevivesPerRun)
        {
            return false; // Reached max revives
        }

        if (reviveDialogCanvas == null)
        {
            ResolveHierarchyReferences();
        }

        if (reviveDialogCanvas == null)
        {
            CreateRuntimeReviveUI();
        }

        if (reviveDialogCanvas != null)
        {
            reviveDialogCanvas.SetActive(true);
            UpdateUI();

            if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
            countdownCoroutine = StartCoroutine(ReviveCountdownRoutine());
            return true;
        }

        return false;
    }

    private IEnumerator ReviveCountdownRoutine()
    {
        float remaining = countdownDuration;
        while (remaining > 0f)
        {
            remaining -= Time.unscaledDeltaTime;
            if (countdownText != null)
            {
                countdownText.text = Mathf.CeilToInt(remaining).ToString();
            }
            if (countdownFillImage != null)
            {
                countdownFillImage.fillAmount = remaining / countdownDuration;
            }
            yield return null;
        }

        // Time ran out -> decline revive
        OnEndRunClicked();
    }

    public void OnReviveClicked()
    {
        if (isReviving) return;

        int cost = GetCurrentReviveCost();
        if (SpendGems(cost))
        {
            isReviving = true;
            revivesUsedInCurrentRun++;

            if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
            if (reviveDialogCanvas != null) reviveDialogCanvas.SetActive(false);

            ExecuteReviveOnPlayer();
        }
        else
        {
            // Not enough gems
            OnEndRunClicked();
        }
    }

    public void OnEndRunClicked()
    {
        if (isReviving) return;

        if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
        if (reviveDialogCanvas != null) reviveDialogCanvas.SetActive(false);

        // Continue to standard Game Over
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver();
        }
    }

    private void ExecuteReviveOnPlayer()
    {
        RunnerController runner = RunnerController.Instance;
        if (runner == null) return;

        // 1. Clear nearby obstacles and enemies
        Collider[] nearby = Physics.OverlapSphere(runner.transform.position, clearHazardRadius);
        foreach (var col in nearby)
        {
            if (col.CompareTag("Obstacle") || col.GetComponent<FomoBot>() != null)
            {
                col.gameObject.SetActive(false);
            }
        }

        // 2. Reposition player safely back to center of nearest lane
        Vector3 safePos = runner.transform.position;
        safePos.y = 0.1f;
        runner.transform.position = safePos;

        // 3. Reset runner state
        runner.RevivePlayer();

        // 4. Grant invulnerability
        StartCoroutine(InvulnerabilityRoutine(runner));

        // 5. Resume music & unfreeze
        if (SoundManager.Instance != null && SoundManager.Instance.musicSource != null)
        {
            if (!SoundManager.Instance.musicSource.isPlaying)
            {
                SoundManager.Instance.RestartMusic();
            }
        }

        isReviving = false;
    }

    private IEnumerator InvulnerabilityRoutine(RunnerController runner)
    {
        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.ActivatePowerUp(PowerUpType.Invincibility);
        }

        // Visual blinking on player renderers
        Renderer[] renderers = runner.GetComponentsInChildren<Renderer>();
        float elapsed = 0f;
        bool visible = true;
        while (elapsed < invulnerabilityDuration)
        {
            elapsed += 0.15f;
            visible = !visible;
            foreach (var r in renderers)
            {
                if (r != null) r.enabled = visible;
            }
            yield return new WaitForSeconds(0.15f);
        }

        foreach (var r in renderers)
        {
            if (r != null) r.enabled = true;
        }
    }

    private void UpdateUI()
    {
        if (gemsBalanceText != null)
        {
            gemsBalanceText.text = $"{GetGems():N0}";
        }
        if (reviveCostText != null)
        {
            int cost = GetCurrentReviveCost();
            reviveCostText.text = $"REVIVE ({cost} GEM{(cost > 1 ? "S" : "")})";
        }
        if (reviveButton != null)
        {
            reviveButton.interactable = CanRevive();
        }
    }

    private void CreateRuntimeReviveUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("ReviveCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 99;
            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
        }

        if (UnityEngine.EventSystems.EventSystem.current == null && FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        // Procedural Sci-Fi Revive Dialog overlay
        GameObject panelObj = new GameObject("GemReviveDialog", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = panelObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(500, 360);

        Image bg = panelObj.GetComponent<Image>();
        bg.color = new Color(0.04f, 0.08f, 0.16f, 0.95f);

        // Header Title
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(panelObj.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(0, 130);
        titleRt.sizeDelta = new Vector2(460, 45);
        TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
        titleText.text = "CONTINUE RUN?";
        titleText.fontSize = 32;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0f, 0.95f, 1f, 1f);

        // Countdown Text
        GameObject cdObj = new GameObject("Countdown", typeof(RectTransform), typeof(TextMeshProUGUI));
        cdObj.transform.SetParent(panelObj.transform, false);
        RectTransform cdRt = cdObj.GetComponent<RectTransform>();
        cdRt.anchoredPosition = new Vector2(0, 50);
        cdRt.sizeDelta = new Vector2(120, 60);
        countdownText = cdObj.GetComponent<TextMeshProUGUI>();
        countdownText.text = "6";
        countdownText.fontSize = 54;
        countdownText.fontStyle = FontStyles.Bold;
        countdownText.alignment = TextAlignmentOptions.Center;
        countdownText.color = Color.white;

        // Gems Info
        GameObject infoObj = new GameObject("GemsInfo", typeof(RectTransform), typeof(TextMeshProUGUI));
        infoObj.transform.SetParent(panelObj.transform, false);
        RectTransform infoRt = infoObj.GetComponent<RectTransform>();
        infoRt.anchoredPosition = new Vector2(0, -10);
        infoRt.sizeDelta = new Vector2(400, 35);
        gemsBalanceText = infoObj.GetComponent<TextMeshProUGUI>();
        gemsBalanceText.fontSize = 20;
        gemsBalanceText.alignment = TextAlignmentOptions.Center;
        gemsBalanceText.color = new Color(0.8f, 0.85f, 0.95f, 1f);

        // Revive Button
        GameObject btnReviveObj = new GameObject("BtnRevive", typeof(RectTransform), typeof(Image), typeof(Button));
        btnReviveObj.transform.SetParent(panelObj.transform, false);
        RectTransform btnReviveRt = btnReviveObj.GetComponent<RectTransform>();
        btnReviveRt.anchoredPosition = new Vector2(0, -70);
        btnReviveRt.sizeDelta = new Vector2(380, 55);
        Image btnReviveImg = btnReviveObj.GetComponent<Image>();
        btnReviveImg.color = new Color(0.05f, 0.8f, 0.4f, 1f);
        reviveButton = btnReviveObj.GetComponent<Button>();
        reviveButton.onClick.AddListener(OnReviveClicked);

        GameObject btnReviveTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnReviveTxtObj.transform.SetParent(btnReviveObj.transform, false);
        RectTransform btnReviveTxtRt = btnReviveTxtObj.GetComponent<RectTransform>();
        btnReviveTxtRt.sizeDelta = btnReviveRt.sizeDelta;
        reviveCostText = btnReviveTxtObj.GetComponent<TextMeshProUGUI>();
        reviveCostText.fontSize = 22;
        reviveCostText.fontStyle = FontStyles.Bold;
        reviveCostText.alignment = TextAlignmentOptions.Center;
        reviveCostText.color = Color.white;

        // End Run Button
        GameObject btnEndObj = new GameObject("BtnEndRun", typeof(RectTransform), typeof(Image), typeof(Button));
        btnEndObj.transform.SetParent(panelObj.transform, false);
        RectTransform btnEndRt = btnEndObj.GetComponent<RectTransform>();
        btnEndRt.anchoredPosition = new Vector2(0, -135);
        btnEndRt.sizeDelta = new Vector2(380, 45);
        Image btnEndImg = btnEndObj.GetComponent<Image>();
        btnEndImg.color = new Color(0.3f, 0.35f, 0.45f, 0.8f);
        endRunButton = btnEndObj.GetComponent<Button>();
        endRunButton.onClick.AddListener(OnEndRunClicked);

        GameObject btnEndTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnEndTxtObj.transform.SetParent(btnEndObj.transform, false);
        RectTransform btnEndTxtRt = btnEndTxtObj.GetComponent<RectTransform>();
        btnEndTxtRt.sizeDelta = btnEndRt.sizeDelta;
        TextMeshProUGUI endText = btnEndTxtObj.GetComponent<TextMeshProUGUI>();
        endText.text = "GIVE UP (END RUN)";
        endText.fontSize = 18;
        endText.alignment = TextAlignmentOptions.Center;
        endText.color = new Color(0.9f, 0.9f, 0.9f, 0.9f);

        reviveDialogCanvas = panelObj;
        reviveDialogCanvas.SetActive(false);
    }
}
