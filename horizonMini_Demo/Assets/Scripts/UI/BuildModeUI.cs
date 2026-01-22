using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HorizonMini.Controllers;
using HorizonMini.Build;

namespace HorizonMini.UI
{
    /// <summary>
    /// Main UI manager for Build Mode
    /// Controls visibility and state of all build UI panels
    /// </summary>
    public class BuildModeUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BuildController buildController;

        [Header("UI Panels")]
        [SerializeField] private VolumeSizePickerUI sizePickerUI;
        [SerializeField] private AssetCatalogUI assetCatalogUI;
        [SerializeField] private GameObject viewModeUI;
        [SerializeField] private GameObject editModeUI;
        [SerializeField] private GameObject playModeUI;

        [Header("View Mode Buttons")]
        [SerializeField] private Button goButton;
        [SerializeField] private Button publicButton;
        [SerializeField] private Toggle snapToGridToggle;
        [SerializeField] private Toggle snapToObjectToggle;

        [Header("Edit Mode Buttons")]
        [SerializeField] private Button deleteButton;
        [SerializeField] private Button doneEditingButton;

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI modeStatusText;
        [SerializeField] private TextMeshProUGUI instructionsText;

        private BuildModeInitializer initializer;

        private void Awake()
        {
            // Get initializer component
            initializer = GetComponent<BuildModeInitializer>();

            // Force hide all panels (prevent overlap)
            HideAllPanels();
        }

        private void HideAllPanels()
        {
            if (sizePickerUI != null) sizePickerUI.Hide();
            if (viewModeUI != null) viewModeUI.SetActive(false);
            if (editModeUI != null) editModeUI.SetActive(false);
            if (playModeUI != null) playModeUI.SetActive(false);
            if (assetCatalogUI != null) assetCatalogUI.Hide();

            Debug.Log("BuildModeUI: All panels hidden");
        }

        private void Start()
        {
            SetupButtons();
            SetupToggles();

            // Show initial mode (Size Picker)
            if (sizePickerUI != null)
            {
                sizePickerUI.Show();
                Debug.Log("BuildModeUI: Showing Size Picker");
            }
        }

        private void SetupButtons()
        {
            if (goButton != null)
            {
                goButton.onClick.AddListener(OnGoClicked);
            }

            if (publicButton != null)
            {
                publicButton.onClick.AddListener(OnPublicClicked);
            }

            if (deleteButton != null)
            {
                deleteButton.onClick.AddListener(OnDeleteClicked);
            }

            if (doneEditingButton != null)
            {
                doneEditingButton.onClick.AddListener(OnDoneEditingClicked);
            }
        }

        private void SetupToggles()
        {
            if (snapToGridToggle != null)
            {
                snapToGridToggle.isOn = true;
                snapToGridToggle.onValueChanged.AddListener(OnSnapToGridToggled);
            }

            if (snapToObjectToggle != null)
            {
                snapToObjectToggle.isOn = false;
                snapToObjectToggle.onValueChanged.AddListener(OnSnapToObjectToggled);
            }
        }

        public void OnModeChanged(BuildMode mode)
        {
            Debug.Log($"[BuildModeUI] OnModeChanged: {mode}");

            // Use initializer to set panel visibility (if exists)
            if (initializer != null)
            {
                initializer.SetModeVisibility(mode);
            }
            else
            {
                // Fallback: manually hide/show
                if (sizePickerUI != null) sizePickerUI.Hide();
                if (viewModeUI != null) viewModeUI.SetActive(false);
                if (editModeUI != null) editModeUI.SetActive(false);
                if (assetCatalogUI != null) assetCatalogUI.Hide();
            }

            // Show appropriate UI for mode
            switch (mode)
            {
                case BuildMode.SizePicker:
                    Debug.Log("[BuildModeUI] Showing SizePicker UI");
                    if (sizePickerUI != null) sizePickerUI.Show();
                    UpdateInstructions("Select space size, then click Create");
                    break;

                case BuildMode.VolumeDrawing:
                    Debug.Log("[BuildModeUI] Showing VolumeDrawing UI");
                    // Keep sizePickerUI visible (it contains Create/Cancel buttons)
                    if (sizePickerUI != null) sizePickerUI.Show();
                    UpdateInstructions("Drag cursor to draw volume size | Click Create to confirm");
                    break;

                case BuildMode.View:
                    Debug.Log("[BuildModeUI] Showing View UI");
                    if (viewModeUI != null) viewModeUI.SetActive(true);
                    if (assetCatalogUI != null) assetCatalogUI.Show();
                    UpdateInstructions("Drag to place | Tap object to Edit");
                    break;

                case BuildMode.Edit:
                    Debug.Log("[BuildModeUI] Showing Edit UI");
                    if (editModeUI != null) editModeUI.SetActive(true);
                    UpdateInstructions("Drag arrow/rotate button/object | Tap X to delete | Tap empty to return");
                    break;

                case BuildMode.Play:
                    Debug.Log("[BuildModeUI] Showing Play UI");
                    if (playModeUI != null) playModeUI.SetActive(true);
                    UpdateInstructions("Use joystick to move | Drag to rotate camera");
                    break;
            }

            // Update status text
            if (modeStatusText != null)
            {
                modeStatusText.text = $"Mode: {GetModeDisplayName(mode)}";
            }
        }

        private string GetModeDisplayName(BuildMode mode)
        {
            switch (mode)
            {
                case BuildMode.SizePicker: return "Size Picker";
                case BuildMode.VolumeDrawing: return "Volume Drawing";
                case BuildMode.View: return "View";
                case BuildMode.Edit: return "Edit";
                case BuildMode.Play: return "Play";
                default: return mode.ToString();
            }
        }

        private void UpdateInstructions(string text)
        {
            if (instructionsText != null)
            {
                instructionsText.text = text;
            }
        }

        // Button callbacks
        private void OnGoClicked()
        {
            if (buildController != null)
            {
                buildController.OnGoButtonPressed();
            }
        }

        private void OnPublicClicked()
        {
            if (buildController != null)
            {
                buildController.OnPublicButtonPressed();
            }
        }

        private void OnDeleteClicked()
        {
            if (buildController != null)
            {
                buildController.DeleteSelectedObject();
            }
        }

        private void OnDoneEditingClicked()
        {
            if (buildController != null)
            {
                buildController.SwitchMode(BuildMode.View);
            }
        }

        private void OnSnapToGridToggled(bool enabled)
        {
            if (buildController != null)
            {
                var placementSystem = buildController.GetComponent<PlacementSystem>();
                if (placementSystem != null)
                {
                    placementSystem.SetSnapToGrid(enabled);
                }
            }
        }

        private void OnSnapToObjectToggled(bool enabled)
        {
            if (buildController != null)
            {
                var placementSystem = buildController.GetComponent<PlacementSystem>();
                if (placementSystem != null)
                {
                    placementSystem.SetSnapToObject(enabled);
                }
            }
        }

        public void Initialize(BuildController controller)
        {
            buildController = controller;

            if (sizePickerUI != null)
            {
                sizePickerUI.SetBuildController(controller);
            }

            if (assetCatalogUI != null)
            {
                assetCatalogUI.SetBuildController(controller);
            }
        }

        private void Update()
        {
            // Update UI based on current mode
            if (buildController != null && buildController.CurrentMode != BuildMode.SizePicker)
            {
                // Could add dynamic updates here
            }
        }
    }
}
