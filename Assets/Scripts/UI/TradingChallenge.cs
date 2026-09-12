using UnityEngine;
using TMPro;

public class TradingChallenge : MonoBehaviour
{
    public GameObject challengeUI;
    public TextMeshProUGUI challengeText;
    public GameObject redLaserWall;

    private bool challengeActive = false;
    private bool isBuyScenario;
    private FOMOController fomo;

    private void Start()
    {
        fomo = FindObjectOfType<FOMOController>();
        if (challengeUI != null) challengeUI.SetActive(false);
    }

    private void OnEnable()
    {
        challengeActive = false;
        if (challengeUI != null) challengeUI.SetActive(false);
        if (redLaserWall != null) redLaserWall.SetActive(true);
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
                challengeText.text =
                "RSI: <color=#00FF00>25 (Oversold)</color>\n" +
                "Trend: <color=#00FF00>Price > 200 EMA</color>\n" +
                "Pivot Point: <color=#00FF00>At Support (S1)</color>\n\n" +
                "ACTION: ?";
            }
            else
            {
                challengeText.text =
                "RSI: <color=#FF0000>75 (Overbought)</color>\n" +
                "Trend: <color=#FF0000>Price < 200 EMA</color>\n" +
                "Pivot Point: <color=#FF0000>At Resistance (R1)</color>\n\n" +
                "ACTION: ?";
            }
        }
    }

    public void SelectBuy()
    {
        ResolveChallenge(true);
    }

    public void SelectSell()
    {
        ResolveChallenge(false);
    }

    public void ResolveChallenge(bool playerChoseBuy)
    {
        challengeActive = false;
        if (challengeUI != null) challengeUI.SetActive(false);
        Time.timeScale = 1f;

        if (playerChoseBuy == isBuyScenario)
        {
            if (GameManager.Instance != null) GameManager.Instance.AddCorrectTrade();

            if (redLaserWall != null) redLaserWall.SetActive(false);

            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.correctStrategySound);

            Debug.Log("STRATEGY CONFIRMED - Path Opened!");
        }
        else
        {
            if (fomo != null) fomo.PunishPlayer();

            if (redLaserWall != null) redLaserWall.SetActive(false);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.currentSpeedMultiplier += 0.25f;
            }

            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.wrongStrategySound);

            Debug.Log("WRONG STRATEGY - FOMO approaches! Game Speed Increased!");
        }
    }
}
