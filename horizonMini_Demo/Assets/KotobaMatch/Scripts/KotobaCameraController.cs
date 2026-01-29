using UnityEngine;

/// <summary>
/// Controls the camera for the KotobaMatch game.
/// Sets up orthographic isometric view looking at the stage.
/// </summary>
public class KotobaCameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Vector3 cameraPosition = new Vector3(10f, 10f, 10f);
    [SerializeField] private float orthographicSize = 6f;
    [SerializeField] private Vector3 lookAtTarget = Vector3.zero;

    [Header("References")]
    [SerializeField] private Camera gameCamera;

    private KotobaTheme theme;

    private void Awake()
    {
        // Auto-find camera
        if (gameCamera == null)
            gameCamera = GetComponentInChildren<Camera>();

        if (gameCamera == null)
            gameCamera = GetComponent<Camera>();
    }

    /// <summary>
    /// Initialize the camera with theme settings
    /// </summary>
    public void Initialize(KotobaTheme cardTheme)
    {
        theme = cardTheme;
        SetupCamera();

        if (theme != null)
        {
            SetBackgroundColor(theme.backgroundColor);
        }
    }

    /// <summary>
    /// Setup the camera as orthographic with isometric view
    /// </summary>
    public void SetupCamera()
    {
        if (gameCamera == null) return;

        // Set position
        gameCamera.transform.position = cameraPosition;

        // Look at target
        gameCamera.transform.LookAt(lookAtTarget);

        // Setup orthographic
        gameCamera.orthographic = true;
        gameCamera.orthographicSize = orthographicSize;

        // Near/far clip
        gameCamera.nearClipPlane = 0.1f;
        gameCamera.farClipPlane = 100f;
    }

    /// <summary>
    /// Set the camera background color
    /// </summary>
    public void SetBackgroundColor(Color color)
    {
        if (gameCamera != null)
        {
            gameCamera.backgroundColor = color;
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
        }
    }

    /// <summary>
    /// Get the camera reference
    /// </summary>
    public Camera GetCamera()
    {
        return gameCamera;
    }

    /// <summary>
    /// Set the camera reference
    /// </summary>
    public void SetCamera(Camera camera)
    {
        gameCamera = camera;
        SetupCamera();
    }
}
