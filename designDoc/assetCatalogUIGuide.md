# Asset Catalog UI Setup Guide

## 新布局设计

### 左侧：类别标签栏（垂直滚动）
- 固定宽度 150px
- 垂直排列所有类别
- 可上下滚动
- 当前选中类别高亮显示（黄色）

### 右侧：资源网格（5列，垂直滚动）
- 每行显示 5 个资源
- 网格自动排列
- 可上下滚动
- 每个资源显示图标和名称

---

## 使用方法

### 1. 创建 UI 结构

**步骤**：
1. 在 Unity 中打开菜单：`Tools > Create Asset Catalog UI`
2. 选择你的 Canvas（通常是 BuildCanvas）
3. 点击 **"Create UI"** 按钮

**自动创建**：
- `AssetCatalogPanel` - 主面板（屏幕底部 40%）
- 左侧类别面板（150px 宽，垂直滚动）
- 右侧资源网格（5 列，垂直滚动）
- 自动生成的 Prefabs：
  - `Assets/Prefabs/UI/CategoryTabPrefab.prefab`
  - `Assets/Prefabs/UI/AssetItemPrefab.prefab`

---

### 2. 配置 AssetCatalogUI 组件

在创建的 `AssetCatalogPanel` GameObject 上，配置 `AssetCatalogUI` 组件：

**必填字段**：
- **Build Controller**: 拖拽场景中的 BuildController
- **Asset Catalog**: 拖拽你的 AssetCatalog ScriptableObject
- **Panel**: 指向自己（AssetCatalogPanel）
- **Category Tab Container**: `CategoryPanel > CategoryScrollView > Viewport > CategoryTabContainer`
- **Asset Grid Container**: `AssetPanel > AssetScrollView > Viewport > AssetGridContainer`
- **Category Tab Prefab**: `Assets/Prefabs/UI/CategoryTabPrefab.prefab`
- **Asset Item Prefab**: `Assets/Prefabs/UI/AssetItemPrefab.prefab`

---

## UI 层级结构

```
AssetCatalogPanel (Main Panel - 底部 40%)
├── CategoryPanel (Left - 150px fixed width)
│   └── CategoryScrollView
│       └── Viewport
│           └── CategoryTabContainer
│               └── [Category Tabs - created at runtime]
│
└── AssetPanel (Right - flexible width)
    └── AssetScrollView
        └── Viewport
            └── AssetGridContainer (GridLayout: 5 columns)
                └── [Asset Items - created at runtime]
```

---

## 布局参数

### CategoryPanel
- Width: 150px (fixed)
- Background: Dark gray (0.15, 0.15, 0.15)
- Vertical Layout Group:
  - Spacing: 5px
  - Padding: 5px all sides

### Category Tab
- Height: 50px
- Normal Color: Gray (0.3, 0.3, 0.3)
- Selected Color: Yellow (1.0, 0.8, 0.0)
- Font Size: 16

### AssetPanel
- Width: Flexible (fills remaining space)
- Background: Medium gray (0.25, 0.25, 0.25)
- Grid Layout Group:
  - Constraint: 5 columns
  - Cell Size: 100x120px
  - Spacing: 10px
  - Padding: 10px all sides

### Asset Item
- Size: 100x120px
- Icon area: Top 70%
- Label area: Bottom 30%
- Label Font Size: 10
- Word wrap enabled

---

## 运行时行为

### 初始化
1. `AssetCatalogUI.Start()` 调用 `InitializeCatalog()`
2. 扫描 AssetCatalog 中的所有类别
3. 为每个类别创建一个 tab button
4. 默认显示第一个类别的资源

### 切换类别
1. 用户点击类别 tab
2. 清除当前显示的资源
3. 加载新类别的所有资源
4. 更新 tab 高亮状态

### 拖拽资源
1. 用户按住资源图标拖拽
2. `AssetItemDragHandler.OnBeginDrag()` 触发
3. 通知 `BuildController` 的 `PlacementSystem`
4. 在场景中显示预览
5. 释放时放置物体（如果位置有效）

---

## 自定义样式

### 修改颜色

在 `CreateAssetCatalogUI.cs` 中修改：

```csharp
// Panel background
panelBg.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

// Category panel background
bg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

// Category tab colors
colors.normalColor = new Color(0.3f, 0.3f, 0.3f, 1f);
colors.selectedColor = new Color(1f, 0.8f, 0f, 1f); // Yellow

// Asset panel background
bg.color = new Color(0.25f, 0.25f, 0.25f, 1f);
```

### 修改网格列数

在 `CreateAssetPanel()` 中修改：

```csharp
gridLayout.constraintCount = 5; // 改为你想要的列数
```

### 修改资源格子大小

```csharp
gridLayout.cellSize = new Vector2(100, 120); // 宽x高
```

---

## 故障排除

### 问题：类别 tab 不显示

**解决方案**：
1. 检查 AssetCatalog 是否有资源
2. 确认 Category Tab Container 正确赋值
3. 确认 Category Tab Prefab 存在并正确

### 问题：资源显示为空白

**解决方案**：
1. 检查 PlaceableAsset 是否设置了 icon
2. 如果没有 icon，AssetItemPrefab 的 Image 会显示白色方块
3. 可以为每个资源创建预览图标

### 问题：网格布局错乱

**解决方案**：
1. 检查 GridLayoutGroup 的 Constraint 设置
2. 确认 ContentSizeFitter 设置正确
3. 查看 Canvas Scaler 设置

---

## 下一步

1. **创建资源图标**：为每个 PlaceableAsset 生成预览图
2. **添加搜索功能**：在顶部添加搜索框
3. **添加收藏功能**：允许用户标记常用资源
4. **添加动画**：面板展开/收起动画
