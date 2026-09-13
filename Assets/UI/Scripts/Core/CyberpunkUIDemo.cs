using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SciFiUI.Effects;

namespace SciFiUI.Core
{
    /// <summary>
    /// Unified Cyberpunk UI Transition & Interactive Demo Manager.
    /// Controls screen transitions across MainMenu, GameHUD, MarketAnalysis, GameOver, and Settings.
    /// Manages the in-game Cyberpunk Demo Toolbar to switch screens, toggle visual effects,
    /// and live-select between the 5 distinct glitch algorithms via dropdown/buttons.
    /// </summary>
    public class CyberpunkUIDemo : MonoBehaviour
    {
        public static CyberpunkUIDemo Instance { get; private set; }

        [Header("Screens")]
        [SerializeField] private RectTransform mainMenuOverlay;
        [SerializeField] private RectTransform gameHUDOverlay;
        [SerializeField] private RectTransform marketAnalysisPanel;
        [SerializeField] private RectTransform gameOverPanel;
        [SerializeField] private RectTransform settingsPanel;

        [Header("Transition Settings")]
        [SerializeField] private float transitionDuration = 0.28f;
        [SerializeField] private bool useScalePunch = true;
        [SerializeField] private Vector3 punchScale = new Vector3(0.92f, 0.92f, 1f);

        [Header("Target Demo Glitch & Effect Components")]
        [Tooltip("Primary glitch targets in the scene to showcase the glitch types.")]
        [SerializeField] private List<CyberpunkUIGlitch> glitchTargets = new List<CyberpunkUIGlitch>();

        [Tooltip("Primary effect targets in the scene (buttons/badges with shine and glow).")]
        [SerializeField] private List<CyberpunkUIEffects> effectTargets = new List<CyberpunkUIEffects>();

        [Header("Interactive Demo Toolbar")]
        [SerializeField] private bool showDemoToolbar = true;

        private RectTransform _currentActiveScreen;
        private Coroutine _activeTransitionCoroutine;
        private GameObject _demoToolbarGo;
        private TMP_Dropdown _glitchDropdown;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            AutoDiscoverScreens();
            AutoDiscoverEffects();
        }

        private void Start()
        {
            // Initial state: Start clean on Main Menu
            ShowMainMenu(true);

            if (showDemoToolbar)
            {
                CreateDemoToolbar();
            }
        }

        #region Screen Transitions

        public void ShowMainMenu(bool immediate = false) => TransitionTo(mainMenuOverlay, immediate);
        public void ShowGameHUD(bool immediate = false) => TransitionTo(gameHUDOverlay, immediate);
        public void ShowMarketAnalysis(bool immediate = false) => TransitionTo(marketAnalysisPanel, immediate);
        public void ShowGameOver(bool immediate = false) => TransitionTo(gameOverPanel, immediate);
        public void ShowSettings(bool immediate = false) => TransitionTo(settingsPanel, immediate);

        public void TransitionTo(RectTransform targetScreen, bool immediate = false)
        {
            if (targetScreen == null) return;
            if (_currentActiveScreen == targetScreen && targetScreen.gameObject.activeSelf) return;

            if (_activeTransitionCoroutine != null)
            {
                StopCoroutine(_activeTransitionCoroutine);
            }

            if (immediate)
            {
                HideAllScreensExcept(targetScreen);
                SetScreenVisible(targetScreen, 1f, Vector3.one, true);
                _currentActiveScreen = targetScreen;

                if (targetScreen == mainMenuOverlay)
                {
                    PlayGlitchOnScreen(targetScreen, true);
                }
            }
            else
            {
                _activeTransitionCoroutine = StartCoroutine(TransitionRoutine(targetScreen));
            }
        }

        private IEnumerator TransitionRoutine(RectTransform nextScreen)
        {
            RectTransform prevScreen = _currentActiveScreen;

            // Fade out previous screen
            if (prevScreen != null && prevScreen != nextScreen && prevScreen.gameObject.activeSelf)
            {
                // When closing Main Menu, play the glitch effect again
                if (prevScreen == mainMenuOverlay)
                {
                    PlayGlitchOnScreen(prevScreen, false);
                }

                CanvasGroup prevCg = GetOrAddCanvasGroup(prevScreen);
                float elapsedOut = 0f;
                float durOut = prevScreen == mainMenuOverlay ? Mathf.Max(transitionDuration * 0.5f, 0.22f) : transitionDuration * 0.5f;

                while (elapsedOut < durOut)
                {
                    elapsedOut += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsedOut / durOut);
                    prevCg.alpha = 1f - t;
                    yield return null;
                }

                prevScreen.gameObject.SetActive(false);
            }

