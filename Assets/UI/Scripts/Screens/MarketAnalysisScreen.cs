using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SciFiUI.Core;
using SciFiUI.Effects;

namespace SciFiUI.Screens
{
    public class MarketAnalysisScreen : UIScreen
    {
        [Header("Analysis Telemetry")]
        [SerializeField] private TextMeshProUGUI rsiValueText;
        [SerializeField] private TextMeshProUGUI trendText;
        [SerializeField] private TextMeshProUGUI levelText;

        [Header("Action Buttons")]
        [SerializeField] private Button sellButton;
        [SerializeField] private Button buyButton;
        [SerializeField] private UIShineEffect buyShine;
        [SerializeField] private UIPulseEffect alertPulse;

        public event Action<bool> OnDecisionMade; // true = Buy, false = Sell

        protected override void Awake()
        {
            base.Awake();

            if (sellButton != null) sellButton.onClick.AddListener(OnSellClicked);
            if (buyButton != null) buyButton.onClick.AddListener(OnBuyClicked);
        }

        public void SetMarketData(string rsi, string trend, string level)
        {
            if (rsiValueText != null) rsiValueText.text = rsi;
            if (trendText != null) trendText.text = trend;
            if (levelText != null) levelText.text = level;

            if (buyShine != null) buyShine.TriggerShine();
        }

        private void OnBuyClicked()
        {
            OnDecisionMade?.Invoke(true);
            if (UIManager.Instance != null)
            {
                UIManager.Instance.CloseMarketAnalysis(true);
            }
            else
            {
                Hide();
            }
        }

        private void OnSellClicked()
        {
            OnDecisionMade?.Invoke(false);
            if (UIManager.Instance != null)
            {
                UIManager.Instance.CloseMarketAnalysis(false);
            }
            else
            {
                Hide();
            }
        }
    }
}
