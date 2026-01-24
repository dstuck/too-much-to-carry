using UnityEngine;

/// <summary>
/// Manages all UI elements in the game.
/// Coordinates updates to hand slots and other UI elements.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Hand Slots")]
    [SerializeField] private HandSlotUI leftHandSlot;
    [SerializeField] private HandSlotUI rightHandSlot;
    
    [Header("Player Reference")]
    [SerializeField] private PlayerController playerController;

    /// <summary>
    /// Updates the specified hand slot with the given sprite.
    /// </summary>
    /// <param name="hand">Which hand slot to update</param>
    /// <param name="sprite">Sprite to display (null to show empty)</param>
    private void UpdateHandSlot(HandSlot hand, Sprite sprite)
    {
        HandSlotUI slot = hand == HandSlot.Left ? leftHandSlot : rightHandSlot;
        if (slot != null)
        {
            slot.SetItemSprite(sprite);
        }
    }

    private void Awake()
    {
        // Find hand slots if not assigned
        if (leftHandSlot == null || rightHandSlot == null)
        {
            HandSlotUI[] slots = FindObjectsByType<HandSlotUI>(FindObjectsSortMode.None);
            foreach (var slot in slots)
            {
                if (slot.IsLeftHand() && leftHandSlot == null)
                {
                    leftHandSlot = slot;
                }
                else if (!slot.IsLeftHand() && rightHandSlot == null)
                {
                    rightHandSlot = slot;
                }
            }
        }
        
        // Find player controller if not assigned
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }
    }

    private void OnEnable()
    {
        // Subscribe to player hand change events
        if (playerController != null)
        {
            playerController.OnHandChanged += HandleHandChanged;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        if (playerController != null)
        {
            playerController.OnHandChanged -= HandleHandChanged;
        }
    }

    /// <summary>
    /// Handles hand change events from PlayerController.
    /// </summary>
    private void HandleHandChanged(HandSlot hand, HoldableItem item)
    {
        Sprite sprite = item != null ? item.GetSprite() : null;
        UpdateHandSlot(hand, sprite);
    }
    
    /// <summary>
    /// Public method to refresh a hand slot with the current sprite from a held item.
    /// Called when a held item's sprite changes (e.g., baby starts crying).
    /// </summary>
    /// <param name="hand">Which hand slot to refresh</param>
    /// <param name="item">The item being held (to get current sprite)</param>
    public void RefreshHandSlot(HandSlot hand, HoldableItem item)
    {
        Sprite sprite = item != null ? item.GetSprite() : null;
        UpdateHandSlot(hand, sprite);
    }
}
