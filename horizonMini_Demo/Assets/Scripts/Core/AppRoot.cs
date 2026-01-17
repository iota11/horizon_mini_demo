using UnityEngine;
using HorizonMini.Controllers;
using HorizonMini.UI;

namespace HorizonMini.Core
{
    /// <summary>
    /// Main application controller - manages all systems and UI routing
    /// </summary>
    public class AppRoot : MonoBehaviour
    {
        [Header("Core Systems")]
        [SerializeField] private WorldLibrary worldLibrary;
        [SerializeField] private SaveService saveService;

        [Header("Controllers")]
        [SerializeField] private BrowseController browseController;
        [SerializeField] private BuildController buildController;
        [SerializeField] private HomeController homeController;
        [SerializeField] private PlayController playController;

        [Header("UI")]
        [SerializeField] private UIRouter uiRouter;

        public static AppRoot Instance { get; private set; }

        public WorldLibrary WorldLibrary => worldLibrary;
        public SaveService SaveService => saveService;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeSystems();
        }

        private void InitializeSystems()
        {
            // Initialize save service first
            if (saveService == null)
            {
                saveService = gameObject.AddComponent<SaveService>();
            }

            // Initialize world library
            if (worldLibrary == null)
            {
                worldLibrary = gameObject.AddComponent<WorldLibrary>();
            }
            worldLibrary.Initialize(saveService);

            // Initialize controllers
            InitializeControllers();

            // Initialize UI
            if (uiRouter != null)
            {
                uiRouter.Initialize(this);
            }

            // Start in Browse mode
            SwitchToMode(AppMode.Browse);
        }

        private void InitializeControllers()
        {
            if (browseController != null)
            {
                browseController.Initialize(this);
            }

            if (buildController != null)
            {
                buildController.Initialize(this);
            }

            if (homeController != null)
            {
                homeController.Initialize(this);
            }

            if (playController != null)
            {
                playController.Initialize(this);
            }
        }

        public void SwitchToMode(AppMode mode)
        {
            // Disable all controllers
            if (browseController != null) browseController.SetActive(false);
            if (buildController != null) buildController.SetActive(false);
            if (homeController != null) homeController.SetActive(false);
            if (playController != null) playController.SetActive(false);

            // Enable the requested mode
            switch (mode)
            {
                case AppMode.Browse:
                    if (browseController != null)
                    {
                        browseController.SetActive(true);
                    }
                    break;

                case AppMode.Build:
                    if (buildController != null)
                    {
                        buildController.SetActive(true);
                    }
                    break;

                case AppMode.Home:
                    if (homeController != null)
                    {
                        homeController.SetActive(true);
                    }
                    break;

                case AppMode.Play:
                    if (playController != null)
                    {
                        playController.SetActive(true);
                    }
                    break;
            }

            // Update UI
            if (uiRouter != null)
            {
                uiRouter.OnModeChanged(mode);
            }
        }

        public void EnterPlayMode(string worldId)
        {
            if (playController != null)
            {
                playController.EnterWorld(worldId);
                SwitchToMode(AppMode.Play);
            }
        }

        public void ExitPlayMode()
        {
            if (playController != null)
            {
                playController.ExitWorld();
            }
            SwitchToMode(AppMode.Browse);
        }
    }

    public enum AppMode
    {
        Browse,
        Build,
        Home,
        Play
    }
}
