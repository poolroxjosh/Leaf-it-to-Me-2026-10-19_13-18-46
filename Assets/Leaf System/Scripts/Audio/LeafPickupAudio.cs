
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class LeafPickupAudio : MonoBehaviour
{
    [Header("Pickup Sound Effects")]
    public AudioClip[] pickupSounds;

    [Header("Audio Settings")]
    [Range(0f, 1f)]
    public float volume = 0.8f;

    [Range(0.5f, 1.5f)]
    public float minPitch = 0.95f;

    [Range(0.5f, 1.5f)]
    public float maxPitch = 1.05f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    public void PlayPickupSound()
    {
        if (pickupSounds == null ||
            pickupSounds.Length == 0)
            return;

        AudioClip clip = pickupSounds[
            Random.Range(0, pickupSounds.Length)
        ];

        if (clip == null)
            return;

        audioSource.pitch = Random.Range(
            minPitch,
            maxPitch
        );

        audioSource.PlayOneShot(clip, volume);
    }
}
