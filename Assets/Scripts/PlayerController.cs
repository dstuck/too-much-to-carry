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

            Vector3 xMovement = new Vector3(movement.x, 0, 0);
            Vector3 newPosition = transform.position + xMovement;
            
            // Check if movement would collide with a wall or other blocking object
            if (CanMoveInDirection(xMovement.normalized, xMovement.magnitude))
            {
                transform.position = newPosition;
            }
            Vector3 yMovement = new Vector3(0, movement.y, 0);
            newPosition = transform.position + yMovement;
            
            // Check if movement would collide with a wall or other blocking object
            if (CanMoveInDirection(yMovement.normalized, yMovement.magnitude))
            {
                transform.position = newPosition;
            }

            // Update facing direction based on movement
            facingDirection = moveInput.normalized;
            lastMoveInput = moveInput;
        }
    }

    /// <summary>
    /// Checks if the player can move in the specified direction.
    /// Uses a box cast to check only the path being traversed, not adjacent tiles.
    /// </summary>
    /// <param name="direction">Normalized movement direction</param>
    /// <param name="distance">Distance to move</param>
    /// <returns>True if movement is allowed, false if blocked</returns>
    private bool CanMoveInDirection(Vector2 direction, float distance)
    {
        // Get player's collider to determine check size
        Collider2D playerCollider = GetComponent<Collider2D>();
        Vector2 boxSize = Vector2.zero;
        
        if (playerCollider != null)
        {
            // Use collider bounds to determine box size
            Bounds bounds = playerCollider.bounds;
            boxSize = new Vector2(bounds.size.x * 0.8f, bounds.size.y * 0.8f); // Slightly smaller to avoid edge cases
        }
        else
        {
            // Fallback: use a small box if no collider
            boxSize = new Vector2(0.1f, 0.1f);
        }

        // Use box cast to check only the movement path
        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter();
        filter.useTriggers = false; // Don't check trigger colliders for movement blocking

        RaycastHit2D[] hits = new RaycastHit2D[10];
        int hitCount = Physics2D.BoxCast(
            transform.position,
            boxSize,
            0f, // No rotation
            direction,
            filter,
            hits,
            distance
        );

        // Check if any hit is a blocking object
        // Since we're using non-trigger colliders, any non-trigger collider should block movement
        // (The filter already excludes triggers, so all hits here are blocking colliders)
        for (int i = 0; i < hitCount; i++)
        {
            // Skip the player itself
            if (hits[i].collider.gameObject == gameObject)
            {
                continue;
            }

            // Any non-trigger collider blocks movement
            // This relies on the collider setup: walls, cribs, changing tables should all have non-trigger colliders
            return false;
        }

        return true;
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
        Interact(HandSlot.Left);
    }

    private void OnRightInteract(InputAction.CallbackContext context)
    {
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
                continue;
            }
            
            // Check if this object has a HoldableItem component
            HoldableItem item = collider.GetComponent<HoldableItem>();
            if (item != null)
            {
                frontObject = collider.gameObject;
                foundItem = item;
                break;
            }
        }
        
        // If no HoldableItem found, check for IInteractable objects (like Hamper)
        if (frontObject == null)
        {
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D collider = allHits[i];
                
                // Skip the player itself
                if (collider.gameObject == gameObject)
                {
                    continue;
                }
                
                // Check if this object implements IInteractable
                IInteractable interactable = collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    frontObject = collider.gameObject;
                    break;
                }
            }
        }
        
        // If no collider hit, try finding HoldableItems by distance (fallback)
        if (frontObject == null)
        {
            HoldableItem[] allItems = FindObjectsByType<HoldableItem>(FindObjectsSortMode.None);
            float closestDistance = float.MaxValue;
            HoldableItem closestItem = null;
            
            foreach (var item in allItems)
            {
                if (!item.gameObject.activeInHierarchy) continue;
                
                float distance = Vector3.Distance(item.transform.position, frontTilePosition);
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
            }
        }
        
        // Check if front object implements IInteractable
        if (frontObject != null)
        {
            IInteractable interactable = frontObject.GetComponent<IInteractable>();
            if (interactable != null && interactable.CanInteractWith(heldItem))
            {
                interactable.InteractWith(heldItem);
                
                // Check if the held item was consumed (disabled) during interaction
                if (heldItem != null && !heldItem.gameObject.activeInHierarchy)
                {
                    // Clear hand reference and fire event
                    if (hand == HandSlot.Left)
                    {
                        leftHand = null;
                    }
                    else
                    {
                        rightHand = null;
                    }
                    OnHandChanged?.Invoke(hand, null);
                }
                
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
                    // Check if item is already held in the other hand
                    HoldableItem otherHandItem = GetHeldItem(hand == HandSlot.Left ? HandSlot.Right : HandSlot.Left);
                    if (otherHandItem == item)
                    {
                        // Item is already in the other hand, don't pick it up again
                        return;
                    }
                    
                    PickUpItem(item, hand);
                }
            }
        }
        else
        {
            // Item in hand - put down
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
    /// Checks if an item can be placed at the specified position.
    /// Uses the FrontTileHighlight's trigger collider to detect blocking objects.
    /// </summary>
    /// <param name="position">World position to check</param>
    /// <returns>True if placement is allowed, false if blocked</returns>
    public bool CanPlaceItemAt(Vector3 position)
    {
        // Find the FrontTileHighlight component
        FrontTileHighlight highlight = FindFirstObjectByType<FrontTileHighlight>();
        if (highlight != null)
        {
            return !highlight.IsPlacementBlocked();
        }

        // Fallback: if no highlight found, allow placement
        return true;
    }

    /// <summary>
    /// Puts down an item from the specified hand at the target position.
    /// </summary>
    private void PutDownItem(HoldableItem item, Vector3 position, HandSlot hand)
    {
        if (item == null) return;

        // Check if placement is allowed at this position
        if (!CanPlaceItemAt(position))
        {
            return;
        }
        
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
