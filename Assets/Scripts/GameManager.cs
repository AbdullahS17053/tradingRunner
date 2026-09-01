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

    private void Update()
    {
        if (isGameOver) return;

        distanceRun += (10f * currentSpeedMultiplier) * Time.deltaTime;
        
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
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        
        Time.timeScale = 0f; 

        int highScore = PlayerPrefs.GetInt("HighScore_Distance", 0);
        if (distanceRun > highScore)
        {
            highScore = Mathf.FloorToInt(distanceRun);
            PlayerPrefs.SetInt("HighScore_Distance", highScore);
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        
        if (statsText != null)
        {
            statsText.text = $"CAUGHT BY FOMO!\n\n" +
                             $"Distance: {Mathf.FloorToInt(distanceRun)}m\n" +
                             $"Take Profits: {takeProfit}\n" +
                             $"Correct Trades: {correctTrades}\n" +
                             $"High Score: {highScore}m";
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}