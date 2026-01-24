using UnityEngine;

/// <summary>
/// Laundry item that can be folded when placed on a counter.
/// Tracks folded/unfolded state and changes sprite accordingly.
/// </summary>
public class Laundry : HoldableItem, IInteractable
{
    /// <summary>
    /// Enum representing the current folding state of the laundry.
    /// </summary>
    public enum LaundryState
    {
        Unfolded,
        Folded
    }

    [Header("Sprites")]
    [SerializeField] private Sprite unfoldedSprite;
    [SerializeField] private Sprite foldedSprite;

    // Laundry state
    private LaundryState currentState = LaundryState.Unfolded;
    private bool isOnCounter = false;

    // Components to disable when held (but keep GameObject active for updates)
    private Collider2D laundryCollider;

    /// <summary>
    /// Gets the current folding state of the laundry.
    /// </summary>
    public LaundryState State => currentState;

    private void Awake()
    {
        // Get SpriteRenderer
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Get Collider2D
        laundryCollider = GetComponent<Collider2D>();
    }

    /// <summary>
    /// Override GetSprite to return the correct sprite based on current state.
    /// </summary>
    public override Sprite GetSprite()
    {
        if (currentState == LaundryState.Folded && foldedSprite != null)
        {
            return foldedSprite;
        }
        else if (currentState == LaundryState.Unfolded && unfoldedSprite != null)
        {
            return unfoldedSprite;
        }
        
        // Fallback to base implementation
        return base.GetSprite();
    }

    /// <summary>
    /// Called when the laundry is picked up. Keeps GameObject active for state updates.
    /// </summary>
    public override void OnPickedUp()
    {
        // Don't call base.OnPickedUp() - we want to keep the GameObject active
        // Instead, hide the sprite and disable the collider
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        if (laundryCollider != null)
        {
            laundryCollider.enabled = false;
        }

        // Reset counter flag when picked up
        isOnCounter = false;

        // Refresh UI to show current sprite
        RefreshUIForHeldLaundry();
    }

    /// <summary>
    /// Called when the laundry is put down. Detects counter placement.
    /// </summary>
    /// <param name="position">World position to place the laundry</param>
    public override void OnPutDown(Vector3 position)
    {
        // Re-enable sprite and collider (GameObject was kept active)
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        if (laundryCollider != null)
        {
            laundryCollider.enabled = true;
        }

        // Don't call base.OnPutDown() since we didn't disable the GameObject
        transform.position = position;

        // Check if position overlaps with a counter (counters use "Crib" tag)
        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter();
        filter.useTriggers = false; // Counters use non-trigger colliders

        Collider2D[] hits = new Collider2D[10];
        int hitCount = Physics2D.OverlapPoint(position, filter, hits);

        bool foundCounter = false;
        for (int i = 0; i < hitCount; i++)
        {
            if (hits[i].gameObject.CompareTag("Crib"))
            {
                foundCounter = true;
                break;
            }
        }

        isOnCounter = foundCounter;

        if (foundCounter)
        {
            Debug.Log($"[Laundry {gameObject.name}] Placed on counter. State: {currentState}");
        }
    }

    /// <summary>
    /// Checks if this laundry can interact with the given held item.
    /// Can fold if unfolded, on counter, and hand is empty.
    /// </summary>
    /// <param name="heldItem">The item currently held by the player (null if hand is empty)</param>
    /// <returns>True if this laundry can interact with the held item</returns>
    public bool CanInteractWith(HoldableItem heldItem)
    {
        // Can only fold if unfolded, on counter, and hand is empty
        if (currentState == LaundryState.Unfolded && isOnCounter && heldItem == null)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Performs the interaction with the given held item.
    /// Folds the laundry if conditions are met.
    /// </summary>
    /// <param name="heldItem">The item currently held by the player (should be null for folding)</param>
    public void InteractWith(HoldableItem heldItem)
    {
        if (CanInteractWith(heldItem) && currentState == LaundryState.Unfolded)
        {
            // Fold the laundry
            currentState = LaundryState.Folded;
            
            // Update sprite to folded sprite
            if (spriteRenderer != null && foldedSprite != null)
            {
                spriteRenderer.sprite = foldedSprite;
            }
            
            Debug.Log($"[Laundry {gameObject.name}] Folded!");

            // Refresh UI if being held (shouldn't happen, but just in case)
            RefreshUIForHeldLaundry();
        }
    }

    /// <summary>
    /// Refreshes the UI to show the current sprite when laundry is being held.
    /// </summary>
    private void RefreshUIForHeldLaundry()
    {
        // Find the player controller to check which hand is holding this laundry
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            // Check if this laundry is in left or right hand
            HoldableItem leftItem = player.GetHeldItem(HandSlot.Left);
            HoldableItem rightItem = player.GetHeldItem(HandSlot.Right);
            
            if (leftItem == this || rightItem == this)
            {
                HandSlot hand = (leftItem == this) ? HandSlot.Left : HandSlot.Right;
                
                // Find UIManager and refresh the hand slot
                UIManager uiManager = FindFirstObjectByType<UIManager>();
                if (uiManager != null)
                {
                    uiManager.RefreshHandSlot(hand, this);
                }
            }
        }
    }
}
