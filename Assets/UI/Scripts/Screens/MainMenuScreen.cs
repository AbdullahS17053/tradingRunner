using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace SciFiUI.Screens
{
    public class MainMenuScreen : MonoBehaviour
    {
        public static MainMenuScreen Instance { get; private set; }

        [Header("Sequence References")]
        [SerializeField] private Camera menuCamera;
        [SerializeField] private GameObject player;
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private RunnerController runnerController;
        [SerializeField] private CanvasGroup menuCanvasGroup;

        [Header("Camera Views")]
        [SerializeField] private Vector3 menuCamPosition = new Vector3(0.694f, 0.306f, 11.879f);
        [SerializeField] private Vector3 menuCamRotation = new Vector3(-8.387f, 562.243f, 0f);
        [SerializeField] private Vector3 runnerCamOffset = new Vector3(0f, 2.467f, -3.53f);
        [SerializeField] private Vector3 runnerCamRotation = new Vector3(15f, 0f, 0f);

        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button charactersButton;
        [SerializeField] private Button storeButton;
        [SerializeField] private Button rankingButton;
        [SerializeField] private Button forexQuizButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;

        [Header("Bottom Controls")]
        [SerializeField] private Button soundButton;
        [SerializeField] private Image soundIcon;
        [SerializeField] private Button musicButton;
        [SerializeField] private Image musicIcon;
        [SerializeField] private Button cloudSaveButton;

        [Header("Audio Sprites")]
        [SerializeField] private Sprite soundOnSprite;
        [SerializeField] private Sprite soundOffSprite;
        [SerializeField] private Sprite musicOnSprite;
        [SerializeField] private Sprite musicOffSprite;

        [Header("Top Right Currencies")]
        [SerializeField] private GameObject currenciesContainer;
        [SerializeField] private TextMeshProUGUI coinsText;
        [SerializeField] private TextMeshProUGUI gemsText;

        [Header("Sequence Timings")]
        [SerializeField] private float cameraTransitionDuration = 1.2f;
        [SerializeField] private float standCompletionWait = 0.15f;
        [SerializeField] private string targetGameplaySceneName = "City";
        [SerializeField] private int targetGameplaySceneIndex = 0; // Fallback index

        [Header("Settings Panel")]
        [SerializeField] private GameObject settingsPanel;

        private bool isTransitioning = false;
        private bool isSoundMuted = false;
        private bool isMusicMuted = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(this);
                return;
            }

            if (menuCanvasGroup == null)
            {
                menuCanvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void Start()
        {
            // 1. Locate references if not assigned
            if (menuCamera == null)
            {
                var cameras = FindObjectsOfType<Camera>();
                foreach (var cam in cameras)
                {
                    if (cam.gameObject.name == "MenuCamera")
                    {
                        menuCamera = cam;
                        break;
                    }
                }
                if (menuCamera == null) menuCamera = Camera.main;
            }

            if (player == null)
            {
                var runner = FindObjectOfType<RunnerController>(true);
                if (runner != null)
                {
                    player = runner.gameObject;
                    runnerController = runner;
                    playerAnimator = runner.GetComponentInChildren<Animator>();
                }
            }
            else
            {
                if (runnerController == null) runnerController = player.GetComponent<RunnerController>();
                if (playerAnimator == null) playerAnimator = player.GetComponentInChildren<Animator>();
            }

            // 2. Disable RunnerController so player doesn't move forward during menu
            if (runnerController != null)
            {
                runnerController.enabled = false;
            }

            // 3. Pose the player in SittingPose
            if (playerAnimator != null)
            {
                playerAnimator.Play("SittingPose", 0, 0f);
                playerAnimator.SetBool("GameStarted", false);
            }

            // 4. Setup Menu Camera framing
            if (menuCamera != null)
            {
                menuCamera.transform.position = menuCamPosition;
                menuCamera.transform.rotation = Quaternion.Euler(menuCamRotation);
            }

            // 5. Disable player's internal camera during menu so menuCamera is sole view
            if (player != null && menuCamera != null)
            {
                foreach (var cam in player.GetComponentsInChildren<Camera>(true))
                {
                    if (cam != menuCamera) cam.gameObject.SetActive(false);
                }
            }

            // 6. Setup button listeners
            if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
            if (soundButton != null) soundButton.onClick.AddListener(OnSoundToggleClicked);
            if (musicButton != null) musicButton.onClick.AddListener(OnMusicToggleClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
            if (charactersButton != null) charactersButton.onClick.AddListener(OnCharactersClicked);
            if (storeButton != null) storeButton.onClick.AddListener(OnStoreClicked);
            if (rankingButton != null) rankingButton.onClick.AddListener(OnRankingClicked);
            if (forexQuizButton != null) forexQuizButton.onClick.AddListener(OnForexQuizClicked);
            if (cloudSaveButton != null) cloudSaveButton.onClick.AddListener(OnCloudSaveClicked);
            if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);

            // 7. Ensure currencies container is active and populate current balances
            if (currenciesContainer != null)
            {
                currenciesContainer.SetActive(true);
            }
            UpdateCurrenciesDisplay();

            // 8. Initialize audio states
            if (SoundManager.Instance != null)
            {
                isSoundMuted = SoundManager.Instance.GetSFXVolume() <= 0.01f;
                isMusicMuted = SoundManager.Instance.GetMusicVolume() <= 0.01f;
            }
            UpdateAudioIcons();
        }

        private void OnEnable()
        {
            UpdateCurrenciesDisplay();
            if (SoundManager.Instance != null)
            {
                isSoundMuted = SoundManager.Instance.GetSFXVolume() <= 0.01f;
                isMusicMuted = SoundManager.Instance.GetMusicVolume() <= 0.01f;
            }
            UpdateAudioIcons();
        }

        public void UpdateCurrenciesDisplay()
        {
            int coins = GameManager.GetTotalCoins();
            int gems = GemReviveManager.Instance != null ? GemReviveManager.Instance.GetGems() : PlayerPrefs.GetInt("PlayerGems_Total", 3500);

            if (coinsText != null)
            {
                coinsText.text = coins.ToString("N0");
            }
            if (gemsText != null)
            {
                gemsText.text = gems.ToString("N0");
            }
        }

        public void OnPlayClicked()
        {
            if (isTransitioning) return;
            isTransitioning = true;

            if (SoundManager.Instance != null && SoundManager.Instance.uiClickSound != null)
            {
                if (SoundManager.Instance.sfxSource != null)
                {
                    SoundManager.Instance.sfxSource.PlayOneShot(SoundManager.Instance.uiClickSound);
                }
            }

            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.blocksRaycasts = false;
                menuCanvasGroup.interactable = false;
            }

            // Immediately fade to black and load the gameplay level (skipping camera swoop & stand-up sequence)
            string currentSceneName = SceneManager.GetActiveScene().name;
            string sceneToLoad = !string.IsNullOrEmpty(targetGameplaySceneName) ? targetGameplaySceneName : "City";

            // Safety guard: NEVER reload MainMenu when Play is clicked
            if (sceneToLoad.Equals(currentSceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                sceneToLoad = "City";
            }

            if (LevelLoader.Instance != null)
            {
                LevelLoader.Instance.LoadScene(sceneToLoad);
            }
            else
            {
                SceneManager.LoadScene(sceneToLoad);
            }
        }

        public void Show(bool immediate = false)
        {
            UpdateCurrenciesDisplay();
            if (SoundManager.Instance != null)
            {
                isSoundMuted = SoundManager.Instance.GetSFXVolume() <= 0.01f;
                isMusicMuted = SoundManager.Instance.GetMusicVolume() <= 0.01f;
            }
            UpdateAudioIcons();

            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.alpha = 1f;
                menuCanvasGroup.blocksRaycasts = true;
                menuCanvasGroup.interactable = true;
            }
            gameObject.SetActive(true);
        }

        public void Hide(bool immediate = false)
        {
            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.alpha = 0f;
                menuCanvasGroup.blocksRaycasts = false;
                menuCanvasGroup.interactable = false;
            }
        }

        private void OnSoundToggleClicked()
        {
            isSoundMuted = !isSoundMuted;
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.SetSFXVolume(isSoundMuted ? 0f : 0.7f);
            }
            UpdateAudioIcons();
        }

        private void OnMusicToggleClicked()
        {
            isMusicMuted = !isMusicMuted;
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.SetMusicVolume(isMusicMuted ? 0f : 0.7f);
            }
            UpdateAudioIcons();
        }

        private void UpdateAudioIcons()
        {
            if (soundIcon != null && soundOnSprite != null && soundOffSprite != null)
            {
                soundIcon.sprite = isSoundMuted ? soundOffSprite : soundOnSprite;
            }
            if (musicIcon != null && musicOnSprite != null && musicOffSprite != null)
            {
                musicIcon.sprite = isMusicMuted ? musicOffSprite : musicOnSprite;
            }
        }

        private void OnCharactersClicked()
        {
            Debug.Log("[MainMenu] Characters clicked");
            if (SoundManager.Instance != null && SoundManager.Instance.uiClickSound != null)
                SoundManager.Instance.sfxSource?.PlayOneShot(SoundManager.Instance.uiClickSound);
        }

        private void OnStoreClicked()
        {
            Debug.Log("[MainMenu] Store clicked");
            if (SoundManager.Instance != null && SoundManager.Instance.uiClickSound != null)
                SoundManager.Instance.sfxSource?.PlayOneShot(SoundManager.Instance.uiClickSound);
        }

        private void OnRankingClicked()
        {
            Debug.Log("[MainMenu] Ranking clicked");
            if (SoundManager.Instance != null && SoundManager.Instance.uiClickSound != null)
                SoundManager.Instance.sfxSource?.PlayOneShot(SoundManager.Instance.uiClickSound);
        }

        private void OnForexQuizClicked()
        {
            Debug.Log("[MainMenu] Forex Quiz clicked");
            if (SoundManager.Instance != null && SoundManager.Instance.uiClickSound != null)
                SoundManager.Instance.sfxSource?.PlayOneShot(SoundManager.Instance.uiClickSound);

            RunnerForex.Castle.ForexPatternsPanel.OpenPanel();
        }

        private void OnSettingsClicked()
        {
            Debug.Log("[MainMenu] Settings clicked");
            if (SoundManager.Instance != null && SoundManager.Instance.uiClickSound != null)
                SoundManager.Instance.sfxSource?.PlayOneShot(SoundManager.Instance.uiClickSound);

            if (settingsPanel == null) settingsPanel = GameObject.Find("SettingsPanel");
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }
        }

        private void OnCloudSaveClicked()
        {
            Debug.Log("[MainMenu] Cloud Save (Firebase) clicked");
            if (SoundManager.Instance != null && SoundManager.Instance.uiClickSound != null)
                SoundManager.Instance.sfxSource?.PlayOneShot(SoundManager.Instance.uiClickSound);
        }

        private void OnExitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
