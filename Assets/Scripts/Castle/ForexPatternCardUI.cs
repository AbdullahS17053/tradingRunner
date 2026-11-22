using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace RunnerForex.Castle
{
    /// <summary>
    /// Interactive UI Card representing one of the 30 Candlestick Patterns with Movement Animations.
    /// Pre-organized in the scene hierarchy so the designer can inspect and change listings directly.
    /// </summary>
    public class ForexPatternCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Data")]
        public int patternId = 1;
        public ForexPatternInfo patternInfo;

        [Header("UI Elements")]
        public Image cardImage;
        public Image borderGlow;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI moveNameText;
        public GameObject badgeTag;
        public TextMeshProUGUI badgeText;
        public CandlestickBadge candlestickBadge;

        [Header("Animator")]
        public ForexPatternMovementAnimator animator;

        private Vector3 normalScale = Vector3.one;
        private System.Action<ForexPatternInfo> onClickCallback;

        private void Awake()
        {
            FindUIElements();

            Button btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(TriggerCardSelection);
            }

            RefreshCardUI();
        }

        private void OnEnable()
        {
            RefreshCardUI();
        }

        private void FindUIElements()
        {
            if (patternInfo == null && patternId > 0)
            {
                patternInfo = ForexPatternDatabase.GetPattern(patternId);
            }

            if (cardImage == null) cardImage = transform.Find("CardSprite")?.GetComponent<Image>();
            if (borderGlow == null) borderGlow = transform.Find("BorderGlow")?.GetComponent<Image>();
            if (moveNameText == null) moveNameText = transform.Find("MoveLabel")?.GetComponent<TextMeshProUGUI>();
            if (titleText == null) titleText = transform.Find("TitleLabel")?.GetComponent<TextMeshProUGUI>();
            if (animator == null) animator = GetComponent<ForexPatternMovementAnimator>() ?? cardImage?.GetComponent<ForexPatternMovementAnimator>();

            EnsureCandlestickBadge();
        }

        public void EnsureCandlestickBadge()
        {
            Transform cardSpriteTf = transform.Find("CardSprite");
            if (cardSpriteTf == null) return;

            if (cardImage != null && cardImage.sprite == null)
            {
                cardImage.color = new Color(1f, 1f, 1f, 0f);
            }

            if (candlestickBadge == null)
            {
                Transform badgeTf = cardSpriteTf.Find("CandlestickBadge");
                if (badgeTf != null)
                {
                    candlestickBadge = badgeTf.GetComponent<CandlestickBadge>();
                }
                else
                {
                    GameObject badgeObj = new GameObject("CandlestickBadge", typeof(RectTransform), typeof(CandlestickBadge));
                    badgeObj.transform.SetParent(cardSpriteTf, false);
                    candlestickBadge = badgeObj.GetComponent<CandlestickBadge>();
                }
            }

            if (candlestickBadge != null)
            {
                RectTransform rt = candlestickBadge.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0f, 0.45f);
                    rt.anchorMax = new Vector2(1f, 0.98f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = Vector2.zero;
                    rt.pivot = new Vector2(0.5f, 0.5f);
                }
                candlestickBadge.patternId = patternId;
            }
        }

        /// <summary>
        /// Enforces consistent sizing, centering, padding, and alignment across all graph cards.
        /// </summary>
        public void AdjustCardLayout()
        {
            // 1. Uniform Card Dimensions
            RectTransform cardRt = GetComponent<RectTransform>();
            if (cardRt != null)
            {
                cardRt.sizeDelta = new Vector2(176f, 230f);
            }

            // 2. Consistent Border Glow framing
            if (borderGlow != null)
            {
                RectTransform brt = borderGlow.rectTransform;
                brt.anchorMin = Vector2.zero;
                brt.anchorMax = Vector2.one;
                brt.pivot = new Vector2(0.5f, 0.5f);
                brt.anchoredPosition = Vector2.zero;
                brt.sizeDelta = new Vector2(4f, 4f);
            }

            // 3. CardSprite: upper and middle area for chart & animation
            if (cardImage != null)
            {
                RectTransform srt = cardImage.rectTransform;
                srt.anchorMin = new Vector2(0f, 0.22f);
                srt.anchorMax = Vector2.one;
                srt.pivot = new Vector2(0.5f, 0.5f);
                srt.anchoredPosition = Vector2.zero;
                srt.sizeDelta = new Vector2(-8f, -8f);
            }

            // 4. CandlestickBadge: centered in the dedicated top chart zone
            if (candlestickBadge != null)
            {
                RectTransform badgeRt = candlestickBadge.GetComponent<RectTransform>();
                if (badgeRt != null)
                {
                    badgeRt.anchorMin = new Vector2(0f, 0.45f);
                    badgeRt.anchorMax = new Vector2(1f, 0.98f);
                    badgeRt.pivot = new Vector2(0.5f, 0.5f);
                    badgeRt.anchoredPosition = Vector2.zero;
                    badgeRt.sizeDelta = Vector2.zero;
                }
            }

            // 5. MoveLabel: uniform bottom area with padding, readable and non-overlapping
            if (moveNameText != null)
            {
                RectTransform mlrt = moveNameText.rectTransform;
                mlrt.anchorMin = Vector2.zero;
                mlrt.anchorMax = new Vector2(1f, 0.22f);
                mlrt.pivot = new Vector2(0.5f, 0f);
                mlrt.anchoredPosition = new Vector2(0f, 4f);
                mlrt.sizeDelta = new Vector2(-8f, 0f);

                moveNameText.fontSize = 11f;
                moveNameText.alignment = TextAlignmentOptions.Center;
                moveNameText.enableWordWrapping = true;
                moveNameText.fontStyle = FontStyles.Bold;
            }
        }

        public void RefreshCardUI()
        {
            if (patternInfo == null && patternId > 0)
            {
                patternInfo = ForexPatternDatabase.GetPattern(patternId);
            }

            EnsureCandlestickBadge();
            AdjustCardLayout();

            if (candlestickBadge != null)
            {
                candlestickBadge.patternId = patternId;
                candlestickBadge.Build(patternId);
            }

            // Retain hierarchy customized sprite if already set, otherwise assign database sprite
            if (cardImage != null)
            {
                if (cardImage.sprite == null && patternInfo != null)
                {
                    cardImage.sprite = patternInfo.GetSprite();
                }

                // If no external sprite exists, make cardImage transparent so procedural CandlestickBadge shows cleanly
                if (cardImage.sprite == null)
                {
                    cardImage.color = new Color(1f, 1f, 1f, 0f);
                }
                else
                {
                    cardImage.color = Color.white;
                }
            }

            // Retain hierarchy customized title if already set, otherwise assign database title
            if (titleText != null && patternInfo != null)
            {
                titleText.text = patternInfo.DisplayTitle;
            }

            // Move text with formatted pattern and movement name
            if (moveNameText != null && patternInfo != null)
            {
                moveNameText.text = $"{patternInfo.id}. {patternInfo.patternName}\n{patternInfo.moveName}";
                moveNameText.color = patternInfo.BiasColor;
            }

            if (borderGlow != null && patternInfo != null)
            {
                borderGlow.color = new Color(patternInfo.BiasColor.r, patternInfo.BiasColor.g, patternInfo.BiasColor.b, 0.4f);
            }

            if (animator == null)
            {
                animator = GetComponent<ForexPatternMovementAnimator>() ?? cardImage?.GetComponent<ForexPatternMovementAnimator>();
            }
            if (animator != null && patternInfo != null)
            {
                animator.animationType = patternInfo.animationType;
            }
        }

        public void Bind(ForexPatternInfo info, System.Action<ForexPatternInfo> onSelected)
        {
            if (info != null)
            {
                patternInfo = info;
                patternId = info.id;
            }
            else if (patternInfo != null)
            {
                info = patternInfo;
            }
            else if (patternId > 0)
            {
                patternInfo = ForexPatternDatabase.GetPattern(patternId);
                info = patternInfo;
            }

            onClickCallback = onSelected;
            RefreshCardUI();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.localScale = normalScale * 1.06f;
            if (borderGlow != null && patternInfo != null)
            {
                borderGlow.color = new Color(patternInfo.BiasColor.r, patternInfo.BiasColor.g, patternInfo.BiasColor.b, 1.0f);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = normalScale;
            if (borderGlow != null && patternInfo != null)
            {
                borderGlow.color = new Color(patternInfo.BiasColor.r, patternInfo.BiasColor.g, patternInfo.BiasColor.b, 0.4f);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            TriggerCardSelection();
        }

        public void TriggerCardSelection()
        {
            if (animator != null)
            {
                animator.PlayBurstAnimation();
            }

            if (SoundManager.Instance != null && SoundManager.Instance.collectTPSound != null)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.collectTPSound);
            }

            if (patternInfo == null && patternId > 0)
            {
                patternInfo = ForexPatternDatabase.GetPattern(patternId);
            }

            if (onClickCallback != null)
            {
                onClickCallback.Invoke(patternInfo);
            }
            else if (ForexPatternsPanel.Instance != null && patternInfo != null)
            {
                ForexPatternsPanel.Instance.OnCardClicked(patternInfo);
            }
        }
    }
}
