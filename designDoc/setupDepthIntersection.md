# Setup Guide: Depth Intersection Effect

## Required Configuration

### 1. Enable Depth Texture in URP Asset

**Path**: `Assets/Settings/UniversalRenderPipelineAsset` (or your URP asset location)

**Steps**:
1. Select the URP Asset in Project window
2. In Inspector, find **Rendering** section
3. Enable `Depth Texture` checkbox

**Why**: The shader uses `SampleSceneDepth()` which requires the depth texture to be available.

---

## Optional: Add Render Feature (Recommended)

While the depth texture setting above is sufficient for basic functionality, adding a Render Feature ensures better control and debugging.

### What is a Render Feature?

A Render Feature in URP allows you to inject custom rendering logic into the rendering pipeline. For depth intersection, we created `DepthIntersectionRenderFeature` to:
- Ensure depth texture is properly configured
- Control render pass timing
- Provide settings in the Inspector

### How to Add the Render Feature

1. **Select URP Renderer**:
   - Go to `Assets/Settings/UniversalRenderPipelineAsset_Renderer`
   - Or whatever your Forward Renderer asset is named

2. **Add Render Feature**:
   - In Inspector, scroll to bottom
   - Click `Add Renderer Feature`
   - Select `Depth Intersection Render Feature`

3. **Configure Settings**:
   - `Render Pass Event`: `After Rendering Transparents` (default)
   - `Enable Depth Texture`: ✓ (checked)

---

## How It Works

### Without Render Feature (Simple Method)
```
URP Asset "Depth Texture" ON
    ↓
Shader uses SampleSceneDepth()
    ↓
Depth intersection effect works
```

### With Render Feature (Recommended Method)
```
URP Asset "Depth Texture" ON
    +
Render Feature ensures proper setup
    ↓
Better control and debugging
    ↓
Depth intersection effect works reliably
```

---

## Verification

### Check if Depth Texture is Working

1. **In Edit Mode**: Drag an object vertically or horizontally
2. **Virtual Grid Plane**: Should appear with grid pattern
3. **Intersection Highlight**: Look for **cyan outline** where plane touches other objects

### Troubleshooting

**Problem**: No cyan intersection outline appears

**Solutions**:
1. ✓ Check URP Asset has "Depth Texture" enabled
2. ✓ Verify shader is `HorizonMini/DepthIntersection`
3. ✓ Check material properties in EditCursor.cs line 331
4. ✓ Ensure camera has URP renderer assigned
5. ✓ Check Frame Debugger (Window > Analysis > Frame Debugger)

**Problem**: Plane is completely invisible

**Solutions**:
1. ✓ Check VolumeGrid exists in scene
2. ✓ Verify virtual grid plane is being created (check Hierarchy during drag)
3. ✓ Material alpha values (_BaseColor.a, _GridColor.a)

---

## Technical Details

### Shader Features Used

```hlsl
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

// In fragment shader:
float sceneDepth = SampleSceneDepth(screenUV);
float sceneDepthEye = LinearEyeDepth(sceneDepth, _ZBufferParams);
```

### Render Queue
- Queue: `Transparent` (3000)
- Ensures plane renders after opaque geometry
- Depth buffer is already populated with scene objects

### Blending
- `Blend SrcAlpha OneMinusSrcAlpha`
- Standard alpha blending for transparency
- Allows grid plane to be semi-transparent while intersection is opaque

---

## Alternative: Custom Render Pass (Advanced)

If you need more control, you could create a custom render pass that:
1. Renders opaque objects to depth buffer
2. Renders transparent grid plane with depth comparison
3. Applies post-processing to intersection edges

**Example structure**:
```csharp
public class DepthIntersectionPass : ScriptableRenderPass
{
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        // 1. Ensure depth texture is ready
        // 2. Set up render targets
        // 3. Draw grid plane with custom material
        // 4. Apply edge detection/glow
    }
}
```

This is **not required** for the current implementation, but can be added later for advanced effects like:
- Glow/bloom on intersection edges
- Animated pulses
- Multi-colored intersections based on object tags
- Screen-space edge detection
