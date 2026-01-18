using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

namespace GalleryVR.Multiplayer
{
    /// <summary>
    /// Avatar customization system for the social VR gallery.
    /// Allows players to customize appearance, outfits, and accessories.
    /// </summary>
    public class AvatarCustomization : MonoBehaviour
    {
        public static AvatarCustomization Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private Canvas customizationCanvas;
        [SerializeField] private Transform avatarPreviewPosition;
        [SerializeField] private Camera previewCamera;

        [Header("Category Buttons")]
        [SerializeField] private Button bodyButton;
        [SerializeField] private Button outfitButton;
        [SerializeField] private Button accessoriesButton;
        [SerializeField] private Button colorsButton;

        [Header("Selection Panels")]
        [SerializeField] private GameObject bodyPanel;
        [SerializeField] private GameObject outfitPanel;
        [SerializeField] private GameObject accessoriesPanel;
        [SerializeField] private GameObject colorsPanel;

        [Header("Avatar Prefabs")]
        [SerializeField] private GameObject[] avatarBasePrefabs;
        [SerializeField] private GameObject[] outfitPrefabs;
        [SerializeField] private GameObject[] accessoryPrefabs;

        [Header("Color Options")]
        [SerializeField] private Color[] skinColors;
        [SerializeField] private Color[] hairColors;
        [SerializeField] private Color[] outfitColors;

        [Header("Preview")]
        [SerializeField] private float previewRotationSpeed = 30f;
        [SerializeField] private bool autoRotatePreview = true;

        // Current customization state
        private AvatarConfig currentConfig;
        private GameObject previewAvatar;
        private CustomizationCategory activeCategory;

        // Events
        public event Action<AvatarConfig> OnCustomizationChanged;
        public event Action<AvatarConfig> OnCustomizationConfirmed;

        private enum CustomizationCategory
        {
            Body,
            Outfit,
            Accessories,
            Colors
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

            InitializeDefaultColors();
            LoadSavedConfig();
        }

        private void Start()
        {
            SetupUI();
            CreateDefaultPreview();
        }

        private void InitializeDefaultColors()
        {
            if (skinColors == null || skinColors.Length == 0)
            {
                skinColors = new Color[]
                {
                    new Color(1.0f, 0.87f, 0.77f),    // Light
                    new Color(0.96f, 0.80f, 0.69f),   // Fair
                    new Color(0.87f, 0.72f, 0.53f),   // Medium
                    new Color(0.76f, 0.57f, 0.42f),   // Tan
                    new Color(0.55f, 0.38f, 0.26f),   // Brown
                    new Color(0.36f, 0.24f, 0.15f),   // Dark
                    // Fantasy colors
                    new Color(0.4f, 0.8f, 0.4f),      // Green
                    new Color(0.4f, 0.4f, 0.9f),      // Blue
                    new Color(0.9f, 0.4f, 0.9f),      // Purple
                };
            }

            if (hairColors == null || hairColors.Length == 0)
            {
                hairColors = new Color[]
                {
                    new Color(0.1f, 0.05f, 0.0f),     // Black
                    new Color(0.35f, 0.23f, 0.14f),   // Brown
                    new Color(0.55f, 0.35f, 0.2f),    // Auburn
                    new Color(0.85f, 0.65f, 0.35f),   // Blonde
                    new Color(0.9f, 0.9f, 0.9f),      // White
                    new Color(0.6f, 0.5f, 0.5f),      // Gray
                    // Fantasy colors
                    new Color(1.0f, 0.2f, 0.2f),      // Red
                    new Color(0.2f, 0.6f, 1.0f),      // Blue
                    new Color(0.8f, 0.2f, 0.8f),      // Purple
                    new Color(0.2f, 1.0f, 0.5f),      // Green
                    new Color(1.0f, 0.5f, 0.8f),      // Pink
                };
            }

            if (outfitColors == null || outfitColors.Length == 0)
            {
                outfitColors = new Color[]
                {
                    Color.white,
                    Color.black,
                    Color.red,
                    Color.blue,
                    Color.green,
                    Color.yellow,
                    new Color(1f, 0.5f, 0f),          // Orange
                    new Color(0.5f, 0f, 0.5f),        // Purple
                    new Color(0f, 0.5f, 0.5f),        // Teal
                    new Color(1f, 0.75f, 0.8f),       // Pink
                };
            }
        }

