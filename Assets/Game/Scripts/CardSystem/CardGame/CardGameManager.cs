using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using static CardEffect;
using DG.Tweening;
using System.Collections;

public class CardGameManager : MonoBehaviour
{
    public static CardGameManager Instance { get; private set; }

    [Header("Players")]
    public Player playerOne;
    public Player playerTwo;
    public bool isPlayerOneTurn = true;
    public delegate void SpellCastHandler(Player caster, Card spell);
    public event SpellCastHandler OnSpellCast;

    public delegate void TurnEndHandler(Player player);
    public event TurnEndHandler OnTurnEnd;

    public delegate void CardPlayedHandler(Player player, Card card);
    public event CardPlayedHandler OnCardPlayed;

    [Header("Sounds")]
    public AudioClip cardSelectSound;
    public AudioClip cardTargetSound;
    public AudioClip attackSound;
    public AudioClip damageSound;
    public AudioClip healSound;
    public AudioClip victorySound;
    public AudioClip defeatSound;

    [Header("UI References")]
    public GameBoardVisuals boardVisuals;
    public ManaDisplay playerManaDisplay;
    public ManaDisplay opponentManaDisplay;
    public HealthDisplay playerHealthDisplay;
    public HealthDisplay opponentHealthDisplay;
    public Sprite creatureIcon;
    public Sprite spellIcon;
    public RectTransform playArea;
    public GameObject cardPreviewPanel;
    public Image cardPreviewImage;
    public TMPro.TextMeshProUGUI cardPreviewName;
    public TMPro.TextMeshProUGUI cardPreviewDescription;
    public GameObject targetingArrow;
    public Button endTurnButton;
    public TMPro.TextMeshProUGUI turnText;
    public TMPro.TextMeshProUGUI playerOneHealthText;
    public TMPro.TextMeshProUGUI playerTwoHealthText;
    public TMPro.TextMeshProUGUI playerOneManaText;
    public TMPro.TextMeshProUGUI playerTwoManaText;
    public GameObject gameOverPanel;
    public TMPro.TextMeshProUGUI gameOverText;

    [Header("Game Settings")]
    public int startingHandSize = 4;
    public int maxTurns = 50;

    [System.Serializable]
    public class AttackPair
    {
        public Card attacker;
        public Card defender;
    }

    // For targeting
    private Vector3 _targetingStartPos;
    private Vector3 _targetingEndPos;

    // Private variables
    private List<AttackPair> _pendingAttacks = new List<AttackPair>();
    private CardVisual _selectedAttacker = null;
    private int _currentTurn = 1;
    private Card _targetingCard;
    private Player _targetingPlayer;
    private LineRenderer _targetingLine;
    private Dictionary<Player, List<System.Action>> _startOfTurnEffects = new Dictionary<Player, List<System.Action>>();
    private List<DelayedEffect> _delayedEffects = new List<DelayedEffect>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initialize targeting line
        _targetingLine = targetingArrow.GetComponent<LineRenderer>();
        targetingArrow.SetActive(false);

        // Hide game over panel
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void Start()
    {
        // Initialize game
        InitializeGame();
    }

    private void Update()
    {
        // Update targeting arrow if active
        if (targetingArrow.activeSelf)
        {
            UpdateTargetingArrow();
        }

        // Update UI
        UpdateUI();
    }

    private void InitializeGame()
    {
        // Initialize players
        playerOne.InitializeDeck();
        playerTwo.InitializeDeck();

        // Shuffle decks
        playerOne.ShuffleDeck();
        playerTwo.ShuffleDeck();

        // Draw starting hands
        playerOne.DrawCard(startingHandSize);
        playerTwo.DrawCard(startingHandSize);

        // Start first turn
        StartTurn();
    }

    public void SelectAttacker(CardVisual cardVisual)
    {
        // Deselect previous attacker if any
        if (_selectedAttacker != null)
        {
            _selectedAttacker.SetSelected(false);
        }

        // Set new attacker
        _selectedAttacker = cardVisual;
        _selectedAttacker.SetSelected(true);

        // Play selection sound
        if (cardSelectSound != null)
        {
            AudioSource.PlayClipAtPoint(cardSelectSound, Camera.main.transform.position);
        }
    }

    public void DeselectAttacker(CardVisual cardVisual)
    {
        if (_selectedAttacker == cardVisual)
        {
            _selectedAttacker.SetSelected(false);
            _selectedAttacker = null;

            // Remove any pending attacks from this attacker
            RemovePendingAttacksForCard(cardVisual.GetCard());
        }
    }

