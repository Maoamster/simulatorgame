using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DeckBuilder : MonoBehaviour
{
    [Header("UI References")]
    public Transform cardCollectionContent;
    public Transform deckContent;
    public GameObject cardPrefab;
    public TMPro.TextMeshProUGUI deckCountText;
    public Button saveButton;
    public TMPro.TMP_InputField deckNameInput;

    [Header("Settings")]
    public List<Card> availableCards = new List<Card>();
    public int maxDeckSize = 30;
    public int maxCopiesOfCard = 2;

    // Private variables
    private List<Card> _currentDeck = new List<Card>();
    private Dictionary<Card, int> _cardCounts = new Dictionary<Card, int>();

    private void Start()
    {
        // Initialize UI
        PopulateCardCollection();
        UpdateDeckUI();

        // Set up save button
        saveButton.onClick.AddListener(SaveDeck);
    }

    private void PopulateCardCollection()
    {
        // Clear existing cards
        foreach (Transform child in cardCollectionContent)
        {
            Destroy(child.gameObject);
        }

        // Add all available cards
        foreach (Card card in availableCards)
        {
            GameObject cardObj = Instantiate(cardPrefab, cardCollectionContent);
            DeckBuilderCardUI cardUI = cardObj.GetComponent<DeckBuilderCardUI>();

            if (cardUI != null)
            {
                cardUI.SetupCard(card);
                cardUI.OnCardClicked += AddCardToDeck;
            }
        }
    }

    private void UpdateDeckUI()
    {
        // Clear existing cards
        foreach (Transform child in deckContent)
        {
            Destroy(child.gameObject);
        }

        // Add all cards in current deck
        foreach (Card card in _currentDeck)
        {
            GameObject cardObj = Instantiate(cardPrefab, deckContent);
            DeckBuilderCardUI cardUI = cardObj.GetComponent<DeckBuilderCardUI>();

            if (cardUI != null)
            {
                cardUI.SetupCard(card);
                cardUI.OnCardClicked += RemoveCardFromDeck;

                // Show count if multiple copies
                int count = _cardCounts[card];
                if (count > 1)
                {
                    cardUI.SetCount(count);
                }
            }
        }

        // Update deck count
        int totalCards = 0;
        foreach (int count in _cardCounts.Values)
        {
            totalCards += count;
        }

        deckCountText.text = $"Deck: {totalCards}/{maxDeckSize}";

        // Enable/disable save button
        saveButton.interactable = totalCards == maxDeckSize;
    }

    private void AddCardToDeck(Card card)
    {
        // Check if deck is full
        int totalCards = 0;
        foreach (int count in _cardCounts.Values)
        {
            totalCards += count;
        }

        if (totalCards >= maxDeckSize)
            return;

        // Check if we already have max copies
        if (_cardCounts.ContainsKey(card) && _cardCounts[card] >= maxCopiesOfCard)
            return;

        // Add card to deck
        if (!_cardCounts.ContainsKey(card))
        {
            _currentDeck.Add(card);
            _cardCounts[card] = 1;
        }
        else
        {
            _cardCounts[card]++;
        }

        // Update UI
        UpdateDeckUI();
    }

    private void RemoveCardFromDeck(Card card)
    {
        if (_cardCounts.ContainsKey(card))
        {
            _cardCounts[card]--;

            if (_cardCounts[card] <= 0)
            {
                _currentDeck.Remove(card);
                _cardCounts.Remove(card);
            }

            // Update UI
            UpdateDeckUI();
        }
    }

    public void ClearDeck()
    {
        _currentDeck.Clear();
        _cardCounts.Clear();
        UpdateDeckUI();
    }

    private void SaveDeck()
    {
        string deckName = deckNameInput.text;
        if (string.IsNullOrEmpty(deckName))
        {
            deckName = "New Deck";
        }

        // Create deck data
        DeckData deckData = new DeckData
        {
            deckName = deckName,
            cards = new List<Card>()
        };

        // Add all cards with their counts
        foreach (Card card in _currentDeck)
        {
            for (int i = 0; i < _cardCounts[card]; i++)
            {
                deckData.cards.Add(card);
            }
        }

        // Save deck to player prefs or file
        SaveDeckToPlayerPrefs(deckData);

        Debug.Log($"Deck '{deckName}' saved with {deckData.cards.Count} cards.");
    }

    private void SaveDeckToPlayerPrefs(DeckData deckData)
    {
        // Convert deck to JSON
        string deckJson = JsonUtility.ToJson(deckData);

        // Save to player prefs
        PlayerPrefs.SetString("Deck_" + deckData.deckName, deckJson);
        PlayerPrefs.Save();

        // Add to deck list
        string deckList = PlayerPrefs.GetString("DeckList", "");
        if (!deckList.Contains(deckData.deckName))
        {
            if (!string.IsNullOrEmpty(deckList))
            {
                deckList += ",";
            }
            deckList += deckData.deckName;
            PlayerPrefs.SetString("DeckList", deckList);
            PlayerPrefs.Save();
        }
    }

    public void LoadDeck(string deckName)
    {
        string deckJson = PlayerPrefs.GetString("Deck_" + deckName, "");
        if (!string.IsNullOrEmpty(deckJson))
        {
            DeckData deckData = JsonUtility.FromJson<DeckData>(deckJson);

            // Clear current deck
            _currentDeck.Clear();
            _cardCounts.Clear();

            // Add cards from saved deck
            foreach (Card card in deckData.cards)
            {
                AddCardToDeck(card);
            }

            // Update UI
            deckNameInput.text = deckData.deckName;
            UpdateDeckUI();
        }
    }
}

[System.Serializable]
public class DeckData
{
    public string deckName;
    public List<Card> cards;
}