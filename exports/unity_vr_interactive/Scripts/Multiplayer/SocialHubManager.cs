using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace GalleryVR.Multiplayer
{
    /// <summary>
    /// Manages social hub areas and coordinates social features.
    /// Creates gathering zones, activity areas, and facilitates social interactions.
    /// </summary>
    public class SocialHubManager : MonoBehaviour
    {
        public static SocialHubManager Instance { get; private set; }

        [Header("Hub Settings")]
        [SerializeField] private Transform galleryRoot;
        [SerializeField] private bool autoCreateHubs = true;
        [SerializeField] private float hubDetectionRange = 10f;

        [Header("Prefabs")]
        [SerializeField] private GameObject socialSeatPrefab;
        [SerializeField] private GameObject socialTablePrefab;
        [SerializeField] private GameObject photoSpotPrefab;
        [SerializeField] private GameObject viewingBenchPrefab;
        [SerializeField] private GameObject activityZonePrefab;

        [Header("Hub Areas")]
        [SerializeField] private SocialHubArea[] hubAreas;

        [Header("Player Interaction")]
        [SerializeField] private float interactionCheckInterval = 0.2f;
        [SerializeField] private LayerMask interactableLayer;

        [Header("Events")]
        public UnityEvent<SocialHubArea> OnPlayerEnteredHub;
        public UnityEvent<SocialHubArea> OnPlayerLeftHub;
        public UnityEvent<SocialInteractable> OnInteractableHighlighted;

        // State
        private SocialHubArea currentHub;
        private SocialInteractable currentHighlightedInteractable;
        private List<SocialInteractable> allInteractables = new List<SocialInteractable>();
        private float lastInteractionCheck;

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
        }

        private void Start()
        {
            if (autoCreateHubs)
            {
                CreateDefaultHubAreas();
            }

            // Find all interactables in scene
            RefreshInteractablesList();

            // Subscribe to network events
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnPlayerJoined.AddListener(OnNetworkPlayerJoined);
                NetworkManager.Instance.OnPlayerLeft.AddListener(OnNetworkPlayerLeft);
            }
        }

        private void Update()
        {
            // Check for nearby interactables periodically
            if (Time.time - lastInteractionCheck > interactionCheckInterval)
            {
                lastInteractionCheck = Time.time;
                CheckNearbyInteractables();
                CheckCurrentHub();
            }

            // Handle interaction input
            HandleInteractionInput();
        }

        private void CreateDefaultHubAreas()
        {
            if (galleryRoot == null)
            {
                galleryRoot = transform;
            }

            var hubList = new List<SocialHubArea>();

            // Create entrance/lobby hub
            var entranceHub = CreateHubArea("Gallery Entrance", new Vector3(0, 0, -8), 8f);
            entranceHub.hubType = HubType.Lounge;
            hubList.Add(entranceHub);

            // Add seating
            CreateSeat(entranceHub.transform, new Vector3(-2, 0, -1));
            CreateSeat(entranceHub.transform, new Vector3(2, 0, -1));
            CreateTable(entranceHub.transform, new Vector3(0, 0, 0), 4);

            // Create central gathering area
            var centralHub = CreateHubArea("Central Gallery", new Vector3(0, 0, 5), 10f);
            centralHub.hubType = HubType.GatheringPoint;
            hubList.Add(centralHub);

            // Add photo spot in center
            CreatePhotoSpot(centralHub.transform, new Vector3(0, 0, 0));

            // Viewing benches facing artwork walls
            CreateViewingBench(centralHub.transform, new Vector3(-8, 0, 0), Quaternion.Euler(0, 90, 0));
            CreateViewingBench(centralHub.transform, new Vector3(8, 0, 0), Quaternion.Euler(0, -90, 0));

            // Create lounge area
            var loungeHub = CreateHubArea("Artist Lounge", new Vector3(10, 0, 15), 6f);
            loungeHub.hubType = HubType.Lounge;
            hubList.Add(loungeHub);

            // Lounge seating arrangement
            CreateTable(loungeHub.transform, Vector3.zero, 6);
            CreateSeat(loungeHub.transform, new Vector3(-3, 0, 2));
            CreateSeat(loungeHub.transform, new Vector3(3, 0, 2));

            // Create discussion area
            var discussionHub = CreateHubArea("Discussion Corner", new Vector3(-10, 0, 15), 5f);
            discussionHub.hubType = HubType.Discussion;
            hubList.Add(discussionHub);

            // Circle of seats
            CreateActivityZone(discussionHub.transform, Vector3.zero, ActivityZone.ActivityType.Discussion);

            // Create activity area
            var activityHub = CreateHubArea("Creative Space", new Vector3(0, 0, 20), 8f);
            activityHub.hubType = HubType.Activity;
            hubList.Add(activityHub);

            // Activity table
            CreateActivityZone(activityHub.transform, Vector3.zero, ActivityZone.ActivityType.Workshop);

            hubAreas = hubList.ToArray();

            Debug.Log($"[SocialHub] Created {hubAreas.Length} hub areas with social interactables");
        }

        private SocialHubArea CreateHubArea(string name, Vector3 position, float radius)
        {
            var hubObj = new GameObject($"Hub_{name}");
            hubObj.transform.SetParent(galleryRoot);
            hubObj.transform.localPosition = position;

            var hub = hubObj.AddComponent<SocialHubArea>();
            hub.hubName = name;
            hub.radius = radius;

            // Add trigger collider
            var collider = hubObj.AddComponent<SphereCollider>();
            collider.radius = radius;
            collider.isTrigger = true;

            // Add visual indicator (optional)
            CreateHubVisuals(hubObj.transform, radius);

            return hub;
        }

        private void CreateHubVisuals(Transform parent, float radius)
        {
            // Create subtle floor indicator
            var indicatorObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            indicatorObj.name = "HubIndicator";
            indicatorObj.transform.SetParent(parent);
            indicatorObj.transform.localPosition = new Vector3(0, 0.01f, 0);
            indicatorObj.transform.localScale = new Vector3(radius * 2, 0.01f, radius * 2);

            // Remove collider
            DestroyImmediate(indicatorObj.GetComponent<Collider>());

            // Semi-transparent material
            var renderer = indicatorObj.GetComponent<Renderer>();
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetFloat("_Surface", 1); // Transparent
            material.color = new Color(0.3f, 0.5f, 0.8f, 0.1f);
            renderer.material = material;
        }

        private void CreateSeat(Transform parent, Vector3 localPosition)
        {
            GameObject seatObj;

            if (socialSeatPrefab != null)
            {
                seatObj = Instantiate(socialSeatPrefab, parent);
            }
            else
            {
                seatObj = CreateDefaultSeat();
                seatObj.transform.SetParent(parent);
            }

            seatObj.transform.localPosition = localPosition;
            allInteractables.Add(seatObj.GetComponent<SocialInteractable>());
        }

        private GameObject CreateDefaultSeat()
        {
            var seatObj = new GameObject("Seat");

            // Seat base
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(seatObj.transform);
            cube.transform.localPosition = new Vector3(0, 0.25f, 0);
            cube.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            var renderer = cube.GetComponent<Renderer>();
            renderer.material.color = new Color(0.3f, 0.3f, 0.35f);

            // Add seat component
            var seat = seatObj.AddComponent<SocialSeat>();

            return seatObj;
        }

        private void CreateTable(Transform parent, Vector3 localPosition, int seats)
        {
            GameObject tableObj;

            if (socialTablePrefab != null)
            {
                tableObj = Instantiate(socialTablePrefab, parent);
            }
            else
            {
                tableObj = CreateDefaultTable(seats);
                tableObj.transform.SetParent(parent);
            }

            tableObj.transform.localPosition = localPosition;

            var table = tableObj.GetComponent<SocialTable>();
            if (table != null)
            {
                allInteractables.Add(table);
            }
        }

        private GameObject CreateDefaultTable(int seats)
        {
            var tableObj = new GameObject("Table");

            // Table top
            var top = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            top.name = "TableTop";
            top.transform.SetParent(tableObj.transform);
            top.transform.localPosition = new Vector3(0, 0.75f, 0);
            top.transform.localScale = new Vector3(1.5f, 0.05f, 1.5f);

            var renderer = top.GetComponent<Renderer>();
            renderer.material.color = new Color(0.4f, 0.3f, 0.2f);

            // Table leg
            var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leg.name = "TableLeg";
            leg.transform.SetParent(tableObj.transform);
            leg.transform.localPosition = new Vector3(0, 0.35f, 0);
            leg.transform.localScale = new Vector3(0.15f, 0.35f, 0.15f);

            leg.GetComponent<Renderer>().material.color = new Color(0.3f, 0.25f, 0.2f);

            // Add table component
            var table = tableObj.AddComponent<SocialTable>();

            return tableObj;
        }

        private void CreatePhotoSpot(Transform parent, Vector3 localPosition)
        {
            GameObject photoObj;

            if (photoSpotPrefab != null)
            {
                photoObj = Instantiate(photoSpotPrefab, parent);
            }
            else
            {
                photoObj = CreateDefaultPhotoSpot();
                photoObj.transform.SetParent(parent);
            }

            photoObj.transform.localPosition = localPosition;

            var photoSpot = photoObj.GetComponent<PhotoSpot>();
            if (photoSpot != null)
            {
                allInteractables.Add(photoSpot);
            }
        }

        private GameObject CreateDefaultPhotoSpot()
        {
            var photoObj = new GameObject("PhotoSpot");

            // Photo frame/backdrop indicator
            var backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backdrop.name = "PhotoBackdrop";
            backdrop.transform.SetParent(photoObj.transform);
            backdrop.transform.localPosition = new Vector3(0, 2f, 2f);
            backdrop.transform.localScale = new Vector3(4f, 3f, 1f);
            backdrop.transform.rotation = Quaternion.Euler(0, 180, 0);

            var renderer = backdrop.GetComponent<Renderer>();
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.color = new Color(0.1f, 0.1f, 0.15f);
            renderer.material = material;

            // Camera icon indicator
            var camIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            camIndicator.name = "CameraIndicator";
            camIndicator.transform.SetParent(photoObj.transform);
            camIndicator.transform.localPosition = new Vector3(0, 1.5f, -3f);
            camIndicator.transform.localScale = new Vector3(0.3f, 0.2f, 0.2f);

            // Add photo spot component
            var photoSpot = photoObj.AddComponent<PhotoSpot>();

            return photoObj;
        }

        private void CreateViewingBench(Transform parent, Vector3 localPosition, Quaternion rotation)
        {
            GameObject benchObj;

            if (viewingBenchPrefab != null)
            {
                benchObj = Instantiate(viewingBenchPrefab, parent);
            }
            else
            {
                benchObj = CreateDefaultBench();
                benchObj.transform.SetParent(parent);
            }

            benchObj.transform.localPosition = localPosition;
            benchObj.transform.localRotation = rotation;

            var bench = benchObj.GetComponent<ViewingBench>();
            if (bench != null)
            {
                allInteractables.Add(bench);
            }
        }

        private GameObject CreateDefaultBench()
        {
            var benchObj = new GameObject("ViewingBench");

            // Bench seat
            var seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seat.name = "BenchSeat";
            seat.transform.SetParent(benchObj.transform);
            seat.transform.localPosition = new Vector3(0, 0.4f, 0);
            seat.transform.localScale = new Vector3(2f, 0.1f, 0.5f);

            seat.GetComponent<Renderer>().material.color = new Color(0.35f, 0.35f, 0.4f);

            // Bench legs
            for (int i = -1; i <= 1; i += 2)
            {
                var leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.name = $"BenchLeg_{i}";
                leg.transform.SetParent(benchObj.transform);
                leg.transform.localPosition = new Vector3(i * 0.8f, 0.2f, 0);
                leg.transform.localScale = new Vector3(0.1f, 0.4f, 0.4f);

                leg.GetComponent<Renderer>().material.color = new Color(0.25f, 0.25f, 0.3f);
            }

            // Add viewing bench component
            var bench = benchObj.AddComponent<ViewingBench>();

            return benchObj;
        }

        private void CreateActivityZone(Transform parent, Vector3 localPosition, ActivityZone.ActivityType type)
        {
            var zoneObj = new GameObject($"ActivityZone_{type}");
            zoneObj.transform.SetParent(parent);
            zoneObj.transform.localPosition = localPosition;

            var zone = zoneObj.AddComponent<ActivityZone>();
            // Activity type is set in ActivityZone.Awake based on serialized field

            // Create visual boundary
            var boundary = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            boundary.name = "ZoneBoundary";
            boundary.transform.SetParent(zoneObj.transform);
            boundary.transform.localPosition = new Vector3(0, 0.01f, 0);
            boundary.transform.localScale = new Vector3(4f, 0.01f, 4f);

            DestroyImmediate(boundary.GetComponent<Collider>());

            var renderer = boundary.GetComponent<Renderer>();
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetFloat("_Surface", 1);
            material.color = new Color(0.4f, 0.6f, 0.4f, 0.15f);
            renderer.material = material;

            allInteractables.Add(zone);
        }

        // Runtime interaction checking
        private void CheckNearbyInteractables()
        {
            if (NetworkManager.Instance?.LocalPlayer == null) return;

            Transform playerTransform = NetworkManager.Instance.LocalPlayer.transform;
            Vector3 playerPos = playerTransform.position;

            SocialInteractable closest = null;
            float closestDist = float.MaxValue;

            foreach (var interactable in allInteractables)
            {
                if (interactable == null) continue;

                float dist = Vector3.Distance(playerPos, interactable.transform.position);
                if (dist < 3f && dist < closestDist) // 3m interaction range
                {
                    closest = interactable;
                    closestDist = dist;
                }
            }

            // Update highlighting
            if (closest != currentHighlightedInteractable)
            {
                if (currentHighlightedInteractable != null)
                {
                    currentHighlightedInteractable.SetHighlighted(false);
                }

                currentHighlightedInteractable = closest;

                if (currentHighlightedInteractable != null)
                {
                    currentHighlightedInteractable.SetHighlighted(true);
                    OnInteractableHighlighted?.Invoke(currentHighlightedInteractable);
                }
            }
        }

        private void CheckCurrentHub()
        {
            if (NetworkManager.Instance?.LocalPlayer == null) return;

            Vector3 playerPos = NetworkManager.Instance.LocalPlayer.transform.position;

            SocialHubArea newHub = null;

            foreach (var hub in hubAreas)
            {
                if (hub == null) continue;

                float dist = Vector3.Distance(playerPos, hub.transform.position);
                if (dist <= hub.radius)
                {
                    newHub = hub;
                    break;
                }
            }

            if (newHub != currentHub)
            {
                if (currentHub != null)
                {
                    OnPlayerLeftHub?.Invoke(currentHub);
                    Debug.Log($"[SocialHub] Left hub: {currentHub.hubName}");
                }

                currentHub = newHub;

                if (currentHub != null)
                {
                    OnPlayerEnteredHub?.Invoke(currentHub);
                    Debug.Log($"[SocialHub] Entered hub: {currentHub.hubName}");
                }
            }
        }

        private void HandleInteractionInput()
        {
            if (currentHighlightedInteractable == null) return;
            if (NetworkManager.Instance?.LocalPlayer == null) return;

            // Check for interaction trigger
            bool triggerPressed = false;

            // VR trigger
            if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand)
                .TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool trigger))
            {
                triggerPressed = trigger;
            }

            // Keyboard fallback
            if (Input.GetKeyDown(KeyCode.E))
            {
                triggerPressed = true;
            }

            if (triggerPressed)
            {
                var localPlayer = NetworkManager.Instance.LocalPlayer;

                // Check if already occupying - leave if so
                if (currentHighlightedInteractable.Occupants.Contains(localPlayer))
                {
                    currentHighlightedInteractable.Leave(localPlayer);
                }
                else
                {
                    currentHighlightedInteractable.Interact(localPlayer);
                }
            }
        }

        private void RefreshInteractablesList()
        {
            allInteractables.Clear();
            allInteractables.AddRange(FindObjectsOfType<SocialInteractable>());
            Debug.Log($"[SocialHub] Found {allInteractables.Count} social interactables");
        }

        // Network callbacks
        private void OnNetworkPlayerJoined(NetworkPlayer player)
        {
            // Create voice source for new player
            if (VoiceChatManager.Instance != null)
            {
                VoiceChatManager.Instance.CreatePlayerVoiceSource(player.PlayerId, player.transform);
            }
        }

        private void OnNetworkPlayerLeft(NetworkPlayer player)
        {
            // Remove from any interactables
            foreach (var interactable in allInteractables)
            {
                if (interactable != null && interactable.Occupants.Contains(player))
                {
                    interactable.Leave(player);
                }
            }

            // Remove voice source
            if (VoiceChatManager.Instance != null)
            {
                VoiceChatManager.Instance.RemovePlayerVoiceSource(player.PlayerId);
            }
        }

        // Public API
        public SocialHubArea GetCurrentHub() => currentHub;
        public SocialInteractable GetHighlightedInteractable() => currentHighlightedInteractable;
        public IReadOnlyList<SocialInteractable> GetAllInteractables() => allInteractables;

        public SocialHubArea[] GetAllHubs() => hubAreas;

        public int GetPlayersInHub(SocialHubArea hub)
        {
            if (hub == null || NetworkManager.Instance == null) return 0;

            int count = 0;
            foreach (var player in NetworkManager.Instance.Players.Values)
            {
                float dist = Vector3.Distance(player.transform.position, hub.transform.position);
                if (dist <= hub.radius)
                {
                    count++;
                }
            }
            return count;
        }
    }

    /// <summary>
    /// Defines a social hub area in the gallery.
    /// </summary>
    public class SocialHubArea : MonoBehaviour
    {
        public string hubName = "Social Hub";
        public HubType hubType = HubType.GatheringPoint;
        public float radius = 5f;
        public string description = "";

        [Header("Ambient")]
        public AudioClip ambientSound;
        public float ambientVolume = 0.3f;

        private AudioSource ambientSource;

        private void Start()
        {
            if (ambientSound != null)
            {
                ambientSource = gameObject.AddComponent<AudioSource>();
                ambientSource.clip = ambientSound;
                ambientSource.loop = true;
                ambientSource.volume = ambientVolume;
                ambientSource.spatialBlend = 1f;
                ambientSource.Play();
            }
        }
    }

    public enum HubType
    {
        GatheringPoint,
        Lounge,
        Discussion,
        Activity,
        Performance,
        Quiet
    }
}
