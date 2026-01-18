using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

namespace GalleryVR.Multiplayer
{
    /// <summary>
    /// VR-friendly radial emote selection wheel.
    /// Triggered by controller button and selected via thumbstick/joystick direction.
    /// </summary>
    public class EmoteWheel : MonoBehaviour
    {
        public static EmoteWheel Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private Canvas wheelCanvas;
        [SerializeField] private RectTransform wheelContainer;
        [SerializeField] private Image centerIcon;
        [SerializeField] private TextMeshProUGUI selectedLabel;

        [Header("Wheel Settings")]
        [SerializeField] private float wheelRadius = 150f;
        [SerializeField] private float segmentSize = 80f;
        [SerializeField] private float deadzone = 0.3f;
        [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.3f, 0.9f);
        [SerializeField] private Color highlightColor = new Color(0.4f, 0.5f, 0.8f, 1f);
        [SerializeField] private Color selectedColor = new Color(0.3f, 0.7f, 0.4f, 1f);

        [Header("Emotes")]
        [SerializeField] private EmoteData[] emotes;

        [Header("Input")]
        [SerializeField] private bool useRightHand = true;
        [SerializeField] private float activationHoldTime = 0.2f;

        // Components
        private List<EmoteSegment> segments = new List<EmoteSegment>();
        private int selectedIndex = -1;
        private int hoveredIndex = -1;
        private bool isActive;
        private float activationTimer;

        // Input state
        private Vector2 inputDirection;
        private bool wasButtonPressed;

        // Events
        public event Action<string> OnEmoteSelected;

        [Serializable]
        public class EmoteData
        {
            public string id;
            public string displayName;
            public string emoji;
            public Sprite icon;
            public AudioClip sound;
        }

        private class EmoteSegment
        {
            public RectTransform transform;
            public Image background;
            public Image icon;
            public TextMeshProUGUI emojiText;
            public float angle;
            public int index;
        }

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

            InitializeDefaultEmotes();
        }

        private void Start()
        {
            CreateWheelUI();
            HideWheel();
        }

        private void InitializeDefaultEmotes()
        {
            if (emotes == null || emotes.Length == 0)
            {
                emotes = new EmoteData[]
                {
                    new EmoteData { id = "wave", displayName = "Wave", emoji = "👋" },
                    new EmoteData { id = "dance", displayName = "Dance", emoji = "💃" },
                    new EmoteData { id = "clap", displayName = "Clap", emoji = "👏" },
                    new EmoteData { id = "heart", displayName = "Love", emoji = "❤️" },
                    new EmoteData { id = "fire", displayName = "Fire", emoji = "🔥" },
                    new EmoteData { id = "laugh", displayName = "Laugh", emoji = "😂" },
                    new EmoteData { id = "think", displayName = "Think", emoji = "🤔" },
                    new EmoteData { id = "thumbsup", displayName = "Like", emoji = "👍" }
                };
            }
        }

        private void CreateWheelUI()
        {
            if (wheelCanvas == null)
            {
                // Create world-space canvas
                var canvasObj = new GameObject("EmoteWheelCanvas");
                canvasObj.transform.SetParent(transform);

                wheelCanvas = canvasObj.AddComponent<Canvas>();
                wheelCanvas.renderMode = RenderMode.WorldSpace;

                var rectTransform = wheelCanvas.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(400, 400);
                rectTransform.localScale = Vector3.one * 0.001f;

                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            if (wheelContainer == null)
            {
                var containerObj = new GameObject("WheelContainer");
                containerObj.transform.SetParent(wheelCanvas.transform);

                wheelContainer = containerObj.AddComponent<RectTransform>();
                wheelContainer.anchorMin = Vector2.zero;
                wheelContainer.anchorMax = Vector2.one;
                wheelContainer.offsetMin = Vector2.zero;
                wheelContainer.offsetMax = Vector2.zero;
            }

            // Create center background
            var centerBg = new GameObject("CenterBackground");
            centerBg.transform.SetParent(wheelContainer);

            var centerRect = centerBg.AddComponent<RectTransform>();
            centerRect.anchoredPosition = Vector2.zero;
            centerRect.sizeDelta = new Vector2(80, 80);

            var centerImage = centerBg.AddComponent<Image>();
            centerImage.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

            // Create center icon
            var iconObj = new GameObject("CenterIcon");
            iconObj.transform.SetParent(centerBg.transform);

            var iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(10, 10);
            iconRect.offsetMax = new Vector2(-10, -10);

            centerIcon = iconObj.AddComponent<Image>();
            centerIcon.color = Color.white;

            // Create selected label
            var labelObj = new GameObject("SelectedLabel");
            labelObj.transform.SetParent(wheelContainer);

            var labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchoredPosition = new Vector2(0, -wheelRadius - 50);
            labelRect.sizeDelta = new Vector2(200, 40);

            selectedLabel = labelObj.AddComponent<TextMeshProUGUI>();
            selectedLabel.text = "";
            selectedLabel.fontSize = 24;
            selectedLabel.alignment = TextAlignmentOptions.Center;
            selectedLabel.color = Color.white;

            // Create emote segments
            CreateEmoteSegments();
        }

        private void CreateEmoteSegments()
        {
            segments.Clear();

            int count = emotes.Length;
            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float angle = -90f + (i * angleStep); // Start from top
                CreateSegment(emotes[i], i, angle);
            }
        }

