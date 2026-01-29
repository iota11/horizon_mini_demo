# Storage Migration Plan - User Created Worlds (JSON)

## Current Architecture (Discovered)

### ❌ Outside Git (Cannot Sync):
**Location:** `~/Library/Application Support/DefaultCompany/horizonMini_Demo/`

**Files:**
- `horizonmini_save.json` - User progress (list of created world IDs)
- `world_{worldId}.json` - Individual world data files

**World JSON Structure:**
```json
{
    "worldId": "a214d8ba-841f-4732-a08d-e15d3384fe5b",
    "worldTitle": "My World",
    "worldAuthor": "Creator",
    "isDraft": false,
    "gridDimensions": { "x": 1, "y": 1, "z": 1 },
    "volumes": [...],
    "props": [
        {
            "propId": "...",
            "prefabName": "...",
            "position": {...},
            "rotation": {...},
            "scale": {...},
            "smartTerrainControlPoint": {...},
            "smartWallControlPoints": [],
            "smartWallHeight": 0.0
        }
    ],
    "skyColor": {...},
    "gravity": -9.81,
    "miniGames": [...]
}
```

### ✅ In Git (Manual Export):
**Location:** `Assets/Data/ManualWorlds/`
- `World_My_World_20260125_020831.asset` - ScriptableObject format
- Only works when manually exported via `PermanentWorldSaver.SaveAsPermanent()`

## Problem
1. **User worlds in persistentDataPath cannot be synced via git**
   - Lost if app data cleared
   - Cannot collaborate across devices
   - No version control

2. **Manual export to .asset is cumbersome**
   - Requires Editor
   - Separate format from runtime JSON
   - Extra step for users

## Proposed Solution: StreamingAssets JSON Storage

### Architecture
```
Assets/StreamingAssets/Worlds/
├── Published/                     # ✅ Git-tracked, synced
│   ├── world_123.json
│   ├── world_456.json
│   └── index.json                 # World registry
└── .gitkeep

~/Library/Application Support/.../  # ❌ Local only
├── Drafts/                         # New: separate drafts
│   ├── world_draft_1.json
│   └── world_draft_2.json
└── horizonmini_save.json          # User progress
```

### Workflow
1. **Create World** → Saved to `persistentDataPath/Drafts/world_{id}.json`
2. **Edit World** → Auto-save to draft location
3. **Publish World** → Copy JSON to `StreamingAssets/Worlds/Published/`
4. **Git Commit** → Published worlds now in version control
5. **Git Pull** → Other devices get published worlds automatically

### Why StreamingAssets?
- ✅ Can read at runtime on all platforms
- ✅ JSON format works in git (readable diffs)
- ✅ Same format as persistentDataPath (no conversion)
- ✅ In Editor: can write to StreamingAssets
- ✅ In Build: read-only (safe, use persistentDataPath for edits)

## Implementation Plan

### Phase 1: Folder Structure
```bash
mkdir -p Assets/StreamingAssets/Worlds/Published
mkdir -p Assets/StreamingAssets/Worlds/Drafts  # Optional: for testing
```

**Update .gitignore:**
```gitignore
# User drafts (local only)
horizonMini_Demo/Assets/StreamingAssets/Worlds/Drafts/

# Published worlds (git-tracked)
!horizonMini_Demo/Assets/StreamingAssets/Worlds/Published/
```

### Phase 2: Update SaveService

