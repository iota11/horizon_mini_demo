using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages card spawning, pooling, and grid layout for the KotobaMatch game.
/// </summary>
public class CardManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cardsContainer;

    [Header("Pooling")]
    [SerializeField] private int initialPoolSize = 20;

    // Internal state
    private Queue<GameObject> cardPool = new Queue<GameObject>();
    private List<CardController> activeCards = new List<CardController>();
    private KotobaTheme theme;

    /// <summary>
    /// Initialize the card manager with a theme
    /// </summary>
    public void Initialize(KotobaTheme cardTheme)
    {
        theme = cardTheme;

        // Create cards container if not assigned
        if (cardsContainer == null)
        {
            GameObject container = new GameObject("CardsContainer");
            container.transform.SetParent(transform);
            container.transform.localPosition = Vector3.zero;
            cardsContainer = container.transform;
        }

        // Pre-warm the pool
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject card = CreateCardObject();
            card.SetActive(false);
            cardPool.Enqueue(card);
        }
    }

    private GameObject CreateCardObject()
    {
        // Create a cube for the card
        GameObject card = GameObject.CreatePrimitive(PrimitiveType.Cube);
        card.name = "WordCard";
        card.transform.SetParent(cardsContainer);
        card.transform.localScale = theme.cardDimensions;

        // Add CardController
        card.AddComponent<CardController>();

        // Setup collider (already added by CreatePrimitive)
        // Setup shadow
        MeshRenderer mr = card.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            mr.receiveShadows = true;
        }

        return card;
    }

    /// <summary>
    /// Spawn a new round of cards with the given word pairs
    /// </summary>
    public void SpawnCards(WordPair[] pairs)
    {
        // Clear any existing active cards
        ClearCards();

        // Create card data for all words
        List<CardSpawnData> spawnData = new List<CardSpawnData>();

        for (int i = 0; i < pairs.Length; i++)
        {
            // Japanese card
            spawnData.Add(new CardSpawnData
            {
                text = pairs[i].japanese,
                pairId = i,
                cardType = CardType.Japanese
            });

            // English card
            spawnData.Add(new CardSpawnData
            {
                text = pairs[i].english,
                pairId = i,
                cardType = CardType.English
            });
        }

        // Shuffle the spawn data
        ShuffleList(spawnData);

        // Calculate grid positions
        float spacingX = theme.gridSpacing;
        float spacingZ = theme.gridSpacing;
        int cols = theme.gridColumns;

        // Center offset
        float offsetX = (cols - 1) * spacingX / 2f;
        float offsetZ = ((spawnData.Count / cols) - 1) * spacingZ / 2f;

        // Spawn cards
        for (int i = 0; i < spawnData.Count; i++)
        {
            int col = i % cols;
            int row = i / cols;

            float x = col * spacingX - offsetX;
            float z = row * spacingZ - offsetZ;
            float startY = 5f + Random.Range(0f, 5f);

            CardController card = GetCard();
            card.Initialize(spawnData[i].text, spawnData[i].pairId, spawnData[i].cardType, theme);
            card.SetSpawnPosition(x, z, startY);
            card.gameObject.SetActive(true);

            activeCards.Add(card);
        }
    }

    /// <summary>
    /// Clear all active cards (return to pool)
    /// </summary>
    public void ClearCards()
    {
        foreach (CardController card in activeCards)
        {
            if (card != null && card.gameObject != null)
            {
                card.ResetCard();
                card.gameObject.SetActive(false);
                cardPool.Enqueue(card.gameObject);
            }
        }
        activeCards.Clear();
    }

    /// <summary>
    /// Get all active cards
    /// </summary>
    public List<CardController> GetActiveCards()
    {
        return activeCards;
    }

    /// <summary>
    /// Check if all active cards are matched
    /// </summary>
    public bool AreAllMatched()
    {
        foreach (CardController card in activeCards)
        {
            if (!card.isMatched)
                return false;
        }
        return activeCards.Count > 0;
    }

    /// <summary>
    /// Get count of remaining unmatched cards
    /// </summary>
    public int GetUnmatchedCount()
    {
        int count = 0;
        foreach (CardController card in activeCards)
        {
            if (!card.isMatched)
                count++;
        }
        return count;
    }

    private CardController GetCard()
    {
        GameObject cardObj;

        if (cardPool.Count > 0)
        {
            cardObj = cardPool.Dequeue();
        }
        else
        {
            cardObj = CreateCardObject();
        }

        CardController controller = cardObj.GetComponent<CardController>();
        if (controller == null)
        {
            controller = cardObj.AddComponent<CardController>();
        }

        return controller;
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    /// <summary>
    /// Internal data structure for card spawning
    /// </summary>
    private struct CardSpawnData
    {
        public string text;
        public int pairId;
        public CardType cardType;
    }
}
