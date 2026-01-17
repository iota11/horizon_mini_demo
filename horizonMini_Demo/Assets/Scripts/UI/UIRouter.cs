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

            SetupTabButtons();
            SetupBrowseButtons();
            SetupBuildButtons();
            SetupPlayButtons();
        }

        private void SetupTabButtons()
        {
            if (worldTabButton != null)
            {
                worldTabButton.onClick.AddListener(() => appRoot.SwitchToMode(AppMode.Browse));
            }

            if (addTabButton != null)
            {
                addTabButton.onClick.AddListener(() => appRoot.SwitchToMode(AppMode.Build));
            }

            if (homeTabButton != null)
            {
                homeTabButton.onClick.AddListener(() => appRoot.SwitchToMode(AppMode.Home));
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
            if (exitPlayButton != null)
            {
                var playController = appRoot.GetComponent<PlayController>();
                if (playController == null)
                    playController = FindFirstObjectByType<PlayController>();

                if (playController != null)
                {
                    exitPlayButton.onClick.AddListener(() => playController.OnExitButtonPressed());
                }
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
