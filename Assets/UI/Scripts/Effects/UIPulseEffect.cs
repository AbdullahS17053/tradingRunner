using UnityEngine;
using UnityEngine.UI;

namespace SciFiUI.Effects
{
    public class UIPulseEffect : MonoBehaviour
    {
        [Header("Scale Pulse")]
        [SerializeField] private bool pulseScale = true;
        [SerializeField] private float minScale = 0.96f;
        [SerializeField] private float maxScale = 1.04f;
        [SerializeField] private float scaleSpeed = 2.4f;

        [Header("Alpha Pulse")]
        [SerializeField] private bool pulseAlpha = false;
        [SerializeField] private float minAlpha = 0.4f;
        [SerializeField] private float maxAlpha = 1.0f;
        [SerializeField] private float alphaSpeed = 2.4f;

        private Vector3 _baseScale;
        private Graphic _graphic;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _baseScale = transform.localScale;
            _graphic = GetComponent<Graphic>();
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            _baseScale = transform.localScale;
        }

        private void OnDisable()
        {
            transform.localScale = _baseScale;
        }

        private void Update()
        {
            float time = Time.unscaledTime;

            if (pulseScale)
            {
                float t = (Mathf.Sin(time * scaleSpeed) + 1f) * 0.5f;
                float currentScale = Mathf.Lerp(minScale, maxScale, t);
                transform.localScale = _baseScale * currentScale;
            }

            if (pulseAlpha)
            {
                float t = (Mathf.Sin(time * alphaSpeed) + 1f) * 0.5f;
                float currentAlpha = Mathf.Lerp(minAlpha, maxAlpha, t);

                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = currentAlpha;
                }
                else if (_graphic != null)
                {
                    Color c = _graphic.color;
                    c.a = currentAlpha;
                    _graphic.color = c;
                }
            }
        }
    }
}
