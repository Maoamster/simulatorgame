using UnityEngine;

[CreateAssetMenu(fileName = "New Creature Card", menuName = "Cards/Creature Card")]
public class CreatureCard : Card
{
    [Header("Creature State")]
    public bool canAttackThisTurn = false;
    public bool isTaunting = false;
    public bool isDivine = false;
    public int currentHealth;
    public int currentAttack;

    public bool isFrozen = false;
    public bool isStealth = false;

    private void OnEnable()
    {
        // Initialize current stats from base stats
        currentHealth = health;
        currentAttack = attack;
    }

    public void ResetToBaseStats()
    {
        currentAttack = attack;
        currentHealth = health;
    }

    public void ClearEffects()
    {
        // Clear any ongoing effects
        // This is a placeholder - you'll need to implement based on your effect system
        isFrozen = false;
        isStealth = false;

        // If you have any event subscriptions, unsubscribe here
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        Debug.Log($"{cardName} takes {amount} damage, health now {health}");

        // Update visual if available
        if (visualInstance != null)
        {
            visualInstance.UpdateCardVisual();
        }
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, health);
    }

    public void BuffAttack(int amount)
    {
        currentAttack += amount;
    }

    public void BuffHealth(int amount)
    {
        health += amount;
        currentHealth += amount;
    }

    public override void OnPlay(CardGameManager gameManager, Player owner, System.Collections.Generic.List<Card> targets = null)
    {
        base.OnPlay(gameManager, owner, targets);

        // Creatures with charge can attack immediately
        canAttackThisTurn = hasCharge;

        // Set taunt status
        isTaunting = hasTaunt;
    }
}