        private void LoadSavedConfig()
        {
            currentConfig = new AvatarConfig();

            // Load from PlayerPrefs
            currentConfig.bodyType = PlayerPrefs.GetInt("Avatar_BodyType", 0);
            currentConfig.outfitId = PlayerPrefs.GetInt("Avatar_OutfitId", 0);
            currentConfig.accessoryMask = PlayerPrefs.GetInt("Avatar_Accessories", 0);

            string skinHex = PlayerPrefs.GetString("Avatar_SkinColor", "");
            if (!string.IsNullOrEmpty(skinHex) && ColorUtility.TryParseHtmlString("#" + skinHex, out Color skinCol))
            {
                currentConfig.skinColor = skinCol;
            }
            else
            {
                currentConfig.skinColor = skinColors[0];
            }

            string hairHex = PlayerPrefs.GetString("Avatar_HairColor", "");
            if (!string.IsNullOrEmpty(hairHex) && ColorUtility.TryParseHtmlString("#" + hairHex, out Color hairCol))
            {
                currentConfig.hairColor = hairCol;
            }
            else
            {
                currentConfig.hairColor = hairColors[0];
            }

            string outfitHex = PlayerPrefs.GetString("Avatar_OutfitColor", "");
            if (!string.IsNullOrEmpty(outfitHex) && ColorUtility.TryParseHtmlString("#" + outfitHex, out Color outfitCol))
            {
                currentConfig.outfitColor = outfitCol;
            }
            else
            {
                currentConfig.outfitColor = outfitColors[0];
            }

            currentConfig.username = PlayerPrefs.GetString("Username", "Guest");
        }

        private void SaveConfig()
        {
            PlayerPrefs.SetInt("Avatar_BodyType", currentConfig.bodyType);
            PlayerPrefs.SetInt("Avatar_OutfitId", currentConfig.outfitId);
            PlayerPrefs.SetInt("Avatar_Accessories", currentConfig.accessoryMask);
            PlayerPrefs.SetString("Avatar_SkinColor", ColorUtility.ToHtmlStringRGB(currentConfig.skinColor));
            PlayerPrefs.SetString("Avatar_HairColor", ColorUtility.ToHtmlStringRGB(currentConfig.hairColor));
            PlayerPrefs.SetString("Avatar_OutfitColor", ColorUtility.ToHtmlStringRGB(currentConfig.outfitColor));
            PlayerPrefs.Save();
        }

        private void SetupUI()
        {
            if (customizationCanvas == null)
            {
                CreateCustomizationUI();
            }

            // Setup category buttons
            if (bodyButton) bodyButton.onClick.AddListener(() => ShowCategory(CustomizationCategory.Body));
            if (outfitButton) outfitButton.onClick.AddListener(() => ShowCategory(CustomizationCategory.Outfit));
            if (accessoriesButton) accessoriesButton.onClick.AddListener(() => ShowCategory(CustomizationCategory.Accessories));
            if (colorsButton) colorsButton.onClick.AddListener(() => ShowCategory(CustomizationCategory.Colors));

            // Start with body category
            ShowCategory(CustomizationCategory.Body);

            // Hide canvas initially
            if (customizationCanvas != null)
            {
                customizationCanvas.gameObject.SetActive(false);
            }
        }

        private void CreateCustomizationUI()
        {
            // Create canvas
            var canvasObj = new GameObject("AvatarCustomizationUI");
            canvasObj.transform.SetParent(transform);

            customizationCanvas = canvasObj.AddComponent<Canvas>();
            customizationCanvas.renderMode = RenderMode.WorldSpace;

            var rectTransform = customizationCanvas.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(800, 600);
            rectTransform.localScale = Vector3.one * 0.002f;

            // Add canvas scaler
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100;

            // Add graphic raycaster for VR interaction
            canvasObj.AddComponent<GraphicRaycaster>();

            // Create background panel
            var bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform);
            var bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            // Create title
            CreateTitle(canvasObj.transform);

            // Create category buttons
            CreateCategoryButtons(canvasObj.transform);

            // Create content area
            CreateContentArea(canvasObj.transform);

            // Create confirm button
            CreateConfirmButton(canvasObj.transform);
        }

        private void CreateTitle(Transform parent)
        {
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parent);

