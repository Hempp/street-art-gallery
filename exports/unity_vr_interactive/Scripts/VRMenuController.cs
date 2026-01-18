using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GalleryVR
{
    /// <summary>
    /// Handles VR menu interactions and world-space UI positioning.
    /// </summary>
    public class VRMenuController : MonoBehaviour
    {
        [Header("Menu Canvases")]
        [SerializeField] private Canvas mainMenuCanvas;
        [SerializeField] private Canvas pauseMenuCanvas;
        [SerializeField] private Canvas settingsMenuCanvas;
        [SerializeField] private Canvas tourControlCanvas;

        [Header("Main Menu Buttons")]
        [SerializeField] private Button startExperienceButton;
        [SerializeField] private Button startTourButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Pause Menu Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseSettingsButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button pauseQuitButton;

        [Header("Tour Control Buttons")]
        [SerializeField] private Button tourPreviousButton;
        [SerializeField] private Button tourNextButton;
        [SerializeField] private Button tourPauseButton;
        [SerializeField] private Button tourExitButton;
        [SerializeField] private TextMeshProUGUI tourProgressText;

        [Header("Settings")]
        [SerializeField] private float menuDistance = 2f;
        [SerializeField] private float menuHeight = 1.5f;
        [SerializeField] private bool followPlayer = true;
        [SerializeField] private float followSpeed = 2f;

        [Header("Audio")]
        [SerializeField] private AudioClip buttonHoverSound;
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioSource uiAudioSource;

        private Transform playerCamera;
        private bool tourPaused;

        private void Start()
        {
            playerCamera = Camera.main?.transform;

            SetupButtonListeners();
            SetupAudio();

            // Initially position menus
            PositionMenu(mainMenuCanvas);
        }

        private void SetupButtonListeners()
        {
            // Main Menu
            if (startExperienceButton) startExperienceButton.onClick.AddListener(OnStartExperience);
            if (startTourButton) startTourButton.onClick.AddListener(OnStartTour);
            if (settingsButton) settingsButton.onClick.AddListener(OnOpenSettings);
            if (quitButton) quitButton.onClick.AddListener(OnQuit);

            // Pause Menu
            if (resumeButton) resumeButton.onClick.AddListener(OnResume);
            if (pauseSettingsButton) pauseSettingsButton.onClick.AddListener(OnOpenSettings);
            if (mainMenuButton) mainMenuButton.onClick.AddListener(OnReturnToMainMenu);
            if (pauseQuitButton) pauseQuitButton.onClick.AddListener(OnQuit);

            // Tour Controls
            if (tourPreviousButton) tourPreviousButton.onClick.AddListener(OnTourPrevious);
            if (tourNextButton) tourNextButton.onClick.AddListener(OnTourNext);
            if (tourPauseButton) tourPauseButton.onClick.AddListener(OnTourPauseToggle);
            if (tourExitButton) tourExitButton.onClick.AddListener(OnTourExit);

            // Add hover sounds to all buttons
            AddHoverSounds(mainMenuCanvas);
            AddHoverSounds(pauseMenuCanvas);
            AddHoverSounds(settingsMenuCanvas);
            AddHoverSounds(tourControlCanvas);
        }

        private void AddHoverSounds(Canvas canvas)
        {
            if (canvas == null) return;

            var buttons = canvas.GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                var trigger = button.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                if (trigger == null)
                {
                    trigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                }

                // Pointer enter
                var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry
                {
                    eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
                };
                enterEntry.callback.AddListener((data) => PlayHoverSound());
                trigger.triggers.Add(enterEntry);

                // Pointer click
                var clickEntry = new UnityEngine.EventSystems.EventTrigger.Entry
                {
                    eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick
                };
                clickEntry.callback.AddListener((data) => PlayClickSound());
                trigger.triggers.Add(clickEntry);
            }
        }

        private void SetupAudio()
        {
            if (uiAudioSource == null)
            {
                uiAudioSource = gameObject.AddComponent<AudioSource>();
                uiAudioSource.spatialBlend = 0; // 2D
                uiAudioSource.playOnAwake = false;
            }
        }

        private void Update()
        {
            // Update tour progress
            if (GalleryTourManager.Instance != null && GalleryTourManager.Instance.IsTourActive())
            {
                UpdateTourProgress();
            }

            // Follow player for visible menus
            if (followPlayer && playerCamera != null)
            {
                if (mainMenuCanvas != null && mainMenuCanvas.gameObject.activeSelf)
                    SmoothFollowPlayer(mainMenuCanvas);
                if (pauseMenuCanvas != null && pauseMenuCanvas.gameObject.activeSelf)
                    SmoothFollowPlayer(pauseMenuCanvas);
                if (settingsMenuCanvas != null && settingsMenuCanvas.gameObject.activeSelf)
                    SmoothFollowPlayer(settingsMenuCanvas);
            }
        }

        private void PositionMenu(Canvas canvas)
        {
            if (canvas == null || playerCamera == null) return;

            Vector3 forward = playerCamera.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 targetPos = playerCamera.position + forward * menuDistance;
            targetPos.y = menuHeight;

            canvas.transform.position = targetPos;
            canvas.transform.LookAt(playerCamera);
            canvas.transform.Rotate(0, 180, 0); // Face player
        }

        private void SmoothFollowPlayer(Canvas canvas)
        {
            if (canvas == null || playerCamera == null) return;

            Vector3 forward = playerCamera.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 targetPos = playerCamera.position + forward * menuDistance;
            targetPos.y = menuHeight;

            canvas.transform.position = Vector3.Lerp(
                canvas.transform.position,
                targetPos,
                Time.deltaTime * followSpeed
            );

            // Smoothly rotate to face player
            Quaternion targetRot = Quaternion.LookRotation(canvas.transform.position - playerCamera.position);
            canvas.transform.rotation = Quaternion.Slerp(
                canvas.transform.rotation,
                targetRot,
                Time.deltaTime * followSpeed
            );
        }

        private void UpdateTourProgress()
        {
            if (tourProgressText == null) return;

            var tour = GalleryTourManager.Instance;
            int current = tour.GetCurrentStopIndex() + 1;
            int total = tour.GetTotalStops();

            tourProgressText.text = $"{current} / {total}";

            // Update button states
            if (tourPreviousButton) tourPreviousButton.interactable = current > 1;
            if (tourNextButton) tourNextButton.interactable = current < total;

            // Update pause button text
            if (tourPauseButton)
            {
                var text = tourPauseButton.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = tour.IsPaused() ? "Resume" : "Pause";
                }
            }
        }

        // Button handlers
        private void OnStartExperience()
        {
            GalleryManager.Instance?.StartExperience();
        }

        private void OnStartTour()
        {
            GalleryManager.Instance?.StartGuidedTour();
            ShowTourControls(true);
        }

        private void OnOpenSettings()
        {
            VRComfortSettings.Instance?.ShowSettings();
        }

        private void OnQuit()
        {
            GalleryManager.Instance?.QuitApplication();
        }

        private void OnResume()
        {
            GalleryManager.Instance?.ResumeExperience();
        }

        private void OnReturnToMainMenu()
        {
            GalleryManager.Instance?.ReturnToMainMenu();
            ShowTourControls(false);
        }

        private void OnTourPrevious()
        {
            GalleryTourManager.Instance?.GoToPreviousStop();
        }

        private void OnTourNext()
        {
            GalleryTourManager.Instance?.GoToNextStop();
        }

        private void OnTourPauseToggle()
        {
            var tour = GalleryTourManager.Instance;
            if (tour == null) return;

            if (tour.IsPaused())
                tour.ResumeTour();
            else
                tour.PauseTour();
        }

        private void OnTourExit()
        {
            GalleryTourManager.Instance?.EndTour();
            ShowTourControls(false);
            GalleryManager.Instance?.ReturnToMainMenu();
        }

        // Public API
        public void ShowMainMenu(bool show)
        {
            if (mainMenuCanvas != null)
            {
                mainMenuCanvas.gameObject.SetActive(show);
                if (show) PositionMenu(mainMenuCanvas);
            }
        }

        public void ShowPauseMenu(bool show)
        {
            if (pauseMenuCanvas != null)
            {
                pauseMenuCanvas.gameObject.SetActive(show);
                if (show) PositionMenu(pauseMenuCanvas);
            }
        }

        public void ShowTourControls(bool show)
        {
            if (tourControlCanvas != null)
            {
                tourControlCanvas.gameObject.SetActive(show);
            }
        }

        // Audio
        private void PlayHoverSound()
        {
            if (buttonHoverSound != null && uiAudioSource != null)
            {
                uiAudioSource.PlayOneShot(buttonHoverSound, 0.5f);
            }

            // Haptic feedback
            var rightHand = FindObjectOfType<VRHandController>();
            if (rightHand != null)
            {
                rightHand.TriggerHaptic(0.1f, 0.02f);
            }
        }

        private void PlayClickSound()
        {
            if (buttonClickSound != null && uiAudioSource != null)
            {
                uiAudioSource.PlayOneShot(buttonClickSound, 0.8f);
            }

            // Stronger haptic feedback
            var rightHand = FindObjectOfType<VRHandController>();
            if (rightHand != null)
            {
                rightHand.TriggerHaptic(0.3f, 0.05f);
            }
        }
    }
}