    public void SetAttackTarget(CardVisual targetCardVisual)
    {
        if (_selectedAttacker == null)
            return;

        Card attackerCard = _selectedAttacker.GetCard();
        Card targetCard = targetCardVisual.GetCard();

        // Check if target is valid
        if (IsValidAttackTarget(attackerCard, targetCard))
        {
            // Remove any existing attack for this attacker
            RemovePendingAttacksForCard(attackerCard);

            // Add new attack pair
            _pendingAttacks.Add(new AttackPair
            {
                attacker = attackerCard,
                defender = targetCard
            });

            // Update visuals
            _selectedAttacker.SetAttackTarget(targetCard);
            targetCardVisual.SetTargeted(true);

            // Deselect attacker
            _selectedAttacker.SetSelected(false);
            _selectedAttacker = null;
        }
    }

    private bool IsValidAttackTarget(Card attacker, Card defender)
    {
        // Check if defender is an enemy card
        Player attackerOwner = GetCardOwner(attacker);
        Player defenderOwner = GetCardOwner(defender);

        if (attackerOwner == defenderOwner)
            return false;

        // Check if defender has taunt
        if (defenderOwner.HasTauntCreatures() && !defender.hasTaunt)
            return false;

        return true;
    }

    private void RemovePendingAttacksForCard(Card card)
    {
        for (int i = _pendingAttacks.Count - 1; i >= 0; i--)
        {
            if (_pendingAttacks[i].attacker == card)
            {
                // Clear target highlight
                if (_pendingAttacks[i].defender.visualInstance != null)
                {
                    _pendingAttacks[i].defender.visualInstance.SetTargeted(false);
                }

                // Remove attack
                _pendingAttacks.RemoveAt(i);
            }
        }
    }

    public void DrawCardWithAnimation(Player player, int amount = 1)
    {
        for (int i = 0; i < amount; i++)
        {
            if (player.GetDeckCount() == 0)
            {
                PlayerTakeDamage(player, 1); // Fatigue damage
                continue;
            }

            if (player.GetHandCount() >= player.maxHandSize)
            {
                // Burn card if hand is full
                Card burnedCard = player.GetTopDeckCard();
                player.DiscardFromDeck(1);

                // Show card burn animation
                if (burnedCard != null && burnedCard.cardArtwork != null)
                {
                    // Create temporary visual for the burned card
                    GameObject burnVisual = new GameObject("BurnedCard");
                    burnVisual.transform.SetParent(playArea);

                    Image burnImage = burnVisual.AddComponent<Image>();
                    burnImage.sprite = burnedCard.cardArtwork;

                    // Position above deck
                    RectTransform burnRect = burnVisual.GetComponent<RectTransform>();
                    burnRect.anchoredPosition = new Vector2(0, 200);
                    burnRect.sizeDelta = new Vector2(100, 150);

                    // Burn animation
                    Sequence burnSequence = DOTween.Sequence();
                    burnSequence.Append(burnRect.DOScale(1.5f, 0.3f));
                    burnSequence.Join(burnImage.DOColor(Color.red, 0.3f));
                    burnSequence.Append(burnRect.DOScale(0, 0.3f));
                    burnSequence.Join(burnImage.DOFade(0, 0.3f));
                    burnSequence.OnComplete(() =>
                    {
                        Destroy(burnVisual);
                    });
                }

                continue;
            }

            // Draw the card with animation
            player.DrawCardWithAnimation();
        }
    }

    public void PlayerHeal(Player player, int amount)
    {
        player.Heal(amount);

        // Update health display with animation
        if (player == playerOne && playerHealthDisplay != null)
        {
            playerHealthDisplay.UpdateHealthDisplay(player.health);
        }
        else if (player == playerTwo && opponentHealthDisplay != null)
        {
            opponentHealthDisplay.UpdateHealthDisplay(player.health);
        }

        // Play heal sound
        if (healSound != null)
        {
            AudioSource.PlayClipAtPoint(healSound, Camera.main.transform.position);
        }
    }

    public void PlayerTakeDamage(Player player, int amount)
    {
        player.TakeDamage(amount);

        // Update health display with animation
        if (player == playerOne && playerHealthDisplay != null)
        {
            playerHealthDisplay.UpdateHealthDisplay(player.health);
        }
        else if (player == playerTwo && opponentHealthDisplay != null)
        {
            opponentHealthDisplay.UpdateHealthDisplay(player.health);
        }

        // Play damage sound
        if (damageSound != null)
        {
            AudioSource.PlayClipAtPoint(damageSound, Camera.main.transform.position);
        }

        // Check for game over
        if (player.health <= 0)
        {
            EndGame(player);
        }
    }

