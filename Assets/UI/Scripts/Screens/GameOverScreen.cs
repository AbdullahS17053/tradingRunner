using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SciFiUI.Core;
using SciFiUI.Effects;

namespace SciFiUI.Screens
{
    public class GameOverScreen : UIScreen
    {
        [Header("Stat Tickers")]
        [SerializeField] private UICounterTicker distanceTicker;
        [SerializeField] private UICounterTicker profitsTicker;
        [SerializeField] private UICounterTicker highScoreTicker;

        [Header("Buttons")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private UIShineEffect restartShine;

        private long _targetDistance = 27;
        private long _targetProfits = 4;
        private long _targetHighScore = 1678;

        protected override void Awake()
        {
            base.Awake();

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }

            OnOpened += PlayStatsCountAnimation;
        }

        public void SetRunData(long distance, long profits, long highScore)
        {
            _targetDistance = distance;
            _targetProfits = profits;
            _targetHighScore = highScore;
        }

        private void PlayStatsCountAnimation()
        {
            if (distanceTicker != null) distanceTicker.StartCount(0, _targetDistance, 1.0f);
            if (profitsTicker != null) profitsTicker.StartCount(0, _targetProfits, 1.0f);
            if (highScoreTicker != null) highScoreTicker.StartCount(0, _targetHighScore, 1.2f);
            if (restartShine != null) restartShine.TriggerShine();
        }

        private void OnRestartClicked()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.RestartGame();
            }
        }

        private void OnMainMenuClicked()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OpenMainMenu();
            }
        }
    }
}
