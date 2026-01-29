using UnityEngine;
using System.Collections;

/// <summary>
/// Controls individual card behavior including selection, matching, and animations.
/// </summary>
public class CardController : MonoBehaviour
{
    [Header("Card Data")]
    public string text;
    public int pairId;
    public CardType cardType;
    public bool isMatched;
    public bool isSelected;

    // Animation state
    private float targetY = 0.5f;
    private float baseY = 0.5f;
    private float spawnTime;
    private Vector3 originalScale;
    private Color originalColor;
    private bool isShaking;
    private bool isFloatingAway;
    private float baseX;

    // Theme reference
    private KotobaTheme theme;

    // Renderer and material
    private MeshRenderer meshRenderer;
    private Material cardMaterial;
    private Material topMaterial;

    // Animation settings (defaults, overridden by theme)
    public float dropSpeed = 5f;
    private float hoverAmplitude = 0.03f;
    private float hoverSpeed = 2f;
    private float floatSpeed = 3f;
    private float shrinkSpeed = 2f;
    private float popHeight = 0.3f;
    private float rotationSpeed = 180f;

    /// <summary>
    /// Initialize the card with data and theme
    /// </summary>
    public void Initialize(string cardText, int id, CardType type, KotobaTheme cardTheme)
    {
        text = cardText;
        pairId = id;
        cardType = type;
        theme = cardTheme;
        isMatched = false;
        isSelected = false;
        isFloatingAway = false;
        isShaking = false;
        spawnTime = Time.time;

        // Get settings from theme (values are already in proper units)
        if (theme != null)
        {
            dropSpeed = theme.dropSpeed;
            hoverAmplitude = theme.hoverAmplitude;
            hoverSpeed = theme.hoverFrequency * Mathf.PI * 2f; // Convert cycles/sec to rad/sec
            floatSpeed = theme.floatSpeed;
            shrinkSpeed = theme.shrinkRate;
            popHeight = theme.popHeight;
            rotationSpeed = theme.matchRotationSpeed;
        }

        // Setup materials
        SetupMaterials();
        CreateTextMesh();

        // Store original scale
        originalScale = transform.localScale;
    }

