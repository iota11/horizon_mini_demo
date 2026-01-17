# Horizon Mini — Build Mode Detailed Spec (Unity Mobile Prototype) (for Claude Code)

You are Claude Code. Extend the **Build** feature with a multi-mode editor: **View / Edit / Play**. Implement it for mobile (touch-first). Keep it prototype-feasible but functional.

---

## 0) Build Mode Overview

Entering **Build Mode** (bottom tab: **Add**) opens a builder flow:

1) **Volume Size Picker (pre-edit step)**
2) **Build View Mode** (default after picking size)
3) **Build Edit Mode** (object manipulation: move/rotate/delete)
4) **Build Play Mode** (FPS test play inside the built world)

Build Mode must support:
- max scene bounds of **4 × 4 × 4 volumes** (each volume = 8×8×8 units)
- object catalog with categories + drag & drop placement into the 3D scene
- snapping options:
  - Snap to Grid (0.5m increments)
  - Snap to Obj (snap to surfaces of existing objects via bbox)

Persist built worlds:
- Save to “My Worlds” and show in world lists (Browse/Home) after publishing.

---

## 1) Step 1 — Volume Size Picker (like 3ds Max cube creation)

### Goal
Before editing, show a UI step allowing the user to choose the **volume grid size** of the scene.

### UX
- Top: a preview 3D area showing the volume box.
- Bottom: a modal/popup panel with:
  - Size selection controls:
    - X volumes: 1–4
    - Y volumes: 1–4
    - Z volumes: 1–4
  - A confirm button: **Create**
  - A cancel/back button: **Back**

### Behavior
- As user changes X/Y/Z, update the preview.
- On **Create**, instantiate the base VolumeGrid and transition to **Build View Mode**.

### Constraints
- Maximum: **4×4×4 volumes**.
- Volume cell size = 8 units.
- Total build bounds should be visible and act as placement boundary.

---

## 2) Build Modes

Build Mode has three modes:
- **View Mode** (default, navigation + selection)
- **Edit Mode** (manipulate selected object: Move or Rotate)
- **Play Mode** (FPS test)

---

## 3) View Mode

### Camera controls in View Mode
Touch gestures on empty space:
- **1-finger drag (on empty space)**: orbit/rotate the 3D view
- **2-finger pinch**: zoom
- **2-finger drag**: pan

### Selection & editing entry
- **Double tap an object** → enter **Edit Mode: Move**
- **Long press an object (>= 1s)** → enter **Edit Mode: Rotate**
- **Double tap empty space** → return to View Mode (if currently in Edit)
- If user is already in View Mode and double taps empty, do nothing (or reset selection).

### UI in View Mode
- A **GO** button: enters Play Mode
- A **Public** button: saves/publishes the current build to “My Worlds” and ensures it appears in:
  - Home → My Worlds row
  - Browse library list (your own list) / view list

*(Public = “save to your world list”; prototype can treat as local publish.)*

---

## 4) Object Catalog Panel + Drag & Drop Placement (Edit placement flow)

### Catalog panel layout
Bottom drawer/popup:
- multiple **category tabs** (labels)
- each category shows a grid/list of **placeable items** (prefab icons)

### Placement interaction
- User drags an item from the catalog into the 3D viewport.
- When drag enters viewport:
  - show a “ghost” preview of the item following the pointer
- When released:
  - place the object at a valid location determined by raycasting.

### Placement positioning rules
On drop:
1) Raycast from screen point into the scene.
2) Prefer hits in this order:
   - existing objects (their colliders)
   - volume floor/base plane collider(s)
3) Determine placement point:
   - If hit object surface: use hit point + slight normal offset
   - If hit volume base: use hit point on base plane

### Placement boundary
- Ensure placed objects are within the selected volume bounds.
- If out of bounds, clamp or reject (reject is fine for prototype: show subtle error).

### Entering Edit Mode after placing
After placing:
- Option A (preferred): remain in View Mode with the new object selected
- Double tap (or explicit tap) can enter Edit Mode
- (Do NOT auto-enter edit move unless simpler; either is acceptable if consistent.)

---

## 5) Snapping Controls

Provide two toggle buttons (or toggles) accessible in builder UI (View & Edit modes):

### A) Snap to Grid (0.5m increments)
- Toggle: `snapToGrid`
- When enabled:
  - object position should snap to a 3D grid with **0.5m** spacing (0.5 Unity units if 1 unit = 1m).
- Applies to:
  - placement ghost following the pointer
  - Move mode manipulation

