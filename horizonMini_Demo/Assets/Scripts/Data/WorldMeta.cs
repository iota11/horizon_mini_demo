using UnityEngine;
using System;

namespace HorizonMini.Data
{
    /// <summary>
    /// Metadata for a world (used in library and UI)
    /// </summary>
    [Serializable]
    public class WorldMeta
    {
        public string id;
        public string title;
        public string author;
        public Sprite thumbnail; // Optional
        public string worldDataPath; // Path to WorldData asset or JSON

        public WorldMeta()
        {
            id = Guid.NewGuid().ToString();
            title = "Untitled World";
            author = "Anonymous";
        }

        public WorldMeta(string title, string author)
        {
            id = Guid.NewGuid().ToString();
            this.title = title;
            this.author = author;
        }
    }
}
