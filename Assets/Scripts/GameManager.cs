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

    [Header("Game Over UI References")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalDistanceText;
    public TextMeshProUGUI finalTradesText;
    public TextMeshProUGUI finalTPText;
    public TextMeshProUGUI highScoreText;

    public bool isGameOver = false;
    private bool gameStarted = false;
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
        if (isLoopingRun)
        {
            takeProfit = carryOverTakeProfit;
            distanceRun = carryOverDistance;
            correctTrades = carryOverTrades;
            currentSpeedMultiplier = Mathf.Max(1f, carryOverSpeedMultiplier);
        }
        else
        {
            carryOverTakeProfit = 0;
            carryOverDistance = 0f;
            carryOverTrades = 0;
            carryOverSpeedMultiplier = 1f;
        }

        UpdateTradesUI();
        UpdateDistanceUI();

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

    public void AddTakeProfit()
    {
        takeProfit++;
    }

    public void AddCorrectTrade()
    {
        correctTrades++;
        UpdateTradesUI();
    }

    private void UpdateDistanceUI()
    {
        if (hudDistanceText != null)
        {
            hudDistanceText.text = $"{Mathf.FloorToInt(distanceRun)}m";
        }
    }

    private void UpdateTradesUI()
    {
        if (hudTradesText != null)
        {
            hudTradesText.text = $"{correctTrades}";
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

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            gameoverAnimator.Play("Game Over Panel Enter");
        }

        if (finalDistanceText != null) finalDistanceText.text = $"{Mathf.FloorToInt(distanceRun)}m";
        if (finalTradesText != null) finalTradesText.text = $"{correctTrades}";
        if (finalTPText != null) finalTPText.text = $"{takeProfit}";
        if (highScoreText != null) highScoreText.text = $"{highScore}";
    }

    public void RestartGame()
    {
        if (LevelLoader.Instance != null)
        {
            LevelLoader.Instance.RestartLevel();
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
