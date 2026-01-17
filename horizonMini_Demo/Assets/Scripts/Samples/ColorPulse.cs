using UnityEngine;

namespace HorizonMini.Samples
{
    /// <summary>
    /// Pulsing color animation for demo worlds
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class ColorPulse : MonoBehaviour
    {
        [SerializeField] private Color colorA = Color.red;
        [SerializeField] private Color colorB = Color.blue;
        [SerializeField] private float pulseSpeed = 1f;

        private Renderer rend;
        private MaterialPropertyBlock propBlock;
        private float time;

        private void Start()
        {
            rend = GetComponent<Renderer>();
            propBlock = new MaterialPropertyBlock();
        }

        private void Update()
        {
            time += Time.deltaTime * pulseSpeed;
            float t = (Mathf.Sin(time) + 1f) * 0.5f; // Oscillate between 0 and 1

            Color currentColor = Color.Lerp(colorA, colorB, t);

            propBlock.SetColor("_BaseColor", currentColor);
            propBlock.SetColor("_Color", currentColor); // Fallback for built-in pipeline
            rend.SetPropertyBlock(propBlock);
        }
    }
}
