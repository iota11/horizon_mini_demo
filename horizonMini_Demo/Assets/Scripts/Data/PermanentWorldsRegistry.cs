using UnityEngine;
using System.Collections.Generic;

namespace HorizonMini.Data
{
    /// <summary>
    /// Registry of permanent worlds that cannot be deleted by users
    /// </summary>
    [CreateAssetMenu(fileName = "PermanentWorldsRegistry", menuName = "HorizonMini/Permanent Worlds Registry")]
    public class PermanentWorldsRegistry : ScriptableObject
    {
        [Header("Permanent World IDs")]
        [Tooltip("List of world IDs that are protected from deletion")]
        public List<string> permanentWorldIds = new List<string>();

        [Header("Asset Paths")]
        [Tooltip("Corresponding asset paths for permanent worlds")]
        public List<string> permanentWorldPaths = new List<string>();

        /// <summary>
        /// Check if a world is marked as permanent
        /// </summary>
        public bool IsPermanent(string worldId)
        {
            return permanentWorldIds.Contains(worldId);
        }

        /// <summary>
        /// Add a world to permanent list
        /// </summary>
        public void AddPermanentWorld(string worldId, string assetPath = "")
        {
            if (!permanentWorldIds.Contains(worldId))
            {
                permanentWorldIds.Add(worldId);
                permanentWorldPaths.Add(assetPath);
            }
        }

        /// <summary>
        /// Remove a world from permanent list (editor only)
        /// </summary>
        public void RemovePermanentWorld(string worldId)
        {
            int index = permanentWorldIds.IndexOf(worldId);
            if (index >= 0)
            {
                permanentWorldIds.RemoveAt(index);
                if (index < permanentWorldPaths.Count)
                {
                    permanentWorldPaths.RemoveAt(index);
                }
            }
        }

        /// <summary>
        /// Get asset path for a permanent world
        /// </summary>
        public string GetAssetPath(string worldId)
        {
            int index = permanentWorldIds.IndexOf(worldId);
            if (index >= 0 && index < permanentWorldPaths.Count)
            {
                return permanentWorldPaths[index];
            }
            return null;
        }
    }
}
