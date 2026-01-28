using UnityEngine;

namespace HorizonMini.MiniGames
{
    /// <summary>
    /// Marker component for mini games in Build mode
    /// This gets saved as part of the World and converted to MiniGameData
    /// </summary>
    public class MiniGameMarker : MonoBehaviour
    {
        public string gameType = "CubeStack";
        public string gameName = "Stack Game";
    }
}
