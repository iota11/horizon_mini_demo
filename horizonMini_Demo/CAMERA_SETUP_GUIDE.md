# Camera Setup Guide - Fixed Perspective for Volume Sizing

## Problem Solved

**Before**: Camera zoomed in/out as you adjusted volume sliders, making it hard to perceive actual size changes.

**Now**: Camera stays at a fixed distance, allowing you to feel the real size difference between volumes.

---

## How It Works

### Initial Setup (First Time)
```
1. User enters Size Picker mode
2. Camera positions itself to fit the MAXIMUM possible volume (4x4x4)
3. Camera distance is locked at this position
```

### During Slider Adjustment
```
1. User drags X/Y/Z sliders
2. Volume size changes in real-time
3. Camera target updates to follow volume center
4. Camera distance STAYS THE SAME ← Key improvement!
```

### Result
- Small volume (2x1x2): Looks small, lots of empty space around it
- Large volume (4x4x4): Fills the view, clearly bigger
- **You can perceive the actual size difference!**

---

## Configuration in Unity

### BuildController Settings

Select `BuildController` in Hierarchy, in Inspector you'll see:

```
Volume Settings:
├─ Max Volume Size: (4, 4, 4)  ← Maximum slider values
└─ Volume Unit Size: 8         ← Each unit = 8 meters
```

**Max Volume Size**: Must match your UI slider ranges
- X slider: 1-4 → Max X = 4
- Y slider: 1-4 → Max Y = 4
- Z slider: 1-4 → Max Z = 4

**Volume Unit Size**: World space size per grid cell
- Default: 8 meters per cell
- Max volume = 4x4x4 cells = 32x32x32 meters

### Camera Distance Calculation

Camera automatically positions at:
```
distance = maxVolumeSize.magnitude * 1.5 * volumeUnitSize
```

For (4, 4, 4) with 8m units:
```
magnitude = √(32² + 32² + 32²) = 55.4 meters
distance = 55.4 * 1.5 = 83.1 meters
```

This ensures the largest volume fits comfortably in view.

---

## BuildCameraController Methods

### New Methods Added:

#### `SetupForMaxVolume(maxDimensions, volumeSize)`
Called once when first volume is created.
```csharp
// Example: Setup for 4x4x4 max, 8m per unit
cameraController.SetupForMaxVolume(new Vector3Int(4, 4, 4), 8f);
```

**Behavior**:
- Calculates camera distance for max volume
- Sets initial target position
- Distance is LOCKED after this

#### `UpdateTarget(newCenter)`
Called when volume size changes.
```csharp
// Update camera to look at new volume center
cameraController.UpdateTarget(volumeGrid.GetCenter());
```

**Behavior**:
- Updates target position only
- Distance stays the same
- Camera smoothly follows center

#### `FocusOnBounds(bounds)` (Legacy)
Old method - still available for other uses.
```csharp
// Zooms camera to fit bounds perfectly
cameraController.FocusOnBounds(volumeGrid.GetBounds());
```

**Behavior**:
- Calculates distance to fit bounds
- Changes camera distance
- Use for initial setup in other modes

---

## Usage Flow

### Size Picker Mode:
```
1. Enter mode → Camera sets up for max volume (ONCE)
2. Adjust sliders → Camera follows center (distance unchanged)
3. Small volumes look small ✓
4. Large volumes look large ✓
5. Real size perception maintained ✓
```

### View Mode:
```
1. Click Create → Switch to View mode
2. Camera maintains position
3. User can orbit/pan/zoom freely
4. Build objects in the volume
```

---

## Testing the Setup

### Test 1: Size Perception
1. Run game
2. Set volume to minimum (1x1x1)
3. Note: Should look tiny in the view
4. Set volume to maximum (4x4x4)
5. Note: Should fill most of the view
6. ✅ You should clearly see the size difference

