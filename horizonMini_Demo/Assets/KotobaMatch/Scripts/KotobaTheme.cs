using UnityEngine;

/// <summary>
/// Defines the card type for word matching
/// </summary>
public enum CardType
{
    Japanese,
    English
}

/// <summary>
/// Represents a Japanese-English word pair
/// </summary>
[System.Serializable]
public struct WordPair
{
    public string japanese;
    public string english;

    public WordPair(string jp, string en)
    {
        japanese = jp;
        english = en;
    }
}

/// <summary>
/// ScriptableObject that holds all theme configuration for KotobaMatch game.
/// Includes word pairs, colors, audio clips, and game settings.
/// </summary>
[CreateAssetMenu(fileName = "KotobaTheme", menuName = "KotobaMatch/Theme")]
public class KotobaTheme : ScriptableObject
{
    public string themeName = "Default";

    [Header("Word Data")]
    [Tooltip("List of Japanese-English word pairs for the matching game")]
    public WordPair[] wordPairs = new WordPair[]
    {
        new WordPair("ねこ", "Cat"),
        new WordPair("いぬ", "Dog"),
        new WordPair("みず", "Water"),
        new WordPair("き", "Tree"),
        new WordPair("ひ", "Fire"),
        new WordPair("つき", "Moon"),
        new WordPair("たいよう", "Sun"),
        new WordPair("とり", "Bird"),
        new WordPair("さかな", "Fish"),
        new WordPair("ほん", "Book")
    };

    [Header("Card Colors")]
    [Tooltip("Color for Japanese word cards")]
    public Color japaneseCardColor = new Color(0.306f, 0.804f, 0.769f, 1f); // 0x4ecdc4

    [Tooltip("Color for English word cards")]
    public Color englishCardColor = new Color(1f, 0.420f, 0.420f, 1f); // 0xff6b6b

    [Tooltip("Color when a card is selected")]
    public Color selectedColor = new Color(0.996f, 0.792f, 0.341f, 1f); // 0xfeca57

    [Tooltip("Color when cards are matched")]
    public Color matchedColor = new Color(0.114f, 0.820f, 0.631f, 1f); // 0x1dd1a1

    [Header("Environment Colors")]
    [Tooltip("Color for the stage platform")]
    public Color stageColor = new Color(0.875f, 0.902f, 0.914f, 1f); // 0xdfe6e9

    [Tooltip("Color for stage borders")]
    public Color borderColor = new Color(0.698f, 0.745f, 0.765f, 1f); // 0xb2bec3

    [Tooltip("Background/sky color")]
    public Color backgroundColor = new Color(0.969f, 0.969f, 0.969f, 1f); // 0xf7f7f7

    [Tooltip("Background color on game over")]
    public Color gameOverBackgroundColor = new Color(1f, 0.8f, 0.8f, 1f); // 0xffcccc

    [Header("Text Colors")]
    [Tooltip("Color for text on cards")]
    public Color cardTextColor = new Color(0.176f, 0.204f, 0.212f, 1f); // 0x2d3436

    [Header("Audio")]
    [Tooltip("Sound played on successful match")]
    public AudioClip matchSound;

    [Tooltip("Sound played on failed match")]
    public AudioClip mismatchSound;

    [Tooltip("Sound played on game over")]
    public AudioClip gameOverSound;

    [Tooltip("Background music during gameplay")]
    public AudioClip backgroundMusic;

    [Header("Game Settings")]
    [Tooltip("Total game duration in seconds")]
    public int gameDuration = 60;

    [Tooltip("Number of word pairs per round")]
    public int pairsPerRound = 6;

    [Header("Card Settings")]
    [Tooltip("Card dimensions (width, height, depth)")]
    public Vector3 cardDimensions = new Vector3(1.5f, 1f, 1.5f);

    [Tooltip("Spacing between cards in the grid")]
    public float gridSpacing = 2.2f;

    [Tooltip("Number of columns in the card grid")]
    public int gridColumns = 4;

    [Tooltip("Number of rows in the card grid")]
    public int gridRows = 3;

    [Header("Animation Settings")]
    [Tooltip("Speed at which cards drop (units per second)")]
    public float dropSpeed = 5f;

    [Tooltip("Amplitude of the hover animation")]
    public float hoverAmplitude = 0.05f;

    [Tooltip("Frequency of the hover animation (cycles per second)")]
    public float hoverFrequency = 2f;

    [Tooltip("Speed at which matched cards float away (units per second)")]
    public float floatSpeed = 3f;

    [Tooltip("Rate at which matched cards shrink (units per second)")]
    public float shrinkRate = 1f;

    [Tooltip("Height cards pop up when selected")]
    public float popHeight = 0.3f;

    [Tooltip("Rotation speed of matched cards (degrees per second)")]
    public float matchRotationSpeed = 180f;

    [Header("Timing Settings")]
    [Tooltip("Delay before respawning new cards after all matched")]
    public float respawnDelay = 0.8f;

    [Tooltip("Interval between shake iterations")]
    public float shakeInterval = 0.02f;

    [Tooltip("Number of shake iterations on mismatch")]
    public int shakeIterations = 10;

    [Tooltip("Amplitude of shake animation")]
    public float shakeAmplitude = 0.1f;

    [Header("Stage Settings")]
    [Tooltip("Size of the game stage")]
    public float stageSize = 10f;

    /// <summary>
    /// Gets a random selection of word pairs for a round
    /// </summary>
    public WordPair[] GetRandomPairs(int count)
    {
        if (wordPairs == null || wordPairs.Length == 0)
            return new WordPair[0];

        count = Mathf.Min(count, wordPairs.Length);
        WordPair[] shuffled = (WordPair[])wordPairs.Clone();

        // Fisher-Yates shuffle
        for (int i = shuffled.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            WordPair temp = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = temp;
        }

        WordPair[] result = new WordPair[count];
        System.Array.Copy(shuffled, result, count);
        return result;
    }
}
