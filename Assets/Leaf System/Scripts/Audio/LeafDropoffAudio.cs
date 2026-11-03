
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class LeafDropoffAudio : MonoBehaviour
{
    [Header("Dropoff Sound Effects")]
    public AudioClip[] dropoffSounds;

    [Header("Audio Settings")]
    [Range(0f, 1f)]
    public float volume = 0.8f;

    [Range(0.5f, 1.5f)]
    public float minPitch = 0.95f;

    [Range(0.5f, 1.5f)]
    public float maxPitch = 1.05f;

    [Header("Variation")]
    public bool preventConsecutiveRepeats = true;

    private AudioSource audioSource;
    private int lastPlayedIndex = -1;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;

        // 2D audio: consistent volume regardless
        // of distance from the wheelbarrow.
        audioSource.spatialBlend = 0f;
    }

    public void PlayDropoffSound()
    {
        if (dropoffSounds == null ||
            dropoffSounds.Length == 0)
            return;

        int index = Random.Range(
            0,
            dropoffSounds.Length
        );

        // Avoid selecting the same clip twice in a row.
        if (preventConsecutiveRepeats &&
            dropoffSounds.Length > 1 &&
            index == lastPlayedIndex)
        {
            index = (
                index +
                Random.Range(1, dropoffSounds.Length)
            ) % dropoffSounds.Length;
        }

        lastPlayedIndex = index;

        AudioClip clip = dropoffSounds[index];

        if (clip == null)
            return;

        audioSource.pitch = Random.Range(
            Mathf.Min(minPitch, maxPitch),
            Mathf.Max(minPitch, maxPitch)
        );

        audioSource.PlayOneShot(clip, volume);
    }
}
