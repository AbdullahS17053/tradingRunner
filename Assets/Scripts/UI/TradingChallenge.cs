using UnityEngine;
using TMPro;

public class TradingChallenge : MonoBehaviour
{
    public GameObject challengeUI;

    [Header("Stat Texts")]
    public TextMeshProUGUI rsiText;
    public TextMeshProUGUI trendText;
    public TextMeshProUGUI keyZoneText;

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

    private void Update()
    {
        if (challengeActive && RunnerController.Instance != null && !RunnerController.Instance.enabled)
        {
            ForceCancelChallenge();
        }
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

        if (isBuyScenario)
        {
            if (rsiText != null) rsiText.text = "<color=#00FF00>25.4 (OVERSOLD)</color>";
            if (trendText != null) trendText.text = "<color=#00FFFF>PRICE > 200 EMA (BULLISH)</color>";
            if (keyZoneText != null) keyZoneText.text = "<color=#FFD700>NEARING SUPPORT (S1)</color>";
        }
        else
        {
            if (rsiText != null) rsiText.text = "<color=#FF0000>75.2 (OVERBOUGHT)</color>";
            if (trendText != null) trendText.text = "<color=#FF0000>PRICE < 200 EMA (BEARISH)</color>";
            if (keyZoneText != null) keyZoneText.text = "<color=#FFD700>NEARING RESISTANCE (R1)</color>";
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
                GameManager.Instance.currentSpeedMultiplier += 0.02f;
            }
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.wrongStrategySound);
            Debug.Log("WRONG STRATEGY - FOMO approaches! Game Speed Increased!");
        }
    }

    public void ForceCancelChallenge()
    {
        if (!challengeActive) return;

        challengeActive = false;
        if (challengeUI != null) challengeUI.SetActive(false);
        if (redLaserWall != null) redLaserWall.SetActive(false);

        if (Time.timeScale == 0.2f)
        {
            Time.timeScale = 1f;
        }
    }
}
