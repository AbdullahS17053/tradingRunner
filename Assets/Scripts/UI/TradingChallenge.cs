using UnityEngine;
using TMPro;

public class TradingChallenge : MonoBehaviour
{
    public GameObject challengeUI;
    public TextMeshProUGUI challengeText;
    
    public GameObject redLaserWall;
    public GameObject greenPathHologram;

    private bool challengeActive = false;
    private bool isBuyScenario;
    
    private FOMOController fomo;

    private void Start()
    {
        fomo = FindObjectOfType<FOMOController>();
        
        if (challengeUI != null) challengeUI.SetActive(false);
        if (greenPathHologram != null) greenPathHologram.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !challengeActive)
        {
            StartChallenge();
        }
    }

    private void StartChallenge()
    {
        challengeActive = true;
        
        Time.timeScale = 0.2f;
        
        if (challengeUI != null) challengeUI.SetActive(true);

        isBuyScenario = Random.value > 0.5f;

        if (challengeText != null)
        {
            if (isBuyScenario)
            {
                challengeText.text = "RSI: 25 (Oversold)\nPrice > 200 EMA\nNearing Pivot Support (S1)\n\n< LEFT: BUY  |  RIGHT: SELL >";
            }
            else
            {
                challengeText.text = "RSI: 75 (Overbought)\nPrice < 200 EMA\nNearing Pivot Resistance (R1)\n\n< LEFT: BUY  |  RIGHT: SELL >";
            }
        }
    }

    private void Update()
    {
        if (!challengeActive) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            ResolveChallenge(true);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            ResolveChallenge(false);
        }
    }

    public void ResolveChallenge(bool playerChoseBuy)
    {
        challengeActive = false;
        
        if (challengeUI != null) challengeUI.SetActive(false);
        
        Time.timeScale = 1f;

        if (playerChoseBuy == isBuyScenario)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCorrectTrade();
            }
            
            if (redLaserWall != null) redLaserWall.SetActive(false);
            if (greenPathHologram != null) greenPathHologram.SetActive(true);
            
            Debug.Log("STRATEGY CONFIRMED - Path Opened!");
        }
        else
        {
            if (fomo != null) 
            {
                fomo.PunishPlayer();
            }
            
            if (redLaserWall != null) redLaserWall.SetActive(false);
            
            Debug.Log("WRONG STRATEGY - FOMO approaches!");
        }
    }
}