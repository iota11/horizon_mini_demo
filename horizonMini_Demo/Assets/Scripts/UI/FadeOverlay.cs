using UnityEngine;
using UnityEngine.UI;

namespace HorizonMini.UI
{
    /// <summary>
    /// Simple fade overlay for screen transitions
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class FadeOverlay : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private static FadeOverlay instance;

        public static FadeOverlay Instance
        {
            get
            {
                if (instance == null)
                {
                    // Try to find existing instance
                    instance = FindFirstObjectByType<FadeOverlay>();

                    // Create new one if none exists
                    if (instance == null)
                    {
                        GameObject overlayObj = new GameObject("FadeOverlay");
                        Canvas canvas = overlayObj.AddComponent<Canvas>();
                        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                        canvas.sortingOrder = 9999; // On top of everything

                        CanvasScaler scaler = overlayObj.AddComponent<CanvasScaler>();
                        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                        scaler.referenceResolution = new Vector2(1920, 1080);

                        overlayObj.AddComponent<GraphicRaycaster>();

                        // Create image panel
                        GameObject panel = new GameObject("Panel");
                        panel.transform.SetParent(overlayObj.transform, false);

                        RectTransform rt = panel.AddComponent<RectTransform>();
                        rt.anchorMin = Vector2.zero;
                        rt.anchorMax = Vector2.one;
                        rt.sizeDelta = Vector2.zero;

                        Image img = panel.AddComponent<Image>();
                        img.color = Color.black;

                        instance = overlayObj.AddComponent<FadeOverlay>();
                        DontDestroyOnLoad(overlayObj);
                    }
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Start transparent
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        public void SetAlpha(float alpha)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Clamp01(alpha);
                canvasGroup.blocksRaycasts = alpha > 0.5f; // Block input when mostly faded
            }
        }

        public float GetAlpha()
        {
            return canvasGroup != null ? canvasGroup.alpha : 0f;
        }
    }
}
