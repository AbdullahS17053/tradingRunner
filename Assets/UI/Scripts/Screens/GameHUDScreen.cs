using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SciFiUI.Core;
using SciFiUI.Effects;

namespace SciFiUI.Screens
{
    public class GameHUDScreen : UIScreen
    {
        public static GameHUDScreen Instance { get; private set; }

        [Header("Telemetry Badges")]
        [SerializeField] private TextMeshProUGUI tpCountText;
        [SerializeField] private TextMeshProUGUI distanceText;
        [SerializeField] private TextMeshProUGUI tradesText;
        [SerializeField] private UIShineEffect tpBadgeShine;

        [Header("Top Right Currencies")]
        [SerializeField] private TextMeshProUGUI coinsText;
        [SerializeField] private TextMeshProUGUI gemsText;

        [Header("Controls")]
        [SerializeField] private Button pauseButton;

        protected override void Awake()
        {
            base.Awake();
            Instance = this;

            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(OnPauseClicked);
            }

            UpdateCurrencies();
        }

        public void UpdateHUD(long takeProfits, long distanceMeters)
        {
            if (tpCountText != null) tpCountText.text = takeProfits.ToString();
            if (distanceText != null) distanceText.text = $"{distanceMeters}m";
        }

        public void UpdateTrades(int trades)
        {
            if (tradesText != null) tradesText.text = trades.ToString();
        }

        public void UpdateCurrencies()
        {
            int coins = GameManager.GetTotalCoins();
            int gems = GemReviveManager.Instance != null ? GemReviveManager.Instance.GetGems() : PlayerPrefs.GetInt("PlayerGems_Total", 3500);

            if (coinsText != null) coinsText.text = coins.ToString("N0");
            if (gemsText != null) gemsText.text = gems.ToString("N0");
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
            Time.timeScale = 0f;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.OpenSettings();
            }
            else
            {
                GameObject settings = GameObject.Find("SettingsPanel");
                if (settings != null)
                {
                    settings.SetActive(true);
                }
            }
        }
    }
}
