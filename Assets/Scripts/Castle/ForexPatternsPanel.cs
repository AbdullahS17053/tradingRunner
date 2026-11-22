using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RunnerForex.Castle
{
    /// <summary>
    /// Master UI Panel implementing the '30 CANDLESTICK PATTERNS (WITH MOVEMENT ANIMATIONS)'
    /// centerpiece trading codex and interactive pattern explorer.
    /// </summary>
    public class ForexPatternsPanel : MonoBehaviour
    {
        public static ForexPatternsPanel Instance { get; private set; }

        [Header("Panel Root & Modal")]
        public GameObject panelRoot;
        public Image headerBannerImage;
        public Button closeButton;

        [Header("Filter Tabs")]
        public Button filterAllButton;
        public Button filterBullishButton;
        public Button filterBearishButton;
        public Button filterNeutralButton;

        [Header("Grid Container")]
        public Transform cardsGridContainer;
        public GameObject cardPrefab;

        [Header("Pattern Detail / Showcase Modal")]
        public GameObject detailModalRoot;
        public Image detailCardImage;
        public TextMeshProUGUI detailTitleText;
        public TextMeshProUGUI detailMoveNameText;
        public TextMeshProUGUI detailMoveDescText;
        public TextMeshProUGUI detailSignalBadge;
        public TextMeshProUGUI detailTradingRuleText;
        public Button detailPracticeButton;
        public Button detailCloseButton;
        public ForexPatternMovementAnimator detailAnimator;

        private List<ForexPatternCardUI> spawnedCards = new List<ForexPatternCardUI>();
        private PatternBias? activeFilter = null; // null = all
        private ForexPatternInfo selectedPattern = null;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (panelRoot == null) panelRoot = gameObject;
            EnsureUIHierarchy();
        }

        private void Start()
        {
            WireButtons();
            BuildCardsGrid();

            if (detailModalRoot != null) detailModalRoot.SetActive(false);
            if (panelRoot != null) panelRoot.SetActive(false); // Closed by default until opened
        }

        public static void OpenPanel()
        {
            if (Instance == null)
            {
                var found = Object.FindFirstObjectByType<ForexPatternsPanel>(FindObjectsInactive.Include);
                if (found != null) Instance = found;
                else
                {
                    GameObject go = new GameObject("ForexPatternsPanel");
                    Instance = go.AddComponent<ForexPatternsPanel>();
                }
            }

            if (Instance != null)
            {
                Instance.Show();
            }
        }

        public void Show()
        {
            if (panelRoot != null) panelRoot.SetActive(true);
            ApplyFilter(null);
        }

        public void Hide()
        {
            if (detailModalRoot != null) detailModalRoot.SetActive(false);
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void Update()
        {
            // Convenience hotkey 'P' to toggle the 30-Patterns Codex anytime in Play Mode
            if (Input.GetKeyDown(KeyCode.P))
            {
                if (panelRoot != null && panelRoot.activeSelf) Hide();
                else Show();
            }
        }

        private void WireButtons()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            if (detailCloseButton != null) detailCloseButton.onClick.AddListener(() => detailModalRoot?.SetActive(false));

            if (filterAllButton != null) filterAllButton.onClick.AddListener(() => ApplyFilter(null));
            if (filterBullishButton != null) filterBullishButton.onClick.AddListener(() => ApplyFilter(PatternBias.Bullish));
            if (filterBearishButton != null) filterBearishButton.onClick.AddListener(() => ApplyFilter(PatternBias.Bearish));
            if (filterNeutralButton != null) filterNeutralButton.onClick.AddListener(() => ApplyFilter(PatternBias.Neutral));

            if (detailPracticeButton != null) detailPracticeButton.onClick.AddListener(OnPracticeClicked);
        }

        private void ApplyFilter(PatternBias? bias)
        {
            activeFilter = bias;
            foreach (var card in spawnedCards)
            {
                if (card == null || card.patternInfo == null) continue;
                bool show = (!bias.HasValue) || (card.patternInfo.bias == bias.Value);
                card.gameObject.SetActive(show);
            }
        }

        private void BuildCardsGrid()
        {
            if (cardsGridContainer == null) return;

            // Clear existing
            foreach (Transform child in cardsGridContainer)
            {
                Destroy(child.gameObject);
            }
            spawnedCards.Clear();

            var allPatterns = ForexPatternDatabase.GetAllPatterns();
            foreach (var pattern in allPatterns)
            {
                GameObject cardObj = CreateCardGameObject(cardsGridContainer, pattern);
                ForexPatternCardUI cardUI = cardObj.GetComponent<ForexPatternCardUI>();
                cardUI.Bind(pattern, OnCardClicked);
                spawnedCards.Add(cardUI);
            }
        }

        private void OnCardClicked(ForexPatternInfo info)
        {
            selectedPattern = info;
            ShowDetailModal(info);
        }

        private void ShowDetailModal(ForexPatternInfo info)
        {
            if (detailModalRoot == null) return;
            detailModalRoot.SetActive(true);

            if (detailCardImage != null)
            {
                Sprite sp = info.GetSprite();
                if (sp != null) detailCardImage.sprite = sp;
            }

            if (detailTitleText != null)
            {
                detailTitleText.text = info.DisplayTitle;
            }

            if (detailMoveNameText != null)
            {
                detailMoveNameText.text = $"MOVEMENT: {info.moveName.ToUpper()}";
                detailMoveNameText.color = info.BiasColor;
            }

            if (detailMoveDescText != null)
            {
                detailMoveDescText.text = info.moveDescription;
            }

            if (detailSignalBadge != null)
            {
                detailSignalBadge.text = info.SignalText;
                detailSignalBadge.color = info.BiasColor;
            }

            if (detailTradingRuleText != null)
            {
                detailTradingRuleText.text = $"<b>Trading Rule:</b> {info.tradingRule}";
            }

            if (detailAnimator != null)
            {
                detailAnimator.animationType = info.animationType;
                detailAnimator.PlayBurstAnimation();
            }
        }

        private void OnPracticeClicked()
        {
            if (selectedPattern == null) return;

            // If CastleChallengeController is available, load this challenge immediately
            if (CastleChallengeController.Instance != null)
            {
                Hide();
                CastleChallengeController.Instance.LoadSpecificPatternChallenge(selectedPattern.id);
            }
        }

        private GameObject CreateCardGameObject(Transform parent, ForexPatternInfo info)
        {
            GameObject cardObj = new GameObject($"Card_{info.id:02d}_{info.patternName}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cardObj.transform.SetParent(parent, false);

            RectTransform rt = cardObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100, 130);

            Image bg = cardObj.GetComponent<Image>();
            bg.color = new Color(0.04f, 0.05f, 0.09f, 0.95f);

            // Glowing border
            GameObject borderObj = new GameObject("BorderGlow", typeof(RectTransform), typeof(Image));
            borderObj.transform.SetParent(cardObj.transform, false);
            RectTransform brt = borderObj.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.sizeDelta = new Vector2(4, 4);
            Image borderImg = borderObj.GetComponent<Image>();
            borderImg.color = new Color(info.BiasColor.r, info.BiasColor.g, info.BiasColor.b, 0.4f);

            // Sprite Container
            GameObject sprObj = new GameObject("CardSprite", typeof(RectTransform), typeof(Image));
            sprObj.transform.SetParent(cardObj.transform, false);
            RectTransform srt = sprObj.GetComponent<RectTransform>();
            srt.anchorMin = Vector2.zero;
            srt.anchorMax = Vector2.one;
            srt.offsetMin = new Vector2(2, 2);
            srt.offsetMax = new Vector2(-2, -2);
            Image sprImg = sprObj.GetComponent<Image>();
            Sprite sp = info.GetSprite();
            if (sp != null) sprImg.sprite = sp;

            // Move Name Label at bottom
            GameObject moveLblObj = new GameObject("MoveLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            moveLblObj.transform.SetParent(cardObj.transform, false);
            RectTransform mlrt = moveLblObj.GetComponent<RectTransform>();
            mlrt.anchorMin = new Vector2(0f, 0f);
            mlrt.anchorMax = new Vector2(1f, 0.22f);
            mlrt.offsetMin = Vector2.zero;
            mlrt.offsetMax = Vector2.zero;
            TextMeshProUGUI mlt = moveLblObj.GetComponent<TextMeshProUGUI>();
            mlt.text = info.moveName;
            mlt.fontSize = 11;
            mlt.fontStyle = FontStyles.Bold;
            mlt.alignment = TextAlignmentOptions.Center;
            mlt.color = info.BiasColor;

            ForexPatternCardUI cardUI = cardObj.AddComponent<ForexPatternCardUI>();
            cardUI.cardImage = sprImg;
            cardUI.borderGlow = borderImg;
            cardUI.moveNameText = mlt;

            return cardObj;
        }

        private void EnsureUIHierarchy()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1005;

                CanvasScaler cs = gameObject.AddComponent<CanvasScaler>();
                cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                cs.referenceResolution = new Vector2(1080, 1920);

                gameObject.AddComponent<GraphicRaycaster>();
            }

            // Create Master Container if empty
            if (cardsGridContainer == null)
            {
                // Background overlay
                Image bg = GetComponent<Image>();
                if (bg == null) bg = gameObject.AddComponent<Image>();
                bg.color = new Color(0.02f, 0.03f, 0.07f, 0.98f);

                RectTransform masterRt = GetComponent<RectTransform>();
                masterRt.anchorMin = Vector2.zero;
                masterRt.anchorMax = Vector2.one;
                masterRt.sizeDelta = Vector2.zero;

                // Header Banner
                GameObject bannerObj = new GameObject("HeaderBanner", typeof(RectTransform), typeof(Image));
                bannerObj.transform.SetParent(transform, false);
                RectTransform bnrt = bannerObj.GetComponent<RectTransform>();
                bnrt.anchorMin = new Vector2(0.05f, 0.88f);
                bnrt.anchorMax = new Vector2(0.95f, 0.98f);
                bnrt.offsetMin = Vector2.zero;
                bnrt.offsetMax = Vector2.zero;
                headerBannerImage = bannerObj.GetComponent<Image>();
                headerBannerImage.color = new Color(0.08f, 0.12f, 0.22f, 1f);

                // Header Title
                GameObject titleObj = new GameObject("HeaderTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
                titleObj.transform.SetParent(bannerObj.transform, false);
                RectTransform trt = titleObj.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.sizeDelta = Vector2.zero;
                TextMeshProUGUI tt = titleObj.GetComponent<TextMeshProUGUI>();
                tt.text = "30 CANDLESTICK PATTERNS (WITH MOVEMENT ANIMATIONS)";
                tt.fontSize = 26;
                tt.fontStyle = FontStyles.Bold;
                tt.alignment = TextAlignmentOptions.Center;
                tt.color = new Color(0f, 0.95f, 1f);

                // Close Button
                GameObject cbObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
                cbObj.transform.SetParent(bannerObj.transform, false);
                RectTransform cbrt = cbObj.GetComponent<RectTransform>();
                cbrt.anchorMin = new Vector2(0.93f, 0.2f);
                cbrt.anchorMax = new Vector2(0.99f, 0.8f);
                cbrt.sizeDelta = Vector2.zero;
                Image cbImg = cbObj.GetComponent<Image>();
                cbImg.color = new Color(1f, 0.2f, 0.3f);
                closeButton = cbObj.GetComponent<Button>();

                GameObject cbTxtObj = new GameObject("X", typeof(RectTransform), typeof(TextMeshProUGUI));
                cbTxtObj.transform.SetParent(cbObj.transform, false);
                TextMeshProUGUI cbTxt = cbTxtObj.GetComponent<TextMeshProUGUI>();
                cbTxt.text = "✕";
                cbTxt.fontSize = 24;
                cbTxt.alignment = TextAlignmentOptions.Center;
                cbTxt.color = Color.white;

                // Scroll View Container
                GameObject scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
                scrollObj.transform.SetParent(transform, false);
                RectTransform srt = scrollObj.GetComponent<RectTransform>();
                srt.anchorMin = new Vector2(0.05f, 0.05f);
                srt.anchorMax = new Vector2(0.95f, 0.86f);
                srt.offsetMin = Vector2.zero;
                srt.offsetMax = Vector2.zero;
                Image sImg = scrollObj.GetComponent<Image>();
                sImg.color = new Color(0.03f, 0.05f, 0.1f, 0.85f);
                ScrollRect sr = scrollObj.GetComponent<ScrollRect>();

                GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
                viewportObj.transform.SetParent(scrollObj.transform, false);
                RectTransform vprt = viewportObj.GetComponent<RectTransform>();
                vprt.anchorMin = Vector2.zero;
                vprt.anchorMax = Vector2.one;
                vprt.sizeDelta = Vector2.zero;
                sr.viewport = vprt;

                GameObject contentObj = new GameObject("GridContent", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
                contentObj.transform.SetParent(viewportObj.transform, false);
                RectTransform crt = contentObj.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0f, 1f);
                crt.anchorMax = new Vector2(1f, 1f);
                crt.pivot = new Vector2(0.5f, 1f);
                crt.sizeDelta = new Vector2(0, 1200);

                GridLayoutGroup grid = contentObj.GetComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(170, 215);
                grid.spacing = new Vector2(20, 20);
                grid.padding = new RectOffset(20, 20, 20, 20);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 5;

                ContentSizeFitter csf = contentObj.GetComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                sr.content = crt;
                cardsGridContainer = contentObj.transform;

                // Detail Modal
                CreateDetailModal(transform);
            }
        }

        private void CreateDetailModal(Transform parent)
        {
            detailModalRoot = new GameObject("PatternDetailModal", typeof(RectTransform), typeof(Image));
            detailModalRoot.transform.SetParent(parent, false);
            RectTransform dmRt = detailModalRoot.GetComponent<RectTransform>();
            dmRt.anchorMin = new Vector2(0.15f, 0.15f);
            dmRt.anchorMax = new Vector2(0.85f, 0.85f);
            dmRt.sizeDelta = Vector2.zero;
            Image dmBg = detailModalRoot.GetComponent<Image>();
            dmBg.color = new Color(0.02f, 0.04f, 0.1f, 0.98f);

            // Modal Header
            GameObject titleObj = new GameObject("DetailTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(detailModalRoot.transform, false);
            RectTransform trt = titleObj.GetComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(0, 240);
            trt.sizeDelta = new Vector2(600, 50);
            detailTitleText = titleObj.GetComponent<TextMeshProUGUI>();
            detailTitleText.fontSize = 28;
            detailTitleText.fontStyle = FontStyles.Bold;
            detailTitleText.alignment = TextAlignmentOptions.Center;
            detailTitleText.color = new Color(0f, 0.95f, 1f);

            // Move Name
            GameObject moveObj = new GameObject("DetailMoveName", typeof(RectTransform), typeof(TextMeshProUGUI));
            moveObj.transform.SetParent(detailModalRoot.transform, false);
            RectTransform mrt = moveObj.GetComponent<RectTransform>();
            mrt.anchoredPosition = new Vector2(0, 195);
            mrt.sizeDelta = new Vector2(600, 40);
            detailMoveNameText = moveObj.GetComponent<TextMeshProUGUI>();
            detailMoveNameText.fontSize = 22;
            detailMoveNameText.fontStyle = FontStyles.Bold;
            detailMoveNameText.alignment = TextAlignmentOptions.Center;

            // Big Card Image with movement animation
            GameObject cardImgObj = new GameObject("DetailCardImage", typeof(RectTransform), typeof(Image));
            cardImgObj.transform.SetParent(detailModalRoot.transform, false);
            RectTransform crt = cardImgObj.GetComponent<RectTransform>();
            crt.anchoredPosition = new Vector2(0, 60);
            crt.sizeDelta = new Vector2(170, 210);
            detailCardImage = cardImgObj.GetComponent<Image>();
            detailAnimator = cardImgObj.AddComponent<ForexPatternMovementAnimator>();

            // Signal Badge
            GameObject sigObj = new GameObject("DetailSignalBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
            sigObj.transform.SetParent(detailModalRoot.transform, false);
            RectTransform srt = sigObj.GetComponent<RectTransform>();
            srt.anchoredPosition = new Vector2(0, -65);
            srt.sizeDelta = new Vector2(400, 35);
            detailSignalBadge = sigObj.GetComponent<TextMeshProUGUI>();
            detailSignalBadge.fontSize = 20;
            detailSignalBadge.fontStyle = FontStyles.Bold;
            detailSignalBadge.alignment = TextAlignmentOptions.Center;

            // Move Description
            GameObject mDescObj = new GameObject("DetailMoveDesc", typeof(RectTransform), typeof(TextMeshProUGUI));
            mDescObj.transform.SetParent(detailModalRoot.transform, false);
            RectTransform mdrt = mDescObj.GetComponent<RectTransform>();
            mdrt.anchoredPosition = new Vector2(0, -110);
            mdrt.sizeDelta = new Vector2(580, 50);
            detailMoveDescText = mDescObj.GetComponent<TextMeshProUGUI>();
            detailMoveDescText.fontSize = 17;
            detailMoveDescText.alignment = TextAlignmentOptions.Center;
            detailMoveDescText.color = new Color(0.7f, 0.85f, 1f, 0.9f);

            // Trading Rule Text
            GameObject ruleObj = new GameObject("DetailTradingRule", typeof(RectTransform), typeof(TextMeshProUGUI));
            ruleObj.transform.SetParent(detailModalRoot.transform, false);
            RectTransform rrt = ruleObj.GetComponent<RectTransform>();
            rrt.anchoredPosition = new Vector2(0, -185);
            rrt.sizeDelta = new Vector2(580, 90);
            detailTradingRuleText = ruleObj.GetComponent<TextMeshProUGUI>();
            detailTradingRuleText.fontSize = 16;
            detailTradingRuleText.alignment = TextAlignmentOptions.Center;
            detailTradingRuleText.color = Color.white;

            // Close Modal Button
            GameObject dcbObj = new GameObject("DetailCloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            dcbObj.transform.SetParent(detailModalRoot.transform, false);
            RectTransform dcbrt = dcbObj.GetComponent<RectTransform>();
            dcbrt.anchoredPosition = new Vector2(0, -265);
            dcbrt.sizeDelta = new Vector2(160, 44);
            Image dcbImg = dcbObj.GetComponent<Image>();
            dcbImg.color = new Color(0.2f, 0.4f, 0.7f);
            detailCloseButton = dcbObj.GetComponent<Button>();

            GameObject dcbTxtObj = new GameObject("CloseTxt", typeof(RectTransform), typeof(TextMeshProUGUI));
            dcbTxtObj.transform.SetParent(dcbObj.transform, false);
            TextMeshProUGUI dcbTxt = dcbTxtObj.GetComponent<TextMeshProUGUI>();
            dcbTxt.text = "BACK";
            dcbTxt.fontSize = 18;
            dcbTxt.fontStyle = FontStyles.Bold;
            dcbTxt.alignment = TextAlignmentOptions.Center;
            dcbTxt.color = Color.white;
        }
    }
}
