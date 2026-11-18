using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    public static MainMenuController Instance { get; private set; }

    [Header("Sequence References")]
    public Camera menuCamera;
    public GameObject player;
    public Animator playerAnimator;
    public RunnerController runnerController;
    public CanvasGroup menuCanvasGroup;

    [Header("Camera Views")]
    public Vector3 menuCamPosition = new Vector3(-1.35f, 0.95f, 12.1f);
    public Vector3 menuCamRotation = new Vector3(4.8f, 154.2f, 0f);
    public Vector3 runnerCamOffset = new Vector3(0f, 2.467f, -3.53f);
    public Vector3 runnerCamRotation = new Vector3(15f, 0f, 0f);

    [Header("Buttons")]
    public Button playButton;
    public Button charactersButton;
    public Button storeButton;
    public Button rankingButton;
    public Button forexQuizButton;
    public Button settingsButton;

    [Header("Bottom Controls")]
    public Button soundButton;
    public Image soundIcon;
    public Button musicButton;
    public Image musicIcon;
    public Button cloudSaveButton;

    [Header("Audio Sprites")]
    public Sprite soundOnSprite;
    public Sprite soundOffSprite;
    public Sprite musicOnSprite;
    public Sprite musicOffSprite;

    [Header("Sequence Timings")]
    public float cameraTransitionDuration = 1.6f;
    public float sprintBeforeLoadDuration = 0.8f;
    public int targetGameplaySceneIndex = 0; // City scene

    private bool isTransitioning = false;
    private bool isSoundMuted = false;
    private bool isMusicMuted = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
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

        // 5. Setup button listeners
        if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
        if (soundButton != null) soundButton.onClick.AddListener(OnSoundToggleClicked);
        if (musicButton != null) musicButton.onClick.AddListener(OnMusicToggleClicked);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
        if (charactersButton != null) charactersButton.onClick.AddListener(OnCharactersClicked);
        if (storeButton != null) storeButton.onClick.AddListener(OnStoreClicked);
        if (rankingButton != null) rankingButton.onClick.AddListener(OnRankingClicked);
        if (forexQuizButton != null) forexQuizButton.onClick.AddListener(OnForexQuizClicked);
        if (cloudSaveButton != null) cloudSaveButton.onClick.AddListener(OnCloudSaveClicked);

        // Initialize audio states
        if (SoundManager.Instance != null)
        {
            isSoundMuted = SoundManager.Instance.GetSFXVolume() <= 0.01f;
            isMusicMuted = SoundManager.Instance.GetMusicVolume() <= 0.01f;
        }
        UpdateAudioIcons();
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

        StartCoroutine(StartGameSequenceRoutine());
    }

    private IEnumerator StartGameSequenceRoutine()
    {
        // 1. Fade out the menu UI
        float fadeTime = 0.35f;
        float elapsed = 0f;
        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.blocksRaycasts = false;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                menuCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                yield return null;
            }
            menuCanvasGroup.alpha = 0f;
            menuCanvasGroup.gameObject.SetActive(false);
        }

        // 2. Trigger SitToStand animation on player
        if (playerAnimator != null)
        {
            playerAnimator.ResetTrigger("SitToStand");
            playerAnimator.SetTrigger("SitToStand");
        }

        // 3. Smooth Camera Swoop from menu view to behind runner
        Vector3 camStartPos = menuCamera != null ? menuCamera.transform.position : menuCamPosition;
        Quaternion camStartRot = menuCamera != null ? menuCamera.transform.rotation : Quaternion.Euler(menuCamRotation);

        Vector3 playerPos = player != null ? player.transform.position : Vector3.zero;
        Quaternion playerRot = player != null ? player.transform.rotation : Quaternion.identity;

        Vector3 camTargetPos = playerPos + (playerRot * runnerCamOffset);
        Quaternion camTargetRot = playerRot * Quaternion.Euler(runnerCamRotation);

        elapsed = 0f;
        while (elapsed < cameraTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / cameraTransitionDuration);
            // Smooth ease in-out curve
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (player != null)
            {
                playerPos = player.transform.position;
                playerRot = player.transform.rotation;
                camTargetPos = playerPos + (playerRot * runnerCamOffset);
                camTargetRot = playerRot * Quaternion.Euler(runnerCamRotation);
            }

            if (menuCamera != null)
            {
                menuCamera.transform.position = Vector3.Lerp(camStartPos, camTargetPos, smoothT);
                menuCamera.transform.rotation = Quaternion.Slerp(camStartRot, camTargetRot, smoothT);
            }

            yield return null;
        }

        // Snap to exact target behind player
        if (menuCamera != null)
        {
            menuCamera.transform.position = camTargetPos;
            menuCamera.transform.rotation = camTargetRot;
        }

        // 4. Enable runner controller & start sprint
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("GameStarted", true);
        }
        if (runnerController != null)
        {
            runnerController.enabled = true;
        }

        // 5. If player has child Main Camera, activate it and parent
        if (player != null && menuCamera != null)
        {
            Camera playerCam = player.GetComponentInChildren<Camera>(true);
            if (playerCam != null && playerCam != menuCamera)
            {
                playerCam.gameObject.SetActive(true);
                menuCamera.gameObject.SetActive(false);
            }
            else
            {
                menuCamera.transform.SetParent(player.transform, true);
            }
        }

        // 6. Brief sprint forward then load City gameplay scene
        yield return new WaitForSeconds(sprintBeforeLoadDuration);

        if (LevelLoader.Instance != null)
        {
            LevelLoader.Instance.LoadScene(targetGameplaySceneIndex);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetGameplaySceneIndex);
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
        if (soundIcon != null)
        {
            soundIcon.sprite = isSoundMuted ? soundOffSprite : soundOnSprite;
        }
        if (musicIcon != null)
        {
            musicIcon.sprite = isMusicMuted ? musicOffSprite : musicOnSprite;
        }
    }

    private void OnCharactersClicked()
    {
        Debug.Log("[MainMenu] Characters clicked");
    }

    private void OnStoreClicked()
    {
        Debug.Log("[MainMenu] Store clicked");
    }

    private void OnRankingClicked()
    {
        Debug.Log("[MainMenu] Ranking clicked");
    }

    private void OnForexQuizClicked()
    {
        Debug.Log("[MainMenu] Forex Quiz clicked");
    }

    private void OnSettingsClicked()
    {
        Debug.Log("[MainMenu] Settings clicked");
        var settings = GameObject.Find("SettingsPanel");
        if (settings != null)
        {
            settings.SetActive(true);
        }
    }

    private void OnCloudSaveClicked()
    {
        Debug.Log("[MainMenu] Cloud Save (Firebase) clicked");
    }
}