### B) Snap to Obj
- Button or toggle: `snapToObj`
- When active, object placement/move should snap to surfaces of existing objects.
- Prototype requirement:
  - Use target objects’ **bounding boxes (AABB)** to find the closest face/surface point.
  - Snap the selected object so its bbox touches the target bbox face (simple contact).

Rules:
- If both Snap to Grid and Snap to Obj are active:
  - Snap-to-Obj takes priority when a target is detected under pointer.
  - Otherwise fallback to grid snap.

---

## 6) Edit Mode

Edit Mode includes two sub-modes:
- **Move Mode**
- **Rotate Mode**

Edit Mode UI is **3D UI** attached around the selected object.

### Entering Edit Mode
- Double tap object → Edit Mode (Move)
- Long press object (>=1s) → Edit Mode (Rotate)

### Exiting Edit Mode
- Double tap empty space → return to View Mode

---

## 7) Edit Mode: Move Mode (3-axis capsule gizmos)

When in Move Mode:
- Spawn a local XYZ gizmo at the object pivot:
  - 3 capsule-like handles (X, Y, Z)
  - oriented in the object’s **local** axes
- Also spawn a **Delete** button as 3D UI near the object.

#### Move interaction
- User drags a capsule handle:
  - movement constrained along that axis
- The object moves accordingly.
- Apply snapping:
  - Snap to Grid if enabled
  - Snap to Obj if enabled (snap to bbox surface of nearby/pointed object)

#### Delete interaction
- Tapping Delete removes the object from the scene.

Prototype constraints:
- Keep gizmo interaction simple:
  - When handle is grabbed, lock movement mode until release.
  - Use ray-plane intersection aligned to axis plane for stability.

---

## 8) Edit Mode: Rotate Mode (3-axis ring gizmos)

When in Rotate Mode:
- Show 3 ring handles around object (XYZ):
  - circles aligned to local axes planes
- User drags a ring:
  - rotates object about that axis
- Rotation snapping (optional):
  - If time allows: 15° increments toggle
  - Otherwise free rotation is acceptable

Exit:
- Double tap empty → View Mode

---

## 9) View Mode vs Edit Mode Camera Behavior

- In **Edit Mode**, camera orbit/pan/zoom should still work when dragging empty space.
- If user drags on gizmo, it manipulates; if drags elsewhere, it navigates camera.

Gesture conflict resolution:
- Gizmo hit test first:
  - If touch begins on gizmo collider → manipulation
  - Else → camera control

---

## 10) Play Mode (Build test)

From **View Mode**, tapping **GO**:
- Switch to **Play Mode**
- Spawn player and enable FPS controls:
  - left virtual joystick → move
  - right swipe → look
- Provide an **Exit** button:
  - returns to **View Mode** in builder
  - player despawns / disables FPS controller
- World logic should run normally.

---

## 11) Public / Publish

In **View Mode**, tapping **Public**:
- Serialize the built world:
  - volume grid dimensions (X,Y,Z)
  - list of placed objects (prefab id, position, rotation, scale)
- Save it into **My Worlds** (local persistence for prototype).
- Ensure it appears in:
  - **Home → My Worlds** row
  - the general **world view list** (your personal list / library section)

Show feedback:
- “Published!” toast/banner

---

## 12) Implementation Guidance (Unity)

### Suggested components
- `BuildController` (mode state machine)
  - states: `SizePicker`, `View`, `EditMove`, `EditRotate`, `Play`
- `VolumeGrid` (bounds + placement floor colliders)
- `CatalogUI` (categories, items, drag events)
- `PlacementSystem`
  - raycast placement, ghost preview, snapping
- `SelectionSystem`
  - hit testing, double tap/long press detection
- `GizmoMove` / `GizmoRotate`
  - 3D UI prefabs with colliders
- `PublishService` / `SaveService`

### Physics/raycasting
- All placeable objects should have colliders.
- Volume grid should include:
  - a base collider plane (or per-volume floor colliders)

### Touch handling
- Use EnhancedTouch or Input System.
- Implement:
  - double tap detection (time threshold)
  - long press detection (>= 1s without significant movement)
  - gesture angle/intent for camera vs rotation in browse (already exists) and for camera vs gizmo in build.

---

## 13) Acceptance Criteria (Build)

- Enter Build → choose volume size up to 4×4×4 → Create.
- View Mode:
  - camera orbit/pan/zoom works
  - catalog panel shows categories + items
  - drag item into scene places it via raycast
  - snapping toggles work (0.5m grid + bbox surface snap)
  - double tap object → Move mode gizmo + delete button
  - long press object → Rotate mode rings
  - double tap empty → back to View Mode
  - GO → Play Mode FPS; Exit returns to View Mode
  - Public saves world into My Worlds and shows in lists

End of Build spec.
