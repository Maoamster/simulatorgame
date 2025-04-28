using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Card", menuName = "Cards/Card")]
public class Card : ScriptableObject
{
    [Header("Basic Info")]
    public string cardName;
    public string cardDescription;
    public Sprite cardArtwork;
    public int manaCost;

    [Header("Card Type")]
    public CardType type;
    public enum CardType { Creature, Spell, Item, Enchantment }

    [Header("Creature Stats")]
    public int attack;
    public int health;
    public bool hasCharge;
    public bool hasTaunt;

    [Header("Effects")]
    public List<CardEffect> cardEffects = new List<CardEffect>();

    [Header("Visual")]
    public Color cardColor = Color.white;
    public GameObject specialEffectPrefab;
    public Rarity rarity = Rarity.Common;

    [Header("Audio")]
    public AudioClip playSound;
    public AudioClip attackSound;
    public AudioClip deathSound;

    [System.NonSerialized]
    public CardVisual visualInstance;

    public virtual void OnPlay(CardGameManager gameManager, Player owner, List<Card> targets = null)
    {
        Debug.Log($"Playing card: {cardName}");

        // Apply card effects
        foreach (CardEffect effect in cardEffects)
        {
            effect.ApplyEffect(gameManager, owner, targets);
        }
    }

    public enum Rarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    public virtual void OnAttack(Card target)
    {
        Debug.Log($"{cardName} attacks {target.cardName}");
    }

    public virtual void OnDamaged(int amount, Card source)
    {
        Debug.Log($"{cardName} takes {amount} damage from {source.cardName}");
    }

    public virtual void OnDeath()
    {
        Debug.Log($"{cardName} has died");
    }
}