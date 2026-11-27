using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("Drag the Music AudioSource here")]
    public AudioSource musicSource;
    [Tooltip("Drag the SFX AudioSource here")]
    public AudioSource sfxSource;

    [Header("Background Music")]
    [Tooltip("Theme music played on the Main Menu")]
    public AudioClip menuBGM;
    [Tooltip("Theme music played on the City level")]
    public AudioClip cityBGM;
    [Tooltip("Theme music played on the Space level")]
    public AudioClip spaceBGM;
    [Tooltip("Theme music played on the Labyrinth level")]
    public AudioClip labyrinthBGM;
    [Tooltip("Legacy fallback for cyberpunk background music")]
    public AudioClip cyberpunkBGM;

    [Header("Sound Effects")]
    public AudioClip collectTPSound;
    public AudioClip correctStrategySound;
    public AudioClip wrongStrategySound;
    public AudioClip gameOverSound;
    public AudioClip crashSound;
    public AudioClip fomoLaserShotSound;
    public AudioClip fomoOpenGunSound;
    public AudioClip fomoAscendSound;
    public AudioClip fomoDescendSound;
    public AudioClip uiClickSound;

    private const string MUSIC_KEY = "Settings_Music";
    private const string SFX_KEY = "Settings_SFX";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Instance.AdoptClipsFrom(this);
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        DontDestroyOnLoad(gameObject);
        EnsureClipsLoaded();
        LoadVolumeSettings();
    }

    public void AdoptClipsFrom(SoundManager other)
    {
        if (other == null) return;
        if (menuBGM == null) menuBGM = other.menuBGM;
        if (cityBGM == null) cityBGM = other.cityBGM;
        if (spaceBGM == null) spaceBGM = other.spaceBGM;
        if (labyrinthBGM == null) labyrinthBGM = other.labyrinthBGM;
        if (cyberpunkBGM == null) cyberpunkBGM = other.cyberpunkBGM;

        if (collectTPSound == null) collectTPSound = other.collectTPSound;
        if (correctStrategySound == null) correctStrategySound = other.correctStrategySound;
        if (wrongStrategySound == null) wrongStrategySound = other.wrongStrategySound;
        if (gameOverSound == null) gameOverSound = other.gameOverSound;
        if (crashSound == null) crashSound = other.crashSound;
        if (fomoLaserShotSound == null) fomoLaserShotSound = other.fomoLaserShotSound;
        if (fomoOpenGunSound == null) fomoOpenGunSound = other.fomoOpenGunSound;
        if (fomoAscendSound == null) fomoAscendSound = other.fomoAscendSound;
        if (fomoDescendSound == null) fomoDescendSound = other.fomoDescendSound;
        if (uiClickSound == null) uiClickSound = other.uiClickSound;
    }

    public void EnsureClipsLoaded()
    {
        if (cityBGM == null && cyberpunkBGM != null)
        {
            cityBGM = cyberpunkBGM;
        }
        if (cyberpunkBGM == null && cityBGM != null)
        {
            cyberpunkBGM = cityBGM;
        }

        if (cityBGM == null)
        {
            cityBGM = Resources.Load<AudioClip>("Audio/BGM/City_Theme");
            if (cyberpunkBGM == null) cyberpunkBGM = cityBGM;
        }
        if (spaceBGM == null)
        {
            spaceBGM = Resources.Load<AudioClip>("Audio/BGM/Space_Theme");
        }
        if (labyrinthBGM == null)
        {
            labyrinthBGM = Resources.Load<AudioClip>("Audio/BGM/Labyrinth_Theme");
        }
        if (menuBGM == null)
        {
            menuBGM = Resources.Load<AudioClip>("Audio/BGM/Menu_Theme");
        }
    }

    public AudioClip GetBGMForScene(string sceneName)
    {
        EnsureClipsLoaded();
        if (string.IsNullOrEmpty(sceneName))
        {
            return cityBGM ?? cyberpunkBGM;
        }

        if (sceneName.Equals("MainMenu", System.StringComparison.OrdinalIgnoreCase))
        {
            return menuBGM;
        }
        else if (sceneName.Equals("Space", System.StringComparison.OrdinalIgnoreCase))
        {
            return spaceBGM != null ? spaceBGM : (cityBGM ?? cyberpunkBGM);
        }
        else if (sceneName.Equals("Labyrinth", System.StringComparison.OrdinalIgnoreCase))
        {
            return labyrinthBGM != null ? labyrinthBGM : (cityBGM ?? cyberpunkBGM);
        }
        else // City or any other gameplay scene
        {
            return cityBGM != null ? cityBGM : cyberpunkBGM;
        }
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        AudioClip targetClip = GetBGMForScene(scene.name);
        if (targetClip != null)
        {
            PlayMusic(targetClip);
        }
    }

    private void Start()
    {
        string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        AudioClip targetClip = GetBGMForScene(activeScene);
        if (targetClip != null)
        {
            PlayMusic(targetClip);
        }
    }

    public void RestartMusic()
    {
        string activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        AudioClip targetClip = GetBGMForScene(activeScene);
        if (targetClip != null)
        {
            PlayMusic(targetClip, forceRestart: true);
        }
    }

    public void LoadVolumeSettings()
    {
        float musicVol = PlayerPrefs.GetFloat(MUSIC_KEY, 0.7f);
        float sfxVol = PlayerPrefs.GetFloat(SFX_KEY, 0.7f);

        SetMusicVolume(musicVol);
        SetSFXVolume(sfxVol);
    }

    public void SetMusicVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = volume;
        }
        PlayerPrefs.SetFloat(MUSIC_KEY, volume);
    }

    public void SetSFXVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            sfxSource.volume = volume;
        }
        PlayerPrefs.SetFloat(SFX_KEY, volume);
    }

    public float GetMusicVolume()
    {
        if (musicSource != null)
        {
            return musicSource.volume;
        }
        return PlayerPrefs.GetFloat(MUSIC_KEY, 0.7f);
    }

    public float GetSFXVolume()
    {
        if (sfxSource != null)
        {
            return sfxSource.volume;
        }
        return PlayerPrefs.GetFloat(SFX_KEY, 0.7f);
    }

    public void PlayMusic(AudioClip clip, bool forceRestart = false)
    {
        if (clip == null) return;

        if (musicSource != null)
        {
            if (!forceRestart && musicSource.clip == clip && musicSource.isPlaying)
            {
                return;
            }

            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.volume = GetMusicVolume();
            musicSource.Play();
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;

        if (sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, GetSFXVolume());
        }
    }

    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }

    public void PauseMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Pause();
        }
    }

    public void ResumeMusic()
    {
        if (musicSource != null && !musicSource.isPlaying && musicSource.clip != null)
        {
            musicSource.UnPause();
        }
    }
}
