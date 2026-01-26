using UnityEngine;
using HorizonMini.Data;

namespace HorizonMini.MiniGames
{
    /// <summary>
    /// Marks a mini game instance as a preview (pre-loaded but not active)
    /// </summary>
    public class MiniGamePreview : MonoBehaviour
    {
        public MiniGameData gameData;
        public bool isActive = false;
    }
}