        private void CreateSegment(EmoteData emote, int index, float angle)
        {
            var segmentObj = new GameObject($"Segment_{emote.id}");
            segmentObj.transform.SetParent(wheelContainer);

            var segmentRect = segmentObj.AddComponent<RectTransform>();

            // Position on circle
            float rad = angle * Mathf.Deg2Rad;
            Vector2 pos = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * wheelRadius;
            segmentRect.anchoredPosition = pos;
            segmentRect.sizeDelta = new Vector2(segmentSize, segmentSize);

            // Background
            var bgImage = segmentObj.AddComponent<Image>();
            bgImage.color = normalColor;

            // Make it circular-ish (rounded square effect)
            // In actual implementation, use a circular sprite

            // Emoji text
            var emojiObj = new GameObject("Emoji");
            emojiObj.transform.SetParent(segmentObj.transform);

            var emojiRect = emojiObj.AddComponent<RectTransform>();
            emojiRect.anchorMin = Vector2.zero;
            emojiRect.anchorMax = Vector2.one;
            emojiRect.offsetMin = new Vector2(5, 15);
            emojiRect.offsetMax = new Vector2(-5, -5);

            var emojiText = emojiObj.AddComponent<TextMeshProUGUI>();
            emojiText.text = emote.emoji;
            emojiText.fontSize = 36;
            emojiText.alignment = TextAlignmentOptions.Center;

            // Icon (if sprite provided)
            Image iconImage = null;
            if (emote.icon != null)
            {
                var iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(segmentObj.transform);

                var iconRect = iconObj.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.1f, 0.3f);
                iconRect.anchorMax = new Vector2(0.9f, 0.9f);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;

                iconImage = iconObj.AddComponent<Image>();
                iconImage.sprite = emote.icon;
                iconImage.preserveAspect = true;

                emojiText.gameObject.SetActive(false);
            }

            // Label under segment
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(segmentObj.transform);

            var labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchoredPosition = new Vector2(0, -segmentSize / 2 - 15);
            labelRect.sizeDelta = new Vector2(segmentSize + 20, 20);

            var label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = emote.displayName;
            label.fontSize = 12;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;

            var segment = new EmoteSegment
            {
                transform = segmentRect,
                background = bgImage,
                icon = iconImage,
                emojiText = emojiText,
                angle = angle,
                index = index
            };