            var rect = titleObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.85f);
            rect.anchorMax = new Vector2(0.9f, 0.95f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = titleObj.AddComponent<TextMeshProUGUI>();
            text.text = "CUSTOMIZE YOUR AVATAR";
            text.fontSize = 32;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
        }

        private void CreateCategoryButtons(Transform parent)
        {
            var buttonsContainer = new GameObject("CategoryButtons");
            buttonsContainer.transform.SetParent(parent);

            var containerRect = buttonsContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.05f, 0.75f);
            containerRect.anchorMax = new Vector2(0.95f, 0.82f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            var layout = buttonsContainer.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            bodyButton = CreateCategoryButton(buttonsContainer.transform, "Body");
            outfitButton = CreateCategoryButton(buttonsContainer.transform, "Outfit");
            accessoriesButton = CreateCategoryButton(buttonsContainer.transform, "Accessories");
            colorsButton = CreateCategoryButton(buttonsContainer.transform, "Colors");
        }

        private Button CreateCategoryButton(Transform parent, string label)
        {
            var btnObj = new GameObject(label + "Button");
            btnObj.transform.SetParent(parent);

            var rect = btnObj.AddComponent<RectTransform>();

            var image = btnObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.3f);

            var button = btnObj.AddComponent<Button>();
            button.targetGraphic = image;

            var colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.3f);
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.5f);
            colors.pressedColor = new Color(0.4f, 0.4f, 0.6f);
            colors.selectedColor = new Color(0.3f, 0.5f, 0.8f);
            button.colors = colors;

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 20;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            return button;
        }

        private void CreateContentArea(Transform parent)
        {
            var contentArea = new GameObject("ContentArea");
            contentArea.transform.SetParent(parent);

            var rect = contentArea.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.15f);
            rect.anchorMax = new Vector2(0.95f, 0.72f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Create panels for each category
            bodyPanel = CreateSelectionPanel(contentArea.transform, "BodyPanel");
            outfitPanel = CreateSelectionPanel(contentArea.transform, "OutfitPanel");
            accessoriesPanel = CreateSelectionPanel(contentArea.transform, "AccessoriesPanel");
            colorsPanel = CreateColorPanel(contentArea.transform);
        }

        private GameObject CreateSelectionPanel(Transform parent, string name)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent);

            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var layout = panel.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(100, 120);
            layout.spacing = new Vector2(15, 15);
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.childAlignment = TextAnchor.UpperLeft;

            // Add scroll rect
            var scrollRect = panel.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            return panel;
        }

        private GameObject CreateColorPanel(Transform parent)
        {
            var panel = new GameObject("ColorsPanel");
            panel.transform.SetParent(parent);

            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 20;
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.childForceExpandHeight = false;

            // Create color rows
            CreateColorRow(panel.transform, "Skin Color", skinColors, (color) => SetSkinColor(color));
            CreateColorRow(panel.transform, "Hair Color", hairColors, (color) => SetHairColor(color));
            CreateColorRow(panel.transform, "Outfit Color", outfitColors, (color) => SetOutfitColor(color));

            return panel;
        }

        private void CreateColorRow(Transform parent, string label, Color[] colors, Action<Color> onColorSelected)
        {
            var rowObj = new GameObject(label + "Row");
            rowObj.transform.SetParent(parent);

            var rowRect = rowObj.AddComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0, 80);

            var rowLayout = rowObj.AddComponent<VerticalLayoutGroup>();
            rowLayout.spacing = 5;

            // Label
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(rowObj.transform);

            var labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 18;
            labelText.color = Color.white;

            var labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(0, 25);

            // Color swatches container
            var swatchesObj = new GameObject("Swatches");
            swatchesObj.transform.SetParent(rowObj.transform);

            var swatchRect = swatchesObj.AddComponent<RectTransform>();
            swatchRect.sizeDelta = new Vector2(0, 50);

            var swatchLayout = swatchesObj.AddComponent<HorizontalLayoutGroup>();
            swatchLayout.spacing = 8;
            swatchLayout.childForceExpandWidth = false;

            // Create color buttons
            foreach (var color in colors)
            {
                CreateColorSwatch(swatchesObj.transform, color, onColorSelected);
            }
        }

        private void CreateColorSwatch(Transform parent, Color color, Action<Color> onSelected)
        {
            var swatchObj = new GameObject("Swatch");
            swatchObj.transform.SetParent(parent);

            var rect = swatchObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(40, 40);

            var image = swatchObj.AddComponent<Image>();
            image.color = color;

            var button = swatchObj.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onSelected(color));

            // Add outline for selected state
            var outline = swatchObj.AddComponent<Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(2, 2);
            outline.enabled = false;
        }

        private void CreateConfirmButton(Transform parent)
        {
            var btnObj = new GameObject("ConfirmButton");
            btnObj.transform.SetParent(parent);

            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.35f, 0.03f);
            rect.anchorMax = new Vector2(0.65f, 0.1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = btnObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.6f, 0.3f);

            var button = btnObj.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(ConfirmCustomization);

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "CONFIRM";
            text.fontSize = 24;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
        }

        private void CreateDefaultPreview()
        {
            if (avatarPreviewPosition == null)
            {
                var previewObj = new GameObject("AvatarPreview");
                previewObj.transform.SetParent(transform);
                previewObj.transform.localPosition = new Vector3(0, 0, 2);
                avatarPreviewPosition = previewObj.transform;
            }

            UpdatePreviewAvatar();
        }

        private void Update()
        {
            // Auto-rotate preview avatar
            if (autoRotatePreview && previewAvatar != null && customizationCanvas.gameObject.activeSelf)
            {
                previewAvatar.transform.Rotate(Vector3.up, previewRotationSpeed * Time.deltaTime);
            }
        }

        // Category switching
        private void ShowCategory(CustomizationCategory category)
        {
            activeCategory = category;

            // Hide all panels
            if (bodyPanel) bodyPanel.SetActive(false);
            if (outfitPanel) outfitPanel.SetActive(false);
            if (accessoriesPanel) accessoriesPanel.SetActive(false);
            if (colorsPanel) colorsPanel.SetActive(false);

            // Show selected panel
            switch (category)
            {
                case CustomizationCategory.Body:
                    if (bodyPanel) bodyPanel.SetActive(true);
                    PopulateBodyOptions();
                    break;
                case CustomizationCategory.Outfit:
                    if (outfitPanel) outfitPanel.SetActive(true);
                    PopulateOutfitOptions();
                    break;
                case CustomizationCategory.Accessories:
                    if (accessoriesPanel) accessoriesPanel.SetActive(true);
                    PopulateAccessoryOptions();
                    break;
                case CustomizationCategory.Colors:
                    if (colorsPanel) colorsPanel.SetActive(true);
                    break;
            }

            UpdateCategoryButtonStates();
        }

        private void UpdateCategoryButtonStates()
        {
            var selectedColor = new Color(0.3f, 0.5f, 0.8f);
            var normalColor = new Color(0.2f, 0.2f, 0.3f);

            if (bodyButton) bodyButton.image.color = activeCategory == CustomizationCategory.Body ? selectedColor : normalColor;
            if (outfitButton) outfitButton.image.color = activeCategory == CustomizationCategory.Outfit ? selectedColor : normalColor;
            if (accessoriesButton) accessoriesButton.image.color = activeCategory == CustomizationCategory.Accessories ? selectedColor : normalColor;
            if (colorsButton) colorsButton.image.color = activeCategory == CustomizationCategory.Colors ? selectedColor : normalColor;
        }

        private void PopulateBodyOptions()
        {
            // Clear existing options
            foreach (Transform child in bodyPanel.transform)
            {
                Destroy(child.gameObject);
            }

            // Create body type options
            string[] bodyTypes = { "Default", "Slim", "Athletic", "Heavy", "Robot", "Fantasy" };

            for (int i = 0; i < bodyTypes.Length; i++)
            {
                int index = i;
                CreateOptionButton(bodyPanel.transform, bodyTypes[i], null, () => SetBodyType(index));
            }
        }

        private void PopulateOutfitOptions()
        {
            // Clear existing options
            foreach (Transform child in outfitPanel.transform)
            {
                Destroy(child.gameObject);
            }

            // Create outfit options matching reference images
            string[] outfits = {
                "Casual",
                "Streetwear",
                "Business",
                "Creative",
                "Futuristic",
                "Sporty",
                "Formal",
                "Punk",
                "Vintage",
                "Minimalist"
            };

            for (int i = 0; i < outfits.Length; i++)
            {
                int index = i;
                CreateOptionButton(outfitPanel.transform, outfits[i], null, () => SetOutfit(index));
            }
        }

        private void PopulateAccessoryOptions()
        {
            // Clear existing options
            foreach (Transform child in accessoriesPanel.transform)
            {
                Destroy(child.gameObject);
            }

            // Create accessory options
            string[] accessories = {
                "None",
                "Glasses",
                "Sunglasses",
                "Hat",
                "Headphones",
                "Earrings",
                "Necklace",
                "Watch",
                "Backpack",
                "Wings"
            };

            for (int i = 0; i < accessories.Length; i++)
            {
                int index = i;
                CreateOptionButton(accessoriesPanel.transform, accessories[i], null, () => ToggleAccessory(index));
            }
        }

        private void CreateOptionButton(Transform parent, string label, Sprite icon, Action onClick)
        {
            var btnObj = new GameObject(label + "Option");
            btnObj.transform.SetParent(parent);

            var rect = btnObj.AddComponent<RectTransform>();

            var image = btnObj.AddComponent<Image>();
            image.color = new Color(0.25f, 0.25f, 0.35f);

            var button = btnObj.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick?.Invoke());

            var colors = button.colors;
            colors.highlightedColor = new Color(0.35f, 0.35f, 0.5f);
            colors.pressedColor = new Color(0.4f, 0.5f, 0.7f);
            button.colors = colors;

            // Icon (if provided)
            if (icon != null)
            {
                var iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(btnObj.transform);

                var iconRect = iconObj.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.1f, 0.3f);
                iconRect.anchorMax = new Vector2(0.9f, 0.9f);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;

                var iconImage = iconObj.AddComponent<Image>();
                iconImage.sprite = icon;
                iconImage.preserveAspect = true;
            }

            // Label
            var textObj = new GameObject("Label");
            textObj.transform.SetParent(btnObj.transform);

            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 0.3f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
        }

        // Customization setters
        public void SetBodyType(int typeId)
        {
            currentConfig.bodyType = typeId;
            UpdatePreviewAvatar();
            OnCustomizationChanged?.Invoke(currentConfig);
        }

        public void SetOutfit(int outfitId)
        {
            currentConfig.outfitId = outfitId;
            UpdatePreviewAvatar();
            OnCustomizationChanged?.Invoke(currentConfig);
        }

        public void ToggleAccessory(int accessoryId)
        {
            // Use bitmask for multiple accessories
            currentConfig.accessoryMask ^= (1 << accessoryId);
            UpdatePreviewAvatar();
            OnCustomizationChanged?.Invoke(currentConfig);
        }

        public void SetSkinColor(Color color)
        {
            currentConfig.skinColor = color;
            UpdatePreviewAvatar();
            OnCustomizationChanged?.Invoke(currentConfig);
        }

        public void SetHairColor(Color color)
        {
            currentConfig.hairColor = color;
            UpdatePreviewAvatar();
            OnCustomizationChanged?.Invoke(currentConfig);
        }

        public void SetOutfitColor(Color color)
        {
            currentConfig.outfitColor = color;
            UpdatePreviewAvatar();
            OnCustomizationChanged?.Invoke(currentConfig);
        }

        private void UpdatePreviewAvatar()
        {
            if (previewAvatar != null)
            {
                Destroy(previewAvatar);
            }

            // Create preview avatar with current config
            previewAvatar = CreateAvatarFromConfig(currentConfig);

            if (previewAvatar != null && avatarPreviewPosition != null)
            {
                previewAvatar.transform.SetParent(avatarPreviewPosition);
                previewAvatar.transform.localPosition = Vector3.zero;
                previewAvatar.transform.localRotation = Quaternion.identity;
            }
        }

        public GameObject CreateAvatarFromConfig(AvatarConfig config)
        {
            // Create base avatar
            GameObject avatar;

            if (avatarBasePrefabs != null && config.bodyType < avatarBasePrefabs.Length && avatarBasePrefabs[config.bodyType] != null)
            {
                avatar = Instantiate(avatarBasePrefabs[config.bodyType]);
            }
            else
            {
                // Create default placeholder avatar
                avatar = CreatePlaceholderAvatar(config);
            }

            // Apply colors to avatar
            ApplyColorsToAvatar(avatar, config);

            return avatar;
        }

        private GameObject CreatePlaceholderAvatar(AvatarConfig config)
        {
            var avatar = new GameObject("Avatar");

            // Body (capsule)
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(avatar.transform);
            body.transform.localPosition = new Vector3(0, 1f, 0);
            body.transform.localScale = GetBodyScale(config.bodyType);
            DestroyImmediate(body.GetComponent<Collider>());

            // Head (sphere)
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(avatar.transform);
            head.transform.localPosition = new Vector3(0, 1.75f, 0);
            head.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            DestroyImmediate(head.GetComponent<Collider>());

            // Hair (scaled sphere)
            var hair = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hair.name = "Hair";
            hair.transform.SetParent(head.transform);
            hair.transform.localPosition = new Vector3(0, 0.2f, 0);
            hair.transform.localScale = new Vector3(1.1f, 0.5f, 1.1f);
            DestroyImmediate(hair.GetComponent<Collider>());

            return avatar;
        }

        private Vector3 GetBodyScale(int bodyType)
        {
            switch (bodyType)
            {
                case 1: return new Vector3(0.4f, 0.5f, 0.4f);  // Slim
                case 2: return new Vector3(0.55f, 0.5f, 0.45f); // Athletic
                case 3: return new Vector3(0.6f, 0.5f, 0.55f);  // Heavy
                case 4: return new Vector3(0.5f, 0.55f, 0.45f); // Robot
                case 5: return new Vector3(0.45f, 0.52f, 0.4f); // Fantasy
                default: return new Vector3(0.5f, 0.5f, 0.5f);  // Default
            }
        }

        private void ApplyColorsToAvatar(GameObject avatar, AvatarConfig config)
        {
            // Apply skin color to body and head
            var body = avatar.transform.Find("Body");
            var head = avatar.transform.Find("Head");
            var hair = head?.Find("Hair");

            Material skinMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            skinMat.color = config.skinColor;

            Material hairMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            hairMat.color = config.hairColor;

            Material outfitMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            outfitMat.color = config.outfitColor;

            if (body != null)
            {
                var renderer = body.GetComponent<Renderer>();
                if (renderer != null) renderer.material = outfitMat;
            }

            if (head != null)
            {
                var renderer = head.GetComponent<Renderer>();
                if (renderer != null) renderer.material = skinMat;
            }

            if (hair != null)
            {
                var renderer = hair.GetComponent<Renderer>();
                if (renderer != null) renderer.material = hairMat;
            }
        }

        // Public API
        public void ShowCustomization()
        {
            if (customizationCanvas != null)
            {
                customizationCanvas.gameObject.SetActive(true);

                // Position in front of player
                if (Camera.main != null)
                {
                    Vector3 forward = Camera.main.transform.forward;
                    forward.y = 0;
                    forward.Normalize();

                    customizationCanvas.transform.position = Camera.main.transform.position + forward * 2f;
                    customizationCanvas.transform.position = new Vector3(
                        customizationCanvas.transform.position.x,
                        1.5f,
                        customizationCanvas.transform.position.z
                    );
                    customizationCanvas.transform.LookAt(Camera.main.transform);
                    customizationCanvas.transform.Rotate(0, 180, 0);
                }

                // Position preview avatar
                if (avatarPreviewPosition != null)
                {
                    avatarPreviewPosition.position = customizationCanvas.transform.position + customizationCanvas.transform.right * 1f;
                }

                UpdatePreviewAvatar();
            }
        }

        public void HideCustomization()
        {
            if (customizationCanvas != null)
            {
                customizationCanvas.gameObject.SetActive(false);
            }
        }

        public void ToggleCustomization()
        {
            if (customizationCanvas != null)
            {
                if (customizationCanvas.gameObject.activeSelf)
                    HideCustomization();
                else
                    ShowCustomization();
            }
        }

        public void ConfirmCustomization()
        {
            SaveConfig();
            OnCustomizationConfirmed?.Invoke(currentConfig);

            // Update network player
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.UpdateLocalAvatar(
                    currentConfig.outfitId,
                    ColorUtility.ToHtmlStringRGB(currentConfig.outfitColor),
                    GetOutfitName(currentConfig.outfitId)
                );
            }

            HideCustomization();
        }

        private string GetOutfitName(int outfitId)
        {
            string[] outfits = { "casual", "streetwear", "business", "creative", "futuristic", "sporty", "formal", "punk", "vintage", "minimalist" };
            if (outfitId >= 0 && outfitId < outfits.Length)
            {
                return outfits[outfitId];
            }
            return "casual";
        }

        public AvatarConfig GetCurrentConfig() => currentConfig;
    }

    [Serializable]
    public class AvatarConfig
    {
        public string username = "Guest";
        public int bodyType = 0;
        public int outfitId = 0;
        public int accessoryMask = 0;
        public Color skinColor = Color.white;
        public Color hairColor = Color.black;
        public Color outfitColor = Color.blue;
    }
}
