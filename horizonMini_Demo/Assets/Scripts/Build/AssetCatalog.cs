using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace HorizonMini.Build
{
    /// <summary>
    /// Manages the library of placeable assets, organized by category
    /// </summary>
    [CreateAssetMenu(fileName = "AssetCatalog", menuName = "HorizonMini/AssetCatalog")]
    public class AssetCatalog : ScriptableObject
    {
        [SerializeField] private List<PlaceableAsset> allAssets = new List<PlaceableAsset>();

        public List<PlaceableAsset> GetAllAssets()
        {
            // Filter out null assets
            return allAssets.Where(a => a != null).ToList();
        }

        public List<PlaceableAsset> GetAssetsByCategory(AssetCategory category)
        {
            // Filter out null assets
            return allAssets.Where(a => a != null && a.category == category).ToList();
        }

        public PlaceableAsset GetAssetById(string assetId)
        {
            // Filter out null assets
            return allAssets.Find(a => a != null && a.assetId == assetId);
        }

        public List<AssetCategory> GetAvailableCategories()
        {
            // Return all categories that have at least one asset (filter out null assets)
            var categoriesWithAssets = allAssets.Where(a => a != null).Select(a => a.category).Distinct().ToList();

            // Sort categories in logical order
            categoriesWithAssets.Sort((a, b) => ((int)a).CompareTo((int)b));

            return categoriesWithAssets;
        }

        /// <summary>
        /// Get all possible categories (even if no assets assigned yet)
        /// </summary>
        public List<AssetCategory> GetAllCategories()
        {
            return System.Enum.GetValues(typeof(AssetCategory)).Cast<AssetCategory>().ToList();
        }

        public void AddAsset(PlaceableAsset asset)
        {
            if (!allAssets.Contains(asset))
            {
                allAssets.Add(asset);
            }
        }

        public void RemoveAsset(PlaceableAsset asset)
        {
            allAssets.Remove(asset);
        }
    }
}
