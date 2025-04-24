using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Mana Wyrm", menuName = "Cards/Special/Mana Wyrm")]
public class ManaWyrm : CreatureCard
{
    public override void OnPlay(CardGameManager gameManager, Player owner, List<Card> targets = null)
    {
        base.OnPlay(gameManager, owner, targets);

        // Subscribe to spell cast event
        gameManager.OnSpellCast += OnSpellCast;
    }

    private void OnSpellCast(Player caster, Card spell)
    {
        // Only buff if our owner cast the spell
        if (caster == GetOwner())
        {
            BuffAttack(1);

            // Update visual
            if (visualInstance != null)
                visualInstance.UpdateCardVisual();
        }
    }

    private Player GetOwner()
    {
        return CardGameManager.Instance.GetCardOwner(this);
    }

    // Clean up when card is destroyed
    public override void OnDeath()
    {
        base.OnDeath();

        // Unsubscribe from event
        CardGameManager.Instance.OnSpellCast -= OnSpellCast;
    }
}