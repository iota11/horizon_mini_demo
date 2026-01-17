# Build Mode UI 设置指南

## 概述
这个指南将帮你在 Unity 中创建完整的 Build Mode UI（竖屏手机布局）。

---

## 前置准备

### 1. 安装 TextMeshPro
1. Window → TextMeshPro → Import TMP Essential Resources
2. 点击 Import

### 2. 创建基础场景
1. File → New Scene → Basic
2. Save As: `Assets/Scenes/BuildModeComplete.unity`

---

## Part 1: 创建 Canvas 和基础设置

### Step 1: 创建 UI Canvas

1. Hierarchy → 右键 → UI → Canvas
2. 命名为 `BuildModeCanvas`
3. 在 Canvas 组件中设置：
   - Render Mode: Screen Space - Overlay
   - Pixel Perfect: ✓（可选）

4. 在 Canvas Scaler 组件中设置：
   - UI Scale Mode: **Scale With Screen Size**
   - Reference Resolution: **1080 × 1920** （竖屏）
   - Match: 0.5

### Step 2: 设置 Canvas 结构

在 BuildModeCanvas 下创建以下空对象：

```
BuildModeCanvas
├── VolumeSizePickerPanel     (整屏模态对话框)
├── ViewModeUI                 (浏览模式 UI)
├── EditModeUI                 (编辑模式 UI)
├── AssetCatalogPanel          (底部抽屉)
└── StatusBar                  (顶部状态栏)
```

创建方法：
- 右键 BuildModeCanvas → Create Empty
- 为每个添加 RectTransform（自动添加）

---

## Part 2: Volume Size Picker UI

### Step 1: 创建面板背景

1. 选中 `VolumeSizePickerPanel`
2. 在 Inspector 中设置 RectTransform：
   - Anchor Presets: 点击左上角方框，按住 Alt+Shift，选择 stretch-stretch（填满）
   - Left: 0, Right: 0, Top: 0, Bottom: 0

3. Add Component → Image
   - Color: 半透明黑色 (0, 0, 0, 200)

### Step 2: 创建内容面板

1. 右键 VolumeSizePickerPanel → UI → Panel
2. 命名为 `ContentPanel`
3. RectTransform 设置：
   - Width: 800
   - Height: 1200
   - Anchor: Center-Middle
   - Pos X: 0, Pos Y: 0

### Step 3: 添加标题

1. 右键 ContentPanel → UI → Text - TextMeshPro
2. 命名为 `TitleText`
3. RectTransform:
   - Anchor: Top-Center
   - Pos Y: -100
   - Width: 600, Height: 100
4. TextMeshPro 设置：
   - Text: "选择空间大小"
   - Font Size: 60
   - Alignment: Center
   - Color: White

### Step 4: 创建 X 轴滑块

1. 右键 ContentPanel → UI → Slider
2. 命名为 `XSlider`
3. RectTransform:
   - Anchor: Top-Center
   - Pos Y: -300
   - Width: 600, Height: 60

4. 添加标签：
   - 右键 XSlider → UI → Text - TextMeshPro
   - 命名为 `XLabel`
   - 放在滑块左边
   - Text: "宽度 (X):"

5. 添加数值显示：
   - 右键 XSlider → UI → Text - TextMeshPro
   - 命名为 `XValueText`
   - 放在滑块右边
   - Text: "2"
   - Font Size: 50

### Step 5: 重复创建 Y 和 Z 滑块

复制 XSlider 两次：
- YSlider (Pos Y: -450)
  - YLabel: "高度 (Y):"
  - YValueText: "1"
- ZSlider (Pos Y: -600)
  - ZLabel: "深度 (Z):"
  - ZValueText: "2"

### Step 6: 添加预览信息

1. 右键 ContentPanel → UI → Text - TextMeshPro
2. 命名为 `PreviewSizeText`
3. RectTransform:
   - Anchor: Top-Center
   - Pos Y: -750
   - Width: 600, Height: 80
4. Text: "Space: 16 × 8 × 16 units"
5. Alignment: Center

### Step 7: 添加描述文本

1. 右键 ContentPanel → UI → Text - TextMeshPro
2. 命名为 `DescriptionText`
3. RectTransform:
   - Pos Y: -850
   - Width: 600, Height: 60
4. Text: "4 volumes (2×1×2)"
5. Font Size: 40

### Step 8: 创建按钮

1. 右键 ContentPanel → UI → Button - TextMeshPro
2. 命名为 `CreateButton`
3. RectTransform:
   - Anchor: Bottom-Center
   - Pos Y: 150
   - Width: 300, Height: 100
4. 按钮文字: "Create"
5. Button 颜色: 绿色

6. 复制创建 `CancelButton`:
   - Pos Y: 50
   - 文字: "Back"
   - 颜色: 灰色

### Step 9: 添加脚本组件

1. 选中 VolumeSizePickerPanel
2. Add Component → Volume Size Picker UI
3. 拖拽连接所有引用：
   - Panel: VolumeSizePickerPanel
   - Create Button: CreateButton
   - Cancel Button: CancelButton
   - X Slider: XSlider
   - Y Slider: YSlider
   - Z Slider: ZSlider
   - X Value Text: XValueText
   - Y Value Text: YValueText
   - Z Value Text: ZValueText
   - Preview Size Text: PreviewSizeText
   - Description Text: DescriptionText

