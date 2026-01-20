using UnityEngine;
using UnityEditor;
using HorizonMini.Build;
using System.Collections.Generic;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Smart asset categorizer using semantic analysis
    /// Intelligently assigns categories based on asset naming patterns and context
    /// </summary>
    public class SmartAssetCategorizer
    {
        [MenuItem("HorizonMini/Smart Assign Asset Categories")]
        public static void SmartAssignCategories()
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
                    AssetCategory newCategory = DetermineCategoryIntelligently(asset.assetId);

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
            Debug.Log($"<color=green>Smart category assignment complete! Updated {updatedCount} assets.</color>");
        }

        private static AssetCategory DetermineCategoryIntelligently(string assetId)
        {
            string lower = assetId.ToLower();

            // ===== CHARACTERS (highest priority - living beings) =====
            if (IsCharacter(lower))
                return AssetCategory.Characters;

            // ===== EFFECTS (visual/audio effects) =====
            if (IsEffect(lower))
                return AssetCategory.Effects;

            // ===== TERRAIN (natural ground features) =====
            if (IsTerrain(lower))
                return AssetCategory.Terrain;

            // ===== VEGETATION (all plants and organic growth) =====
            if (IsVegetation(lower))
                return AssetCategory.Vegetation;

            // ===== WALLS (vertical barriers and enclosures) =====
            if (IsWall(lower))
                return AssetCategory.Walls;

            // ===== ROOFS (covering structures) =====
            if (IsRoof(lower))
                return AssetCategory.Roofs;

            // ===== STRUCTURES (architectural building blocks) =====
            if (IsStructure(lower))
                return AssetCategory.Structures;

            // ===== FURNITURE (functional interior/exterior objects) =====
            if (IsFurniture(lower))
                return AssetCategory.Furniture;

            // ===== TOOLS (weapons, tools, interactive items) =====
            if (IsTool(lower))
                return AssetCategory.Tools;

            // ===== DECORATIONS (pure aesthetic items) =====
            if (IsDecoration(lower))
                return AssetCategory.Decorations;

            // Default fallback
            return AssetCategory.Decorations;
        }

        private static bool IsCharacter(string name)
        {
            // Living creatures, people, monsters
            string[] patterns = {
                "boy", "girl", "monster", "character", "npc", "enemy",
                "villager", "hero", "player", "creature", "animal",
                "knight", "mage", "warrior", "dragon", "zombie"
            };
            return ContainsAny(name, patterns);
        }

        private static bool IsEffect(string name)
        {
            // Visual and audio effects
            string[] patterns = {
                "fire", "flame", "smoke", "particle", "spark", "glow",
                "explosion", "magic", "effect", "bubble", "steam",
                "lightning", "beam", "aura", "trail", "dust",
                "cloud" // clouds are effects/sky elements
            };

            // Exclude if it's a weapon with fire
            if (name.Contains("weapon"))
                return false;

            return ContainsAny(name, patterns);
        }

        private static bool IsTerrain(string name)
        {
            // Natural ground, rocks, water bodies
            string[] patterns = {
                "terrain", "ground", "dirt", "soil", "sand", "gravel",
                "rock", "boulder", "stone", "cliff", "mountain",
                "water", "river", "lake", "pond", "ocean",
                "hill", "valley", "crater", "cave", "crystal"
            };

            // Exclude blocks (those are structures)
            if (name.Contains("block"))
                return false;

            return ContainsAny(name, patterns);
        }

        private static bool IsVegetation(string name)
        {
            // All organic plant life
            string[] patterns = {
                // Trees
                "tree", "palm", "oak", "pine", "birch", "trunk", "stump",
                "banana_stamm", "banana_tree", "fern_tree", "big_tree",

                // Bushes and shrubs
                "bush", "shrub", "hedge",

                // Flowers and plants
                "flower", "rose", "dandelion", "tulip", "orchid", "lotus",
                "heliconia", "philodendron", "plant", "fern",

                // Grass and ground cover
                "grass", "tall_grass", "lawn", "turf", "moss",

                // Vines and foliage
                "vine", "ivy", "leaf", "leaves", "foliage", "branch",
                "log", "twig", "root", "bamboo", "reed", "cane",

                // Crops and farming
                "crop", "wheat", "corn", "carrot", "potato", "pumpkin",
                "tomato", "berry", "fruit",

                // Other organic
                "cactus", "succulent", "algae", "seaweed", "mushroom"
            };

            return ContainsAny(name, patterns);
        }

        private static bool IsWall(string name)
        {
            // Vertical barriers, fences, railings
            string[] patterns = {
                "fence", "rail", "railing", "barrier", "wall",
                "gate", "palisade", "barricade", "hedge_wall",
                "pole" // fence poles
            };

            // Exclude windows and doors (those are structures)
            if (name.Contains("window") || name.Contains("door"))
                return false;

            return ContainsAny(name, patterns);
        }

        private static bool IsRoof(string name)
        {
            // Covering structures for buildings
            string[] patterns = {
                "roof", "thatch", "shingle", "tile_roof", "awning",
                "canopy", "overhang", "eave", "gable", "ridge"
            };

            return ContainsAny(name, patterns);
        }

        private static bool IsStructure(string name)
        {
            // Architectural building components
            string[] patterns = {
                // Basic blocks
                "block_", "cube", "brick",

                // Doors and windows
                "door", "window", "hatch", "doorframe", "sill",

                // Stairs and levels
                "stair", "step", "ladder", "ramp",

                // Structural elements
                "beam", "column", "pillar", "post", "support",
                "foundation", "floor", "ceiling", "wall_corner",

                // Barn/building specific
                "barn_beam", "barn_wall", "barn_cube"
            };

            return ContainsAny(name, patterns);
        }

        private static bool IsFurniture(string name)
        {
            // Interior and exterior functional objects
            string[] patterns = {
                // Seating
                "chair", "bench", "stool", "throne", "couch", "sofa",

                // Tables and surfaces
                "table", "desk", "counter", "shelf", "rack",

                // Storage
                "chest", "box", "crate", "barrel", "cabinet", "drawer",
                "cupboard", "wardrobe", "closet",

                // Beds
                "bed", "cot", "hammock", "mattress",

                // Lighting (functional)
                "lantern", "lamp", "torch_holder", "chandelier",

                // Kitchen
                "oven", "stove", "sink", "fridge",

                // Other furniture
                "mirror", "painting_frame", "bookshelf", "rug", "carpet"
            };

            return ContainsAny(name, patterns);
        }

        private static bool IsTool(string name)
        {
            // Weapons, tools, and interactive items
            string[] patterns = {
                // Weapons
                "weapon", "sword", "axe", "bow", "arrow", "spear",
                "dagger", "mace", "hammer", "staff", "wand",
                "gun", "rifle", "pistol", "cannon", "bullet",

                // Tools
                "tool", "pickaxe", "shovel", "hoe", "rake",
                "saw", "drill", "wrench",

                // Interactive items
                "trap", "drum", "cauldron", "lever", "switch",
                "button", "chain", "rope", "key", "lock",

                // Projectiles
                "cannonball", "projectile", "ammo"
            };

            return ContainsAny(name, patterns);
        }

        private static bool IsDecoration(string name)
        {
            // Pure decorative items with no function
            string[] patterns = {
                // Bones and remains
                "bone", "skull", "skeleton", "ribcage", "jaw",

                // Ornaments
                "ornament", "decoration", "deco", "ornate",
                "bauble", "trinket", "statue", "figurine",

                // Piles and clutter
                "pile", "heap", "stack", "clutter",
                "leaves_pile", "grasstop",

                // Holiday/festive
                "candle", "banner", "flag", "ribbon",

                // Signs and markers
                "sign", "marker", "signpost",

                // Miscellaneous decorative
                "vase", "pot_deco", "basket", "jar",
                "crystal_deco", "gem_deco"
            };

            // Special case: candles as decoration, not lighting
            if (name.Contains("candle"))
                return true;

            return ContainsAny(name, patterns);
        }

        private static bool ContainsAny(string text, string[] patterns)
        {
            foreach (string pattern in patterns)
            {
                if (text.Contains(pattern))
                    return true;
            }
            return false;
        }
    }
}
