using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GalleryVR
{
    public class InfoPanelController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas infoCanvas;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI artistText;
        [SerializeField] private TextMeshProUGUI yearText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image artworkPreview;

        [Header("Settings")]
        [SerializeField] private float displayDistance = 0.8f;
        [SerializeField] private float fadeSpeed = 3f;

        private CanvasGroup canvasGroup;
        private bool isVisible;
        private int currentArtworkId = -1;

        private void Awake()
        {
            canvasGroup = infoCanvas.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = infoCanvas.gameObject.AddComponent<CanvasGroup>();

            canvasGroup.alpha = 0;
            infoCanvas.gameObject.SetActive(false);
        }

        public void ShowArtworkInfo(int artworkId, Vector3 position)
        {
            if (ArtworkData.Instance == null) return;

            var artwork = ArtworkData.Instance.GetArtworkById(artworkId);
            if (artwork == null) return;

            currentArtworkId = artworkId;

            // Update UI
            if (titleText) titleText.text = artwork.title;
            if (artistText) artistText.text = $"by {artwork.artist}";
            if (yearText) yearText.text = artwork.year;
            if (descriptionText) descriptionText.text = artwork.description;

            // Position panel
            infoCanvas.transform.position = position;
            infoCanvas.transform.LookAt(Camera.main.transform);
            infoCanvas.transform.Rotate(0, 180, 0);

            // Show
            infoCanvas.gameObject.SetActive(true);
            isVisible = true;
        }

        public void HideArtworkInfo()
        {
            isVisible = false;
            currentArtworkId = -1;
        }

        private void Update()
        {
            if (canvasGroup == null) return;

            float targetAlpha = isVisible ? 1f : 0f;
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);

            if (!isVisible && canvasGroup.alpha < 0.01f)
            {
                infoCanvas.gameObject.SetActive(false);
            }
        }
    }
}
