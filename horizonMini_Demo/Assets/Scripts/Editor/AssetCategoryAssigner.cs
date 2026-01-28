using UnityEngine;
using UnityEditor;
using HorizonMini.Build;
using System.Collections.Generic;
using System.Linq;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Uses LLM-based logic to assign categories to PlaceableAssets based on their names
    /// </summary>
    public class AssetCategoryAssigner
    {
        [MenuItem("HorizonMini/Assign Asset Categories (LLM-based)")]
        public static void AssignCategories()
        {
            // Find all PlaceableAsset files
            string[] guids = AssetDatabase.FindAssets("t:PlaceableAsset");

            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("Info", "No PlaceableAsset files found!", "OK");
                return;
            }

            int assignedCount = 0;
            int skippedCount = 0;

            EditorUtility.DisplayProgressBar("Assigning Categories", "Processing assets...", 0f);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                PlaceableAsset asset = AssetDatabase.LoadAssetAtPath<PlaceableAsset>(path);

                if (asset == null || asset.prefab == null)
                {
                    skippedCount++;
                    continue;
                }

                // Get asset name (from prefab or displayName)
                string assetName = asset.prefab.name;
                if (string.IsNullOrEmpty(assetName))
                {
                    assetName = asset.displayName;
                }

                // Assign category using LLM-based logic
                AssetCategory oldCategory = asset.category;
                AssetCategory newCategory = DetermineCategory(assetName);

                if (oldCategory != newCategory)
                {
                    asset.category = newCategory;
                    EditorUtility.SetDirty(asset);
                    assignedCount++;
                    Debug.Log($"[AssetCategoryAssigner] {assetName}: {oldCategory} → {newCategory}");
                }
                else
                {
                    skippedCount++;
                }

                EditorUtility.DisplayProgressBar(
                    "Assigning Categories",
                    $"Processing {i + 1}/{guids.Length}: {assetName}",
                    (float)i / guids.Length
                );
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Category Assignment Complete",
                $"✓ Assigned: {assignedCount}\n" +
                $"○ Skipped: {skippedCount}\n" +
                $"Total: {guids.Length}",
                "OK"
            );

            Debug.Log($"<color=green>✓ Category assignment complete! Assigned: {assignedCount}, Skipped: {skippedCount}</color>");
        }

        /// <summary>
        /// LLM-based category determination logic
        /// Uses pattern matching and semantic understanding
        /// </summary>
        private static AssetCategory DetermineCategory(string assetName)
        {
            string name = assetName.ToLower();

            // 1. Buildings & Structures
            if (ContainsAny(name, "wall", "window", "door", "beam", "base", "stair", "partition",
                "molding", "porch", "barrier", "billboard", "brick", "roof", "fence"))
            {
                return AssetCategory.Buildings;
            }

            // 2. Nature
            if (ContainsAny(name, "tree", "cherry", "palm", "grass", "shrub", "flower", "plant",
                "mushroom", "cloud", "cactus", "fern"))
            {
                return AssetCategory.Nature;
            }

            // 3. Terrain
            if (ContainsAny(name, "parquet", "tile", "road", "ground", "rock", "sand", "dirt",
                "snow", "lava", "stone", "wood", "metal") &&
                !ContainsAny(name, "table", "chair", "desk", "shelf", "door", "spike"))
            {
                return AssetCategory.Terrain;
            }

            // 4. Furniture
            if (ContainsAny(name, "chair", "armchair", "couch", "sofa", "table", "desk", "bed",
                "cabinet", "cupboard", "shelf", "dresser", "nightstand", "chest", "stool",
                "ottoman", "mirror", "bath", "toilet", "washbasin", "washstand"))
            {
                return AssetCategory.Furniture;
            }

            // 5. Decorations
            if (ContainsAny(name, "lamp", "chandelier", "lantern", "light", "candle", "carpet",
                "rug", "painting", "picture", "photo", "frame", "decoration", "vase", "pillow",
                "curtain", "blind", "macrame", "flag", "neon", "clock", "bust", "gnome",
                "mannequin", "shield"))
            {
                return AssetCategory.Decorations;
            }

            // 6. Functional Items
            if (ContainsAny(name, "box", "storage", "basket", "trashcan", "fridge", "tv",
                "monitor", "washer", "stove", "electrical", "socket", "switch", "iron", "mixer",
                "kitchen_set", "pot", "saucepan", "pan", "kettle", "teapot", "telephone",
                "cassette", "radio", "boombox", "jukebox", "ladder", "hanger", "mailbox",
                "pipe", "barrel", "crate", "mine_rail"))
            {
                return AssetCategory.FunctionalItems;
            }

            // 7. Tools & Weapons
            if (ContainsAny(name, "knife", "fork", "spoon", "spatula", "cutting_board", "camera",
                "video", "comb", "mirror", "toothbrush", "cannon", "tower_defense", "mining",
                "weapon", "sword", "tool"))
            {
                return AssetCategory.Tools;
            }

            // 8. Characters
            if (ContainsAny(name, "dummy", "totem", "character", "npc", "player", "girl", "boy"))
            {
                return AssetCategory.Characters;
            }

            // 9. Farming
            if (ContainsAny(name, "farming", "farm", "crop", "herb", "wheat"))
            {
                return AssetCategory.Farming;
            }

            // 10. WholeSet (complete room scenes)
            if (ContainsAny(name, "room_", "demo_interiors") || name.StartsWith("room"))
            {
                return AssetCategory.WholeSet;
            }

            // 11. SmartBuilder
            if (ContainsAny(name, "smartterrain", "smartwall", "smarthouse", "smartbuilder"))
            {
                return AssetCategory.SmartBuilder;
            }

            // 12. Special Effects (catch-all for everything else)
            // Food, drinks, toys, books, office supplies, christmas, powerups, etc.
            if (ContainsAny(name, "apple", "bread", "cake", "pizza", "food", "drink", "wine",
                "cup", "plate", "toy_", "book", "magazine", "vinyl", "audio", "game", "chess",
                "ball", "tennis", "skateboard", "roller", "computer", "keyboard", "mouse",
                "pen", "paper", "necklace", "perfume", "soap", "sock", "shoe", "headphone",
                "fire", "spider", "garland", "christmas", "powerup", "effect"))
            {
                return AssetCategory.SpecialEffects;
            }

            // Default fallback
            Debug.LogWarning($"[AssetCategoryAssigner] Could not determine category for: {assetName}, defaulting to Decorations");
            return AssetCategory.Decorations;
        }

        /// <summary>
        /// Helper method to check if name contains any of the keywords
        /// </summary>
        private static bool ContainsAny(string name, params string[] keywords)
        {
            foreach (string keyword in keywords)
            {
                if (name.Contains(keyword))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Generate report of category distribution
        /// </summary>
        [MenuItem("HorizonMini/Report Asset Category Distribution")]
        public static void ReportCategoryDistribution()
        {
            string[] guids = AssetDatabase.FindAssets("t:PlaceableAsset");

            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("Info", "No PlaceableAsset files found!", "OK");
                return;
            }

            Dictionary<AssetCategory, int> distribution = new Dictionary<AssetCategory, int>();
            Dictionary<AssetCategory, List<string>> examples = new Dictionary<AssetCategory, List<string>>();

            foreach (var category in System.Enum.GetValues(typeof(AssetCategory)))
            {
                distribution[(AssetCategory)category] = 0;
                examples[(AssetCategory)category] = new List<string>();
            }

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PlaceableAsset asset = AssetDatabase.LoadAssetAtPath<PlaceableAsset>(path);

                if (asset != null)
                {
                    distribution[asset.category]++;
                    if (examples[asset.category].Count < 5)
                    {
                        examples[asset.category].Add(asset.displayName);
                    }
                }
            }

            string report = "=== Asset Category Distribution ===\n\n";

            foreach (var kvp in distribution.OrderByDescending(x => x.Value))
            {
                report += $"{kvp.Key}: {kvp.Value} assets\n";
                if (examples[kvp.Key].Count > 0)
                {
                    report += $"  Examples: {string.Join(", ", examples[kvp.Key])}\n";
                }
                report += "\n";
            }

            report += $"Total: {guids.Length} assets";

            Debug.Log(report);

            EditorUtility.DisplayDialog("Category Distribution", report, "OK");
        }
    }
}
