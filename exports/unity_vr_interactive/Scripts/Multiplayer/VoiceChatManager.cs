using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

namespace GalleryVR.Multiplayer
{
    /// <summary>
    /// Spatial voice chat manager for the social VR gallery.
    /// Handles microphone input, spatial audio output, and voice activity detection.
    /// Supports integration with Photon Voice, Vivox, or custom WebRTC solution.
    /// </summary>
    public class VoiceChatManager : MonoBehaviour
    {
        public static VoiceChatManager Instance { get; private set; }

        [Header("Voice Settings")]
        [SerializeField] private bool voiceEnabled = true;
        [SerializeField] private bool pushToTalk = false;
        [SerializeField] private float voiceActivityThreshold = 0.02f;
        [SerializeField] private float silenceTimeout = 0.5f;
        [SerializeField] private float maxVoiceRange = 15f;
        [SerializeField] private float minVoiceRange = 1f;

        [Header("Spatial Audio")]
        [SerializeField] private bool spatialAudioEnabled = true;
        [SerializeField] private AnimationCurve spatialFalloff;
        [SerializeField] private float dopplerLevel = 0f;
        [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Custom;

        [Header("Microphone")]
        [SerializeField] private int sampleRate = 44100;
        [SerializeField] private int recordingLength = 1;
        [SerializeField] private string preferredMicrophone = "";

        [Header("UI")]
        [SerializeField] private Canvas voiceSettingsCanvas;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Slider micSensitivitySlider;
        [SerializeField] private Toggle muteToggle;
        [SerializeField] private Toggle pushToTalkToggle;
        [SerializeField] private Image micIndicator;
        [SerializeField] private TMP_Dropdown microphoneDropdown;

        [Header("Indicator Colors")]
        [SerializeField] private Color mutedColor = Color.red;
        [SerializeField] private Color idleColor = Color.gray;
        [SerializeField] private Color speakingColor = Color.green;
        [SerializeField] private Color receivingColor = Color.cyan;

        // Microphone state
        private AudioClip microphoneClip;
        private string currentMicrophone;
        private bool isMicrophoneActive;
        private bool isSpeaking;
        private float lastVoiceTime;
        private float[] sampleBuffer;
        private int lastSamplePosition;

        // Voice state
        private bool isMuted = false;
        private float masterVolume = 1f;
        private float micSensitivity = 1f;

        // Player voice sources
        private Dictionary<string, PlayerVoice> playerVoices = new Dictionary<string, PlayerVoice>();

        // Events
        public event Action<bool> OnMicrophoneStateChanged;
        public event Action<bool> OnSpeakingStateChanged;
        public event Action<string, bool> OnPlayerSpeakingChanged;

        private class PlayerVoice
        {
            public AudioSource source;
            public bool isSpeaking;
            public float lastVoiceTime;
            public CircularAudioBuffer buffer;
        }

        private class CircularAudioBuffer
        {
            private float[] buffer;
            private int writePosition;
            private int readPosition;

            public CircularAudioBuffer(int size)
            {
                buffer = new float[size];
                writePosition = 0;
                readPosition = 0;
            }

            public void Write(float[] data)
            {
                foreach (var sample in data)
                {
                    buffer[writePosition] = sample;
                    writePosition = (writePosition + 1) % buffer.Length;
                }
            }

            public float[] Read(int count)
            {
                var result = new float[count];
                for (int i = 0; i < count; i++)
                {
                    result[i] = buffer[readPosition];
                    readPosition = (readPosition + 1) % buffer.Length;
                }
                return result;
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializeSpatialFalloff();
        }

        private void Start()
        {
            LoadSettings();
            SetupUI();
            PopulateMicrophoneList();

            if (voiceEnabled && !pushToTalk)
            {
                StartMicrophone();
            }
        }

        private void InitializeSpatialFalloff()
        {
            if (spatialFalloff == null || spatialFalloff.length == 0)
            {
                spatialFalloff = new AnimationCurve(
                    new Keyframe(0, 1),
                    new Keyframe(0.2f, 0.8f),
                    new Keyframe(0.5f, 0.4f),
                    new Keyframe(1, 0)
                );
            }
        }

        private void LoadSettings()
        {
            voiceEnabled = PlayerPrefs.GetInt("Voice_Enabled", 1) == 1;
            pushToTalk = PlayerPrefs.GetInt("Voice_PushToTalk", 0) == 1;
            isMuted = PlayerPrefs.GetInt("Voice_Muted", 0) == 1;
            masterVolume = PlayerPrefs.GetFloat("Voice_Volume", 1f);
            micSensitivity = PlayerPrefs.GetFloat("Voice_Sensitivity", 1f);
            preferredMicrophone = PlayerPrefs.GetString("Voice_Microphone", "");
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetInt("Voice_Enabled", voiceEnabled ? 1 : 0);
            PlayerPrefs.SetInt("Voice_PushToTalk", pushToTalk ? 1 : 0);
            PlayerPrefs.SetInt("Voice_Muted", isMuted ? 1 : 0);
            PlayerPrefs.SetFloat("Voice_Volume", masterVolume);
            PlayerPrefs.SetFloat("Voice_Sensitivity", micSensitivity);
            PlayerPrefs.SetString("Voice_Microphone", currentMicrophone ?? "");
            PlayerPrefs.Save();
        }

        private void SetupUI()
        {
            if (voiceSettingsCanvas == null)
            {
                CreateSettingsUI();
            }

            // Setup UI listeners
            if (volumeSlider != null)
            {
                volumeSlider.value = masterVolume;
                volumeSlider.onValueChanged.AddListener(SetVolume);
            }

            if (micSensitivitySlider != null)
            {
                micSensitivitySlider.value = micSensitivity;
                micSensitivitySlider.onValueChanged.AddListener(SetMicSensitivity);
            }

            if (muteToggle != null)
            {
                muteToggle.isOn = isMuted;
                muteToggle.onValueChanged.AddListener(SetMuted);
            }

            if (pushToTalkToggle != null)
            {
                pushToTalkToggle.isOn = pushToTalk;
                pushToTalkToggle.onValueChanged.AddListener(SetPushToTalk);
            }

            if (microphoneDropdown != null)
            {
                microphoneDropdown.onValueChanged.AddListener(OnMicrophoneSelected);
            }

            UpdateMicIndicator();

            // Hide settings initially
            if (voiceSettingsCanvas != null)
            {
                voiceSettingsCanvas.gameObject.SetActive(false);
            }
        }

        private void CreateSettingsUI()
        {
            // Create minimal settings canvas
            var canvasObj = new GameObject("VoiceSettingsCanvas");
            canvasObj.transform.SetParent(transform);

            voiceSettingsCanvas = canvasObj.AddComponent<Canvas>();
            voiceSettingsCanvas.renderMode = RenderMode.WorldSpace;

            var rectTransform = voiceSettingsCanvas.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(400, 300);
            rectTransform.localScale = Vector3.one * 0.002f;

            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // Background
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform);

            var bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            // Title
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(canvasObj.transform);

            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.85f);
            titleRect.anchorMax = new Vector2(0.9f, 0.95f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            var titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "VOICE SETTINGS";
            titleText.fontSize = 28;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;

            // Mic indicator
            var indicatorObj = new GameObject("MicIndicator");
            indicatorObj.transform.SetParent(canvasObj.transform);

            var indicatorRect = indicatorObj.AddComponent<RectTransform>();
            indicatorRect.anchorMin = new Vector2(0.85f, 0.85f);
            indicatorRect.anchorMax = new Vector2(0.95f, 0.95f);
            indicatorRect.offsetMin = Vector2.zero;
            indicatorRect.offsetMax = Vector2.zero;

            micIndicator = indicatorObj.AddComponent<Image>();
            micIndicator.color = idleColor;
        }

        private void PopulateMicrophoneList()
        {
            if (microphoneDropdown == null) return;

            microphoneDropdown.ClearOptions();

            var options = new List<TMP_Dropdown.OptionData>();
            string[] devices = Microphone.devices;

            foreach (string device in devices)
            {
                options.Add(new TMP_Dropdown.OptionData(device));
            }

            if (options.Count == 0)
            {
                options.Add(new TMP_Dropdown.OptionData("No microphone found"));
            }

            microphoneDropdown.AddOptions(options);

            // Select preferred or first microphone
            int selectedIndex = 0;
            if (!string.IsNullOrEmpty(preferredMicrophone))
            {
                for (int i = 0; i < devices.Length; i++)
                {
                    if (devices[i] == preferredMicrophone)
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }

            microphoneDropdown.value = selectedIndex;
            if (devices.Length > 0)
            {
                currentMicrophone = devices[selectedIndex];
            }
        }

        private void Update()
        {
            // Handle push-to-talk
            if (pushToTalk && voiceEnabled)
            {
                bool pttPressed = CheckPushToTalkInput();

                if (pttPressed && !isMicrophoneActive)
                {
                    StartMicrophone();
                }
                else if (!pttPressed && isMicrophoneActive)
                {
                    StopMicrophone();
                }
            }

            // Process microphone input
            if (isMicrophoneActive && !isMuted)
            {
                ProcessMicrophoneInput();
            }

            // Update speaking state
            UpdateSpeakingState();

            // Update indicator
            UpdateMicIndicator();

            // Update player voice sources
            UpdatePlayerVoices();
        }

        private bool CheckPushToTalkInput()
        {
            // Check VR controller grip
            bool leftGrip = false;
            bool rightGrip = false;

            if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand)
                .TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool leftGripValue))
            {
                leftGrip = leftGripValue;
            }

