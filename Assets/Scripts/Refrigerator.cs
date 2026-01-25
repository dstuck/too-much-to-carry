using UnityEngine;

/// <summary>
/// Refrigerator that can store cooked and burnt meat items.
/// Implements IInteractable to accept cooked/burnt meat and track the count.
/// </summary>
public class Refrigerator : MonoBehaviour, IInteractable
{
    [Header("Storage")]
    [SerializeField] private int storedCount = 0;

    /// <summary>
    /// Gets the number of meat items stored in the refrigerator.
    /// </summary>
    public int GetStoredCount()
    {
        return storedCount;
    }

    /// <summary>
    /// Checks if this refrigerator can interact with the given held item.
    /// Can accept cooked or burnt meat.
    /// </summary>
    /// <param name="heldItem">The item currently held by the player (null if hand is empty)</param>
    /// <returns>True if this refrigerator can interact with the held item</returns>
    public bool CanInteractWith(HoldableItem heldItem)
    {
        // Can only accept cooked or burnt meat
        if (heldItem != null && heldItem is RawMeat)
        {
            RawMeat meat = heldItem as RawMeat;
            if (meat.State == RawMeat.MeatState.Cooked || meat.State == RawMeat.MeatState.Burnt)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Performs the interaction with the given held item.
    /// Stores cooked or burnt meat in the refrigerator.
    /// </summary>
    /// <param name="heldItem">The item currently held by the player (should be cooked/burnt RawMeat)</param>
    public void InteractWith(HoldableItem heldItem)
    {
        if (CanInteractWith(heldItem) && heldItem is RawMeat)
        {
            RawMeat meat = heldItem as RawMeat;
            if (meat.State == RawMeat.MeatState.Cooked || meat.State == RawMeat.MeatState.Burnt)
            {
                // Store the meat - increment count
                storedCount++;
                Debug.Log($"[Refrigerator {gameObject.name}] Stored {meat.State} meat. Total count: {storedCount}");

                // Consume the meat - disable it
                heldItem.gameObject.SetActive(false);

                // The PlayerController will handle clearing the hand reference
            }
        }
    }
}
