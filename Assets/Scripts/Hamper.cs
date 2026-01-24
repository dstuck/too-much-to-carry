using UnityEngine;

/// <summary>
/// Hamper that can store folded laundry items.
/// Implements IInteractable to accept folded laundry and track the count.
/// </summary>
public class Hamper : MonoBehaviour, IInteractable
{
    [Header("Storage")]
    [SerializeField] private int storedCount = 0;

    /// <summary>
    /// Gets the number of folded laundry items stored in the hamper.
    /// </summary>
    public int GetStoredCount()
    {
        return storedCount;
    }

    /// <summary>
    /// Checks if this hamper can interact with the given held item.
    /// Can accept folded laundry.
    /// </summary>
    /// <param name="heldItem">The item currently held by the player (null if hand is empty)</param>
    /// <returns>True if this hamper can interact with the held item</returns>
    public bool CanInteractWith(HoldableItem heldItem)
    {
        // Can only accept folded laundry
        if (heldItem != null && heldItem is Laundry)
        {
            Laundry laundry = heldItem as Laundry;
            if (laundry.State == Laundry.LaundryState.Folded)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Performs the interaction with the given held item.
    /// Stores folded laundry in the hamper.
    /// </summary>
    /// <param name="heldItem">The item currently held by the player (should be folded Laundry)</param>
    public void InteractWith(HoldableItem heldItem)
    {
        if (CanInteractWith(heldItem) && heldItem is Laundry)
        {
            Laundry laundry = heldItem as Laundry;
            if (laundry.State == Laundry.LaundryState.Folded)
            {
                // Store the laundry - increment count
                storedCount++;
                Debug.Log($"[Hamper {gameObject.name}] Stored folded laundry. Total count: {storedCount}");

                // Consume the laundry - disable it
                heldItem.gameObject.SetActive(false);

                // The PlayerController will handle clearing the hand reference
            }
        }
    }
}
