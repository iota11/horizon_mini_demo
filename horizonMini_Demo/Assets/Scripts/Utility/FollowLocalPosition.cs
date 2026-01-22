using UnityEngine;

namespace HorizonMini.Utility
{
    /// <summary>
    /// Follows another object's local position with multipliers
    /// Works in both Editor and Play mode
    /// Each axis can reference a different target object
    /// </summary>
    [ExecuteAlways] // Runs in both Edit and Play mode
    public class FollowLocalPosition : MonoBehaviour
    {
        [Header("Targets (per axis)")]
        [Tooltip("Target object for X axis (if null, uses main target)")]
        [SerializeField] private Transform targetX;

        [Tooltip("Target object for Y axis (if null, uses main target)")]
        [SerializeField] private Transform targetY;

        [Tooltip("Target object for Z axis (if null, uses main target)")]
        [SerializeField] private Transform targetZ;

        [Header("Fallback Target")]
        [Tooltip("Fallback target if individual axis targets are not set")]
        [SerializeField] private Transform target;

        [Header("Anchor (Reference Point)")]
        [Tooltip("Anchor transform to calculate relative positions from. If null, uses absolute local positions.")]
        [SerializeField] private Transform anchor;

        [Header("Multipliers")]
        [Tooltip("Multiplier for X axis")]
        [SerializeField] private float multiplierX = 1f;

        [Tooltip("Multiplier for Y axis")]
        [SerializeField] private float multiplierY = 1f;

        [Tooltip("Multiplier for Z axis")]
        [SerializeField] private float multiplierZ = 1f;

        [Header("Offsets")]
        [Tooltip("Offset to add after multiplying X")]
        [SerializeField] private float offsetX = 0f;

        [Tooltip("Offset to add after multiplying Y")]
        [SerializeField] private float offsetY = 0f;

        [Tooltip("Offset to add after multiplying Z")]
        [SerializeField] private float offsetZ = 0f;

        [Header("Options")]
        [Tooltip("Update every frame (disable for performance if only manual updates needed)")]
        [SerializeField] private bool updateEveryFrame = true;

        private Vector3 lastTargetXLocalPosition;
        private Vector3 lastTargetYLocalPosition;
        private Vector3 lastTargetZLocalPosition;
        private Vector3 lastAnchorLocalPosition;

        private void Start()
        {
            InitializeLastPositions();
            UpdatePosition();
        }

        private void Update()
        {
            if (!updateEveryFrame)
                return;

            // Check if any target position changed
            bool needsUpdate = false;

            Transform actualTargetX = targetX != null ? targetX : target;
            Transform actualTargetY = targetY != null ? targetY : target;
            Transform actualTargetZ = targetZ != null ? targetZ : target;

            if (actualTargetX != null && actualTargetX.localPosition != lastTargetXLocalPosition)
            {
                lastTargetXLocalPosition = actualTargetX.localPosition;
                needsUpdate = true;
            }

            if (actualTargetY != null && actualTargetY.localPosition != lastTargetYLocalPosition)
            {
                lastTargetYLocalPosition = actualTargetY.localPosition;
                needsUpdate = true;
            }

            if (actualTargetZ != null && actualTargetZ.localPosition != lastTargetZLocalPosition)
            {
                lastTargetZLocalPosition = actualTargetZ.localPosition;
                needsUpdate = true;
            }

            // Check if anchor position changed
            if (anchor != null && anchor.localPosition != lastAnchorLocalPosition)
            {
                lastAnchorLocalPosition = anchor.localPosition;
                needsUpdate = true;
            }

            if (needsUpdate)
            {
                UpdatePosition();
            }
        }

        private void InitializeLastPositions()
        {
            Transform actualTargetX = targetX != null ? targetX : target;
            Transform actualTargetY = targetY != null ? targetY : target;
            Transform actualTargetZ = targetZ != null ? targetZ : target;

            if (actualTargetX != null)
                lastTargetXLocalPosition = actualTargetX.localPosition;

            if (actualTargetY != null)
                lastTargetYLocalPosition = actualTargetY.localPosition;

            if (actualTargetZ != null)
                lastTargetZLocalPosition = actualTargetZ.localPosition;

            if (anchor != null)
                lastAnchorLocalPosition = anchor.localPosition;
        }

