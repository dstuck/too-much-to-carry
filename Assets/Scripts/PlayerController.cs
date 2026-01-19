using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

/// <summary>
/// Handles all player functionality: movement, hands/inventory, and interactions.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1f;
    
    [Header("Grid Reference")]
    [SerializeField] private Grid grid;

    // Input System
    private InputSystem_Actions inputActions;
    private Vector2 moveInput;
    private Vector2 lastMoveInput;
    
    // Hands/Inventory
    private HoldableItem leftHand;
    private HoldableItem rightHand;
    
    // Movement state
    private Vector2 facingDirection = Vector2.down; // Default facing down
    
    // Events
    /// <summary>
    /// Fired when a hand slot changes (item picked up or put down).
    /// Parameters: HandSlot, HoldableItem (null if hand is now empty)
    /// </summary>
    public event System.Action<HandSlot, HoldableItem> OnHandChanged;
    
    private void Awake()
    {
        inputActions = new InputSystem_Actions();
        
        // Find Grid if not assigned
        if (grid == null)
        {
            grid = FindFirstObjectByType<Grid>();
        }
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMoveCanceled;
        inputActions.Player.Left.performed += OnLeftInteract;
        inputActions.Player.Right.performed += OnRightInteract;
    }

    private void OnDisable()
    {
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMoveCanceled;
        inputActions.Player.Left.performed -= OnLeftInteract;
        inputActions.Player.Right.performed -= OnRightInteract;
        inputActions.Disable();
    }

    private void Update()
    {
        // Read movement input continuously from the action
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        
        // Continuous movement
        if (moveInput.magnitude > 0.1f)
        {
            Vector3 movement = new Vector3(moveInput.x, moveInput.y, 0) * moveSpeed * Time.deltaTime;
            transform.position += movement;
            
            // Update facing direction based on movement
            facingDirection = moveInput.normalized;
            lastMoveInput = moveInput;
        }
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero;
    }

    private void OnLeftInteract(InputAction.CallbackContext context)
    {
        Debug.Log("Left interact pressed");
        Interact(HandSlot.Left);
    }

    private void OnRightInteract(InputAction.CallbackContext context)
    {
        Debug.Log("Right interact pressed");
        Interact(HandSlot.Right);
    }

    /// <summary>
    /// Unified interaction method that handles all interaction cases.
    /// </summary>
    /// <param name="hand">Which hand to use for the interaction</param>
    public void Interact(HandSlot hand)
    {
        HoldableItem heldItem = GetHeldItem(hand);
        Vector3 frontTilePosition = GetFrontTilePosition();
        
        Debug.Log($"Interact called - Hand: {hand}, Held item: {(heldItem != null ? heldItem.name : "null")}, Front tile: {frontTilePosition}");
        
        // Detect what's at the front tile
        // Use ContactFilter2D to include trigger colliders (babies use triggers)
        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter(); // Include all layers
        filter.useTriggers = true; // Include trigger colliders
        
        Collider2D[] allHits = new Collider2D[10];
        int hitCount = Physics2D.OverlapPoint(frontTilePosition, filter, allHits);
        
        GameObject frontObject = null;
        HoldableItem foundItem = null;
        
        // Check all colliders found at the front tile
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D collider = allHits[i];
            
            // Skip the player itself
            if (collider.gameObject == gameObject)
            {
                Debug.Log($"Skipping player collider: {collider.gameObject.name}");
                continue;
            }
            
            // Check if this object has a HoldableItem component
            HoldableItem item = collider.GetComponent<HoldableItem>();
            if (item != null)
            {
                frontObject = collider.gameObject;
                foundItem = item;
                Debug.Log($"Found HoldableItem: {item.name} at front tile");
                break;
            }
        }
        
        // If no collider hit, try finding HoldableItems by distance (fallback)
        if (frontObject == null)
        {
            Debug.Log("No collider found, trying distance-based detection...");
            HoldableItem[] allItems = FindObjectsByType<HoldableItem>(FindObjectsSortMode.None);
            float closestDistance = float.MaxValue;
            HoldableItem closestItem = null;
            
            foreach (var item in allItems)
            {
                if (!item.gameObject.activeInHierarchy) continue;
                
                float distance = Vector3.Distance(item.transform.position, frontTilePosition);
                Debug.Log($"Checking {item.name} at distance {distance}");
                if (distance < 0.2f && distance < closestDistance) // Within ~1.25 cells (more forgiving)
                {
                    closestDistance = distance;
                    closestItem = item;
                }
            }
            
            if (closestItem != null)
            {
                frontObject = closestItem.gameObject;
                foundItem = closestItem;
                Debug.Log($"Found closest item by distance: {closestItem.name} at {closestDistance}");
            }
        }
        
        // Check if front object implements IInteractable
        if (frontObject != null)
        {
            IInteractable interactable = frontObject.GetComponent<IInteractable>();
            if (interactable != null && interactable.CanInteractWith(heldItem))
            {
                interactable.InteractWith(heldItem);
                return;
            }
        }
        
        // Default behavior: pickup/putdown
        if (heldItem == null)
        {
            // Empty hand - try to pick up item
            if (frontObject != null)
            {
                HoldableItem item = frontObject.GetComponent<HoldableItem>();
                if (item != null)
                {
                    Debug.Log($"Picking up {item.name} with {hand} hand");
                    PickUpItem(item, hand);
                }
                else
                {
                    Debug.Log($"Front object {frontObject.name} doesn't have HoldableItem component");
                }
            }
            else
            {
                Debug.Log("No object found at front tile");
            }
        }
        else
        {
            // Item in hand - put down
            Debug.Log($"Putting down {heldItem.name} from {hand} hand");
            PutDownItem(heldItem, frontTilePosition, hand);
        }
    }

    /// <summary>
    /// Gets the world position of the tile in front of the player.
    /// </summary>
    private Vector3 GetFrontTilePosition()
    {
        if (grid == null)
        {
            Debug.LogWarning("Grid not found! Using player position + facing direction.");
            return transform.position + (Vector3)(facingDirection * 0.16f);
        }
        
        // Get current grid cell
        Vector3Int currentCell = grid.WorldToCell(transform.position);
        
        // Calculate front cell based on facing direction
        Vector3Int frontCell = currentCell;
        if (Mathf.Abs(facingDirection.x) > Mathf.Abs(facingDirection.y))
        {
            // Moving horizontally
            frontCell.x += facingDirection.x > 0 ? 1 : -1;
        }
        else
        {
            // Moving vertically
            frontCell.y += facingDirection.y > 0 ? 1 : -1;
        }
        
        // Convert back to world position (cell center)
        return grid.GetCellCenterWorld(frontCell);
    }

    /// <summary>
    /// Picks up an item and stores it in the specified hand.
    /// </summary>
    private void PickUpItem(HoldableItem item, HandSlot hand)
    {
        if (item == null) return;
        
        // Store reference
        if (hand == HandSlot.Left)
        {
            leftHand = item;
        }
        else
        {
            rightHand = item;
        }
        
        // Disable GameObject
        item.OnPickedUp();
        
        // Fire event for hand change
        OnHandChanged?.Invoke(hand, item);
    }

    /// <summary>
    /// Puts down an item from the specified hand at the target position.
    /// </summary>
    private void PutDownItem(HoldableItem item, Vector3 position, HandSlot hand)
    {
        if (item == null) return;
        
        // Enable GameObject at position
        item.OnPutDown(position);
        
        // Clear hand reference
        if (hand == HandSlot.Left)
        {
            leftHand = null;
        }
        else
        {
            rightHand = null;
        }
        
        // Fire event for hand change (now empty)
        OnHandChanged?.Invoke(hand, null);
    }

    /// <summary>
    /// Gets the item currently held in the specified hand.
    /// </summary>
    public HoldableItem GetHeldItem(HandSlot hand)
    {
        return hand == HandSlot.Left ? leftHand : rightHand;
    }
    
    /// <summary>
    /// Gets the current facing direction of the player.
    /// </summary>
    public Vector2 GetFacingDirection()
    {
        return facingDirection;
    }
    
    /// <summary>
    /// Gets the world position of the tile in front of the player.
    /// </summary>
    public Vector3 GetFrontTileWorldPosition()
    {
        return GetFrontTilePosition();
    }
}

/// <summary>
/// Enum for identifying which hand slot to use.
/// </summary>
public enum HandSlot
{
    Left,
    Right
}
