using UnityEngine;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    [Header("Player Info")]
    public string playerName;
    public int health = 30;
    public int maxMana = 10;
    public int currentMana = 1;

    [Header("Deck Settings")]
    public List<Card> deckCards = new List<Card>();
    public int maxHandSize = 10;
    public int maxFieldSize = 7;

    [Header("References")]
    public Transform handArea;
    public Transform fieldArea;
    public GameObject cardPrefab;

    // Runtime variables
    private List<Card> _deck = new List<Card>();
    private List<Card> _hand = new List<Card>();
    private List<Card> _field = new List<Card>();
    private List<Card> _graveyard = new List<Card>();

    public virtual void Start()
    {
        InitializeDeck();
        ShuffleDeck();
    }

    public void InitializeDeck()
    {
        _deck.Clear();
        foreach (Card card in deckCards)
        {
            _deck.Add(Instantiate(card)); // Create instance of scriptable object
        }
    }

    public void ClearAllCards()
    {
        // Clear all card collections
        _deck.Clear();
        _hand.Clear();
        _field.Clear();
        _graveyard.Clear();

        // Clear UI
        foreach (Transform child in handArea)
        {
            Destroy(child.gameObject);
        }

        foreach (Transform child in fieldArea)
        {
            Destroy(child.gameObject);
        }
    }

    public void ShuffleDeck()
    {
        // Fisher-Yates shuffle
        for (int i = _deck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Card temp = _deck[i];
            _deck[i] = _deck[j];
            _deck[j] = temp;
        }
    }

    public void DrawCard(int amount = 1)
    {
        for (int i = 0; i < amount; i++)
        {
            if (_deck.Count == 0)
            {
                TakeDamage(1); // Fatigue damage
                continue;
            }

            if (_hand.Count >= maxHandSize)
            {
                // Burn card if hand is full
                _graveyard.Add(_deck[0]);
                _deck.RemoveAt(0);
                continue;
            }

            Card drawnCard = _deck[0];
            _hand.Add(drawnCard);
            _deck.RemoveAt(0);

            // Create card visual in hand
            CreateCardVisual(drawnCard, handArea);
        }
    }

    public bool PlayCard(Card card, List<Card> targets = null)
    {
        if (!_hand.Contains(card))
            return false;

        if (currentMana < card.manaCost)
            return false;

        if (card.type == Card.CardType.Creature && _field.Count >= maxFieldSize)
            return false;

        // Pay mana cost
        currentMana -= card.manaCost;

        // Remove from hand
        _hand.Remove(card);

        // If creature, add to field
        if (card.type == Card.CardType.Creature)
        {
            _field.Add(card);

            // Create card visual on field
            CreateCardVisual(card, fieldArea);
        }
        else
        {
            // For non-creature cards, add to graveyard after playing
            _graveyard.Add(card);
        }

        // Trigger card's play effect
        card.OnPlay(CardGameManager.Instance, this, targets);

        return true;
    }

    public void TakeDamage(int amount)
    {
        health -= amount;

        if (health <= 0)
        {
            health = 0;
            Die();
        }
    }

    public void Heal(int amount)
    {
        health = Mathf.Min(health + amount, 30);
    }

    public void AddMana(int amount)
    {
        currentMana = Mathf.Min(currentMana + amount, maxMana);
    }

    public void StartTurn()
    {
        // Increase max mana (up to 10)
        maxMana = Mathf.Min(maxMana + 1, 10);

        // Refill mana
        currentMana = maxMana;

        // Draw a card
        DrawCard();

        // Reset creature attacks
        foreach (Card card in _field)
        {
            if (card is CreatureCard creatureCard)
            {
                creatureCard.canAttackThisTurn = true;
            }
        }
    }

    public void EndTurn()
    {
        // End of turn effects would go here
    }

    private void Die()
    {
        Debug.Log($"Player {playerName} has been defeated!");
        CardGameManager.Instance.EndGame(this);
    }

    private void CreateCardVisual(Card card, Transform parent)
    {
        GameObject cardObj = Instantiate(cardPrefab, parent);
        CardVisual cardVisual = cardObj.GetComponent<CardVisual>();

        if (cardVisual != null)
        {
            cardVisual.SetupCard(card, this);
        }
    }

    public List<Card> GetHandCards()
    {
        return new List<Card>(_hand);
    }

    public List<Card> GetFieldCards()
    {
        return new List<Card>(_field);
    }

    public List<Card> GetGraveyardCards()
    {
        return new List<Card>(_graveyard);
    }
}