    private void SetupMaterials()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null) return;

        // Determine card color based on type
        if (theme != null)
        {
            originalColor = cardType == CardType.Japanese
                ? theme.japaneseCardColor
                : theme.englishCardColor;
        }
        else
        {
            // Fallback colors if no theme
            originalColor = cardType == CardType.Japanese
                ? new Color(0.306f, 0.804f, 0.769f) // Teal
                : new Color(1f, 0.420f, 0.420f);    // Coral
        }

        // Side material with card color
        cardMaterial = CreateURPMaterial(originalColor);

        // Top material (white for text visibility)
        topMaterial = CreateURPMaterial(Color.white);

        // Apply materials: [right, left, top, bottom, front, back]
        meshRenderer.materials = new Material[]
        {
            cardMaterial, cardMaterial, topMaterial,
            cardMaterial, cardMaterial, cardMaterial
        };
    }

    private Material CreateURPMaterial(Color color)
    {
        // Try URP Lit shader first, fall back to Standard
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material mat = new Material(shader);

        // Set URP Lit properties
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);

        // Set surface properties like BlockMaterial
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0.5f);
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", 0f);

        return mat;
    }

    private void CreateTextMesh()
    {
        // Remove existing text if any
        Transform existing = transform.Find("CardText");
        if (existing != null)
            DestroyImmediate(existing.gameObject);

        // Create TextMesh for displaying the word
        GameObject textObj = new GameObject("CardText");
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = new Vector3(0, 0.51f, 0);
        textObj.transform.localRotation = Quaternion.Euler(90, 180, 0);
        textObj.transform.localScale = Vector3.one;

        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.fontSize = 48;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;

        // Text color from theme or default
        textMesh.color = theme != null ? theme.cardTextColor : new Color(0.176f, 0.204f, 0.212f);

        // Adjust character size based on text length
        if (text.Length <= 2)
            textMesh.characterSize = 0.08f;
        else if (text.Length <= 4)
            textMesh.characterSize = 0.06f;
        else
            textMesh.characterSize = 0.045f;

        // Use a font that supports Japanese if available
        textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    /// <summary>
    /// Set the spawn position (for drop animation)
    /// </summary>
    public void SetSpawnPosition(float x, float z, float startY)
    {
        transform.position = new Vector3(x, startY, z);
        baseX = x;
        targetY = baseY;
        spawnTime = Time.time;

        // Random slight rotation for visual interest
        transform.rotation = Quaternion.Euler(0, Random.Range(-8f, 8f), 0);
    }

    private void Update()
    {
        if (isFloatingAway)
        {
            UpdateFloatAway();
        }
        else if (!isShaking)
        {
            UpdateDropAndHover();
        }
    }

    private void UpdateDropAndHover()
    {
        Vector3 pos = transform.position;
        float dt = Time.deltaTime;

        // Drop animation (smooth MoveTowards for consistent speed)
        if (pos.y > targetY + 0.01f)
        {
            pos.y = Mathf.MoveTowards(pos.y, targetY, dropSpeed * dt);

            // Stop dropping when landed
            if (pos.y <= targetY + 0.01f)
            {
                pos.y = targetY;
                dropSpeed = 0f;
            }
        }
        // Hover animation (only when landed and not selected)
        else if (!isSelected && !isMatched)
        {
            float elapsed = Time.time - spawnTime;
            float hoverOffset = Mathf.Sin(elapsed * hoverSpeed) * hoverAmplitude;
            pos.y = baseY + hoverOffset;
        }

        transform.position = pos;
    }

    private void UpdateFloatAway()
    {
        float dt = Time.deltaTime;

        // Move up smoothly
        Vector3 pos = transform.position;
        pos.y += floatSpeed * dt;
        transform.position = pos;

        // Spin smoothly
        transform.Rotate(0, rotationSpeed * dt, 0);

        // Shrink smoothly (use MoveTowards for consistent shrinking)
        Vector3 scale = transform.localScale;
        float newScale = Mathf.MoveTowards(scale.x, 0f, shrinkSpeed * dt);
        transform.localScale = new Vector3(newScale, newScale, newScale);

        // Deactivate when small enough or high enough
        if (newScale < 0.05f || pos.y > 12f)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Select this card (highlight and pop up)
    /// </summary>
    public void Select()
    {
        if (isMatched) return;

        isSelected = true;

        // Change to selected color
        Color selectedColor = theme != null ? theme.selectedColor : new Color(0.996f, 0.792f, 0.341f);
        SetCardColor(selectedColor);

        // Pop up
        Vector3 pos = transform.position;
        pos.y = baseY + popHeight;
        transform.position = pos;
    }

    /// <summary>
    /// Deselect this card (return to original state)
    /// </summary>
    public void Deselect()
    {
        if (isMatched) return;

        isSelected = false;
        SetCardColor(originalColor);

        // Return to base height
        Vector3 pos = transform.position;
        pos.y = baseY;
        transform.position = pos;
    }

    /// <summary>
    /// Mark this card as matched (turn green and float away)
    /// </summary>
    public void MarkMatched()
    {
        isMatched = true;
        isSelected = false;

        Color matchedColor = theme != null ? theme.matchedColor : new Color(0.114f, 0.820f, 0.631f);
        SetCardColor(matchedColor);

        // Start float away animation
        isFloatingAway = true;
    }

    /// <summary>
    /// Play shake animation for mismatch feedback
    /// </summary>
    public void Shake()
    {
        if (!isShaking)
        {
            StartCoroutine(ShakeCoroutine());
        }
    }

    private IEnumerator ShakeCoroutine()
    {
        isShaking = true;
        float originalX = transform.position.x;
        float elapsed = 0f;
        float duration = 0.2f;
        float amplitude = theme != null ? theme.shakeAmplitude : 0.1f;

        while (elapsed < duration)
        {
            float shake = Mathf.Sin(elapsed * 50f) * amplitude * (1f - elapsed / duration);
            Vector3 pos = transform.position;
            pos.x = originalX + shake;
            transform.position = pos;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Return to original position
        Vector3 finalPos = transform.position;
        finalPos.x = originalX;
        transform.position = finalPos;

        isShaking = false;
    }

    private void SetCardColor(Color color)
    {
        if (cardMaterial != null)
        {
            // Set both URP and Standard color properties
            if (cardMaterial.HasProperty("_BaseColor"))
                cardMaterial.SetColor("_BaseColor", color);
            if (cardMaterial.HasProperty("_Color"))
                cardMaterial.SetColor("_Color", color);
        }
    }

    /// <summary>
    /// Reset the card for reuse (pooling)
    /// </summary>
    public void ResetCard()
    {
        isMatched = false;
        isSelected = false;
        isFloatingAway = false;
        isShaking = false;
        transform.localScale = originalScale;
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        // Clean up materials
        if (cardMaterial != null)
            Destroy(cardMaterial);
        if (topMaterial != null)
            Destroy(topMaterial);
    }
}
