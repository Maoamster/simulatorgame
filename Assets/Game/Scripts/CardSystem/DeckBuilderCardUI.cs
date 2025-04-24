using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

public class DeckBuilderCardUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    public Image cardImage;
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI cardCostText;
    public TextMeshProUGUI cardCountText;
    public Image cardBackground;

    // Events
    public event Action<Card> OnCardClicked;

    // Private variables
    private Card _card;
    private Vector3 _originalScale;

    private void Awake()
    {
        _originalScale = transform.localScale;

        if (cardCountText != null)
            cardCountText.gameObject.SetActive(false);
    }

    public void SetupCard(Card card)
    {
        _card = card;

        // Set card info
        if (cardNameText != null)
            cardNameText.text = card.cardName;

        if (cardCostText != null)
            cardCostText.text = card.manaCost.ToString();

        if (cardImage != null && card.cardArtwork != null)
            cardImage.sprite = card.cardArtwork;

        if (cardBackground != null)
            cardBackground.color = card.cardColor;
    }

    public void SetCount(int count)
    {
        if (cardCountText != null)
        {
            cardCountText.gameObject.SetActive(count > 1);
            cardCountText.text = "x" + count;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnCardClicked?.Invoke(_card);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = _originalScale * 1.1f;

        // Show card preview
        DeckBuilderManager.Instance?.ShowCardPreview(_card);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = _originalScale;

        // Hide card preview
        DeckBuilderManager.Instance?.HideCardPreview();
    }
}