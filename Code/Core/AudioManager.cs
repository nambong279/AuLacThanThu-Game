using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace AuLacThanThu.Core
{
    /// <summary>
    /// Quản lý âm thanh trong game, bao gồm nhạc nền và hiệu ứng âm thanh
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        #region Singleton
        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<AudioManager>();
                    
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("AudioManager");
                        _instance = obj.AddComponent<AudioManager>();
                    }
                }
                
                return _instance;
            }
        }
        #endregion
        
        #region Properties
        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource uiSfxSource;
        [SerializeField] private AudioSource ambientSource;
        
        [Header("Audio Mixers")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private string masterVolumeParam = "MasterVolume";
        [SerializeField] private string musicVolumeParam = "MusicVolume";
        [SerializeField] private string sfxVolumeParam = "SFXVolume";
        [SerializeField] private string uiVolumeParam = "UIVolume";
        [SerializeField] private string ambientVolumeParam = "AmbientVolume";
        
        [Header("Audio Clips")]
        [SerializeField] private List<AudioClip> musicTracks = new List<AudioClip>();
        [SerializeField] private List<AudioClip> ambientTracks = new List<AudioClip>();
        [SerializeField] private List<AudioClip> uiSounds = new List<AudioClip>();
        [SerializeField] private List<AudioClip> combatSounds = new List<AudioClip>();
        
        [Header("Settings")]
        [SerializeField] private bool playMusicOnStart = true;
        [SerializeField] private bool loopMusic = true;
        [SerializeField] private float crossFadeDuration = 1.5f;
        [SerializeField] private float minVolume = 0.0001f; // -80dB
        
        // Runtime state
        private Dictionary<string, AudioClip> audioClipDict = new Dictionary<string, AudioClip>();
        private Dictionary<SoundType, List<AudioClip>> soundTypeDict = new Dictionary<SoundType, List<AudioClip>>();
        
        private AudioSource currentMusicSource;
        private AudioSource nextMusicSource;
        
        private float musicVolume = 1f;
        private float sfxVolume = 1f;
        private float uiVolume = 1f;
        private float ambientVolume = 1f;
        private float masterVolume = 1f;
        
        private Coroutine fadeCoroutine;
        
        // Temporary audio sources for one-shot sounds
        private List<AudioSource> tempAudioSources = new List<AudioSource>();
        #endregion
        
        #region Events
        public delegate void VolumeChangedHandler(float newVolume);
        public event VolumeChangedHandler OnMasterVolumeChanged;
        public event VolumeChangedHandler OnMusicVolumeChanged;
        public event VolumeChangedHandler OnSFXVolumeChanged;
        #endregion
        
        #region Unity Lifecycle
        private void Awake()
        {
            // Ensure singleton
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Create audio sources if not set
            if (musicSource == null)
            {
                GameObject musicObj = new GameObject("MusicSource");
                musicObj.transform.parent = transform;
                musicSource = musicObj.AddComponent<AudioSource>();
                musicSource.loop = loopMusic;
                musicSource.playOnAwake = false;
                
                // Create a second music source for crossfading
                GameObject musicObj2 = new GameObject("MusicSource2");
                musicObj2.transform.parent = transform;
                nextMusicSource = musicObj2.AddComponent<AudioSource>();
                nextMusicSource.loop = loopMusic;
                nextMusicSource.playOnAwake = false;
                nextMusicSource.volume = 0f;
                
                currentMusicSource = musicSource;
            }
            
            if (sfxSource == null)
            {
                GameObject sfxObj = new GameObject("SFXSource");
                sfxObj.transform.parent = transform;
                sfxSource = sfxObj.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }
            
            if (uiSfxSource == null)
            {
                GameObject uiObj = new GameObject("UISFXSource");
                uiObj.transform.parent = transform;
                uiSfxSource = uiObj.AddComponent<AudioSource>();
                uiSfxSource.loop = false;
                uiSfxSource.playOnAwake = false;
            }
            
            if (ambientSource == null)
            {
                GameObject ambientObj = new GameObject("AmbientSource");
                ambientObj.transform.parent = transform;
                ambientSource = ambientObj.AddComponent<AudioSource>();
                ambientSource.loop = true;
                ambientSource.playOnAwake = false;
            }
            
            // Set up dictionaries
            BuildAudioDictionaries();
        }
        
        private void Start()
        {
            // Load saved volumes
            LoadVolumes();
            
            // Start playing music if enabled
            if (playMusicOnStart && musicTracks.Count > 0)
            {
                PlayMusic(musicTracks[0].name);
            }
        }
        
        private void LoadVolumes()
        {
            // Load volume settings from PlayerPrefs
            masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
            sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            uiVolume = PlayerPrefs.GetFloat("UIVolume", 1f);
            ambientVolume = PlayerPrefs.GetFloat("AmbientVolume", 1f);
            
            // Apply loaded volumes
            ApplyVolumeSettings();
        }
        
        private void ApplyVolumeSettings()
        {
            // Apply to mixer if available
            if (audioMixer != null)
            {
                // Convert to decibels (using log scale)
                // 0 = -80dB (silence), 1 = 0dB (full volume)
                float masterDb = masterVolume <= minVolume ? -80f : Mathf.Log10(masterVolume) * 20f;
                float musicDb = musicVolume <= minVolume ? -80f : Mathf.Log10(musicVolume) * 20f;
                float sfxDb = sfxVolume <= minVolume ? -80f : Mathf.Log10(sfxVolume) * 20f;
                float uiDb = uiVolume <= minVolume ? -80f : Mathf.Log10(uiVolume) * 20f;
                float ambientDb = ambientVolume <= minVolume ? -80f : Mathf.Log10(ambientVolume) * 20f;
                
                audioMixer.SetFloat(masterVolumeParam, masterDb);
                audioMixer.SetFloat(musicVolumeParam, musicDb);
                audioMixer.SetFloat(sfxVolumeParam, sfxDb);
                audioMixer.SetFloat(uiVolumeParam, uiDb);
                audioMixer.SetFloat(ambientVolumeParam, ambientDb);
            }
            else
            {
                // Apply directly to sources if no mixer
                musicSource.volume = musicVolume * masterVolume;
                nextMusicSource.volume = 0f; // This is controlled during crossfade
                sfxSource.volume = sfxVolume * masterVolume;
                uiSfxSource.volume = uiVolume * masterVolume;
                ambientSource.volume = ambientVolume * masterVolume;
            }
        }
        
        private void BuildAudioDictionaries()
        {
            // Clear dictionaries
            audioClipDict.Clear();
            soundTypeDict.Clear();
            
            // Initialize sound type lists
            soundTypeDict[SoundType.Music] = new List<AudioClip>();
            soundTypeDict[SoundType.SFX] = new List<AudioClip>();
            soundTypeDict[SoundType.UI] = new List<AudioClip>();
            soundTypeDict[SoundType.Ambient] = new List<AudioClip>();
            soundTypeDict[SoundType.Combat] = new List<AudioClip>();
            
            // Add music tracks
            foreach (AudioClip clip in musicTracks)
            {
                if (clip != null)
                {
                    audioClipDict[clip.name] = clip;
                    soundTypeDict[SoundType.Music].Add(clip);
                }
            }
            
            // Add ambient tracks
            foreach (AudioClip clip in ambientTracks)
            {
                if (clip != null)
                {
                    audioClipDict[clip.name] = clip;
                    soundTypeDict[SoundType.Ambient].Add(clip);
                }
            }
            
            // Add UI sounds
            foreach (AudioClip clip in uiSounds)
            {
                if (clip != null)
                {
                    audioClipDict[clip.name] = clip;
                    soundTypeDict[SoundType.UI].Add(clip);
                }
            }
            
            // Add combat sounds
            foreach (AudioClip clip in combatSounds)
            {
                if (clip != null)
                {
                    audioClipDict[clip.name] = clip;
                    soundTypeDict[SoundType.Combat].Add(clip);
                    soundTypeDict[SoundType.SFX].Add(clip); // Combat sounds are also SFX
                }
            }
        }
        
        private void OnDestroy()
        {
            // Save volume settings
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            PlayerPrefs.SetFloat("UIVolume", uiVolume);
            PlayerPrefs.SetFloat("AmbientVolume", ambientVolume);
            PlayerPrefs.Save();
        }
        #endregion
        
        #region Music Control
        public void PlayMusic(string trackName, bool crossFade = true)
        {
            if (!audioClipDict.ContainsKey(trackName))
            {
                Debug.LogWarning($"Music track '{trackName}' not found!");
                return;
            }
            
            AudioClip clip = audioClipDict[trackName];
            
            // Check if it's already playing
            if (currentMusicSource.clip != null && currentMusicSource.clip.name == trackName && currentMusicSource.isPlaying)
            {
                return;
            }
            
            // Stop any current crossfade
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            
            if (crossFade && currentMusicSource.isPlaying)
            {
                // Swap sources for crossfade
                AudioSource temp = currentMusicSource;
                currentMusicSource = nextMusicSource;
                nextMusicSource = temp;
                
                // Set up next track
                currentMusicSource.clip = clip;
                currentMusicSource.volume = 0f;
                currentMusicSource.Play();
                
                // Start crossfade
                fadeCoroutine = StartCoroutine(CrossFadeMusic());
            }
            else
            {
                // Simple play without crossfade
                currentMusicSource.Stop();
                currentMusicSource.clip = clip;
                currentMusicSource.volume = musicVolume * masterVolume;
                currentMusicSource.Play();
                
                // Reset next source
                nextMusicSource.Stop();
                nextMusicSource.volume = 0f;
            }
        }
        
        public void PlayMusicByType(SoundType type = SoundType.Music, bool crossFade = true)
        {
            if (!soundTypeDict.ContainsKey(type) || soundTypeDict[type].Count == 0)
            {
                Debug.LogWarning($"No tracks found for type {type}");
                return;
            }
            
            // Pick a random track from the category
            int index = Random.Range(0, soundTypeDict[type].Count);
            AudioClip clip = soundTypeDict[type][index];
            
            PlayMusic(clip.name, crossFade);
        }
        
        public void StopMusic(bool fadeOut = true)
        {
            // Stop any current crossfade
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }
            
            if (fadeOut)
            {
                // Fade out both sources
                fadeCoroutine = StartCoroutine(FadeOutMusic());
            }
            else
            {
                // Immediately stop
                currentMusicSource.Stop();
                nextMusicSource.Stop();
            }
        }
        
        private IEnumerator CrossFadeMusic()
        {
            float timer = 0f;
            float startVolume = nextMusicSource.volume;
            float targetVolume = musicVolume * masterVolume;
            
            while (timer < crossFadeDuration)
            {
                timer += Time.deltaTime;
                float t = timer / crossFadeDuration;
                
                // Fade in current, fade out next
                currentMusicSource.volume = Mathf.Lerp(0f, targetVolume, t);
                nextMusicSource.volume = Mathf.Lerp(startVolume, 0f, t);
                
                yield return null;
            }
            
            // Ensure final values
            currentMusicSource.volume = targetVolume;
            nextMusicSource.volume = 0f;
            nextMusicSource.Stop();
            
            fadeCoroutine = null;
        }
        
        private IEnumerator FadeOutMusic()
        {
            float timer = 0f;
            float currentStartVolume = currentMusicSource.volume;
            float nextStartVolume = nextMusicSource.volume;
            
            while (timer < crossFadeDuration)
            {
                timer += Time.deltaTime;
                float t = timer / crossFadeDuration;
                
                // Fade out both sources
                currentMusicSource.volume = Mathf.Lerp(currentStartVolume, 0f, t);
                nextMusicSource.volume = Mathf.Lerp(nextStartVolume, 0f, t);
                
                yield return null;
            }
            
            // Ensure final values and stop
            currentMusicSource.volume = 0f;
            nextMusicSource.volume = 0f;
            currentMusicSource.Stop();
            nextMusicSource.Stop();
            
            fadeCoroutine = null;
        }
        #endregion
        
        #region SFX Control
        public void PlaySFX(string sfxName, float volumeScale = 1f)
        {
            if (!audioClipDict.ContainsKey(sfxName))
            {
                Debug.LogWarning($"SFX '{sfxName}' not found!");
                return;
            }
            
            AudioClip clip = audioClipDict[sfxName];
            
            // Play through SFX source
            sfxSource.PlayOneShot(clip, sfxVolume * masterVolume * volumeScale);
        }
        
        public void PlaySFXByType(SoundType type, float volumeScale = 1f)
        {
            if (!soundTypeDict.ContainsKey(type) || soundTypeDict[type].Count == 0)
            {
                Debug.LogWarning($"No sounds found for type {type}");
                return;
            }
            
            // Pick a random sound from the category
            int index = Random.Range(0, soundTypeDict[type].Count);
            AudioClip clip = soundTypeDict[type][index];
            
            // Use appropriate source based on type
            switch (type)
            {
                case SoundType.UI:
                    uiSfxSource.PlayOneShot(clip, uiVolume * masterVolume * volumeScale);
                    break;
                    
                case SoundType.Combat:
                case SoundType.SFX:
                default:
                    sfxSource.PlayOneShot(clip, sfxVolume * masterVolume * volumeScale);
                    break;
            }
        }
        
        public void PlaySFX3D(string sfxName, Vector3 position, float volumeScale = 1f, float minDistance = 1f, float maxDistance = 20f)
        {
            if (!audioClipDict.ContainsKey(sfxName))
            {
                Debug.LogWarning($"SFX '{sfxName}' not found!");
                return;
            }
            
            AudioClip clip = audioClipDict[sfxName];
            
            // Create temporary audio source at position
            GameObject tempGO = new GameObject("TempAudio");
            tempGO.transform.position = position;
            
            AudioSource tempSource = tempGO.AddComponent<AudioSource>();
            tempSource.clip = clip;
            tempSource.volume = sfxVolume * masterVolume * volumeScale;
            tempSource.spatialBlend = 1.0f; // 3D sound
            tempSource.minDistance = minDistance;
            tempSource.maxDistance = maxDistance;
            tempSource.rolloffMode = AudioRolloffMode.Linear;
            tempSource.loop = false;
            tempSource.Play();
            
            // Add to temp list for cleanup
            tempAudioSources.Add(tempSource);
            
            // Destroy after clip is done
            Destroy(tempGO, clip.length + 0.1f);
            
            // Schedule cleanup of the list
            StartCoroutine(CleanupTempAudioSources());
        }
        
        private IEnumerator CleanupTempAudioSources()
        {
            yield return new WaitForSeconds(0.5f);
            
            // Remove any null references (destroyed objects)
            tempAudioSources.RemoveAll(source => source == null);
        }
        
        public void PlayUISound(string soundName)
        {
            if (!audioClipDict.ContainsKey(soundName))
            {
                Debug.LogWarning($"UI sound '{soundName}' not found!");
                return;
            }
            
            AudioClip clip = audioClipDict[soundName];
            
            // Play through UI source
            uiSfxSource.PlayOneShot(clip, uiVolume * masterVolume);
        }
        
        public void PlayAmbientSound(string ambientName, bool loop = true)
        {
            if (!audioClipDict.ContainsKey(ambientName))
            {
                Debug.LogWarning($"Ambient sound '{ambientName}' not found!");
                return;
            }
            
            // Stop current ambient sound
            ambientSource.Stop();
            
            // Set new clip and play
            AudioClip clip = audioClipDict[ambientName];
            ambientSource.clip = clip;
            ambientSource.loop = loop;
            ambientSource.volume = ambientVolume * masterVolume;
            ambientSource.Play();
        }
        #endregion
        
        #region Volume Control
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            
            // Apply volume changes
            ApplyVolumeSettings();
            
            // Save to PlayerPrefs
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
            PlayerPrefs.Save();
            
            // Trigger event
            OnMasterVolumeChanged?.Invoke(masterVolume);
        }
        
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            
            // Apply volume changes
            ApplyVolumeSettings();
            
            // Save to PlayerPrefs
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
            PlayerPrefs.Save();
            
            // Trigger event
            OnMusicVolumeChanged?.Invoke(musicVolume);
        }
        
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            uiVolume = volume; // UI volume follows SFX for simplicity
            
            // Apply volume changes
            ApplyVolumeSettings();
            
            // Save to PlayerPrefs
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            PlayerPrefs.SetFloat("UIVolume", uiVolume);
            PlayerPrefs.Save();
            
            // Trigger event
            OnSFXVolumeChanged?.Invoke(sfxVolume);
        }
        
        public float GetMasterVolume() => masterVolume;
        public float GetMusicVolume() => musicVolume;
        public float GetSFXVolume() => sfxVolume;
        #endregion
        
        #region Audio Management
        public void AddAudioClip(AudioClip clip, SoundType type)
        {
            if (clip == null) return;
            
            // Add to dictionaries
            audioClipDict[clip.name] = clip;
            
            if (!soundTypeDict.ContainsKey(type))
            {
                soundTypeDict[type] = new List<AudioClip>();
            }
            
            soundTypeDict[type].Add(clip);
        }
        
        public AudioClip GetAudioClip(string clipName)
        {
            if (audioClipDict.ContainsKey(clipName))
            {
                return audioClipDict[clipName];
            }
            return null;
        }
        
        public List<AudioClip> GetAudioClipsByType(SoundType type)
        {
            if (soundTypeDict.ContainsKey(type))
            {
                return soundTypeDict[type];
            }
            return new List<AudioClip>();
        }
        #endregion
    }
    
    public enum SoundType
    {
        Music,
        SFX,
        UI,
        Ambient,
        Combat
    }
}