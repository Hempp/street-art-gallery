using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace GalleryVR
{
    public enum HandType { Left, Right }

    public class VRHandController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private HandType handType;
        [SerializeField] private float pointerLength = 10f;
        [SerializeField] private LayerMask interactionMask;

        [Header("Visual")]
        [SerializeField] private LineRenderer pointerLine;
        [SerializeField] private GameObject pointerDot;
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color hoverColor = Color.cyan;
        [SerializeField] private Color activeColor = Color.green;

        [Header("Haptics")]
        [SerializeField] private float hoverHapticIntensity = 0.1f;
        [SerializeField] private float selectHapticIntensity = 0.3f;
        [SerializeField] private float hapticDuration = 0.05f;

        [Header("Hand Model")]
        [SerializeField] private Animator handAnimator;
        [SerializeField] private string gripAnimParam = "Grip";
        [SerializeField] private string triggerAnimParam = "Trigger";
        [SerializeField] private string pointAnimParam = "Point";

        // Input device
        private InputDevice controller;
        private InputDeviceCharacteristics controllerCharacteristics;

        // Interaction state
        private bool isPointing;
        private bool isGripping;
        private float gripValue;
        private float triggerValue;
        private ArtworkHotspot currentHotspot;
        private GameObject currentHoverObject;

        // UI Interaction
        private bool triggerPressed;
        private bool triggerReleased;

        private void Start()
        {
            // Set controller characteristics based on hand type
            controllerCharacteristics = handType == HandType.Left ?
                InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller :
                InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller;

            GetController();
            SetupPointer();
        }

        private void GetController()
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(controllerCharacteristics, devices);

            if (devices.Count > 0)
            {
                controller = devices[0];
            }
        }

        private void SetupPointer()
        {
            // Create pointer line if not assigned
            if (pointerLine == null)
            {
                var lineObj = new GameObject("PointerLine");
                lineObj.transform.SetParent(transform);
                lineObj.transform.localPosition = Vector3.zero;
                lineObj.transform.localRotation = Quaternion.identity;

                pointerLine = lineObj.AddComponent<LineRenderer>();
                pointerLine.startWidth = 0.005f;
                pointerLine.endWidth = 0.002f;
                pointerLine.positionCount = 2;

                var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                mat.color = defaultColor;
                pointerLine.material = mat;
            }

            // Create pointer dot if not assigned
            if (pointerDot == null)
            {
                pointerDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pointerDot.transform.localScale = Vector3.one * 0.02f;
                pointerDot.GetComponent<Collider>().enabled = false;

                var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                mat.color = defaultColor;
                pointerDot.GetComponent<Renderer>().material = mat;
            }

            pointerLine.enabled = false;
            pointerDot.SetActive(false);
        }

        private void Update()
        {
            if (!controller.isValid)
            {
                GetController();
                return;
            }

            ReadInput();
            UpdateHandAnimation();
            UpdatePointer();
            HandleInteraction();
        }

        private void ReadInput()
        {
            // Grip
            controller.TryGetFeatureValue(CommonUsages.grip, out gripValue);
            isGripping = gripValue > 0.8f;

            // Trigger
            controller.TryGetFeatureValue(CommonUsages.trigger, out triggerValue);

            bool triggerDown = triggerValue > 0.8f;
            triggerPressed = triggerDown && !triggerReleased;
            triggerReleased = !triggerDown;

            // Primary button for pointing mode toggle (optional)
            controller.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryPressed);

            // Thumbstick click to toggle pointer
            controller.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool stickClick);
        }

        private void UpdateHandAnimation()
        {
            if (handAnimator == null) return;

            handAnimator.SetFloat(gripAnimParam, gripValue);
            handAnimator.SetFloat(triggerAnimParam, triggerValue);
            handAnimator.SetBool(pointAnimParam, isPointing);
        }

        private void UpdatePointer()
        {
            // Show pointer when trigger is partially pressed (pointing gesture)
            isPointing = triggerValue > 0.1f && triggerValue < 0.8f;

            // Also show when holding trigger for selection
            bool showPointer = isPointing || triggerValue > 0.8f;

            pointerLine.enabled = showPointer;

            if (!showPointer)
            {
                pointerDot.SetActive(false);
                return;
            }

            // Raycast for pointer
            Ray ray = new Ray(transform.position, transform.forward);

            Vector3 endPoint;
            bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, pointerLength, interactionMask);

            if (hitSomething)
            {
                endPoint = hit.point;
                pointerDot.SetActive(true);
                pointerDot.transform.position = hit.point;
                pointerDot.transform.rotation = Quaternion.LookRotation(hit.normal);

                // Check for artwork hotspot
                var hotspot = hit.collider.GetComponent<ArtworkHotspot>();
                if (hotspot != null && hotspot != currentHotspot)
                {
                    // Exit previous hotspot
                    if (currentHotspot != null)
                    {
                        currentHotspot.OnPointerExit();
                    }

                    // Enter new hotspot
                    currentHotspot = hotspot;
                    currentHotspot.OnPointerEnter();

                    // Haptic feedback
                    SendHapticFeedback(hoverHapticIntensity, hapticDuration);

                    // Update pointer color
                    SetPointerColor(hoverColor);
                }
                else if (hotspot == null && currentHotspot != null)
                {
                    currentHotspot.OnPointerExit();
                    currentHotspot = null;
                    SetPointerColor(defaultColor);
                }

                currentHoverObject = hit.collider.gameObject;
            }
            else
            {
                endPoint = ray.origin + ray.direction * pointerLength;
                pointerDot.SetActive(false);

                if (currentHotspot != null)
                {
                    currentHotspot.OnPointerExit();
                    currentHotspot = null;
                    SetPointerColor(defaultColor);
                }

                currentHoverObject = null;
            }

            // Update line positions
            pointerLine.SetPosition(0, transform.position);
            pointerLine.SetPosition(1, endPoint);
        }

        private void HandleInteraction()
        {
            // Select on trigger press
            if (triggerPressed && currentHotspot != null)
            {
                currentHotspot.OnPointerClick();
                SendHapticFeedback(selectHapticIntensity, hapticDuration * 2);
                SetPointerColor(activeColor);
            }
        }

        private void SetPointerColor(Color color)
        {
            if (pointerLine != null)
            {
                pointerLine.material.color = color;
            }

            if (pointerDot != null)
            {
                pointerDot.GetComponent<Renderer>().material.color = color;
            }
        }

        private void SendHapticFeedback(float intensity, float duration)
        {
            if (controller.isValid)
            {
                controller.SendHapticImpulse(0, intensity, duration);
            }
        }

        // Public API
        public bool IsPointing() => isPointing;
        public bool IsGripping() => isGripping;
        public float GetGripValue() => gripValue;
        public float GetTriggerValue() => triggerValue;
        public GameObject GetHoveredObject() => currentHoverObject;
        public ArtworkHotspot GetHoveredHotspot() => currentHotspot;

        public void SetPointerActive(bool active)
        {
            pointerLine.enabled = active;
            pointerDot.SetActive(active);
        }

        public void TriggerHaptic(float intensity = 0.5f, float duration = 0.1f)
        {
            SendHapticFeedback(intensity, duration);
        }
    }
}