    public Player GetCurrentPlayer()
    {
        return isPlayerOneTurn ? playerOne : playerTwo;
    }

    public void StartTurn()
    {
        Player currentPlayer = isPlayerOneTurn ? playerOne : playerTwo;
        currentPlayer.StartTurn();

        // Trigger start of turn effects
        if (_startOfTurnEffects.ContainsKey(currentPlayer))
        {
            foreach (System.Action effect in _startOfTurnEffects[currentPlayer])
            {
                effect();
            }
        }

        // Process delayed effects
        ProcessDelayedEffects();

        // Play turn start visual effects
        if (boardVisuals != null)
        {
            boardVisuals.PlayFieldParticles(isPlayerOneTurn);
        }

        // Update UI
        UpdateUI();
        endTurnButton.interactable = true;

        // If it's AI's turn, trigger AI logic
        if (currentPlayer is AIPlayer aiPlayer)
        {
            Debug.Log("Starting AI turn");
            aiPlayer.StartAITurn();
        }
    }

    private void ProcessDelayedEffects()
    {
        for (int i = _delayedEffects.Count - 1; i >= 0; i--)
        {
            DelayedEffect effect = _delayedEffects[i];
            effect.turnsRemaining--;

            if (effect.turnsRemaining <= 0)
            {
                // Trigger the effect
                effect.effect();

                // Remove from list
                _delayedEffects.RemoveAt(i);
            }
        }
    }

    public void RegisterStartOfTurnEffect(Card card, System.Action<Player> effect)
    {
        Player owner = GetCardOwner(card);
        if (owner != null)
        {
            if (!_startOfTurnEffects.ContainsKey(owner))
            {
                _startOfTurnEffects[owner] = new List<System.Action>();
            }

            _startOfTurnEffects[owner].Add(() => effect(owner));
        }
    }

    public void RegisterDelayedEffect(Player owner, int turns, System.Action effect)
    {
        _delayedEffects.Add(new DelayedEffect
        {
            owner = owner,
            turnsRemaining = turns,
            effect = effect
        });
    }

    public void ShowDiscoverUI(List<Card> options, System.Action<Card> onCardSelected)
    {
        // This is a placeholder - you'll need to implement the UI
        Debug.Log("Showing discover UI with " + options.Count + " options");

        // For now, just select the first option automatically
        if (options.Count > 0)
        {
            onCardSelected(options[0]);
        }
    }

    public void ShowAdaptUI(List<AdaptOption> options, System.Action<AdaptOption> onOptionSelected)
    {
        // This is a placeholder - you'll need to implement the UI
        Debug.Log("Showing adapt UI with " + options.Count + " options");

        // For now, just select the first option automatically
        if (options.Count > 0)
        {
            onOptionSelected(options[0]);
        }
    }

    public bool PlayCardToSlot(Card card, int slotIndex)
    {
        Player currentPlayer = isPlayerOneTurn ? playerOne : playerTwo;

        // Check if card is playable
        if (!currentPlayer.GetHandCards().Contains(card))
            return false;

        if (currentPlayer.currentMana < card.manaCost)
            return false;

        if (card.type != Card.CardType.Creature)
            return false;

        // Play the card to the specific slot
        bool success = currentPlayer.PlayCardToSlot(card, slotIndex);

        if (success && boardVisuals != null)
        {
            // Play visual effects
            boardVisuals.PlayFieldParticles(isPlayerOneTurn);
        }

        return success;
    }

    public void SpendMana(Player player, int amount)
    {
        // Ensure we don't go below zero
        int actualAmount = Mathf.Min(player.currentMana, amount);
        player.currentMana -= actualAmount;

        // Update mana display with animation
        if (player == playerOne && playerManaDisplay != null)
        {
            playerManaDisplay.AnimateManaCrystalUse(actualAmount);
            playerManaDisplay.UpdateManaDisplay(player.currentMana, player.maxMana);
        }
        else if (player == playerTwo && opponentManaDisplay != null)
        {
            opponentManaDisplay.AnimateManaCrystalUse(actualAmount);
            opponentManaDisplay.UpdateManaDisplay(player.currentMana, player.maxMana);
        }
    }

