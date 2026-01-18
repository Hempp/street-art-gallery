using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

namespace GalleryVR
{
    [System.Serializable]
    public class AmbientZone
    {
        public string zoneName;
        public Collider zoneCollider;
        public AudioClip ambientClip;
        [Range(0f, 1f)] public float volume = 0.5f;
        public bool loop = true;
    }

    [System.Serializable]
    public class SoundEffect
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.5f, 1.5f)] public float pitchVariation = 0.1f;
    }

    public class GalleryAudioManager : MonoBehaviour
    {
        public static GalleryAudioManager Instance { get; private set; }

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer masterMixer;
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup ambientGroup;

        [Header("Global Ambient")]
        [SerializeField] private AudioClip globalAmbientClip;
        [SerializeField] private float globalAmbientVolume = 0.3f;

        [Header("Background Music")]
        [SerializeField] private AudioClip[] musicTracks;
        [SerializeField] private float musicVolume = 0.2f;
        [SerializeField] private bool shuffleMusic = true;
        [SerializeField] private float musicCrossfadeDuration = 3f;

        [Header("Ambient Zones")]
        [SerializeField] private List<AmbientZone> ambientZones = new List<AmbientZone>();

        [Header("Sound Effects")]
        [SerializeField] private List<SoundEffect> soundEffects = new List<SoundEffect>();

        [Header("Footstep Audio")]
        [SerializeField] private AudioClip[] footstepClips;
        [SerializeField] private float footstepVolume = 0.3f;
        [SerializeField] private float footstepInterval = 0.5f;

        [Header("Spatial Audio Settings")]
        [SerializeField] private float defaultMinDistance = 1f;
        [SerializeField] private float defaultMaxDistance = 15f;
        [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

        // Audio Sources
        private AudioSource globalAmbientSource;
        private AudioSource musicSourceA;
        private AudioSource musicSourceB;
        private AudioSource sfxSource;
        private Dictionary<string, AudioSource> zoneSources = new Dictionary<string, AudioSource>();

        // State
        private int currentMusicIndex = -1;
        private bool isMusicSourceA = true;
        private Coroutine musicCrossfadeCoroutine;
        private AmbientZone currentZone;
        private float lastFootstepTime;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudio();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeAudio()
        {
            // Create global ambient source
            globalAmbientSource = CreateAudioSource("GlobalAmbient", true, globalAmbientVolume);
            globalAmbientSource.outputAudioMixerGroup = ambientGroup;

            // Create music sources for crossfade
            musicSourceA = CreateAudioSource("MusicA", true, musicVolume);
            musicSourceA.outputAudioMixerGroup = musicGroup;

            musicSourceB = CreateAudioSource("MusicB", true, 0);
            musicSourceB.outputAudioMixerGroup = musicGroup;

            // Create SFX source
            sfxSource = CreateAudioSource("SFX", false, 1f);
            sfxSource.outputAudioMixerGroup = sfxGroup;

            // Create zone sources
            foreach (var zone in ambientZones)
            {
                var source = CreateAudioSource($"Zone_{zone.zoneName}", true, 0);
                source.outputAudioMixerGroup = ambientGroup;
                source.clip = zone.ambientClip;
                source.Play();
                zoneSources[zone.zoneName] = source;
            }
        }

        private AudioSource CreateAudioSource(string name, bool loop, float volume)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);

            var source = go.AddComponent<AudioSource>();
            source.loop = loop;
            source.volume = volume;
            source.playOnAwake = false;
            source.spatialBlend = 0; // 2D by default

            return source;
        }

        private void Start()
        {
            // Start global ambient
            if (globalAmbientClip != null)
            {
                globalAmbientSource.clip = globalAmbientClip;
                globalAmbientSource.Play();
            }

            // Start music
            if (musicTracks.Length > 0)
            {
                PlayNextMusic();
            }

            // Subscribe to player teleport
            if (VRPlayerController.Instance != null)
            {
                VRPlayerController.Instance.OnTeleport += OnPlayerTeleport;
            }
        }

        private void Update()
        {
            UpdateZoneAudio();
            CheckMusicEnd();
        }

        private void UpdateZoneAudio()
        {
            if (Camera.main == null) return;

            Vector3 playerPos = Camera.main.transform.position;
            AmbientZone nearestZone = null;
            float nearestDistance = float.MaxValue;

            foreach (var zone in ambientZones)
            {
                if (zone.zoneCollider == null) continue;

                Vector3 closestPoint = zone.zoneCollider.ClosestPoint(playerPos);
                float distance = Vector3.Distance(playerPos, closestPoint);

                // Check if inside collider
                if (zone.zoneCollider.bounds.Contains(playerPos))
                {
                    nearestZone = zone;
                    break;
                }

                // Track nearest for falloff
                if (distance < nearestDistance && distance < defaultMaxDistance)
                {
                    nearestDistance = distance;
                    nearestZone = zone;
                }
            }

            // Update zone volumes
            foreach (var zone in ambientZones)
            {
                if (!zoneSources.TryGetValue(zone.zoneName, out var source)) continue;

                float targetVolume = 0;

                if (zone == nearestZone)
                {
                    if (zone.zoneCollider.bounds.Contains(playerPos))
                    {
                        targetVolume = zone.volume;
                    }
                    else
                    {
                        // Distance falloff
                        float falloff = 1f - (nearestDistance / defaultMaxDistance);
                        targetVolume = zone.volume * falloff;
                    }
                }

                source.volume = Mathf.Lerp(source.volume, targetVolume, Time.deltaTime * 2f);
            }

            currentZone = nearestZone;
        }

        private void CheckMusicEnd()
        {
            var currentSource = isMusicSourceA ? musicSourceA : musicSourceB;

            if (currentSource.clip != null && !currentSource.isPlaying && currentSource.time == 0)
            {
                PlayNextMusic();
            }
        }

        // Public API

        public void PlayNextMusic()
        {
            if (musicTracks.Length == 0) return;

            // Select next track
            if (shuffleMusic)
            {
                int newIndex;
                do
                {
                    newIndex = Random.Range(0, musicTracks.Length);
                } while (newIndex == currentMusicIndex && musicTracks.Length > 1);
                currentMusicIndex = newIndex;
            }
            else
            {
                currentMusicIndex = (currentMusicIndex + 1) % musicTracks.Length;
            }

            var newClip = musicTracks[currentMusicIndex];
            CrossfadeToMusic(newClip);
        }

        public void CrossfadeToMusic(AudioClip clip)
        {
            if (musicCrossfadeCoroutine != null)
            {
                StopCoroutine(musicCrossfadeCoroutine);
            }

            musicCrossfadeCoroutine = StartCoroutine(CrossfadeMusicCoroutine(clip));
        }

        private IEnumerator CrossfadeMusicCoroutine(AudioClip newClip)
        {
            var fadeOutSource = isMusicSourceA ? musicSourceA : musicSourceB;
            var fadeInSource = isMusicSourceA ? musicSourceB : musicSourceA;

            fadeInSource.clip = newClip;
            fadeInSource.volume = 0;
            fadeInSource.Play();

            float elapsed = 0;
            float startVolume = fadeOutSource.volume;

            while (elapsed < musicCrossfadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / musicCrossfadeDuration;

                fadeOutSource.volume = Mathf.Lerp(startVolume, 0, t);
                fadeInSource.volume = Mathf.Lerp(0, musicVolume, t);

                yield return null;
            }

            fadeOutSource.Stop();
            isMusicSourceA = !isMusicSourceA;
        }

        public void PlaySFX(string soundName, Vector3? position = null)
        {
            var sfx = soundEffects.Find(s => s.name == soundName);
            if (sfx == null || sfx.clip == null) return;

            if (position.HasValue)
            {
                // Play as 3D sound
                AudioSource.PlayClipAtPoint(sfx.clip, position.Value, sfx.volume);
            }
            else
            {
                // Play as 2D sound
                float pitch = 1f + Random.Range(-sfx.pitchVariation, sfx.pitchVariation);
                sfxSource.pitch = pitch;
                sfxSource.PlayOneShot(sfx.clip, sfx.volume);
            }
        }

        public void PlaySFX(AudioClip clip, float volume = 1f, Vector3? position = null)
        {
            if (clip == null) return;

            if (position.HasValue)
            {
                AudioSource.PlayClipAtPoint(clip, position.Value, volume);
            }
            else
            {
                sfxSource.PlayOneShot(clip, volume);
            }
        }

        public void PlayFootstep(Vector3 position)
        {
            if (footstepClips.Length == 0) return;
            if (Time.time - lastFootstepTime < footstepInterval) return;

            var clip = footstepClips[Random.Range(0, footstepClips.Length)];
            AudioSource.PlayClipAtPoint(clip, position, footstepVolume);
            lastFootstepTime = Time.time;
        }

        public AudioSource CreateSpatialSource(Vector3 position, AudioClip clip, bool loop = false)
        {
            var go = new GameObject("SpatialAudio");
            go.transform.position = position;

            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = loop;
            source.spatialBlend = 1f; // Full 3D
            source.minDistance = defaultMinDistance;
            source.maxDistance = defaultMaxDistance;
            source.rolloffMode = rolloffMode;
            source.outputAudioMixerGroup = sfxGroup;

            if (!loop)
            {
                Destroy(go, clip.length + 0.1f);
            }

            return source;
        }

        public void SetMasterVolume(float volume)
        {
            if (masterMixer != null)
            {
                masterMixer.SetFloat("MasterVolume", Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f);
            }
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = volume;
            if (isMusicSourceA)
                musicSourceA.volume = volume;
            else
                musicSourceB.volume = volume;
        }

        public void SetAmbientVolume(float volume)
        {
            globalAmbientVolume = volume;
            globalAmbientSource.volume = volume;
        }

        public void PauseAll()
        {
            globalAmbientSource.Pause();
            musicSourceA.Pause();
            musicSourceB.Pause();

            foreach (var source in zoneSources.Values)
            {
                source.Pause();
            }
        }

        public void ResumeAll()
        {
            globalAmbientSource.UnPause();
            musicSourceA.UnPause();
            musicSourceB.UnPause();

            foreach (var source in zoneSources.Values)
            {
                source.UnPause();
            }
        }

        private void OnPlayerTeleport(Vector3 position)
        {
            // Play teleport whoosh sound
            PlaySFX("teleport", position);
        }

        public string GetCurrentZoneName()
        {
            return currentZone?.zoneName ?? "None";
        }
    }
}
