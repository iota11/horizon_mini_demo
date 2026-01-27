using UnityEngine;

namespace HorizonMini.Build
{
    /// <summary>
    /// Camera controller for Build mode: orbit, pan, zoom
    /// Mobile touch-optimized
    /// </summary>
    public class BuildCameraController : MonoBehaviour
    {
        [Header("Settings")]
        public float orbitSpeed = 0.1f;
        public float panSpeed = 0.01f;
        public float zoomSpeed = 0.1f;
        public float minDistance = 5f;
        public float maxDistance = 50f;

        [Header("Smoothing")]
        public float smoothTime = 0.3f; // Time to smooth camera movement
        public bool enableSmoothing = true;

        private Camera cam;
        private Vector3 targetPosition = Vector3.zero;
        private float currentDistance = 20f;
        private float currentYaw = 45f;
        private float currentPitch = 30f;
        private bool isEnabled = false;

        // Smoothing variables
        private Vector3 targetPositionVelocity;
        private float distanceVelocity;
        private Vector3 desiredTargetPosition;
        private float desiredDistance;

        public void Initialize(Camera camera)
        {
            cam = camera;
            desiredTargetPosition = targetPosition;
            desiredDistance = currentDistance;
        }

        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
        }

        public float GetCurrentDistance()
        {
            return currentDistance;
        }

        public void SetDistance(float distance, bool immediate = false)
        {
            desiredDistance = Mathf.Clamp(distance, minDistance, maxDistance);

            if (immediate || !enableSmoothing)
            {
                currentDistance = desiredDistance;
                UpdateCameraPosition();
            }
        }

        /// <summary>
        /// Sync internal state from current camera position
        /// Useful after external code moves the camera
        /// </summary>
        public void SyncFromCurrentCamera()
        {
            if (cam == null) return;

            // Calculate current target position and distance from camera's actual position and rotation
            Vector3 camPos = cam.transform.position;
            Vector3 forward = cam.transform.forward;

            // Raycast to find what camera is looking at
            Ray ray = new Ray(camPos, forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f))
            {
                targetPosition = hit.point;
                desiredTargetPosition = hit.point;
                currentDistance = Vector3.Distance(camPos, hit.point);
                desiredDistance = currentDistance;
            }
            else
            {
                // No hit, estimate target from camera forward
                float estimatedDistance = currentDistance; // Keep current distance
                targetPosition = camPos + forward * estimatedDistance;
                desiredTargetPosition = targetPosition;
            }

            // Extract yaw and pitch from camera rotation
            Vector3 euler = cam.transform.eulerAngles;
            currentYaw = euler.y;
            currentPitch = euler.x;

            // Normalize pitch to -180 to 180 range
            if (currentPitch > 180f) currentPitch -= 360f;
        }

        public void FocusOnBounds(Bounds bounds)
        {
            targetPosition = bounds.center;
            currentDistance = bounds.size.magnitude * 1.5f;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
            UpdateCameraPosition();
        }

        /// <summary>
        /// Setup camera for maximum volume size - called once when Create button is clicked
        /// Smoothly moves camera to fit the volume
        /// </summary>
        public void SetupForMaxVolume(Vector3Int maxDimensions, float volumeSize, bool immediate = false)
        {
            // Calculate max volume bounds
            Vector3 maxSize = new Vector3(
                maxDimensions.x * volumeSize,
                maxDimensions.y * volumeSize,
                maxDimensions.z * volumeSize
            );
            Vector3 center = maxSize * 0.5f;

            // Set camera distance based on max size (ensure it fits)
            float maxBoundSize = maxSize.magnitude;
            desiredDistance = maxBoundSize * 1.5f;
            desiredDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);

            // Set target to center of max volume
            desiredTargetPosition = center;

            // Immediate update if requested or smoothing disabled
            if (immediate || !enableSmoothing)
            {
                currentDistance = desiredDistance;
                targetPosition = desiredTargetPosition;
                UpdateCameraPosition();
            }
        }

        /// <summary>
        /// Update target position without changing camera distance
        /// Smoothly moves camera to look at new target
        /// </summary>
        public void UpdateTarget(Vector3 newCenter, bool immediate = false)
        {
            desiredTargetPosition = newCenter;

            // Immediate update if requested or smoothing disabled
            if (immediate || !enableSmoothing)
            {
                targetPosition = desiredTargetPosition;
                UpdateCameraPosition();
            }
        }

        public void Orbit(Vector2 delta)
        {
            if (!isEnabled) return;

            currentYaw += delta.x * orbitSpeed;
            currentPitch -= delta.y * orbitSpeed;
            currentPitch = Mathf.Clamp(currentPitch, -89f, 89f);

            UpdateCameraPosition();
        }

        public void Pan(Vector2 delta)
        {
            if (!isEnabled) return;

            Vector3 right = cam.transform.right;
            Vector3 up = cam.transform.up;

            desiredTargetPosition -= right * delta.x * panSpeed * currentDistance;
            desiredTargetPosition -= up * delta.y * panSpeed * currentDistance;

            // Immediate update for responsive controls
            targetPosition = desiredTargetPosition;
            UpdateCameraPosition();
        }

        public void Zoom(float delta)
        {
            if (!isEnabled) return;

            desiredDistance -= delta * zoomSpeed;
            desiredDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);

            // Immediate update for responsive controls
            currentDistance = desiredDistance;
            UpdateCameraPosition();
        }

        private void UpdateCameraPosition()
        {
            if (cam == null) return;

            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
            Vector3 offset = rotation * (Vector3.back * currentDistance);

            cam.transform.position = targetPosition + offset;
            cam.transform.LookAt(targetPosition);
        }

        private void LateUpdate()
        {
            if (isEnabled && cam != null && enableSmoothing)
            {
                // Smooth interpolate to desired target and distance
                targetPosition = Vector3.SmoothDamp(targetPosition, desiredTargetPosition, ref targetPositionVelocity, smoothTime);
                currentDistance = Mathf.SmoothDamp(currentDistance, desiredDistance, ref distanceVelocity, smoothTime);

                UpdateCameraPosition();
            }
        }
    }
}
