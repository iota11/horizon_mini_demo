using UnityEngine;
using System.Collections.Generic;

namespace HorizonMini.Build
{
    /// <summary>
    /// Singleton manager for all SmartHouse instances in the scene
    /// </summary>
    public class SmartHouseManager : MonoBehaviour
    {
        private static SmartHouseManager instance;
        public static SmartHouseManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<SmartHouseManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("SmartHouseManager");
                        instance = go.AddComponent<SmartHouseManager>();
                    }
                }
                return instance;
            }
        }

        private List<SmartHouse> registeredHouses = new List<SmartHouse>();
        private SmartHouse activeHouse = null;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            // Hide from hierarchy to avoid cleanup warnings
            gameObject.hideFlags = HideFlags.HideAndDontSave;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        /// <summary>
        /// Register a SmartHouse instance
        /// </summary>
        public void RegisterHouse(SmartHouse house)
        {
            if (!registeredHouses.Contains(house))
            {
                registeredHouses.Add(house);
                Debug.Log($"[SmartHouseManager] Registered house: {house.name}");
            }
        }

        /// <summary>
        /// Unregister a SmartHouse instance
        /// </summary>
        public void UnregisterHouse(SmartHouse house)
        {
            if (registeredHouses.Contains(house))
            {
                registeredHouses.Remove(house);
                Debug.Log($"[SmartHouseManager] Unregistered house: {house.name}");
            }

            if (activeHouse == house)
            {
                activeHouse = null;
            }
        }

        /// <summary>
        /// Set the active house (for editing)
        /// </summary>
        public void SetActiveHouse(SmartHouse house)
        {
            // Hide previous active house control points
            if (activeHouse != null && activeHouse != house)
            {
                activeHouse.SetControlPointsVisible(false);
            }

            activeHouse = house;

            // Show new active house control points
            if (activeHouse != null)
            {
                activeHouse.SetControlPointsVisible(true);
            }
        }

        /// <summary>
        /// Get the currently active house
        /// </summary>
        public SmartHouse GetActiveHouse()
        {
            return activeHouse;
        }

        /// <summary>
        /// Clear active house
        /// </summary>
        public void ClearActiveHouse()
        {
            if (activeHouse != null)
            {
                activeHouse.SetControlPointsVisible(false);
                activeHouse = null;
            }
        }

        /// <summary>
        /// Get all registered houses
        /// </summary>
        public List<SmartHouse> GetAllHouses()
        {
            return new List<SmartHouse>(registeredHouses);
        }
    }
}
