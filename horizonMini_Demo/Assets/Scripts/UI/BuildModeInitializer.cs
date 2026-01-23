using UnityEngine;
using HorizonMini.Build;

namespace HorizonMini.UI
{
    /// <summary>
    /// Initialize Build Mode UI, set correct show/hide states
    /// Attach to BuildModeCanvas, executes before BuildModeUI
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class BuildModeInitializer : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject volumeSizePickerPanel;
        [SerializeField] private GameObject viewModeUI;
        [SerializeField] private GameObject editModeUI;
        [SerializeField] private GameObject assetCatalogPanel;
        [SerializeField] private GameObject statusBar;

        [Header("Settings")]
        [SerializeField] private BuildMode initialMode = BuildMode.SizePicker;

        private void Awake()
        {
            InitializeUIState();
        }

        private void InitializeUIState()
        {
            // Hide all panels by default
            if (volumeSizePickerPanel != null) volumeSizePickerPanel.SetActive(false);
            if (viewModeUI != null) viewModeUI.SetActive(false);
            if (editModeUI != null) editModeUI.SetActive(false);
            if (assetCatalogPanel != null) assetCatalogPanel.SetActive(false);

            // Status bar always visible
            if (statusBar != null) statusBar.SetActive(true);

            // Show panel based on initial mode
            switch (initialMode)
            {
                case BuildMode.SizePicker:
                    if (volumeSizePickerPanel != null) volumeSizePickerPanel.SetActive(true);
                    break;

                case BuildMode.View:
                    if (viewModeUI != null) viewModeUI.SetActive(true);
                    if (assetCatalogPanel != null) assetCatalogPanel.SetActive(true);
                    break;

                case BuildMode.Edit:
                    if (editModeUI != null) editModeUI.SetActive(true);
                    break;
            }

            Debug.Log($"Build Mode UI initialized - Initial mode: {initialMode}");
        }

        // Manually set mode visibility (called by BuildModeUI)
        public void SetModeVisibility(BuildMode mode)
        {
            // Hide all
            if (volumeSizePickerPanel != null) volumeSizePickerPanel.SetActive(false);
            if (viewModeUI != null) viewModeUI.SetActive(false);
            if (editModeUI != null) editModeUI.SetActive(false);
            if (assetCatalogPanel != null) assetCatalogPanel.SetActive(false);

            // Show corresponding mode
            switch (mode)
            {
                case BuildMode.SizePicker:
                    if (volumeSizePickerPanel != null) volumeSizePickerPanel.SetActive(true);
                    break;

                case BuildMode.VolumeDrawing:
                    // VolumeDrawing uses same panel as SizePicker (but with 3D cursor instead of sliders)
                    if (volumeSizePickerPanel != null) volumeSizePickerPanel.SetActive(true);
                    break;

                case BuildMode.View:
                    if (viewModeUI != null) viewModeUI.SetActive(true);
                    if (assetCatalogPanel != null) assetCatalogPanel.SetActive(true);
                    break;

                case BuildMode.Edit:
                    if (editModeUI != null) editModeUI.SetActive(true);
                    break;

                case BuildMode.Play:
                    // Hide all Build UI in Play mode
                    break;
            }
        }
    }
}
