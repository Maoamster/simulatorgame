using UnityEngine;
using UnityEngine.EventSystems;

public class CardSlotDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public int slotIndex;
    public bool isPlayerSlot = true;

    private CardSlotVisual _visual;
    private Card _occupyingCard = null;

    private void Awake()
    {
        _visual = GetComponent<CardSlotVisual>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        // Get the dragged card
        CardVisual cardVisual = eventData.pointerDrag?.GetComponent<CardVisual>();

        if (cardVisual != null && isPlayerSlot && cardVisual.IsPlayable())
        {
            // Check if slot is empty
            if (_occupyingCard == null)
            {
                // Notify the game manager that a card was dropped on this slot
                bool success = CardGameManager.Instance.PlayCardToSlot(cardVisual.GetCard(), slotIndex);

                if (success)
                {
                    // Store reference to the card
                    _occupyingCard = cardVisual.GetCard();

                    // Show visual feedback
                    if (_visual != null)
                    {
                        _visual.ShowCardPlaced();
                    }
                }
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Check if we're dragging a card
        if (eventData.pointerDrag != null)
        {
            CardVisual cardVisual = eventData.pointerDrag.GetComponent<CardVisual>();

            if (cardVisual != null && isPlayerSlot && cardVisual.IsPlayable())
            {
                // Only highlight if slot is empty
                if (_occupyingCard == null)
                {
                    // Highlight this slot
                    if (_visual != null)
                    {
                        _visual.Highlight(true);
                    }
                }
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Remove highlight
        if (_visual != null)
        {
            _visual.Highlight(false);
        }
    }

    public void SetOccupyingCard(Card card)
    {
        _occupyingCard = card;

        // Update visual state
        if (_visual != null)
        {
            if (_occupyingCard != null)
            {
                _visual.ShowCardPlaced();
            }
            else
            {
                _visual.ShowCardRemoved();
            }
        }
    }

    public void ClearSlot()
    {
        _occupyingCard = null;

        // Update visual state
        if (_visual != null)
        {
            _visual.ShowCardRemoved();
        }
    }
}