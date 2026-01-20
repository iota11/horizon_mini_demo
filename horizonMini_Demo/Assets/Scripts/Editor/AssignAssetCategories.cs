using UnityEngine;
using UnityEditor;
using HorizonMini.Build;
using System.Collections.Generic;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Editor utility to automatically assign categories to PlaceableAssets based on naming patterns
    /// </summary>
    public class AssignAssetCategories
    {
        [MenuItem("HorizonMini/Assign Asset Categories")]
        public static void AssignCategories()
        {
            // Find all PlaceableAsset objects
            string[] guids = AssetDatabase.FindAssets("t:PlaceableAsset");
            int updatedCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PlaceableAsset asset = AssetDatabase.LoadAssetAtPath<PlaceableAsset>(path);

                if (asset != null)
                {
                    AssetCategory newCategory = DetermineCategory(asset.assetId);

                    if (asset.category != newCategory)
                    {
                        asset.category = newCategory;
                        EditorUtility.SetDirty(asset);
                        updatedCount++;
                        Debug.Log($"Updated {asset.assetId}: {newCategory}");
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=green>Category assignment complete! Updated {updatedCount} assets.</color>");
        }

        private static AssetCategory DetermineCategory(string assetId)
        {
            string lower = assetId.ToLower();

            // ===== CHARACTERS =====
            if (lower.Contains("jungle_boy") || lower.Contains("jungle_girl") ||
                lower.Contains("jungle_monster"))
            {
                return AssetCategory.Characters;
            }

            // ===== EFFECTS =====
            if (lower.Contains("fire") && !lower.Contains("weapon"))
            {
                return AssetCategory.Effects;
            }

            // ===== WEAPONS =====
            if (lower.Contains("weapon_"))
            {
                return AssetCategory.Weapons;
            }

            // ===== TOOLS (cauldron, drum, trap) =====
            if (lower.Contains("cauldron") || lower.Contains("drum") ||
                lower.Contains("trap_"))
            {
                return AssetCategory.Tools;
            }

            // ===== DECORATION (bones, skulls, piles) =====
            if (lower.Contains("bone") || lower.Contains("skull") ||
                lower.Contains("grasstop") || lower.Contains("leaves_pile"))
            {
                return AssetCategory.Decoration;
            }

            // ===== DOORS =====
            if (lower.Contains("door_"))
            {
                return AssetCategory.Doors;
            }

            // ===== STAIRS =====
            if (lower.Contains("stair_"))
            {
                return AssetCategory.Stairs;
            }

            // ===== RAILS =====
            if (lower.Contains("rail"))
            {
                return AssetCategory.Rails;
            }

            // ===== BLOCKS (stone blocks) =====
            if (lower.Contains("block_"))
            {
                return AssetCategory.Blocks;
            }

            // ===== TREES =====
            if (lower.Contains("tree") || lower.Contains("banana_stamm") ||
                lower.Contains("banana_tree") || lower.Contains("palm_tree") ||
                lower.Contains("fern_tree") || lower.Contains("sample_big_tree") ||
                lower.Contains("stump"))
            {
                return AssetCategory.Trees;
            }

            // ===== BUSHES =====
            if (lower.Contains("bush"))
            {
                return AssetCategory.Bushes;
            }

            // ===== FLOWERS =====
            if (lower.Contains("flower") || lower.Contains("dandelion") ||
                lower.Contains("heliconia") || lower.Contains("lotus_flower") ||
                lower.Contains("philodendron") || lower.Contains("oval_leave"))
            {
                return AssetCategory.Flowers;
            }

            // ===== GRASS =====
            if (lower.Contains("grass") || lower.Contains("tall_grass"))
            {
                return AssetCategory.Grass;
            }

            // ===== VINES (vines, leaves, branches, logs) =====
            if (lower.Contains("vine") || lower.Contains("leaf") ||
                lower.Contains("leaves") || lower.Contains("branch") ||
                lower.Contains("log") || lower.Contains("lotus_leaf"))
            {
                return AssetCategory.Vines;
            }

            // Default to Trees for unknown jungle assets
            return AssetCategory.Trees;
        }
    }
}
