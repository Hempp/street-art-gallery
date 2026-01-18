using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace GalleryVR
{
    /// <summary>
    /// Main manager for the VR Street Art Gallery experience.
    /// Coordinates all subsystems and handles initialization.
    /// </summary>
    public class GalleryManager : MonoBehaviour
    {
        public static GalleryManager Instance { get; private set; }

        [Header("Scene References")]
        [SerializeField] private Transform galleryRoot;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private GameObject xrOriginPrefab;

        [Header("System Prefabs")]
        [SerializeField] private GameObject playerControllerPrefab;
        [SerializeField] private GameObject audioManagerPrefab;
        [SerializeField] private GameObject comfortSettingsPrefab;
        [SerializeField] private GameObject tourManagerPrefab;

        [Header("UI")]
        [SerializeField] private Canvas mainMenuCanvas;
        [SerializeField] private Canvas pauseMenuCanvas;
        [SerializeField] private Canvas loadingCanvas;

        [Header("Loading")]
        [SerializeField] private float minimumLoadTime = 2f;

        [Header("Debug")]
        [SerializeField] private bool autoStartExperience = true;
        [SerializeField] private bool enableDebugMode = false;

        // State
        private bool isInitialized;
        private bool isPaused;

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
        }

        private void Start()
        {
            StartCoroutine(InitializeGallery());
        }

        private IEnumerator InitializeGallery()
        {
            // Show loading screen
            ShowLoading(true);

            float startTime = Time.time;

            // Initialize subsystems
            yield return StartCoroutine(InitializeSubsystems());

            // Setup artwork hotspots
            yield return StartCoroutine(SetupArtworkHotspots());

            // Ensure minimum load time for smooth transition
            float elapsed = Time.time - startTime;
            if (elapsed < minimumLoadTime)
            {
                yield return new WaitForSeconds(minimumLoadTime - elapsed);
            }

            isInitialized = true;

            // Hide loading screen
            ShowLoading(false);

            // Show main menu or auto-start
            if (autoStartExperience)
            {
                StartExperience();
            }
            else
            {
                ShowMainMenu(true);
            }

            Debug.Log("Gallery initialized successfully");
        }

        private IEnumerator InitializeSubsystems()
        {
            // Spawn player if not present
            if (VRPlayerController.Instance == null && playerControllerPrefab != null)
            {
                var player = Instantiate(playerControllerPrefab, spawnPoint.position, spawnPoint.rotation);
                yield return null;
            }

            // Initialize audio manager
            if (GalleryAudioManager.Instance == null && audioManagerPrefab != null)
            {
                Instantiate(audioManagerPrefab);
                yield return null;
            }

            // Initialize comfort settings
            if (VRComfortSettings.Instance == null && comfortSettingsPrefab != null)
            {
                Instantiate(comfortSettingsPrefab);
                yield return null;
            }

            // Initialize tour manager
            if (GalleryTourManager.Instance == null && tourManagerPrefab != null)
            {
                Instantiate(tourManagerPrefab);
                yield return null;
            }

            // Load artwork database
            if (ArtworkData.Instance == null)
            {
                var dataObj = new GameObject("ArtworkData");
                dataObj.AddComponent<ArtworkData>();
                yield return null;
            }
        }

        private IEnumerator SetupArtworkHotspots()
        {
            // Find all artwork objects and add hotspots if needed
            var artworkObjects = GameObject.FindGameObjectsWithTag("Artwork");

            if (artworkObjects.Length == 0)
            {
                // Try to find by naming convention
                for (int i = 1; i <= 20; i++)
                {
                    var artObj = GameObject.Find($"Art_{i}");
                    if (artObj != null)
                    {
                        SetupHotspot(artObj, i);
                    }
                    yield return null;
                }
            }
            else
            {
                int id = 1;
                foreach (var artObj in artworkObjects)
                {
                    SetupHotspot(artObj, id++);
                    yield return null;
                }
            }
        }

        private void SetupHotspot(GameObject artworkObject, int artworkId)
        {
            // Add hotspot if not present
            var hotspot = artworkObject.GetComponent<ArtworkHotspot>();
            if (hotspot == null)
            {
                hotspot = artworkObject.AddComponent<ArtworkHotspot>();
            }

            hotspot.SetArtworkId(artworkId);

            // Add collider if not present
            if (artworkObject.GetComponent<Collider>() == null)
            {
                var boxCollider = artworkObject.AddComponent<BoxCollider>();
                // Adjust collider size based on renderer bounds
                var renderer = artworkObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    boxCollider.size = renderer.bounds.size;
                    boxCollider.center = renderer.bounds.center - artworkObject.transform.position;
                }
            }
        }

        // Public API

        public void StartExperience()
        {
            ShowMainMenu(false);

            // Position player at spawn point
            if (VRPlayerController.Instance != null && spawnPoint != null)
            {
                VRPlayerController.Instance.TeleportTo(spawnPoint.position);
            }

            // Start ambient audio
            if (GalleryAudioManager.Instance != null)
            {
                GalleryAudioManager.Instance.ResumeAll();
            }
        }

        public void StartGuidedTour()
        {
            ShowMainMenu(false);

            if (GalleryTourManager.Instance != null)
            {
                GalleryTourManager.Instance.StartTour();
            }
        }

        public void PauseExperience()
        {
            isPaused = true;

            // Show pause menu
            if (pauseMenuCanvas != null)
            {
                pauseMenuCanvas.gameObject.SetActive(true);
            }

            // Pause audio
            if (GalleryAudioManager.Instance != null)
            {
                GalleryAudioManager.Instance.PauseAll();
            }

            // Pause tour
            if (GalleryTourManager.Instance != null && GalleryTourManager.Instance.IsTourActive())
            {
                GalleryTourManager.Instance.PauseTour();
            }

            Time.timeScale = 0;
        }

        public void ResumeExperience()
        {
            isPaused = false;
            Time.timeScale = 1;

            // Hide pause menu
            if (pauseMenuCanvas != null)
            {
                pauseMenuCanvas.gameObject.SetActive(false);
            }

            // Resume audio
            if (GalleryAudioManager.Instance != null)
            {
                GalleryAudioManager.Instance.ResumeAll();
            }

            // Resume tour
            if (GalleryTourManager.Instance != null && GalleryTourManager.Instance.IsTourActive())
            {
                GalleryTourManager.Instance.ResumeTour();
            }
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1;
            isPaused = false;

            // End tour if active
            if (GalleryTourManager.Instance != null && GalleryTourManager.Instance.IsTourActive())
            {
                GalleryTourManager.Instance.EndTour();
            }

            // Hide pause menu
            if (pauseMenuCanvas != null)
            {
                pauseMenuCanvas.gameObject.SetActive(false);
            }

            ShowMainMenu(true);
        }

        public void QuitApplication()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        public void OpenSettings()
        {
            if (VRComfortSettings.Instance != null)
            {
                VRComfortSettings.Instance.ShowSettings();
            }
        }

        // UI helpers
        private void ShowMainMenu(bool show)
        {
            if (mainMenuCanvas != null)
            {
                mainMenuCanvas.gameObject.SetActive(show);
            }
        }

        private void ShowLoading(bool show)
        {
            if (loadingCanvas != null)
            {
                loadingCanvas.gameObject.SetActive(show);
            }
        }

        // Input handling (for menu button on controller)
        private void Update()
        {
            if (!isInitialized) return;

            // Check for menu button press
            if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand)
                .TryGetFeatureValue(UnityEngine.XR.CommonUsages.menuButton, out bool menuPressed))
            {
                if (menuPressed)
                {
                    if (isPaused)
                        ResumeExperience();
                    else
                        PauseExperience();
                }
            }

            // Debug shortcuts
            if (enableDebugMode)
            {
                HandleDebugInput();
            }
        }

        private void HandleDebugInput()
        {
            // T - Start tour
            if (Input.GetKeyDown(KeyCode.T))
            {
                StartGuidedTour();
            }

            // P - Toggle pause
            if (Input.GetKeyDown(KeyCode.P))
            {
                if (isPaused) ResumeExperience();
                else PauseExperience();
            }

            // M - Toggle main menu
            if (Input.GetKeyDown(KeyCode.M))
            {
                ShowMainMenu(!mainMenuCanvas.gameObject.activeSelf);
            }

            // S - Toggle settings
            if (Input.GetKeyDown(KeyCode.S))
            {
                VRComfortSettings.Instance?.ToggleSettings();
            }
        }

        // Getters
        public bool IsInitialized => isInitialized;
        public bool IsPaused => isPaused;
        public Transform SpawnPoint => spawnPoint;
    }
}
