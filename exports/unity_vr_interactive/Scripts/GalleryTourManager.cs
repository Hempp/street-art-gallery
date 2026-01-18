using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace GalleryVR
{
    [System.Serializable]
    public class TourStop
    {
        public string stopName;
        public Transform viewpoint;
        public ArtworkHotspot artworkHotspot;
        public float viewDuration = 8f;
        public AudioClip narrationClip;
        [TextArea(3, 5)] public string narrationText;
        public bool autoAdvance = true;
    }

    public class GalleryTourManager : MonoBehaviour
    {
        public static GalleryTourManager Instance { get; private set; }

        [Header("Tour Configuration")]
        [SerializeField] private List<TourStop> tourStops = new List<TourStop>();
        [SerializeField] private float transitionDuration = 2f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private bool loopTour = false;

        [Header("Welcome/Intro")]
        [SerializeField] private Transform introViewpoint;
        [SerializeField] private AudioClip introNarration;
        [TextArea(3, 5)] [SerializeField] private string introText = "Welcome to the Street Art Gallery";
        [SerializeField] private float introDuration = 5f;

        [Header("UI References")]
        [SerializeField] private Canvas tourCanvas;
        [SerializeField] private TextMeshProUGUI stopNameText;
        [SerializeField] private TextMeshProUGUI narrationSubtitle;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private GameObject nextButton;
        [SerializeField] private GameObject previousButton;
        [SerializeField] private GameObject exitButton;

        [Header("Audio")]
        [SerializeField] private AudioSource narrationSource;
        [SerializeField] private float narrationVolume = 1f;

        [Header("Visual Effects")]
        [SerializeField] private CanvasGroup fadeCanvas;
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;

        [Header("Events")]
        public UnityEvent OnTourStarted;
        public UnityEvent OnTourEnded;
        public UnityEvent<int> OnStopReached;

        // State
        private bool tourActive;
        private bool isPaused;
        private int currentStopIndex = -1;
        private Coroutine tourCoroutine;
        private Coroutine transitionCoroutine;
        private Coroutine narrationCoroutine;

        // Player reference
        private Transform playerTransform;
        private Vector3 originalPlayerPosition;
        private Quaternion originalPlayerRotation;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Setup narration audio source
            if (narrationSource == null)
            {
                narrationSource = gameObject.AddComponent<AudioSource>();
                narrationSource.spatialBlend = 0; // 2D
                narrationSource.volume = narrationVolume;
            }

            // Hide tour UI initially
            if (tourCanvas != null)
            {
                tourCanvas.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            // Find player
            if (VRPlayerController.Instance != null)
            {
                playerTransform = VRPlayerController.Instance.transform;
            }
            else if (Camera.main != null)
            {
                playerTransform = Camera.main.transform.root;
            }

            // Auto-populate tour stops if not set
            if (tourStops.Count == 0)
            {
                AutoPopulateTourStops();
            }
        }

        private void AutoPopulateTourStops()
        {
            var hotspots = FindObjectsOfType<ArtworkHotspot>();

            foreach (var hotspot in hotspots)
            {
                var stop = new TourStop
                {
                    stopName = $"Artwork {hotspot.GetArtworkId()}",
                    artworkHotspot = hotspot,
                    viewpoint = CreateViewpointForHotspot(hotspot),
                    viewDuration = 8f,
                    autoAdvance = true
                };

                tourStops.Add(stop);
            }

            // Sort by artwork ID
            tourStops.Sort((a, b) =>
                a.artworkHotspot.GetArtworkId().CompareTo(b.artworkHotspot.GetArtworkId()));

            Debug.Log($"Auto-populated {tourStops.Count} tour stops");
        }

        private Transform CreateViewpointForHotspot(ArtworkHotspot hotspot)
        {
            var viewpoint = new GameObject($"Viewpoint_{hotspot.name}").transform;
            viewpoint.SetParent(transform);

            // Position in front of artwork at viewing distance
            Vector3 artworkPos = hotspot.transform.position;
            Vector3 artworkForward = hotspot.transform.forward;

            viewpoint.position = artworkPos - artworkForward * 2.5f + Vector3.up * 1.6f;
            viewpoint.LookAt(artworkPos + Vector3.up * 1.5f);

            return viewpoint;
        }

        // Public API

        public void StartTour()
        {
            if (tourActive) return;

            tourActive = true;
            isPaused = false;
            currentStopIndex = -1;

            // Store original position
            if (playerTransform != null)
            {
                originalPlayerPosition = playerTransform.position;
                originalPlayerRotation = playerTransform.rotation;
            }

            // Show tour UI
            if (tourCanvas != null)
            {
                tourCanvas.gameObject.SetActive(true);
            }

            OnTourStarted?.Invoke();

            // Start with intro if available
            if (introViewpoint != null)
            {
                tourCoroutine = StartCoroutine(PlayIntroCoroutine());
            }
            else
            {
                GoToNextStop();
            }
        }

        public void EndTour()
        {
            if (!tourActive) return;

            tourActive = false;

            // Stop all coroutines
            if (tourCoroutine != null) StopCoroutine(tourCoroutine);
            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            if (narrationCoroutine != null) StopCoroutine(narrationCoroutine);

            // Stop narration
            narrationSource.Stop();

            // Hide tour UI
            if (tourCanvas != null)
            {
                tourCanvas.gameObject.SetActive(false);
            }

            // Return to original position
            if (playerTransform != null)
            {
                StartCoroutine(TransitionToPosition(originalPlayerPosition, originalPlayerRotation));
            }

            // Deselect current artwork
            if (currentStopIndex >= 0 && currentStopIndex < tourStops.Count)
            {
                tourStops[currentStopIndex].artworkHotspot?.Deselect();
            }

            OnTourEnded?.Invoke();
        }

        public void PauseTour()
        {
            isPaused = true;
            narrationSource.Pause();
        }

        public void ResumeTour()
        {
            isPaused = false;
            narrationSource.UnPause();
        }

        public void GoToNextStop()
        {
            if (currentStopIndex + 1 >= tourStops.Count)
            {
                if (loopTour)
                {
                    GoToStop(0);
                }
                else
                {
                    EndTour();
                }
                return;
            }

            GoToStop(currentStopIndex + 1);
        }

        public void GoToPreviousStop()
        {
            if (currentStopIndex <= 0) return;
            GoToStop(currentStopIndex - 1);
        }

        public void GoToStop(int index)
        {
            if (index < 0 || index >= tourStops.Count) return;

            // Stop current coroutine
            if (tourCoroutine != null)
            {
                StopCoroutine(tourCoroutine);
            }

            // Deselect previous artwork
            if (currentStopIndex >= 0 && currentStopIndex < tourStops.Count)
            {
                tourStops[currentStopIndex].artworkHotspot?.Deselect();
            }

            currentStopIndex = index;
            tourCoroutine = StartCoroutine(PlayStopCoroutine(tourStops[index]));
        }

        private IEnumerator PlayIntroCoroutine()
        {
            // Transition to intro viewpoint
            yield return StartCoroutine(TransitionToViewpoint(introViewpoint));

            // Update UI
            UpdateUI("Welcome", introText, 0, tourStops.Count);

            // Play intro narration
            if (introNarration != null)
            {
                PlayNarration(introNarration, introText);
            }

            // Wait
            float elapsed = 0;
            while (elapsed < introDuration)
            {
                if (!isPaused)
                {
                    elapsed += Time.deltaTime;
                }
                yield return null;
            }

            // Proceed to first stop
            GoToNextStop();
        }

        private IEnumerator PlayStopCoroutine(TourStop stop)
        {
            // Fade out
            yield return StartCoroutine(FadeOut());

            // Transition to viewpoint
            yield return StartCoroutine(TransitionToViewpoint(stop.viewpoint));

            // Fade in
            yield return StartCoroutine(FadeIn());

            // Select artwork
            if (stop.artworkHotspot != null)
            {
                stop.artworkHotspot.Select();
            }

            // Update UI
            UpdateUI(stop.stopName, stop.narrationText, currentStopIndex + 1, tourStops.Count);

            OnStopReached?.Invoke(currentStopIndex);

            // Play narration
            if (stop.narrationClip != null)
            {
                PlayNarration(stop.narrationClip, stop.narrationText);
            }

            // Wait for duration or narration to finish
            float waitTime = stop.narrationClip != null ?
                Mathf.Max(stop.viewDuration, stop.narrationClip.length) :
                stop.viewDuration;

            float elapsed = 0;
            while (elapsed < waitTime)
            {
                if (!isPaused)
                {
                    elapsed += Time.deltaTime;
                }
                yield return null;
            }

            // Auto advance if enabled
            if (stop.autoAdvance && tourActive)
            {
                GoToNextStop();
            }
        }

        private IEnumerator TransitionToViewpoint(Transform viewpoint)
        {
            if (playerTransform == null || viewpoint == null) yield break;

            yield return StartCoroutine(TransitionToPosition(viewpoint.position, viewpoint.rotation));
        }

        private IEnumerator TransitionToPosition(Vector3 targetPos, Quaternion targetRot)
        {
            Vector3 startPos = playerTransform.position;
            Quaternion startRot = playerTransform.rotation;

            float elapsed = 0;

            while (elapsed < transitionDuration)
            {
                if (!isPaused)
                {
                    elapsed += Time.deltaTime;
                    float t = transitionCurve.Evaluate(elapsed / transitionDuration);

                    playerTransform.position = Vector3.Lerp(startPos, targetPos, t);
                    playerTransform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                }
                yield return null;
            }

            playerTransform.position = targetPos;
            playerTransform.rotation = targetRot;
        }

        private IEnumerator FadeOut()
        {
            if (fadeCanvas == null) yield break;

            float elapsed = 0;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                fadeCanvas.alpha = Mathf.Lerp(0, 1, elapsed / fadeOutDuration);
                yield return null;
            }
            fadeCanvas.alpha = 1;
        }

        private IEnumerator FadeIn()
        {
            if (fadeCanvas == null) yield break;

            float elapsed = 0;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                fadeCanvas.alpha = Mathf.Lerp(1, 0, elapsed / fadeInDuration);
                yield return null;
            }
            fadeCanvas.alpha = 0;
        }

        private void PlayNarration(AudioClip clip, string subtitle)
        {
            narrationSource.Stop();
            narrationSource.clip = clip;
            narrationSource.Play();

            // Show subtitles
            if (narrationCoroutine != null)
            {
                StopCoroutine(narrationCoroutine);
            }
            narrationCoroutine = StartCoroutine(ShowSubtitles(subtitle, clip.length));
        }

        private IEnumerator ShowSubtitles(string text, float duration)
        {
            if (narrationSubtitle != null)
            {
                narrationSubtitle.text = text;
                narrationSubtitle.gameObject.SetActive(true);

                yield return new WaitForSeconds(duration);

                narrationSubtitle.gameObject.SetActive(false);
            }
        }

        private void UpdateUI(string stopName, string narration, int current, int total)
        {
            if (stopNameText != null)
            {
                stopNameText.text = stopName;
            }

            if (narrationSubtitle != null)
            {
                narrationSubtitle.text = narration;
            }

            if (progressText != null)
            {
                progressText.text = $"{current} / {total}";
            }

            // Update button states
            if (previousButton != null)
            {
                previousButton.SetActive(currentStopIndex > 0);
            }

            if (nextButton != null)
            {
                nextButton.SetActive(currentStopIndex < tourStops.Count - 1);
            }
        }

        // Getters
        public bool IsTourActive() => tourActive;
        public bool IsPaused() => isPaused;
        public int GetCurrentStopIndex() => currentStopIndex;
        public int GetTotalStops() => tourStops.Count;
        public TourStop GetCurrentStop() =>
            currentStopIndex >= 0 && currentStopIndex < tourStops.Count ?
            tourStops[currentStopIndex] : null;
    }
}
