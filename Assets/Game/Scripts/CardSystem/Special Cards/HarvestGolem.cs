using UnityEngine;

[CreateAssetMenu(fileName = "HarvestGolem", menuName = "Cards/Special/HarvestGolem")]
public class HarvestGolem : CreatureCard
{
    [SerializeField] private Card damagedGolemCard; // Assign your Damaged Golem card in the inspector

    public override void OnDeath()
    {
        base.OnDeath();

        // Summon a Damaged Golem
        if (damagedGolemCard != null)
        {
            Player owner = CardGameManager.Instance.GetCardOwner(this);
            if (owner != null)
            {
                Card summonedCard = Instantiate(damagedGolemCard);
                owner.AddCardToField(summonedCard);
            }
        }
    }
}