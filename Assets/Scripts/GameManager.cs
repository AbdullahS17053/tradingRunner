using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Player Reference")]
    public Animator playerAnimator;

    [Header("Stats")]
    public int takeProfit = 0;
    public float distanceRun = 0f;
    public int correctTrades = 0;

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

    private bool isGameOver = false;
    private bool gameStarted = false;

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
        UpdateTradesUI();

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
        }
    }

    private void Update()
    {
        if (isGameOver || !gameStarted) return;

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
            hudDistanceText.text = $"Distance: {Mathf.FloorToInt(distanceRun)}m";
        }
    }

    private void UpdateTradesUI()
    {
        if (hudTradesText != null)
        {
            hudTradesText.text = $"Good Trades: {correctTrades}";
        }
    }

    public void StartGame()
    {
        if (LevelLoader.Instance != null)
        {
            LevelLoader.Instance.LoadScene(0);
        }
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        Time.timeScale = 0f;

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
        }

        if (finalDistanceText != null) finalDistanceText.text = $"Distance: {Mathf.FloorToInt(distanceRun)}m";
        if (finalTradesText != null) finalTradesText.text = $"Trades: {correctTrades}";
        if (finalTPText != null) finalTPText.text = $"Take Profits: {takeProfit}";
        if (highScoreText != null) highScoreText.text = $"High Score: {highScore}";
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
