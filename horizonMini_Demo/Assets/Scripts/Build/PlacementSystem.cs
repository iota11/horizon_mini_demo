using UnityEngine;
using HorizonMini.Controllers;
using System.Collections;

namespace HorizonMini.Build
{
    /// <summary>
    /// Handles object placement from catalog with drag-and-drop and snapping
    /// </summary>
    public class PlacementSystem : MonoBehaviour
    {
        [Header("Snapping")]
        public bool snapToGrid = true;
        public float gridSize = 0.5f; // 0.5m increments
        public bool snapToObject = false;

        [Header("Ghost Preview")]
        public Material ghostMaterial;

        [Header("Audio")]
        public AudioClip pickupSound;

        private BuildController buildController;
        private Camera cam;
        private bool isEnabled = false;

        // Dragging state
        private bool isDragging = false;
        private PlaceableAsset currentAsset;
        private GameObject ghostObject;
        private Vector3 lastValidPosition;
        private Bounds cachedGhostLocalBounds; // Cache local bounds to avoid recalculating

        // Audio
        private AudioSource audioSource;

        public void Initialize(BuildController controller, Camera camera)
        {
            buildController = controller;
            cam = camera;

            CreateGhostMaterial();

            // Setup audio source
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D sound for UI interaction
            }
        }

        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
        }

        private void CreateGhostMaterial()
        {
            if (ghostMaterial == null)
            {
                // Use URP shader instead of Standard
                Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
                if (urpShader == null)
                {
                    Debug.LogWarning("URP Lit shader not found! Falling back to Unlit/Transparent");
                    urpShader = Shader.Find("Unlit/Transparent");
                }

                ghostMaterial = new Material(urpShader);
                ghostMaterial.SetFloat("_Surface", 1); // Transparent mode
                ghostMaterial.SetFloat("_Blend", 0); // Alpha blending
                ghostMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                ghostMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                ghostMaterial.SetInt("_ZWrite", 0);
                ghostMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                ghostMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                ghostMaterial.renderQueue = 3000;

                // Set initial green color with transparency
                ghostMaterial.SetColor("_BaseColor", new Color(0, 1, 0, 0.5f));
            }
        }

        public void StartDragging(PlaceableAsset asset)
        {
            if (!isEnabled || asset == null || asset.prefab == null)
                return;

            currentAsset = asset;
            isDragging = true;

            // Play pickup sound
            PlayPickupSound();

            // Create ghost preview
            ghostObject = Instantiate(asset.prefab);
            ghostObject.name = "Ghost_" + asset.displayName;

            // Fix materials to URP first (prevent purple materials)
            FixMaterialsToURP(ghostObject);

            // Make materials transparent while preserving textures and colors
            Renderer[] renderers = ghostObject.GetComponentsInChildren<Renderer>();
            foreach (var rend in renderers)
            {
                Material[] originalMaterials = rend.sharedMaterials;
                Material[] ghostMaterials = new Material[originalMaterials.Length];

                for (int i = 0; i < originalMaterials.Length; i++)
                {
                    if (originalMaterials[i] != null)
                    {
                        // Create transparent version of original material
                        Material transparentMat = new Material(originalMaterials[i]);

                        // Make it transparent
                        transparentMat.SetFloat("_Surface", 1); // Transparent
                        transparentMat.SetFloat("_Blend", 0); // Alpha
                        transparentMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        transparentMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        transparentMat.SetInt("_ZWrite", 0);
                        transparentMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        transparentMat.renderQueue = 3000;

                        // Apply transparency to base color - support multiple shader types
                        if (transparentMat.HasProperty("_BaseColor"))
                        {
                            Color originalColor = transparentMat.GetColor("_BaseColor");
                            transparentMat.SetColor("_BaseColor", new Color(originalColor.r, originalColor.g, originalColor.b, 0.5f));
                        }
                        else if (transparentMat.HasProperty("_Color"))
                        {
                            Color originalColor = transparentMat.GetColor("_Color");
                            transparentMat.SetColor("_Color", new Color(originalColor.r, originalColor.g, originalColor.b, 0.5f));
                        }
                        else if (transparentMat.HasProperty("_MainColor"))
                        {
                            Color originalColor = transparentMat.GetColor("_MainColor");
                            transparentMat.SetColor("_MainColor", new Color(originalColor.r, originalColor.g, originalColor.b, 0.5f));
                        }

                        ghostMaterials[i] = transparentMat;
                    }
                }

                rend.sharedMaterials = ghostMaterials;
            }

            // Disable colliders
            Collider[] colliders = ghostObject.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            // Disable auto-update for SmartTerrainChunk ghost to prevent flickering
            SmartTerrainChunk chunk = ghostObject.GetComponent<SmartTerrainChunk>();
            if (chunk != null)
            {
                // Disable the component's Update loop by disabling auto-update
                // The chunks are already generated in Awake(), we don't want them to regenerate
                chunk.enabled = false;
            }

            // Cache local bounds to avoid recalculating world bounds during dragging
            CacheGhostLocalBounds();
        }

        public void UpdateDragging(Vector2 screenPosition)
        {
            if (!isDragging || ghostObject == null)
                return;

            // Raycast to find placement position
            Ray ray = cam.ScreenPointToRay(screenPosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Vector3 position = hit.point;

                // Check if raycast hit an existing placed object
                PlacedObject hitObject = hit.collider.GetComponentInParent<PlacedObject>();
                bool isSnappingToObject = (hitObject != null && snapToObject);

                // Apply snapping
                position = ApplySnapping(position, hit);

                // Update ghost position first for collision check
                ghostObject.transform.position = position;

                // Check bounds and collision
                bool isValid = true;
                if (buildController.VolumeGrid != null)
                {
                    // If snapping to an existing object, allow placement outside volume bounds
                    if (!isSnappingToObject)
                    {
                        // Check if within volume bounds only when placing on ground
                        isValid = buildController.VolumeGrid.IsPositionInBounds(position);
                    }

                    // Check for bbox collision with existing objects
                    if (isValid)
                    {
                        isValid = !IsCollidingWithObjects();
                    }

                    if (isValid)
                    {
                        lastValidPosition = position;
                    }

                    // Update material colors to show valid (green tint) or invalid (red tint)
                    Color tintColor = isValid ? new Color(0.5f, 1f, 0.5f, 0.5f) : new Color(1f, 0.5f, 0.5f, 0.5f);
                    Renderer[] renderers = ghostObject.GetComponentsInChildren<Renderer>();
                    foreach (var rend in renderers)
                    {
                        foreach (var mat in rend.sharedMaterials)
                        {
                            if (mat != null && mat.HasProperty("_BaseColor"))
                            {
                                Color originalColor = mat.GetColor("_BaseColor");
                                mat.SetColor("_BaseColor", new Color(
                                    originalColor.r * tintColor.r,
                                    originalColor.g * tintColor.g,
                                    originalColor.b * tintColor.b,
                                    0.5f // Keep transparency
                                ));
                            }
                        }
                    }
                }

                ghostObject.transform.position = position;
            }
        }

        public void EndDragging(Vector2 screenPosition)
        {
            if (!isDragging || ghostObject == null)
                return;

            // Place object at ghost's current position (already validated in UpdateDragging)
            Vector3 finalPosition = ghostObject.transform.position;
            buildController.PlaceObject(currentAsset, finalPosition);

            // Cleanup
            Destroy(ghostObject);

            isDragging = false;
            currentAsset = null;
        }

        public void CancelDragging()
        {
            if (ghostObject != null)
            {
                Destroy(ghostObject);
            }

            isDragging = false;
            currentAsset = null;
        }

        private Vector3 ApplySnapping(Vector3 position, RaycastHit hit)
        {
            if (snapToObject)
            {
                // Snap to object surface (using bbox)
                PlacedObject targetObj = hit.collider.GetComponentInParent<PlacedObject>();
                if (targetObj != null)
                {
                    Bounds bounds = GetObjectBounds(targetObj.gameObject);
                    position = SnapToObjectSurface(hit.point, bounds, hit.normal);
                    return position; // Return early - already snapped
                }
            }

            // If not snapping to object, snap to ground/volume using bbox
            // Calculate where pivot should be so bbox bottom sits on hit point
            position = SnapToGround(hit.point, hit.normal);

            if (snapToGrid)
            {
                // Snap to grid
                position = SnapToGrid(position);
            }

            return position;
        }

        private Vector3 SnapToGround(Vector3 hitPoint, Vector3 normal)
        {
            // Use cached local bounds instead of recalculating world bounds
            // This prevents flickering caused by bounds changing during dragging
            Vector3 pivotToCenter = cachedGhostLocalBounds.center;

            // For ground (typically normal = (0,1,0)), place bbox bottom on hit point
            Vector3 contactFaceOffset = new Vector3(0, -cachedGhostLocalBounds.extents.y, 0);

            // Calculate pivot position
            Vector3 pivotPosition = hitPoint - pivotToCenter - contactFaceOffset;

            return pivotPosition;
        }

        private Vector3 SnapToGrid(Vector3 position)
        {
            return new Vector3(
                Mathf.Round(position.x / gridSize) * gridSize,
                Mathf.Round(position.y / gridSize) * gridSize,
                Mathf.Round(position.z / gridSize) * gridSize
            );
        }

        private Vector3 SnapToObjectSurface(Vector3 hitPoint, Bounds targetBounds, Vector3 normal)
        {
            // Use cached local bounds instead of recalculating
            Vector3 pivotToCenter = cachedGhostLocalBounds.center;

            // Step 1: Find which face of ghost's bbox should contact the target
            // Based on raycast normal, determine the contact face
            Vector3 contactFaceOffset = Vector3.zero;

            // Normalize the normal to identify primary axis
            Vector3 absNormal = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));

            if (absNormal.y > absNormal.x && absNormal.y > absNormal.z)
            {
                // Vertical surface - Y axis
                if (normal.y > 0)
                {
                    // Target's top surface, ghost's bottom face contacts it
                    contactFaceOffset.y = -cachedGhostLocalBounds.extents.y; // Bottom face
                }
                else
                {
                    // Target's bottom surface, ghost's top face contacts it
                    contactFaceOffset.y = cachedGhostLocalBounds.extents.y; // Top face
                }
            }
            else if (absNormal.x > absNormal.z)
            {
                // X-axis surface
                if (normal.x > 0)
                {
                    // Target's right surface, ghost's left face contacts it
                    contactFaceOffset.x = -cachedGhostLocalBounds.extents.x; // Left face
                }
                else
                {
                    // Target's left surface, ghost's right face contacts it
                    contactFaceOffset.x = cachedGhostLocalBounds.extents.x; // Right face
                }
            }
            else
            {
                // Z-axis surface
                if (normal.z > 0)
                {
                    // Target's front surface, ghost's back face contacts it
                    contactFaceOffset.z = -cachedGhostLocalBounds.extents.z; // Back face
                }
                else
                {
                    // Target's back surface, ghost's front face contacts it
                    contactFaceOffset.z = cachedGhostLocalBounds.extents.z; // Front face
                }
            }

            // Step 2: Calculate where the contact face should be positioned
            // The contact face should be at the hit point on the target surface
            Vector3 contactFacePosition = hitPoint;

            // Step 3: Calculate pivot position from contact face position
            // Pivot = ContactFace - (PivotToCenter + ContactFaceOffset)
            Vector3 pivotPosition = contactFacePosition - pivotToCenter - contactFaceOffset;

            return pivotPosition;
        }

        private bool IsCollidingWithObjects()
        {
            if (ghostObject == null)
                return false;

            // Get ghost object's bounding box
            Bounds ghostBounds = GetObjectBounds(ghostObject);

            // Add small margin to prevent false positives from floating point errors
            float margin = 0.01f;
            ghostBounds.Expand(-margin * 2f); // Shrink bounds slightly

            // Find all placed objects
            PlacedObject[] placedObjects = FindObjectsByType<PlacedObject>(FindObjectsSortMode.None);

            foreach (PlacedObject placedObj in placedObjects)
            {
                if (placedObj == null || placedObj.gameObject == ghostObject)
                    continue;

                Bounds objBounds = GetObjectBounds(placedObj.gameObject);

                // Check if bounding boxes intersect (with margin)
                if (ghostBounds.Intersects(objBounds))
                {
                    return true; // Collision detected
                }
            }

            return false; // No collision
        }

        private Bounds GetObjectBounds(GameObject obj)
        {
            // Special handling for SmartTerrainChunk - check chunksContainer
            SmartTerrainChunk chunk = obj.GetComponent<SmartTerrainChunk>();
            if (chunk != null)
            {
                Transform chunksContainer = obj.transform.Find("ChunksContainer");
                if (chunksContainer != null && chunksContainer.childCount > 0)
                {
                    // Get bounds from generated chunks
                    Renderer[] chunkRenderers = chunksContainer.GetComponentsInChildren<Renderer>();
                    if (chunkRenderers.Length > 0)
                    {
                        Bounds combinedBounds = chunkRenderers[0].bounds;
                        for (int i = 1; i < chunkRenderers.Length; i++)
                        {
                            combinedBounds.Encapsulate(chunkRenderers[i].bounds);
                        }
                        return combinedBounds;
                    }
                }

                // Fallback: use BoxCollider if chunks not yet generated
                BoxCollider boxCollider = obj.GetComponent<BoxCollider>();
                if (boxCollider != null)
                {
                    return boxCollider.bounds;
                }
            }

            // Try to get combined bounds from all renderers
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds combinedBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    combinedBounds.Encapsulate(renderers[i].bounds);
                }
                return combinedBounds;
            }

            // Fallback to collider bounds
            Collider collider = obj.GetComponentInChildren<Collider>();
            if (collider != null)
            {
                return collider.bounds;
            }

            return new Bounds(obj.transform.position, Vector3.one);
        }

        public void SetSnapToGrid(bool enabled)
        {
            snapToGrid = enabled;
        }

        public void SetSnapToObject(bool enabled)
        {
            snapToObject = enabled;
        }

        /// <summary>
        /// Cache ghost object's local bounds to avoid recalculating during drag
        /// </summary>
        private void CacheGhostLocalBounds()
        {
            if (ghostObject == null)
            {
                cachedGhostLocalBounds = new Bounds(Vector3.zero, Vector3.one);
                return;
            }

            // Get world bounds first
            Bounds worldBounds = GetObjectBounds(ghostObject);

            // Convert to local bounds by transforming center and size to local space
            Vector3 localCenter = ghostObject.transform.InverseTransformPoint(worldBounds.center);

            // For local bounds size, we need to handle rotation
            // Calculate all 8 corners in world space, transform to local, then encapsulate
            Vector3 center = worldBounds.center;
            Vector3 extents = worldBounds.extents;

            Vector3[] worldCorners = new Vector3[8]
            {
                center + new Vector3(-extents.x, -extents.y, -extents.z),
                center + new Vector3(+extents.x, -extents.y, -extents.z),
                center + new Vector3(+extents.x, -extents.y, +extents.z),
                center + new Vector3(-extents.x, -extents.y, +extents.z),
                center + new Vector3(-extents.x, +extents.y, -extents.z),
                center + new Vector3(+extents.x, +extents.y, -extents.z),
                center + new Vector3(+extents.x, +extents.y, +extents.z),
                center + new Vector3(-extents.x, +extents.y, +extents.z)
            };

            Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);
            bool first = true;
            foreach (Vector3 worldCorner in worldCorners)
            {
                Vector3 localCorner = ghostObject.transform.InverseTransformPoint(worldCorner);
                if (first)
                {
                    localBounds = new Bounds(localCorner, Vector3.zero);
                    first = false;
                }
                else
                {
                    localBounds.Encapsulate(localCorner);
                }
            }

            cachedGhostLocalBounds = localBounds;
        }

        /// <summary>
        /// Fix materials to use URP shaders (prevent purple/pink materials)
        /// </summary>
        private void FixMaterialsToURP(GameObject obj)
        {
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLitShader == null)
            {
                Debug.LogWarning("URP Lit shader not found!");
                return;
            }

            // Get all renderers in object and children
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0)
                    continue;

                Material[] newMaterials = new Material[renderer.sharedMaterials.Length];

                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    Material oldMat = renderer.sharedMaterials[i];

                    if (oldMat == null)
                    {
                        newMaterials[i] = oldMat;
                        continue;
                    }

                    // Check if material is using wrong shader (pink/purple material)
                    bool needsFix = oldMat.shader == null ||
                                   oldMat.shader.name.Contains("Standard") ||
                                   oldMat.shader.name.Contains("Legacy") ||
                                   oldMat.shader.name == "Hidden/InternalErrorShader";

                    if (needsFix)
                    {
                        // Create new material with URP shader
                        Material newMat = new Material(urpLitShader);

                        // Preserve color
                        if (oldMat.HasProperty("_Color"))
                        {
                            newMat.SetColor("_BaseColor", oldMat.color);
                        }
                        else if (oldMat.HasProperty("_BaseColor"))
                        {
                            newMat.SetColor("_BaseColor", oldMat.GetColor("_BaseColor"));
                        }

                        // Preserve texture
                        if (oldMat.HasProperty("_MainTex") && oldMat.mainTexture != null)
                        {
                            newMat.SetTexture("_MainTex", oldMat.mainTexture);
                        }

                        newMat.name = oldMat.name + "_URP";
                        newMaterials[i] = newMat;
                    }
                    else
                    {
                        // Material is fine, keep it
                        newMaterials[i] = oldMat;
                    }
                }

                renderer.sharedMaterials = newMaterials;
            }
        }

        private void PlayPickupSound()
        {
            if (audioSource == null) return;

            if (pickupSound != null)
            {
                // Use custom sound
                audioSource.PlayOneShot(pickupSound, 0.5f);
            }
            else
            {
                // Generate procedural "pickup" sound (ascending chirp)
                StartCoroutine(GeneratePickupSound());
            }
        }

        private System.Collections.IEnumerator GeneratePickupSound()
        {
            float duration = 0.15f; // 150ms
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(duration * sampleRate);
            AudioClip clip = AudioClip.Create("PickupSound", sampleCount, 1, sampleRate, false);

            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                // Ascending frequency sweep (chirp up)
                float startFreq = 400f;
                float endFreq = 800f;
                float freq = Mathf.Lerp(startFreq, endFreq, progress);

                // Quick attack, medium decay envelope
                float envelope = Mathf.Exp(-t * 8f) * (1f - Mathf.Exp(-t * 50f));

                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.4f;
            }

            clip.SetData(samples, 0);
            audioSource.PlayOneShot(clip, 0.5f);

            yield return new WaitForSeconds(duration);
            Destroy(clip);
        }
    }
}
