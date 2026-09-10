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
    public AudioClip jumpSound;
    public AudioClip slideSound;
    public AudioClip crashSound;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        PlayMusic(cyberpunkBGM);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;

        sfxSource.PlayOneShot(clip);
    }

    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }
}
