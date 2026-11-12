
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeafAudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource ambienceSource;

    [Header("Music Playlist")]
    [SerializeField] private AudioClip[] musicTracks;

    [Header("Ambience")]
    [SerializeField] private AudioClip ambienceTrack;

    [Header("Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.5f;

    [Range(0f, 1f)]
    [SerializeField] private float ambienceVolume = 0.35f;

    [Header("Music Transitions")]
    [Min(0f)]
    [SerializeField] private float fadeDuration = 1.5f;

    [Header("Random Delay Between Songs")]
    [Min(0f)]
    [SerializeField] private float minDelayBetweenSongs = 30f;

    [Min(0f)]
    [SerializeField] private float maxDelayBetweenSongs = 90f;

    private readonly List<int> shuffleBag = new List<int>();

    private int shuffleIndex;
    private int lastPlayedIndex = -1;

    private Coroutine musicRoutine;

    private void Awake()
    {
        if (musicSource == null ||
            ambienceSource == null ||
            musicSource == ambienceSource)
        {
            Debug.LogError(
                "LeafAudioManager: Assign two different AudioSources."
            );

            enabled = false;
            return;
        }

        musicSource.playOnAwake = false;
        musicSource.loop = false;
        musicSource.spatialBlend = 0f;

        ambienceSource.playOnAwake = false;
        ambienceSource.loop = true;
        ambienceSource.spatialBlend = 0f;
    }

    private void Start()
    {
        StartAmbience();

        if (musicTracks != null && musicTracks.Length > 0)
            musicRoutine = StartCoroutine(MusicPlaylist());
    }

    private void StartAmbience()
    {
        if (ambienceTrack == null)
            return;

        ambienceSource.clip = ambienceTrack;
        ambienceSource.volume = ambienceVolume;
        ambienceSource.loop = true;
        ambienceSource.Play();
    }

    private IEnumerator MusicPlaylist()
    {
        BuildShuffleBag();

        while (true)
        {
            if (shuffleBag.Count == 0)
                yield break;

            if (shuffleIndex >= shuffleBag.Count)
                BuildShuffleBag();

            int index = shuffleBag[shuffleIndex++];
            AudioClip track = musicTracks[index];

            if (track == null)
                continue;

            lastPlayedIndex = index;

            musicSource.clip = track;
            musicSource.volume = 0f;
            musicSource.Play();

            // Fade in.
            yield return FadeVolume(
                musicSource,
                0f,
                musicVolume,
                fadeDuration
            );

            // Play until near the end.
            float remaining = Mathf.Max(
                0f,
                track.length - fadeDuration
            );

            yield return new WaitForSecondsRealtime(remaining);

            // Fade out.
            yield return FadeVolume(
                musicSource,
                musicSource.volume,
                0f,
                fadeDuration
            );

            musicSource.Stop();

            // Randomly choose how long to wait before
            // playing the next song.
            float delay = Random.Range(
                Mathf.Min(minDelayBetweenSongs, maxDelayBetweenSongs),
                Mathf.Max(minDelayBetweenSongs, maxDelayBetweenSongs)
            );

            if (delay > 0f)
            {
                yield return new WaitForSecondsRealtime(delay);
            }
        }
    }

    private IEnumerator FadeVolume(
        AudioSource source,
        float from,
        float to,
        float duration)
    {
        if (duration <= 0f)
        {
            source.volume = to;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            source.volume = Mathf.Lerp(from, to, t);

            yield return null;
        }

        source.volume = to;
    }

    private void BuildShuffleBag()
    {
        shuffleBag.Clear();

        if (musicTracks == null)
            return;

        for (int i = 0; i < musicTracks.Length; i++)
        {
            if (musicTracks[i] != null)
                shuffleBag.Add(i);
        }

        // Fisher-Yates shuffle.
        for (int i = shuffleBag.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);

            int temp = shuffleBag[i];
            shuffleBag[i] = shuffleBag[j];
            shuffleBag[j] = temp;
        }

        // Avoid repeating the same track across
        // consecutive shuffle cycles.
        if (shuffleBag.Count > 1 &&
            shuffleBag[0] == lastPlayedIndex)
        {
            int swapIndex = Random.Range(
                1,
                shuffleBag.Count
            );

            int temp = shuffleBag[0];
            shuffleBag[0] = shuffleBag[swapIndex];
            shuffleBag[swapIndex] = temp;
        }

        shuffleIndex = 0;
    }

    private void OnDisable()
    {
        if (musicRoutine != null)
        {
            StopCoroutine(musicRoutine);
            musicRoutine = null;
        }

        if (musicSource != null)
            musicSource.Stop();

        if (ambienceSource != null)
            ambienceSource.Stop();
    }
}