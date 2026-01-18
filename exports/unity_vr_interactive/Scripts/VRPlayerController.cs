using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections.Generic;

namespace GalleryVR
{
    public class VRPlayerController : MonoBehaviour
    {
        public static VRPlayerController Instance { get; private set; }

        [Header("Movement Settings")]
        [SerializeField] private float smoothMoveSpeed = 2f;
        [SerializeField] private float snapTurnAngle = 45f;
        [SerializeField] private float smoothTurnSpeed = 90f;
        [SerializeField] private bool useSnapTurn = true;
        [SerializeField] private bool useSmoothLocomotion = false;

        [Header("Teleportation")]
        [SerializeField] private LayerMask teleportMask;
        [SerializeField] private float maxTeleportDistance = 15f;
        [SerializeField] private GameObject teleportReticlePrefab;
        [SerializeField] private LineRenderer teleportLine;
        [SerializeField] private Color validTeleportColor = Color.green;
        [SerializeField] private Color invalidTeleportColor = Color.red;

        [Header("References")]
        [SerializeField] private Transform xrOrigin;
        [SerializeField] private Transform cameraOffset;
        [SerializeField] private Transform mainCamera;
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightHand;

        [Header("Comfort Settings")]
        [SerializeField] private bool enableVignette = true;
        [SerializeField] private float vignetteIntensity = 0.5f;
        [SerializeField] private Material vignetteMaterial;

        // Input
        private InputDevice leftController;
        private InputDevice rightController;
        private bool isTeleporting;
        private Vector3 teleportTarget;
        private bool validTeleportTarget;
        private GameObject teleportReticle;

        // Snap turn cooldown
        private float snapTurnCooldown = 0.3f;
        private float lastSnapTurnTime;

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

            InitializeTeleport();
        }

        private void Start()
        {
            // Find XR components if not assigned
            if (xrOrigin == null)
                xrOrigin = transform;

            if (mainCamera == null)
                mainCamera = Camera.main?.transform;

            GetControllers();
        }

        private void InitializeTeleport()
        {
            if (teleportReticlePrefab != null)
            {
                teleportReticle = Instantiate(teleportReticlePrefab);
                teleportReticle.SetActive(false);
            }
            else
            {
                // Create default reticle
                teleportReticle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                teleportReticle.transform.localScale = new Vector3(0.5f, 0.01f, 0.5f);
                teleportReticle.GetComponent<Collider>().enabled = false;

                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = validTeleportColor;
                teleportReticle.GetComponent<Renderer>().material = mat;
                teleportReticle.SetActive(false);
            }

            // Create teleport line if not assigned
            if (teleportLine == null)
            {
                var lineObj = new GameObject("TeleportLine");
                lineObj.transform.SetParent(transform);
                teleportLine = lineObj.AddComponent<LineRenderer>();
                teleportLine.startWidth = 0.02f;
                teleportLine.endWidth = 0.02f;
                teleportLine.positionCount = 20;
                teleportLine.enabled = false;

                var lineMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                lineMat.color = validTeleportColor;
                teleportLine.material = lineMat;
            }
        }

