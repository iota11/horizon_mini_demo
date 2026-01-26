using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip placeSound;
    [SerializeField] private AudioClip perfectSound;
    [SerializeField] private AudioClip failSound;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        audioSource.playOnAwake = false;
    }

    public void SetTheme(StackTheme theme)
    {
        if (theme != null)
        {
            placeSound = theme.placementSound;
            perfectSound = theme.perfectSound;
            failSound = theme.failSound;

            if (theme.backgroundMusic != null)
            {
                audioSource.clip = theme.backgroundMusic;
                audioSource.loop = true;
                audioSource.Play();
            }
        }
    }

    public void PlayPlace()
    {
        if (placeSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(placeSound);
        }
    }

    public void PlayPerfect()
    {
        if (perfectSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(perfectSound);
        }
        else
        {
            PlayPlace(); // Fallback to place sound
        }
    }

    public void PlayFail()
    {
        if (failSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(failSound);
        }
    }

    public void StopMusic()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
}
