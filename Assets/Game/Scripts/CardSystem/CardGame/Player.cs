using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using DG.Tweening;

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

    public int GetDeckCount()
    {
        return _deck.Count;
    }

    public int GetHandCount()
    {
        return _hand.Count;
    }

    public Card GetTopDeckCard()
    {
        if (_deck.Count > 0)
            return _deck[0];
        return null;
    }

    public void DiscardFromDeck(int count = 1)
    {
        for (int i = 0; i < count && _deck.Count > 0; i++)
        {
            Card discardedCard = _deck[0];
            _deck.RemoveAt(0);
            _graveyard.Add(discardedCard);
        }
    }

    public void DrawCardWithAnimation()
    {
        if (_deck.Count == 0)
        {
            // Fatigue damage handled by CardGameManager
            return;
        }

        if (_hand.Count >= maxHandSize)
        {
            // Burning card handled by CardGameManager
            return;
        }

        // Get the card from the deck
        Card drawnCard = _deck[0];
        _hand.Add(drawnCard);
        _deck.RemoveAt(0);

        // Create card visual in hand with animation
        GameObject cardObj = Instantiate(cardPrefab, handArea);
        CardVisual cardVisual = cardObj.GetComponent<CardVisual>();

        if (cardVisual != null)
        {
            cardVisual.SetupCard(drawnCard, this);

            // Start the card off-screen
            RectTransform rectTransform = cardObj.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(0, -300); // Below the hand

            // Animate it into position
            rectTransform.DOAnchorPos(Vector2.zero, 0.5f).SetEase(Ease.OutBack);

            // Scale animation
            cardObj.transform.localScale = Vector3.zero;
            cardObj.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        }
    }

    public bool PlayCardToSlot(Card card, int slotIndex)
    {
        if (!_hand.Contains(card))
            return false;

        if (currentMana < card.manaCost)
            return false;

        if (card.type != Card.CardType.Creature)
            return false;

        if (slotIndex < 0 || slotIndex >= maxFieldSize)
            return false;

        // Check if slot is already occupied
        while (_field.Count <= slotIndex)
        {
            _field.Add(null);
        }

        if (_field[slotIndex] != null)
            return false;

        // Pay mana cost - use CardGameManager to handle this
        if (CardGameManager.Instance != null)
        {
            CardGameManager.Instance.SpendMana(this, card.manaCost);
        }
        else
        {
            // Fallback if CardGameManager is not available
            currentMana -= card.manaCost;
        }

        // Remove from hand
        _hand.Remove(card);

        // Add to field at specific slot
        _field[slotIndex] = card;

        // Create or move card visual
        if (card.visualInstance != null)
        {
            // Move existing visual to field
            card.visualInstance.transform.SetParent(fieldArea);
            card.visualInstance.SetFieldCard();

            // Position at the correct slot
            if (fieldArea.childCount > slotIndex)
            {
                Transform slotTransform = fieldArea.GetChild(slotIndex);
                if (slotTransform != null)
                {
                    card.visualInstance.transform.position = slotTransform.position;
                }
            }

            // Play animation
            card.visualInstance.PlayCardAnimation();
        }
        else
        {
            // Create new visual
            GameObject cardObj = Instantiate(cardPrefab, fieldArea);
            CardVisual cardVisual = cardObj.GetComponent<CardVisual>();

            if (cardVisual != null)
            {
                cardVisual.SetupCard(card, this);
                cardVisual.SetFieldCard();

                // Position at the correct slot
                if (fieldArea.childCount > slotIndex)
                {
                    Transform slotTransform = fieldArea.GetChild(slotIndex);
                    if (slotTransform != null)
                    {
                        cardObj.transform.position = slotTransform.position;
                    }
                }
            }
        }

        // Notify the game manager that a card was played
        CardGameManager.Instance.NotifyCardPlayed(this, card);

        // Trigger card's play effect
        card.OnPlay(CardGameManager.Instance, this, null);

        return true;
    }

    public void AddCardToField(Card card)
    {
        if (_field.Count >= maxFieldSize)
            return;

        _field.Add(card);

        // Create visual
        CreateCardVisual(card, fieldArea);

        // Trigger card's play effect
        card.OnPlay(CardGameManager.Instance, this, null);
    }

    public void ReplaceCard(Card oldCard, Card newCard)
    {
        // Find the index of the old card
        int index = _field.IndexOf(oldCard);
        if (index >= 0)
        {
            // Remove old card
            _field.RemoveAt(index);
            _graveyard.Add(oldCard);

            // Destroy visual
            if (oldCard.visualInstance != null)
                Destroy(oldCard.visualInstance.gameObject);

            // Add new card
            _field.Insert(index, newCard);

            // Create visual
            CreateCardVisual(newCard, fieldArea);
        }
    }

    public void InitializeDeck()
    {
        _deck.Clear();
        foreach (Card card in deckCards)
        {
            Card newCard = Instantiate(card); // Create instance of scriptable object

            // Initialize creature card stats
            if (newCard is CreatureCard creatureCard)
            {
                creatureCard.InitializeStats();
            }

            _deck.Add(newCard);
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
            GameObject cardObj = Instantiate(cardPrefab, handArea);
            CardVisual cardVisual = cardObj.GetComponent<CardVisual>();

            if (cardVisual != null)
            {
                cardVisual.SetupCard(drawnCard, this);

                // Force correct scale for opponent cards
                if (this != CardGameManager.Instance.playerOne)
                {
                    cardVisual.transform.localScale = Vector3.one;
                }
            }
        }
    }

    public bool HasTauntCreatures()
    {
        foreach (Card card in _field)
        {
            if (card != null && card.hasTaunt)
                return true;
        }
        return false;
    }

    public void RemoveCardFromField(Card card)
    {
        int index = _field.IndexOf(card);

        if (index >= 0)
        {
            _field[index] = null;
            _graveyard.Add(card);

            // If the card has a visual instance, destroy it
            if (card.visualInstance != null)
            {
                Destroy(card.visualInstance.gameObject);
            }

            // Clear the slot
            if (fieldArea.childCount > index)
            {
                Transform slotTransform = fieldArea.GetChild(index);
                CardSlotDropZone slotZone = slotTransform.GetComponent<CardSlotDropZone>();
                if (slotZone != null)
                {
                    slotZone.ClearSlot();
                }
            }
        }
    }

    public void AddCardToHand(Card card)
    {
        if (_hand.Count < maxHandSize)
        {
            _hand.Add(card);

            // Create visual in hand
            CreateCardVisual(card, handArea);
        }
        else
        {
            // Hand is full, discard the card
            _graveyard.Add(card);
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

        // Pay mana cost - use CardGameManager to handle this
        if (CardGameManager.Instance != null)
        {
            CardGameManager.Instance.SpendMana(this, card.manaCost);
        }
        else
        {
            // Fallback if CardGameManager is not available
            currentMana -= card.manaCost;
        }

        // Remove from hand
        _hand.Remove(card);

        // If creature, add to field
        if (card.type == Card.CardType.Creature)
        {
            _field.Add(card);

            // If the card already has a visual instance, just move it to the field
            if (card.visualInstance != null)
            {
                // Set parent to field area
                card.visualInstance.transform.SetParent(fieldArea);

                // Let the layout group handle positioning
                card.visualInstance.SetFieldCard();

                // Force layout rebuild
                LayoutRebuilder.ForceRebuildLayoutImmediate(fieldArea.GetComponent<RectTransform>());
            }
            else
            {
                // Create new visual if needed
                CreateCardVisual(card, fieldArea);
            }
        }
        else
        {
            // For non-creature cards, add to graveyard after playing
            _graveyard.Add(card);

            // If it has a visual, we'll let the animation handle destruction
        }

        // Notify the game manager that a card was played
        CardGameManager.Instance.NotifyCardPlayed(this, card);

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
            if (card != null && card is CreatureCard creatureCard)
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
            // Ensure creature card stats are initialized
            if (card is CreatureCard creatureCard && creatureCard.currentHealth <= 0)
            {
                creatureCard.InitializeStats();
            }

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