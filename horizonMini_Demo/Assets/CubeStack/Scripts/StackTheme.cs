using UnityEngine;

[CreateAssetMenu(fileName = "GameTheme", menuName = "CubeStack/Theme")]
public class StackTheme : ScriptableObject
{
    public string themeName;

    [Header("Environment")]
    public Color skyColorBottom = new Color(0.5f, 0.7f, 1f);
    public Color skyColorTop = new Color(0.2f, 0.4f, 0.8f);
    public Material groundMaterial;
    public GameObject[] environmentProps;

    [Header("Blocks")]
    public Gradient blockColorGradient;
    public Material blockMaterial;

    [Header("Zones")]
    public ZoneTransition[] zones;

    [Header("Audio")]
    public AudioClip placementSound;
    public AudioClip perfectSound;
    public AudioClip failSound;
    public AudioClip backgroundMusic;
}

[System.Serializable]
public class ZoneTransition
{
    public int triggerHeight;
    public Color newSkyColor;
    public GameObject[] propsToSpawn;
    public string zoneName;
}
