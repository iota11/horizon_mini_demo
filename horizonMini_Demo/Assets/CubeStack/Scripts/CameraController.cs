using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float followSpeed = 0.05f;
    [SerializeField] private float heightOffset = 5f;
    [SerializeField] private Vector3 cameraOffset = new Vector3(15, 20, 15);

    private float _targetHeight;
    private Camera _camera;
    private bool _isShaking;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        if (_camera == null)
        {
            _camera = gameObject.AddComponent<Camera>();
        }

        // Setup orthographic camera
        _camera.orthographic = true;
        _camera.orthographicSize = 10f;
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = new Color(0.5f, 0.7f, 1f);

        // Initial position
        transform.position = cameraOffset;
        transform.LookAt(Vector3.zero);
    }

    public void SetTargetHeight(float height)
    {
        _targetHeight = height + heightOffset;
    }

    public void ResetCamera()
    {
        _targetHeight = heightOffset;
        transform.position = cameraOffset;
        transform.LookAt(Vector3.zero);
    }

    private void LateUpdate()
    {
        if (_isShaking) return;

        // Smooth follow
        Vector3 targetPos = cameraOffset;
        targetPos.y = _targetHeight + cameraOffset.y - heightOffset;

        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed);

        // Look at tower top
        transform.LookAt(new Vector3(0, _targetHeight - heightOffset, 0));
    }

    public void Shake(float duration, float intensity)
    {
        // Only shake if the GameObject is active (can't start coroutine on inactive object)
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(ShakeCoroutine(duration, intensity));
        }
    }

    private IEnumerator ShakeCoroutine(float duration, float intensity)
    {
        _isShaking = true;
        Vector3 originalPos = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;
            float z = Random.Range(-1f, 1f) * intensity;

            transform.position = originalPos + new Vector3(x, y, z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPos;
        _isShaking = false;
    }
}
