using UnityEngine;

public class SoundFXManager : MonoBehaviour
{
    public static SoundFXManager instance;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip[] hitTableClips;
    [SerializeField] private AudioClip[] paddleClips;
    [SerializeField] private AudioClip[] missedClips;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public void PlaySoundFXClip(AudioClip audioClip, Transform spawnTransform, float volume = 1.0f)
    {
        if (audioClip == null)
        {
            Debug.LogWarning("[SoundFXManager] AudioClip is null!");
            return;
        }
        if (spawnTransform == null)
        {
            Debug.LogWarning("[SoundFXManager] Spawn transform is null!");
            return;
        }

        // Create a temporary GameObject with AudioSource
        GameObject soundObject = new GameObject("TempAudioSource");
        soundObject.transform.position = spawnTransform.position;
        AudioSource audioSource = soundObject.AddComponent<AudioSource>();
        
        audioSource.clip = audioClip;
        audioSource.volume = volume;
        audioSource.Play();

        float clipLength = audioClip.length;
        Destroy(soundObject, clipLength);
    }

    public void PlayRandomSoundFXClip(AudioClip[] audioClips, Transform spawnTransform, float volume = 1.0f)
    {
        if (audioClips == null || audioClips.Length == 0)
        {
            Debug.LogWarning($"[SoundFXManager] No clips provided for random selection. Array is null: {audioClips == null}, Length: {(audioClips != null ? audioClips.Length : 0)}");
            return;
        }

        AudioClip randomClip = audioClips[Random.Range(0, audioClips.Length)];
        PlaySoundFXClip(randomClip, spawnTransform, volume);
    }
}