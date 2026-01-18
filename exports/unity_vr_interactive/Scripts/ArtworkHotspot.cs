using UnityEngine;
using UnityEngine.Events;

namespace GalleryVR
{
    public class ArtworkHotspot : MonoBehaviour
    {
        [Header("Artwork Reference")]
        [SerializeField] private int artworkId;
        [SerializeField] private string artworkName;
        [SerializeField] private Transform artworkTransform;
        [SerializeField] private Transform infoPanelAnchor;

        [Header("Interaction Settings")]
        [SerializeField] private float activationDistance = 3f;
        [SerializeField] private float gazeActivationTime = 1.5f;
        [SerializeField] private bool requireGaze = false;
        [SerializeField] private bool highlightOnHover = true;

        [Header("Visual Feedback")]
        [SerializeField] private GameObject hoverIndicator;
        [SerializeField] private GameObject selectionIndicator;
        [SerializeField] private Material highlightMaterial;
        [SerializeField] private float highlightIntensity = 0.3f;

        [Header("Audio")]
        [SerializeField] private AudioClip hoverSound;
        [SerializeField] private AudioClip selectSound;
        [SerializeField] private AudioClip artworkAmbientSound;

        [Header("Events")]
        public UnityEvent OnHoverEnter;
        public UnityEvent OnHoverExit;
        public UnityEvent OnSelected;

        // State
        private bool isHovered;
        private bool isSelected;
        private float gazeTimer;
        private Material originalMaterial;
        private Renderer artworkRenderer;
        private AudioSource audioSource;
        private InfoPanelController infoPanelController;

        // Gaze indicator
        private GameObject gazeProgressIndicator;
        private float gazeProgress;

        private void Start()
        {
            // Get components
            if (artworkTransform != null)
            {
                artworkRenderer = artworkTransform.GetComponent<Renderer>();
                if (artworkRenderer != null)
                {
                    originalMaterial = artworkRenderer.material;
                }
            }

            // Setup audio
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f; // 3D sound
                audioSource.minDistance = 1f;
                audioSource.maxDistance = 10f;
            }

            // Find info panel controller
            infoPanelController = FindObjectOfType<InfoPanelController>();

            // Create gaze progress indicator
            CreateGazeIndicator();

            // Hide indicators initially
            if (hoverIndicator) hoverIndicator.SetActive(false);
            if (selectionIndicator) selectionIndicator.SetActive(false);

            // Setup info panel anchor if not assigned
            if (infoPanelAnchor == null)
            {
                var anchor = new GameObject("InfoPanelAnchor");
                anchor.transform.SetParent(transform);
                anchor.transform.localPosition = Vector3.up * 0.5f + Vector3.forward * 0.3f;
                infoPanelAnchor = anchor.transform;
            }
        }

        private void CreateGazeIndicator()
        {
            if (!requireGaze) return;

            gazeProgressIndicator = new GameObject("GazeProgress");
            gazeProgressIndicator.transform.SetParent(transform);
            gazeProgressIndicator.transform.localPosition = Vector3.zero;

            // Create circular progress indicator
            var lineRenderer = gazeProgressIndicator.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = false;
            lineRenderer.startWidth = 0.02f;
            lineRenderer.endWidth = 0.02f;
            lineRenderer.positionCount = 37;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.color = Color.white;
            lineRenderer.material = mat;

            gazeProgressIndicator.SetActive(false);
        }

        private void UpdateGazeIndicator()
        {
            if (gazeProgressIndicator == null) return;

            var lineRenderer = gazeProgressIndicator.GetComponent<LineRenderer>();
            if (lineRenderer == null) return;

            int segments = Mathf.RoundToInt(36 * gazeProgress);
            lineRenderer.positionCount = segments + 1;

            float radius = 0.15f;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (i / 36f) * Mathf.PI * 2f;
                Vector3 pos = new Vector3(Mathf.Sin(angle) * radius, Mathf.Cos(angle) * radius, 0);
                lineRenderer.SetPosition(i, pos);
            }

