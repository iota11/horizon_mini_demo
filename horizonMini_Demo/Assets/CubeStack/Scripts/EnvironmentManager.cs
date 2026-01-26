using UnityEngine;
using System.Collections.Generic;

public class EnvironmentManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;

    private int _currentZone = -1;
    private List<GameObject> _spawnedProps = new List<GameObject>();
    private StackTheme _currentTheme;

    public void Reset()
    {
        // Clear all spawned props
        foreach (var prop in _spawnedProps)
        {
            if (prop != null) Destroy(prop);
        }
        _spawnedProps.Clear();
        _currentZone = -1;
    }

    public void InitializeZone(int zoneIndex, StackTheme theme)
    {
        _currentTheme = theme;
        _currentZone = zoneIndex;

        if (theme == null) return;

        // Set initial sky color
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = theme.skyColorBottom;
        }

        // Spawn initial environment props
        if (theme.environmentProps != null)
        {
            foreach (var propPrefab in theme.environmentProps)
            {
                if (propPrefab != null)
                {
                    var prop = Instantiate(propPrefab, transform);
                    _spawnedProps.Add(prop);
                }
            }
        }
    }

    public void CheckZoneTransition(int currentHeight, StackTheme theme)
    {
        if (theme == null || theme.zones == null) return;

        for (int i = theme.zones.Length - 1; i >= 0; i--)
        {
            if (currentHeight >= theme.zones[i].triggerHeight && i > _currentZone)
            {
                TransitionToZone(i, theme.zones[i]);
                break;
            }
        }
    }

    private void TransitionToZone(int zoneIndex, ZoneTransition zone)
    {
        _currentZone = zoneIndex;

        // Update sky color
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = zone.newSkyColor;
        }

        // Spawn zone props
        if (zone.propsToSpawn != null)
        {
            foreach (var propPrefab in zone.propsToSpawn)
            {
                if (propPrefab != null)
                {
                    var prop = Instantiate(propPrefab, transform);
                    _spawnedProps.Add(prop);
                }
            }
        }

        Debug.Log($"Entered zone: {zone.zoneName}");
    }

    public void SetCamera(Camera cam)
    {
        mainCamera = cam;
    }
}
