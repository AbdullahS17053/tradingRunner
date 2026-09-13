using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SciFiUI.Effects
{
    [RequireComponent(typeof(RectTransform))]
    public class UIShineEffect : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        [Header("Timing")]
        [SerializeField] private float sweepDuration = 0.65f;
        [SerializeField] private float loopInterval = 3.2f;
        [SerializeField] private float initialDelay = 0.5f;

        [Header("Appearance")]
        [SerializeField] private Color shineColor = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField] private float shineWidth = 65f;
        [SerializeField] private float shineAngle = -25f;
        [SerializeField] private Sprite customShineSprite;

        [Header("Triggers")]
        [SerializeField] private bool autoLoop = true;
        [SerializeField] private bool triggerOnHover = true;
        [SerializeField] private bool triggerOnClick = true;
        [Header("Masking")]
        [Tooltip("Mask the shine strictly to the sprite's alpha shape (stops shine bleeding into transparent corners).")]
        [SerializeField] private bool maskToSpriteAlpha = true;
        [SerializeField] private bool ensureMask = true;

        private RectTransform _parentRect;
        private GameObject _containerGo;
        private RectTransform _shineRect;
        private Image _shineImage;
        private Coroutine _loopCoroutine;
        private Coroutine _sweepCoroutine;
        private static Sprite _cachedProceduralSprite;

        private void Awake()
        {
            _parentRect = GetComponent<RectTransform>();
            CreateShineObject();
        }

        private void OnEnable()
        {
            if (autoLoop)
            {
                if (_loopCoroutine != null) StopCoroutine(_loopCoroutine);
                _loopCoroutine = StartCoroutine(AutoLoopRoutine());
            }
        }

        private void OnDisable()
        {
            if (_loopCoroutine != null)
            {
                StopCoroutine(_loopCoroutine);
                _loopCoroutine = null;
            }
            if (_sweepCoroutine != null)
            {
                StopCoroutine(_sweepCoroutine);
                _sweepCoroutine = null;
            }
            if (_shineImage != null)
            {
                _shineImage.enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (_containerGo != null)
            {
                if (Application.isPlaying) Destroy(_containerGo);
                else DestroyImmediate(_containerGo);
            }
        }

        private void CreateShineObject()
        {
            // Create a dedicated container for the shine so parent buttons/text are never masked out
            Transform existingContainer = transform.Find("UI_Shine_Container");
            if (existingContainer != null)
            {
                _containerGo = existingContainer.gameObject;
            }
            else
            {
                _containerGo = new GameObject("UI_Shine_Container");
                _containerGo.transform.SetParent(transform, false);
                _containerGo.transform.SetAsLastSibling();
            }

            RectTransform containerRect = _containerGo.GetComponent<RectTransform>();
            if (containerRect == null) containerRect = _containerGo.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.sizeDelta = Vector2.zero;
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.localScale = Vector3.one;

            if (ensureMask && maskToSpriteAlpha)
            {
                Image maskImg = _containerGo.GetComponent<Image>();
                if (maskImg == null) maskImg = _containerGo.AddComponent<Image>();
                maskImg.raycastTarget = false;

                Image parentImg = GetComponent<Image>();
                if (parentImg != null)
                {
                    maskImg.sprite = parentImg.sprite;
                    maskImg.type = parentImg.type;
                    maskImg.preserveAspect = parentImg.preserveAspect;
                }

                Mask mask = _containerGo.GetComponent<Mask>();
                if (mask == null) mask = _containerGo.AddComponent<Mask>();
                mask.showMaskGraphic = false;
            }
            else if (ensureMask)
            {
                if (_containerGo.GetComponent<RectMask2D>() == null)
                {
                    _containerGo.AddComponent<RectMask2D>();
                }
            }

            // The shine bar itself is inside the dedicated container
            Transform existing = _containerGo.transform.Find("UI_Shine_Effect");
            GameObject shineGo;
            if (existing != null)
            {
                shineGo = existing.gameObject;
            }
            else
            {
                shineGo = new GameObject("UI_Shine_Effect");
                shineGo.transform.SetParent(_containerGo.transform, false);
            }

            _shineRect = shineGo.GetComponent<RectTransform>();
            if (_shineRect == null) _shineRect = shineGo.AddComponent<RectTransform>();

            _shineImage = shineGo.GetComponent<Image>();
            if (_shineImage == null) _shineImage = shineGo.AddComponent<Image>();

            _shineImage.raycastTarget = false;
            _shineImage.color = shineColor;

            if (customShineSprite != null)
            {
                _shineImage.sprite = customShineSprite;
            }
            else
            {
                _shineImage.sprite = GetOrCreateProceduralShineSprite();
            }

            _shineRect.localRotation = Quaternion.Euler(0, 0, shineAngle);
            _shineImage.enabled = false;
        }

        private Sprite GetOrCreateProceduralShineSprite()
        {
            if (_cachedProceduralSprite != null) return _cachedProceduralSprite;

            int width = 64;
            int height = 8;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int x = 0; x < width; x++)
            {
                float t = (float)x / (width - 1);
                float alpha = Mathf.Sin(t * Mathf.PI);
                alpha = Mathf.Pow(alpha, 1.8f);

                Color col = new Color(1f, 1f, 1f, alpha);
                for (int y = 0; y < height; y++)
                {
                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();

            _cachedProceduralSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
            return _cachedProceduralSprite;
        }

        private IEnumerator AutoLoopRoutine()
        {
            yield return new WaitForSeconds(initialDelay);

            while (true)
            {
                yield return StartCoroutine(SweepRoutine());
                yield return new WaitForSeconds(loopInterval);
            }
        }

        public void TriggerShine()
        {
            if (!gameObject.activeInHierarchy) return;
            if (_sweepCoroutine != null) StopCoroutine(_sweepCoroutine);
            _sweepCoroutine = StartCoroutine(SweepRoutine());
        }

        private IEnumerator SweepRoutine()
        {
            if (_shineRect == null || _shineImage == null) yield break;

            float parentW = _parentRect.rect.width;
            float parentH = _parentRect.rect.height;

            float diagonal = Mathf.Sqrt(parentW * parentW + parentH * parentH);
            _shineRect.sizeDelta = new Vector2(shineWidth, diagonal * 1.5f);

            float startX = -parentW * 0.5f - shineWidth;
            float endX = parentW * 0.5f + shineWidth;

            _shineImage.enabled = true;
            _shineImage.color = shineColor;

            float elapsed = 0f;
            while (elapsed < sweepDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / sweepDuration);
                float easedT = t * t * (3f - 2f * t);

                float currentX = Mathf.Lerp(startX, endX, easedT);
                _shineRect.anchoredPosition = new Vector2(currentX, 0f);

                yield return null;
            }

            _shineImage.enabled = false;
            _sweepCoroutine = null;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (triggerOnHover) TriggerShine();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (triggerOnClick) TriggerShine();
        }
    }
}
