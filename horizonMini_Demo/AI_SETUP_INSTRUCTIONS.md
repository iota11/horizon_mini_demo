# AI Scene Generator 配置说明

## 步骤1：创建AISceneGenerator组件

1. 在Build场景的Hierarchy中找到一个合适的GameObject（建议挂在BuildController同级）
2. 创建新的空GameObject，命名为 `AISceneGenerator`
3. 添加 `AISceneGenerator` 组件到这个GameObject

## 步骤2：配置AISceneGenerator

在Inspector中配置以下字段：

### Open AI Api Key (必填)
- 输入你的OpenAI API Key
- 获取方式：https://platform.openai.com/api-keys

### Open AI Model (默认值)
- 默认：`gpt-4o-mini`（推荐，便宜且快）
- 可选：`gpt-4o`, `gpt-4-turbo`

### Asset Library (必填)
- 拖动 `Assets/Data/BedroomAssetLibrary.asset` 到这个字段

### Log Api Calls (可选)
- 勾选：在Console显示API请求和响应（用于调试）

## 步骤3：连接AISceneGeneratorUI

1. 在Hierarchy中找到 `Canvas -> AISceneGeneratorPanel`
2. 选中后在Inspector找到 `AISceneGeneratorUI` 组件
3. 在 `Ai Scene Generator` 字段中，拖动步骤1创建的 `AISceneGenerator` GameObject

## 步骤4：测试

1. 运行Build场景
2. 创建一个Volume（或加载已有world）
3. 进入View Mode
4. 点击右侧 `AI Generate` 按钮
5. 输入提示词，例如：
   - "Create a cozy bedroom"
   - "Modern minimalist bedroom"
   - "Vintage bedroom with lots of books"
6. 点击 `Generate Scene`
7. 等待5-10秒（调用OpenAI API）
8. 场景中会自动生成家具！

## 预期结果

- Console会显示：
  - 用户prompt
  - Volume大小
  - OpenAI请求JSON
  - OpenAI响应JSON
  - 成功实例化的对象列表

- 场景中会出现：
  - 床、灯、地毯、装饰品等卧室家具
  - 按照prompt风格排列

## 常见问题

### Q: 提示"OpenAI API Key not set"
A: 检查AISceneGenerator组件的openAIApiKey字段是否填写

### Q: 提示"AssetLibrary not assigned"
A: 检查是否拖动了BedroomAssetLibrary.asset到Asset Library字段

### Q: API调用失败
A:
1. 检查网络连接
2. 检查API Key是否有效
3. 检查OpenAI账户是否有余额
4. 查看Console的完整错误信息

### Q: 生成的物体位置不对
A: 这是正常的，AI可能需要调整。可以：
1. 修改prompt，更具体描述布局
2. 手动调整生成的物体位置
3. 重新生成

### Q: 物体重叠
A: AI有时会忽略碰撞，可以：
1. 在prompt中强调"不要重叠"
2. 手动调整
3. 改进OpenAI的system prompt（在AISceneGenerator.cs中）

## API费用参考

使用 `gpt-4o-mini` 模型：
- 每次生成约消耗 1000-2000 tokens
- 费用约 $0.001 - $0.002 USD
- 非常便宜！

## 重要更新 (2026-01-25)

### ✅ 已完成优化：

1. **修正地面Y坐标**
   - 地面现在正确位于 `volumeCenter.y - (volumeSize.y * 8 / 2)` 位置
   - AI会生成地面坐标为负数的物体（例如Y=-4m表示地面）

2. **支持基于已有对象的AI生成**
   - 系统会自动检测场景中所有PlacedObject（包括SmartTerrain）
   - AI会基于已有布局进行补充，不会与现有物体重叠
   - 示例：如果场景中已有地形，可以输入"在这个地形上添加树木和石头"

3. **智能对象检测**
   - 检测所有PlacedObject、SmartTerrain、SmartTerrainChunk
   - 使用SmartTerrain.GetSize()获取准确的实时尺寸（即使修改了厚度）
   - 传递位置、大小、类型信息给AI

4. **自动吸附到Terrain表面** ⭐ NEW
   - 生成的物体会自动吸附到SmartTerrain的顶部表面
   - 不依赖LLM计算，系统自动处理
   - 壁画、镜子等wall-mounted物体除外（保持原始位置）
   - 支持多个terrain，自动吸附到最高的terrain表面

5. **自动朝向中心点** ⭐ NEW
   - 所有生成的物体自动旋转朝向volume中心点
   - 旋转角度自动吸附到90度增量（0°, 90°, 180°, 270°）
   - 确保物体排列整齐，面向中心区域

6. **智能替换现有物体** ⭐ NEW
   - 可以要求AI替换场景中已有的物体
   - 示例："change to a different type of bed"
   - AI会识别要替换的物体，生成新物体在相同位置
   - 自动删除旧物体，保持场景整洁

7. **自动碰撞分离** ⭐ NEW
   - 生成后自动执行最多10次碰撞分离迭代
   - 使用Unity Physics.ComputePenetration进行精确碰撞检测
   - 自动为没有Collider的物体添加BoxCollider
   - 支持旋转物体的碰撞检测（比AABB更准确）
   - 将重叠物体在水平面(XZ)推开，保持在terrain表面
   - 如果10次迭代后仍有重叠，自动删除较小的物体
   - 确保最终场景无重叠，布局合理

8. **自动墙壁生成和吸附** ⭐ NEW
   - 每次生成时自动创建两面墙壁：
     - 左墙（-X方向）：0.5m厚
     - 后墙（+Z方向）：0.5m厚
   - 墙壁prefab从AssetCatalog自动获取（无需手动配置）
   - Wall-mounted物体（画、镜子、钟等）自动吸附到最近的墙上
   - 吸附时自动调整位置和旋转，面向房间
   - 如果2米内找不到墙，自动删除wall-mounted物体
   - 墙壁使用SmartTerrain创建，可编辑调整

### 使用方法：

1. **从零开始生成**：
   - 创建空Volume
   - 输入prompt："Create a cozy bedroom"
   - AI会生成完整场景

2. **基于已有对象补充**：
   - 先手动放置一些物体（例如床、地形）
   - 输入prompt："添加配套家具" 或 "在地形上添加装饰"
   - AI会检测现有物体并智能补充

3. **替换现有物体**：
   - 已生成场景后，输入prompt："change to a different type of bed"
   - 或："replace the chair with an armchair"
   - AI会识别要替换的物体名称，生成新物体替换它
   - 保持相同位置，自动删除旧物体

## 下一步优化

1. 添加Undo功能
2. 支持删除AI生成的特定物体
3. 优化碰撞检测精度
4. 支持更多房间类型（客厅、厨房等）
