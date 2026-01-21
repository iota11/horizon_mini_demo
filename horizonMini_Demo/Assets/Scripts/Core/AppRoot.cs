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
            // Don't use DontDestroyOnLoad - let each scene have its own AppRoot
            // This avoids conflicts when switching between Main and Build scenes
            // DontDestroyOnLoad(gameObject);

            // Only initialize if we have the required UI components
            // Play scene creates AppRoot manually without UI
            if (uiRouter != null || browseController != null || buildController != null || homeController != null)
            {
                InitializeSystems();
            }
            else
            {
                Debug.Log("AppRoot: Skipping full initialization (minimal AppRoot for Play scene)");
            }
        }

        /// <summary>
        /// Public initialize method for standalone scene setup (e.g., Play scene)
        /// </summary>
        public void Initialize()
        {
            InitializeSystems();
        }

        private void InitializeSystems()
        {
            Debug.Log("AppRoot: InitializeSystems started");

            // Initialize save service first
            if (saveService == null)
            {
                saveService = gameObject.AddComponent<SaveService>();
                Debug.Log("AppRoot: Created SaveService");
            }

            // Initialize world library
            if (worldLibrary == null)
            {
                worldLibrary = gameObject.AddComponent<WorldLibrary>();
                Debug.Log("AppRoot: Created WorldLibrary");
            }
            worldLibrary.Initialize(saveService);

            // Initialize controllers
            InitializeControllers();

            // Initialize UI
            if (uiRouter != null)
            {
                Debug.Log("AppRoot: Initializing UIRouter");
                uiRouter.Initialize(this);
            }
            else
            {
                Debug.LogError("AppRoot: UIRouter is null! Cannot initialize UI.");
            }

            // Start in Browse mode
            Debug.Log("AppRoot: Starting in Browse mode");
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

            // PlayController is now standalone in Play scene, doesn't need Initialize from AppRoot
            // if (playController != null)
            // {
            //     playController.Initialize(this);
            // }
        }

        public void SwitchToMode(AppMode mode)
        {
            Debug.Log($"AppRoot: Switching to {mode} mode");

            // Disable all controllers
            if (browseController != null) browseController.SetActive(false);
            if (buildController != null) buildController.SetActive(false);
            if (homeController != null) homeController.SetActive(false);
            // PlayController is now in separate scene
            // if (playController != null) playController.SetActive(false);

            // Enable the requested mode
            switch (mode)
            {
                case AppMode.Browse:
                    if (browseController != null)
                    {
                        Debug.Log("Activating Browse mode");
                        browseController.SetActive(true);
                    }
                    else
                    {
                        Debug.LogError("BrowseController is null!");
                    }
                    break;

                case AppMode.Build:
                    if (buildController != null)
                    {
                        Debug.Log("Activating Build mode");
                        buildController.SetActive(true);
                    }
                    else
                    {
                        Debug.LogError("BuildController is null!");
                    }
                    break;

                case AppMode.Home:
                    if (homeController != null)
                    {
                        Debug.Log("Activating Home mode");
                        homeController.SetActive(true);
                    }
                    else
                    {
                        Debug.LogError("HomeController is null!");
                    }
                    break;

                case AppMode.Play:
                    // Play mode is now a separate scene
                    Debug.LogWarning("AppMode.Play is deprecated - use Play scene instead");
                    break;
            }

            // Update UI
            if (uiRouter != null)
            {
                uiRouter.OnModeChanged(mode);
            }
            else
            {
                Debug.LogWarning("UIRouter is null!");
            }
        }

        [System.Obsolete("Use SceneManager.LoadScene(\"Play\") instead")]
        public void EnterPlayMode(string worldId)
        {
            // Deprecated: Play mode is now a separate scene
            Debug.LogWarning("EnterPlayMode is deprecated - use SceneTransitionData.SetWorldToPlay() and load Play scene");
            SceneTransitionData.SetWorldToPlay(worldId);
            UnityEngine.SceneManagement.SceneManager.LoadScene("Play");
        }

        [System.Obsolete("Play mode is now a separate scene")]
        public void ExitPlayMode()
        {
            // Deprecated: Play mode is now a separate scene
            Debug.LogWarning("ExitPlayMode is deprecated - use SceneManager to return to desired scene");
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