        /// <summary>
        /// Manually update position based on target(s)
        /// Formula: result = anchor + ((target - anchor) * multiplier + offset)
        /// If anchor is null: result = target * multiplier + offset
        /// </summary>
        public void UpdatePosition()
        {
            // Get actual target for each axis (individual target or fallback)
            Transform actualTargetX = targetX != null ? targetX : target;
            Transform actualTargetY = targetY != null ? targetY : target;
            Transform actualTargetZ = targetZ != null ? targetZ : target;

            // Validate at least one target is set
            if (actualTargetX == null && actualTargetY == null && actualTargetZ == null)
            {
                Debug.LogWarning($"[FollowLocalPosition] No targets assigned on {gameObject.name}", this);
                return;
            }

            // Get anchor position (if set)
            Vector3 anchorPos = anchor != null ? anchor.localPosition : Vector3.zero;

            // Calculate new local position for each axis
            // Formula: anchor + ((target - anchor) * multiplier + offset)
            float newX, newY, newZ;

            if (actualTargetX != null)
            {
                float relativeX = anchor != null ? (actualTargetX.localPosition.x - anchorPos.x) : actualTargetX.localPosition.x;
                newX = (anchor != null ? anchorPos.x : 0f) + (relativeX * multiplierX + offsetX);
            }
            else
            {
                newX = transform.localPosition.x;
            }

            if (actualTargetY != null)
            {
                float relativeY = anchor != null ? (actualTargetY.localPosition.y - anchorPos.y) : actualTargetY.localPosition.y;
                newY = (anchor != null ? anchorPos.y : 0f) + (relativeY * multiplierY + offsetY);
            }
            else
            {
                newY = transform.localPosition.y;
            }

            if (actualTargetZ != null)
            {
                float relativeZ = anchor != null ? (actualTargetZ.localPosition.z - anchorPos.z) : actualTargetZ.localPosition.z;
                newZ = (anchor != null ? anchorPos.z : 0f) + (relativeZ * multiplierZ + offsetZ);
            }
            else
            {
                newZ = transform.localPosition.z;
            }

            Vector3 newLocalPos = new Vector3(newX, newY, newZ);
            transform.localPosition = newLocalPos;
        }

        /// <summary>
        /// Set the fallback target (used when individual axis targets are not set)
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            InitializeLastPositions();
            UpdatePosition();
        }

        /// <summary>
        /// Set target for X axis
        /// </summary>
        public void SetTargetX(Transform newTarget)
        {
            targetX = newTarget;
            InitializeLastPositions();
            UpdatePosition();
        }

        /// <summary>
        /// Set target for Y axis
        /// </summary>
        public void SetTargetY(Transform newTarget)
        {
            targetY = newTarget;
            InitializeLastPositions();
            UpdatePosition();
        }

        /// <summary>
        /// Set target for Z axis
        /// </summary>
        public void SetTargetZ(Transform newTarget)
        {
            targetZ = newTarget;
            InitializeLastPositions();
            UpdatePosition();
        }

        /// <summary>
        /// Set individual targets for each axis
        /// </summary>
        public void SetTargets(Transform x, Transform y, Transform z)
        {
            targetX = x;
            targetY = y;
            targetZ = z;
            InitializeLastPositions();
            UpdatePosition();
        }

        /// <summary>
        /// Set the anchor (reference point for relative positioning)
        /// </summary>
        public void SetAnchor(Transform newAnchor)
        {
            anchor = newAnchor;
            UpdatePosition();
        }

        /// <summary>
        /// Set multipliers
        /// </summary>
        public void SetMultipliers(float x, float y, float z)
        {
            multiplierX = x;
            multiplierY = y;
            multiplierZ = z;
            UpdatePosition();
        }

        /// <summary>
        /// Set offsets
        /// </summary>
        public void SetOffsets(float x, float y, float z)
        {
            offsetX = x;
            offsetY = y;
            offsetZ = z;
            UpdatePosition();
        }

        /// <summary>
        /// Set multiplier for specific axis
        /// </summary>
        public void SetMultiplierX(float value)
        {
            multiplierX = value;
            UpdatePosition();
        }

        public void SetMultiplierY(float value)
        {
            multiplierY = value;
            UpdatePosition();
        }

        public void SetMultiplierZ(float value)
        {
            multiplierZ = value;
            UpdatePosition();
        }

        /// <summary>
        /// Set offset for specific axis
        /// </summary>
        public void SetOffsetX(float value)
        {
            offsetX = value;
            UpdatePosition();
        }

        public void SetOffsetY(float value)
        {
            offsetY = value;
            UpdatePosition();
        }

        public void SetOffsetZ(float value)
        {
            offsetZ = value;
            UpdatePosition();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Update immediately in editor when values change
            if (target != null && Application.isPlaying == false)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null && target != null)
                    {
                        UpdatePosition();
                    }
                };
            }
        }
#endif
    }
}
