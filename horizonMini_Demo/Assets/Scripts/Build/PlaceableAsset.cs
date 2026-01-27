using UnityEngine;

namespace HorizonMini.Build
{
    /// <summary>
    /// Data for an asset that can be placed in the scene
    /// </summary>
    [CreateAssetMenu(fileName = "PlaceableAsset", menuName = "HorizonMini/PlaceableAsset")]
    public class PlaceableAsset : ScriptableObject
    {
        public string assetId;
        public string displayName;
        public AssetCategory category;
        public GameObject prefab;
        public Sprite icon;

        [Header("Placement Settings")]
        public Vector3 defaultScale = Vector3.one;
        public Vector3 pivotOffset = Vector3.zero;
    }

    public enum AssetCategory
    {
        // New 12-category system
        Buildings,          // 建筑结构: Walls, windows, doors, roofs, stairs, structures
        Nature,             // 自然环境: Trees, flowers, grass, plants, mushrooms, clouds
        Terrain,            // 地形地貌: Ground, rocks, sand, snow, lava, tiles, parquet
        Furniture,          // 家具陈设: Chairs, tables, beds, cabinets, shelves
        Decorations,        // 装饰物品: Lamps, carpets, paintings, vases, pillows, curtains
        FunctionalItems,    // 功能设施: Boxes, appliances, kitchen items, electronics, storage
        Tools,              // 工具武器: Knives, weapons, cameras, tools
        Characters,         // 角色生物: NPCs, creatures, characters
        Farming,            // 农业种植: Crops, farming tools, herbs
        WholeSet,           // 完整场景: Complete room scenes, pre-built sets
        SmartBuilder,       // 智能建造: SmartTerrain, SmartWall, SmartHouse, procedural systems
        SpecialEffects,     // 特殊效果: Effects, powerups, food, toys, books, office items

        // Legacy compatibility (deprecated, map to new categories)
        [System.Obsolete("Use Buildings instead")]
        Walls = Buildings,
        [System.Obsolete("Use Buildings instead")]
        Roofs = Buildings,
        [System.Obsolete("Use Buildings instead")]
        Structures = Buildings,
        [System.Obsolete("Use Nature instead")]
        Vegetation = Nature,
        [System.Obsolete("Use SpecialEffects instead")]
        Effects = SpecialEffects,
        [System.Obsolete("Use SmartBuilder instead")]
        SmartTerrain = SmartBuilder,
        [System.Obsolete("Use SmartBuilder instead")]
        SmartTerrainChunk = SmartBuilder,
        [System.Obsolete("Use SmartBuilder instead")]
        SmartWall = SmartBuilder
    }
}
