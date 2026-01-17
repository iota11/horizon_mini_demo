# Horizon Mini (Miniverse) — Unity Mobile Prototype Spec (for Claude Code)

You are Claude Code. Create a **Unity (2022+ recommended)** mobile prototype app called **Horizon Mini**. The app simulates a lightweight “metaverse” where content is made of **volumetric tiles**.

Keep the implementation pragmatic: prioritize a smooth, tactile browsing experience, simple building, and clean architecture.

---

## 1) Core Concept: Volume + World

### Volume (minimum unit)
- The smallest building block is a **Volume** of size **8 × 8 × 8** (Unity units).
- A **World** (also referred to as a “World” in the UI) is composed of multiple **contiguous volumes** in a grid.
  - Example: a **2 × 1 × 2** arrangement of volumes forms a Scene footprint:
    - Width: 2 volumes (16 units)
    - Height: 1 volume (8 units)
    - Depth: 2 volumes (16 units)

### Terms
- **World**: A user-facing item that can be browsed, collected, and played. The 3D content of a world (volumes + props + scripts).
- **VolumeGrid**: The layout/grid definition of volumes for a scene.

---

## 2) App Modes (Entry has 2 main functions)

Upon launch, the app supports two major functions:
1) **Browse** (discover worlds from the library, including others’ worlds)
2) **Build** (create/edit a world)

Navigation is via a bottom tab bar:
- **World** → Browse mode
- **Add** → Build mode
- **Home** → Shows your created + collected worlds

---

## 3) Browse Mode (World feed)

### UX
- Show a vertical “feed” of worlds from a library.
- User swipes **up/down** to switch to the next/previous world.
- **Preload** the world above and below to ensure smooth transitions.

### Critical requirement: Worlds are “alive” while browsing
- When a world is visible in the feed, its:
  - animations
  - update loops
  - gameplay logic (within reason)
  should be **active** (not a static thumbnail).
- Preloaded adjacent worlds may be active at a lower tick rate / simplified, but must appear responsive when snapped into view.

### Interaction
- User can rotate the currently viewed world by swiping **left/right**:
  - Horizontal drag rotates the world around the Y-axis.
  - This should not conflict with vertical paging (use a gesture threshold/angle test).

### Right-side vertical buttons (overlay UI)
Three vertically stacked buttons on the right side:
1) **GO**
   - Enters the selected world in **first-person** play mode.
2) **Likes**
   - (Simple toggle / counter prototype. Persist locally.)
3) **Collect**
   - Saves the world to the user’s **collection** (persist locally).

*(If you need to defer “Likes” depth, implement as a toggle + count placeholder.)*

---

## 4) GO → Play Mode (First-person)

When tapping **GO** in Browse:
- Transition to **first-person** inside that world.
- Minimal FPS controls for mobile:
  - Left thumb: virtual joystick for movement
  - Right thumb: swipe to look
- A simple “Exit” button returns to Browse (same world index).

Keep it prototype-simple: CharacterController + camera.

---

## 5) Build Mode (Add)

check build mode md

## 6) Home Mode

When tapping **Home**:
- Show **two horizontal rows** of 3D world cards:
  - Row A: **My Worlds** (created)
  - Row B: **Collected Worlds** (saved)
- Each row supports **left/right swipe** to browse the row like a carousel.
- Each item is a **3D preview** (same “alive” concept, but can be cheaper than Browse).

Selecting a world in Home:
- Tapping a world opens it in **Browse** at that world (or directly opens a detail panel with GO/Collect/Likes).

---

## 7) Data & Persistence (Prototype-level)

### Library
- Include a starter “library” list shipped with the app:
  - A small set (5–10) sample worlds, each with simple animation or logic.
- Worlds are loaded via an abstract interface so the library can later be replaced by network.

### Local persistence
Persist locally:
- collected world IDs
- created worlds (layout + minimal metadata)
- likes state per world (optional)

Use PlayerPrefs for quick prototype, or JSON files in persistentDataPath.

---

## 8) Performance & Loading

### Preloading strategy in Browse
- Keep exactly **3 world instances** around:
  - previous, current, next
- When user swipes to next:
  - recycle the farthest one
  - load the new adjacent
- Must support smooth 60fps-ish interaction on mobile.

### Activation rules
- Current: fully active
- Adjacent preloads: active enough to appear alive instantly, but you may:
  - reduce update frequency
  - disable expensive effects
  - use simplified LOD

---

## 9) Suggested Unity Architecture

Implement with clean separations:

### Main systems
- `AppRoot`
  - holds references to `WorldLibrary`, `SaveService`, `UIRouter`
- `WorldLibrary`
  - provides list of `WorldMeta` + methods to instantiate a world scene
- `WorldInstance`
  - wrapper component for a loaded world (root GameObject + lifecycle)
- `BrowseController`
  - manages paging, preloading, recycling, rotation gesture
- `BuildController`
  - manages grid, placement, serialization
- `HomeController`
  - manages two 3D rows, horizontal scrolling

### Data models
- `WorldMeta` (id, title, author, thumbnail optional)
- `WorldData` (volumes layout, props, optional scripts)
- `VolumeCell` (x,y,z integer grid coords)
- `GridSettings` (cellSize = 8)

### Scene organization
- One Unity scene for the app shell (UI + controllers).
- World content can be:
  - prefabs instantiated under a container, OR
  - additive scenes (optional; prefabs is simpler for prototype).

---

## 10) UI Requirements (Mobile)

### Bottom tab bar (always visible in shell)
- Buttons:
  - **World**
  - **Add**
  - **Home**
- Tapping switches modes.

### Browse overlay
- Right-side vertical buttons:
  - GO
  - Likes
  - Collect
- Minimal text labels are fine.

### Gestures
- Vertical swipe = page world feed
- Horizontal swipe = rotate current world
- Resolve gesture conflicts:
  - If swipe angle is more vertical than horizontal → paging
  - Else → rotation

---

## 11) Deliverables

Produce:
1) Unity C# scripts implementing the above.
2) A simple sample world set (prefabs) demonstrating:
   - at least one looping animation
   - at least one simple “game logic” behavior (e.g., bouncing object, spinning platform, NPC idle)
3) Persistent collected + created worlds.
4) A working mobile-friendly UI (Unity UI Toolkit or UGUI; choose whichever is faster).

---

## 12) Acceptance Criteria

- Launch → bottom tab bar visible.
- World tab:
  - Vertical swipe browses a feed of worlds.
  - Adjacent worlds are preloaded (no hitching when paging).
  - Visible world is animated and logic runs.
  - Horizontal drag rotates world.
  - GO enters first-person play mode.
  - Collect saves the world.
- Add tab:
  - Can place/remove 8×8×8 volumes snapped to grid.
  - Save world to My Worlds.
- Home tab:
  - Two rows: My Worlds + Collected Worlds.
  - Each row scrolls horizontally.
  - Items are 3D previews.

---

## 13) Notes / Constraints

- This is a **prototype**: keep assets simple, focus on interaction + system structure.
- Mobile first: avoid heavy shaders, expensive lighting, large textures.
- Favor readability + modularity in code.

End of spec.