    public void EndTurn()
    {
        // Process all pending attacks
        StartCoroutine(ProcessAttacksSequentially());
    }

    private IEnumerator ProcessAttacksSequentially()
    {
        // Disable end turn button during attacks
        endTurnButton.interactable = false;

        // Process each attack with a delay between them
        for (int i = 0; i < _pendingAttacks.Count; i++)
        {
            AttackPair attack = _pendingAttacks[i];

            // Execute the attack
            yield return StartCoroutine(ExecuteAttackWithAnimation(attack.attacker, attack.defender));

            // Wait a bit between attacks
            yield return new WaitForSeconds(0.5f);
        }

        // Clear pending attacks
        _pendingAttacks.Clear();

        // Continue with normal end turn logic
        Player currentPlayer = isPlayerOneTurn ? playerOne : playerTwo;
        OnTurnEnd?.Invoke(currentPlayer);

        // Switch turns
        isPlayerOneTurn = !isPlayerOneTurn;

        // Increment turn counter if it's back to player one
        if (isPlayerOneTurn)
        {
            _currentTurn++;
            if (_currentTurn > maxTurns)
            {
                EndGame(null); // Draw
                yield break;
            }
        }

        // Start next turn
        StartTurn();
    }

    private IEnumerator ExecuteAttackWithAnimation(Card attacker, Card defender)
    {
        if (attacker is CreatureCard attackerCreature && defender is CreatureCard defenderCreature)
        {
            // Get visual instances
            CardVisual attackerVisual = attacker.visualInstance;
            CardVisual defenderVisual = defender.visualInstance;

            if (attackerVisual != null && defenderVisual != null)
            {
                // Store original position
                Vector3 originalPos = attackerVisual.transform.position;

                // Debug log to track damage values
                Debug.Log($"ATTACK: {attackerCreature.cardName} ({attackerCreature.currentAttack}) vs {defenderCreature.cardName} ({defenderCreature.currentAttack})");

                // Animate attacker moving toward defender
                yield return attackerVisual.transform.DOMove(
                    Vector3.Lerp(originalPos, defenderVisual.transform.position, 0.7f),
                    0.3f).SetEase(Ease.OutQuad).WaitForCompletion();

                // Shake defender to show impact
                defenderVisual.transform.DOShakePosition(0.2f, 10f, 10, 90, false, true);

                // Apply damage - use currentAttack instead of attack
                defenderCreature.TakeDamage(attackerCreature.currentAttack);
                attackerCreature.TakeDamage(defenderCreature.currentAttack);

                Debug.Log($"AFTER ATTACK: {attackerCreature.cardName} (Health:{attackerCreature.currentHealth}) vs {defenderCreature.cardName} (Health:{defenderCreature.currentHealth})");

                // Update visuals
                attackerVisual.UpdateCardVisual();
                defenderVisual.UpdateCardVisual();

                // Return attacker to original position
                yield return attackerVisual.transform.DOMove(originalPos, 0.3f)
                    .SetEase(Ease.InQuad).WaitForCompletion();

                // Mark attacker as having attacked
                attackerCreature.hasAttackedThisTurn = true;

                // Check if cards died
                yield return StartCoroutine(CheckCardDeathWithAnimation(defenderCreature));
                yield return StartCoroutine(CheckCardDeathWithAnimation(attackerCreature));
            }
        }
    }

    private IEnumerator CheckCardDeathWithAnimation(CreatureCard card)
    {
        // Debug to check if this method is being called
        Debug.Log($"Checking if {card.cardName} should die. Health: {card.currentHealth}");

        if (card.currentHealth <= 0)
        {
            Debug.Log($"{card.cardName} has died!");

            CardVisual cardVisual = card.visualInstance;

            if (cardVisual != null)
            {
                // Play death animation
                Sequence deathSequence = DOTween.Sequence();

                // Fade out
                CanvasGroup canvasGroup = cardVisual.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = cardVisual.gameObject.AddComponent<CanvasGroup>();

                deathSequence.Append(canvasGroup.DOFade(0, 0.5f));

                // Shrink
                deathSequence.Join(cardVisual.transform.DOScale(Vector3.zero, 0.5f));

                // Rotate
                deathSequence.Join(cardVisual.transform.DORotate(new Vector3(0, 0, 90), 0.5f, RotateMode.FastBeyond360));

                // Wait for animation to complete
                yield return deathSequence.WaitForCompletion();
            }

            // Remove from field
            Player owner = GetCardOwner(card);
            if (owner != null)
            {
                Debug.Log($"Removing {card.cardName} from {owner.playerName}'s field");
                owner.RemoveCardFromField(card);
            }
        }
    }