---

## Part 3: Asset Catalog UI (底部抽屉)

### Step 1: 创建主面板

1. 选中 `AssetCatalogPanel`
2. RectTransform:
   - Anchor: Bottom-Stretch
   - Height: 600
   - Left: 0, Right: 0, Bottom: 0

3. Add Component → Image
   - Color: 深灰色 (40, 40, 40, 255)

### Step 2: 创建分类标签栏

1. 右键 AssetCatalogPanel → Create Empty
2. 命名为 `CategoryTabContainer`
3. RectTransform:
   - Anchor: Top-Stretch
   - Height: 100
   - Left: 20, Right: 20, Top: -20

4. Add Component → Horizontal Layout Group
   - Child Alignment: Middle Left
   - Spacing: 10
   - Child Force Expand: Width ✓

### Step 3: 创建分类标签 Prefab

1. 右键 CategoryTabContainer → UI → Button - TextMeshPro
2. 命名为 `CategoryTabPrefab`
3. RectTransform:
   - Width: 150, Height: 80

4. 按钮文字设置：
   - Font Size: 36
   - Text: "Category"

5. 拖到 Project 窗口创建 Prefab:
   - 保存位置: `Assets/Prefabs/UI/CategoryTabPrefab.prefab`
   - 删除 Hierarchy 中的实例

### Step 4: 创建资源网格容器

1. 右键 AssetCatalogPanel → UI → Scroll View
2. 命名为 `AssetScrollView`
3. RectTransform:
   - Anchor: Stretch-Stretch
   - Left: 20, Right: 20, Top: -140, Bottom: 20

4. 找到 Content 子对象:
   - Add Component → Grid Layout Group
   - Cell Size: (150, 180)
   - Spacing: (20, 20)
   - Start Corner: Upper Left
   - Start Axis: Horizontal
   - Child Alignment: Upper Left

### Step 5: 创建资源项 Prefab

**在 Hierarchy 中创建（临时）：**

1. 右键 Hierarchy（任意位置）→ UI → Image
2. 命名为 `AssetItemPrefab`
3. 选中 AssetItemPrefab，在 Inspector 中设置：

   **RectTransform:**
   - Width: 150
   - Height: 180
   - Anchors: 设置为 Top-Left（左上角）

   **Image 组件（背景）:**
   - Color: 深灰色 (60, 60, 60, 255)
   - 这就是卡片的背景

4. **添加图标（子对象）：**
   - 右键 AssetItemPrefab → UI → Image
   - 命名: `Icon`
   - RectTransform 设置:
     - Anchors: Top-Center
     - Pos X: 0, Pos Y: -10
     - Width: 120, Height: 120
   - Image 组件:
     - Source Image: 留空（稍后会动态设置）
     - Color: 白色

5. **添加文字标签（子对象）：**
   - 右键 AssetItemPrefab → UI → Text - TextMeshPro
   - 命名: `Label`
   - RectTransform 设置:
     - Anchors: Bottom-Stretch
     - Left: 5, Right: -5
     - Bottom: 5
     - Height: 40
   - TextMeshPro 设置:
     - Text: "Item Name"
     - Font Size: 20
     - Alignment: Center-Middle
     - Color: 白色

6. **添加 Button 组件（使卡片可点击）：**
   - 选中 AssetItemPrefab（父对象）
   - Add Component → Button
   - Transition: Color Tint（默认即可）

7. **结构检查：**

   此时你的 Hierarchy 应该是这样：
   ```
   AssetItemPrefab (Image + Button)
   ├── Icon (Image)
   └── Label (TextMeshPro)
   ```

8. **保存为 Prefab：**
   - 在 Project 窗口创建文件夹: `Assets/Prefabs/UI`
   - 将 Hierarchy 中的 `AssetItemPrefab` **拖拽** 到 `Assets/Prefabs/UI` 文件夹
   - 会自动创建 Prefab（图标变蓝色）
   - **删除** Hierarchy 中的 AssetItemPrefab 实例（只保留 Project 中的 Prefab）

### Step 6: 添加脚本

1. 选中 AssetCatalogPanel
2. Add Component → Asset Catalog UI
3. 设置引用：
   - Panel: AssetCatalogPanel
   - Category Tab Container: CategoryTabContainer
   - Asset Grid Container: AssetScrollView/Viewport/Content
   - Category Tab Prefab: CategoryTabPrefab
   - Asset Item Prefab: AssetItemPrefab

---

## Part 4: View Mode UI

### Step 1: 创建右侧按钮栏

1. 选中 ViewModeUI
2. 右键 → Create Empty → 命名 `RightButtonBar`
3. RectTransform:
   - Anchor: Right-Stretch
   - Width: 120
   - Right: -20
   - Top: -200, Bottom: 200

### Step 2: 添加 GO 按钮

1. 右键 RightButtonBar → UI → Button - TextMeshPro
2. 命名: `GOButton`
3. RectTransform:
   - Anchor: Top-Center
   - Pos Y: 0
   - Width: 100, Height: 100
