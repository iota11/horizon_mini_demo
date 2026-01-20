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
                lower.Contains("jungle_monster") || lower.Contains("character"))
            {
                return AssetCategory.Characters;
            }

            // ===== EFFECTS =====
            if (lower.Contains("fire") && !lower.Contains("weapon"))
            {
                return AssetCategory.Effects;
            }

            // ===== WALLS (rails, fences, walls) =====
            if (lower.Contains("rail") || lower.Contains("fence") ||
                lower.Contains("wall") || lower.Contains("barrier"))
            {
                return AssetCategory.Walls;
            }

            // ===== ROOFS (roof structures) =====
            if (lower.Contains("roof") || lower.Contains("thatch") ||
                lower.Contains("shingle"))
            {
                return AssetCategory.Roofs;
            }

            // ===== STRUCTURES (blocks, doors, stairs) =====
            if (lower.Contains("block_") || lower.Contains("door_") ||
                lower.Contains("stair_") || lower.Contains("column") ||
                lower.Contains("pillar"))
            {
                return AssetCategory.Structures;
            }

            // ===== TERRAIN (ground, rocks, water, natural terrain) =====
            if (lower.Contains("terrain") || lower.Contains("ground") ||
                lower.Contains("rock") || lower.Contains("stone") && !lower.Contains("block") ||
                lower.Contains("water") || lower.Contains("cliff"))
            {
                return AssetCategory.Terrain;
            }

            // ===== FURNITURE (tables, chairs, beds, etc.) =====
            if (lower.Contains("table") || lower.Contains("chair") ||
                lower.Contains("bed") || lower.Contains("bench") ||
                lower.Contains("shelf") || lower.Contains("cabinet"))
            {
                return AssetCategory.Furniture;
            }

            // ===== DECORATIONS (bones, skulls, piles - pure decorative) =====
            if (lower.Contains("bone") || lower.Contains("skull") ||
                lower.Contains("grasstop") || lower.Contains("leaves_pile") ||
                lower.Contains("decoration") || lower.Contains("ornament"))
            {
                return AssetCategory.Decorations;
            }

            // ===== TOOLS (weapons, tools, cauldron, drum, trap) =====
            if (lower.Contains("weapon_") || lower.Contains("cauldron") ||
                lower.Contains("drum") || lower.Contains("trap_") ||
                lower.Contains("tool") || lower.Contains("axe") ||
                lower.Contains("hammer") || lower.Contains("spear"))
            {
                return AssetCategory.Tools;
            }

            // ===== VEGETATION (trees, bushes, flowers, grass, vines, leaves) =====
            if (lower.Contains("tree") || lower.Contains("banana_stamm") ||
                lower.Contains("banana_tree") || lower.Contains("palm_tree") ||
                lower.Contains("fern_tree") || lower.Contains("sample_big_tree") ||
                lower.Contains("stump") || lower.Contains("bush") ||
                lower.Contains("flower") || lower.Contains("dandelion") ||
                lower.Contains("heliconia") || lower.Contains("lotus_flower") ||
                lower.Contains("philodendron") || lower.Contains("oval_leave") ||
                lower.Contains("grass") || lower.Contains("tall_grass") ||
                lower.Contains("vine") || lower.Contains("leaf") ||
                lower.Contains("leaves") || lower.Contains("branch") ||
                lower.Contains("log") || lower.Contains("lotus_leaf") ||
                lower.Contains("plant") || lower.Contains("fern"))
            {
                return AssetCategory.Vegetation;
            }

            // Default to Vegetation for unknown jungle assets
            return AssetCategory.Vegetation;
        }
    }
}
