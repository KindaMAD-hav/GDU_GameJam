using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Global SoundManager that can play SFX or music.
/// Place this on a persistent GameObject (e.g., 'SoundManager').
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [System.Serializable]
    public class SoundClip
    {
        public string soundName;  // e.g. "Dash"
        public AudioClip clip;    // Assign an AudioClip here
    }

    [Header("Audio Sources")]
    public AudioSource sfxSource;   // For playing one-shot SFX
    public AudioSource musicSource; // (Optional) For looping music

    [Header("Sound Library")]
    public List<SoundClip> soundClips = new List<SoundClip>();

    // Internally store sounds by name for quick lookup
    private Dictionary<string, AudioClip> clipDictionary = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Populate our dictionary with (soundName -> AudioClip)
            foreach (var sound in soundClips)
            {
                if (!clipDictionary.ContainsKey(sound.soundName))
                {
                    clipDictionary.Add(sound.soundName, sound.clip);
                }
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Play a one-shot SFX by name (must exist in soundClips list).
    /// </summary>
    public void PlaySFX(string soundName)
    {
        if (clipDictionary.TryGetValue(soundName, out AudioClip clip))
        {
            sfxSource.PlayOneShot(clip);
        }
        else
        {
            Debug.LogWarning($"SoundManager: No SFX named '{soundName}' found!");
        }
    }

    /// <summary>
    /// Play (or loop) music by name (must exist in soundClips list).
    /// </summary>
    public void PlayMusic(string soundName)
    {
        if (clipDictionary.TryGetValue(soundName, out AudioClip clip))
        {
            musicSource.clip = clip;
            musicSource.Play();
        }
        else
        {
            Debug.LogWarning($"SoundManager: No music named '{soundName}' found!");
        }
    }
}
