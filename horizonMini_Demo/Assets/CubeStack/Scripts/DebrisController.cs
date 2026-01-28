using UnityEngine;

public class DebrisController : MonoBehaviour
{
    private float _gravity;
    private float _despawnY;
    private Vector3 _velocity;
    private Vector3 _angularVelocity;
    private bool _initialized;

    public void Initialize(float gravity, float despawnY, Vector3 velocity, Vector3 angularVelocity)
    {
        _gravity = gravity;
        _despawnY = despawnY;
        _velocity = velocity;
        _angularVelocity = angularVelocity;
        _initialized = true;

        // Darken material to distinguish from placed blocks
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            // Create material instance to avoid modifying shared material
            Material mat = new Material(renderer.material);
            Color c = mat.color;
            mat.color = new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f, c.a);
            renderer.material = mat;
        }
    }

    private void Update()
    {
        if (!_initialized) return;

        // Apply gravity
        _velocity.y -= _gravity * Time.deltaTime;

        // Update position and rotation
        transform.position += _velocity * Time.deltaTime;
        transform.Rotate(_angularVelocity * Time.deltaTime * 100f);

        // Despawn check
        if (transform.position.y < _despawnY)
        {
            Destroy(gameObject);
        }
    }
}
