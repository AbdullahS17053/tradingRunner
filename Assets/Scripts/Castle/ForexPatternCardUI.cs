using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace RunnerForex.Castle
{
    /// <summary>
    /// Interactive UI Card representing one of the 30 Candlestick Patterns with Movement Animations.
    /// </summary>
    public class ForexPatternCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Data")]
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

        public void Bind(ForexPatternInfo info, System.Action<ForexPatternInfo> onSelected)
        {
            patternInfo = info;
            onClickCallback = onSelected;

            if (cardImage != null)
            {
                Sprite sp = info.GetSprite();
                if (sp != null)
                {
                    cardImage.sprite = sp;
                }
            }

            if (titleText != null)
            {
                titleText.text = info.DisplayTitle;
            }

            if (moveNameText != null)
            {
                moveNameText.text = info.moveName.ToUpper();
                moveNameText.color = info.BiasColor;
            }

            if (badgeText != null)
            {
                badgeText.text = info.signal.ToString();
                badgeText.color = info.BiasColor;
            }

            if (borderGlow != null)
            {
                borderGlow.color = new Color(info.BiasColor.r, info.BiasColor.g, info.BiasColor.b, 0.4f);
            }

            if (animator == null)
            {
                animator = GetComponent<ForexPatternMovementAnimator>() ?? gameObject.AddComponent<ForexPatternMovementAnimator>();
            }
            if (animator != null)
            {
                animator.animationType = info.animationType;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.localScale = normalScale * 1.08f;
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
            if (animator != null)
            {
                animator.PlayBurstAnimation();
            }

            if (SoundManager.Instance != null && SoundManager.Instance.collectTPSound != null)
            {
                SoundManager.Instance.PlaySFX(SoundManager.Instance.collectTPSound);
            }

            onClickCallback?.Invoke(patternInfo);
        }
    }
}
