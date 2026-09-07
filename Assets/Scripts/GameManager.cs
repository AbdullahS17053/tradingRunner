using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

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
    public TextMeshProUGUI statsText;

    private bool isGameOver = false;

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
        UpdateTradesUI(); // Initialize the UI to show 0 at the start
    }

    private void Update()
    {
        if (isGameOver) return;

        // Calculate distance and update UI
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
            hudTradesText.text = $"Good Trades: {correctTrades}";
        }
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        int highScore = PlayerPrefs.GetInt("HighScore_Distance", 0);
        if (distanceRun > highScore)
        {
            highScore = Mathf.FloorToInt(distanceRun);
            PlayerPrefs.SetInt("HighScore_Distance", highScore);
        }

        Debug.Log($"CAUGHT BY FOMO! Distance: {Mathf.FloorToInt(distanceRun)}m | " +
        $"Take Profits: {takeProfit} | Correct Trades: {correctTrades} | High Score: {highScore}m");

        RestartGame();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
