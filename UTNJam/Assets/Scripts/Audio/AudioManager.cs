using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("---------- Audio Source ----------")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("---------- Audio Clip ----------")]
    public AudioClip pickUp;
    public AudioClip bind;
    public AudioClip release;
    public AudioClip fall;
    public AudioClip uiButton;
    public AudioClip acept;

    [Header("---------- Fade Settings ----------")]
    [SerializeField] private float fadeDuration;

    [Header("---------- Botones ----------")]
    [Tooltip("Textos de los botones que suenan con 'acept' (sin importar mayúsculas ni tildes). El resto suena con 'uiButton'.")]
    [SerializeField] private string[] textosBotonesAceptar =
        { "Jugar", "Empezar de nuevo", "Rehacer ultimo grupo", "Continuar", "Reintentar", "Regresar al principio", "Regresar al ultimo grupo" };

    private Coroutine fadeCoroutine;
    private float targetVolume;
    private readonly HashSet<Button> botonesConSonido = new HashSet<Button>();

    public AudioClip MusicaActual => musicSource.clip;

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
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private void Start()
    {
        if (Instance == this) ConectarBotones();
    }

    private void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= AlCargarEscena;
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        ConectarBotones();
    }

    // Le pone sonido a todos los botones de la UI de las escenas cargadas (también a los de paneles ocultos)
    private void ConectarBotones()
    {
        botonesConSonido.RemoveWhere(b => b == null);
        foreach (Button boton in FindObjectsByType<Button>(FindObjectsInactive.Include))
        {
            if (!botonesConSonido.Add(boton)) continue;

            TMP_Text texto = boton.GetComponentInChildren<TMP_Text>(true);
            bool esAceptar = texto != null && EsBotonAceptar(texto.text);
            boton.onClick.AddListener(() => PlaySFX(esAceptar ? acept : uiButton));
        }
    }

    private bool EsBotonAceptar(string texto)
    {
        string normalizado = Normalizar(texto);
        foreach (string t in textosBotonesAceptar)
            if (Normalizar(t) == normalizado) return true;
        return false;
    }

    // Minúsculas y sin tildes, para que "Menú" y "Menù" o "Último" y "ultimo" coincidan
    private static string Normalizar(string texto)
    {
        StringBuilder sb = new StringBuilder();
        foreach (char c in texto.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Trim().ToLowerInvariant();
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

    // Reproduce un tema una sola vez (ej. victoria o derrota) y al terminar vuelve, con fundido, a "despues"
    public void PlayMusicUnaVez(AudioClip clip, AudioClip despues)
    {
        if (clip == null) return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(MusicaUnaVez(clip, despues));
    }

    private IEnumerator MusicaUnaVez(AudioClip clip, AudioClip despues)
    {
        yield return FadeMusic(clip, false);

        while (musicSource.isPlaying)
            yield return null;

        if (despues != null)
            yield return FadeMusic(despues);
    }

    private IEnumerator FadeMusic(AudioClip newClip, bool enLoop = true)
    {
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

        musicSource.clip = newClip;
        musicSource.loop = enLoop;
        musicSource.Play();

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

    // Reproduce un efecto solo si hay un AudioManager en juego
    // (no lo hay si se da Play directo en una escena que no es el menú).
    // Uso: AudioManager.ReproducirSFX(a => a.pickUp);
    public static void ReproducirSFX(System.Func<AudioManager, AudioClip> elegir)
    {
        if (Instance != null) Instance.PlaySFX(elegir(Instance));
    }
}