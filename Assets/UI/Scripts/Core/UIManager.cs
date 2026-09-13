using UnityEngine;
using SciFiUI.Screens;

namespace SciFiUI.Core
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Screens")]
        [SerializeField] private MainMenuScreen mainMenuScreen;
        [SerializeField] private GameHUDScreen gameHUDScreen;
        [SerializeField] private MarketAnalysisScreen marketAnalysisScreen;
        [SerializeField] private GameOverScreen gameOverScreen;
        [SerializeField] private SettingsScreen settingsScreen;

        [Header("Initial State")]
        [SerializeField] private bool startOnMainMenu = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (startOnMainMenu)
            {
                OpenMainMenu();
            }
        }

        [ContextMenu("Preview: Main Menu")]
        public void OpenMainMenu()
        {
            if (mainMenuScreen != null) mainMenuScreen.Show();
            if (gameHUDScreen != null) gameHUDScreen.Hide(true);
            if (marketAnalysisScreen != null) marketAnalysisScreen.Hide(true);
            if (gameOverScreen != null) gameOverScreen.Hide(true);
            if (settingsScreen != null) settingsScreen.Hide(true);
        }

        [ContextMenu("Preview: Game HUD (Playing)")]
        public void StartGame()
        {
            if (mainMenuScreen != null) mainMenuScreen.Hide();
            if (gameHUDScreen != null)
            {
                gameHUDScreen.Show();
                gameHUDScreen.UpdateHUD(4, 27);
            }
            if (marketAnalysisScreen != null) marketAnalysisScreen.Hide(true);
            if (gameOverScreen != null) gameOverScreen.Hide(true);
            if (settingsScreen != null) settingsScreen.Hide(true);
        }

        [ContextMenu("Preview: Market Analysis Encounter")]
        public void OpenMarketAnalysis()
        {
            OpenMarketAnalysis("RSI: 25 (Oversold)", "Trend: Price > 200 EMA", "Level: Nearing Support (S1)");
        }

        public void OpenMarketAnalysis(string rsi, string trend, string level)
        {
            if (marketAnalysisScreen != null)
            {
                marketAnalysisScreen.SetMarketData(rsi, trend, level);
                marketAnalysisScreen.Show();
            }
        }

        public void CloseMarketAnalysis(bool isBuyDecision)
        {
            if (marketAnalysisScreen != null) marketAnalysisScreen.Hide();

            if (isBuyDecision && gameHUDScreen != null)
            {
                gameHUDScreen.TriggerTPGainEffect();
            }
        }

        [ContextMenu("Preview: Game Over")]
        public void OpenGameOver()
        {
            OpenGameOver(27, 4, 1678);
        }

        public void OpenGameOver(long distance, long profits, long highScore)
        {
            if (gameHUDScreen != null) gameHUDScreen.Hide();
            if (marketAnalysisScreen != null) marketAnalysisScreen.Hide(true);
            if (mainMenuScreen != null) mainMenuScreen.Hide(true);
            if (settingsScreen != null) settingsScreen.Hide(true);

            if (gameOverScreen != null)
            {
                gameOverScreen.SetRunData(distance, profits, highScore);
                gameOverScreen.Show();
            }
        }

        [ContextMenu("Preview: Settings")]
        public void OpenSettings()
        {
            if (settingsScreen != null)
            {
                settingsScreen.Show();
            }
        }

        public void CloseSettings()
        {
            if (settingsScreen != null) settingsScreen.Hide();
            if (CyberpunkUIDemo.Instance != null)
            {
                CyberpunkUIDemo.Instance.ShowMainMenu();
            }
            else if (mainMenuScreen != null && (gameHUDScreen == null || !gameHUDScreen.gameObject.activeSelf))
            {
                OpenMainMenu();
            }
        }

        public void RestartGame()
        {
            StartGame();
        }
    }
}
