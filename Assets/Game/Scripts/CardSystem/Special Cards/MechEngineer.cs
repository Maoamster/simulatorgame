using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "MechEngineer", menuName = "Cards/Special/MechEngineer")]
public class MechEngineer : CreatureCard
{
    [SerializeField] private List<Card> mechCards; // Assign your 5 mech cards in the inspector

    public override void OnPlay(CardGameManager gameManager, Player owner, List<Card> targets = null)
    {
        base.OnPlay(gameManager, owner, targets);

        // Add a random mech card to hand
        if (mechCards != null && mechCards.Count > 0)
        {
            int randomIndex = Random.Range(0, mechCards.Count);
            Card randomMech = mechCards[randomIndex];

            // Create a copy of the card
            Card cardCopy = Instantiate(randomMech);

            // Add to hand
            owner.AddCardToHand(cardCopy);

            // Visual feedback
            Debug.Log($"Added {randomMech.cardName} to hand");
        }
        else
        {
            Debug.LogWarning("MechEngineer: No mech cards assigned!");
        }
    }
}