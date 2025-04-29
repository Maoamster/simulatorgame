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
    public bool hasAttackedThisTurn = false;

    // Add this flag to prevent multiple initializations
    private bool _hasBeenInitialized = false;

    private void OnEnable()
    {
        // Only initialize once
        if (!_hasBeenInitialized)
        {
            InitializeStats();
            _hasBeenInitialized = true;
        }
    }

    // Add this method to explicitly initialize stats
    public void InitializeStats()
    {
        currentHealth = health;
        currentAttack = attack;
        _hasBeenInitialized = true;

        Debug.Log($"Initialized {cardName}: Attack={currentAttack}, Health={currentHealth}");
    }

    public void ResetToBaseStats()
    {
        currentAttack = attack;
        currentHealth = health;

        Debug.Log($"Reset {cardName} to base stats: Attack={currentAttack}, Health={currentHealth}");
    }

    public void ClearEffects()
    {
        isFrozen = false;
        isStealth = false;
    }

    public void TakeDamage(int amount)
    {
        // Ensure currentHealth is initialized
        if (currentHealth <= 0 && health > 0)
        {
            InitializeStats();
        }

        // Modify currentHealth, not health (which is the base stat)
        currentHealth -= amount;
        Debug.Log($"{cardName} takes {amount} damage, health now {currentHealth}/{health}");

        // Update visual if available
        if (visualInstance != null)
        {
            visualInstance.UpdateCardVisual();
        }
    }

    public void Heal(int amount)
    {
        // Ensure currentHealth is initialized
        if (currentHealth <= 0 && health > 0)
        {
            InitializeStats();
        }

        currentHealth = Mathf.Min(currentHealth + amount, health);
        Debug.Log($"{cardName} heals {amount}, health now {currentHealth}/{health}");

        // Update visual if available
        if (visualInstance != null)
        {
            visualInstance.UpdateCardVisual();
        }
    }

    public void BuffAttack(int amount)
    {
        currentAttack += amount;
        Debug.Log($"{cardName} attack buffed by {amount}, attack now {currentAttack}/{attack}");

        // Update visual if available
        if (visualInstance != null)
        {
            visualInstance.UpdateCardVisual();
        }
    }

    public void BuffHealth(int amount)
    {
        health += amount;
        currentHealth += amount;
        Debug.Log($"{cardName} health buffed by {amount}, health now {currentHealth}/{health}");

        // Update visual if available
        if (visualInstance != null)
        {
            visualInstance.UpdateCardVisual();
        }
    }

    public override void OnPlay(CardGameManager gameManager, Player owner, System.Collections.Generic.List<Card> targets = null)
    {
        base.OnPlay(gameManager, owner, targets);

        // Ensure stats are initialized
        if (!_hasBeenInitialized)
        {
            InitializeStats();
        }

        // Creatures with charge can attack immediately
        canAttackThisTurn = hasCharge;

        // Set taunt status
        isTaunting = hasTaunt;

        Debug.Log($"Played {cardName}: Attack={currentAttack}, Health={currentHealth}, Charge={hasCharge}, Taunt={hasTaunt}");
    }
}