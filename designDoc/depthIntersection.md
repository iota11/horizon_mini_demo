# Depth Intersection Effect

## Overview
The virtual grid plane in Edit mode now has a **depth intersection highlight** effect. When the plane intersects with scene objects, a cyan outline appears at the intersection edge.

## How It Works

### Shader: `DepthIntersection.shader`
- **Location**: `Assets/Shaders/DepthIntersection.shader`
- **Type**: Transparent shader with depth comparison

### Key Features:
1. **Grid Pattern**: Shows snap grid (0.5m spacing)
2. **Depth Comparison**: Samples scene depth buffer using `SampleSceneDepth()`
3. **Intersection Highlight**: Compares plane depth vs scene depth
4. **Cyan Outline**: Highlights where plane touches objects

### Shader Properties:
```hlsl
_BaseColor             // Plane base color (blue, semi-transparent)
_GridColor             // Grid line color (white)
_GridSize              // Grid cell size (0.5m)
_GridThickness         // Grid line thickness
_IntersectionColor     // Intersection highlight color (cyan)
_IntersectionThickness // How thick the intersection edge is
```

### Algorithm:
```hlsl
1. Sample scene depth buffer at screen position
2. Get current plane surface depth
3. Calculate depth difference: abs(sceneDepth - surfaceDepth)
4. Create highlight when difference < threshold
5. Only show when plane is behind geometry
6. Blend cyan color at intersection
```

## Usage

### In EditCursor.cs
The shader is automatically applied when creating the virtual grid plane during drag operations.

### Requirements
- **URP Depth Texture**: Must be enabled in URP Asset
  - Go to: `Assets/Settings/UniversalRenderPipelineAsset`
  - Enable: `Depth Texture` under "Rendering"

### Visual Effect
- **Grid Plane**: Semi-transparent blue with white grid lines
- **Intersection**: Bright cyan outline where plane touches objects
- **Real-time**: Updates as you drag objects vertically or horizontally

## Technical Notes

### Depth Buffer Access
Uses URP's `DeclareDepthTexture.hlsl` which provides:
- `SampleSceneDepth(uv)` - Sample depth buffer
- `LinearEyeDepth()` - Convert to linear eye space depth

### Performance
- Minimal overhead (one depth texture sample per pixel)
- Only active during drag operations
- Destroyed when drag ends

### Customization
You can adjust intersection appearance in `EditCursor.cs` line 331:
```csharp
mat.SetColor("_IntersectionColor", new Color(0f, 1f, 1f, 1f)); // Cyan
mat.SetFloat("_IntersectionThickness", 0.1f); // Edge thickness
```

## Future Enhancements
- Add glow effect to intersection
- Animate intersection pulse
- Different colors for different object types
- Adjust brightness based on intersection angle
