using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AIPlayer : Player
{
    [Header("AI Settings")]
    public int difficultyLevel = 1;
    public float decisionDelay = 1f;
    public float playCardDelay = 0.5f;

    private CardGameManager _gameManager;

    public override void Start()
    {
        base.Start();
        _gameManager = CardGameManager.Instance;
    }

    public void StartAITurn()
    {
        StartCoroutine(AITurnRoutine());
    }

    private IEnumerator AITurnRoutine()
    {
        // Wait a moment before starting AI turn
        yield return new WaitForSeconds(decisionDelay);

        // Play cards
        yield return StartCoroutine(PlayCardsRoutine());

        // Attack with creatures
        yield return StartCoroutine(AttackWithCreaturesRoutine());

        // End turn
        yield return new WaitForSeconds(decisionDelay);
        _gameManager.EndTurn();
    }

    private IEnumerator PlayCardsRoutine()
    {
        bool playedCard;

        do
        {
            playedCard = false;

            // Get playable cards sorted by priority
            List<Card> playableCards = GetPlayableCards();

            if (playableCards.Count > 0)
            {
                // Play highest priority card
                Card cardToPlay = playableCards[0];

                // Get targets if needed
                List<Card> targets = GetTargetsForCard(cardToPlay);

                // Play the card
                bool success = PlayCard(cardToPlay, targets);

                if (success)
                {
                    playedCard = true;
                    yield return new WaitForSeconds(playCardDelay);
                }
            }
        }
        while (playedCard);
    }

    private IEnumerator AttackWithCreaturesRoutine()
    {
        List<Card> attackingCreatures = GetAttackingCreatures();

        foreach (Card attacker in attackingCreatures)
        {
            // Get best target
            Card target = GetBestAttackTarget(attacker);

            if (target != null)
            {
                // Attack target
                _gameManager.AttackCard(attacker, target);
                yield return new WaitForSeconds(playCardDelay);
            }
        }
    }

    private List<Card> GetPlayableCards()
    {
        List<Card> playableCards = new List<Card>();
        List<Card> handCards = GetHandCards();

        foreach (Card card in handCards)
        {
            if (card.manaCost <= currentMana)
            {
                // For creatures, check if we have space on the field
                if (card.type == Card.CardType.Creature && GetFieldCards().Count < maxFieldSize)
                {
                    playableCards.Add(card);
                }
                // For spells, always add
                else if (card.type != Card.CardType.Creature)
                {
                    playableCards.Add(card);
                }
            }
        }

        // Sort by AI priority
        playableCards.Sort((a, b) => GetCardPriority(b).CompareTo(GetCardPriority(a)));

        return playableCards;
    }

    private float GetCardPriority(Card card)
    {
        float priority = 0;

        // Base priority on mana efficiency
        priority += card.manaCost;

        // Creatures with higher stats get priority
        if (card.type == Card.CardType.Creature)
        {
            priority += card.attack + card.health;

            // Bonus for taunt
            if (card.hasTaunt)
                priority += 2;

            // Bonus for charge
            if (card.hasCharge)
                priority += 2;
        }

        // Higher difficulty AIs make better decisions
        if (difficultyLevel >= 2)
        {
            // Consider board state
            Player enemy = _gameManager.GetEnemyPlayer(this);
            List<Card> enemyField = enemy.GetFieldCards();

            // If enemy has threats, prioritize removal
            if (card.type != Card.CardType.Creature)
            {
                foreach (CardEffect effect in card.cardEffects)
                {
                    if (effect.effectType == CardEffect.EffectType.Damage)
                    {
                        priority += 3;
                    }
                }
            }
        }

        return priority;
    }

    private List<Card> GetTargetsForCard(Card card)
    {
        List<Card> targets = new List<Card>();

        // Check if card needs targets
        bool needsTarget = false;

        foreach (CardEffect effect in card.cardEffects)
        {
            if (effect.targetType == CardEffect.TargetType.SingleTarget)
            {
                needsTarget = true;
                break;
            }
        }

        if (needsTarget)
        {
            // Get enemy creatures
            Player enemy = _gameManager.GetEnemyPlayer(this);
            List<Card> enemyField = enemy.GetFieldCards();

            if (enemyField.Count > 0)
            {
                // Sort targets by priority
                enemyField.Sort((a, b) => GetTargetPriority(b).CompareTo(GetTargetPriority(a)));

                // Add highest priority target
                targets.Add(enemyField[0]);
            }
        }

        return targets;
    }

    public void PlayCardWithAnimation(Card card, List<Card> targets = null)
    {
        // Play the card
        bool success = PlayCard(card, targets);

        if (success && card.visualInstance != null)
        {
            // Add some visual effect to show AI is playing a card
            card.visualInstance.GetComponent<CardVisual>().PlayAICardAnimation();
        }
    }

    private float GetTargetPriority(Card card)
    {
        float priority = 0;

        // Prioritize high attack creatures
        priority += card.attack * 2;

        // Prioritize low health creatures that can be killed
        if (card.health <= 2)
            priority += 3;

        // Prioritize taunt creatures
        if (card.hasTaunt)
            priority += 5;

        return priority;
    }

    private List<Card> GetAttackingCreatures()
    {
        List<Card> attackers = new List<Card>();
        List<Card> fieldCards = GetFieldCards();

        foreach (Card card in fieldCards)
        {
            if (card is CreatureCard creatureCard && creatureCard.canAttackThisTurn)
            {
                attackers.Add(card);
            }
        }

        return attackers;
    }

    private Card GetBestAttackTarget(Card attacker)
    {
        Player enemy = _gameManager.GetEnemyPlayer(this);
        List<Card> enemyField = enemy.GetFieldCards();

        // Check if there are taunt creatures
        List<Card> tauntCreatures = enemyField.FindAll(c => c.hasTaunt);

        if (tauntCreatures.Count > 0)
        {
            // Must attack taunt creatures
            tauntCreatures.Sort((a, b) => a.health.CompareTo(b.health));
            return tauntCreatures[0]; // Attack weakest taunt
        }

        // No taunts, find best target
        if (enemyField.Count > 0)
        {
            // Sort by attack priority
            enemyField.Sort((a, b) => GetAttackTargetPriority(attacker, b).CompareTo(GetAttackTargetPriority(attacker, a)));

            // Check if we can make favorable trades
            if (GetAttackTargetPriority(attacker, enemyField[0]) > 0)
            {
                return enemyField[0];
            }
        }

        // If no good creature targets or empty board, attack player directly
        return null; // Null means attack player
    }

    private float GetAttackTargetPriority(Card attacker, Card defender)
    {
        float priority = 0;

        // Check if we can kill it
        if (attacker.attack >= defender.health)
        {
            priority += 5 + defender.attack; // Higher priority for killing high attack minions
        }

        // Check if we survive
        if (defender.attack < attacker.health)
        {
            priority += 3;
        }

        // Prioritize killing high attack minions
        priority += defender.attack;

        return priority;
    }
}