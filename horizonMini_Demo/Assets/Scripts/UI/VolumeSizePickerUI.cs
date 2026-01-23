using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HorizonMini.Controllers;
using System;

namespace HorizonMini.UI
{
    /// <summary>
    /// UI for selecting volume grid size (1-4 x 1-4 x 1-4)
    /// Mobile portrait layout
    /// </summary>
    public class VolumeSizePickerUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BuildController buildController;

        [Header("UI Elements")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button createButton;
        [SerializeField] private Button cancelButton;

        [Header("World Naming")]
        [SerializeField] private TMP_InputField worldNameInput;
        [SerializeField] private TextMeshProUGUI worldNameLabel;

        [Header("Size Controls - LEGACY (sliders removed, now uses 3D cursor)")]
        [SerializeField] private Slider xSlider; // LEGACY - not used
        [SerializeField] private Slider ySlider; // LEGACY - not used
        [SerializeField] private Slider zSlider; // LEGACY - not used
        [SerializeField] private TextMeshProUGUI xValueText;
        [SerializeField] private TextMeshProUGUI yValueText;
        [SerializeField] private TextMeshProUGUI zValueText;
        [SerializeField] private TextMeshProUGUI descriptionText;

        [Header("Preview")]
        [SerializeField] private TextMeshProUGUI previewSizeText;

        private Vector3Int selectedSize = new Vector3Int(1, 1, 1); // Start with 1x1x1
        private string worldName = "";

        private void Awake()
        {
            // Sliders are now disabled - size is controlled by 3D cursor
            // SetupSliders(); // LEGACY - disabled
            SetupButtons();
            SetupWorldNameInput();
            UpdateUI();
        }

        private void SetupWorldNameInput()
        {
            if (worldNameInput != null)
            {
                // Set default name
                worldName = $"My World {System.DateTime.Now:MM-dd HH:mm}";
                worldNameInput.text = worldName;

                worldNameInput.onValueChanged.AddListener(OnWorldNameChanged);
            }
        }

        private void OnWorldNameChanged(string newName)
        {
            worldName = newName;
        }

        public string GetWorldName()
        {
            return string.IsNullOrWhiteSpace(worldName) ? $"My World {System.DateTime.Now:MM-dd HH:mm}" : worldName;
        }

        private void SetupSliders()
        {
            // X slider
            if (xSlider != null)
            {
                xSlider.minValue = 1;
                xSlider.maxValue = 4;
                xSlider.wholeNumbers = true;
                xSlider.value = selectedSize.x;
                xSlider.onValueChanged.AddListener(OnXChanged);
            }

            // Y slider
            if (ySlider != null)
            {
                ySlider.minValue = 1;
                ySlider.maxValue = 4;
                ySlider.wholeNumbers = true;
                ySlider.value = selectedSize.y;
                ySlider.onValueChanged.AddListener(OnYChanged);
            }

            // Z slider
            if (zSlider != null)
            {
                zSlider.minValue = 1;
                zSlider.maxValue = 4;
                zSlider.wholeNumbers = true;
                zSlider.value = selectedSize.z;
                zSlider.onValueChanged.AddListener(OnZChanged);
            }
        }

        private void SetupButtons()
        {
            if (createButton != null)
            {
                createButton.onClick.AddListener(OnCreateClicked);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelClicked);
            }
        }

        private void OnXChanged(float value)
        {
            selectedSize.x = Mathf.RoundToInt(value);
            UpdateUI();
            UpdateVolumePreview();
        }

        private void OnYChanged(float value)
        {
            selectedSize.y = Mathf.RoundToInt(value);
            UpdateUI();
            UpdateVolumePreview();
        }

        private void OnZChanged(float value)
        {
            selectedSize.z = Mathf.RoundToInt(value);
            UpdateUI();
            UpdateVolumePreview();
        }

