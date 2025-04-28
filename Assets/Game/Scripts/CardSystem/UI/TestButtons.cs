using UnityEngine;
using UnityEngine.UI;

public class TestButtons : MonoBehaviour
{
    public Player player;
    public AIPlayer aiPlayer;
    public CardGameManager gameManager;

    // Reference to specific cards you want to test
    public Card flameImpCard;
    public Card mechEngineerCard;
    // Add more cards as needed

    private void Start()
    {
        // Set up button listeners
        Transform buttonPanel = transform.Find("ButtonPanel");

        Button drawButton = buttonPanel.Find("DrawButton").GetComponent<Button>();
        drawButton.onClick.AddListener(DrawCard);

        Button flameImpButton = buttonPanel.Find("FlameImpButton").GetComponent<Button>();
        flameImpButton.onClick.AddListener(AddFlameImp);

        Button mechEngineerButton = buttonPanel.Find("MechEngineerButton").GetComponent<Button>();
        mechEngineerButton.onClick.AddListener(AddMechEngineer);

        Button clearButton = buttonPanel.Find("ClearButton").GetComponent<Button>();
        clearButton.onClick.AddListener(ClearHand);

        Button resetButton = buttonPanel.Find("ResetButton").GetComponent<Button>();
        resetButton.onClick.AddListener(ResetGame);
    }

    public void DrawCard()
    {
        player.DrawCard();
    }

    public void AddFlameImp()
    {
        AddCardToHand(flameImpCard);
    }

    public void AddMechEngineer()
    {
        AddCardToHand(mechEngineerCard);
    }

    private void AddCardToHand(Card cardPrefab)
    {
        if (cardPrefab != null)
        {
            Card newCard = Instantiate(cardPrefab);
            player.AddCardToHand(newCard);
        }
    }

    public void ClearHand()
    {
        player.ClearAllCards();
    }

    public void ResetGame()
    {
        gameManager.RestartGame();
    }
}