            // Hide all other screens to guarantee zero overlap
            HideAllScreensExcept(nextScreen);

            // Animate in next screen
            nextScreen.gameObject.SetActive(true);
            CanvasGroup nextCg = GetOrAddCanvasGroup(nextScreen);
            nextCg.alpha = 0f;
            nextCg.blocksRaycasts = true;
            nextCg.interactable = true;

            // When Main Menu appears, play the glitch effect
            if (nextScreen == mainMenuOverlay)
            {
                PlayGlitchOnScreen(nextScreen, true);
            }

            if (useScalePunch) nextScreen.localScale = punchScale;

            float elapsedIn = 0f;
            while (elapsedIn < transitionDuration)
            {
                elapsedIn += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsedIn / transitionDuration);
                float easedT = Mathf.SmoothStep(0f, 1f, t);

                nextCg.alpha = easedT;
                if (useScalePunch)
                {
                    nextScreen.localScale = Vector3.Lerp(punchScale, Vector3.one, easedT);
                }

                yield return null;
            }

            SetScreenVisible(nextScreen, 1f, Vector3.one, true);
            _currentActiveScreen = nextScreen;
            _activeTransitionCoroutine = null;
        }

        private void PlayGlitchOnScreen(RectTransform screen, bool isShow)
        {
            if (screen == null) return;
            var glitches = screen.GetComponentsInChildren<CyberpunkUIGlitch>(true);
            foreach (var g in glitches)
            {
                if (g != null)
                {
                    if (isShow)
                    {
                        g.gameObject.SetActive(true);
                        g.PlayShow();
                    }
                    else if (g.gameObject.activeInHierarchy)
                    {
                        g.PlayHide(false);
                    }
                }
            }
        }

        private void HideAllScreensExcept(RectTransform keepScreen)
        {
            RectTransform[] all = { mainMenuOverlay, gameHUDOverlay, marketAnalysisPanel, gameOverPanel, settingsPanel };
            foreach (var s in all)
            {
                if (s != null && s != keepScreen)
                {
                    s.gameObject.SetActive(false);
                }
            }
        }

        private void SetScreenVisible(RectTransform screen, float alpha, Vector3 scale, bool active)
        {
            if (screen == null) return;
            screen.gameObject.SetActive(active);
            if (active)
            {
                CanvasGroup cg = GetOrAddCanvasGroup(screen);
                cg.alpha = alpha;
                cg.interactable = true;
                cg.blocksRaycasts = true;
                screen.localScale = scale;
            }
        }

        private CanvasGroup GetOrAddCanvasGroup(RectTransform rt)
        {
            CanvasGroup cg = rt.GetComponent<CanvasGroup>();
            if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
            return cg;
        }

        #endregion

        #region Demo Controls & Effect Triggering

        public void SetGlitchType(int glitchIndex)
        {
            foreach (var g in glitchTargets)
            {
                if (g != null) g.SetGlitchType(glitchIndex);
            }
        }

        public void TriggerGlitchBurst()
        {
            foreach (var g in glitchTargets)
            {
                if (g != null && g.gameObject.activeInHierarchy) g.PlayGlitch();
            }
        }

        public void TriggerGlassShine()
        {
            foreach (var eff in effectTargets)
            {
                if (eff != null && eff.gameObject.activeInHierarchy) eff.TriggerShine();
            }
        }

        public void ToggleBorderGlow(bool enabled)
        {
            foreach (var eff in effectTargets)
            {
                if (eff != null) eff.SetBorderGlowEnabled(enabled);
            }
        }

        #endregion

        #region Auto Discovery

        private void AutoDiscoverScreens()
        {
            if (mainMenuOverlay == null) mainMenuOverlay = transform.Find("MainMenuOverlay") as RectTransform;
            if (gameHUDOverlay == null) gameHUDOverlay = transform.Find("GameHUDOverlay") as RectTransform;
            if (marketAnalysisPanel == null) marketAnalysisPanel = transform.Find("MarketAnalysisPanel") as RectTransform;
            if (gameOverPanel == null) gameOverPanel = transform.Find("GameOverPanel") as RectTransform;
            if (settingsPanel == null) settingsPanel = transform.Find("SettingsPanel") as RectTransform;
        }

        private void AutoDiscoverEffects()
        {
            glitchTargets.Clear();
            var glitches = GetComponentsInChildren<CyberpunkUIGlitch>(true);
            glitchTargets.AddRange(glitches);

            effectTargets.Clear();
            var effects = GetComponentsInChildren<CyberpunkUIEffects>(true);
            effectTargets.AddRange(effects);
        }

        #endregion

        #region Cyberpunk Demo Toolbar Construction

        private void CreateDemoToolbar()
        {
            Transform existing = transform.Find("Cyberpunk_Demo_Toolbar");
            if (existing != null) DestroyImmediate(existing.gameObject);

            _demoToolbarGo = new GameObject("Cyberpunk_Demo_Toolbar", typeof(RectTransform), typeof(CanvasGroup));
            _demoToolbarGo.transform.SetParent(transform, false);
            _demoToolbarGo.transform.SetAsLastSibling();

            RectTransform toolbarRect = _demoToolbarGo.GetComponent<RectTransform>();
            toolbarRect.anchorMin = new Vector2(0.5f, 0.02f);
            toolbarRect.anchorMax = new Vector2(0.5f, 0.02f);
            toolbarRect.pivot = new Vector2(0.5f, 0f);
            toolbarRect.sizeDelta = new Vector2(980f, 90f);
            toolbarRect.anchoredPosition = Vector2.zero;

            // Background panel
            Image bg = _demoToolbarGo.AddComponent<Image>();
            bg.color = new Color(0.02f, 0.05f, 0.12f, 0.88f);

            // Horizontal layout
            HorizontalLayoutGroup layout = _demoToolbarGo.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.spacing = 10f;
            layout.padding = new RectOffset(14, 14, 12, 12);

            // Screen Switcher Buttons
            CreateToolbarButton("MENU", () => ShowMainMenu());
            CreateToolbarButton("HUD", () => ShowGameHUD());
            CreateToolbarButton("MARKET", () => ShowMarketAnalysis());
            CreateToolbarButton("OVER", () => ShowGameOver());
            CreateToolbarButton("SETTINGS", () => ShowSettings());

            // Glitch Action Button
            CreateToolbarActionButton("GLITCH >>", new Color(1f, 0.15f, 0.45f, 1f), () => TriggerGlitchBurst());

            // Shine Action Button
            CreateToolbarActionButton("SHINE >>", new Color(0f, 0.95f, 1f, 1f), () => TriggerGlassShine());

            // Glitch Type Cycle Button
            int currentGlitchType = 0;
            string[] typeNames = { "HOLO", "DIGITAL", "MATRIX", "FLICKER", "CHROMA" };
            Button cycleBtn = null;
            TextMeshProUGUI cycleLabel = null;

            GameObject cycleObj = CreateToolbarActionButton("TYPE: HOLO", new Color(0.15f, 0.85f, 1f, 1f), null);
            cycleBtn = cycleObj.GetComponent<Button>();
            cycleLabel = cycleObj.GetComponentInChildren<TextMeshProUGUI>();

            cycleBtn.onClick.AddListener(() =>
            {
                currentGlitchType = (currentGlitchType + 1) % typeNames.Length;
                cycleLabel.text = "TYPE: " + typeNames[currentGlitchType];
                SetGlitchType(currentGlitchType);
                TriggerGlitchBurst();
            });
        }

        private GameObject CreateToolbarButton(string label, System.Action onClick)
        {
            GameObject btnObj = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(_demoToolbarGo.transform, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(85f, 66f);

            Image img = btnObj.GetComponent<Image>();
            img.color = new Color(0.08f, 0.18f, 0.32f, 0.85f);

            Button btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(btnObj.transform, false);

            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI txt = txtObj.GetComponent<TextMeshProUGUI>();
            txt.text = label;
            txt.fontSize = 18f;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = new Color(0.7f, 0.88f, 1f, 1f);

            return btnObj;
        }

        private GameObject CreateToolbarActionButton(string label, Color accentColor, System.Action onClick)
        {
            GameObject btnObj = new GameObject("BtnAction_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(_demoToolbarGo.transform, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(130f, 66f);

            Image img = btnObj.GetComponent<Image>();
            img.color = new Color(accentColor.r * 0.25f, accentColor.g * 0.25f, accentColor.b * 0.25f, 0.95f);

            Button btn = btnObj.GetComponent<Button>();
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(() => onClick.Invoke());

            GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(btnObj.transform, false);

            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI txt = txtObj.GetComponent<TextMeshProUGUI>();
            txt.text = label;
            txt.fontSize = 18f;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = accentColor;

            return btnObj;
        }

        #endregion
    }
}
