# Manual World Creator - 使用指南

## 功能概述

**Manual World Creator** 是一个编辑器工具，允许你手动在场景中摆放物体，然后将场景保存为永久世界（Permanent World）。

永久世界的特点：
- ✓ 不能被游戏内的删除功能删除
- ✓ 保存为 ScriptableObject 资产文件
- ✓ 可以在项目中直接编辑和版本控制
- ✓ 自动注册到 `PermanentWorldsRegistry`

---

## 使用步骤

### 1. 准备场景

1. 创建一个新场景或打开现有场景
2. 在场景中手动放置你想要的物体（预制体实例）
3. 调整物体的位置、旋转、缩放

### 2. 添加 PlacedObject 组件

每个要保存的物体都需要 `PlacedObject` 组件：

**方法 A - 使用工具自动添加：**
1. 打开工具：`Tools → HorizonMini → Manual World Creator`
2. 点击 "Scan Current Scene"
3. 如果有物体缺少 PlacedObject，工具会列出它们
4. 点击 "Add PlacedObject to All" 自动添加

**方法 B - 手动添加：**
1. 选中物体
2. `Add Component → PlacedObject`
3. 组件会自动记录预制体引用

### 3. 配置世界设置

在 Manual World Creator 窗口中：

```
World Settings:
├─ World Title: "我的自定义世界"
├─ World Author: "设计师名字"
├─ Grid Dimensions: (4, 1, 4)  // 世界网格大小
├─ Sky Color: 天空颜色
├─ Gravity: -9.81
└─ Mark as Permanent: ✓ (勾选以保护世界不被删除)
```

### 4. 选择保存位置

```
Save Path: Assets/Data/ManualWorlds
```

点击 "Browse" 可以选择其他文件夹。

### 5. 扫描并创建

1. 点击 **"Scan Current Scene"** 按钮
   - 工具会列出所有找到的物体
   - 显示哪些物体有 PlacedObject，哪些没有

2. 确认无误后，点击 **"Create Permanent World"** 按钮

3. 完成！工具会创建：
   - WorldData 资产文件
   - 自动添加到 PermanentWorldsRegistry
   - 在 Project 窗口中高亮显示

---

## 工作原理

### 扫描逻辑

工具会扫描场景中的所有 GameObject：
- ✓ 包含：有 `MeshRenderer` 或 `MeshFilter` 的物体
- ✗ 排除：Camera、Light、EventSystem、Canvas 等系统对象

### 数据保存

创建的 WorldData 包含：
```csharp
WorldData {
    worldId: "guid"
    worldTitle: "世界名称"
    gridDimensions: Vector3Int
    props: List<PropData> {
        prefabName: "预制体名称"
        position, rotation, scale
        smartTerrainControlPoint (如果是地形)
        smartWallControlPoints (如果是墙)
    }
}
```

### 永久保护机制

1. WorldData 被添加到 `PermanentWorldsRegistry`
2. `SaveService.DeleteCreatedWorld()` 会检查 `IsPermanentWorld()`
3. 如果是永久世界，拒绝删除并输出警告

---

## 高级用法

### 支持的特殊物体

- **SmartTerrain**: 会保存控制点位置
- **SmartWall**: 会保存所有控制点和高度
- **SmartHouse**: 会保存多个控制点
- **SpawnPoint**: 会保存生成点位置

### 修改已创建的世界

1. 在 Project 窗口找到创建的 WorldData 资产
2. 直接编辑 Inspector 中的属性
3. 保存即可

### 取消永久保护

如果需要允许删除某个永久世界：

1. 找到 `PermanentWorldsRegistry` 资产
2. 从 `Permanent World Ids` 列表中移除对应的 ID
3. 保存

---

## 故障排除

### Q: 扫描后没有找到任何物体？
A: 确保物体有 MeshRenderer 或 MeshFilter 组件。系统对象（Camera、Light等）会被自动排除。

### Q: 创建的世界无法加载物体？
A: 检查 `AssetCatalog` 是否包含这些预制体的引用。

### Q: 永久世界仍然可以被删除？
A: 检查 `AppRoot` 中的 `WorldLibrary` 是否正确引用了 `PermanentWorldsRegistry`。

### Q: 预制体变成粉色？
A: 材质可能不兼容 URP。WorldLibrary 有自动修复功能，但最好使用 URP 兼容的材质。

---

## 文件位置

```
Assets/
├── Scripts/
│   ├── Editor/
│   │   └── ManualWorldCreator.cs       // 编辑器工具
│   ├── Data/
│   │   ├── PermanentWorldsRegistry.cs  // 永久世界注册表
│   │   ├── WorldData.cs                 // 世界数据
│   │   └── WorldMeta.cs                 // 世界元数据
│   └── Core/
│       ├── WorldLibrary.cs              // 世界库（已修改）
│       └── SaveService.cs               // 存档服务（已修改）
└── Data/
    ├── ManualWorlds/                    // 手动创建的世界
    │   └── World_*.asset
    └── PermanentWorldsRegistry.asset    // 注册表资产
```

---

## 示例工作流

```
1. 新建场景 "ForestWorld"
2. 拖入树、石头、房子等预制体
3. 摆放到理想位置
4. Tools → HorizonMini → Manual World Creator
5. 配置：
   - Title: "Forest Adventure"
   - Author: "Designer A"
   - Mark as Permanent: ✓
6. Scan Current Scene
7. Add PlacedObject to All
8. Create Permanent World
9. ✓ 世界创建完成！
```

现在你的 "Forest Adventure" 世界：
- 出现在游戏的世界列表中
- 无法被玩家删除
- 可以在编辑器中直接修改
- 受版本控制保护
