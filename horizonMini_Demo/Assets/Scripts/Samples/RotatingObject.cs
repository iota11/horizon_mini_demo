using UnityEngine;

namespace HorizonMini.Samples
{
    /// <summary>
    /// Simple rotating animation for demo worlds
    /// </summary>
    public class RotatingObject : MonoBehaviour
    {
        [SerializeField] private Vector3 rotationSpeed = new Vector3(0, 45, 0);
        [SerializeField] private bool useLocalSpace = true;

        private void Update()
        {
            if (useLocalSpace)
            {
                transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);
            }
            else
            {
                transform.Rotate(rotationSpeed * Time.deltaTime, Space.World);
            }
        }
    }
}