            if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand)
                .TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool rightGripValue))
            {
                rightGrip = rightGripValue;
            }

            // Also check keyboard for testing
            return leftGrip || rightGrip || Input.GetKey(KeyCode.V);
        }

        private void ProcessMicrophoneInput()
        {
            if (microphoneClip == null) return;

            int currentPosition = Microphone.GetPosition(currentMicrophone);
            if (currentPosition < 0 || currentPosition == lastSamplePosition) return;

            // Get samples
            int samplesToRead;
            if (currentPosition > lastSamplePosition)
            {
                samplesToRead = currentPosition - lastSamplePosition;
            }
            else
            {
                samplesToRead = (microphoneClip.samples - lastSamplePosition) + currentPosition;
            }

            if (sampleBuffer == null || sampleBuffer.Length < samplesToRead)
            {
                sampleBuffer = new float[samplesToRead];
            }

            microphoneClip.GetData(sampleBuffer, lastSamplePosition);
            lastSamplePosition = currentPosition;

            // Calculate voice level
            float maxLevel = 0;
            for (int i = 0; i < samplesToRead; i++)
            {
                float abs = Mathf.Abs(sampleBuffer[i]);
                if (abs > maxLevel) maxLevel = abs;
            }

            // Apply sensitivity
            maxLevel *= micSensitivity;

            // Voice activity detection
            if (maxLevel > voiceActivityThreshold)
            {
                lastVoiceTime = Time.time;

                if (!isSpeaking)
                {
                    isSpeaking = true;
                    OnSpeakingStateChanged?.Invoke(true);
                    BroadcastSpeakingState(true);
                }

                // Send voice data to network
                SendVoiceData(sampleBuffer, samplesToRead);
            }
        }

        private void UpdateSpeakingState()
        {
            if (isSpeaking && Time.time - lastVoiceTime > silenceTimeout)
            {
                isSpeaking = false;
                OnSpeakingStateChanged?.Invoke(false);
                BroadcastSpeakingState(false);
            }
        }

        private void SendVoiceData(float[] samples, int count)
        {
            // In real implementation, compress and send via network
            // For now, just broadcast to local player indicator
            if (NetworkManager.Instance?.LocalPlayer != null)
            {
                NetworkManager.Instance.LocalPlayer.SetVoiceActive(true);
            }
        }

        private void BroadcastSpeakingState(bool speaking)
        {
            if (NetworkManager.Instance?.LocalPlayer != null)
            {
                NetworkManager.Instance.LocalPlayer.SetVoiceActive(speaking);
            }

            Debug.Log($"[VoiceChat] Speaking: {speaking}");
        }

        private void UpdateMicIndicator()
        {
            if (micIndicator == null) return;

            Color targetColor;

            if (isMuted || !voiceEnabled)
            {
                targetColor = mutedColor;
            }
            else if (isSpeaking)
            {
                targetColor = speakingColor;
            }
            else
            {
                targetColor = idleColor;
            }

            micIndicator.color = Color.Lerp(micIndicator.color, targetColor, Time.deltaTime * 10f);
        }

        private void UpdatePlayerVoices()
        {
            // Update spatial audio for all player voice sources
            foreach (var kvp in playerVoices)
            {
                var voice = kvp.Value;
                if (voice.source == null) continue;

                // Update volume based on distance
                if (spatialAudioEnabled && Camera.main != null)
                {
                    float distance = Vector3.Distance(voice.source.transform.position, Camera.main.transform.position);
                    float normalizedDistance = Mathf.Clamp01((distance - minVoiceRange) / (maxVoiceRange - minVoiceRange));
                    float volumeMultiplier = spatialFalloff.Evaluate(normalizedDistance);

                    voice.source.volume = masterVolume * volumeMultiplier;
                }
            }
        }

        // Microphone control
        public void StartMicrophone()
        {
            if (isMicrophoneActive) return;

            string[] devices = Microphone.devices;
            if (devices.Length == 0)
            {
                Debug.LogWarning("[VoiceChat] No microphone found");
                return;
            }

            if (string.IsNullOrEmpty(currentMicrophone) || !Array.Exists(devices, d => d == currentMicrophone))
            {
                currentMicrophone = devices[0];
            }

            microphoneClip = Microphone.Start(currentMicrophone, true, recordingLength, sampleRate);

            if (microphoneClip != null)
            {
                isMicrophoneActive = true;
                lastSamplePosition = 0;
                OnMicrophoneStateChanged?.Invoke(true);
                Debug.Log($"[VoiceChat] Microphone started: {currentMicrophone}");
            }
        }

        public void StopMicrophone()
        {
            if (!isMicrophoneActive) return;

            Microphone.End(currentMicrophone);
            microphoneClip = null;
            isMicrophoneActive = false;

            if (isSpeaking)
            {
                isSpeaking = false;
                OnSpeakingStateChanged?.Invoke(false);
                BroadcastSpeakingState(false);
            }

            OnMicrophoneStateChanged?.Invoke(false);
            Debug.Log("[VoiceChat] Microphone stopped");
        }

        // Settings
        public void SetVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            SaveSettings();
        }

        public void SetMicSensitivity(float sensitivity)
        {
            micSensitivity = Mathf.Clamp(sensitivity, 0.1f, 3f);
            SaveSettings();
        }

        public void SetMuted(bool muted)
        {
            isMuted = muted;

            if (isMuted && isSpeaking)
            {
                isSpeaking = false;
                OnSpeakingStateChanged?.Invoke(false);
                BroadcastSpeakingState(false);
            }

            SaveSettings();
        }

        public void SetPushToTalk(bool ptt)
        {
            pushToTalk = ptt;

            if (!pushToTalk && voiceEnabled && !isMicrophoneActive)
            {
                StartMicrophone();
            }
            else if (pushToTalk && isMicrophoneActive && !CheckPushToTalkInput())
            {
                StopMicrophone();
            }

            SaveSettings();
        }

        public void SetVoiceEnabled(bool enabled)
        {
            voiceEnabled = enabled;

            if (voiceEnabled && !pushToTalk)
            {
                StartMicrophone();
            }
            else if (!voiceEnabled)
            {
                StopMicrophone();
            }

            SaveSettings();
        }

        private void OnMicrophoneSelected(int index)
        {
            string[] devices = Microphone.devices;
            if (index >= 0 && index < devices.Length)
            {
                bool wasActive = isMicrophoneActive;

                if (wasActive)
                {
                    StopMicrophone();
                }

                currentMicrophone = devices[index];
                preferredMicrophone = currentMicrophone;
                SaveSettings();

                if (wasActive)
                {
                    StartMicrophone();
                }
            }
        }

        // Remote player voice
        public void CreatePlayerVoiceSource(string playerId, Transform playerTransform)
        {
            if (playerVoices.ContainsKey(playerId)) return;

            var sourceObj = new GameObject($"VoiceSource_{playerId}");
            sourceObj.transform.SetParent(playerTransform);
            sourceObj.transform.localPosition = new Vector3(0, 1.7f, 0); // Head height

            var source = sourceObj.AddComponent<AudioSource>();
            source.spatialize = true;
            source.spatialBlend = 1f; // Full 3D
            source.dopplerLevel = dopplerLevel;
            source.rolloffMode = rolloffMode;
            source.minDistance = minVoiceRange;
            source.maxDistance = maxVoiceRange;
            source.volume = masterVolume;

            var voice = new PlayerVoice
            {
                source = source,
                isSpeaking = false,
                buffer = new CircularAudioBuffer(sampleRate * 2) // 2 second buffer
            };

            playerVoices[playerId] = voice;
        }

        public void RemovePlayerVoiceSource(string playerId)
        {
            if (playerVoices.TryGetValue(playerId, out var voice))
            {
                if (voice.source != null)
                {
                    Destroy(voice.source.gameObject);
                }
                playerVoices.Remove(playerId);
            }
        }

        public void ReceiveVoiceData(string playerId, float[] samples)
        {
            if (!playerVoices.TryGetValue(playerId, out var voice)) return;

            voice.buffer.Write(samples);
            voice.isSpeaking = true;
            voice.lastVoiceTime = Time.time;

            OnPlayerSpeakingChanged?.Invoke(playerId, true);

            // Update network player indicator
            if (NetworkManager.Instance?.Players.TryGetValue(playerId, out var player) == true)
            {
                player.SetVoiceActive(true);
            }
        }

        // UI
        public void ShowSettings()
        {
            if (voiceSettingsCanvas != null)
            {
                voiceSettingsCanvas.gameObject.SetActive(true);

                // Position in front of player
                if (Camera.main != null)
                {
                    Vector3 forward = Camera.main.transform.forward;
                    forward.y = 0;
                    forward.Normalize();

                    voiceSettingsCanvas.transform.position = Camera.main.transform.position + forward * 1.5f;
                    voiceSettingsCanvas.transform.position = new Vector3(
                        voiceSettingsCanvas.transform.position.x,
                        1.5f,
                        voiceSettingsCanvas.transform.position.z
                    );
                    voiceSettingsCanvas.transform.LookAt(Camera.main.transform);
                    voiceSettingsCanvas.transform.Rotate(0, 180, 0);
                }
            }
        }

        public void HideSettings()
        {
            if (voiceSettingsCanvas != null)
            {
                voiceSettingsCanvas.gameObject.SetActive(false);
            }
        }

        public void ToggleSettings()
        {
            if (voiceSettingsCanvas != null)
            {
                if (voiceSettingsCanvas.gameObject.activeSelf)
                    HideSettings();
                else
                    ShowSettings();
            }
        }

        // Getters
        public bool IsVoiceEnabled => voiceEnabled;
        public bool IsMuted => isMuted;
        public bool IsSpeaking => isSpeaking;
        public bool IsPushToTalk => pushToTalk;
        public float Volume => masterVolume;

        private void OnDestroy()
        {
            StopMicrophone();
        }
    }
}
