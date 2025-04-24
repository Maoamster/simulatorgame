using UnityEngine;
using System.Collections.Generic;

public class CardCollectionManager : MonoBehaviour
{
    public static CardCollectionManager Instance { get; private set; }

    [Header("Card Collection")]
    public List<Card> allCards = new List<Card>();

    // Player's collection
    private Dictionary<Card, int> _playerCollection = new Dictionary<Card, int>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Load player collection
        LoadCollection();
    }

    public List<Card> GetAllCards()
    {
        // Return a copy of all available cards
        return new List<Card>(allCards);
    }

    public void AddCardToCollection(Card card, int count = 1)
    {
        if (_playerCollection.ContainsKey(card))
        {
            _playerCollection[card] += count;
        }
        else
        {
            _playerCollection[card] = count;
        }

        // Save collection
        SaveCollection();
    }

    public bool RemoveCardFromCollection(Card card, int count = 1)
    {
        if (!_playerCollection.ContainsKey(card) || _playerCollection[card] < count)
            return false;

        _playerCollection[card] -= count;

        if (_playerCollection[card] <= 0)
        {
            _playerCollection.Remove(card);
        }

        // Save collection
        SaveCollection();
        return true;
    }

    public int GetCardCount(Card card)
    {
        if (_playerCollection.ContainsKey(card))
            return _playerCollection[card];

        return 0;
    }

    public List<Card> GetAllCollectionCards()
    {
        List<Card> cards = new List<Card>();

        foreach (KeyValuePair<Card, int> pair in _playerCollection)
        {
            cards.Add(pair.Key);
        }

        return cards;
    }

    private void SaveCollection()
    {
        // Convert collection to serializable format
        CollectionData data = new CollectionData();

        foreach (KeyValuePair<Card, int> pair in _playerCollection)
        {
            data.cardIDs.Add(pair.Key.name);
            data.cardCounts.Add(pair.Value);
        }

        // Save to player prefs
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString("CardCollection", json);
        PlayerPrefs.Save();
    }

    private void LoadCollection()
    {
        string json = PlayerPrefs.GetString("CardCollection", "");

        if (string.IsNullOrEmpty(json))
        {
            // New player - give starter cards
            GiveStarterCards();
            return;
        }

        CollectionData data = JsonUtility.FromJson<CollectionData>(json);

        // Clear current collection
        _playerCollection.Clear();

        // Load cards
        for (int i = 0; i < data.cardIDs.Count; i++)
        {
            string cardID = data.cardIDs[i];
            int count = data.cardCounts[i];

            // Find card by name
            Card card = allCards.Find(c => c.name == cardID);

            if (card != null)
            {
                _playerCollection[card] = count;
            }
        }
    }

    private void GiveStarterCards()
    {
        // Give some basic cards to new players
        foreach (Card card in allCards)
        {
            if (card.manaCost <= 2) // Give all low-cost cards
            {
                _playerCollection[card] = 2; // Two copies of each
            }
            else if (card.manaCost <= 4) // Give some mid-cost cards
            {
                _playerCollection[card] = 1; // One copy of each
            }
        }

        // Save the starter collection
        SaveCollection();
    }
}

[System.Serializable]
public class CollectionData
{
    public List<string> cardIDs = new List<string>();
    public List<int> cardCounts = new List<int>();
}