    public void NotifyCardPlayed(Player player, Card card)
    {
        OnCardPlayed?.Invoke(player, card);

        // If it's a spell, also trigger the spell cast event
        if (card.type == Card.CardType.Spell)
        {
            OnSpellCast?.Invoke(player, card);
        }
    }

    public void EndGame(Player loser)
    {
        string resultMessage;
        bool playerWon = false;

        if (loser == null)
        {
            resultMessage = "Game ended in a draw!";
        }
        else
        {
            Player winner = (loser == playerOne) ? playerTwo : playerOne;
            resultMessage = $"{winner.playerName} wins!";
            playerWon = (winner == playerOne);

            // Play victory/defeat sound
            if (playerWon && victorySound != null)
            {
                AudioSource.PlayClipAtPoint(victorySound, Camera.main.transform.position);
            }
            else if (!playerWon && defeatSound != null)
            {
                AudioSource.PlayClipAtPoint(defeatSound, Camera.main.transform.position);
            }
        }

        Debug.Log(resultMessage);

        // Show game over UI
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            gameOverText.text = resultMessage;

            // Add visual effects to game over panel
            gameOverPanel.transform.localScale = Vector3.zero;
            gameOverPanel.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);

            // Fade in background
            Image panelImage = gameOverPanel.GetComponent<Image>();
            if (panelImage != null)
            {
                panelImage.color = new Color(panelImage.color.r, panelImage.color.g, panelImage.color.b, 0);
                panelImage.DOFade(0.8f, 0.5f);
            }
        }
    }



    public bool IsPlayerTurn(Player player)
    {
        return (isPlayerOneTurn && player == playerOne) || (!isPlayerOneTurn && player == playerTwo);
    }

    public Player GetEnemyPlayer(Player player)
    {
        return player == playerOne ? playerTwo : playerOne;
    }

    public bool IsOverPlayArea(Vector2 screenPosition)
    {
        // Convert screen position to local position in play area
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            playArea, screenPosition, Camera.main, out Vector2 localPoint))
        {
            // Debug.Log("Point in play area: " + localPoint);
            return playArea.rect.Contains(localPoint);
        }
        return false;
    }


    public bool IsOverPlayerField(Vector2 screenPosition, Player player)
    {
        // Convert screen position to world position
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0));

        // Check if there's a collider at this position
        Collider2D hitCollider = Physics2D.OverlapPoint(new Vector2(worldPos.x, worldPos.y));

        // Debug information
        Debug.Log($"Screen pos: {screenPosition}, World pos: {worldPos}");
        if (hitCollider != null)
        {
            Debug.Log($"Hit collider: {hitCollider.gameObject.name}");
        }
        else
        {
            Debug.Log("No collider hit");
        }

        // Check if the hit collider is the player field
        return hitCollider != null && hitCollider.transform == player.fieldArea;
    }

    public void ShowCardPreview(Card card)
    {
        cardPreviewPanel.SetActive(true);
        cardPreviewName.text = card.cardName;
        cardPreviewDescription.text = card.cardDescription;

        if (card.cardArtwork != null)
            cardPreviewImage.sprite = card.cardArtwork;
    }

    public void HideCardPreview()
    {
        cardPreviewPanel.SetActive(false);
    }

    public void StartTargeting(Card card, Player owner)
    {
        _targetingCard = card;
        _targetingPlayer = owner;
        targetingArrow.SetActive(true);
    }

    public void EndTargeting()
    {
        _targetingCard = null;
        _targetingPlayer = null;
        targetingArrow.SetActive(false);
    }

    private void UpdateTargetingArrow()
    {
        // Update line renderer positions
        if (_targetingCard != null && _targetingCard.visualInstance != null)
        {
            Vector3 startPos = _targetingCard.visualInstance.transform.position;
            Vector3 endPos = Input.mousePosition;
            endPos.z = 10; // Set some distance from the camera
            endPos = Camera.main.ScreenToWorldPoint(endPos);

            _targetingLine.SetPosition(0, startPos);
            _targetingLine.SetPosition(1, endPos);
        }
    }

    public Card GetTargetUnderMouse()
    {
        // Cast a ray from the mouse position
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

        if (hit.collider != null)
        {
            CardVisual cardVisual = hit.collider.GetComponent<CardVisual>();
            if (cardVisual != null)
            {
                // Check if it's an enemy card
                Player cardOwner = cardVisual.GetOwner();
                if (cardOwner != _targetingPlayer)
                {
                    return cardVisual.GetCard();
                }
            }
        }

        return null;
    }

    public Card GetSheepCard()
    {
        // Return a predefined sheep card
        // You'll need to create this card asset
        return Resources.Load<Card>("Cards/Sheep");
    }

    public Player GetCardOwner(Card card)
    {
        // Check if card is in player one's field or hand
        if (playerOne.GetFieldCards().Contains(card) || playerOne.GetHandCards().Contains(card))
            return playerOne;

        // Check if card is in player two's field or hand
        if (playerTwo.GetFieldCards().Contains(card) || playerTwo.GetHandCards().Contains(card))
            return playerTwo;

        return null;
    }

    public void AttackCard(Card attacker, Card defender)
    {
        if (attacker is CreatureCard attackerCreature && defender is CreatureCard defenderCreature)
        {
            Debug.Log($"Direct attack: {attackerCreature.cardName} ({attackerCreature.currentAttack}) vs {defenderCreature.cardName} ({defenderCreature.currentAttack})");

            // Apply damage - use currentAttack instead of attack
            defenderCreature.TakeDamage(attackerCreature.currentAttack);
            attackerCreature.TakeDamage(defenderCreature.currentAttack);

            Debug.Log($"After direct attack: {attackerCreature.cardName} (Health:{attackerCreature.currentHealth}) vs {defenderCreature.cardName} (Health:{defenderCreature.currentHealth})");

            // Mark attacker as having attacked this turn
            attackerCreature.hasAttackedThisTurn = true;

            // Play attack sound
            if (attacker.attackSound != null && attacker.visualInstance != null)
            {
                AudioSource.PlayClipAtPoint(attacker.attackSound, attacker.visualInstance.transform.position);
            }

            // Check if cards died
            if (defenderCreature.currentHealth <= 0)
            {
                Player defenderOwner = GetCardOwner(defenderCreature);
                if (defenderOwner != null)
                {
                    defenderOwner.RemoveCardFromField(defenderCreature);
                }
            }

            if (attackerCreature.currentHealth <= 0)
            {
                Player attackerOwner = GetCardOwner(attackerCreature);
                if (attackerOwner != null)
                {
                    attackerOwner.RemoveCardFromField(attackerCreature);
                }
            }

            // Update visuals
            if (attacker.visualInstance != null)
                attacker.visualInstance.UpdateCardVisual();

            if (defender.visualInstance != null)
                defender.visualInstance.UpdateCardVisual();
        }
    }

    private void CheckCardDeath(CreatureCard card)
    {
        if (card.health <= 0)
        {
            Debug.Log($"{card.cardName} has died!");

            // Find the owner
            Player owner = GetCardOwner(card);
            if (owner != null)
            {
                // Remove from field
                owner.RemoveCardFromField(card);
            }
        }
    }

    private void UpdateUI()
    {
        // Update health displays
        if (playerHealthDisplay != null)
            playerHealthDisplay.UpdateHealthDisplay(playerOne.health);

        if (opponentHealthDisplay != null)
            opponentHealthDisplay.UpdateHealthDisplay(playerTwo.health);

        // Update mana displays
        if (playerManaDisplay != null)
            playerManaDisplay.UpdateManaDisplay(playerOne.currentMana, playerOne.maxMana);

        if (opponentManaDisplay != null)
            opponentManaDisplay.UpdateManaDisplay(playerTwo.currentMana, playerTwo.maxMana);

        // Update turn indicator
        if (boardVisuals != null)
            boardVisuals.UpdateTurnIndicator(isPlayerOneTurn);

        // Update turn text
        if (turnText != null)
            turnText.text = $"Turn {_currentTurn}: {(isPlayerOneTurn ? playerOne.playerName : playerTwo.playerName)}'s Turn";
    }

    public void RestartGame()
    {
        // Hide game over panel
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // Reset turn counter
        _currentTurn = 1;
        isPlayerOneTurn = true;

        // Reset players
        playerOne.health = 30;
        playerTwo.health = 30;
        playerOne.maxMana = 1;
        playerOne.currentMana = 1;
        playerTwo.maxMana = 1;
        playerTwo.currentMana = 1;

        // Clear all cards
        playerOne.ClearAllCards();
        playerTwo.ClearAllCards();

        // Initialize game again
        InitializeGame();
    }
}