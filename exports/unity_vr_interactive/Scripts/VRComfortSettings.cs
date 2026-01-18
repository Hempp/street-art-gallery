using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;

namespace GalleryVR
{
    [Serializable]
    public class ComfortPreset
    {
        public string name;
        public bool useSnapTurn;
        public float snapTurnAngle;
        public bool useSmoothLocomotion;
        public float smoothMoveSpeed;
        public bool enableVignette;
        public float vignetteIntensity;
        public bool enableTeleport;
    }

    public class VRComfortSettings : MonoBehaviour
    {
        public static VRComfortSettings Instance { get; private set; }

        [Header("Locomotion")]
        [SerializeField] private bool useSnapTurn = true;
        [SerializeField] private float snapTurnAngle = 45f;
        [SerializeField] private bool useSmoothLocomotion = false;
        [SerializeField] private float smoothMoveSpeed = 2f;
        [SerializeField] private bool enableTeleport = true;

        [Header("Comfort Vignette")]
        [SerializeField] private bool enableVignette = true;
        [SerializeField] [Range(0f, 1f)] private float vignetteIntensity = 0.4f;
        [SerializeField] private float vignetteSmoothing = 0.2f;
        [SerializeField] private Color vignetteColor = Color.black;

        [Header("References")]
        [SerializeField] private Volume postProcessVolume;
        [SerializeField] private Material vignetteMaterial;
        [SerializeField] private Canvas settingsCanvas;

        [Header("UI Elements")]
        [SerializeField] private Toggle snapTurnToggle;
        [SerializeField] private Slider snapTurnAngleSlider;
        [SerializeField] private Toggle smoothLocomotionToggle;
        [SerializeField] private Slider smoothSpeedSlider;
        [SerializeField] private Toggle vignetteToggle;
        [SerializeField] private Slider vignetteIntensitySlider;
        [SerializeField] private Toggle teleportToggle;

        [Header("Presets")]
        [SerializeField] private ComfortPreset[] presets = new ComfortPreset[]
        {
            new ComfortPreset
            {
                name = "Maximum Comfort",
                useSnapTurn = true,
                snapTurnAngle = 30f,
                useSmoothLocomotion = false,
                smoothMoveSpeed = 1.5f,
                enableVignette = true,
                vignetteIntensity = 0.6f,
                enableTeleport = true
            },
            new ComfortPreset
            {
                name = "Balanced",
                useSnapTurn = true,
                snapTurnAngle = 45f,
                useSmoothLocomotion = false,
                smoothMoveSpeed = 2f,
                enableVignette = true,
                vignetteIntensity = 0.4f,
                enableTeleport = true
            },
            new ComfortPreset
            {
                name = "Immersive",
                useSnapTurn = false,
                snapTurnAngle = 45f,
                useSmoothLocomotion = true,
                smoothMoveSpeed = 3f,
                enableVignette = false,
                vignetteIntensity = 0.2f,
                enableTeleport = true
            }
        };

        // Runtime state
        private Vignette vignetteEffect;
        private float currentVignetteValue;
        private float targetVignetteValue;
        private bool isMoving;

        // Events
        public event Action<bool> OnSnapTurnChanged;
        public event Action<float> OnSnapTurnAngleChanged;
        public event Action<bool> OnSmoothLocomotionChanged;
        public event Action<bool> OnVignetteChanged;

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

            LoadSettings();
            SetupPostProcessing();
            SetupUI();
        }

        private void Start()
        {
            ApplySettings();

            // Hide settings canvas initially
            if (settingsCanvas != null)
            {
                settingsCanvas.gameObject.SetActive(false);
            }
        }

        private void SetupPostProcessing()
        {
            // Try to find or create post process volume
            if (postProcessVolume == null)
            {
                postProcessVolume = FindObjectOfType<Volume>();

                if (postProcessVolume == null)
                {
                    var volumeGO = new GameObject("ComfortPostProcess");
                    postProcessVolume = volumeGO.AddComponent<Volume>();
                    postProcessVolume.isGlobal = true;
                    postProcessVolume.priority = 100;
                    postProcessVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
                }
            }

            // Get or add vignette effect
            if (!postProcessVolume.profile.TryGet(out vignetteEffect))
            {
                vignetteEffect = postProcessVolume.profile.Add<Vignette>();
            }

            vignetteEffect.active = enableVignette;
            vignetteEffect.intensity.value = 0;
            vignetteEffect.smoothness.value = vignetteSmoothing;
            vignetteEffect.color.value = vignetteColor;
        }

