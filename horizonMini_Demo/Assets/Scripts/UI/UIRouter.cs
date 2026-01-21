using UnityEngine;
using UnityEngine.UI;
using HorizonMini.Core;
using HorizonMini.Controllers;

namespace HorizonMini.UI
{
    /// <summary>
    /// Manages UI navigation and mode switching
    /// </summary>
    public class UIRouter : MonoBehaviour
    {
        [Header("Bottom Tab Bar")]
        [SerializeField] private Button worldTabButton;
        [SerializeField] private Button addTabButton;
        [SerializeField] private Button homeTabButton;

        [Header("Browse UI")]
        [SerializeField] private GameObject browseUI;
        [SerializeField] private Button goButton;
        [SerializeField] private Button likeButton;
        [SerializeField] private Button collectButton;

        [Header("Build UI")]
        [SerializeField] private GameObject buildUI;
        [SerializeField] private Button saveWorldButton;
        [SerializeField] private Button newWorldButton;
        [SerializeField] private Button placeModeButton;
        [SerializeField] private Button removeModeButton;

        [Header("Play UI")]
        [SerializeField] private GameObject playUI;
        [SerializeField] private Button exitPlayButton;

        [Header("Home UI")]
        [SerializeField] private GameObject homeUI;

        private AppRoot appRoot;

        public void Initialize(AppRoot root)
        {
            appRoot = root;

            // Auto-find tab buttons if not assigned
            if (worldTabButton == null || addTabButton == null || homeTabButton == null)
            {
                Debug.Log("Auto-finding tab buttons...");
                Button[] buttons = GetComponentsInChildren<Button>(true);
                foreach (Button btn in buttons)
                {
                    if (btn.name == "WorldTab" && worldTabButton == null)
                    {
                        worldTabButton = btn;
                        Debug.Log("Found WorldTab button");
                    }
                    else if (btn.name == "BuildTab" && addTabButton == null)
                    {
                        addTabButton = btn;
                        Debug.Log("Found BuildTab button");
                    }
                    else if (btn.name == "HomeTab" && homeTabButton == null)
                    {
                        homeTabButton = btn;
                        Debug.Log("Found HomeTab button");
                    }
                }
            }

            SetupTabButtons();
            SetupBrowseButtons();
            SetupBuildButtons();
            SetupPlayButtons();
        }

        private void SetupTabButtons()
        {
            if (worldTabButton != null)
            {
                // Clear existing listeners first
                worldTabButton.onClick.RemoveAllListeners();
                worldTabButton.onClick.AddListener(() => {
                    Debug.Log("Browse tab button clicked");
                    appRoot.SwitchToMode(AppMode.Browse);
                });
                Debug.Log($"Browse tab button listener added. Interactable: {worldTabButton.interactable}");
            }
            else
            {
                Debug.LogWarning("WorldTabButton is null in UIRouter!");
            }

            if (addTabButton != null)
            {
                // Clear existing listeners first
                addTabButton.onClick.RemoveAllListeners();
                addTabButton.onClick.AddListener(() => {
                    Debug.Log("Build tab button clicked - Loading Build scene");
                    // Load Build scene instead of switching mode
                    if (UnityEngine.SceneManagement.SceneManager.GetSceneByName("Build").isLoaded ||
                        Application.CanStreamedLevelBeLoaded("Build"))
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadScene("Build");
                    }
                    else
                    {
                        Debug.LogError("Build scene not found! Please create Build scene first.");
                    }
                });
                Debug.Log($"Build tab button listener added. Interactable: {addTabButton.interactable}");
            }
            else
            {
                Debug.LogWarning("AddTabButton is null in UIRouter!");
            }

            if (homeTabButton != null)
            {
                // Clear existing listeners first
                homeTabButton.onClick.RemoveAllListeners();
                homeTabButton.onClick.AddListener(() => {
                    Debug.Log("Home tab button clicked");
                    appRoot.SwitchToMode(AppMode.Home);
                });
                Debug.Log($"Home tab button listener added. Interactable: {homeTabButton.interactable}");
            }
            else
            {
                Debug.LogWarning("HomeTabButton is null in UIRouter!");
            }
        }

        private void SetupBrowseButtons()
        {
            if (goButton != null)
            {
                var browseController = appRoot.GetComponent<BrowseController>();
                if (browseController == null)
                    browseController = FindFirstObjectByType<BrowseController>();

                if (browseController != null)
                {
                    goButton.onClick.AddListener(() => browseController.OnGoButtonPressed());
                }
            }

            if (likeButton != null)
            {
                var browseController = appRoot.GetComponent<BrowseController>();
                if (browseController == null)
                    browseController = FindFirstObjectByType<BrowseController>();

                if (browseController != null)
                {
                    likeButton.onClick.AddListener(() => browseController.OnLikeButtonPressed());
                }
            }

            if (collectButton != null)
            {
                var browseController = appRoot.GetComponent<BrowseController>();
                if (browseController == null)
                    browseController = FindFirstObjectByType<BrowseController>();

                if (browseController != null)
                {
                    collectButton.onClick.AddListener(() => browseController.OnCollectButtonPressed());
                }
            }
        }

        private void SetupBuildButtons()
        {
            var buildController = appRoot.GetComponent<BuildController>();
            if (buildController == null)
                buildController = FindFirstObjectByType<BuildController>();

            if (buildController != null)
            {
                // Note: New BuildController uses BuildModeUI for button handling
                // These legacy buttons are optional and map to new BuildController methods

                if (saveWorldButton != null)
                {
                    saveWorldButton.onClick.AddListener(() => buildController.OnPublicButtonPressed());
                }

                // NewWorld and SetPlacementMode don't exist in new BuildController
                // These were part of the old volume-based system
                // Comment them out for now

                /*
                if (newWorldButton != null)
                {
                    newWorldButton.onClick.AddListener(() => buildController.NewWorld());
                }

                if (placeModeButton != null)
                {
                    placeModeButton.onClick.AddListener(() => buildController.SetPlacementMode(true));
                }

                if (removeModeButton != null)
                {
                    removeModeButton.onClick.AddListener(() => buildController.SetPlacementMode(false));
                }
                */
            }
        }

        private void SetupPlayButtons()
        {
            // Play mode is now a separate scene, no need to setup buttons here
            // Exit button is handled in the Play scene itself
            if (exitPlayButton != null)
            {
                // Deprecated: This button is not used anymore
                exitPlayButton.gameObject.SetActive(false);
            }
        }

        public void OnModeChanged(AppMode mode)
        {
            // Hide all mode UIs
            if (browseUI != null) browseUI.SetActive(false);
            if (buildUI != null) buildUI.SetActive(false);
            if (playUI != null) playUI.SetActive(false);
            if (homeUI != null) homeUI.SetActive(false);

            // Show appropriate UI
            switch (mode)
            {
                case AppMode.Browse:
                    if (browseUI != null) browseUI.SetActive(true);
                    break;

                case AppMode.Build:
                    if (buildUI != null) buildUI.SetActive(true);
                    break;

                case AppMode.Play:
                    if (playUI != null) playUI.SetActive(true);
                    break;

                case AppMode.Home:
                    if (homeUI != null) homeUI.SetActive(true);
                    break;
            }

            // Update tab button states
            UpdateTabButtonStates(mode);
        }

        private void UpdateTabButtonStates(AppMode mode)
        {
            // Simple implementation - can be enhanced with visual feedback
            // For now, just ensure buttons are interactable
        }
    }
}
