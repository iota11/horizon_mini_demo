using UnityEngine;

namespace HorizonMini.Build
{
    /// <summary>
    /// Represents the volume grid boundaries (the "room" space)
    /// </summary>
    public class VolumeGrid : MonoBehaviour
    {
        [Header("Grid Configuration")]
        public Vector3Int volumeDimensions = new Vector3Int(2, 1, 2); // X, Y, Z in volumes
        public float volumeSize = 8f; // Each volume is 8x8x8 units

        [Header("Visualization")]
        public Material boundsMaterial;
        public bool showBounds = true;
        [SerializeField] private Texture2D gridTexture; // Optional: custom grid texture
        [SerializeField] private float gridScale = 1.0f; // Cells per meter

        private BoxCollider floorCollider;
        private GameObject boundsVisual;

        public void Initialize(Vector3Int dimensions)
        {
            volumeDimensions = dimensions;
            CreateBounds();
            CreateFloorCollider();
        }

        private void CreateBounds()
        {
            // Create visual bounds
            if (boundsVisual != null)
                Destroy(boundsVisual);

            boundsVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boundsVisual.name = "VolumeBounds";
            boundsVisual.transform.SetParent(transform);
            boundsVisual.transform.localPosition = GetCenter();

            Vector3 size = GetWorldSize();
            boundsVisual.transform.localScale = size;

            // Setup material
            Renderer renderer = boundsVisual.GetComponent<Renderer>();
            if (boundsMaterial != null)
            {
                // Use provided material (clone it to avoid modifying the asset)
                renderer.material = new Material(boundsMaterial);
            }
            else
            {
                // Create default grid material
                // Try custom lit shader first, then fallback to unlit
                Shader shader = Shader.Find("HorizonMini/VolumeGridLit_URP");
                if (shader == null)
                    shader = Shader.Find("HorizonMini/VolumeGridLit");
                if (shader == null)
                    shader = Shader.Find("HorizonMini/VolumeGrid_URP");
                if (shader == null)
                    shader = Shader.Find("HorizonMini/VolumeGrid");
                if (shader == null)
                    shader = Shader.Find("Standard");

                Material mat = new Material(shader);

                // Setup grid material
                if (shader.name.Contains("VolumeGrid"))
                {
                    if (gridTexture != null)
                    {
                        mat.SetTexture("_MainTex", gridTexture);
                    }
                    mat.SetFloat("_GridScale", gridScale);
                    mat.SetColor("_Color", Color.white);

                    // Setup lighting properties if available
                    if (mat.HasProperty("_Smoothness"))
                        mat.SetFloat("_Smoothness", 0.5f);
                    if (mat.HasProperty("_Glossiness"))
                        mat.SetFloat("_Glossiness", 0.5f);
                    if (mat.HasProperty("_Metallic"))
                        mat.SetFloat("_Metallic", 0.0f);
                    if (mat.HasProperty("_OcclusionStrength"))
                        mat.SetFloat("_OcclusionStrength", 1.0f);
                }

                renderer.material = mat;
            }

            // Update material properties based on volume size
            UpdateMaterialProperties(renderer.material);

            // Remove collider from visual bounds
            Destroy(boundsVisual.GetComponent<Collider>());

            boundsVisual.SetActive(showBounds);
        }

        private void CreateFloorCollider()
        {
            // Destroy old floor if exists
            if (floorCollider != null)
            {
                Destroy(floorCollider.gameObject);
            }

            // Create a floor plane collider for placement raycasting
            GameObject floor = new GameObject("Floor");
            floor.transform.SetParent(transform);
            floor.transform.localPosition = Vector3.zero;
            floor.layer = LayerMask.NameToLayer("Default");

            floorCollider = floor.AddComponent<BoxCollider>();
            Vector3 size = GetWorldSize();
            floorCollider.size = new Vector3(size.x, 0.1f, size.z);
            floorCollider.center = new Vector3(size.x * 0.5f, -0.05f, size.z * 0.5f);
        }

        public Vector3 GetWorldSize()
        {
            return new Vector3(
                volumeDimensions.x * volumeSize,
                volumeDimensions.y * volumeSize,
                volumeDimensions.z * volumeSize
            );
        }

        public Vector3 GetCenter()
        {
            Vector3 size = GetWorldSize();
            return size * 0.5f;
        }

        public Bounds GetBounds()
        {
            return new Bounds(GetCenter(), GetWorldSize());
        }

        public bool IsPositionInBounds(Vector3 worldPosition)
        {
            return GetBounds().Contains(worldPosition);
        }

        private void UpdateMaterialProperties(Material mat)
        {
            if (mat == null)
                return;

            // Update grid scale based on volume size if using custom grid shader
            if (mat.HasProperty("_GridScale"))
            {
                mat.SetFloat("_GridScale", gridScale);
            }

            // Update texture if set
            if (gridTexture != null && mat.HasProperty("_MainTex"))
            {
                mat.SetTexture("_MainTex", gridTexture);
            }
        }

        // Public method to update grid settings at runtime
        public void SetGridTexture(Texture2D texture)
        {
            gridTexture = texture;
            if (boundsVisual != null)
            {
                Renderer renderer = boundsVisual.GetComponent<Renderer>();
                if (renderer != null)
                {
                    UpdateMaterialProperties(renderer.material);
                }
            }
        }

        public void SetGridScale(float scale)
        {
            gridScale = scale;
            if (boundsVisual != null)
            {
                Renderer renderer = boundsVisual.GetComponent<Renderer>();
                if (renderer != null)
                {
                    UpdateMaterialProperties(renderer.material);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (showBounds)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(GetCenter(), GetWorldSize());
            }
        }
    }
}
