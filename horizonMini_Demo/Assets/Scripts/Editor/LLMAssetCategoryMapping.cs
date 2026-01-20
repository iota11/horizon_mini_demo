using UnityEngine;
using UnityEditor;
using HorizonMini.Build;
using System.Collections.Generic;

namespace HorizonMini.Editor
{
    /// <summary>
    /// LLM-generated intelligent asset category mappings
    /// Analyzed and categorized by Claude AI based on semantic understanding
    /// </summary>
    public class LLMAssetCategoryMapping
    {
        [MenuItem("HorizonMini/Apply LLM Asset Categories")]
        public static void ApplyLLMCategories()
        {
            Dictionary<string, AssetCategory> categoryMap = GetLLMCategoryMapping();

            string[] guids = AssetDatabase.FindAssets("t:PlaceableAsset");
            int updatedCount = 0;
            int notFoundCount = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PlaceableAsset asset = AssetDatabase.LoadAssetAtPath<PlaceableAsset>(path);

                if (asset != null)
                {
                    if (categoryMap.TryGetValue(asset.assetId, out AssetCategory newCategory))
                    {
                        if (asset.category != newCategory)
                        {
                            asset.category = newCategory;
                            EditorUtility.SetDirty(asset);
                            updatedCount++;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Asset not in LLM mapping: {asset.assetId}");
                        notFoundCount++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"<color=green>LLM category assignment complete!</color>");
            Debug.Log($"Updated: {updatedCount} | Not found in mapping: {notFoundCount}");
        }

        private static Dictionary<string, AssetCategory> GetLLMCategoryMapping()
        {
            var map = new Dictionary<string, AssetCategory>();

            // ========== VEGETATION (Trees, bushes, flowers, grass, crops, vines) ==========
            // Trees
            map["_example_tree"] = AssetCategory.Vegetation;
            map["_example_hollow_log"] = AssetCategory.Vegetation;
            map["banana_stamm_LOD_group"] = AssetCategory.Vegetation;
            map["banana_stamm_top_LOD_group"] = AssetCategory.Vegetation;
            map["banana_tree_big_LOD_group"] = AssetCategory.Vegetation;
            map["banana_tree_leafs_LOD_group"] = AssetCategory.Vegetation;
            map["banana_tree_small_LOD_group"] = AssetCategory.Vegetation;
            map["big_tree_foliage_group_LOD_group"] = AssetCategory.Vegetation;
            map["big_tree_foliage_single_LOD_group"] = AssetCategory.Vegetation;
            map["fern_tree_big_LOD_group"] = AssetCategory.Vegetation;
            map["fern_tree_small_LOD_group"] = AssetCategory.Vegetation;
            map["palm_1"] = AssetCategory.Vegetation;
            map["palm_2"] = AssetCategory.Vegetation;
            map["palm_leaf"] = AssetCategory.Vegetation;
            map["palm_leaf_end"] = AssetCategory.Vegetation;
            map["palm_leaf_start"] = AssetCategory.Vegetation;
            map["palm_tree_small_LOD_group"] = AssetCategory.Vegetation;
            map["palm_tree_small_red_LOD_group"] = AssetCategory.Vegetation;
            map["palm_trunk_block"] = AssetCategory.Vegetation;
            map["sample_big_tree_1"] = AssetCategory.Vegetation;
            map["sample_big_tree_2"] = AssetCategory.Vegetation;
            map["sample_hollow_log"] = AssetCategory.Vegetation;
            map["stump_LOD_group"] = AssetCategory.Vegetation;
            map["treetrunk_block"] = AssetCategory.Vegetation;
            map["treetrunk_block_small"] = AssetCategory.Vegetation;

            // Bushes
            map["bush_dead_deco"] = AssetCategory.Vegetation;
            map["bush_square_big_flower_LOD_group"] = AssetCategory.Vegetation;
            map["bush_square_big_leaves_LOD_group"] = AssetCategory.Vegetation;
            map["bush_square_big_plain_LOD_group"] = AssetCategory.Vegetation;
            map["bush_square_small_flower_LOD_group"] = AssetCategory.Vegetation;
            map["bush_square_small_leaves_LOD_group"] = AssetCategory.Vegetation;
            map["bush_square_small_plain_LOD_group"] = AssetCategory.Vegetation;
            map["bushes_small_orange_berries_LOD_group"] = AssetCategory.Vegetation;
            map["bushes_small_plain_LOD_group"] = AssetCategory.Vegetation;
            map["bushes_small_purple_berries_LOD_group"] = AssetCategory.Vegetation;
            map["bushes_small_red_berries_LOD_group"] = AssetCategory.Vegetation;
            map["bushes_star_leaves_big_LOD_group"] = AssetCategory.Vegetation;
            map["bushes_star_leaves_small_LOD_group"] = AssetCategory.Vegetation;

            // Flowers
            map["dandelion_double_balls"] = AssetCategory.Vegetation;
            map["dandelion_double_balls_LOD_group"] = AssetCategory.Vegetation;
            map["dandelion_flower_and_ball"] = AssetCategory.Vegetation;
            map["dandelion_flower_and_ball_LOD_group"] = AssetCategory.Vegetation;
            map["dandelion_flower_big_and_small_LOD_group"] = AssetCategory.Vegetation;
            map["dandelion_leaves"] = AssetCategory.Vegetation;
            map["dandelion_leaves_LOD_group"] = AssetCategory.Vegetation;
            map["flower_double_purple_LOD_group"] = AssetCategory.Vegetation;
            map["flower_double_red_LOD_group"] = AssetCategory.Vegetation;
            map["flower_double_white_LOD_group"] = AssetCategory.Vegetation;
            map["flower_double_yellow_LOD_group"] = AssetCategory.Vegetation;
            map["flower_red"] = AssetCategory.Vegetation;
            map["flower_red1_LOD_group"] = AssetCategory.Vegetation;
            map["flower_red_low"] = AssetCategory.Vegetation;
            map["flower_red_low_LOD_group"] = AssetCategory.Vegetation;
            map["flower_single_purple_LOD_group"] = AssetCategory.Vegetation;
            map["flower_single_red_LOD_group"] = AssetCategory.Vegetation;
            map["flower_single_white_LOD_group"] = AssetCategory.Vegetation;
            map["flower_single_yellow_LOD_group"] = AssetCategory.Vegetation;
            map["flower_trio_purple_LOD_group"] = AssetCategory.Vegetation;
            map["flower_trio_red_LOD_group"] = AssetCategory.Vegetation;
            map["flower_trio_white_LOD_group"] = AssetCategory.Vegetation;
            map["flower_trio_yellow_LOD_group"] = AssetCategory.Vegetation;
            map["flower_yellow"] = AssetCategory.Vegetation;
            map["flower_yellow1_LOD_group"] = AssetCategory.Vegetation;
            map["flower_yellow_low"] = AssetCategory.Vegetation;
            map["flower_yellow_low_LOD_group"] = AssetCategory.Vegetation;
            map["heliconia_big_LOD_group"] = AssetCategory.Vegetation;
            map["heliconia_leaves_LOD_group"] = AssetCategory.Vegetation;
            map["heliconia_small_LOD_group"] = AssetCategory.Vegetation;
            map["lotus_flower_pink_LOD_group"] = AssetCategory.Vegetation;
            map["lotus_flower_white_LOD_group"] = AssetCategory.Vegetation;
            map["philodendron_green_large_LOD_group"] = AssetCategory.Vegetation;
            map["philodendron_green_small_LOD_group"] = AssetCategory.Vegetation;
            map["philodendron_orange_large_LOD_group"] = AssetCategory.Vegetation;
            map["philodendron_orange_small_LOD_group"] = AssetCategory.Vegetation;
            map["oval_leave_plant_flower_LOD_group"] = AssetCategory.Vegetation;
            map["oval_leave_plant_plain_LOD_group"] = AssetCategory.Vegetation;
            map["cactus_flower"] = AssetCategory.Vegetation;

            // Grass
            map["grass_bundle"] = AssetCategory.Vegetation;
            map["grass_deco_1"] = AssetCategory.Vegetation;
            map["grass_deco_2"] = AssetCategory.Vegetation;
            map["grass_deco_3"] = AssetCategory.Vegetation;
            map["grass_flower_balls_high_LOD_group"] = AssetCategory.Vegetation;
            map["grass_flower_balls_low_LOD_group"] = AssetCategory.Vegetation;
            map["grass_swamp"] = AssetCategory.Vegetation;
            map["grass_tulip_flower_red_LOD_group"] = AssetCategory.Vegetation;
            map["grass_tulip_flower_white_LOD_group"] = AssetCategory.Vegetation;
            map["tall_grass_green"] = AssetCategory.Vegetation;
            map["tall_grass_green_LOD_group"] = AssetCategory.Vegetation;

            // Vines and foliage
            map["branch_L_LOD_group"] = AssetCategory.Vegetation;
            map["branch_straight_LOD_group"] = AssetCategory.Vegetation;
            map["leaf"] = AssetCategory.Vegetation;
            map["leafs1_LOD_group"] = AssetCategory.Vegetation;
            map["leafs2_LOD_group"] = AssetCategory.Vegetation;
            map["leafs3_LOD_group"] = AssetCategory.Vegetation;
            map["leafs_1"] = AssetCategory.Vegetation;
            map["leafs_2"] = AssetCategory.Vegetation;
            map["leafs_3"] = AssetCategory.Vegetation;
            map["leafs_LOD_group"] = AssetCategory.Vegetation;
            map["leafs_branch_LOD_group"] = AssetCategory.Vegetation;
            map["leaves_pile_big_LOD_group"] = AssetCategory.Vegetation;
            map["leaves_pile_small_LOD_group"] = AssetCategory.Vegetation;
            map["log1_LOD_group"] = AssetCategory.Vegetation;
            map["log2_LOD_group"] = AssetCategory.Vegetation;
            map["log3_LOD_group"] = AssetCategory.Vegetation;
            map["log4_LOD_group"] = AssetCategory.Vegetation;
            map["lotus_leaf_big_LOD_group"] = AssetCategory.Vegetation;
            map["lotus_leaf_small_LOD_group"] = AssetCategory.Vegetation;
            map["vine"] = AssetCategory.Vegetation;
            map["vine1_LOD_group"] = AssetCategory.Vegetation;
            map["vine2_LOD_group"] = AssetCategory.Vegetation;
            map["vine_big_curve_brown_LOD_group"] = AssetCategory.Vegetation;
            map["vine_horizontal_curve_big_green_LOD_group"] = AssetCategory.Vegetation;
            map["vine_horizontal_curve_small_green_LOD_group"] = AssetCategory.Vegetation;
            map["vine_small_curve_brown_LOD_group"] = AssetCategory.Vegetation;
            map["vine_vertical_curled_brown_LOD_group"] = AssetCategory.Vegetation;
            map["vine_vertical_curled_green_LOD_group"] = AssetCategory.Vegetation;
            map["vine_vertical_straight_brown_LOD_group"] = AssetCategory.Vegetation;
            map["vine_vertical_straight_green_LOD_group"] = AssetCategory.Vegetation;

            // Crops
            map["crop_carrot_1"] = AssetCategory.Vegetation;
            map["crop_carrot_2"] = AssetCategory.Vegetation;
            map["crop_carrot_3"] = AssetCategory.Vegetation;
            map["crop_carrot_single"] = AssetCategory.Vegetation;
            map["crop_corn_1"] = AssetCategory.Vegetation;
            map["crop_corn_2"] = AssetCategory.Vegetation;
            map["crop_corn_3"] = AssetCategory.Vegetation;
            map["crop_corn_single"] = AssetCategory.Vegetation;
            map["crop_potato_1"] = AssetCategory.Vegetation;
            map["crop_potato_2"] = AssetCategory.Vegetation;
            map["crop_potato_3"] = AssetCategory.Vegetation;
            map["crop_potato_single"] = AssetCategory.Vegetation;
            map["crop_salad_1"] = AssetCategory.Vegetation;
            map["crop_salad_2"] = AssetCategory.Vegetation;
            map["crop_salad_3"] = AssetCategory.Vegetation;
            map["crop_salad_single"] = AssetCategory.Vegetation;
            map["crop_tomato_1"] = AssetCategory.Vegetation;
            map["crop_tomato_2"] = AssetCategory.Vegetation;
            map["crop_tomato_3"] = AssetCategory.Vegetation;
            map["crop_tomato_single"] = AssetCategory.Vegetation;
            map["crop_wheat_1"] = AssetCategory.Vegetation;
            map["crop_wheat_2"] = AssetCategory.Vegetation;
            map["crop_wheat_3"] = AssetCategory.Vegetation;

            // Mushrooms and cacti
            map["mushroom_1"] = AssetCategory.Vegetation;
            map["mushroom_2"] = AssetCategory.Vegetation;
            map["mushroom_3"] = AssetCategory.Vegetation;
            map["mushroom_4"] = AssetCategory.Vegetation;
            map["mushroom_5"] = AssetCategory.Vegetation;
            map["cactus_1"] = AssetCategory.Vegetation;
            map["cactus_2"] = AssetCategory.Vegetation;
            map["cactus_3"] = AssetCategory.Vegetation;
            map["cactus_block"] = AssetCategory.Vegetation;
            map["cactus_block_top"] = AssetCategory.Vegetation;

            // Berries
            map["berry"] = AssetCategory.Vegetation;

            // ========== TERRAIN (Ground, rocks, natural features) ==========
            map["dirt_cube_1"] = AssetCategory.Terrain;
            map["dirt_cube_2"] = AssetCategory.Terrain;
            map["ground_cube"] = AssetCategory.Terrain;
            map["ground_cube_grass"] = AssetCategory.Terrain;
            map["ground_tile_stone_1"] = AssetCategory.Terrain;
            map["ground_tile_stone_2"] = AssetCategory.Terrain;
            map["ground_tile_stone_3"] = AssetCategory.Terrain;
            map["ground_tile_stone_4"] = AssetCategory.Terrain;
            map["crop_ground_lines"] = AssetCategory.Terrain;
            map["crop_ground_mounds"] = AssetCategory.Terrain;
            map["rock_cube_1"] = AssetCategory.Terrain;
            map["rock_cube_2"] = AssetCategory.Terrain;
            map["rock_cube_3"] = AssetCategory.Terrain;
            map["rock_cube_4"] = AssetCategory.Terrain;
            map["sand_cube_1"] = AssetCategory.Terrain;
            map["sand_cube_2"] = AssetCategory.Terrain;
            map["sand_cube_3"] = AssetCategory.Terrain;
            map["sand_pile"] = AssetCategory.Terrain;
            map["sand_top"] = AssetCategory.Terrain;
            map["sand_top1"] = AssetCategory.Terrain;
            map["sand_top2"] = AssetCategory.Terrain;
            map["snow_cube"] = AssetCategory.Terrain;
            map["pebbles_1"] = AssetCategory.Terrain;
            map["pebbles_2"] = AssetCategory.Terrain;
            map["pebbles_3"] = AssetCategory.Terrain;
            map["ice_cube_1"] = AssetCategory.Terrain;
            map["ice_cube_2"] = AssetCategory.Terrain;
            map["ice_cube_3"] = AssetCategory.Terrain;
            map["icicle_1"] = AssetCategory.Terrain;
            map["icicle_2"] = AssetCategory.Terrain;
            map["lava_cubes_1"] = AssetCategory.Terrain;
            map["lava_cubes_2"] = AssetCategory.Terrain;
            map["lava_cubes_3"] = AssetCategory.Terrain;
            map["lava_cubes_4"] = AssetCategory.Terrain;
            map["lava_cubes_5"] = AssetCategory.Terrain;
            map["coal_block"] = AssetCategory.Terrain;
            map["coal_ore_block_1"] = AssetCategory.Terrain;
            map["coal_ore_block_2"] = AssetCategory.Terrain;
            map["diamond_ore_block"] = AssetCategory.Terrain;
            map["gold_ore_block_1"] = AssetCategory.Terrain;
            map["gold_ore_block_2"] = AssetCategory.Terrain;
            map["metal_ore_block_1"] = AssetCategory.Terrain;
            map["metal_ore_block_2"] = AssetCategory.Terrain;
            map["ruby_ore_block"] = AssetCategory.Terrain;
            map["ore_coal"] = AssetCategory.Terrain;
            map["ore_gold"] = AssetCategory.Terrain;
            map["ore_ruby"] = AssetCategory.Terrain;
            map["ore_silver"] = AssetCategory.Terrain;

            // ========== WALLS (Fences, rails, barriers) ==========
            map["fence"] = AssetCategory.Walls;
            map["fence_pole_1"] = AssetCategory.Walls;
            map["fence_pole_2"] = AssetCategory.Walls;
            map["fence_pole_wood"] = AssetCategory.Walls;
            map["fence_pole_wood_light"] = AssetCategory.Walls;
            map["fence_wood_1"] = AssetCategory.Walls;
            map["fence_wood_2"] = AssetCategory.Walls;
            map["fence_wood_3"] = AssetCategory.Walls;
            map["fence_wood_gate_left"] = AssetCategory.Walls;
            map["fence_wood_gate_right"] = AssetCategory.Walls;
            map["fence_wood_half"] = AssetCategory.Walls;
            map["fence_wood_ligh_half"] = AssetCategory.Walls;
            map["fence_wood_light"] = AssetCategory.Walls;
            map["block_rail1_LOD_group"] = AssetCategory.Walls;
            map["block_rail2_LOD_group"] = AssetCategory.Walls;
            map["rail1_1_stone1_LOD_group"] = AssetCategory.Walls;
            map["rail1_2_stone1_LOD_group"] = AssetCategory.Walls;
            map["rail2_1_stone1_LOD_group"] = AssetCategory.Walls;
            map["rail2_2_stone1_LOD_group"] = AssetCategory.Walls;
            map["rail3_stone1_LOD_group"] = AssetCategory.Walls;
            map["rail4_stone1_LOD_group"] = AssetCategory.Walls;
            map["rail5_stone1_LOD_group"] = AssetCategory.Walls;
            map["rail_stone_1"] = AssetCategory.Walls;
            map["rail_stone_2"] = AssetCategory.Walls;
            map["rail_stone_3"] = AssetCategory.Walls;
            map["rail_stone_4"] = AssetCategory.Walls;
            map["rail_stone_5"] = AssetCategory.Walls;
            map["rail_stone_6"] = AssetCategory.Walls;

            // ========== ROOFS (Barn roofs, canopies) ==========
            map["barn_beam_roof"] = AssetCategory.Roofs;
            map["barn_roof1"] = AssetCategory.Roofs;
            map["barn_roof2"] = AssetCategory.Roofs;
            map["barn_roof_top"] = AssetCategory.Roofs;
            map["canopy_block_1"] = AssetCategory.Roofs;
            map["canopy_block_2"] = AssetCategory.Roofs;

            // ========== STRUCTURES (Blocks, doors, stairs, beams, walls) ==========
            // Basic blocks
            map["block_1"] = AssetCategory.Structures;
            map["block_1_arrow"] = AssetCategory.Structures;
            map["block_1_health"] = AssetCategory.Structures;
            map["block_1_heart"] = AssetCategory.Structures;
            map["block_1_lightning"] = AssetCategory.Structures;
            map["block_1_random"] = AssetCategory.Structures;
            map["block_1_star"] = AssetCategory.Structures;
            map["block_2"] = AssetCategory.Structures;
            map["block_arrow"] = AssetCategory.Structures;
            map["block_health"] = AssetCategory.Structures;
            map["block_heart"] = AssetCategory.Structures;
            map["block_lightning"] = AssetCategory.Structures;
            map["block_random"] = AssetCategory.Structures;
            map["block_ruby"] = AssetCategory.Structures;
            map["block_star"] = AssetCategory.Structures;

            // Stone blocks
            map["block_stone1_1_LOD_group"] = AssetCategory.Structures;
            map["block_stone1_1_debris_LOD_group"] = AssetCategory.Structures;
            map["block_stone1_1_half_LOD_group"] = AssetCategory.Structures;
            map["block_stone1_1_tiny_LOD_group"] = AssetCategory.Structures;
            map["block_stone1_2_LOD_group"] = AssetCategory.Structures;
            map["block_stone1_2_debris_LOD_group"] = AssetCategory.Structures;
            map["block_stone1_2_half_LOD_group"] = AssetCategory.Structures;
            map["block_stone1_2_tiny_LOD_group"] = AssetCategory.Structures;
            map["block_stone1_3_LOD_group"] = AssetCategory.Structures;
            map["block_stone1_3_debris_LOD_group"] = AssetCategory.Structures;
            map["block_stone1_3_half_dark_LOD_group"] = AssetCategory.Structures;
            map["block_stone1_3_half_light_LOD_group"] = AssetCategory.Structures;
            map["block_stone1_3_half_plinth_LOD_group"] = AssetCategory.Structures;
            map["block_stone1_3_half_torch_LOD_group"] = AssetCategory.Structures;
            map["block_stone1_4_LOD_group"] = AssetCategory.Structures;
            map["block_stone1_4_debris_LOD_group"] = AssetCategory.Structures;
            map["block_stone1_5_LOD_group"] = AssetCategory.Structures;
            map["block_stone1_6_LOD_group"] = AssetCategory.Structures;
            map["block_stone1_7_LOD_group"] = AssetCategory.Structures;
            map["block_stone1_8_LOD_group"] = AssetCategory.Structures;
            map["block_stone1_9_LOD_group"] = AssetCategory.Structures;
            map["block_stone1_10_LOD_group"] = AssetCategory.Structures;
            map["block_stone1_head_LOD_group"] = AssetCategory.Structures;
            map["block_stone_large1_LOD_group"] = AssetCategory.Structures;
            map["block_stone_large2_LOD_group"] = AssetCategory.Structures;
            map["block_stone_large3_LOD_group"] = AssetCategory.Structures;
            map["block_stone_large4_LOD_group"] = AssetCategory.Structures;
            map["block_stone_large5_LOD_group"] = AssetCategory.Structures;
            map["block_stone_torch_LOD_group"] = AssetCategory.Structures;

            // Bricks
            map["brick_black_1"] = AssetCategory.Structures;
            map["brick_black_2"] = AssetCategory.Structures;
            map["brick_black_3"] = AssetCategory.Structures;
            map["brick_black_4"] = AssetCategory.Structures;
            map["brick_brown_1"] = AssetCategory.Structures;
            map["brick_brown_2"] = AssetCategory.Structures;
            map["brick_brown_3"] = AssetCategory.Structures;
            map["brick_brown_4"] = AssetCategory.Structures;

            // Wood cubes
            map["wood_cube_1"] = AssetCategory.Structures;
            map["wood_cube_2"] = AssetCategory.Structures;
            map["wood_cube_3"] = AssetCategory.Structures;
            map["wood_cube_4"] = AssetCategory.Structures;
            map["wood_cube_5"] = AssetCategory.Structures;
            map["wood_cube_6"] = AssetCategory.Structures;
            map["wood_cube_7"] = AssetCategory.Structures;
            map["wood_cube_8"] = AssetCategory.Structures;
            map["wood_cube_9"] = AssetCategory.Structures;
            map["wood_cube_10"] = AssetCategory.Structures;
            map["wood_cube_11"] = AssetCategory.Structures;
            map["wood_cube_12"] = AssetCategory.Structures;
            map["wood_cube_light_1"] = AssetCategory.Structures;
            map["wood_cube_light_2"] = AssetCategory.Structures;
            map["wood_cube_light_3"] = AssetCategory.Structures;
            map["wood_cube_light_4"] = AssetCategory.Structures;
            map["wood_cube_light_5"] = AssetCategory.Structures;
            map["wood_cube_light_6"] = AssetCategory.Structures;

            // Metal cubes
            map["metal_cube_1"] = AssetCategory.Structures;
            map["metal_cube_2"] = AssetCategory.Structures;
            map["metal_cube_3"] = AssetCategory.Structures;
            map["metal_cube_4"] = AssetCategory.Structures;

            // Stone cubes
            map["stone_cube_1"] = AssetCategory.Structures;
            map["stone_cube_1_half"] = AssetCategory.Structures;
            map["stone_cube_2"] = AssetCategory.Structures;
            map["stone_cube_2_half"] = AssetCategory.Structures;
            map["stone_cube_3"] = AssetCategory.Structures;
            map["stone_cube_3_half"] = AssetCategory.Structures;
            map["stone_cube_4"] = AssetCategory.Structures;
            map["stone_cube_5"] = AssetCategory.Structures;
            map["stone_cube_6"] = AssetCategory.Structures;

            // Barn structures
            map["barn_beam"] = AssetCategory.Structures;
            map["barn_beam_diagonal"] = AssetCategory.Structures;
            map["barn_beam_horizontal"] = AssetCategory.Structures;
            map["barn_beam_ring"] = AssetCategory.Structures;
            map["barn_cube"] = AssetCategory.Structures;
            map["barn_cube_small"] = AssetCategory.Structures;
            map["barn_sill"] = AssetCategory.Structures;
            map["barn_wall"] = AssetCategory.Structures;

            // Sandstone structures
            map["sandstone"] = AssetCategory.Structures;
            map["sandstone_block_dark"] = AssetCategory.Structures;
            map["sandstone_block_deco"] = AssetCategory.Structures;
            map["sandstone_block_fountain"] = AssetCategory.Structures;
            map["sandstone_block_light"] = AssetCategory.Structures;
            map["sandstone_block_slope_corner_conc"] = AssetCategory.Structures;
            map["sandstone_block_slope_corner_conv"] = AssetCategory.Structures;
            map["sandstone_block_slope_straight"] = AssetCategory.Structures;
            map["sandstone_bricks"] = AssetCategory.Structures;
            map["sandstone_bricks_small"] = AssetCategory.Structures;
            map["sandstone_pillar"] = AssetCategory.Structures;
            map["sandstone_pillar_bottom"] = AssetCategory.Structures;
            map["sandstone_pillar_dark"] = AssetCategory.Structures;
            map["sandstone_pillar_deco"] = AssetCategory.Structures;
            map["sandstone_pillar_deco2"] = AssetCategory.Structures;
            map["sandstone_pillar_deco_dark"] = AssetCategory.Structures;
            map["sandstone_pillar_top"] = AssetCategory.Structures;

            // Doors and windows
            map["barn_door_left"] = AssetCategory.Structures;
            map["barn_door_right"] = AssetCategory.Structures;
            map["barn_door_rounded"] = AssetCategory.Structures;
            map["barn_door_rounded_frame"] = AssetCategory.Structures;
            map["barn_door_rounded_left"] = AssetCategory.Structures;
            map["barn_door_rounded_right"] = AssetCategory.Structures;
            map["barn_doorframe"] = AssetCategory.Structures;
            map["barn_doubledoor_rounded_frame"] = AssetCategory.Structures;
            map["barn_trap_door"] = AssetCategory.Structures;
            map["barn_window1"] = AssetCategory.Structures;
            map["barn_window2"] = AssetCategory.Structures;
            map["coop_door"] = AssetCategory.Structures;
            map["door_frame_metal_double"] = AssetCategory.Structures;
            map["door_frame_metal_rounded_double"] = AssetCategory.Structures;
            map["door_frame_metal_rounded_single"] = AssetCategory.Structures;
            map["door_frame_metal_single"] = AssetCategory.Structures;
            map["door_frame_stone_double"] = AssetCategory.Structures;
            map["door_frame_stone_rounded_double"] = AssetCategory.Structures;
            map["door_frame_stone_rounded_single"] = AssetCategory.Structures;
            map["door_frame_stone_single"] = AssetCategory.Structures;
            map["door_frame_wood_double"] = AssetCategory.Structures;
            map["door_frame_wood_rounded_double"] = AssetCategory.Structures;
            map["door_frame_wood_rounded_single"] = AssetCategory.Structures;
            map["door_frame_wood_single"] = AssetCategory.Structures;
            map["door_handles"] = AssetCategory.Structures;
            map["door_hatch_wood"] = AssetCategory.Structures;
            map["door_hatch_wood_light"] = AssetCategory.Structures;
            map["door_hinges"] = AssetCategory.Structures;
            map["door_hinges_close"] = AssetCategory.Structures;
            map["door_large_crown_stone_LOD_group"] = AssetCategory.Structures;
            map["door_large_face_stone_LOD_group"] = AssetCategory.Structures;
            map["door_large_floor_stone_LOD_group"] = AssetCategory.Structures;
            map["door_metal_left"] = AssetCategory.Structures;
            map["door_metal_right"] = AssetCategory.Structures;
            map["door_metal_rounded"] = AssetCategory.Structures;
            map["door_metal_rounded_double_top"] = AssetCategory.Structures;
            map["door_metal_rounded_left"] = AssetCategory.Structures;
            map["door_metal_rounded_right"] = AssetCategory.Structures;
            map["door_metal_rounded_top"] = AssetCategory.Structures;
            map["door_small_stone_LOD_group"] = AssetCategory.Structures;
            map["door_stone_left"] = AssetCategory.Structures;
            map["door_stone_right"] = AssetCategory.Structures;
            map["door_stone_rounded"] = AssetCategory.Structures;
            map["door_stone_rounded_double_top"] = AssetCategory.Structures;
            map["door_stone_rounded_left"] = AssetCategory.Structures;
            map["door_stone_rounded_right"] = AssetCategory.Structures;
            map["door_stone_rounded_top"] = AssetCategory.Structures;
            map["door_wood"] = AssetCategory.Structures;
            map["door_wood_left"] = AssetCategory.Structures;
            map["door_wood_light"] = AssetCategory.Structures;
            map["door_wood_right"] = AssetCategory.Structures;
            map["door_wood_rounded"] = AssetCategory.Structures;
            map["door_wood_rounded_double_top"] = AssetCategory.Structures;
            map["door_wood_rounded_left"] = AssetCategory.Structures;
            map["door_wood_rounded_right"] = AssetCategory.Structures;
            map["door_wood_rounded_top"] = AssetCategory.Structures;
            map["wood_window"] = AssetCategory.Structures;
            map["wood_window_light"] = AssetCategory.Structures;

            // Stairs and ladders
            map["ladder_wood"] = AssetCategory.Structures;
            map["ladder_wood_light"] = AssetCategory.Structures;
            map["sandstone_stair_corner_conc"] = AssetCategory.Structures;
            map["sandstone_stair_corner_conv"] = AssetCategory.Structures;
            map["sandstone_stair_straight"] = AssetCategory.Structures;
            map["stair_stone1_corner_conc_LOD_group"] = AssetCategory.Structures;
            map["stair_stone1_corner_conv_LOD_group"] = AssetCategory.Structures;
            map["stair_stone1_straight_LOD_group"] = AssetCategory.Structures;
            map["stairs_stone_corner_conc"] = AssetCategory.Structures;
            map["stairs_stone_corner_conv"] = AssetCategory.Structures;
            map["stairs_stone_straight"] = AssetCategory.Structures;
            map["stairs_wood"] = AssetCategory.Structures;
            map["stairs_wood_light"] = AssetCategory.Structures;

            // Scaffolding
            map["scaffolding_cube"] = AssetCategory.Structures;
            map["scaffolding_cube_half"] = AssetCategory.Structures;
            map["rail_tracks_scaffolding"] = AssetCategory.Structures;
            map["rail_tracks_scaffolding_half"] = AssetCategory.Structures;
            map["rail_tracks_scaffolding_half_turn"] = AssetCategory.Structures;
            map["rail_tracks_scaffolding_turn"] = AssetCategory.Structures;

            // Rail tracks
            map["rail_track_end"] = AssetCategory.Structures;
            map["rail_tracks_Y_left"] = AssetCategory.Structures;
            map["rail_tracks_Y_right"] = AssetCategory.Structures;
            map["rail_tracks_cross"] = AssetCategory.Structures;
            map["rail_tracks_straight"] = AssetCategory.Structures;
            map["rail_tracks_t"] = AssetCategory.Structures;
            map["rail_tracks_tile_up_half"] = AssetCategory.Structures;
            map["rail_tracks_turn"] = AssetCategory.Structures;
            map["rail_tracks_upS"] = AssetCategory.Structures;
            map["rail_tracks_upS_half"] = AssetCategory.Structures;
            map["rail_tracks_up_bottom"] = AssetCategory.Structures;
            map["rail_tracks_up_bottom_half"] = AssetCategory.Structures;
            map["rail_tracks_up_tile"] = AssetCategory.Structures;
            map["rail_tracks_up_top"] = AssetCategory.Structures;
            map["rail_tracks_up_top_half"] = AssetCategory.Structures;

            // Pipes
            map["pipe_L"] = AssetCategory.Structures;
            map["pipe_T"] = AssetCategory.Structures;
            map["pipe_X"] = AssetCategory.Structures;
            map["pipe_end"] = AssetCategory.Structures;
            map["pipe_straight"] = AssetCategory.Structures;
            map["pipe_straight_2"] = AssetCategory.Structures;
            map["pipe_up"] = AssetCategory.Structures;

            // ========== FURNITURE (Storage, seating, functional items) ==========
            map["box_wood_1"] = AssetCategory.Furniture;
            map["box_wood_2"] = AssetCategory.Furniture;
            map["chest_gold"] = AssetCategory.Furniture;
            map["chest_silver"] = AssetCategory.Furniture;
            map["chest_wood"] = AssetCategory.Furniture;
            map["wooden_barrel_1"] = AssetCategory.Furniture;
            map["wooden_barrel_1_destructible"] = AssetCategory.Furniture;
            map["wooden_barrel_2"] = AssetCategory.Furniture;
            map["wooden_barrel_2_destructible"] = AssetCategory.Furniture;
            map["wooden_barrel_3"] = AssetCategory.Furniture;
            map["wooden_barrel_3_destructible"] = AssetCategory.Furniture;
            map["wooden_box_1"] = AssetCategory.Furniture;
            map["wooden_box_2"] = AssetCategory.Furniture;
            map["wooden_box_3"] = AssetCategory.Furniture;
            map["wooden_box_4"] = AssetCategory.Furniture;
            map["throne"] = AssetCategory.Furniture;
            map["sarcophagus"] = AssetCategory.Furniture;
            map["sarcophagus_lid"] = AssetCategory.Furniture;
            map["hay_bale"] = AssetCategory.Furniture;
            map["hay_bale_round"] = AssetCategory.Furniture;
            map["lantern"] = AssetCategory.Furniture;

            // ========== TOOLS (Weapons, tools, interactive items) ==========
            // Weapons - Arrows
            map["arrow_diamond"] = AssetCategory.Tools;
            map["arrow_gold"] = AssetCategory.Tools;
            map["arrow_metal"] = AssetCategory.Tools;
            map["arrow_stone"] = AssetCategory.Tools;
            map["arrow_wood"] = AssetCategory.Tools;

            // Weapons - Axes
            map["axe_diamond"] = AssetCategory.Tools;
            map["axe_gold"] = AssetCategory.Tools;
            map["axe_metal"] = AssetCategory.Tools;
            map["axe_stone"] = AssetCategory.Tools;
            map["axe_wood"] = AssetCategory.Tools;

            // Weapons - Bows
            map["bow_diamond"] = AssetCategory.Tools;
            map["bow_gold"] = AssetCategory.Tools;
            map["bow_metal"] = AssetCategory.Tools;
            map["bow_stone"] = AssetCategory.Tools;
            map["bow_wood"] = AssetCategory.Tools;

            // Weapons - Hammers
            map["hammer_diamond"] = AssetCategory.Tools;
            map["hammer_gold"] = AssetCategory.Tools;
            map["hammer_metal"] = AssetCategory.Tools;
            map["hammer_stone"] = AssetCategory.Tools;
            map["hammer_wood"] = AssetCategory.Tools;

            // Weapons - Pickaxes
            map["pickaxe_diamond"] = AssetCategory.Tools;
            map["pickaxe_gold"] = AssetCategory.Tools;
            map["pickaxe_metal"] = AssetCategory.Tools;
            map["pickaxe_stone"] = AssetCategory.Tools;
            map["pickaxe_wood"] = AssetCategory.Tools;

            // Weapons - Shovels
            map["shovel_diamond"] = AssetCategory.Tools;
            map["shovel_gold"] = AssetCategory.Tools;
            map["shovel_metal"] = AssetCategory.Tools;
            map["shovel_stone"] = AssetCategory.Tools;
            map["shovel_wood"] = AssetCategory.Tools;

            // Weapons - Spears
            map["spear_diamond"] = AssetCategory.Tools;
            map["spear_gold"] = AssetCategory.Tools;
            map["spear_metal"] = AssetCategory.Tools;
            map["spear_short_diamond"] = AssetCategory.Tools;
            map["spear_short_gold"] = AssetCategory.Tools;
            map["spear_short_metal"] = AssetCategory.Tools;
            map["spear_short_stone"] = AssetCategory.Tools;
            map["spear_short_wood"] = AssetCategory.Tools;
            map["spear_stone"] = AssetCategory.Tools;
            map["spear_wood"] = AssetCategory.Tools;

            // Weapons - Swords
            map["sword_diamond"] = AssetCategory.Tools;
            map["sword_gold"] = AssetCategory.Tools;
            map["sword_metal"] = AssetCategory.Tools;
            map["sword_stone"] = AssetCategory.Tools;
            map["sword_wood"] = AssetCategory.Tools;

            // Special weapons
            map["weapon_blowpipe"] = AssetCategory.Tools;
            map["weapon_boomerang"] = AssetCategory.Tools;
            map["weapon_fire_flower"] = AssetCategory.Tools;
            map["weapon_spear"] = AssetCategory.Tools;
            map["weapon_stone_hammer"] = AssetCategory.Tools;

            // Cannons and projectiles
            map["bullet"] = AssetCategory.Tools;
            map["cannon"] = AssetCategory.Tools;
            map["cannonball"] = AssetCategory.Tools;
            map["cannonball_stack"] = AssetCategory.Tools;
            map["rocket"] = AssetCategory.Tools;
            map["snowball"] = AssetCategory.Tools;

            // Generator weapons
            map["generator_head_lvl1"] = AssetCategory.Tools;
            map["generator_head_lvl2"] = AssetCategory.Tools;
            map["generator_head_lvl3"] = AssetCategory.Tools;
            map["generator_head_lvl4"] = AssetCategory.Tools;
            map["gun_head_lvl1"] = AssetCategory.Tools;
            map["gun_head_lvl2"] = AssetCategory.Tools;
            map["gun_head_lvl3"] = AssetCategory.Tools;
            map["gun_head_lvl4"] = AssetCategory.Tools;
            map["mortar_bullet_lvl1"] = AssetCategory.Tools;
            map["mortar_bullet_lvl2"] = AssetCategory.Tools;
            map["mortar_bullet_lvl3"] = AssetCategory.Tools;
            map["mortar_bullet_lvl4"] = AssetCategory.Tools;
            map["mortar_head_lvl1"] = AssetCategory.Tools;
            map["mortar_head_lvl2"] = AssetCategory.Tools;
            map["mortar_head_lvl3"] = AssetCategory.Tools;
            map["mortar_head_lvl4"] = AssetCategory.Tools;
            map["rocketlauncher_head_lvl1"] = AssetCategory.Tools;
            map["rocketlauncher_head_lvl2"] = AssetCategory.Tools;
            map["rocketlauncher_head_lvl3"] = AssetCategory.Tools;
            map["rocketlauncher_head_lvl4"] = AssetCategory.Tools;
            map["stun_hammer_lvl1"] = AssetCategory.Tools;
            map["stun_hammer_lvl2"] = AssetCategory.Tools;
            map["stun_hammer_lvl3"] = AssetCategory.Tools;
            map["stun_hammer_lvl4"] = AssetCategory.Tools;
            map["turret_base"] = AssetCategory.Tools;

            // Interactive tools
            map["cauldron_LOD_group"] = AssetCategory.Tools;
            map["cauldron_animated_LOD_group"] = AssetCategory.Tools;
            map["chain"] = AssetCategory.Tools;
            map["drum_LOD_group"] = AssetCategory.Tools;
            map["drum_half_LOD_group"] = AssetCategory.Tools;
            map["drum_stick"] = AssetCategory.Tools;
            map["lever_floor"] = AssetCategory.Tools;
            map["lever_wall"] = AssetCategory.Tools;
            map["switch_floor"] = AssetCategory.Tools;
            map["switch_rail"] = AssetCategory.Tools;
            map["trap_floor_LOD_group"] = AssetCategory.Tools;
            map["trap_wall_LOD_group"] = AssetCategory.Tools;
            map["valve"] = AssetCategory.Tools;
            map["key_gold"] = AssetCategory.Tools;
            map["key_silver"] = AssetCategory.Tools;
            map["keyhole"] = AssetCategory.Tools;
            map["keyhole_gold"] = AssetCategory.Tools;
            map["minecart"] = AssetCategory.Tools;
            map["tractor"] = AssetCategory.Tools;

            // ========== DECORATIONS (Ornaments, bones, signs, aesthetic items) ==========
            // Bones
            map["bone_LOD_group"] = AssetCategory.Decorations;
            map["bone_jaw_LOD_group"] = AssetCategory.Decorations;
            map["bone_pelvis_LOD_group"] = AssetCategory.Decorations;
            map["bone_ribcage_LOD_group"] = AssetCategory.Decorations;
            map["bone_skull_LOD_group"] = AssetCategory.Decorations;
            map["skull_pile_large_LOD_group"] = AssetCategory.Decorations;
            map["skull_pile_small_LOD_group"] = AssetCategory.Decorations;

            // Baubles (Christmas ornaments)
            map["bauble_large_color1"] = AssetCategory.Decorations;
            map["bauble_large_color1_ornate"] = AssetCategory.Decorations;
            map["bauble_large_color2"] = AssetCategory.Decorations;
            map["bauble_large_color2_ornate"] = AssetCategory.Decorations;
            map["bauble_large_color3"] = AssetCategory.Decorations;
            map["bauble_large_color3_ornate"] = AssetCategory.Decorations;
            map["bauble_large_color4"] = AssetCategory.Decorations;
            map["bauble_large_color4_ornate"] = AssetCategory.Decorations;
            map["bauble_medium_color1"] = AssetCategory.Decorations;
            map["bauble_medium_color1_ornate"] = AssetCategory.Decorations;
            map["bauble_medium_color2"] = AssetCategory.Decorations;
            map["bauble_medium_color2_ornate"] = AssetCategory.Decorations;
            map["bauble_medium_color3"] = AssetCategory.Decorations;
            map["bauble_medium_color3_ornate"] = AssetCategory.Decorations;
            map["bauble_medium_color4"] = AssetCategory.Decorations;
            map["bauble_medium_color4_ornate"] = AssetCategory.Decorations;
            map["bauble_small_color1"] = AssetCategory.Decorations;
            map["bauble_small_color1_ornate"] = AssetCategory.Decorations;
            map["bauble_small_color2"] = AssetCategory.Decorations;
            map["bauble_small_color2_ornate"] = AssetCategory.Decorations;
            map["bauble_small_color3"] = AssetCategory.Decorations;
            map["bauble_small_color3_ornate"] = AssetCategory.Decorations;
            map["bauble_small_color4"] = AssetCategory.Decorations;
            map["bauble_small_color4_ornate"] = AssetCategory.Decorations;

            // Presents (gifts)
            map["presi_large_color1"] = AssetCategory.Decorations;
            map["presi_large_color2"] = AssetCategory.Decorations;
            map["presi_large_color3"] = AssetCategory.Decorations;
            map["presi_large_color4"] = AssetCategory.Decorations;
            map["presi_large_empty_color1"] = AssetCategory.Decorations;
            map["presi_large_empty_color2"] = AssetCategory.Decorations;
            map["presi_large_empty_color3"] = AssetCategory.Decorations;
            map["presi_large_empty_color4"] = AssetCategory.Decorations;
            map["presi_medium_color1"] = AssetCategory.Decorations;
            map["presi_medium_color2"] = AssetCategory.Decorations;
            map["presi_medium_color3"] = AssetCategory.Decorations;
            map["presi_medium_color4"] = AssetCategory.Decorations;
            map["presi_medium_empty_color1"] = AssetCategory.Decorations;
            map["presi_medium_empty_color2"] = AssetCategory.Decorations;
            map["presi_medium_empty_color3"] = AssetCategory.Decorations;
            map["presi_medium_empty_color4"] = AssetCategory.Decorations;
            map["presi_small_color1"] = AssetCategory.Decorations;
            map["presi_small_color2"] = AssetCategory.Decorations;
            map["presi_small_color3"] = AssetCategory.Decorations;
            map["presi_small_color4"] = AssetCategory.Decorations;
            map["presi_small_empty_color1"] = AssetCategory.Decorations;
            map["presi_small_empty_color2"] = AssetCategory.Decorations;
            map["presi_small_empty_color3"] = AssetCategory.Decorations;
            map["presi_small_empty_color4"] = AssetCategory.Decorations;

            // Ribbons
            map["ribbon_large_color1"] = AssetCategory.Decorations;
            map["ribbon_large_color2"] = AssetCategory.Decorations;
            map["ribbon_large_color3"] = AssetCategory.Decorations;
            map["ribbon_large_color4"] = AssetCategory.Decorations;
            map["ribbon_small_color1"] = AssetCategory.Decorations;
            map["ribbon_small_color2"] = AssetCategory.Decorations;
            map["ribbon_small_color3"] = AssetCategory.Decorations;
            map["ribbon_small_color4"] = AssetCategory.Decorations;

            // Stars
            map["star_color1"] = AssetCategory.Decorations;
            map["star_color2"] = AssetCategory.Decorations;
            map["star_color3"] = AssetCategory.Decorations;
            map["star_color4"] = AssetCategory.Decorations;

            // Candles
            map["candle_block_color1"] = AssetCategory.Decorations;
            map["candle_block_top_color1"] = AssetCategory.Decorations;
            map["candle_block_top_spill_color1"] = AssetCategory.Decorations;
            map["candle_block_top_spill_wax"] = AssetCategory.Decorations;
            map["candle_block_top_wax"] = AssetCategory.Decorations;
            map["candle_block_wax"] = AssetCategory.Decorations;
            map["wick"] = AssetCategory.Decorations;

            // Torches
            map["stone_torch"] = AssetCategory.Decorations;
            map["torch_floor"] = AssetCategory.Decorations;

            // Decorative piles
            map["grasstop_deco"] = AssetCategory.Decorations;
            map["grasstop1_deco_LOD_group"] = AssetCategory.Decorations;
            map["grasstop2_deco_LOD_group"] = AssetCategory.Decorations;
            map["snowtop_deco"] = AssetCategory.Decorations;
            map["coal_nuggets"] = AssetCategory.Decorations;
            map["coal_pile"] = AssetCategory.Decorations;
            map["diamond_pile"] = AssetCategory.Decorations;
            map["gold_pile"] = AssetCategory.Decorations;
            map["metal_pile"] = AssetCategory.Decorations;
            map["ore_pile_coal"] = AssetCategory.Decorations;
            map["ore_pile_gold"] = AssetCategory.Decorations;
            map["ore_pile_ruby"] = AssetCategory.Decorations;
            map["ore_pile_silver"] = AssetCategory.Decorations;
            map["ruby_pile"] = AssetCategory.Decorations;

            // Crop decorations
            map["crop_carrot_sack"] = AssetCategory.Decorations;
            map["crop_carrot_sign"] = AssetCategory.Decorations;
            map["crop_corn_sack"] = AssetCategory.Decorations;
            map["crop_corn_sign"] = AssetCategory.Decorations;
            map["crop_potato_sack"] = AssetCategory.Decorations;
            map["crop_potato_sign"] = AssetCategory.Decorations;
            map["crop_salad_sack"] = AssetCategory.Decorations;
            map["crop_salad_sign"] = AssetCategory.Decorations;
            map["crop_tomato_sign"] = AssetCategory.Decorations;
            map["crop_tomatoes_sack"] = AssetCategory.Decorations;
            map["crop_wheat_pile"] = AssetCategory.Decorations;
            map["crop_wheat_sack"] = AssetCategory.Decorations;
            map["crop_wheat_sign"] = AssetCategory.Decorations;

            // Wood decorations
            map["wood_deco_top_1"] = AssetCategory.Decorations;
            map["wood_deco_top_1_light"] = AssetCategory.Decorations;
            map["wood_deco_top_2"] = AssetCategory.Decorations;
            map["wood_deco_top_2_light"] = AssetCategory.Decorations;
            map["wood_stack_block"] = AssetCategory.Decorations;
            map["wood_stack_block_half"] = AssetCategory.Decorations;

            // Vases
            map["vase_col1"] = AssetCategory.Decorations;
            map["vase_col2"] = AssetCategory.Decorations;
            map["vase_col3"] = AssetCategory.Decorations;

            // Statues
            map["head_statue_gold"] = AssetCategory.Decorations;
            map["head_statue_stone"] = AssetCategory.Decorations;
            map["totem_lower_1"] = AssetCategory.Decorations;
            map["totem_lower_2"] = AssetCategory.Decorations;
            map["totem_middle_1"] = AssetCategory.Decorations;
            map["totem_middle_2"] = AssetCategory.Decorations;
            map["totem_top_1"] = AssetCategory.Decorations;
            map["totem_top_2"] = AssetCategory.Decorations;

            // Signs
            map["wooden_sign_plain"] = AssetCategory.Decorations;

            // Miscellaneous decorations
            map["cork_1"] = AssetCategory.Decorations;
            map["cover_wood"] = AssetCategory.Decorations;
            map["dummy_1"] = AssetCategory.Decorations;
            map["dummy_2"] = AssetCategory.Decorations;
            map["sack_empty"] = AssetCategory.Decorations;
            map["sand_fall"] = AssetCategory.Decorations;
            map["sand_fall_spring"] = AssetCategory.Decorations;
            map["scarecrow"] = AssetCategory.Decorations;
            map["snowman"] = AssetCategory.Decorations;
            map["spike_deco_1"] = AssetCategory.Decorations;
            map["spike_deco_2"] = AssetCategory.Decorations;
            map["spike_deco_3"] = AssetCategory.Decorations;
            map["spike_cube_1"] = AssetCategory.Decorations;
            map["spike_cube_2"] = AssetCategory.Decorations;
            map["spike_cube_animated"] = AssetCategory.Decorations;
            map["stand_wooden_1"] = AssetCategory.Decorations;
            map["target"] = AssetCategory.Decorations;

            // Precious materials (decorative)
            map["diamond"] = AssetCategory.Decorations;
            map["diamond_ruby"] = AssetCategory.Decorations;
            map["gold_bar"] = AssetCategory.Decorations;
            map["gold_bar_block"] = AssetCategory.Decorations;
            map["gold_block"] = AssetCategory.Decorations;
            map["gold_pyramid_top"] = AssetCategory.Decorations;
            map["metal_bar"] = AssetCategory.Decorations;
            map["metal_bar_block"] = AssetCategory.Decorations;
            map["metal_block"] = AssetCategory.Decorations;
            map["ruby"] = AssetCategory.Decorations;

            // Powerups (decorative collectibles)
            map["powerup_arrow_1"] = AssetCategory.Decorations;
            map["powerup_coin_1"] = AssetCategory.Decorations;
            map["powerup_health_1"] = AssetCategory.Decorations;
            map["powerup_heart_1"] = AssetCategory.Decorations;
            map["powerup_lightning_1"] = AssetCategory.Decorations;
            map["powerup_random_1"] = AssetCategory.Decorations;
            map["powerup_star_1"] = AssetCategory.Decorations;

            // ========== CHARACTERS (Living beings) ==========
            map["jungle_boy"] = AssetCategory.Characters;
            map["jungle_girl"] = AssetCategory.Characters;
            map["jungle_monster_blowpipe"] = AssetCategory.Characters;
            map["jungle_monster_cannibal"] = AssetCategory.Characters;
            map["jungle_monster_plant"] = AssetCategory.Characters;
            map["jungle_monster_spear"] = AssetCategory.Characters;

            // ========== EFFECTS (Visual/audio effects, clouds) ==========
            map["bubble_lava"] = AssetCategory.Effects;
            map["cloud_1"] = AssetCategory.Effects;
            map["cloud_1_double"] = AssetCategory.Effects;
            map["cloud_2"] = AssetCategory.Effects;
            map["cloud_2_double"] = AssetCategory.Effects;
            map["cloud_3"] = AssetCategory.Effects;
            map["cloud_3_double"] = AssetCategory.Effects;
            map["cloud_half"] = AssetCategory.Effects;
            map["cloud_large"] = AssetCategory.Effects;
            map["cloud_medium"] = AssetCategory.Effects;
            map["cloud_quadrouple"] = AssetCategory.Effects;
            map["cloud_small"] = AssetCategory.Effects;
            map["fire_LOD_group"] = AssetCategory.Effects;

            return map;
        }
    }
}