        private void SetupUI()
        {
            // Snap turn toggle
            if (snapTurnToggle != null)
            {
                snapTurnToggle.isOn = useSnapTurn;
                snapTurnToggle.onValueChanged.AddListener(SetSnapTurn);
            }

            // Snap turn angle slider
            if (snapTurnAngleSlider != null)
            {
                snapTurnAngleSlider.value = snapTurnAngle;
                snapTurnAngleSlider.onValueChanged.AddListener(SetSnapTurnAngle);
            }

            // Smooth locomotion toggle
            if (smoothLocomotionToggle != null)
            {
                smoothLocomotionToggle.isOn = useSmoothLocomotion;
                smoothLocomotionToggle.onValueChanged.AddListener(SetSmoothLocomotion);
            }

            // Smooth speed slider
            if (smoothSpeedSlider != null)
            {
                smoothSpeedSlider.value = smoothMoveSpeed;
                smoothSpeedSlider.onValueChanged.AddListener(SetSmoothMoveSpeed);
            }

            // Vignette toggle
            if (vignetteToggle != null)
            {
                vignetteToggle.isOn = enableVignette;
                vignetteToggle.onValueChanged.AddListener(SetVignetteEnabled);
            }

            // Vignette intensity slider
            if (vignetteIntensitySlider != null)
            {
                vignetteIntensitySlider.value = vignetteIntensity;
                vignetteIntensitySlider.onValueChanged.AddListener(SetVignetteIntensity);
            }

            // Teleport toggle
            if (teleportToggle != null)
            {
                teleportToggle.isOn = enableTeleport;
                teleportToggle.onValueChanged.AddListener(SetTeleportEnabled);
            }
        }

        private void Update()
        {
            UpdateVignette();
        }

        private void UpdateVignette()
        {
            if (!enableVignette || vignetteEffect == null) return;

            // Determine target vignette based on movement
            targetVignetteValue = isMoving ? vignetteIntensity : 0f;

            // Smooth transition
            currentVignetteValue = Mathf.Lerp(
                currentVignetteValue,
                targetVignetteValue,
                Time.deltaTime * 10f
            );

            vignetteEffect.intensity.value = currentVignetteValue;
        }

        // Called by locomotion system
        public void SetMoving(bool moving)
        {
            isMoving = moving;
        }

        // Called during teleport
        public void TriggerTeleportVignette()
        {
            if (!enableVignette) return;
            StartCoroutine(TeleportVignetteCoroutine());
        }

        private System.Collections.IEnumerator TeleportVignetteCoroutine()
        {
            // Quick fade to black
            float elapsed = 0;
            float duration = 0.15f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                vignetteEffect.intensity.value = Mathf.Lerp(0, 0.8f, t);
                yield return null;
            }

            // Hold briefly
            yield return new WaitForSeconds(0.05f);

