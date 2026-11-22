using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Player Reference")]
    public Animator playerAnimator;
    public Animator gameoverAnimator;

    [Header("Stats")]
    public int takeProfit = 0;
    public float distanceRun = 0f;
    public int correctTrades = 0;

    // Persistent run stats across portal transitions
    public static bool isLoopingRun = false;
    public static int carryOverTakeProfit = 0;
    public static float carryOverDistance = 0f;
    public static int carryOverTrades = 0;
    public static float carryOverSpeedMultiplier = 1f;

    [Header("Difficulty Progression")]
    public float currentSpeedMultiplier = 1f;
    public float speedIncreaseRate = 0.02f;
    public float maxSpeedMultiplier = 3f;

    [Header("Active HUD UI")]
    public TextMeshProUGUI hudDistanceText;
    public TextMeshProUGUI hudTradesText;
    public TextMeshProUGUI hudTPText;

    [Header("Game Over UI References")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalDistanceText;
    public TextMeshProUGUI finalTradesText;
    public TextMeshProUGUI finalTPText;
    public TextMeshProUGUI highScoreText;

    public const string COINS_KEY = "PlayerCoins_Total";

    public static int GetTotalCoins()
    {
        return PlayerPrefs.GetInt(COINS_KEY, 0);
    }

    public static void AddTotalCoins(int amount)
    {
        int current = GetTotalCoins();
        current += amount;
        PlayerPrefs.SetInt(COINS_KEY, current);
        PlayerPrefs.Save();
    }

    public bool isGameOver = false;
    private bool gameStarted = false;
    public bool IsGameStarted => gameStarted;
    public bool isCaptureSequenceActive = false;
    public bool isTradingChallengeActive = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;

        // Auto-resolve HUD UI references if not explicitly wired in Inspector
        ResolveHUDReferences();

        if (isLoopingRun)
        {
            takeProfit = carryOverTakeProfit;
            distanceRun = carryOverDistance;
            correctTrades = carryOverTrades;
            currentSpeedMultiplier = 1.0f; // Start fresh at calibrated base speed in every new world!
        }
        else
        {
            carryOverTakeProfit = 0;
            carryOverDistance = 0f;
            carryOverTrades = 0;
            carryOverSpeedMultiplier = 1f;
            currentSpeedMultiplier = 1.0f;
        }

        UpdateTradesUI();
        UpdateDistanceUI();
        UpdateTakeProfitUI();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (SceneManager.GetActiveScene().buildIndex != 1)
        {
            gameStarted = true;
            if (playerAnimator != null)
            {
                playerAnimator.SetBool("GameStarted", true);
            }

            if (SoundManager.Instance != null && (SoundManager.Instance.musicSource == null || !SoundManager.Instance.musicSource.isPlaying))
            {
                SoundManager.Instance.RestartMusic();
            }
        }
    }

    private void ResolveHUDReferences()
    {
        if (hudDistanceText == null)
        {
            var go = GameObject.Find("DistanceText");
            if (go != null) hudDistanceText = go.GetComponent<TextMeshProUGUI>();
        }

        if (hudTradesText == null)
        {
            var go = GameObject.Find("TradesText") ?? GameObject.Find("GoodTradesText") ?? GameObject.Find("ScoreText");
            if (go != null) hudTradesText = go.GetComponent<TextMeshProUGUI>();
        }

        if (hudTPText == null)
        {
            var go = GameObject.Find("TPCount") ?? GameObject.Find("TPText") ?? GameObject.Find("CoinsText") ?? GameObject.Find("CoinText");
            if (go != null) hudTPText = go.GetComponent<TextMeshProUGUI>();
        }

        if (gameOverPanel == null)
        {
            var screens = Object.FindObjectsByType<SciFiUI.Screens.GameOverScreen>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (screens != null && screens.Length > 0)
            {
                SciFiUI.Screens.GameOverScreen bestScreen = null;
                foreach (var s in screens)
                {
                    // Prefer a GameOverScreen under an active gameplay HUD or active canvas
                    if (s.transform.parent != null && s.transform.parent.gameObject.activeInHierarchy)
                    {
                        bestScreen = s;
                        break;
                    }
                    if (s.transform.parent != null && s.transform.parent.name.Contains("HUD"))
                    {
                        bestScreen = s;
                    }
                }
                if (bestScreen == null) bestScreen = screens[0];
                gameOverPanel = bestScreen.gameObject;
            }
            else
            {
                var scene = SceneManager.GetActiveScene();
                if (scene.isLoaded)
                {
                    foreach (var root in scene.GetRootGameObjects())
                    {
                        var trs = root.GetComponentsInChildren<Transform>(true);
                        foreach (var t in trs)
                        {
                            if (t.name == "GameOverUI" || t.name == "GameOverPanel")
                            {
                                gameOverPanel = t.gameObject;
                                break;
                            }
                        }
                        if (gameOverPanel != null) break;
                    }
                }
            }

            if (gameOverPanel == null)
            {
                var go = GameObject.Find("GameOverUI") ?? GameObject.Find("GameOverPanel");
                if (go != null) gameOverPanel = go;
            }

            if (gameOverPanel != null && !isGameOver)
            {
                gameOverPanel.SetActive(false);
            }
        }
    }

    private void Update()
    {
        if (isGameOver || !gameStarted || isCaptureSequenceActive) return;

        distanceRun += (10f * currentSpeedMultiplier) * Time.deltaTime;
        UpdateDistanceUI();

        if (currentSpeedMultiplier < maxSpeedMultiplier)
        {
            currentSpeedMultiplier += speedIncreaseRate * Time.deltaTime;
        }
    }

    public void AddTakeProfit(int amount = 1)
    {
        takeProfit += amount;
        UpdateTakeProfitUI();
    }

    public void AddCorrectTrade()
    {
        correctTrades++;
        UpdateTradesUI();
    }

    private void UpdateDistanceUI()
    {
        int distInt = Mathf.FloorToInt(distanceRun);
        if (hudDistanceText != null)
        {
            hudDistanceText.text = $"{distInt}m";
        }
        if (SciFiUI.Screens.GameHUDScreen.Instance != null)
        {
            SciFiUI.Screens.GameHUDScreen.Instance.UpdateHUD(takeProfit, distInt);
        }
    }

    private void UpdateTradesUI()
    {
        if (hudTradesText != null)
        {
            hudTradesText.text = $"{correctTrades}";
        }
        if (SciFiUI.Screens.GameHUDScreen.Instance != null)
        {
            SciFiUI.Screens.GameHUDScreen.Instance.UpdateTrades(correctTrades);
        }
    }

    public void UpdateTakeProfitUI()
    {
        if (hudTPText != null)
        {
            hudTPText.text = $"{takeProfit}";
        }
        if (SciFiUI.Screens.GameHUDScreen.Instance != null)
        {
            SciFiUI.Screens.GameHUDScreen.Instance.UpdateHUD(takeProfit, Mathf.FloorToInt(distanceRun));
            SciFiUI.Screens.GameHUDScreen.Instance.TriggerTPGainEffect();
        }
    }

    public void StartGame()
    {
        if (LevelLoader.Instance != null)
        {
            LevelLoader.Instance.LoadScene(0);
        }
    }

    public void LoadScene(int sceneIndex)
    {
        if (LevelLoader.Instance != null)
        {
            LevelLoader.Instance.LoadScene(sceneIndex);
        }
    }

    public void FreezeGame()
    {
        Time.timeScale = 0f;
    }

    public void UnfreezeGame()
    {
        Time.timeScale = 1.0f;
    }

    public void TriggerGameOver()
    {
        isLoopingRun = false;
        carryOverTakeProfit = 0;
        carryOverDistance = 0f;
        carryOverTrades = 0;
        carryOverSpeedMultiplier = 1f;
        currentSpeedMultiplier = 1f;
        PortalTrigger.ResetLevelBag();

        if (RunnerController.Instance != null)
        {
            RunnerController.Instance.enabled = false;
        }

        SoundManager.Instance.StopMusic();

        Time.timeScale = 1.0f;

        GameObject[] activeTradingMenus = GameObject.FindGameObjectsWithTag("TradingMenu");
        foreach (GameObject menu in activeTradingMenus)
        {
            menu.SetActive(false);
        }

        int currentTotalScore = Mathf.FloorToInt(distanceRun) + (correctTrades * 5) + (takeProfit * 10);

        int highScore = PlayerPrefs.GetInt("HighScore_Total", 0);
        if (currentTotalScore > highScore)
        {
            highScore = currentTotalScore;
            PlayerPrefs.SetInt("HighScore_Total", highScore);
            PlayerPrefs.Save();
        }

        // 100% Offline Persistence: Persist all coins collected during this run
        if (takeProfit > 0)
        {
            AddTotalCoins(takeProfit);
        }

        // Auto-resolve GameOverUI from scene hierarchy
        if (gameOverPanel == null)
        {
            ResolveHUDReferences();
        }

        if (gameOverPanel == null)
        {
            GameObject prefab = Resources.Load<GameObject>("GameOverUI");
            if (prefab != null)
            {
                Canvas canvas = Object.FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    gameOverPanel = Instantiate(prefab, canvas.transform);
                    var rt = gameOverPanel.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        rt.anchorMin = Vector2.zero;
                        rt.anchorMax = Vector2.one;
                        rt.offsetMin = Vector2.zero;
                        rt.offsetMax = Vector2.zero;
                    }
                }
            }
        }

        if (gameOverPanel != null)
        {
            Transform p = gameOverPanel.transform.parent;
            while (p != null)
            {
                if (!p.gameObject.activeSelf)
                {
                    p.gameObject.SetActive(true);
                }
                p = p.parent;
            }

            gameOverPanel.SetActive(true);

            var screen = gameOverPanel.GetComponent<SciFiUI.Screens.GameOverScreen>() ?? gameOverPanel.GetComponentInChildren<SciFiUI.Screens.GameOverScreen>(true);
            if (screen != null)
            {
                screen.SetRunData(Mathf.FloorToInt(distanceRun), takeProfit, highScore);
                screen.Show();
            }

            if (gameoverAnimator != null)
            {
                gameoverAnimator.Play("Game Over Panel Enter");
            }
        }

        if (finalDistanceText != null) finalDistanceText.text = $"{Mathf.FloorToInt(distanceRun)}m";
        if (finalTradesText != null) finalTradesText.text = $"{correctTrades}";
        if (finalTPText != null) finalTPText.text = $"{takeProfit}";
        if (highScoreText != null) highScoreText.text = $"{highScore}";
    }

    public void RestartGame()
    {
        isLoopingRun = false;
        carryOverTakeProfit = 0;
        carryOverDistance = 0f;
        carryOverTrades = 0;
        carryOverSpeedMultiplier = 1f;
        currentSpeedMultiplier = 1f;
        PortalTrigger.ResetLevelBag();
        Time.timeScale = 1f;

        if (LevelLoader.Instance != null)
        {
            LevelLoader.Instance.RestartLevel();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
