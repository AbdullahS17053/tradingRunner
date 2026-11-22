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

        [Header("Animator")]
        public ForexPatternMovementAnimator animator;

        private Vector3 normalScale = Vector3.one;
        private System.Action<ForexPatternInfo> onClickCallback;

        private void Awake()
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

            Button btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(TriggerCardSelection);
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

            // Retain hierarchy customized sprite if already set, otherwise assign database sprite
            if (cardImage != null && cardImage.sprite == null && info != null)
            {
                cardImage.sprite = info.GetSprite();
            }

            // Retain hierarchy customized title if already set, otherwise assign database title
            if (titleText != null && string.IsNullOrEmpty(titleText.text) && info != null)
            {
                titleText.text = info.DisplayTitle;
            }

            // Retain hierarchy customized move text if already set, otherwise assign database move
            if (moveNameText != null && string.IsNullOrEmpty(moveNameText.text) && info != null)
            {
                moveNameText.text = $"{info.id}. {info.patternName}\n{info.moveName}";
                moveNameText.color = info.BiasColor;
            }

            if (borderGlow != null && borderGlow.color.a <= 0.05f && info != null)
            {
                borderGlow.color = new Color(info.BiasColor.r, info.BiasColor.g, info.BiasColor.b, 0.4f);
            }

            if (animator == null)
            {
                animator = GetComponent<ForexPatternMovementAnimator>() ?? cardImage?.GetComponent<ForexPatternMovementAnimator>();
            }
            if (animator != null && info != null)
            {
                animator.animationType = info.animationType;
            }
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
