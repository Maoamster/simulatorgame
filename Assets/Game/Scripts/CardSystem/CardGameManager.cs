using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CardGameManager : MonoBehaviour
{
    public static CardGameManager Instance { get; private set; }

    [Header("Players")]
    public Player playerOne;
    public Player playerTwo;
    public bool isPlayerOneTurn = true;

    [Header("UI References")]
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

    // For targeting
    private Vector3 _targetingStartPos;
    private Vector3 _targetingEndPos;

    // Private variables
    private int _currentTurn = 1;
    private Card _targetingCard;
    private Player _targetingPlayer;
    private LineRenderer _targetingLine;

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

    public void StartTurn()
    {
        Player currentPlayer = isPlayerOneTurn ? playerOne : playerTwo;
        currentPlayer.StartTurn();

        // Update UI
        turnText.text = $"Turn {_currentTurn}: {currentPlayer.playerName}'s Turn";
        endTurnButton.interactable = true;
    }

    public void EndTurn()
    {
        Player currentPlayer = isPlayerOneTurn ? playerOne : playerTwo;
        currentPlayer.EndTurn();

        // Switch turns
        isPlayerOneTurn = !isPlayerOneTurn;

        // Increment turn counter if it's back to player one
        if (isPlayerOneTurn)
        {
            _currentTurn++;

            // Check for max turns
            if (_currentTurn > maxTurns)
            {
                EndGame(null); // Draw
                return;
            }
        }

        // Start next turn
        StartTurn();
    }

    public void EndGame(Player loser)
    {
        string resultMessage;

        if (loser == null)
        {
            resultMessage = "Game ended in a draw!";
        }
        else
        {
            Player winner = (loser == playerOne) ? playerTwo : playerOne;
            resultMessage = $"{winner.playerName} wins!";
        }

        Debug.Log(resultMessage);

        // Show game over UI
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            gameOverText.text = resultMessage;
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
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            playArea, screenPosition, null, out Vector2 localPoint);

        // Check if point is inside play area
        return playArea.rect.Contains(localPoint);
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

    public void AttackCard(Card attacker, Card defender)
    {
        if (attacker is CreatureCard attackerCreature && defender is CreatureCard defenderCreature)
        {
            // Apply damage to both cards
            defenderCreature.TakeDamage(attackerCreature.attack);
            attackerCreature.TakeDamage(defenderCreature.attack);

            // Mark attacker as having attacked this turn
            attackerCreature.canAttackThisTurn = false;

            // Play attack sound
            if (attacker.attackSound != null && attacker.visualInstance != null)
            {
                AudioSource.PlayClipAtPoint(attacker.attackSound, attacker.visualInstance.transform.position);
            }

            // Update visuals
            if (attacker.visualInstance != null)
                attacker.visualInstance.UpdateCardVisual();

            if (defender.visualInstance != null)
                defender.visualInstance.UpdateCardVisual();
        }
    }

    private void UpdateUI()
    {
        // Update health and mana text
        playerOneHealthText.text = $"Health: {playerOne.health}";
        playerTwoHealthText.text = $"Health: {playerTwo.health}";
        playerOneManaText.text = $"Mana: {playerOne.currentMana}/{playerOne.maxMana}";
        playerTwoManaText.text = $"Mana: {playerTwo.currentMana}/{playerTwo.maxMana}";
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