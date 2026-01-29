using UnityEngine;

namespace HorizonMini.Debugging
{
    /// <summary>
    /// Simple test script to verify mouse scroll wheel input is working
    /// Attach this to any GameObject to test
    /// </summary>
    public class ScrollWheelTest : MonoBehaviour
    {
        private void Update()
        {
            // Try to get scroll wheel input
            try
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.001f)
                {
                    UnityEngine.Debug.Log($"<color=cyan>[ScrollWheelTest] SCROLL DETECTED! Value: {scroll}</color>");
                }
            }
            catch (System.ArgumentException e)
            {
                if (Time.frameCount % 60 == 0)
                {
                    UnityEngine.Debug.LogError($"[ScrollWheelTest] Mouse ScrollWheel axis not configured: {e.Message}");
                }
            }

            // Also try mouse scroll delta (new input system style)
            Vector2 scrollDelta = Input.mouseScrollDelta;
            if (scrollDelta.magnitude > 0.001f)
            {
                UnityEngine.Debug.Log($"<color=green>[ScrollWheelTest] SCROLL DELTA DETECTED! Value: {scrollDelta}</color>");
            }
        }
    }
}
