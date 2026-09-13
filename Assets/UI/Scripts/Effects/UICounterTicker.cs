using System.Collections;
using UnityEngine;
using TMPro;

namespace SciFiUI.Effects
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class UICounterTicker : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float countDuration = 1.2f;
        [SerializeField] private string format = "{0}";
        [SerializeField] private bool punchOnComplete = true;

        private TextMeshProUGUI _text;
        private Coroutine _countCoroutine;

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
        }

        public void StartCount(long fromValue, long toValue, float customDuration = -1f)
        {
            if (_text == null) _text = GetComponent<TextMeshProUGUI>();
            if (_countCoroutine != null) StopCoroutine(_countCoroutine);
            _countCoroutine = StartCoroutine(CountRoutine(fromValue, toValue, customDuration > 0 ? customDuration : countDuration));
        }

        public void SetImmediate(long value)
        {
            if (_text == null) _text = GetComponent<TextMeshProUGUI>();
            _text.text = string.Format(format, value);
        }

        private IEnumerator CountRoutine(long from, long to, float duration)
        {
            float elapsed = 0f;
            Vector3 originalScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float easedT = 1f - Mathf.Pow(1f - t, 3f);

                long current = (long)Mathf.Lerp(from, to, easedT);
                _text.text = string.Format(format, current);

                yield return null;
            }

            _text.text = string.Format(format, to);

            if (punchOnComplete)
            {
                float popElapsed = 0f;
                float popDuration = 0.2f;
                while (popElapsed < popDuration)
                {
                    popElapsed += Time.unscaledDeltaTime;
                    float pt = popElapsed / popDuration;
                    float scaleMultiplier = 1f + Mathf.Sin(pt * Mathf.PI) * 0.15f;
                    transform.localScale = originalScale * scaleMultiplier;
                    yield return null;
                }
                transform.localScale = originalScale;
            }

            _countCoroutine = null;
        }
    }
}