4. 文字: "GO"
5. 颜色: 蓝色

### Step 3: 添加 Public 按钮

1. 复制 GOButton
2. 命名: `PublicButton`
3. Pos Y: -150
4. 文字: "Public"
5. 颜色: 绿色

### Step 4: 添加吸附开关

1. 右键 ViewModeUI → UI → Toggle
2. 命名: `SnapToGridToggle`
3. RectTransform:
   - Anchor: Bottom-Left
   - Pos X: 150, Pos Y: 650
   - Width: 250, Height: 60

4. Label 文字: "网格吸附"

5. 复制创建 `SnapToObjectToggle`:
   - Pos Y: 580
   - Label: "物体吸附"

---

## Part 5: Edit Mode UI

### Step 1: 创建删除按钮

1. 右键 EditModeUI → UI → Button - TextMeshPro
2. 命名: `DeleteButton`
3. RectTransform:
   - Anchor: Bottom-Center
   - Pos Y: 650
   - Width: 200, Height: 80
4. 文字: "删除"
5. 颜色: 红色

### Step 2: 创建完成按钮

1. 复制 DeleteButton
2. 命名: `DoneEditingButton`
3. Pos Y: 750
4. 文字: "完成"
5. 颜色: 蓝色

---

## Part 6: 状态栏

### Step 1: 创建顶部状态栏

1. 选中 StatusBar
2. RectTransform:
   - Anchor: Top-Stretch
   - Height: 100
   - Top: 0, Left: 0, Right: 0

3. Add Component → Image
   - Color: (30, 30, 30, 200)

### Step 2: 添加模式文字

1. 右键 StatusBar → UI → Text - TextMeshPro
2. 命名: `ModeStatusText`
3. Text: "模式: 浏览"
4. Anchor: Left
5. Font Size: 40

### Step 3: 添加指令文字

1. 右键 StatusBar → UI → Text - TextMeshPro
2. 命名: `InstructionsText`
3. Text: "拖动物体放置"
4. Anchor: Center
5. Font Size: 32

---

## Part 7: 连接 BuildModeUI 脚本

### Step 1: 添加主 UI 管理器

1. 选中 BuildModeCanvas
2. Add Component → Build Mode UI

### Step 2: 连接所有引用

在 Build Mode UI 组件中设置：

**UI Panels:**
- Size Picker UI: VolumeSizePickerPanel (脚本组件)
- Asset Catalog UI: AssetCatalogPanel (脚本组件)
- View Mode UI: ViewModeUI
- Edit Mode UI: EditModeUI

**View Mode Buttons:**
- Go Button: ViewModeUI/RightButtonBar/GOButton
- Public Button: ViewModeUI/RightButtonBar/PublicButton
- Snap To Grid Toggle: ViewModeUI/SnapToGridToggle
- Snap To Object Toggle: ViewModeUI/SnapToObjectToggle

**Edit Mode Buttons:**
- Delete Button: EditModeUI/DeleteButton
- Done Editing Button: EditModeUI/DoneEditingButton

**Status:**
- Mode Status Text: StatusBar/ModeStatusText
- Instructions Text: StatusBar/InstructionsText

---

## Part 8: 创建 AssetCatalog

### Step 1: 创建 AssetCatalog 资源

1. Project 窗口右键 → Create → HorizonMini → AssetCatalog
2. 命名: `DefaultAssetCatalog`
3. 保存位置: `Assets/Data/DefaultAssetCatalog.asset`

### Step 2: 创建测试 PlaceableAsset

1. 先创建测试 Prefab:
   - Hierarchy → 3D Object → Cube
   - Scale: (1, 1, 1)
   - 添加材质
   - 保存为: `Assets/Prefabs/PlaceableObjects/TestCube.prefab`
   - 删除 Hierarchy 中的实例

2. Project 窗口右键 → Create → HorizonMini → PlaceableAsset
3. 命名: `TestCubeAsset`
4. 设置：
   - Asset Id: `test_cube_001`
   - Display Name: `Test Cube`
   - Category: Furniture
   - Prefab: 拖入 TestCube prefab

5. 在 DefaultAssetCatalog 中：
   - All Assets: 添加 TestCubeAsset

### Step 3: 连接 AssetCatalog 到 UI

1. 选中 AssetCatalogPanel
2. Asset Catalog UI 组件:
   - Asset Catalog: 拖入 DefaultAssetCatalog

---

## Part 9: 设置 BuildController

### Step 1: 创建 BuildSystem GameObject

1. Hierarchy → Create Empty
2. 命名: `BuildSystem`
3. Add Component → Build Controller

### Step 2: 连接引用

- Build Camera: Main Camera
- Build Container: (留空，会自动创建)

### Step 3: 连接到 UI

1. 选中 BuildModeCanvas
2. Build Mode UI 组件:
   - Build Controller: 拖入 BuildSystem

---

## 测试

1. 点击 Play
2. 应该会自动显示 Volume Size Picker
3. 调整滑块
4. 点击 Create
5. 应该切换到 View Mode，显示 Asset Catalog

完成！🎉
