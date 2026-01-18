using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace GalleryVR.Multiplayer
{
    /// <summary>
    /// Represents a player in the social VR gallery.
    /// Handles avatar display, nametag, emotes, and voice indicators.
    /// </summary>
    public class NetworkPlayer : MonoBehaviour
    {
        [Header("Player Info")]
        [SerializeField] private string playerId;
        [SerializeField] private string username;
        [SerializeField] private bool isLocalPlayer;

        [Header("Avatar")]
        [SerializeField] private GameObject avatarRoot;
        [SerializeField] private Animator avatarAnimator;
        [SerializeField] private SkinnedMeshRenderer avatarRenderer;
        [SerializeField] private int avatarId;

        [Header("Nametag")]
        [SerializeField] private Canvas nametagCanvas;
        [SerializeField] private TextMeshProUGUI usernameText;
        [SerializeField] private Image voiceIndicator;
        [SerializeField] private float nametagHeight = 2.2f;
        [SerializeField] private float nametagScale = 0.005f;
        [SerializeField] private bool faceCamera = true;

        [Header("Chat Bubble")]
        [SerializeField] private Canvas chatBubbleCanvas;
        [SerializeField] private TextMeshProUGUI chatText;
        [SerializeField] private Image chatBackground;
        [SerializeField] private float chatDisplayDuration = 5f;

        [Header("Emote System")]
        [SerializeField] private Transform emoteAnchor;
        [SerializeField] private ParticleSystem emoteParticles;

        [Header("Voice Chat")]
        [SerializeField] private AudioSource voiceSource;
        [SerializeField] private float voiceActivityThreshold = 0.01f;
        [SerializeField] private Color voiceActiveColor = Color.green;
        [SerializeField] private Color voiceInactiveColor = Color.gray;

        [Header("Movement")]
        [SerializeField] private float interpolationSpeed = 10f;
        [SerializeField] private float rotationSpeed = 10f;

        // State
        private PlayerData playerData;
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private Coroutine chatBubbleCoroutine;
        private bool isSpeaking;

        // Animation states
        private static readonly int WalkParam = Animator.StringToHash("Walking");
        private static readonly int WaveParam = Animator.StringToHash("Wave");
        private static readonly int DanceParam = Animator.StringToHash("Dance");
        private static readonly int ClapParam = Animator.StringToHash("Clap");
        private static readonly int EmoteParam = Animator.StringToHash("Emote");

        public string PlayerId => playerId;
        public string Username => username;
        public bool IsLocal => isLocalPlayer;
        public PlayerData Data => playerData;

        private void Awake()
        {
            CreateDefaultComponents();
        }

        private void CreateDefaultComponents()
        {
            // Create nametag if not assigned
            if (nametagCanvas == null)
            {
                CreateNametag();
            }

            // Create chat bubble if not assigned
            if (chatBubbleCanvas == null)
            {
                CreateChatBubble();
            }

            // Create emote anchor if not assigned
            if (emoteAnchor == null)
            {
                var anchor = new GameObject("EmoteAnchor");
                anchor.transform.SetParent(transform);
                anchor.transform.localPosition = new Vector3(0, 2.5f, 0);
                emoteAnchor = anchor.transform;
            }
        }

        private void CreateNametag()
        {
            // Create canvas
            var canvasObj = new GameObject("Nametag");
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = new Vector3(0, nametagHeight, 0);
            canvasObj.transform.localScale = Vector3.one * nametagScale;

            nametagCanvas = canvasObj.AddComponent<Canvas>();
            nametagCanvas.renderMode = RenderMode.WorldSpace;

            var rectTransform = nametagCanvas.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(400, 100);

            // Add canvas scaler
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100;

            // Create background panel
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform);
            var bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.7f);

            // Add rounded corners (via sprite or leave flat)

            // Create username text
            var textObj = new GameObject("UsernameText");
            textObj.transform.SetParent(canvasObj.transform);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.1f, 0.2f);
            textRect.anchorMax = new Vector2(0.9f, 0.8f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            usernameText = textObj.AddComponent<TextMeshProUGUI>();
            usernameText.text = "Username";
            usernameText.fontSize = 36;
            usernameText.alignment = TextAlignmentOptions.Center;
            usernameText.color = Color.white;
            usernameText.fontStyle = FontStyles.Bold;

            // Create voice indicator
            var voiceObj = new GameObject("VoiceIndicator");
            voiceObj.transform.SetParent(canvasObj.transform);
            var voiceRect = voiceObj.AddComponent<RectTransform>();
            voiceRect.anchorMin = new Vector2(0.9f, 0.3f);
            voiceRect.anchorMax = new Vector2(0.95f, 0.7f);
            voiceRect.offsetMin = Vector2.zero;
            voiceRect.offsetMax = Vector2.zero;

            voiceIndicator = voiceObj.AddComponent<Image>();
            voiceIndicator.color = voiceInactiveColor;
            voiceIndicator.gameObject.SetActive(false); // Hidden until voice chat active
        }

        private void CreateChatBubble()
        {
            // Create chat bubble canvas
            var canvasObj = new GameObject("ChatBubble");
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = new Vector3(0, nametagHeight + 0.3f, 0);
            canvasObj.transform.localScale = Vector3.one * nametagScale;

            chatBubbleCanvas = canvasObj.AddComponent<Canvas>();
            chatBubbleCanvas.renderMode = RenderMode.WorldSpace;

            var rectTransform = chatBubbleCanvas.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(500, 150);

            // Background
            var bgObj = new GameObject("BubbleBackground");
            bgObj.transform.SetParent(canvasObj.transform);
            var bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            chatBackground = bgObj.AddComponent<Image>();
            chatBackground.color = new Color(1f, 1f, 1f, 0.95f);

            // Chat text
            var textObj = new GameObject("ChatText");
            textObj.transform.SetParent(canvasObj.transform);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.05f, 0.1f);
            textRect.anchorMax = new Vector2(0.95f, 0.9f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            chatText = textObj.AddComponent<TextMeshProUGUI>();
            chatText.text = "";
            chatText.fontSize = 28;
            chatText.alignment = TextAlignmentOptions.Center;
            chatText.color = Color.black;

            chatBubbleCanvas.gameObject.SetActive(false);
        }

        public void Initialize(PlayerData data, bool local)
        {
            playerData = data;
            playerId = data.userId;
            username = data.username;
            isLocalPlayer = local;

            // Update visuals
            UpdateData(data);

            // Hide nametag for local player in VR
            if (isLocalPlayer && nametagCanvas != null)
            {
                nametagCanvas.gameObject.SetActive(false);
            }

            // Setup voice for local player
            if (isLocalPlayer && voiceSource == null)
            {
                voiceSource = gameObject.AddComponent<AudioSource>();
                voiceSource.spatialBlend = 0; // 2D for local
            }

            // Setup avatar if not local (local uses VR rig)
            if (!isLocalPlayer)
            {
                CreateDefaultAvatar();
            }

            targetPosition = transform.position;
            targetRotation = transform.rotation;
        }

        public void UpdateData(PlayerData data)
        {
            playerData = data;
            username = data.username;
            avatarId = data.avatarId;

            // Update nametag
            if (usernameText != null)
            {
                usernameText.text = data.username;
            }

            // Update avatar appearance
            UpdateAvatarAppearance(data);
        }

        private void CreateDefaultAvatar()
        {
            if (avatarRoot != null) return;

            // Create simple placeholder avatar
            avatarRoot = new GameObject("Avatar");
            avatarRoot.transform.SetParent(transform);
            avatarRoot.transform.localPosition = Vector3.zero;

            // Body (capsule)
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(avatarRoot.transform);
            body.transform.localPosition = new Vector3(0, 1f, 0);
            body.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            Destroy(body.GetComponent<Collider>());

            // Head (sphere)
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(avatarRoot.transform);
            head.transform.localPosition = new Vector3(0, 1.7f, 0);
            head.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            Destroy(head.GetComponent<Collider>());

            // Apply color
            if (playerData != null && !string.IsNullOrEmpty(playerData.avatarColor))
            {
                if (ColorUtility.TryParseHtmlString("#" + playerData.avatarColor, out Color color))
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.color = color;
                    body.GetComponent<Renderer>().material = mat;
                    head.GetComponent<Renderer>().material = mat;
                }
            }
        }

        private void UpdateAvatarAppearance(PlayerData data)
        {
            if (avatarRenderer == null) return;

            // Update material color
            if (ColorUtility.TryParseHtmlString("#" + data.avatarColor, out Color color))
            {
                avatarRenderer.material.color = color;
            }

            // Update outfit/mesh variant
            // This would swap meshes or enable/disable outfit parts
        }

        private void Update()
        {
            // Face camera for nametag
            if (faceCamera && nametagCanvas != null && Camera.main != null)
            {
                nametagCanvas.transform.LookAt(Camera.main.transform);
                nametagCanvas.transform.Rotate(0, 180, 0);
            }

            // Same for chat bubble
            if (chatBubbleCanvas != null && chatBubbleCanvas.gameObject.activeSelf && Camera.main != null)
            {
                chatBubbleCanvas.transform.LookAt(Camera.main.transform);
                chatBubbleCanvas.transform.Rotate(0, 180, 0);
            }

            // Interpolate position/rotation for remote players
            if (!isLocalPlayer)
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * interpolationSpeed);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

                // Update walk animation based on movement
                if (avatarAnimator != null)
                {
                    float velocity = (targetPosition - transform.position).magnitude;
                    avatarAnimator.SetBool(WalkParam, velocity > 0.1f);
                }
            }

            // Update voice indicator
            UpdateVoiceIndicator();
        }

        // Remote position updates
        public void SetTargetPosition(Vector3 position, Quaternion rotation)
        {
            targetPosition = position;
            targetRotation = rotation;
        }

        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            transform.position = position;
            transform.rotation = rotation;
            targetPosition = position;
            targetRotation = rotation;
        }

        // Chat system
        public void ShowChatBubble(string message)
        {
            if (chatBubbleCoroutine != null)
            {
                StopCoroutine(chatBubbleCoroutine);
            }

            chatBubbleCoroutine = StartCoroutine(DisplayChatBubble(message));
        }

        private IEnumerator DisplayChatBubble(string message)
        {
            if (chatText != null)
            {
                chatText.text = message;
            }

            if (chatBubbleCanvas != null)
            {
                chatBubbleCanvas.gameObject.SetActive(true);
            }

            yield return new WaitForSeconds(chatDisplayDuration);

            if (chatBubbleCanvas != null)
            {
                chatBubbleCanvas.gameObject.SetActive(false);
            }
        }

        // Emote system
        public void PlayEmote(string emoteId)
        {
            StartCoroutine(PlayEmoteSequence(emoteId));
        }

        private IEnumerator PlayEmoteSequence(string emoteId)
        {
            // Trigger animation
            if (avatarAnimator != null)
            {
                switch (emoteId.ToLower())
                {
                    case "wave":
                        avatarAnimator.SetTrigger(WaveParam);
                        break;
                    case "dance":
                        avatarAnimator.SetTrigger(DanceParam);
                        break;
                    case "clap":
                        avatarAnimator.SetTrigger(ClapParam);
                        break;
                    default:
                        avatarAnimator.SetTrigger(EmoteParam);
                        break;
                }
            }

            // Show emote particle/icon
            if (emoteParticles != null)
            {
                emoteParticles.Play();
            }

            // Show emote icon above head
            ShowEmoteIcon(emoteId);

            yield return new WaitForSeconds(2f);
        }

        private void ShowEmoteIcon(string emoteId)
        {
            // Create temporary emote icon
            var iconObj = new GameObject("EmoteIcon");
            iconObj.transform.SetParent(emoteAnchor);
            iconObj.transform.localPosition = Vector3.zero;

            var canvas = iconObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.transform.localScale = Vector3.one * 0.01f;

            var text = new GameObject("Text").AddComponent<TextMeshProUGUI>();
            text.transform.SetParent(canvas.transform);
            text.rectTransform.sizeDelta = new Vector2(100, 100);
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 48;

            // Set emoji based on emote
            switch (emoteId.ToLower())
            {
                case "wave": text.text = "👋"; break;
                case "dance": text.text = "💃"; break;
                case "clap": text.text = "👏"; break;
                case "heart": text.text = "❤️"; break;
                case "fire": text.text = "🔥"; break;
                case "laugh": text.text = "😂"; break;
                case "think": text.text = "🤔"; break;
                default: text.text = "😊"; break;
            }

            // Animate and destroy
            StartCoroutine(AnimateEmoteIcon(iconObj));
        }

        private IEnumerator AnimateEmoteIcon(GameObject icon)
        {
            float duration = 2f;
            float elapsed = 0;
            Vector3 startPos = icon.transform.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Float up and fade
                icon.transform.position = startPos + Vector3.up * t * 0.5f;

                // Scale pulse
                float scale = 1f + Mathf.Sin(t * Mathf.PI * 4) * 0.1f;
                icon.transform.localScale = Vector3.one * 0.01f * scale;

                yield return null;
            }

            Destroy(icon);
        }

        // Voice chat
        public void SetVoiceActive(bool active)
        {
            isSpeaking = active;
        }

        private void UpdateVoiceIndicator()
        {
            if (voiceIndicator == null) return;

            // Check if voice is active (local player - check microphone, remote - network data)
            if (isLocalPlayer && voiceSource != null)
            {
                // Would check microphone level here
            }

            voiceIndicator.color = isSpeaking ? voiceActiveColor : voiceInactiveColor;
        }

        public void EnableVoiceIndicator(bool enable)
        {
            if (voiceIndicator != null)
            {
                voiceIndicator.gameObject.SetActive(enable);
            }
        }

        // Cleanup
        private void OnDestroy()
        {
            if (chatBubbleCoroutine != null)
            {
                StopCoroutine(chatBubbleCoroutine);
            }
        }
    }
}