**Add new paths:**
```csharp
public class SaveService : MonoBehaviour
{
    // Existing: persistentDataPath for drafts
    private string DraftPath =>
        Path.Combine(Application.persistentDataPath, "Drafts");

    // New: StreamingAssets for published (read-only in build)
    private string PublishedPathRead =>
        Path.Combine(Application.streamingAssetsPath, "Worlds/Published");

#if UNITY_EDITOR
    // In Editor: can write to StreamingAssets
    private string PublishedPathWrite =>
        Path.Combine(Application.dataPath, "StreamingAssets/Worlds/Published");
#endif

    // Save draft (existing behavior)
    public void SaveWorld(WorldData worldData)
    {
        string path = Path.Combine(DraftPath, $"world_{worldData.worldId}.json");
        // ... save JSON
    }

    // New: Publish to git
    public bool PublishWorld(string worldId)
    {
#if UNITY_EDITOR
        string draftPath = Path.Combine(DraftPath, $"world_{worldId}.json");
        string publishPath = Path.Combine(PublishedPathWrite, $"world_{worldId}.json");

        if (!File.Exists(draftPath))
        {
            Debug.LogError($"Draft world {worldId} not found");
            return false;
        }

        // Copy JSON file
        File.Copy(draftPath, publishPath, overwrite: true);

        // Update index.json
        UpdatePublishedIndex();

        Debug.Log($"✓ Published world {worldId} to {publishPath}");
        return true;
#else
        Debug.LogWarning("Publishing only available in Editor");
        return false;
#endif
    }

    // Load world (check both locations)
    public WorldData LoadWorld(string worldId)
    {
        // Try published first
        string publishedPath = Path.Combine(PublishedPathRead, $"world_{worldId}.json");
        if (File.Exists(publishedPath))
        {
            return LoadWorldFromPath(publishedPath);
        }

        // Fallback to draft
        string draftPath = Path.Combine(DraftPath, $"world_{worldId}.json");
        if (File.Exists(draftPath))
        {
            return LoadWorldFromPath(draftPath);
        }

        return null;
    }

    // New: Get all worlds (published + drafts)
    public List<WorldData> GetAllWorlds()
    {
        List<WorldData> worlds = new List<WorldData>();

        // Load published
        if (Directory.Exists(PublishedPathRead))
        {
            foreach (var file in Directory.GetFiles(PublishedPathRead, "world_*.json"))
            {
                worlds.Add(LoadWorldFromPath(file));
            }
        }

        // Load drafts
        if (Directory.Exists(DraftPath))
        {
            foreach (var file in Directory.GetFiles(DraftPath, "world_*.json"))
            {
                string worldId = Path.GetFileNameWithoutExtension(file).Replace("world_", "");
                // Skip if already loaded from published
                if (!worlds.Any(w => w.worldId == worldId))
                {
                    worlds.Add(LoadWorldFromPath(file));
                }
            }
        }

        return worlds;
    }
}
```

### Phase 3: Migration Tool

Create editor tool to migrate existing worlds:

```csharp
[MenuItem("Tools/Migrate Worlds to StreamingAssets")]
public static void MigrateWorlds()
{
    string persistentPath = Application.persistentDataPath;
    string streamingPath = Path.Combine(Application.dataPath, "StreamingAssets/Worlds/Published");

    // Ensure folder exists
    Directory.CreateDirectory(streamingPath);

    // Copy all world JSON files
    foreach (var file in Directory.GetFiles(persistentPath, "world_*.json"))
    {
        string fileName = Path.GetFileName(file);
        string destPath = Path.Combine(streamingPath, fileName);
        File.Copy(file, destPath, overwrite: true);
        Debug.Log($"Migrated: {fileName}");
    }

    AssetDatabase.Refresh();
}
```

### Phase 4: UI Updates

**Build Mode:**
- Add "Publish to Git" button when editing a world
- Show badge: "Draft" vs "Published"
- Add "Pull from Git" button to sync published worlds

**Browse Mode:**
- Show worlds from both sources
- Filter: "My Drafts" / "Published" / "All"

## Benefits

✅ **Version Control** - All published worlds in git
✅ **Sync Across Devices** - Git pull/push syncs worlds
✅ **Same Format** - JSON in both locations, no conversion
✅ **No Breaking Changes** - Drafts still work in persistentDataPath
✅ **Clear Separation** - Draft vs Published workflow
✅ **Collaboration Ready** - Multiple people can contribute worlds
✅ **Git-Friendly** - JSON diffs are readable

## Migration Steps for User

1. Run migration tool: `Tools > Migrate Worlds to StreamingAssets`
2. Review migrated worlds in `StreamingAssets/Worlds/Published/`
3. Commit to git: `git add Assets/StreamingAssets/Worlds/`
4. Push: `git push`
5. Other devices: `git pull` to get worlds

## Next Steps

1. ✅ Identify current storage location (DONE)
2. ⬜ Create StreamingAssets folder structure
3. ⬜ Update SaveService with dual paths
4. ⬜ Implement PublishWorld() method
5. ⬜ Create migration tool
6. ⬜ Add UI buttons (Publish, Pull from Git)
7. ⬜ Update .gitignore
8. ⬜ Test publish workflow
9. ⬜ Migrate existing worlds
