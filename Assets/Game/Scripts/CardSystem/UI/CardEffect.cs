using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CardEffect
{
    public EffectType effectType;
    public TargetType targetType;
    public int effectValue;
    public string effectDescription;

    // Additional properties for complex effects
    public Card specificCardToSummon;
    public string creatureType;
    public int secondaryValue;
    public bool isTemporary;
    public int durationTurns;
    public EffectCondition condition;
    public EffectTiming timing;

    // Condition for effect to trigger
    public enum EffectCondition
    {
        None,
        IfHoldingDragon,
        IfControllingSecret,
        IfDamagedCharacter,
        IfHandEmpty,
        IfHandFull,
        IfBoardFull,
        IfEnemyHasMinions,
        IfYouHaveNoMinions
    }

    // When the effect triggers
    public enum EffectTiming
    {
        Immediate,
        EndOfTurn,
        StartOfTurn,
        WhenDamaged,
        WhenHealed,
        WhenMinionDies,
        WhenSpellCast,
        WhenCardDrawn,
        AfterAttack
    }

    public enum EffectType
    {
        // Basic Effects
        Damage,                  // Deal damage to target(s)
        Heal,                    // Restore health to target(s)
        DrawCard,                // Draw cards
        GainMana,                // Gain mana crystals
        BuffAttack,              // Increase attack
        BuffHealth,              // Increase health

        // Advanced Effects
        Summon,                  // Summon a specific creature
        DiscardCard,             // Discard cards from hand
        ReturnToHand,            // Return a card to owner's hand
        Transform,               // Transform a card into another

        // Status Effects
        ApplyTaunt,              // Give a creature Taunt
        ApplyCharge,             // Give a creature Charge
        ApplyDivineShield,       // Give a creature Divine Shield
        ApplyPoison,             // Give a creature Poison (kills any creature it damages)
        ApplyFreeze,             // Freeze a character (can't attack next turn)
        ApplyStealth,            // Give a creature Stealth (can't be targeted)
        ApplyImmunity,           // Make a character immune to damage

        // Restrictions
        CantAttack,              // Creature can't attack
        CantBeTargeted,          // Can't be targeted by spells or abilities
        CantAttackHeroes,        // Can't attack heroes

        // Triggered Effects
        GainAttackFromSpells,    // Gain attack when spells are cast
        GainHealthFromHandSize,  // Gain health based on hand size
        GainAttackWhenDamaged,   // Gain attack when damaged
        DealDamageWhenSummoned,  // Deal damage when summoned
        HealWhenSpellCast,       // Heal when a spell is cast
        DrawCardWhenMinionDies,  // Draw a card when a minion dies

        // AoE Effects
        DamageAll,               // Damage all characters
        DamageAllEnemies,        // Damage all enemy characters
        HealAllFriendly,         // Heal all friendly characters
        BuffAllAttack,           // Buff attack of all friendly minions
        BuffAllHealth,           // Buff health of all friendly minions

        // Resource Manipulation
        DestroyMana,             // Destroy mana crystals
        DiscardRandom,           // Discard a random card
        MillCards,               // Force opponent to draw cards

        // Complex Effects
        CopyCard,                // Copy a card
        StealCard,               // Steal a card from opponent
        SilenceMinion,           // Remove all effects from a minion
        SwapAttackAndHealth,     // Swap a minion's attack and health
        SetAttack,               // Set a minion's attack to a specific value
        SetHealth,               // Set a minion's health to a specific value
        DestroyMinion,           // Destroy a minion regardless of health

        // Conditional Effects
        DestroyLowAttack,        // Destroy a minion with low attack
        DestroyHighAttack,       // Destroy a minion with high attack
        DamageBasedOnAttack,     // Deal damage equal to a minion's attack
        HealBasedOnDamage,       // Heal based on damage dealt

        // Deck Manipulation
        ShuffleToDeck,           // Shuffle a card into a deck
        PutOnTop,                // Put a card on top of the deck
        DiscoverCard,            // Discover a card (choose 1 of 3)
        CreateCustomCard,        // Create a custom card

        // Meta Effects
        ReduceManaCost,          // Reduce the mana cost of cards
        DoubleNextSpell,         // Double the effect of the next spell
        TriggerDeathrattle,      // Trigger a minion's deathrattle
        CopyDeathrattle,         // Copy a minion's deathrattle

        // Special Effects
        RandomEffect,            // Apply a random effect
        AdaptMinion,             // Adapt a minion (choose 1 of 3 buffs)
        TransformAll,            // Transform all minions
        MindControl,             // Take control of an enemy minion
        SwapMinions,             // Swap a minion with an opponent's minion

        // Combo Effects
        ComboEffect,             // Effect only triggers if another card was played this turn
        BattlecryEffect,         // Effect triggers when played from hand
        DeathrattleEffect,       // Effect triggers when the minion dies

        // Delayed Effects
        EndOfTurnEffect,         // Effect triggers at the end of turn
        StartOfTurnEffect,       // Effect triggers at the start of turn
        DelayedEffect            // Effect triggers after a set number of turns
    }

    public enum TargetType
    {
        None,                    // No target needed
        SingleTarget,            // Any single valid target
        AllEnemies,              // All enemy characters
        AllAllies,               // All friendly characters
        AllCreatures,            // All creatures
        Self,                    // The card itself
        RandomEnemy,             // Random enemy character
        RandomAlly,              // Random friendly character
        YourHero,                // Your hero
        EnemyHero,               // Enemy hero
        AllEnemyCreatures,       // All enemy creatures
        AllFriendlyCreatures,    // All friendly creatures
        AdjacentCreatures,       // Creatures adjacent to target
        RandomCreature,          // Any random creature
        LowestHealthCreature,    // Creature with lowest health
        HighestAttackCreature,   // Creature with highest attack
        AllCardsInHand,          // All cards in hand
        RandomCardInHand,        // Random card in hand
        AllCardsInDeck,          // All cards in deck
        TopCardOfDeck,           // Top card of deck
        BottomCardOfDeck         // Bottom card of deck
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

            case EffectType.BuffAttack:
                ApplyBuffAttackEffect(gameManager, owner, targets);
                break;

            case EffectType.BuffHealth:
                ApplyBuffHealthEffect(gameManager, owner, targets);
                break;

            case EffectType.Summon:
                ApplySummonEffect(gameManager, owner);
                break;

            case EffectType.SilenceMinion:
                ApplySilenceEffect(gameManager, owner, targets);
                break;

            case EffectType.SwapAttackAndHealth:
                ApplySwapAttackHealthEffect(gameManager, owner, targets);
                break;

            case EffectType.ApplyFreeze:
                ApplyFreezeEffect(gameManager, owner, targets);
                break;

            case EffectType.DiscoverCard:
                ApplyDiscoverEffect(gameManager, owner);
                break;

            case EffectType.MindControl:
                ApplyMindControlEffect(gameManager, owner, targets);
                break;

            case EffectType.AdaptMinion:
                ApplyAdaptEffect(gameManager, owner, targets);
                break;

            case EffectType.DelayedEffect:
                ApplyDelayedEffect(gameManager, owner, targets);
                break;

            case EffectType.DamageAll:
                ApplyDamageAllEffect(gameManager, owner);
                break;

            case EffectType.BuffAllAttack:
                ApplyBuffAllAttackEffect(gameManager, owner);
                break;

                // Add cases for other effect types as needed
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

    private void ApplyDamageEffect(CardGameManager gameManager, Player owner, List<Card> targets)
    {
        List<Card> effectTargets = GetTargets(gameManager, owner, targets);

        foreach (Card target in effectTargets)
        {
            if (target is CreatureCard creatureCard)
            {
                creatureCard.TakeDamage(effectValue);
            }
            else if (targetType == TargetType.YourHero)
            {
                owner.TakeDamage(effectValue);
            }
            else if (targetType == TargetType.EnemyHero)
            {
                gameManager.GetEnemyPlayer(owner).TakeDamage(effectValue);
            }
        }
    }

    private void ApplyHealEffect(CardGameManager gameManager, Player owner, List<Card> targets)
    {
        List<Card> effectTargets = GetTargets(gameManager, owner, targets);

        foreach (Card target in effectTargets)
        {
            if (target is CreatureCard creatureCard)
            {
                creatureCard.Heal(effectValue);
            }
            else if (targetType == TargetType.YourHero || targetType == TargetType.AllAllies)
            {
                owner.Heal(effectValue);
            }
            else if (targetType == TargetType.EnemyHero)
            {
                gameManager.GetEnemyPlayer(owner).Heal(effectValue);
            }
        }
    }

    private void ApplyBuffAttackEffect(CardGameManager gameManager, Player owner, List<Card> targets)
    {
        List<Card> effectTargets = GetTargets(gameManager, owner, targets);

        foreach (Card target in effectTargets)
        {
            if (target is CreatureCard creatureCard)
            {
                creatureCard.BuffAttack(effectValue);

                // Update visual
                if (target.visualInstance != null)
                    target.visualInstance.UpdateCardVisual();
            }
        }
    }

    private void ApplyBuffHealthEffect(CardGameManager gameManager, Player owner, List<Card> targets)
    {
        List<Card> effectTargets = GetTargets(gameManager, owner, targets);

        foreach (Card target in effectTargets)
        {
            if (target is CreatureCard creatureCard)
            {
                creatureCard.BuffHealth(effectValue);

                // Update visual
                if (target.visualInstance != null)
                    target.visualInstance.UpdateCardVisual();
            }
        }
    }

    private void ApplySummonEffect(CardGameManager gameManager, Player owner)
    {
        if (specificCardToSummon != null)
        {
            // Create a copy of the card to summon
            Card summonedCard = UnityEngine.Object.Instantiate(specificCardToSummon);

            // Add to owner's field
            owner.AddCardToField(summonedCard);
        }
    }

    private void ApplySilenceEffect(CardGameManager gameManager, Player owner, List<Card> targets)
    {
        if (targets == null || targets.Count == 0)
            return;

        Card target = targets[0];
        if (target is CreatureCard creatureCard)
        {
            // Reset card to base stats
            creatureCard.ResetToBaseStats();

            // Remove all status effects
            creatureCard.hasTaunt = false;
            creatureCard.hasCharge = false;
            creatureCard.isDivine = false;

            // Clear all special effects
            creatureCard.ClearEffects();

            // Update visual
            if (target.visualInstance != null)
                target.visualInstance.UpdateCardVisual();
        }
    }

    private void ApplySwapAttackHealthEffect(CardGameManager gameManager, Player owner, List<Card> targets)
    {
        if (targets == null || targets.Count == 0)
            return;

        Card target = targets[0];
        if (target is CreatureCard creatureCard)
        {
            // Swap attack and health
            int oldAttack = creatureCard.currentAttack;
            creatureCard.currentAttack = creatureCard.currentHealth;
            creatureCard.currentHealth = oldAttack;

            // Update visual
            if (target.visualInstance != null)
                target.visualInstance.UpdateCardVisual();
        }
    }

    private void ApplyFreezeEffect(CardGameManager gameManager, Player owner, List<Card> targets)
    {
        if (targets == null || targets.Count == 0)
            return;

        Card target = targets[0];
        if (target is CreatureCard creatureCard)
        {
            // Apply freeze status
            creatureCard.isFrozen = true;

            // Register for start of turn to unfreeze
            gameManager.RegisterStartOfTurnEffect(target, (p) =>
            {
                if (p == gameManager.GetCardOwner(target))
                {
                    creatureCard.isFrozen = false;

                    // Update visual
                    if (target.visualInstance != null)
                        target.visualInstance.UpdateCardVisual();
                }
            });

            // Update visual
            if (target.visualInstance != null)
                target.visualInstance.UpdateCardVisual();
        }
    }

    private void ApplyDiscoverEffect(CardGameManager gameManager, Player owner)
    {
        // Generate 3 random cards to discover
        List<Card> discoverOptions = new List<Card>();

        // Get 3 random cards from the collection
        List<Card> allCards = CardCollectionManager.Instance.GetAllCards();
        for (int i = 0; i < 3; i++)
        {
            if (allCards.Count > 0)
            {
                int randomIndex = Random.Range(0, allCards.Count);
                discoverOptions.Add(allCards[randomIndex]);
                allCards.RemoveAt(randomIndex);
            }
        }

        // Show discover UI
        gameManager.ShowDiscoverUI(discoverOptions, (selectedCard) =>
        {
            // Add the selected card to hand
            Card cardCopy = UnityEngine.Object.Instantiate(selectedCard);
            owner.AddCardToHand(cardCopy);
        });
    }

    private void ApplyMindControlEffect(CardGameManager gameManager, Player owner, List<Card> targets)
    {
        if (targets == null || targets.Count == 0)
            return;

        Card target = targets[0];
        if (target is CreatureCard)
        {
            // Get the current owner
            Player currentOwner = gameManager.GetCardOwner(target);

            // Only mind control enemy minions
            if (currentOwner != owner)
            {
                // Remove from current owner
                currentOwner.RemoveCardFromField(target);

                // Add to new owner
                owner.AddCardToField(target);
            }
        }
    }

    private void ApplyAdaptEffect(CardGameManager gameManager, Player owner, List<Card> targets)
    {
        if (targets == null || targets.Count == 0)
            return;

        Card target = targets[0];
        if (target is CreatureCard)
        {
            // Create adapt options - use the standalone AdaptOption class
            List<AdaptOption> adaptOptions = new List<AdaptOption>
        {
            new AdaptOption("Divine Shield", () => ((CreatureCard)target).isDivine = true),
            new AdaptOption("+3 Attack", () => ((CreatureCard)target).BuffAttack(3)),
            new AdaptOption("+3 Health", () => ((CreatureCard)target).BuffHealth(3)),
            new AdaptOption("Taunt", () => ((CreatureCard)target).hasTaunt = true),
            new AdaptOption("Stealth", () => ((CreatureCard)target).isStealth = true)
        };

            // Show adapt UI
            gameManager.ShowAdaptUI(adaptOptions, (selectedOption) =>
            {
                // Apply the selected adaptation
                selectedOption.applyEffect();

                // Update visual
                if (target.visualInstance != null)
                    target.visualInstance.UpdateCardVisual();
            });
        }
    }

    private void ApplyDelayedEffect(CardGameManager gameManager, Player owner, List<Card> targets)
    {
        // Register a delayed effect
        gameManager.RegisterDelayedEffect(owner, durationTurns, () =>
        {
            // Apply the actual effect after delay
            // This would call another effect method based on what you want to happen
            Debug.Log($"Delayed effect triggered after {durationTurns} turns");
        });
    }

    private void ApplyDamageAllEffect(CardGameManager gameManager, Player owner)
    {
        // Get all creatures
        List<Card> allCreatures = new List<Card>();
        allCreatures.AddRange(owner.GetFieldCards());
        allCreatures.AddRange(gameManager.GetEnemyPlayer(owner).GetFieldCards());

        // Apply damage to all
        foreach (Card card in allCreatures)
        {
            if (card is CreatureCard creatureCard)
            {
                creatureCard.TakeDamage(effectValue);
            }
        }
    }

    private void ApplyBuffAllAttackEffect(CardGameManager gameManager, Player owner)
    {
        // Get all friendly creatures
        List<Card> friendlyCreatures = owner.GetFieldCards();

        // Apply buff to all
        foreach (Card card in friendlyCreatures)
        {
            if (card is CreatureCard creatureCard)
            {
                creatureCard.BuffAttack(effectValue);

                // Update visual
                if (card.visualInstance != null)
                    card.visualInstance.UpdateCardVisual();
            }
        }
    }
}