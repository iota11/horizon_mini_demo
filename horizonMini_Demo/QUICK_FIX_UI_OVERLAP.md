# 快速修复：UI 重叠问题

## 问题
所有 UI 面板叠在一起，因为默认都是激活状态。

## ✅ 解决方案（两种方法）

---

### 方法 1: 使用自动化向导（推荐）⭐

1. **打开向导**
   ```
   Unity 顶部菜单 → HorizonMini → Build Mode Setup Wizard
   ```

2. **点击自动连接**
   - Step 1: 自动查找 BuildModeUI
   - Step 2: 自动查找 BuildController
   - Step 4: **点击 "🔗 自动连接所有引用"**

3. **完成！**
   - 向导会自动添加 `BuildModeInitializer` 组件
   - 自动设置正确的显示/隐藏状态

---

### 方法 2: 手动设置

#### Step 1: 添加初始化器组件

1. 选中 `BuildModeCanvas` GameObject
2. Add Component → **Build Mode Initializer**

#### Step 2: 连接引用

在 Build Mode Initializer 组件中设置：

- **Volume Size Picker Panel**: 拖入 VolumeSizePickerPanel
- **View Mode UI**: 拖入 ViewModeUI
- **Edit Mode UI**: 拖入 EditModeUI
- **Asset Catalog Panel**: 拖入 AssetCatalogPanel
- **Status Bar**: 拖入 StatusBar
- **Initial Mode**: 选择 `SizePicker`

#### Step 3: 手动隐藏其他面板（可选）

在 Hierarchy 中，手动关闭这些 GameObject：
- ViewModeUI（取消勾选）
- EditModeUI（取消勾选）
- AssetCatalogPanel（取消勾选）

只保留：
- VolumeSizePickerPanel（开启）
- StatusBar（开启）

---

## 🎯 初始化器的作用

`BuildModeInitializer` 会在游戏启动时：

1. **隐藏所有面板**
2. **只显示初始模式的面板**（默认 Size Picker）
3. **在模式切换时自动管理显示/隐藏**

---

## 📋 面板状态说明

### 启动时应该看到：
- ✅ VolumeSizePickerPanel（显示）
- ✅ StatusBar（显示）
- ❌ ViewModeUI（隐藏）
- ❌ EditModeUI（隐藏）
- ❌ AssetCatalogPanel（隐藏）

### 点击 Create 后：
- ❌ VolumeSizePickerPanel（隐藏）
- ✅ ViewModeUI（显示）
- ✅ AssetCatalogPanel（显示）
- ✅ StatusBar（显示）

---

## 🔧 调试技巧

### 检查初始化器是否工作

1. 运行游戏
2. 查看 Console
3. 应该看到：`Build Mode UI 初始化完成 - 初始模式: SizePicker`

### 如果还是重叠

检查这些：
1. BuildModeCanvas 上是否有 `BuildModeInitializer` 组件？
2. 引用是否正确连接？
3. Initial Mode 是否设置为 `SizePicker`？

### 手动测试

在 Inspector 中：
1. 选中一个面板（如 ViewModeUI）
2. 取消勾选激活状态
3. 运行游戏看效果

---

## 💡 建议

**使用方法 1（自动化向导）最简单！**

只需要：
1. 打开向导
2. 点击几个按钮
3. 完成

向导会自动处理所有设置，包括：
- 添加初始化器
- 连接引用
- 设置正确状态

---

## ✅ 完成后测试

运行游戏后应该看到：
1. 只有 Volume Size Picker 显示
2. 顶部状态栏显示
3. 其他面板都隐藏
4. 调整滑块可以改变数值
5. 点击 Create 切换到 View Mode

完美！🎉