### Test 2: Camera Stability
1. Set volume to (2x2x2)
2. Note camera position
3. Change to (3x3x3)
4. Check: Camera distance unchanged ✓
5. Check: Only target position shifted ✓

### Test 3: Max Volume Fits
1. Set all sliders to maximum (4x4x4)
2. Check: Entire volume visible ✓
3. Check: Not clipping at edges ✓
4. Check: Reasonable margin around volume ✓

---

## Customization

### Adjust Max Volume Size

If you want larger/smaller maximum volumes:

```csharp
// BuildController Inspector
Max Volume Size: (6, 6, 6)  // Larger range
Volume Unit Size: 8
```

**Remember**: Update UI sliders to match!
- VolumeSizePickerUI → X/Y/Z slider maxValue = 6

### Adjust Camera Distance Factor

If max volume doesn't fit well:

```csharp
// In BuildCameraController.SetupForMaxVolume()
currentDistance = maxBoundSize * 2.0f;  // More distance
// or
currentDistance = maxBoundSize * 1.2f;  // Closer
```

Default is `1.5f` - good balance for most cases.

### Adjust Camera Limits

```csharp
// BuildCameraController Inspector
Min Distance: 5
Max Distance: 100  // Increase if max volume is huge
```

---

## Common Scenarios

### Scenario: "Max volume gets clipped"
**Solution**:
- Increase `maxDistance` in BuildCameraController
- Or decrease distance multiplier (1.5 → 2.0)

### Scenario: "Small volumes too tiny to see"
**Solution**:
- This is intentional for size perception
- User can manually zoom in with pinch gesture
- Or reduce `Max Volume Size` if game doesn't need large volumes

### Scenario: "Camera too far away"
**Solution**:
- Reduce distance multiplier in `SetupForMaxVolume()`
- Change `1.5f` to `1.2f` or `1.0f`

### Scenario: "Want different max sizes for X/Y/Z"
**Solution**:
Already supported! Just set:
```
Max Volume Size: (8, 2, 8)  // Wide and shallow
```
Camera will fit the bounding box.

---

## Technical Notes

### Why Lock Camera Distance?

**Goal**: Size perception
- Fixed viewpoint = consistent scale reference
- Brain can judge absolute sizes
- Like looking at objects on a table from same chair

**Alternative Approach** (not used):
- Camera zooms to fit each volume
- Always fills the view
- Loses size perception
- All volumes "feel" the same size

### Camera Target vs Distance

**Target** (`targetPosition`):
- Where camera looks at
- Changes with volume center
- Updates smoothly

**Distance** (`currentDistance`):
- How far camera is from target
- Set once for max volume
- Stays locked during size adjustments

### Orbit/Pan/Zoom Still Work

User can still:
- ✅ Orbit: Rotate around volume
- ✅ Pan: Move view left/right/up/down
- ✅ Zoom: Pinch to get closer/farther

These are manual overrides - automatic zooming is disabled.

---

## Debugging

### Check Camera Position

In Scene view while running:
1. Select Main Camera
2. Check Transform → Position
3. Adjust sliders
4. Position.magnitude should stay roughly constant ✓

### Check Target Position

Add to `BuildCameraController.UpdateTarget()`:
```csharp
Debug.Log($"Camera target updated to: {targetPosition}, distance: {currentDistance}");
```

Should see:
- Target changing as volume resizes
- Distance staying constant

### Visualize Camera Gizmo

In Scene view:
1. Select Main Camera
2. Gizmo should show frustum
3. Max volume should fit within frustum
4. Adjust sliders - frustum stays same size ✓

---

## Summary

✅ **Camera sets up once** for maximum volume
✅ **Distance locked** during size adjustments
✅ **Only target updates** as volume changes
✅ **Size perception** maintained - feel the difference!
✅ **Max volume guaranteed** to fit in view
✅ **User can still** orbit/pan/zoom manually

Your volume sizing now has proper spatial awareness! 🎯
