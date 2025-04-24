using UnityEngine;

[CreateAssetMenu(fileName = "Berserker", menuName = "Cards/Special/Berserker")]
public class Berserker : CreatureCard
{
    public override void OnDamaged(int amount, Card source)
    {
        base.OnDamaged(amount, source);

        // Gain attack when damaged
        BuffAttack(2);

        // Update visual
        if (visualInstance != null)
            visualInstance.UpdateCardVisual();
    }
}