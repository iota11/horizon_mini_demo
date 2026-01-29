using UnityEngine;

/// <summary>
/// Manages audio for the KotobaMatch game.
/// Handles match sounds, mismatch sounds, game over, and background music.
/// </summary>
public class KotobaAudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;

    [Header("Audio Clips (Optional - can be set via theme)")]
    [SerializeField] private AudioClip matchSound;
    [SerializeField] private AudioClip mismatchSound;
    [SerializeField] private AudioClip gameOverSound;
    [SerializeField] private AudioClip backgroundMusic;

    [Header("Settings")]
    [SerializeField] private float sfxVolume = 1f;
    [SerializeField] private float musicVolume = 0.5f;

    private void Awake()
    {
        // Create audio sources if not assigned
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
        }
    }

    /// <summary>
    /// Set the theme and load its audio clips
    /// </summary>
    public void SetTheme(KotobaTheme theme)
    {
        if (theme == null) return;

        if (theme.matchSound != null)
            matchSound = theme.matchSound;

        if (theme.mismatchSound != null)
            mismatchSound = theme.mismatchSound;

        if (theme.gameOverSound != null)
            gameOverSound = theme.gameOverSound;

        if (theme.backgroundMusic != null)
        {
            backgroundMusic = theme.backgroundMusic;
            StartBackgroundMusic();
        }
    }

    /// <summary>
    /// Play the match sound
    /// </summary>
    public void PlayMatch()
    {
        PlaySFX(matchSound);
    }

    /// <summary>
    /// Play the mismatch sound
    /// </summary>
    public void PlayMismatch()
    {
        PlaySFX(mismatchSound);
    }

    /// <summary>
    /// Play the game over sound
    /// </summary>
    public void PlayGameOver()
    {
        PlaySFX(gameOverSound);
    }

    /// <summary>
    /// Play a one-shot sound effect
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }

    /// <summary>
    /// Start playing background music
    /// </summary>
    public void StartBackgroundMusic()
    {
        if (backgroundMusic != null && musicSource != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }
    }

    /// <summary>
    /// Stop background music
    /// </summary>
    public void StopBackgroundMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    /// <summary>
    /// Set the SFX volume
    /// </summary>
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    /// <summary>
    /// Set the music volume
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
    }
}
