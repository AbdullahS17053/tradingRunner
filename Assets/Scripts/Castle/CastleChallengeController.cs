using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RunnerForex.Castle
{
    public class CastleChallengeController : MonoBehaviour
    {
        public static CastleChallengeController Instance { get; private set; }

        [Header("Chart Renderer")]
        public CastleChartRenderer chartRenderer;

        [Header("UI Panels & Screens")]
        public GameObject challengeCanvas;
        public GameObject feedbackModal;
        public GameObject victoryModal;
        public GameObject failureModal;

        [Header("Challenge UI Elements")]
        public TextMeshProUGUI pairTimeframeText;
        public TextMeshProUGUI scenarioDescriptionText;
        public TextMeshProUGUI progressCounterText;
        public TextMeshProUGUI timerText;

        [Header("Special Banners")]
        public GameObject newsSpreadBanner;   // Challenge 24
        public GameObject riskWarningBanner;  // Challenge 27

        [Header("Action Buttons")]
        public Button buyButton;
        public Button sellButton;
        public Button waitButton;

        [Header("Feedback Modal Elements")]
        public TextMeshProUGUI feedbackTitleText;
        public TextMeshProUGUI feedbackExplanationText;
        public Button nextChallengeButton;

        [Header("Failure / Retry Elements")]
        public TextMeshProUGUI failureReasonText;
        public Button retryButton;
        public Button leaveWithoutRewardButton; // Enabled after 3 failures

        [Header("Sequence Triggers")]
        public CastleComputerTrigger computerTrigger;
        public ParticleSystem coinRewardParticles;
        public GameObject exitPortal;
        public AudioClip correctSound;
        public AudioClip wrongSound;
        public AudioClip coinRewardSound;

        [Header("Timer & Settings")]
        public float totalTimerDuration = 2400f; // 40 minutes limit
        public int rewardCoins = 1000;

        private List<CastleChallenge> activeFiveChallenges = new List<CastleChallenge>();
        private int currentChallengeIndex = 0;
        private int failedAttemptsCount = 0;
        private float remainingTimer = 2400f;
        private bool isTimerRunning = false;
        private bool isAnswering = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            if (buyButton != null) buyButton.onClick.AddListener(() => OnActionSelected(TradeAction.BUY));
            if (sellButton != null) sellButton.onClick.AddListener(() => OnActionSelected(TradeAction.SELL));
            if (waitButton != null) waitButton.onClick.AddListener(() => OnActionSelected(TradeAction.WAIT));

            if (nextChallengeButton != null) nextChallengeButton.onClick.AddListener(OnNextChallengeClicked);
            if (retryButton != null) retryButton.onClick.AddListener(OnRetryClicked);
            if (leaveWithoutRewardButton != null) leaveWithoutRewardButton.onClick.AddListener(OnLeaveWithoutRewardClicked);

            if (challengeCanvas != null) challengeCanvas.SetActive(false);
            if (feedbackModal != null) feedbackModal.SetActive(false);
            if (victoryModal != null) victoryModal.SetActive(false);
            if (failureModal != null) failureModal.SetActive(false);
            if (exitPortal != null) exitPortal.SetActive(false);
        }

        private void Update()
        {
            if (isTimerRunning && remainingTimer > 0f)
            {
                remainingTimer -= Time.deltaTime;
                UpdateTimerDisplay();

                if (remainingTimer <= 0f)
                {
                    remainingTimer = 0f;
                    OnAttemptFailed("Time ran out on trading challenge!");
                }
            }
        }

        public void StartCastleChallengeSession()
        {
            if (challengeCanvas != null) challengeCanvas.SetActive(true);

            remainingTimer = totalTimerDuration;
            isTimerRunning = true;
            failedAttemptsCount = 0;

            PickFiveRandomChallenges();
            currentChallengeIndex = 0;
            DisplayCurrentChallenge();
        }

        private void PickFiveRandomChallenges()
        {
            List<CastleChallenge> all = new List<CastleChallenge>(CastleChallengeDatabase.GetAllChallenges());
            activeFiveChallenges.Clear();

            // Shuffle
            for (int i = 0; i < all.Count; i++)
            {
                int r = Random.Range(i, all.Count);
                var temp = all[i];
                all[i] = all[r];
                all[r] = temp;
            }

            // Pick 5
            for (int i = 0; i < 5 && i < all.Count; i++)
            {
                activeFiveChallenges.Add(all[i]);
            }
        }

        private void DisplayCurrentChallenge()
        {
            if (currentChallengeIndex >= activeFiveChallenges.Count) return;

            CastleChallenge cur = activeFiveChallenges[currentChallengeIndex];
            isAnswering = false;

            if (feedbackModal != null) feedbackModal.SetActive(false);
            if (failureModal != null) failureModal.SetActive(false);
            if (victoryModal != null) victoryModal.SetActive(false);

            if (pairTimeframeText != null)
            {
                pairTimeframeText.text = $"{cur.pair} · {cur.timeframe}";
            }
            if (scenarioDescriptionText != null)
            {
                scenarioDescriptionText.text = cur.scenario;
            }
            if (progressCounterText != null)
            {
                progressCounterText.text = $"Challenge {currentChallengeIndex + 1} of 5";
            }

            // Special banners
            if (newsSpreadBanner != null) newsSpreadBanner.SetActive(cur.showNewsBanner);
            if (riskWarningBanner != null) riskWarningBanner.SetActive(cur.showRiskWarning);

            // Render Candlestick chart
            if (chartRenderer != null)
            {
                chartRenderer.RenderChart(cur);
            }

            SetButtonsInteractable(true);
        }

        private void OnActionSelected(TradeAction chosenAction)
        {
            if (isAnswering) return;
            isAnswering = true;
            SetButtonsInteractable(false);

            CastleChallenge cur = activeFiveChallenges[currentChallengeIndex];
            bool isCorrect = (chosenAction == cur.answer);

            if (isCorrect)
            {
                if (SoundManager.Instance != null)
                {
                    if (correctSound != null) SoundManager.Instance.PlaySFX(correctSound);
                    else if (SoundManager.Instance.correctStrategySound != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.correctStrategySound);
                }

                if (currentChallengeIndex == activeFiveChallenges.Count - 1)
                {
                    // Passed all 5!
                    StartCoroutine(VictorySequenceRoutine(cur.explanation));
                }
                else
                {
                    // Show positive feedback modal then proceed to next
                    ShowFeedbackModal(true, cur.explanation);
                }
            }
            else
            {
                if (SoundManager.Instance != null)
                {
                    if (wrongSound != null) SoundManager.Instance.PlaySFX(wrongSound);
                    else if (SoundManager.Instance.wrongStrategySound != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.wrongStrategySound);
                }

                OnAttemptFailed(cur.explanation);
            }
        }

        private void ShowFeedbackModal(bool isCorrect, string explanation)
        {
            if (feedbackModal == null) return;
            feedbackModal.SetActive(true);

            if (feedbackTitleText != null)
            {
                feedbackTitleText.text = isCorrect ? "<color=#00E676>CORRECT DECISION</color>" : "<color=#FF3D71>INCORRECT DECISION</color>";
            }
            if (feedbackExplanationText != null)
            {
                feedbackExplanationText.text = explanation;
            }
        }

        private void OnNextChallengeClicked()
        {
            currentChallengeIndex++;
            DisplayCurrentChallenge();
        }

        private void OnAttemptFailed(string reason)
        {
            failedAttemptsCount++;
            isTimerRunning = false;

            if (failureModal != null)
            {
                failureModal.SetActive(true);
            }
            if (failureReasonText != null)
            {
                failureReasonText.text = $"{reason}\n\n<size=18><color=#FFB300>Attempt failed! (Failed: {failedAttemptsCount}/3)</color></size>";
            }

            // Show 'Leave without reward' if 3 failures reached
            if (leaveWithoutRewardButton != null)
            {
                leaveWithoutRewardButton.gameObject.SetActive(failedAttemptsCount >= 3);
            }
        }

        private void OnRetryClicked()
        {
            // Fresh 5 random questions
            PickFiveRandomChallenges();
            currentChallengeIndex = 0;
            remainingTimer = totalTimerDuration;
            isTimerRunning = true;
            DisplayCurrentChallenge();
        }

        private void OnLeaveWithoutRewardClicked()
        {
            if (challengeCanvas != null) challengeCanvas.SetActive(false);
            if (failureModal != null) failureModal.SetActive(false);

            // Stand up, cinematic lottery box opens & portal unlocks
            if (CastleSequenceController.Instance != null)
            {
                CastleSequenceController.Instance.PlayLotteryBoxPortalCinematicSequence();
            }
            else if (computerTrigger != null)
            {
                computerTrigger.PlayerStandUpAndUnlockPortal(exitPortal);
            }
        }

        private IEnumerator VictorySequenceRoutine(string finalExplanation)
        {
            isTimerRunning = false;
            ShowFeedbackModal(true, finalExplanation);

            yield return new WaitForSeconds(2.0f);

            if (feedbackModal != null) feedbackModal.SetActive(false);
            if (victoryModal != null) victoryModal.SetActive(true);

            // 1. Award 1,000 Coins
            if (GameManager.Instance != null)
            {
                GameManager.Instance.takeProfit += rewardCoins;
            }

            // 2. Play Reward Coin Burst & Sound
            if (coinRewardParticles != null) coinRewardParticles.Play();
            if (SoundManager.Instance != null)
            {
                if (coinRewardSound != null) SoundManager.Instance.PlaySFX(coinRewardSound);
                else if (SoundManager.Instance.collectTPSound != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.collectTPSound);
            }

            yield return new WaitForSeconds(2.5f);

            if (challengeCanvas != null) challengeCanvas.SetActive(false);
            if (victoryModal != null) victoryModal.SetActive(false);

            // 3. Cinematic Lottery Box preview, portal spawn, and player continues to next level
            if (CastleSequenceController.Instance != null)
            {
                CastleSequenceController.Instance.PlayLotteryBoxPortalCinematicSequence();
            }
            else if (computerTrigger != null)
            {
                computerTrigger.PlayerStandUpAndUnlockPortal(exitPortal);
            }
        }

        private void SetButtonsInteractable(bool state)
        {
            if (buyButton != null) buyButton.interactable = state;
            if (sellButton != null) sellButton.interactable = state;
            if (waitButton != null) waitButton.interactable = state;
        }

        private void UpdateTimerDisplay()
        {
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(remainingTimer / 60f);
                int seconds = Mathf.FloorToInt(remainingTimer % 60f);
                timerText.text = $"{minutes:00}:{seconds:00}";
            }
        }
    }
}
