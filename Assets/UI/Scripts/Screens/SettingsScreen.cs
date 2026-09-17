using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SciFiUI.Core;

namespace SciFiUI.Screens
{
    public class SettingsScreen : UIScreen
    {
        [Header("Audio Settings")]
        [SerializeField] private Slider musicSlider;
        [SerializeField] private TextMeshProUGUI musicPercentText;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private TextMeshProUGUI sfxPercentText;

        [Header("Graphics Settings")]
        [SerializeField] private Button graphicsPrevBtn;
        [SerializeField] private Button graphicsNextBtn;
        [SerializeField] private TextMeshProUGUI graphicsValueText;

        [Header("Navigation Buttons")]
        [SerializeField] private Button controlsButton;
        [SerializeField] private Button languageButton;
        [SerializeField] private Button backButton;

        private readonly string[] _graphicsPresets = { "Low", "Medium", "High", "Ultra" };
        private int _currentGraphicsIndex = 2; // Default High

        protected override void Awake()
        {
            base.Awake();

            LoadSettings();

            if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);

            if (graphicsPrevBtn != null) graphicsPrevBtn.onClick.AddListener(OnGraphicsPrev);
            if (graphicsNextBtn != null) graphicsNextBtn.onClick.AddListener(OnGraphicsNext);

            if (backButton != null) backButton.onClick.AddListener(OnBackClicked);
        }

        private void LoadSettings()
        {
            float music = PlayerPrefs.GetFloat("Settings_Music", 0.7f);
            float sfx = PlayerPrefs.GetFloat("Settings_SFX", 0.7f);
            _currentGraphicsIndex = PlayerPrefs.GetInt("Settings_Graphics", 2);

            if (musicSlider != null)
            {
                musicSlider.value = music;
                UpdateMusicPercent(music);
            }

            if (sfxSlider != null)
            {
                sfxSlider.value = sfx;
                UpdateSfxPercent(sfx);
            }

            UpdateGraphicsDisplay();
        }

        private void OnMusicChanged(float val)
        {
            UpdateMusicPercent(val);
            PlayerPrefs.SetFloat("Settings_Music", val);
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.SetMusicVolume(val);
            }
        }

        private void OnSfxChanged(float val)
        {
            UpdateSfxPercent(val);
            PlayerPrefs.SetFloat("Settings_SFX", val);
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.SetSFXVolume(val);
            }
        }

        private void UpdateMusicPercent(float val)
        {
            if (musicPercentText != null)
            {
                musicPercentText.text = $"{Mathf.RoundToInt(val * 100f)}%";
            }
        }

        private void UpdateSfxPercent(float val)
        {
            if (sfxPercentText != null)
            {
                sfxPercentText.text = $"{Mathf.RoundToInt(val * 100f)}%";
            }
        }

        private void OnGraphicsPrev()
        {
            _currentGraphicsIndex = Mathf.Clamp(_currentGraphicsIndex - 1, 0, _graphicsPresets.Length - 1);
            UpdateGraphicsDisplay();
            PlayerPrefs.SetInt("Settings_Graphics", _currentGraphicsIndex);
        }

        private void OnGraphicsNext()
        {
            _currentGraphicsIndex = Mathf.Clamp(_currentGraphicsIndex + 1, 0, _graphicsPresets.Length - 1);
            UpdateGraphicsDisplay();
            PlayerPrefs.SetInt("Settings_Graphics", _currentGraphicsIndex);
        }

        private void UpdateGraphicsDisplay()
        {
            if (graphicsValueText != null)
            {
                graphicsValueText.text = _graphicsPresets[_currentGraphicsIndex];
            }
        }

        private void OnBackClicked()
        {
            PlayerPrefs.Save();
            if (UIManager.Instance != null)
            {
                UIManager.Instance.CloseSettings();
            }
            else
            {
                Hide();
            }
        }
    }
}
