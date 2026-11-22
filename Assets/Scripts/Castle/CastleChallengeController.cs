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
        public GameObject mainChallengePanel;
        public GameObject feedbackModal;
        public GameObject victoryModal;
        public GameObject failureModal;

        public GameObject GetMainChallengePanel()
        {
            if (mainChallengePanel != null) return mainChallengePanel;

            // Search all active and inactive transforms in loaded scenes
            Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (Transform t in allTransforms)
            {
                if (t != null && t.gameObject.name == "MainChallengePanel" && t.gameObject.scene.isLoaded)
                {
                    mainChallengePanel = t.gameObject;
                    return mainChallengePanel;
                }
            }

            GameObject hud = GameObject.Find("CastleGameplayHUD");
            if (hud != null)
            {
                Transform found = hud.transform.Find("MainChallengePanel");
                if (found != null)
                {
                    mainChallengePanel = found.gameObject;
                    return mainChallengePanel;
                }
            }

            return null;
        }

        [Header("Challenge UI Elements")]
        public TextMeshProUGUI pairTimeframeText;
        public TextMeshProUGUI scenarioDescriptionText;
        public TextMeshProUGUI progressCounterText;
        public TextMeshProUGUI timerText;

        [Header("Special Banners")]
        public GameObject newsSpreadBanner;   // Challenge 24
        public GameObject riskWarningBanner;  // Challenge 27

        [Header("30 Candlestick Pattern Card & Movement Animation")]
        public GameObject activePatternCardContainer;
        public Image activePatternCardImage;
        public TextMeshProUGUI activePatternTitleText;
        public TextMeshProUGUI activePatternMoveText;
        public ForexPatternMovementAnimator activePatternAnimator;
        public Button openCodexButton;

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

            CastleSequenceController.EnsureInstanceExists();
        }

        private void Start()
        {
            PickFiveRandomChallenges();

            if (buyButton != null) buyButton.onClick.AddListener(() => OnActionSelected(TradeAction.BUY));
            if (sellButton != null) sellButton.onClick.AddListener(() => OnActionSelected(TradeAction.SELL));
            if (waitButton != null) waitButton.onClick.AddListener(() => OnActionSelected(TradeAction.WAIT));

            if (nextChallengeButton != null) nextChallengeButton.onClick.AddListener(OnNextChallengeClicked);
            if (retryButton != null) retryButton.onClick.AddListener(OnRetryClicked);
            if (leaveWithoutRewardButton != null) leaveWithoutRewardButton.onClick.AddListener(OnLeaveWithoutRewardClicked);

            if (challengeCanvas != null) challengeCanvas.SetActive(false);
            GameObject panel = GetMainChallengePanel();
            if (panel != null) panel.SetActive(false);
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
            GameObject panel = GetMainChallengePanel();
            if (panel != null)
            {
                panel.SetActive(true);
                Debug.Log("[CastleChallengeController] MainChallengePanel successfully enabled!");
            }
            else
            {
                Debug.LogWarning("[CastleChallengeController] MainChallengePanel could not be found!");
            }

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

            EnsurePatternCardUI();

            ForexPatternInfo pInfo = ForexPatternDatabase.GetPattern(cur.patternId);
            if (pInfo != null)
            {
                if (activePatternCardImage != null)
                {
                    Sprite sp = pInfo.GetSprite();
                    if (sp != null) activePatternCardImage.sprite = sp;
                }
                if (activePatternTitleText != null)
                {
                    activePatternTitleText.text = pInfo.DisplayTitle;
                }
                if (activePatternMoveText != null)
                {
                    activePatternMoveText.text = $"MOVE: {pInfo.moveName.ToUpper()}";
                    activePatternMoveText.color = pInfo.BiasColor;
                }
                if (activePatternAnimator != null)
                {
                    activePatternAnimator.animationType = pInfo.animationType;
                }
            }

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

        public void LoadSpecificPatternChallenge(int patternId)
        {
            StartCastleChallengeSession();
            var all = CastleChallengeDatabase.GetAllChallenges();
            CastleChallenge specific = all.Find(c => c.patternId == patternId);
            if (specific != null)
            {
                activeFiveChallenges.Clear();
                activeFiveChallenges.Add(specific);
                currentChallengeIndex = 0;
                DisplayCurrentChallenge();
            }
        }

        public void EnsurePatternCardUI()
        {
            GameObject panel = GetMainChallengePanel();
            if (panel == null) return;

            if (openCodexButton == null)
            {
                Transform foundBtn = panel.transform.Find("OpenCodexButton");
                if (foundBtn != null)
                {
                    openCodexButton = foundBtn.GetComponent<Button>();
                }
                else
                {
                    GameObject btnObj = new GameObject("OpenCodexButton", typeof(RectTransform), typeof(Image), typeof(Button));
                    btnObj.transform.SetParent(panel.transform, false);
                    RectTransform brt = btnObj.GetComponent<RectTransform>();
                    brt.anchoredPosition = new Vector2(280, 605);
                    brt.sizeDelta = new Vector2(220, 36);
                    Image bImg = btnObj.GetComponent<Image>();
                    bImg.color = new Color(0.1f, 0.45f, 0.9f, 0.9f);
                    openCodexButton = btnObj.GetComponent<Button>();

                    GameObject btnTxtObj = new GameObject("CodexText", typeof(RectTransform), typeof(TextMeshProUGUI));
                    btnTxtObj.transform.SetParent(btnObj.transform, false);
                    RectTransform btrt = btnTxtObj.GetComponent<RectTransform>();
                    btrt.anchorMin = Vector2.zero;
                    btrt.anchorMax = Vector2.one;
                    btrt.sizeDelta = Vector2.zero;
                    TextMeshProUGUI bt = btnTxtObj.GetComponent<TextMeshProUGUI>();
                    bt.text = "📖 30 PATTERNS";
                    bt.fontSize = 17;
                    bt.fontStyle = FontStyles.Bold;
                    bt.alignment = TextAlignmentOptions.Center;
                    bt.color = Color.white;
                }

                if (openCodexButton != null)
                {
                    openCodexButton.onClick.RemoveAllListeners();
                    openCodexButton.onClick.AddListener(() => ForexPatternsPanel.OpenPanel());
                }
            }

            if (activePatternCardContainer == null)
            {
                Transform foundCont = panel.transform.Find("ActivePatternCardContainer");
                if (foundCont != null)
                {
                    activePatternCardContainer = foundCont.gameObject;
                    activePatternCardImage = foundCont.Find("CardImage")?.GetComponent<Image>();
                    activePatternTitleText = foundCont.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
                    activePatternMoveText = foundCont.Find("MoveText")?.GetComponent<TextMeshProUGUI>();
                    activePatternAnimator = activePatternCardImage?.GetComponent<ForexPatternMovementAnimator>();
                }
                else
                {
                    GameObject contObj = new GameObject("ActivePatternCardContainer", typeof(RectTransform), typeof(Image));
                    contObj.transform.SetParent(panel.transform, false);
                    RectTransform crt = contObj.GetComponent<RectTransform>();
                    crt.anchoredPosition = new Vector2(0, 485);
                    crt.sizeDelta = new Vector2(140, 165);
                    Image cBg = contObj.GetComponent<Image>();
                    cBg.color = new Color(0.04f, 0.07f, 0.16f, 0.95f);
                    activePatternCardContainer = contObj;

                    // Title
                    GameObject ptObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
                    ptObj.transform.SetParent(contObj.transform, false);
                    RectTransform ptrt = ptObj.GetComponent<RectTransform>();
                    ptrt.anchoredPosition = new Vector2(0, 68);
                    ptrt.sizeDelta = new Vector2(130, 24);
                    activePatternTitleText = ptObj.GetComponent<TextMeshProUGUI>();
                    activePatternTitleText.fontSize = 12;
                    activePatternTitleText.fontStyle = FontStyles.Bold;
                    activePatternTitleText.alignment = TextAlignmentOptions.Center;
                    activePatternTitleText.color = new Color(0f, 0.95f, 1f);

                    // Card Image
                    GameObject imgObj = new GameObject("CardImage", typeof(RectTransform), typeof(Image));
                    imgObj.transform.SetParent(contObj.transform, false);
                    RectTransform irt = imgObj.GetComponent<RectTransform>();
                    irt.anchoredPosition = new Vector2(0, 2);
                    irt.sizeDelta = new Vector2(100, 105);
                    activePatternCardImage = imgObj.GetComponent<Image>();
                    activePatternAnimator = imgObj.AddComponent<ForexPatternMovementAnimator>();

                    // Move Text
                    GameObject mtObj = new GameObject("MoveText", typeof(RectTransform), typeof(TextMeshProUGUI));
                    mtObj.transform.SetParent(contObj.transform, false);
                    RectTransform mtrt = mtObj.GetComponent<RectTransform>();
                    mtrt.anchoredPosition = new Vector2(0, -66);
                    mtrt.sizeDelta = new Vector2(130, 22);
                    activePatternMoveText = mtObj.GetComponent<TextMeshProUGUI>();
                    activePatternMoveText.fontSize = 11;
                    activePatternMoveText.fontStyle = FontStyles.Bold;
                    activePatternMoveText.alignment = TextAlignmentOptions.Center;
                }
            }
        }

        private void OnActionSelected(TradeAction chosenAction)
        {
            if (isAnswering) return;
            if (activeFiveChallenges == null || activeFiveChallenges.Count == 0 || currentChallengeIndex < 0 || currentChallengeIndex >= activeFiveChallenges.Count)
            {
                return;
            }

            isAnswering = true;
            SetButtonsInteractable(false);

            CastleChallenge cur = activeFiveChallenges[currentChallengeIndex];
            bool isCorrect = (chosenAction == cur.answer);

            if (isCorrect)
            {
                if (activePatternAnimator != null)
                {
                    activePatternAnimator.PlayBurstAnimation();
                }

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
            GameObject panel = GetMainChallengePanel();
            if (panel != null) panel.SetActive(false);
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
            GameObject panel = GetMainChallengePanel();
            if (panel != null) panel.SetActive(false);
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
