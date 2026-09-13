using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SciFiUI.Core;
using SciFiUI.Effects;

namespace SciFiUI.Screens
{
    public class GameHUDScreen : UIScreen
    {
        [Header("Telemetry Badges")]
        [SerializeField] private TextMeshProUGUI tpCountText;
        [SerializeField] private TextMeshProUGUI distanceText;
        [SerializeField] private UIShineEffect tpBadgeShine;

        [Header("Controls")]
        [SerializeField] private Button pauseButton;

        protected override void Awake()
        {
            base.Awake();

            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(OnPauseClicked);
            }
        }

        public void UpdateHUD(long takeProfits, long distanceMeters)
        {
            if (tpCountText != null) tpCountText.text = takeProfits.ToString();
            if (distanceText != null) distanceText.text = $"Distance: {distanceMeters}m";
        }

        public void TriggerTPGainEffect()
        {
            if (tpBadgeShine != null)
            {
                tpBadgeShine.TriggerShine();
            }
        }

        private void OnPauseClicked()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OpenSettings();
            }
        }
    }
}
