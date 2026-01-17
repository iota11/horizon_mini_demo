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

        [Header("Size Controls")]
        [SerializeField] private Slider xSlider;
        [SerializeField] private Slider ySlider;
        [SerializeField] private Slider zSlider;
        [SerializeField] private TextMeshProUGUI xValueText;
        [SerializeField] private TextMeshProUGUI yValueText;
        [SerializeField] private TextMeshProUGUI zValueText;
        [SerializeField] private TextMeshProUGUI descriptionText;

        [Header("Preview")]
        [SerializeField] private TextMeshProUGUI previewSizeText;

        private Vector3Int selectedSize = new Vector3Int(2, 1, 2);

        private void Awake()
        {
            SetupSliders();
            SetupButtons();
            UpdateUI();
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
                buildController.CreateVolumeGrid(selectedSize);
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
            if (panel != null)
            {
                panel.SetActive(true);
            }

            // Show initial volume preview
            UpdateVolumePreview();
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
    }
}
