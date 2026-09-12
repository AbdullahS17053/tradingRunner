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
                    "Level: <color=#00FF00>Nearing Support (S1)</color>\n\n";
            }
            else
            {
                challengeText.text =
                    "RSI: <color=#FF0000>75 (Overbought)</color>\n" +
                    "Trend: <color=#FF0000>Price < 200 EMA</color>\n" +
                    "Level: <color=#FF0000>Nearing Resistance (R1)</color>\n\n";
            }
        }
    }
    /*
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
        }*/

    public void ResourceChallengeTrue()
    {
        ResolveChallenge(true);
        SoundManager.Instance.PlaySFX(SoundManager.Instance.correctStrategySound);
    }

    public void ResourceChallengeFalse()
    {
        ResolveChallenge(false);
        SoundManager.Instance.PlaySFX(SoundManager.Instance.wrongStrategySound);
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