            // Fade back
            elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                vignetteEffect.intensity.value = Mathf.Lerp(0.8f, 0, t);
                yield return null;
            }

            vignetteEffect.intensity.value = 0;
        }

        // Settings setters
        public void SetSnapTurn(bool enabled)
        {
            useSnapTurn = enabled;
            OnSnapTurnChanged?.Invoke(enabled);

            if (VRPlayerController.Instance != null)
            {
                VRPlayerController.Instance.SetSnapTurn(enabled);
            }

            SaveSettings();
        }

        public void SetSnapTurnAngle(float angle)
        {
            snapTurnAngle = Mathf.Clamp(angle, 15f, 90f);
            OnSnapTurnAngleChanged?.Invoke(snapTurnAngle);
            SaveSettings();
        }

        public void SetSmoothLocomotion(bool enabled)
        {
            useSmoothLocomotion = enabled;
            OnSmoothLocomotionChanged?.Invoke(enabled);

            if (VRPlayerController.Instance != null)
            {
                VRPlayerController.Instance.SetLocomotionMode(enabled);
            }

            SaveSettings();
        }

        public void SetSmoothMoveSpeed(float speed)
        {
            smoothMoveSpeed = Mathf.Clamp(speed, 0.5f, 5f);
            SaveSettings();
        }

        public void SetVignetteEnabled(bool enabled)
        {
            enableVignette = enabled;
            OnVignetteChanged?.Invoke(enabled);

            if (vignetteEffect != null)
            {
                vignetteEffect.active = enabled;
            }

            if (VRPlayerController.Instance != null)
            {
                VRPlayerController.Instance.SetComfortVignette(enabled);
            }

            SaveSettings();
        }

        public void SetVignetteIntensity(float intensity)
        {
            vignetteIntensity = Mathf.Clamp01(intensity);
            SaveSettings();
        }

        public void SetTeleportEnabled(bool enabled)
        {
            enableTeleport = enabled;
            SaveSettings();
        }

        // Preset management
        public void ApplyPreset(int presetIndex)
        {
            if (presetIndex < 0 || presetIndex >= presets.Length) return;

            var preset = presets[presetIndex];

            SetSnapTurn(preset.useSnapTurn);
            SetSnapTurnAngle(preset.snapTurnAngle);
            SetSmoothLocomotion(preset.useSmoothLocomotion);
            SetSmoothMoveSpeed(preset.smoothMoveSpeed);
            SetVignetteEnabled(preset.enableVignette);
            SetVignetteIntensity(preset.vignetteIntensity);
            SetTeleportEnabled(preset.enableTeleport);

            // Update UI
            RefreshUI();
        }

        public void ApplyPreset(string presetName)
        {
            for (int i = 0; i < presets.Length; i++)
            {
                if (presets[i].name == presetName)
                {
                    ApplyPreset(i);
                    return;
                }
            }
        }

        private void RefreshUI()
        {
            if (snapTurnToggle != null) snapTurnToggle.isOn = useSnapTurn;
            if (snapTurnAngleSlider != null) snapTurnAngleSlider.value = snapTurnAngle;
            if (smoothLocomotionToggle != null) smoothLocomotionToggle.isOn = useSmoothLocomotion;
            if (smoothSpeedSlider != null) smoothSpeedSlider.value = smoothMoveSpeed;
            if (vignetteToggle != null) vignetteToggle.isOn = enableVignette;
            if (vignetteIntensitySlider != null) vignetteIntensitySlider.value = vignetteIntensity;
            if (teleportToggle != null) teleportToggle.isOn = enableTeleport;
        }

        // UI visibility
        public void ShowSettings()
        {
            if (settingsCanvas != null)
            {
                settingsCanvas.gameObject.SetActive(true);
            }
        }

        public void HideSettings()
        {
            if (settingsCanvas != null)
            {
                settingsCanvas.gameObject.SetActive(false);
            }
        }

        public void ToggleSettings()
        {
            if (settingsCanvas != null)
            {
                settingsCanvas.gameObject.SetActive(!settingsCanvas.gameObject.activeSelf);
            }
        }

        // Apply to player controller
        private void ApplySettings()
        {
            if (VRPlayerController.Instance != null)
            {
                VRPlayerController.Instance.SetSnapTurn(useSnapTurn);
                VRPlayerController.Instance.SetLocomotionMode(useSmoothLocomotion);
                VRPlayerController.Instance.SetComfortVignette(enableVignette);
            }
        }

        // Persistence
        private void SaveSettings()
        {
            PlayerPrefs.SetInt("VR_SnapTurn", useSnapTurn ? 1 : 0);
            PlayerPrefs.SetFloat("VR_SnapTurnAngle", snapTurnAngle);
            PlayerPrefs.SetInt("VR_SmoothLocomotion", useSmoothLocomotion ? 1 : 0);
            PlayerPrefs.SetFloat("VR_SmoothMoveSpeed", smoothMoveSpeed);
            PlayerPrefs.SetInt("VR_Vignette", enableVignette ? 1 : 0);
            PlayerPrefs.SetFloat("VR_VignetteIntensity", vignetteIntensity);
            PlayerPrefs.SetInt("VR_Teleport", enableTeleport ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void LoadSettings()
        {
            if (PlayerPrefs.HasKey("VR_SnapTurn"))
            {
                useSnapTurn = PlayerPrefs.GetInt("VR_SnapTurn") == 1;
                snapTurnAngle = PlayerPrefs.GetFloat("VR_SnapTurnAngle", 45f);
                useSmoothLocomotion = PlayerPrefs.GetInt("VR_SmoothLocomotion") == 1;
                smoothMoveSpeed = PlayerPrefs.GetFloat("VR_SmoothMoveSpeed", 2f);
                enableVignette = PlayerPrefs.GetInt("VR_Vignette") == 1;
                vignetteIntensity = PlayerPrefs.GetFloat("VR_VignetteIntensity", 0.4f);
                enableTeleport = PlayerPrefs.GetInt("VR_Teleport") == 1;
            }
        }

        // Getters
        public bool UseSnapTurn => useSnapTurn;
        public float SnapTurnAngle => snapTurnAngle;
        public bool UseSmoothLocomotion => useSmoothLocomotion;
        public float SmoothMoveSpeed => smoothMoveSpeed;
        public bool VignetteEnabled => enableVignette;
        public float VignetteIntensity => vignetteIntensity;
        public bool TeleportEnabled => enableTeleport;
        public ComfortPreset[] Presets => presets;
    }
}
