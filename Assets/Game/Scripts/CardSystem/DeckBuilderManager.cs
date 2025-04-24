using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DeckBuilderManager : MonoBehaviour
{
    public static DeckBuilderManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject cardPreviewPanel;
    public Image cardPreviewImage;
    public TextMeshProUGUI cardPreviewName;
    public TextMeshProUGUI cardPreviewDescription;
    public TextMeshProUGUI cardPreviewStats;
    public DeckBuilder deckBuilder;
    public TMP_Dropdown deckDropdown;
    public Button loadDeckButton;
    public Button newDeckButton;

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

        // Hide card preview
        if (cardPreviewPanel != null)
            cardPreviewPanel.SetActive(false);
    }

    private void Start()
    {
        // Set up buttons
        if (loadDeckButton != null)
            loadDeckButton.onClick.AddListener(LoadSelectedDeck);

        if (newDeckButton != null)
            newDeckButton.onClick.AddListener(CreateNewDeck);

        // Populate deck dropdown
        PopulateDeckDropdown();
    }

    public void ShowCardPreview(Card card)
    {
        if (cardPreviewPanel == null)
            return;

        cardPreviewPanel.SetActive(true);

        if (cardPreviewName != null)
            cardPreviewName.text = card.cardName;

        if (cardPreviewDescription != null)
            cardPreviewDescription.text = card.cardDescription;

        if (cardPreviewImage != null && card.cardArtwork != null)
            cardPreviewImage.sprite = card.cardArtwork;

        if (cardPreviewStats != null)
        {
            string statsText = $"Cost: {card.manaCost}";

            if (card.type == Card.CardType.Creature)
            {
                statsText += $"\nAttack: {card.attack}";
                statsText += $"\nHealth: {card.health}";

                if (card.hasCharge)
                    statsText += "\nCharge";

                if (card.hasTaunt)
                    statsText += "\nTaunt";
            }

            cardPreviewStats.text = statsText;
        }
    }

    public void HideCardPreview()
    {
        if (cardPreviewPanel != null)
            cardPreviewPanel.SetActive(false);
    }

    private void PopulateDeckDropdown()
    {
        if (deckDropdown == null)
            return;

        deckDropdown.ClearOptions();

        // Get saved decks
        string deckList = PlayerPrefs.GetString("DeckList", "");
        if (!string.IsNullOrEmpty(deckList))
        {
            string[] deckNames = deckList.Split(',');
            List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

            foreach (string deckName in deckNames)
            {
                options.Add(new TMP_Dropdown.OptionData(deckName));
            }

            deckDropdown.AddOptions(options);
        }
    }

    private void LoadSelectedDeck()
    {
        if (deckDropdown == null || deckDropdown.options.Count == 0)
            return;

        string deckName = deckDropdown.options[deckDropdown.value].text;
        deckBuilder.LoadDeck(deckName);
    }

    private void CreateNewDeck()
    {
        if (deckBuilder == null)
            return;

        // Clear current deck
        deckBuilder.ClearDeck();
    }
}