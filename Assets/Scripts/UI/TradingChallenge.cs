using UnityEngine;
using TMPro;
using System.Collections;

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
    private Animator uiAnimator;
    private bool isClosing = false;

    private void Start()
    {
        fomo = FindObjectOfType<FOMOController>();
        InitAnimator();
        if (challengeUI != null) challengeUI.SetActive(false);
    }

    private void OnEnable()
    {
        challengeActive = false;
        GameManager.Instance.isTradingChallengeActive = false;
        isClosing = false;
        if (challengeUI != null) challengeUI.SetActive(false);
        if (redLaserWall != null) redLaserWall.SetActive(true);
    }

    private void InitAnimator()
    {
        if (uiAnimator == null && challengeUI != null)
        {
            uiAnimator = challengeUI.GetComponent<Animator>();
            if (uiAnimator == null)
            {
                uiAnimator = challengeUI.GetComponentInChildren<Animator>();
            }
        }
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
        if (other.CompareTag("Player") && !challengeActive && !isClosing)
        {
            StartChallenge();
        }
    }

    private void StartChallenge()
    {
        challengeActive = true;
        GameManager.Instance.isTradingChallengeActive = true;
        isClosing = false;
        Time.timeScale = 0.2f;
        if (challengeUI != null)
        {
            challengeUI.SetActive(true);
            InitAnimator();
            if (uiAnimator != null)
            {
                uiAnimator.enabled = true;
                uiAnimator.speed = 1f;
                uiAnimator.Rebind();
                uiAnimator.Update(0f);
                uiAnimator.Play("TradingMenuOpen", 0, 0f);
            }
        }

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
        if (!challengeActive || isClosing) return;

        challengeActive = false;
        GameManager.Instance.isTradingChallengeActive = false;
        isClosing = true;

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

        StartCoroutine(AnimateAndCloseMenu());
    }

    private IEnumerator AnimateAndCloseMenu()
    {
        InitAnimator();

        if (uiAnimator != null)
        {
            uiAnimator.enabled = true;
            uiAnimator.speed = 1f;
            uiAnimator.ResetTrigger("CloseRequested");
            uiAnimator.SetTrigger("CloseRequested");
            uiAnimator.Play("TradingMenuClose", 0, 0f);

            yield return null;

            float closeAnimDuration = 0.41666666f;
            AnimatorStateInfo stateInfo = uiAnimator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("TradingMenuClose") && stateInfo.length > 0f)
            {
                closeAnimDuration = stateInfo.length;
            }

            float elapsed = 0f;
            while (elapsed < closeAnimDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if (challengeUI != null)
        {
            challengeUI.SetActive(false);
        }
        isClosing = false;
    }

    public void ForceCancelChallenge()
    {
        if (!challengeActive && !isClosing) return;

        StopAllCoroutines();
        challengeActive = false;
        GameManager.Instance.isTradingChallengeActive = false;
        isClosing = false;

        if (challengeUI != null) challengeUI.SetActive(false);
        if (redLaserWall != null) redLaserWall.SetActive(false);

        if (Time.timeScale == 0.2f)
        {
            Time.timeScale = 1f;
        }
    }
}
