using UnityEngine;
using UnityEngine.EventSystems;

public class DropZone : MonoBehaviour, IDropHandler
{
    public enum ZoneType
    {
        PlayerField,
        PlayerHand,
        OpponentField,
        OpponentHand
    }

    public ZoneType zoneType;
    public static DropZone currentHoveredZone;

    public void OnDrop(PointerEventData eventData)
    {
        // This method will be called when something is dropped on this zone
        Debug.Log($"Something dropped on {gameObject.name}");

        // The actual handling will be done by the CardVisual script
    }

    void OnEnable()
    {
        // Register with EventTrigger
        EventTrigger trigger = GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = gameObject.AddComponent<EventTrigger>();
        }

        // Add pointer enter event
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => { OnPointerEnter((PointerEventData)data); });
        trigger.triggers.Add(enterEntry);

        // Add pointer exit event
        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => { OnPointerExit((PointerEventData)data); });
        trigger.triggers.Add(exitEntry);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        currentHoveredZone = this;
        Debug.Log($"Entered {gameObject.name}");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (currentHoveredZone == this)
        {
            currentHoveredZone = null;
            Debug.Log($"Exited {gameObject.name}");
        }
    }
}