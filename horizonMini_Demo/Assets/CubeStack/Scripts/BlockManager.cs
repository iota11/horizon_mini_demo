using UnityEngine;
using System.Collections.Generic;

public class BlockManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private GameObject debrisPrefab;

    [Header("Pooling")]
    [SerializeField] private int initialPoolSize = 50;
    [SerializeField] private int maxDebris = 20;

    [Header("Physics")]
    [SerializeField] private float debrisGravity = 15f;
    [SerializeField] private float debrisDespawnY = -20f;

    private Queue<GameObject> _blockPool = new Queue<GameObject>();
    private List<DebrisController> _activeDebris = new List<DebrisController>();
    private Transform _towerContainer;
    private Transform _debrisContainer;
    private Material _blockMaterial;

    private void Awake()
    {
        _towerContainer = new GameObject("TowerContainer").transform;
        _towerContainer.SetParent(transform);

        _debrisContainer = new GameObject("DebrisContainer").transform;
        _debrisContainer.SetParent(transform);
    }

    public void Initialize(Material material)
    {
        _blockMaterial = material;

        // Create block prefab if not assigned
        if (blockPrefab == null)
        {
            blockPrefab = CreateBlockPrimitive();
        }

        if (debrisPrefab == null)
        {
            debrisPrefab = CreateDebrisPrimitive();
        }

        // Pre-warm pool
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject block = CreateBlockInstance();
            block.SetActive(false);
            _blockPool.Enqueue(block);
        }
    }

    private GameObject CreateBlockPrimitive()
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "BlockPrefab";
        cube.SetActive(false);
        cube.transform.SetParent(transform);

        if (_blockMaterial != null)
        {
            cube.GetComponent<Renderer>().material = _blockMaterial;
        }

        return cube;
    }

    private GameObject CreateDebrisPrimitive()
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "DebrisPrefab";
        cube.AddComponent<DebrisController>();
        cube.SetActive(false);
        cube.transform.SetParent(transform);

        if (_blockMaterial != null)
        {
            cube.GetComponent<Renderer>().material = _blockMaterial;
        }

        return cube;
    }

    private GameObject CreateBlockInstance()
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = "Block";

        if (_blockMaterial != null)
        {
            block.GetComponent<Renderer>().material = new Material(_blockMaterial);
        }

        return block;
    }

    public void CreateBase(float size, StackTheme theme)
    {
        GameObject baseBlock = GetFromPool();
        baseBlock.name = "BaseBlock";
        baseBlock.transform.localScale = new Vector3(size, 10f, size);
        baseBlock.transform.position = new Vector3(0, -5f, 0);

        var renderer = baseBlock.GetComponent<Renderer>();
        if (renderer != null && theme != null)
        {
            renderer.material = new Material(_blockMaterial);
            renderer.material.color = theme.blockColorGradient.Evaluate(0);
        }

        baseBlock.SetActive(true);
        baseBlock.transform.SetParent(_towerContainer);
    }

    public GameObject SpawnBlock(Vector3 position, float width, float depth, float height, Color color)
    {
        GameObject block = GetFromPool();
        block.name = "ActiveBlock";
        block.transform.position = position;
        block.transform.localScale = new Vector3(width, height, depth);

        var renderer = block.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (renderer.material == null || !renderer.material.name.Contains("Instance"))
            {
                renderer.material = new Material(_blockMaterial);
            }
            renderer.material.color = color;
        }

        block.SetActive(true);
        block.transform.SetParent(_towerContainer);

        return block;
    }

    public void TrimBlock(GameObject block, float newWidth, float newDepth, float height)
    {
        block.transform.localScale = new Vector3(newWidth, height, newDepth);
    }

    public void CreateDebris(Vector3 position, float width, float depth, float height, Color color)
    {
        // Enforce debris limit
        CleanupDebris();
        if (_activeDebris.Count >= maxDebris)
        {
            var oldest = _activeDebris[0];
            _activeDebris.RemoveAt(0);
            if (oldest != null) Destroy(oldest.gameObject);
        }

        GameObject debris = GameObject.CreatePrimitive(PrimitiveType.Cube);
        debris.name = "Debris";
        debris.transform.position = position;
        debris.transform.localScale = new Vector3(width, height, depth);
        debris.transform.SetParent(_debrisContainer);

        var renderer = debris.GetComponent<Renderer>();
        if (renderer != null && _blockMaterial != null)
        {
            renderer.material = new Material(_blockMaterial);
            renderer.material.color = color;
        }

        var controller = debris.AddComponent<DebrisController>();
        controller.Initialize(
            debrisGravity,
            debrisDespawnY,
            Random.insideUnitSphere * 2f,
            Random.insideUnitSphere * 3f
        );

        _activeDebris.Add(controller);
    }

    public void ConvertToDebris(GameObject block)
    {
        if (block == null) return;

        block.transform.SetParent(_debrisContainer);
        block.name = "Debris";

        var controller = block.AddComponent<DebrisController>();
        controller.Initialize(
            debrisGravity,
            debrisDespawnY,
            Vector3.zero,
            Random.insideUnitSphere * 3f
        );

        _activeDebris.Add(controller);
    }

    public void ClearAll()
    {
        // Return tower blocks to pool
        List<Transform> children = new List<Transform>();
        foreach (Transform child in _towerContainer)
        {
            children.Add(child);
        }

        foreach (var child in children)
        {
            var debrisCtrl = child.GetComponent<DebrisController>();
            if (debrisCtrl != null)
            {
                Destroy(debrisCtrl);
            }
            child.gameObject.SetActive(false);
            child.SetParent(transform);
            _blockPool.Enqueue(child.gameObject);
        }

        // Destroy debris
        CleanupDebris();
        foreach (var debris in _activeDebris)
        {
            if (debris != null) Destroy(debris.gameObject);
        }
        _activeDebris.Clear();
    }

    private void CleanupDebris()
    {
        _activeDebris.RemoveAll(d => d == null);
    }

    private GameObject GetFromPool()
    {
        if (_blockPool.Count > 0)
        {
            return _blockPool.Dequeue();
        }
        return CreateBlockInstance();
    }

    public void SetBlockPrefab(GameObject prefab)
    {
        blockPrefab = prefab;
    }

    public void SetDebrisPrefab(GameObject prefab)
    {
        debrisPrefab = prefab;
    }
}