            segments.Add(segment);
        }

        private void Update()
        {
            if (!isActive) return;

            // Get input direction from controller
            UpdateInputDirection();

            // Update wheel position to follow hand/view
            UpdateWheelPosition();

            // Determine hovered segment
            UpdateHoveredSegment();

            // Update visuals
            UpdateSegmentVisuals();
        }

        private void UpdateInputDirection()
        {
            // Try to get thumbstick input from XR controller
            var handNode = useRightHand ? UnityEngine.XR.XRNode.RightHand : UnityEngine.XR.XRNode.LeftHand;

            if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(handNode).TryGetFeatureValue(
                UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 axis))
            {
                inputDirection = axis;
            }
            else
            {
                // Fallback to keyboard/mouse for testing
                inputDirection = new Vector2(
                    Input.GetAxis("Horizontal"),
                    Input.GetAxis("Vertical")
                );
            }

            // Check for selection trigger
            bool triggerPressed = false;

            if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(handNode).TryGetFeatureValue(
                UnityEngine.XR.CommonUsages.triggerButton, out bool trigger))
            {
                triggerPressed = trigger;
            }
            else
            {
                triggerPressed = Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
            }

            if (triggerPressed && hoveredIndex >= 0)
            {
                SelectEmote(hoveredIndex);
            }
        }

        private void UpdateWheelPosition()
        {
            if (wheelCanvas == null || Camera.main == null) return;

            // Position wheel in front of the player, slightly to the side of active hand
            Transform cam = Camera.main.transform;
            Vector3 forward = cam.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 right = cam.right * (useRightHand ? 0.3f : -0.3f);

            Vector3 targetPos = cam.position + forward * 0.5f + right;
            targetPos.y = cam.position.y - 0.1f;

            wheelCanvas.transform.position = Vector3.Lerp(
                wheelCanvas.transform.position,
                targetPos,
                Time.deltaTime * 15f
            );

            // Face the camera
            wheelCanvas.transform.LookAt(cam);
            wheelCanvas.transform.Rotate(0, 180, 0);
        }

        private void UpdateHoveredSegment()
        {
            if (inputDirection.magnitude < deadzone)
            {
                hoveredIndex = -1;
                selectedLabel.text = "";
                return;
            }

            // Calculate angle from input
            float inputAngle = Mathf.Atan2(inputDirection.y, inputDirection.x) * Mathf.Rad2Deg;

            // Find closest segment
            float minDiff = float.MaxValue;
            int closest = -1;

            for (int i = 0; i < segments.Count; i++)
            {
                float diff = Mathf.Abs(Mathf.DeltaAngle(inputAngle, segments[i].angle));
                if (diff < minDiff)
                {
                    minDiff = diff;
                    closest = i;
                }
            }

            // Only select if within segment range
            float segmentAngle = 360f / segments.Count;
            if (minDiff < segmentAngle / 2)
            {
                hoveredIndex = closest;
                if (hoveredIndex >= 0 && hoveredIndex < emotes.Length)
                {
                    selectedLabel.text = emotes[hoveredIndex].displayName;
                }
            }
            else
            {
                hoveredIndex = -1;
                selectedLabel.text = "";
            }
        }

        private void UpdateSegmentVisuals()
        {
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                Color targetColor;

                if (i == selectedIndex)
                {
                    targetColor = selectedColor;
                }
                else if (i == hoveredIndex)
                {
                    targetColor = highlightColor;
                    // Scale up hovered segment
                    segment.transform.localScale = Vector3.Lerp(
                        segment.transform.localScale,
                        Vector3.one * 1.2f,
                        Time.deltaTime * 10f
                    );
                }
                else
                {
                    targetColor = normalColor;
                    segment.transform.localScale = Vector3.Lerp(
                        segment.transform.localScale,
                        Vector3.one,
                        Time.deltaTime * 10f
                    );
                }

                segment.background.color = Color.Lerp(
                    segment.background.color,
                    targetColor,
                    Time.deltaTime * 10f
                );
            }

            // Update center icon
            if (hoveredIndex >= 0 && hoveredIndex < emotes.Length)
            {
                if (emotes[hoveredIndex].icon != null)
                {
                    centerIcon.sprite = emotes[hoveredIndex].icon;
                    centerIcon.color = Color.white;
                }
                else
                {
                    centerIcon.color = Color.clear;
                }
            }
        }

        private void SelectEmote(int index)
        {
            if (index < 0 || index >= emotes.Length) return;

            selectedIndex = index;
            var emote = emotes[index];

            Debug.Log($"[EmoteWheel] Selected emote: {emote.displayName}");

            // Play sound if available
            if (emote.sound != null)
            {
                AudioSource.PlayClipAtPoint(emote.sound, transform.position);
            }

            // Trigger emote
            OnEmoteSelected?.Invoke(emote.id);

            // Send to network
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.SendEmote(emote.id);
            }

            // Haptic feedback
            TriggerHapticFeedback();

            // Hide wheel after selection
            HideWheel();
        }

        private void TriggerHapticFeedback()
        {
            var handNode = useRightHand ? UnityEngine.XR.XRNode.RightHand : UnityEngine.XR.XRNode.LeftHand;

            if (UnityEngine.XR.InputDevices.GetDeviceAtXRNode(handNode).TryGetFeatureValue(
                UnityEngine.XR.CommonUsages.isTracked, out bool tracked) && tracked)
            {
                UnityEngine.XR.InputDevices.GetDeviceAtXRNode(handNode).SendHapticImpulse(0, 0.5f, 0.1f);
            }
        }

        // Public API
        public void ShowWheel()
        {
            isActive = true;
            selectedIndex = -1;
            hoveredIndex = -1;

            if (wheelCanvas != null)
            {
                wheelCanvas.gameObject.SetActive(true);
            }

            // Reset segment scales
            foreach (var segment in segments)
            {
                segment.transform.localScale = Vector3.one;
                segment.background.color = normalColor;
            }
        }

        public void HideWheel()
        {
            isActive = false;

            if (wheelCanvas != null)
            {
                wheelCanvas.gameObject.SetActive(false);
            }
        }

        public void ToggleWheel()
        {
            if (isActive)
                HideWheel();
            else
                ShowWheel();
        }

        public bool IsActive => isActive;

        // Quick emote shortcuts
        public void QuickEmote(string emoteId)
        {
            for (int i = 0; i < emotes.Length; i++)
            {
                if (emotes[i].id == emoteId)
                {
                    SelectEmote(i);
                    return;
                }
            }
        }

        // Add custom emotes
        public void AddEmote(EmoteData emote)
        {
            var newEmotes = new EmoteData[emotes.Length + 1];
            emotes.CopyTo(newEmotes, 0);
            newEmotes[emotes.Length] = emote;
            emotes = newEmotes;

            // Rebuild wheel
            foreach (var segment in segments)
            {
                Destroy(segment.transform.gameObject);
            }
            CreateEmoteSegments();
        }
    }
}
