using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using static CardGameManager;

public class AIPlayer : Player
{
    [Header("AI Settings")]
    public int difficultyLevel = 1;
    public float decisionDelay = 1f;
    public float playCardDelay = 0.5f;

    private CardGameManager _gameManager;
    private class AttackPair
    {
        public Card attacker;
        public Card defender;
    }

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
        int attemptCount = 0; // Safety counter to prevent infinite loops

        do
        {
            playedCard = false;
            attemptCount++;

            // Get playable cards sorted by priority
            List<Card> playableCards = GetPlayableCards();
            Debug.Log($"AI has {playableCards.Count} playable cards");

            if (playableCards.Count > 0 && attemptCount < 10) // Safety limit
            {
                // Play highest priority card
                Card cardToPlay = playableCards[0];
                Debug.Log($"AI attempting to play {cardToPlay.cardName}");

                // Get targets if needed
                List<Card> targets = GetTargetsForCard(cardToPlay);

                // Play the card with animation
                bool success = PlayCardWithAnimation(cardToPlay, targets);
                Debug.Log($"AI play success: {success}");

                if (success)
                {
                    playedCard = true;
                    yield return new WaitForSeconds(playCardDelay);
                }
            }
            else
            {
                Debug.Log("AI has no playable cards or reached attempt limit");
                break;
            }
        }
        while (playedCard);
    }

    private IEnumerator ExecuteAIAttackWithAnimation(Card attacker, Card defender)
    {
        if (attacker is CreatureCard attackerCreature && defender is CreatureCard defenderCreature)
        {
            // Get visual instances
            CardVisual attackerVisual = attacker.visualInstance;
            CardVisual defenderVisual = defender.visualInstance;

            if (attackerVisual != null && defenderVisual != null)
            {
                // Store original positions and scales
                Vector3 attackerOrigPos = attackerVisual.transform.position;
                Vector3 attackerOrigScale = attackerVisual.transform.localScale;
                Vector3 defenderOrigScale = defenderVisual.transform.localScale;

                // Ensure consistent scale during animation
                attackerVisual.transform.localScale = Vector3.one;
                defenderVisual.transform.localScale = Vector3.one;

                // Animate attacker moving toward defender
                yield return attackerVisual.transform.DOMove(
                    Vector3.Lerp(attackerOrigPos, defenderVisual.transform.position, 0.7f),
                    0.3f).SetEase(Ease.OutQuad).WaitForCompletion();

                // Shake defender to show impact
                defenderVisual.transform.DOShakePosition(0.2f, 10f, 10, 90, false, true);

                // Apply damage
                defenderCreature.TakeDamage(attackerCreature.currentAttack);
                attackerCreature.TakeDamage(defenderCreature.currentAttack);

                // Update visuals
                attackerVisual.UpdateCardVisual();
                defenderVisual.UpdateCardVisual();

                // Return attacker to original position
                yield return attackerVisual.transform.DOMove(attackerOrigPos, 0.3f)
                    .SetEase(Ease.InQuad).WaitForCompletion();

                // Restore original scales
                attackerVisual.transform.localScale = attackerOrigScale;
                defenderVisual.transform.localScale = defenderOrigScale;

                // Mark attacker as having attacked
                attackerCreature.hasAttackedThisTurn = true;

                // Check if cards died
                if (defenderCreature.currentHealth <= 0)
                {
                    // Play death animation
                    if (defenderVisual != null)
                    {
                        Sequence deathSequence = DOTween.Sequence();

                        // Fade out
                        CanvasGroup canvasGroup = defenderVisual.GetComponent<CanvasGroup>();
                        if (canvasGroup == null)
                            canvasGroup = defenderVisual.gameObject.AddComponent<CanvasGroup>();

                        deathSequence.Append(canvasGroup.DOFade(0, 0.5f));

                        // Shrink
                        deathSequence.Join(defenderVisual.transform.DOScale(Vector3.zero, 0.5f));

                        // Rotate
                        deathSequence.Join(defenderVisual.transform.DORotate(new Vector3(0, 0, 90), 0.5f, RotateMode.FastBeyond360));

                        // Wait for animation to complete
                        yield return deathSequence.WaitForCompletion();
                    }

                    // Remove from field
                    Player defenderOwner = CardGameManager.Instance.GetCardOwner(defenderCreature);
                    if (defenderOwner != null)
                    {
                        defenderOwner.RemoveCardFromField(defenderCreature);
                    }
                }

                if (attackerCreature.currentHealth <= 0)
                {
                    // Play death animation
                    if (attackerVisual != null)
                    {
                        Sequence deathSequence = DOTween.Sequence();

                        // Fade out
                        CanvasGroup canvasGroup = attackerVisual.GetComponent<CanvasGroup>();
                        if (canvasGroup == null)
                            canvasGroup = attackerVisual.gameObject.AddComponent<CanvasGroup>();

                        deathSequence.Append(canvasGroup.DOFade(0, 0.5f));

                        // Shrink
                        deathSequence.Join(attackerVisual.transform.DOScale(Vector3.zero, 0.5f));

                        // Rotate
                        deathSequence.Join(attackerVisual.transform.DORotate(new Vector3(0, 0, 90), 0.5f, RotateMode.FastBeyond360));

                        // Wait for animation to complete
                        yield return deathSequence.WaitForCompletion();
                    }

                    // Remove from field
                    Player attackerOwner = CardGameManager.Instance.GetCardOwner(attackerCreature);
                    if (attackerOwner != null)
                    {
                        attackerOwner.RemoveCardFromField(attackerCreature);
                    }
                }
            }
        }
    }

    private IEnumerator AttackWithCreaturesRoutine()
    {
        List<Card> attackingCreatures = GetAttackingCreatures();
        Debug.Log($"AI has {attackingCreatures.Count} creatures that can attack");

        // Plan all attacks first
        List<AttackPair> plannedAttacks = new List<AttackPair>();
        List<Card> directAttackers = new List<Card>();

        foreach (Card attacker in attackingCreatures)
        {
            // Get best target
            Card target = GetBestAttackTarget(attacker);

            if (target != null)
            {
                Debug.Log($"AI plans to attack {target.cardName} with {attacker.cardName}");
                // Add to planned attacks
                plannedAttacks.Add(new AttackPair { attacker = attacker, defender = target });
            }
            else
            {
                // Attack player directly
                Debug.Log($"AI plans to attack player directly with {attacker.cardName}");
                directAttackers.Add(attacker);
            }
        }

        // Execute all attacks with animations
        foreach (AttackPair attack in plannedAttacks)
        {
            yield return StartCoroutine(ExecuteAIAttackWithAnimation(attack.attacker, attack.defender));

            // Wait between attacks
            yield return new WaitForSeconds(playCardDelay);
        }

        // Execute direct attacks
        foreach (Card attacker in directAttackers)
        {
            yield return StartCoroutine(ExecuteAIDirectAttackWithAnimation(attacker));

            // Wait between attacks
            yield return new WaitForSeconds(playCardDelay);
        }
    }

    private IEnumerator ExecuteAIDirectAttackWithAnimation(Card attacker)
    {
        if (attacker is CreatureCard attackerCreature)
        {
            Debug.Log($"AI executing direct attack with {attackerCreature.cardName}");

            // Get visual instance
            CardVisual attackerVisual = attacker.visualInstance;

            if (attackerVisual != null)
            {
                // Store original position and scale
                Vector3 originalPos = attackerVisual.transform.position;
                Vector3 originalScale = attackerVisual.transform.localScale;

                // Ensure consistent scale during animation
                attackerVisual.transform.localScale = Vector3.one;

                // Get target position (player health)
                Transform targetTransform = CardGameManager.Instance.playerOneHealthText.transform;

                // Animate attacker moving toward player
                yield return attackerVisual.transform.DOMove(
                    Vector3.Lerp(originalPos, targetTransform.position, 0.7f),
                    0.3f).SetEase(Ease.OutQuad).WaitForCompletion();

                // Shake health text
                targetTransform.DOShakePosition(0.2f, 10f, 10, 90, false, true);

                // Deal damage to player
                CardGameManager.Instance.playerOne.TakeDamage(attackerCreature.currentAttack);

                // Return attacker to original position
                yield return attackerVisual.transform.DOMove(originalPos, 0.3f)
                    .SetEase(Ease.InQuad).WaitForCompletion();

                // Mark attacker as having attacked
                attackerCreature.hasAttackedThisTurn = true;

                // Update UI
                CardGameManager.Instance.UpdateUI();

                attackerVisual.transform.localScale = originalScale;
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

    private bool PlayCardWithAnimation(Card card, List<Card> targets = null)
    {
        // Play the card
        bool success = PlayCard(card, targets);

        if (success && card.visualInstance != null)
        {
            // Add some visual effect to show AI is playing a card
            CardVisual cardVisual = card.visualInstance;

            // Force correct scale for AI cards
            cardVisual.transform.localScale = Vector3.one;

            // Scale animation
            cardVisual.transform.DOScale(Vector3.one * 1.2f, 0.2f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => {
                    cardVisual.transform.DOScale(Vector3.one, 0.2f)
                        .SetEase(Ease.InQuad);
                });

            // Play particles if available
            if (cardVisual.playParticles != null)
            {
                cardVisual.playParticles.Play();
            }

            // Play sound if available
            if (card.playSound != null)
            {
                AudioSource.PlayClipAtPoint(card.playSound, cardVisual.transform.position);
            }
        }

        return success;
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
        Player enemy = CardGameManager.Instance.GetEnemyPlayer(this);
        List<Card> enemyField = enemy.GetFieldCards();

        // Remove null entries from the list
        enemyField.RemoveAll(card => card == null);

        Debug.Log($"AI looking for target. Enemy has {enemyField.Count} cards on field");

        // Check if there are taunt creatures
        List<Card> tauntCreatures = enemyField.FindAll(c => c != null && c.hasTaunt);

        if (tauntCreatures.Count > 0)
        {
            // Must attack taunt creatures
            tauntCreatures.Sort((a, b) =>
            {
                CreatureCard aCreature = a as CreatureCard;
                CreatureCard bCreature = b as CreatureCard;
                if (aCreature != null && bCreature != null)
                    return aCreature.currentHealth.CompareTo(bCreature.currentHealth);
                return 0;
            });
            return tauntCreatures[0]; // Attack weakest taunt
        }

        // No taunts, find best target
        if (enemyField.Count > 0)
        {
            // Sort by attack priority
            enemyField.Sort((a, b) =>
            {
                if (a == null) return 1;
                if (b == null) return -1;

                CreatureCard aCreature = a as CreatureCard;
                CreatureCard bCreature = b as CreatureCard;

                if (aCreature != null && bCreature != null)
                    return GetAttackTargetPriority(attacker, b).CompareTo(GetAttackTargetPriority(attacker, a));
                return 0;
            });

            // Check if we can make favorable trades
            if (GetAttackTargetPriority(attacker, enemyField[0]) > 0)
            {
                return enemyField[0];
            }
        }

        // If no good creature targets or empty board, return null (will attack player directly)
        return null;
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