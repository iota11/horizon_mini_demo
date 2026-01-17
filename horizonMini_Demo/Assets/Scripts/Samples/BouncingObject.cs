using UnityEngine;

namespace HorizonMini.Samples
{
    /// <summary>
    /// Bouncing animation for demo worlds
    /// </summary>
    public class BouncingObject : MonoBehaviour
    {
        [SerializeField] private float bounceHeight = 2f;
        [SerializeField] private float bounceSpeed = 2f;

        private Vector3 startPosition;
        private float time;

        private void Start()
        {
            startPosition = transform.localPosition;
        }

        private void Update()
        {
            time += Time.deltaTime * bounceSpeed;
            float yOffset = Mathf.Abs(Mathf.Sin(time)) * bounceHeight;
            transform.localPosition = startPosition + new Vector3(0, yOffset, 0);
        }
    }
}
