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
            if (cardsGridContainer == null)
            {
                cardsGridContainer = transform.Find("ScrollView/Viewport/GridContent");
            }
            if (cardsGridContainer == null) return;

            spawnedCards.Clear();

            // All 30 cards are pre-organized directly in the hierarchy!
            ForexPatternCardUI[] existingCards = cardsGridContainer.GetComponentsInChildren<ForexPatternCardUI>(true);
            if (existingCards != null && existingCards.Length > 0)
            {
                for (int i = 0; i < existingCards.Length; i++)
                {
                    var cardUI = existingCards[i];
                    int pId = cardUI.patternId > 0 ? cardUI.patternId : (i + 1);
                    var pattern = cardUI.patternInfo ?? ForexPatternDatabase.GetPattern(pId);
                    cardUI.Bind(pattern, OnCardClicked);
                    spawnedCards.Add(cardUI);
                }
            }
        }

        public void OnCardClicked(ForexPatternInfo info)
        {
            if (info == null) return;
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

        private void EnsureUIHierarchy()
        {
            if (cardsGridContainer == null)
            {
                cardsGridContainer = transform.Find("ScrollView/Viewport/GridContent");
            }

            if (closeButton == null)
            {
                Transform cb = transform.Find("HeaderBanner/CloseButton") ?? transform.Find("CloseButton");
                if (cb != null) closeButton = cb.GetComponent<Button>();
            }

            if (detailModalRoot == null)
            {
                Transform dm = transform.Find("PatternDetailModal");
                if (dm != null)
                {
                    detailModalRoot = dm.gameObject;
                    detailTitleText = dm.Find("DetailTitle")?.GetComponent<TextMeshProUGUI>();
                    detailMoveNameText = dm.Find("DetailMoveName")?.GetComponent<TextMeshProUGUI>();
                    detailCardImage = dm.Find("DetailCardImage")?.GetComponent<Image>();
                    detailAnimator = detailCardImage?.GetComponent<ForexPatternMovementAnimator>();
                    detailSignalBadge = dm.Find("DetailSignalBadge")?.GetComponent<TextMeshProUGUI>();
                    detailMoveDescText = dm.Find("DetailMoveDesc")?.GetComponent<TextMeshProUGUI>();
                    detailTradingRuleText = dm.Find("DetailTradingRule")?.GetComponent<TextMeshProUGUI>();
                    detailCloseButton = dm.Find("DetailCloseBtn")?.GetComponent<Button>();
                }
            }

            if (filterAllButton == null) filterAllButton = transform.Find("HeaderBanner/FilterAll")?.GetComponent<Button>();
            if (filterBullishButton == null) filterBullishButton = transform.Find("HeaderBanner/FilterBullish")?.GetComponent<Button>();
            if (filterBearishButton == null) filterBearishButton = transform.Find("HeaderBanner/FilterBearish")?.GetComponent<Button>();
            if (filterNeutralButton == null) filterNeutralButton = transform.Find("HeaderBanner/FilterNeutral")?.GetComponent<Button>();
        }
    }
}