        private void GetControllers()
        {
            var leftDevices = new List<InputDevice>();
            var rightDevices = new List<InputDevice>();

            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller,
                leftDevices);
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
                rightDevices);

            if (leftDevices.Count > 0) leftController = leftDevices[0];
            if (rightDevices.Count > 0) rightController = rightDevices[0];
        }

        private void Update()
        {
            // Refresh controllers if disconnected
            if (!leftController.isValid || !rightController.isValid)
            {
                GetControllers();
            }

            HandleTeleportation();
            HandleSnapTurn();

            if (useSmoothLocomotion)
            {
                HandleSmoothLocomotion();
            }
        }

        private void HandleTeleportation()
        {
            // Use left thumbstick forward for teleport (common convention)
            leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 leftStick);

            // Start teleport when pushing stick forward
            if (leftStick.y > 0.7f && !isTeleporting)
            {
                isTeleporting = true;
                teleportLine.enabled = true;
            }

            // Update teleport arc while holding
            if (isTeleporting)
            {
                UpdateTeleportArc();

                // Release to teleport
                if (leftStick.y < 0.3f)
                {
                    if (validTeleportTarget)
                    {
                        ExecuteTeleport();
                    }

                    isTeleporting = false;
                    teleportLine.enabled = false;
                    teleportReticle.SetActive(false);
                }
            }
        }

        private void UpdateTeleportArc()
        {
            if (leftHand == null) return;

            Vector3 startPos = leftHand.position;
            Vector3 forward = leftHand.forward;

            // Calculate arc points
            Vector3[] arcPoints = new Vector3[20];
            Vector3 velocity = forward * 8f;
            Vector3 gravity = Physics.gravity;
            float timeStep = 0.1f;

            validTeleportTarget = false;

            for (int i = 0; i < arcPoints.Length; i++)
            {
                float t = i * timeStep;
                arcPoints[i] = startPos + velocity * t + 0.5f * gravity * t * t;

                // Check for collision
                if (i > 0)
                {
                    if (Physics.Raycast(arcPoints[i-1], arcPoints[i] - arcPoints[i-1],
                        out RaycastHit hit, Vector3.Distance(arcPoints[i-1], arcPoints[i]), teleportMask))
                    {
                        // Check if valid teleport surface (floor)
                        if (Vector3.Dot(hit.normal, Vector3.up) > 0.7f)
                        {
                            teleportTarget = hit.point;
                            validTeleportTarget = true;

                            teleportReticle.SetActive(true);
                            teleportReticle.transform.position = hit.point + Vector3.up * 0.01f;
                            teleportReticle.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

                            // Truncate arc at hit point
                            arcPoints[i] = hit.point;
                            teleportLine.positionCount = i + 1;
                        }
                        break;
                    }
                }
            }

            // Update line renderer
            teleportLine.SetPositions(arcPoints);

            // Update colors
            Color lineColor = validTeleportTarget ? validTeleportColor : invalidTeleportColor;
            teleportLine.material.color = lineColor;

            if (teleportReticle.TryGetComponent<Renderer>(out var renderer))
            {
                renderer.material.color = lineColor;
            }
        }

        private void ExecuteTeleport()
        {
            if (!validTeleportTarget) return;

            // Calculate offset from camera to origin
            Vector3 cameraOffset = mainCamera.position - xrOrigin.position;
            cameraOffset.y = 0; // Keep vertical position

            // Move origin so camera ends up at target
            xrOrigin.position = teleportTarget - cameraOffset;

            // Trigger vignette effect
            if (enableVignette)
            {
                StartCoroutine(TeleportVignetteEffect());
            }

            // Notify other systems
            OnTeleport?.Invoke(teleportTarget);
        }

        public System.Action<Vector3> OnTeleport;

        private System.Collections.IEnumerator TeleportVignetteEffect()
        {
            // Quick fade to black and back
            float duration = 0.15f;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                // Would apply vignette shader here
                yield return null;
            }
        }

        private void HandleSnapTurn()
        {
            if (!useSnapTurn) return;
            if (Time.time - lastSnapTurnTime < snapTurnCooldown) return;

            rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 rightStick);

            if (Mathf.Abs(rightStick.x) > 0.7f)
            {
                float turnDirection = Mathf.Sign(rightStick.x);
                xrOrigin.RotateAround(mainCamera.position, Vector3.up, snapTurnAngle * turnDirection);
                lastSnapTurnTime = Time.time;
            }
        }

        private void HandleSmoothLocomotion()
        {
            leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 leftStick);

            if (leftStick.magnitude < 0.1f) return;

            // Get movement direction relative to head
            Vector3 forward = mainCamera.forward;
            Vector3 right = mainCamera.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDirection = forward * leftStick.y + right * leftStick.x;
            xrOrigin.position += moveDirection * smoothMoveSpeed * Time.deltaTime;

            // Apply vignette during movement
            if (enableVignette && moveDirection.magnitude > 0.1f)
            {
                // Would apply comfort vignette here
            }
        }

        // Public methods for external control
        public void SetLocomotionMode(bool smooth)
        {
            useSmoothLocomotion = smooth;
        }

        public void SetSnapTurn(bool enabled)
        {
            useSnapTurn = enabled;
        }

        public void SetComfortVignette(bool enabled)
        {
            enableVignette = enabled;
        }

        public void TeleportTo(Vector3 position)
        {
            teleportTarget = position;
            validTeleportTarget = true;
            ExecuteTeleport();
        }
    }
}
