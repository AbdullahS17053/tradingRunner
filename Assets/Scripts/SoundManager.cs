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
            Destroy(gameObject);
            return;
        }
        Instance = this;

        DontDestroyOnLoad(gameObject);
        LoadVolumeSettings();
    }

    private void Start()
    {
        PlayMusic(cyberpunkBGM);
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

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;

        if (musicSource != null)
        {
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
