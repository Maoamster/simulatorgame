using UnityEngine;
using UnityEngine.EventSystems;

public class CardSlotDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public int slotIndex;
    public bool isPlayerSlot = true;

    private CardSlotVisual _visual;

    private void Awake()
    {
        _visual = GetComponent<CardSlotVisual>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        // Get the dragged card
        CardVisual cardVisual = eventData.pointerDrag.GetComponent<CardVisual>();

        if (cardVisual != null && isPlayerSlot)
        {
            // Notify the game manager that a card was dropped on this slot
            CardGameManager.Instance.PlayCardToSlot(cardVisual.GetCard(), slotIndex);

            // Show visual feedback
            if (_visual != null)
            {
                _visual.ShowCardPlaced();
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
                // Highlight this slot
                if (_visual != null)
                {
                    _visual.Highlight(true);
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
}