        private void UpdateUI()
        {
            // Update value labels
            if (xValueText != null) xValueText.text = selectedSize.x.ToString();
            if (yValueText != null) yValueText.text = selectedSize.y.ToString();
            if (zValueText != null) zValueText.text = selectedSize.z.ToString();

            // Update preview size
            float volumeSize = 8f; // Each volume is 8x8x8 units
            Vector3 worldSize = new Vector3(
                selectedSize.x * volumeSize,
                selectedSize.y * volumeSize,
                selectedSize.z * volumeSize
            );

            if (previewSizeText != null)
            {
                previewSizeText.text = $"Space: {worldSize.x:F0} × {worldSize.y:F0} × {worldSize.z:F0} units";
            }

            // Update description
            if (descriptionText != null)
            {
                int totalVolumes = selectedSize.x * selectedSize.y * selectedSize.z;
                descriptionText.text = $"{totalVolumes} volume{(totalVolumes > 1 ? "s" : "")} ({selectedSize.x}×{selectedSize.y}×{selectedSize.z})";
            }
        }

        private void OnCreateClicked()
        {
            if (buildController != null)
            {
                // Confirm the volume drawing (size is already set by 3D cursor)
                buildController.ConfirmVolumeDrawing();
                Hide();
            }
        }

        private void OnCancelClicked()
        {
            Hide();
            // TODO: Return to main menu or previous screen
        }

        public void Show()
        {
            Debug.Log("[VolumeSizePickerUI] Show() called");

            if (panel != null)
            {
                panel.SetActive(true);
                Debug.Log($"[VolumeSizePickerUI] Panel activated: {panel.name}");
            }
            else
            {
                Debug.LogError("[VolumeSizePickerUI] Panel is NULL! Please assign it in Inspector.");
            }

            // Log button status
            if (createButton != null)
                Debug.Log($"[VolumeSizePickerUI] Create button assigned: {createButton.name}, active: {createButton.gameObject.activeSelf}");
            else
                Debug.LogError("[VolumeSizePickerUI] Create button is NULL!");

            if (cancelButton != null)
                Debug.Log($"[VolumeSizePickerUI] Cancel button assigned: {cancelButton.name}, active: {cancelButton.gameObject.activeSelf}");
            else
                Debug.LogError("[VolumeSizePickerUI] Cancel button is NULL!");

            // Enter VolumeDrawing mode ONLY if we're starting a new world (not loading existing)
            if (buildController != null)
            {
                // Check if we already have a world loaded
                // If so, don't switch to VolumeDrawing (we should be in View/Edit mode)
                if (buildController.CurrentMode == HorizonMini.Build.BuildMode.SizePicker)
                {
                    // Wait for BuildController to be initialized before switching modes
                    StartCoroutine(WaitForInitializationThenEnterDrawingMode());
                }
                else
                {
                    Debug.Log("[VolumeSizePickerUI] World already loaded, not switching to VolumeDrawing mode");
                }
            }
            else
            {
                Debug.LogWarning("[VolumeSizePickerUI] BuildController not assigned! Use Tools -> Setup BuildMode Scene.");
            }
        }

        private System.Collections.IEnumerator WaitForInitializationThenEnterDrawingMode()
        {
            // Wait a few frames to ensure BuildController is fully initialized
            // (BuildController initializes in Awake/Start, we need to wait for that)
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            if (buildController != null)
            {
                Debug.Log("[VolumeSizePickerUI] Switching to VolumeDrawing mode");
                try
                {
                    buildController.SwitchMode(HorizonMini.Build.BuildMode.VolumeDrawing);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[VolumeSizePickerUI] Failed to switch to VolumeDrawing mode: {e.Message}");
                    Debug.LogError("Make sure BuildController is properly configured. Use Tools -> Setup BuildMode Scene.");
                }
            }
        }

        public void Hide()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private void UpdateVolumePreview()
        {
            if (buildController != null)
            {
                buildController.UpdateVolumePreview(selectedSize);
            }
        }

        public void SetBuildController(BuildController controller)
        {
            buildController = controller;
        }

        /// <summary>
        /// Update UI to show current volume size (called by BuildController during cursor dragging)
        /// </summary>
        public void UpdateVolumeSize(Vector3Int size)
        {
            selectedSize = size;
            UpdateUI();
        }
    }
}
