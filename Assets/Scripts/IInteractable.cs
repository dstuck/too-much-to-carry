using UnityEngine;

/// <summary>
/// Interface for objects that can be interacted with by the player.
/// Objects implement this to define their own interaction behaviors.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Checks if this object can interact with the given held item.
    /// </summary>
    /// <param name="heldItem">The item currently held by the player (null if hand is empty)</param>
    /// <returns>True if this object can interact with the held item</returns>
    bool CanInteractWith(HoldableItem heldItem);

    /// <summary>
    /// Performs the interaction with the given held item.
    /// </summary>
    /// <param name="heldItem">The item currently held by the player (null if hand is empty)</param>
    void InteractWith(HoldableItem heldItem);
}
