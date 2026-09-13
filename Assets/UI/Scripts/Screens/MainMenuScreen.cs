using UnityEngine;
using UnityEngine.UI;
using SciFiUI.Core;
using SciFiUI.Effects;

namespace SciFiUI.Screens
{
    public class MainMenuScreen : UIScreen
    {
        [Header("Menu Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private Button settingsButton;

        protected override void Awake()
        {
            base.Awake();

            if (playButton != null) playButton.onClick.AddListener(OnPlayClicked);
            if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        public override void Show(bool immediate = false)
        {
            base.Show(immediate);

            var glitches = GetComponentsInChildren<CyberpunkUIGlitch>(true);
            foreach (var g in glitches)
            {
                if (g != null)
                {
                    g.gameObject.SetActive(true);
                    g.PlayShow();
                }
            }
        }

        public override void Hide(bool immediate = false)
        {
            var glitches = GetComponentsInChildren<CyberpunkUIGlitch>(false);
            foreach (var g in glitches)
            {
                if (g != null && g.gameObject.activeInHierarchy)
                {
                    g.PlayHide(false);
                }
            }

            base.Hide(immediate);
        }

        private void OnPlayClicked()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.StartGame();
            }
        }

        private void OnSettingsClicked()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OpenSettings();
            }
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