            // Face camera
            if (Camera.main != null)
            {
                gazeProgressIndicator.transform.LookAt(Camera.main.transform);
            }
        }

        private void Update()
        {
            CheckPlayerProximity();

            if (isHovered && requireGaze)
            {
                UpdateGazeTimer();
            }
        }

        private void CheckPlayerProximity()
        {
            if (Camera.main == null) return;

            float distance = Vector3.Distance(transform.position, Camera.main.transform.position);

            if (distance <= activationDistance)
            {
                // Check if looking at artwork
                if (IsPlayerLookingAt())
                {
                    if (!isHovered)
                    {
                        OnHoverEnterInternal();
                    }
                }
                else if (isHovered && !isSelected)
                {
                    OnHoverExitInternal();
                }
            }
            else if (isHovered)
            {
                OnHoverExitInternal();
            }
        }

        private bool IsPlayerLookingAt()
        {
            if (Camera.main == null) return false;

            Ray gazeRay = new Ray(Camera.main.transform.position, Camera.main.transform.forward);

            // Check if gaze ray hits this hotspot's collider
            if (Physics.Raycast(gazeRay, out RaycastHit hit, activationDistance * 1.5f))
            {
                return hit.transform == transform || hit.transform == artworkTransform;
            }

            return false;
        }

        private void UpdateGazeTimer()
        {
            gazeTimer += Time.deltaTime;
            gazeProgress = gazeTimer / gazeActivationTime;

            if (gazeProgressIndicator != null)
            {
                gazeProgressIndicator.SetActive(true);
                UpdateGazeIndicator();
            }

            if (gazeTimer >= gazeActivationTime)
            {
                Select();
            }
        }

        private void OnHoverEnterInternal()
        {
            isHovered = true;
            gazeTimer = 0;
            gazeProgress = 0;

            // Visual feedback
            if (hoverIndicator) hoverIndicator.SetActive(true);

            if (highlightOnHover && artworkRenderer != null)
            {
                ApplyHighlight();
            }

            // Audio feedback
            if (hoverSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(hoverSound, 0.5f);
            }

            OnHoverEnter?.Invoke();

            // Auto-select if not using gaze timer
            if (!requireGaze)
            {
                Select();
            }
        }

        private void OnHoverExitInternal()
        {
            isHovered = false;
            gazeTimer = 0;
            gazeProgress = 0;

            // Visual feedback
            if (hoverIndicator) hoverIndicator.SetActive(false);
            if (gazeProgressIndicator) gazeProgressIndicator.SetActive(false);

            if (highlightOnHover && artworkRenderer != null)
            {
                RemoveHighlight();
            }

            OnHoverExit?.Invoke();

            // Deselect
            if (isSelected)
            {
                Deselect();
            }
        }

        public void Select()
        {
            if (isSelected) return;

            isSelected = true;
            gazeTimer = 0;

            if (gazeProgressIndicator) gazeProgressIndicator.SetActive(false);
            if (selectionIndicator) selectionIndicator.SetActive(true);

            // Audio feedback
            if (selectSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(selectSound);
            }

            // Play ambient sound for this artwork
            if (artworkAmbientSound != null && audioSource != null)
            {
                audioSource.clip = artworkAmbientSound;
                audioSource.loop = true;
                audioSource.Play();
            }

            // Show info panel
            ShowInfoPanel();

            OnSelected?.Invoke();
        }

        public void Deselect()
        {
            isSelected = false;

            if (selectionIndicator) selectionIndicator.SetActive(false);

            // Stop ambient sound
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            // Hide info panel
            HideInfoPanel();
        }

        private void ShowInfoPanel()
        {
            if (infoPanelController != null)
            {
                Vector3 panelPosition = infoPanelAnchor != null ?
                    infoPanelAnchor.position :
                    transform.position + Vector3.up * 0.5f;

                infoPanelController.ShowArtworkInfo(artworkId, panelPosition);
            }
        }

        private void HideInfoPanel()
        {
            if (infoPanelController != null)
            {
                infoPanelController.HideArtworkInfo();
            }
        }

        private void ApplyHighlight()
        {
            if (artworkRenderer == null) return;

            if (highlightMaterial != null)
            {
                // Use dedicated highlight material
                artworkRenderer.material = highlightMaterial;
            }
            else
            {
                // Add emission to existing material
                var mat = artworkRenderer.material;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.white * highlightIntensity);
            }
        }

        private void RemoveHighlight()
        {
            if (artworkRenderer == null || originalMaterial == null) return;

            artworkRenderer.material = originalMaterial;
        }

        // Called by VR pointer/hand interaction
        public void OnPointerEnter()
        {
            if (!isHovered)
            {
                OnHoverEnterInternal();
            }
        }

        public void OnPointerExit()
        {
            if (isHovered)
            {
                OnHoverExitInternal();
            }
        }

        public void OnPointerClick()
        {
            if (isSelected)
            {
                Deselect();
            }
            else
            {
                Select();
            }
        }

        // Editor helpers
        public void SetArtworkId(int id) => artworkId = id;
        public int GetArtworkId() => artworkId;

        private void OnDrawGizmosSelected()
        {
            // Draw activation radius
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, activationDistance);

            // Draw info panel anchor
            if (infoPanelAnchor != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(infoPanelAnchor.position, 0.1f);
                Gizmos.DrawLine(transform.position, infoPanelAnchor.position);
            }
        }
    }
}
