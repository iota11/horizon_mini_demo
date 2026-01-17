# 3D Gizmo 系统设置指南

## 概述
3D Gizmo 是 Build Mode 的核心交互工具，允许用户通过拖拽 3D 手柄来移动和旋转物体。

---

## Gizmo 类型

### 1. Move Gizmo（移动操作杆）
- **3个胶囊手柄**：红色(X)、绿色(Y)、蓝色(Z)
- **功能**：拖动手柄沿对应轴移动物体
- **吸附**：支持网格吸附（0.5m增量）
- **中心球**：白色半透明球体

### 2. Rotate Gizmo（旋转操作杆）
- **3个环形手柄**：红色(X轴)、绿色(Y轴)、蓝色(Z轴)
- **功能**：拖动环形绕对应轴旋转物体
- **吸附**：可选15度角度吸附

---

## 快速设置 - 不使用 Prefab

Gizmo 系统设计为**自动创建**，无需预先制作 Prefab。BuildController 会在需要时动态生成 Gizmo。

### 自动模式（推荐）

1. BuildController 已配置完成
2. 在 Edit Move/Rotate 模式下，Gizmo 会自动生成
3. 无需额外设置

---

## 高级设置 - 使用自定义 Prefab（可选）

如果你想自定义 Gizmo 的外观，可以创建 Prefab。

### Step 1: 创建 Move Gizmo Prefab

1. Hierarchy → Create Empty
2. 命名：`MoveGizmoPrefab`
3. Add Component → Move Gizmo（脚本）
4. 在 Inspector 中调整设置：
   - Handle Length: 2
   - Handle Radius: 0.1
   - Center Sphere Radius: 0.3
   - X Color: Red (255, 0, 0)
   - Y Color: Green (0, 255, 0)
   - Z Color: Blue (0, 0, 255)
   - Highlight Color: Yellow (255, 255, 0)

5. 点击 Play 进入游戏模式，Gizmo 会自动创建手柄
6. 停止游戏，查看生成的结构
7. 保存为 Prefab: `Assets/Prefabs/Gizmos/MoveGizmoPrefab.prefab`
8. 删除 Hierarchy 中的实例

### Step 2: 创建 Rotate Gizmo Prefab

1. Hierarchy → Create Empty
2. 命名：`RotateGizmoPrefab`
3. Add Component → Rotate Gizmo（脚本）
4. 在 Inspector 中调整设置：
   - Ring Radius: 1.5
   - Ring Thickness: 0.05
   - Ring Segments: 32
   - Colors: 同上

5. 保存为 Prefab: `Assets/Prefabs/Gizmos/RotateGizmoPrefab.prefab`

### Step 3: 连接到 BuildController

1. 选中 BuildSystem GameObject
2. Build Controller 组件：
   - Move Gizmo Prefab: 拖入 MoveGizmoPrefab
   - Rotate Gizmo Prefab: 拖入 RotateGizmoPrefab

---

## Gizmo 工作原理

### Move Gizmo

1. **创建阶段**：
   - 生成3个胶囊对象（X/Y/Z）
   - 每个胶囊有 Collider 用于触摸检测
   - 生成中心球体

2. **拖拽检测**：
   - 支持鼠标拖拽（编辑器测试）
   - 支持触摸拖拽（移动设备）
   - OnMouseDown/Drag/Up 事件
   - Touch.phase 检测

3. **移动计算**：
   - 创建拖拽平面（垂直于相机视角，包含轴线）
   - Raycast 到平面
   - 投影到对应轴
   - 应用网格吸附

### Rotate Gizmo

1. **创建阶段**：
   - 程序化生成环形网格
   - 32个分段的圆环
   - MeshCollider 用于交互

2. **旋转计算**：
   - 计算屏幕空间拖拽方向
   - 转换为旋转角度
   - 绕对应轴旋转物体
   - 可选角度吸附（15度）

---

## 交互流程

### 双击物体 → Move Mode

1. 用户双击放置的物体
2. BuildController 切换到 EditMove 模式
3. SelectObject() 被调用
4. ShowMoveGizmo() 创建 Move Gizmo
5. Move Gizmo 初始化，跟随物体位置
6. 用户拖拽胶囊手柄
7. 物体沿轴移动
8. 双击空白 → 返回 View Mode

### 长按物体 → Rotate Mode

1. 用户长按物体（>= 1秒）
2. BuildController 切换到 EditRotate 模式
3. ShowRotateGizmo() 创建 Rotate Gizmo
4. 用户拖拽环形手柄
5. 物体绕轴旋转
6. 双击空白 → 返回 View Mode

---

## 调试技巧

### 在 Scene 视图中观察

1. 进入 Play 模式
2. 切换到 Scene 标签
3. 双击一个物体进入 Edit Move 模式
4. 应该看到：
   - 红色胶囊（X轴）指向右
   - 绿色胶囊（Y轴）指向上
   - 蓝色胶囊（Z轴）指向前
   - 白色球体在中心

### 测试拖拽

在编辑器中：
- 鼠标悬停在手柄上 → 变黄色高亮
- 点击并拖拽 → 物体沿轴移动
- 释放鼠标 → 恢复原色

### Console 调试

Gizmo 会输出以下信息（如果启用 Debug.Log）：
- 拖拽开始位置
- 每帧的移动/旋转量
- 吸附后的最终位置

### 常见问题

**问题 1: Gizmo 手柄不可拖拽**
- 检查手柄是否有 Collider
- 确保 Camera 被正确传入 Initialize()
- 检查 Physics Raycaster 是否存在

**问题 2: 拖拽方向不对**
- 检查相机位置
- 确认轴方向（红=X, 绿=Y, 蓝=Z）
- 尝试从不同角度拖拽

**问题 3: 网格吸附不工作**
- 检查 snapToGrid 是否启用
- 调整 gridSize 值（0.5m）
- 在 PlacementSystem 中设置

**问题 4: Gizmo 不跟随物体**
- 确保 Update() 中调用 UpdateGizmoPosition()
- 检查 targetObject 引用是否正确

---

## 性能优化

### 减少网格复杂度

Rotate Gizmo 的环形可以减少分段数：
```csharp
ringSegments = 16; // 默认32，可降低到16
```

### 禁用不需要的功能

如果不需要吸附：
```csharp
moveGizmo.SetSnapToGrid(false);
rotateGizmo.SetSnapRotation(false);
```

---

## 自定义扩展

### 添加更多手柄

可以添加平面手柄（2轴同时移动）：
```csharp
// 在 MoveGizmo.CreateHandles() 中添加
CreatePlaneHandle("XY", Vector3.right, Vector3.up);
CreatePlaneHandle("YZ", Vector3.up, Vector3.forward);
CreatePlaneHandle("XZ", Vector3.right, Vector3.forward);
```

### 改变视觉样式

修改材质：
```csharp
Material mat = new Material(Shader.Find("Unlit/Color"));
mat.SetFloat("_Metallic", 0.5f);
```

### 添加音效反馈

在拖拽事件中：
```csharp
public void OnHandleDragStart(...)
{
    AudioSource.PlayClipAtPoint(dragStartSound, transform.position);
}
```

---

## 总结

Gizmo 系统特点：
- ✅ 完全程序化生成
- ✅ 触摸优化
- ✅ 吸附支持
- ✅ 高亮反馈
- ✅ 易于扩展

不需要手动创建复杂的 Prefab，系统会自动处理！
