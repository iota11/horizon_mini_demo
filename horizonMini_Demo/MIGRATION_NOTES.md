# Build Mode 新旧版本迁移说明

## 重要说明

项目中现在有**两个不同版本**的 BuildController：

### 旧版本（简化体素系统）
- 位置：之前创建的简单版本
- 功能：键盘控制放置/删除体素
- 文件：已删除，被新版本替代

### 新版本（完整 Build Mode）
- 位置：`Assets/Scripts/Controllers/BuildController.cs`
- 功能：完整的场景编辑器，带 3D Gizmo
- 文件：最新版本

---

## API 变化

### 旧版 BuildController 方法（已移除）

```csharp
// ❌ 这些方法不再存在
buildController.SaveWorld();        // private 方法
buildController.NewWorld();         // 不存在
buildController.SetPlacementMode(); // 不存在
```

### 新版 BuildController 方法

```csharp
// ✅ 新的公共方法
buildController.OnPublicButtonPressed();  // 发布世界
buildController.OnGoButtonPressed();      // 进入测试模式
buildController.DeleteSelectedObject();   // 删除选中物体
buildController.CreateVolumeGrid(size);   // 创建空间
buildController.SwitchMode(mode);         // 切换模式
```

---

## UI 系统变化

### 旧系统：UIRouter
- 直接连接按钮到 Controller 方法
- 适用于简单场景

### 新系统：BuildModeUI
- 专门为 Build Mode 设计
- 管理多个 UI 面板
- 根据模式切换 UI

---

## 如何使用

### 选项 1: 使用新的 BuildModeUI（推荐）

如果你使用完整的 Build Mode：

1. **不使用** UIRouter
2. **使用** BuildModeUI
3. 按照 `BUILD_UI_SETUP_GUIDE.md` 设置

### 选项 2: 继续使用 UIRouter（浏览模式）

如果你只使用 Browse/Home/Play 模式：

1. UIRouter 仍然可以用于这些模式
2. Build 模式的按钮已被注释掉
3. 不影响其他功能

---

## 已修复的问题

### 错误信息
```
error CS0122: 'BuildController.SaveWorld()' is inaccessible due to its protection level
error CS1061: 'BuildController' does not contain a definition for 'NewWorld'
error CS1061: 'BuildController' does not contain a definition for 'SetPlacementMode'
```

### 解决方案
- `SaveWorld()` → `OnPublicButtonPressed()`
- `NewWorld()` → 已注释（不需要）
- `SetPlacementMode()` → 已注释（不需要）

---

## 推荐设置

### 场景结构

```
Scene
├── AppRoot (如果使用完整应用)
│   ├── BrowseController
│   ├── HomeController
│   └── PlayController
│
└── BuildSystem (独立 Build Mode)
    ├── BuildController
    └── BuildModeUI (Canvas)
```

### 两种使用方式

#### 方式 A: 独立 Build Mode
```
BuildSystem GameObject
├── BuildController
├── (自动创建的子系统)
└── BuildModeUI (在 Canvas 上)
```

#### 方式 B: 集成到 AppRoot
```
AppRoot GameObject
├── WorldLibrary
├── SaveService
├── BrowseController
├── HomeController
├── PlayController
└── BuildController
```

---

## 下一步建议

### 如果你想测试完整的 Build Mode：

1. **创建独立测试场景**
   - 新建场景
   - 只添加 BuildSystem + BuildModeUI
   - 按照 `BUILD_UI_SETUP_GUIDE.md` 设置

2. **测试核心功能**
   - Volume Size Picker
   - Asset Catalog 拖放
   - 3D Gizmo 编辑

3. **验证成功后集成**
   - 再连接到 AppRoot
   - 添加模式切换

### 如果你想保持简单：

1. **只使用 Browse/Home/Play**
   - UIRouter 继续工作
   - 忽略 Build Mode

2. **等待后续简化**
   - 可以创建简化版 Build Mode
   - 或者使用预设场景

---

## 常见问题

### Q: UIRouter 和 BuildModeUI 冲突吗？
A: 不冲突。UIRouter 用于 Browse/Home/Play，BuildModeUI 专门用于 Build Mode。

### Q: 我必须删除 UIRouter 吗？
A: 不需要。UIRouter 仍然有效，只是 Build 相关按钮被注释了。

### Q: 如何恢复旧的简单 Build 系统？
A: 可以，但不推荐。新系统更完整，功能更强大。

### Q: 编译错误都修复了吗？
A: 是的。UIRouter 中的 Build 按钮已注释，不会报错。

---

## 总结

- ✅ 编译错误已修复
- ✅ UIRouter 仍可用于其他模式
- ✅ BuildModeUI 是新 Build Mode 的正确选择
- ✅ 两个系统可以共存
- ✅ 推荐使用新的完整 Build Mode

按照 `BUILD_MODE_COMPLETE_SUMMARY.md` 了解新系统的完整功能！
