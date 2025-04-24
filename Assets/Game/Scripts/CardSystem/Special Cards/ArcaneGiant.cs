using UnityEngine;

[CreateAssetMenu(fileName = "ArcaneGiant", menuName = "Cards/Special/ArcaneGiant")]
public class ArcaneGiant : CreatureCard
{
    // Static counter to track spells cast throughout the game
    private static int _spellsCastThisGame = 0;

    // Reset counter when game starts
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetCounter()
    {
        _spellsCastThisGame = 0;
    }

    private void OnEnable()
    {
        // Subscribe to spell cast event when card is created
        if (CardGameManager.Instance != null)
        {
            CardGameManager.Instance.OnSpellCast += OnSpellCast;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe when card is destroyed
        if (CardGameManager.Instance != null)
        {
            CardGameManager.Instance.OnSpellCast -= OnSpellCast;
        }
    }

    private void OnSpellCast(Player caster, Card spell)
    {
        _spellsCastThisGame++;

        // Update card cost if it's in hand
        if (visualInstance != null)
        {
            visualInstance.UpdateCardVisual();
        }
    }

    // Instead of overriding GetManaCost, we'll use a property to calculate the effective cost
    public int EffectiveManaCost
    {
        get { return Mathf.Max(0, manaCost - _spellsCastThisGame); }
    }

    // Override OnPlay to use the effective cost
    public override void OnPlay(CardGameManager gameManager, Player owner, System.Collections.Generic.List<Card> targets = null)
    {
        // Store the original mana cost
        int originalCost = manaCost;

        // Temporarily set the mana cost to the effective cost
        manaCost = EffectiveManaCost;

        // Call the base implementation
        base.OnPlay(gameManager, owner, targets);

        // Restore the original mana cost
        manaCost = originalCost;
    }
}