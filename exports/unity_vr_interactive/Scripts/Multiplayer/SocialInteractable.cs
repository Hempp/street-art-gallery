using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace GalleryVR.Multiplayer
{
    /// <summary>
    /// Base class for interactive social objects in the gallery.
    /// Handles seating, activities, and multiplayer interactions.
    /// </summary>
    public class SocialInteractable : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] protected string interactableName = "Interactive Object";
        [SerializeField] protected string interactionPrompt = "Press to interact";
        [SerializeField] protected float interactionRange = 2f;
        [SerializeField] protected bool requiresMultiplePlayers = false;
        [SerializeField] protected int minPlayers = 2;
        [SerializeField] protected int maxPlayers = 4;

        [Header("Visual Feedback")]
        [SerializeField] protected GameObject highlightObject;
        [SerializeField] protected Material highlightMaterial;
        [SerializeField] protected Color highlightColor = new Color(0.4f, 0.8f, 1f, 0.3f);
        [SerializeField] protected bool showPromptUI = true;

        [Header("Interaction Points")]
        [SerializeField] protected Transform[] interactionPoints;
        [SerializeField] protected bool faceCenter = true;

        [Header("Audio")]
        [SerializeField] protected AudioClip interactSound;
        [SerializeField] protected AudioClip occupySound;
        [SerializeField] protected AudioClip leaveSound;

        [Header("Events")]
        public UnityEvent OnInteractionStarted;
        public UnityEvent OnInteractionEnded;
        public UnityEvent<NetworkPlayer> OnPlayerJoined;
        public UnityEvent<NetworkPlayer> OnPlayerLeft;

        // State
        protected bool isHighlighted;
        protected bool isOccupied;
        protected List<NetworkPlayer> occupyingPlayers = new List<NetworkPlayer>();
        protected Dictionary<int, NetworkPlayer> pointOccupants = new Dictionary<int, NetworkPlayer>();

        // UI
        protected Canvas promptCanvas;
        protected TextMeshProUGUI promptText;
        protected AudioSource audioSource;

        // Components
        protected Collider interactionCollider;
        protected Renderer[] renderers;
        protected Material[] originalMaterials;

        protected virtual void Awake()
        {
            SetupComponents();
            CreatePromptUI();
        }

        protected virtual void SetupComponents()
        {
            // Get or create collider
            interactionCollider = GetComponent<Collider>();
            if (interactionCollider == null)
            {
                interactionCollider = gameObject.AddComponent<BoxCollider>();
                ((BoxCollider)interactionCollider).isTrigger = true;
            }

            // Get renderers for highlighting
            renderers = GetComponentsInChildren<Renderer>();
            originalMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalMaterials[i] = renderers[i].material;
            }

            // Audio source
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.playOnAwake = false;
            }

            // Create default interaction points if none specified
            if (interactionPoints == null || interactionPoints.Length == 0)
            {
                CreateDefaultInteractionPoints();
            }
        }

        protected virtual void CreateDefaultInteractionPoints()
        {
            // Create single interaction point at object position
            var pointObj = new GameObject("InteractionPoint_0");
            pointObj.transform.SetParent(transform);
            pointObj.transform.localPosition = new Vector3(0, 0, -1);
            interactionPoints = new Transform[] { pointObj.transform };
        }

        protected virtual void CreatePromptUI()
        {
            if (!showPromptUI) return;

            var canvasObj = new GameObject("PromptCanvas");
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = new Vector3(0, 1.5f, 0);
            canvasObj.transform.localScale = Vector3.one * 0.005f;

            promptCanvas = canvasObj.AddComponent<Canvas>();
            promptCanvas.renderMode = RenderMode.WorldSpace;

            var rectTransform = promptCanvas.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(300, 80);

            canvasObj.AddComponent<CanvasScaler>();

            // Background
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform);

            var bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var bgImage = bgObj.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);

            // Text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(canvasObj.transform);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.05f, 0.1f);
            textRect.anchorMax = new Vector2(0.95f, 0.9f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            promptText = textObj.AddComponent<TextMeshProUGUI>();
            promptText.text = interactionPrompt;
            promptText.fontSize = 24;
            promptText.alignment = TextAlignmentOptions.Center;
            promptText.color = Color.white;

            promptCanvas.gameObject.SetActive(false);
        }

        protected virtual void Update()
        {
            // Face camera for prompt
            if (promptCanvas != null && promptCanvas.gameObject.activeSelf && Camera.main != null)
            {
                promptCanvas.transform.LookAt(Camera.main.transform);
                promptCanvas.transform.Rotate(0, 180, 0);
            }
        }

        // Highlight system
        public virtual void SetHighlighted(bool highlighted)
        {
            if (isHighlighted == highlighted) return;
            isHighlighted = highlighted;

            if (highlightObject != null)
            {
                highlightObject.SetActive(highlighted);
            }
            else if (highlightMaterial != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].material = highlighted ? highlightMaterial : originalMaterials[i];
                }
            }

            // Show prompt
            if (promptCanvas != null)
            {
                promptCanvas.gameObject.SetActive(highlighted);
            }
        }

        // Interaction
        public virtual bool CanInteract(NetworkPlayer player)
        {
            if (player == null) return false;

            // Check if already occupying
            if (occupyingPlayers.Contains(player)) return false;

            // Check player count requirements
            if (requiresMultiplePlayers && occupyingPlayers.Count + 1 < minPlayers)
            {
                // Need more players, but allow join
            }

            // Check max capacity
            if (occupyingPlayers.Count >= maxPlayers) return false;

            // Check for available interaction point
            if (GetAvailablePoint() < 0) return false;

            return true;
        }

        public virtual void Interact(NetworkPlayer player)
        {
            if (!CanInteract(player)) return;

            int pointIndex = GetAvailablePoint();
            if (pointIndex < 0) return;

            // Add player
            occupyingPlayers.Add(player);
            pointOccupants[pointIndex] = player;

            // Position player at interaction point
            if (interactionPoints[pointIndex] != null)
            {
                PositionPlayerAtPoint(player, pointIndex);
            }

            // Play sound
            if (occupySound != null && audioSource != null)
            {
                audioSource.PlayOneShot(occupySound);
            }

            OnPlayerJoined?.Invoke(player);

            if (occupyingPlayers.Count == 1)
            {
                isOccupied = true;
                OnInteractionStarted?.Invoke();
            }

            UpdatePromptText();
            Debug.Log($"[SocialInteractable] {player.Username} joined {interactableName}");
        }

        public virtual void Leave(NetworkPlayer player)
        {
            if (!occupyingPlayers.Contains(player)) return;

            occupyingPlayers.Remove(player);

            // Remove from point
            int pointIndex = -1;
            foreach (var kvp in pointOccupants)
            {
                if (kvp.Value == player)
                {
                    pointIndex = kvp.Key;
                    break;
                }
            }

            if (pointIndex >= 0)
            {
                pointOccupants.Remove(pointIndex);
            }

            // Play sound
            if (leaveSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(leaveSound);
            }

            OnPlayerLeft?.Invoke(player);

            if (occupyingPlayers.Count == 0)
            {
                isOccupied = false;
                OnInteractionEnded?.Invoke();
            }

            UpdatePromptText();
            Debug.Log($"[SocialInteractable] {player.Username} left {interactableName}");
        }

        protected virtual int GetAvailablePoint()
        {
            for (int i = 0; i < interactionPoints.Length; i++)
            {
                if (!pointOccupants.ContainsKey(i))
                {
                    return i;
                }
            }
            return -1;
        }

        protected virtual void PositionPlayerAtPoint(NetworkPlayer player, int pointIndex)
        {
            if (pointIndex < 0 || pointIndex >= interactionPoints.Length) return;

            Transform point = interactionPoints[pointIndex];
            Vector3 targetPos = point.position;
            Quaternion targetRot = point.rotation;

            if (faceCenter)
            {
                Vector3 toCenter = transform.position - targetPos;
                toCenter.y = 0;
                if (toCenter.magnitude > 0.1f)
                {
                    targetRot = Quaternion.LookRotation(toCenter);
                }
            }

            player.TeleportTo(targetPos, targetRot);
        }

        protected virtual void UpdatePromptText()
        {
            if (promptText == null) return;

            if (occupyingPlayers.Count >= maxPlayers)
            {
                promptText.text = "Full";
            }
            else if (requiresMultiplePlayers)
            {
                int needed = minPlayers - occupyingPlayers.Count;
                if (needed > 0)
                {
                    promptText.text = $"{interactionPrompt}\n({needed} more needed)";
                }
                else
                {
                    promptText.text = interactionPrompt;
                }
            }
            else
            {
                promptText.text = $"{interactionPrompt}\n({occupyingPlayers.Count}/{maxPlayers})";
            }
        }

        // Getters
        public bool IsOccupied => isOccupied;
        public int OccupantCount => occupyingPlayers.Count;
        public IReadOnlyList<NetworkPlayer> Occupants => occupyingPlayers;
        public string Name => interactableName;
    }

    /// <summary>
    /// Seating object for social gathering.
    /// </summary>
    public class SocialSeat : SocialInteractable
    {
        [Header("Seat Settings")]
        [SerializeField] private bool playsSitAnimation = true;
        [SerializeField] private float seatHeight = 0.5f;

        protected override void Awake()
        {
            base.Awake();
            interactableName = "Seat";
            interactionPrompt = "Press to sit";
            maxPlayers = 1;
        }

        protected override void PositionPlayerAtPoint(NetworkPlayer player, int pointIndex)
        {
            base.PositionPlayerAtPoint(player, pointIndex);

            // Could trigger sit animation on player
            if (playsSitAnimation)
            {
                // player.PlayAnimation("Sit");
            }
        }
    }

    /// <summary>
    /// Table with multiple seats for group activities.
    /// </summary>
    public class SocialTable : SocialInteractable
    {
        [Header("Table Settings")]
        [SerializeField] private float tableRadius = 1.5f;
        [SerializeField] private int seatCount = 4;
        [SerializeField] private bool hasActivity = false;
        [SerializeField] private string activityName = "";

        protected override void Awake()
        {
            interactableName = string.IsNullOrEmpty(activityName) ? "Table" : activityName;
            interactionPrompt = hasActivity ? $"Join {activityName}" : "Sit at table";
            maxPlayers = seatCount;

            base.Awake();
        }

        protected override void CreateDefaultInteractionPoints()
        {
            interactionPoints = new Transform[seatCount];

            for (int i = 0; i < seatCount; i++)
            {
                float angle = (360f / seatCount) * i * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(
                    Mathf.Cos(angle) * tableRadius,
                    0,
                    Mathf.Sin(angle) * tableRadius
                );

                var pointObj = new GameObject($"Seat_{i}");
                pointObj.transform.SetParent(transform);
                pointObj.transform.localPosition = pos;

                interactionPoints[i] = pointObj.transform;
            }
        }
    }

    /// <summary>
    /// Photo spot for taking group photos.
    /// </summary>
    public class PhotoSpot : SocialInteractable
    {
        [Header("Photo Settings")]
        [SerializeField] private Transform cameraPosition;
        [SerializeField] private float countdownTime = 3f;
        [SerializeField] private AudioClip countdownSound;
        [SerializeField] private AudioClip shutterSound;
        [SerializeField] private GameObject flashEffect;

        private bool isTakingPhoto;

        protected override void Awake()
        {
            interactableName = "Photo Spot";
            interactionPrompt = "Take group photo";
            maxPlayers = 8;

            base.Awake();
        }

        public override void Interact(NetworkPlayer player)
        {
            base.Interact(player);

            // Start countdown when enough players
            if (occupyingPlayers.Count >= 2 && !isTakingPhoto)
            {
                StartCoroutine(TakePhotoSequence());
            }
        }

        private IEnumerator TakePhotoSequence()
        {
            isTakingPhoto = true;

            // Countdown
            for (int i = (int)countdownTime; i > 0; i--)
            {
                if (promptText != null)
                {
                    promptText.text = i.ToString();
                }

                if (countdownSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(countdownSound);
                }

                yield return new WaitForSeconds(1f);
            }

            // Take photo
            TakePhoto();

            yield return new WaitForSeconds(1f);

            isTakingPhoto = false;
            UpdatePromptText();
        }

        private void TakePhoto()
        {
            // Flash effect
            if (flashEffect != null)
            {
                flashEffect.SetActive(true);
                StartCoroutine(DisableAfterDelay(flashEffect, 0.2f));
            }

            // Shutter sound
            if (shutterSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(shutterSound);
            }

            // Capture screenshot (in real implementation)
            Debug.Log("[PhotoSpot] Photo captured!");

            // Could save to player gallery or share
        }

        private IEnumerator DisableAfterDelay(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            obj.SetActive(false);
        }
    }

    /// <summary>
    /// Activity zone for multiplayer games/activities.
    /// </summary>
    public class ActivityZone : SocialInteractable
    {
        [Header("Activity Settings")]
        [SerializeField] private ActivityType activityType = ActivityType.Discussion;
        [SerializeField] private bool autoStart = false;
        [SerializeField] private float activityDuration = 0f; // 0 = no limit

        public enum ActivityType
        {
            Discussion,
            Game,
            Performance,
            Workshop,
            Meditation
        }

        private bool activityStarted;
        private float activityTimer;

        protected override void Awake()
        {
            SetActivityDefaults();
            base.Awake();
        }

        private void SetActivityDefaults()
        {
            switch (activityType)
            {
                case ActivityType.Discussion:
                    interactableName = "Discussion Circle";
                    interactionPrompt = "Join discussion";
                    minPlayers = 2;
                    maxPlayers = 8;
                    break;

                case ActivityType.Game:
                    interactableName = "Game Table";
                    interactionPrompt = "Join game";
                    minPlayers = 2;
                    maxPlayers = 4;
                    requiresMultiplePlayers = true;
                    break;

                case ActivityType.Performance:
                    interactableName = "Stage";
                    interactionPrompt = "Perform";
                    minPlayers = 1;
                    maxPlayers = 6;
                    break;

                case ActivityType.Workshop:
                    interactableName = "Workshop Area";
                    interactionPrompt = "Join workshop";
                    minPlayers = 1;
                    maxPlayers = 12;
                    break;

                case ActivityType.Meditation:
                    interactableName = "Meditation Space";
                    interactionPrompt = "Join meditation";
                    minPlayers = 1;
                    maxPlayers = 20;
                    break;
            }
        }

        protected override void Update()
        {
            base.Update();

            // Check auto-start conditions
            if (!activityStarted && autoStart && occupyingPlayers.Count >= minPlayers)
            {
                StartActivity();
            }

            // Update timer
            if (activityStarted && activityDuration > 0)
            {
                activityTimer += Time.deltaTime;
                if (activityTimer >= activityDuration)
                {
                    EndActivity();
                }
            }
        }

        public void StartActivity()
        {
            if (activityStarted) return;
            if (occupyingPlayers.Count < minPlayers) return;

            activityStarted = true;
            activityTimer = 0;

            Debug.Log($"[ActivityZone] {interactableName} started with {occupyingPlayers.Count} players");

            // Notify all players
            foreach (var player in occupyingPlayers)
            {
                // Send notification/UI update
            }
        }

        public void EndActivity()
        {
            if (!activityStarted) return;

            activityStarted = false;

            Debug.Log($"[ActivityZone] {interactableName} ended");

            // Clear all players
            while (occupyingPlayers.Count > 0)
            {
                Leave(occupyingPlayers[0]);
            }
        }

        public bool IsActivityActive => activityStarted;
        public float ActivityTimeRemaining => activityDuration > 0 ? Mathf.Max(0, activityDuration - activityTimer) : -1;
    }

    /// <summary>
    /// Viewing bench for artwork.
    /// </summary>
    public class ViewingBench : SocialInteractable
    {
        [Header("Bench Settings")]
        [SerializeField] private int seatsOnBench = 3;
        [SerializeField] private float seatSpacing = 0.6f;
        [SerializeField] private Transform artworkToView;

        protected override void Awake()
        {
            interactableName = "Bench";
            interactionPrompt = "Sit and view artwork";
            maxPlayers = seatsOnBench;
            faceCenter = false; // Face artwork instead

            base.Awake();
        }

        protected override void CreateDefaultInteractionPoints()
        {
            interactionPoints = new Transform[seatsOnBench];

            float startX = -((seatsOnBench - 1) * seatSpacing) / 2f;

            for (int i = 0; i < seatsOnBench; i++)
            {
                var pointObj = new GameObject($"Seat_{i}");
                pointObj.transform.SetParent(transform);
                pointObj.transform.localPosition = new Vector3(startX + i * seatSpacing, 0, 0);

                interactionPoints[i] = pointObj.transform;
            }
        }

        protected override void PositionPlayerAtPoint(NetworkPlayer player, int pointIndex)
        {
            if (pointIndex < 0 || pointIndex >= interactionPoints.Length) return;

            Transform point = interactionPoints[pointIndex];
            Vector3 targetPos = point.position;
            Quaternion targetRot;

            // Face artwork if assigned, otherwise face forward
            if (artworkToView != null)
            {
                Vector3 toArtwork = artworkToView.position - targetPos;
                toArtwork.y = 0;
                targetRot = Quaternion.LookRotation(toArtwork);
            }
            else
            {
                targetRot = transform.rotation;
            }

            player.TeleportTo(targetPos, targetRot);
        }
    }
}
