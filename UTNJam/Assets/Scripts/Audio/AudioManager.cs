using UnityEngine;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("---------- Audio Source ----------")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("---------- Fade Settings ----------")]
    [SerializeField] private float fadeDuration;

    private Coroutine fadeCoroutine;
    private float targetVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        targetVolume = musicSource.volume;
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeMusic(clip));
    }

    private IEnumerator FadeMusic(AudioClip newClip)
    {
        // Fade out (si ya hay algo sonando)
        if (musicSource.isPlaying)
        {
            float startVolume = musicSource.volume;
            float t = 0f;

            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeDuration);
                yield return null;
            }

            musicSource.volume = 0f;
        }

        // Cambia el clip
        musicSource.clip = newClip;
        musicSource.Play();

        // Fade in
        float t2 = 0f;
        while (t2 < fadeDuration)
        {
            t2 += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, t2 / fadeDuration);
            yield return null;
        }

        musicSource.volume = targetVolume;
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }
}