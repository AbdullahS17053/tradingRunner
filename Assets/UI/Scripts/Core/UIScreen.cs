using System;
using System.Collections;
using UnityEngine;

namespace SciFiUI.Core
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIScreen : MonoBehaviour
    {
        [Header("Screen Animation")]
        [SerializeField] private float transitionDuration = 0.35f;
        [SerializeField] private bool useScalePunch = true;
        [SerializeField] private Vector3 punchStartScale = new Vector3(0.85f, 0.85f, 1f);
        [SerializeField] private RectTransform animatedPanel;

        public event Action OnOpened;
        public event Action OnClosed;

        private CanvasGroup _canvasGroup;
        private Coroutine _transitionCoroutine;
        public bool IsOpen { get; private set; }

        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (animatedPanel == null)
            {
                animatedPanel = GetComponent<RectTransform>();
            }
        }

        public virtual void Show(bool immediate = false)
        {
            gameObject.SetActive(true);
            IsOpen = true;

            if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);

            if (immediate)
            {
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = 1f;
                    _canvasGroup.interactable = true;
                    _canvasGroup.blocksRaycasts = true;
                }
                if (animatedPanel != null) animatedPanel.localScale = Vector3.one;
                OnOpened?.Invoke();
            }
            else
            {
                _transitionCoroutine = StartCoroutine(AnimateIn());
            }
        }

        public virtual void Hide(bool immediate = false)
        {
            IsOpen = false;

            if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);

            if (immediate)
            {
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = 0f;
                    _canvasGroup.interactable = false;
                    _canvasGroup.blocksRaycasts = false;
                }
                gameObject.SetActive(false);
                OnClosed?.Invoke();
            }
            else
            {
                _transitionCoroutine = StartCoroutine(AnimateOut());
            }
        }

        private IEnumerator AnimateIn()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = true;
            }

            if (animatedPanel != null && useScalePunch)
            {
                animatedPanel.localScale = punchStartScale;
            }

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);

                float easedScale = OvershootEase(t);
                float easedAlpha = Mathf.SmoothStep(0f, 1f, t);

                if (_canvasGroup != null) _canvasGroup.alpha = easedAlpha;
                if (animatedPanel != null && useScalePunch)
                {
                    animatedPanel.localScale = Vector3.LerpUnclamped(punchStartScale, Vector3.one, easedScale);
                }

                yield return null;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
            }
            if (animatedPanel != null) animatedPanel.localScale = Vector3.one;

            _transitionCoroutine = null;
            OnOpened?.Invoke();
        }

        private IEnumerator AnimateOut()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = false;
            }

            float elapsed = 0f;
            float duration = transitionDuration * 0.7f;
            Vector3 targetScale = punchStartScale;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float easedAlpha = 1f - Mathf.SmoothStep(0f, 1f, t);

                if (_canvasGroup != null) _canvasGroup.alpha = easedAlpha;
                if (animatedPanel != null && useScalePunch)
                {
                    animatedPanel.localScale = Vector3.Lerp(Vector3.one, targetScale, t);
                }

                yield return null;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
            }
            gameObject.SetActive(false);

            _transitionCoroutine = null;
            OnClosed?.Invoke();
        }

        private float OvershootEase(float t)
        {
            float s = 1.70158f;
            t -= 1f;
            return (t * t * ((s + 1f) * t + s) + 1f);
        }
    }
}
