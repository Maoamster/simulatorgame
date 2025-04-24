using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CardEffect
{
    public EffectType effectType;
    public TargetType targetType;
    public int effectValue;
    public string effectDescription;

    public enum EffectType
    {
        Damage,
        Heal,
        DrawCard,
        GainMana,
        BuffAttack,
        BuffHealth,
        Summon,
        DiscardCard,
        ReturnToHand,
        Transform,
        ApplyStatus
    }

    public enum TargetType
    {
        None,
        SingleTarget,
        AllEnemies,
        AllAllies,
        AllCreatures,
        Self,
        RandomEnemy,
        RandomAlly
    }

    public void ApplyEffect(CardGameManager gameManager, Player owner, List<Card> targets = null)
    {
        switch (effectType)
        {
            case EffectType.Damage:
                ApplyDamageEffect(gameManager, owner, targets);
                break;

            case EffectType.Heal:
                ApplyHealEffect(gameManager, owner, targets);
                break;

            case EffectType.DrawCard:
                for (int i = 0; i < effectValue; i++)
                {
                    owner.DrawCard();
                }
                break;

            case EffectType.GainMana:
                owner.AddMana(effectValue);
                break;

                // Implement other effect types...
        }
    }

    private void ApplyDamageEffect(CardGameManager gameManager, Player owner, List<Card> targets)
    {
        List<Card> effectTargets = GetTargets(gameManager, owner, targets);

        foreach (Card target in effectTargets)
        {
            // Apply damage logic
            if (target is CreatureCard creatureCard)
            {
                creatureCard.TakeDamage(effectValue);
            }
        }
    }

    private void ApplyHealEffect(CardGameManager gameManager, Player owner, List<Card> targets)
    {
        List<Card> effectTargets = GetTargets(gameManager, owner, targets);

        foreach (Card target in effectTargets)
        {
            // Apply heal logic
            if (target is CreatureCard creatureCard)
            {
                creatureCard.Heal(effectValue);
            }
        }
    }

    private List<Card> GetTargets(CardGameManager gameManager, Player owner, List<Card> selectedTargets)
    {
        List<Card> targets = new List<Card>();

        switch (targetType)
        {
            case TargetType.None:
                break;

            case TargetType.SingleTarget:
                if (selectedTargets != null && selectedTargets.Count > 0)
                {
                    targets.Add(selectedTargets[0]);
                }
                break;

            case TargetType.AllEnemies:
                targets.AddRange(gameManager.GetEnemyPlayer(owner).GetFieldCards());
                break;

            case TargetType.AllAllies:
                targets.AddRange(owner.GetFieldCards());
                break;

            case TargetType.AllCreatures:
                targets.AddRange(owner.GetFieldCards());
                targets.AddRange(gameManager.GetEnemyPlayer(owner).GetFieldCards());
                break;

            case TargetType.Self:
                // This would typically be the card itself
                break;

            case TargetType.RandomEnemy:
                List<Card> enemyCards = gameManager.GetEnemyPlayer(owner).GetFieldCards();
                if (enemyCards.Count > 0)
                {
                    targets.Add(enemyCards[Random.Range(0, enemyCards.Count)]);
                }
                break;

            case TargetType.RandomAlly:
                List<Card> allyCards = owner.GetFieldCards();
                if (allyCards.Count > 0)
                {
                    targets.Add(allyCards[Random.Range(0, allyCards.Count)]);
                }
                break;
        }

        return targets;
    }
}