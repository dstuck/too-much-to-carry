using UnityEngine;

/// <summary>
/// Raw meat item that can be placed on a stove to cook.
/// Tracks cooking state and transitions through Raw → Cooking → Cooked → Burnt with color tinting.
/// </summary>
public class RawMeat : HoldableItem
{
    /// <summary>
    /// Enum representing the current cooking state of the meat.
    /// </summary>
    public enum MeatState
    {
        Raw,
        Cooking,
        Cooked,
        Burnt
    }

    [Header("Cooking Settings")]
    [SerializeField] private float cookTimeMin = 7f; // Minimum time to cook (seconds)
    [SerializeField] private float cookTimeMax = 12f; // Maximum time to cook (seconds)
    [SerializeField] private float burnTime = 8f; // Time after cooked before burning (seconds)

    [Header("Colors")]
    [SerializeField] private Color rawColor = Color.red;
    [SerializeField] private Color cookedColor = new Color(0.6f, 0.4f, 0.2f); // Brown
    [SerializeField] private Color burntColor = Color.black;

    // Cooking state
    private MeatState currentState = MeatState.Raw;
    private float cookTimer = 0f; // Single timer that tracks total cooking time
    private float targetCookTime; // Set once in Awake, persists across pickups
    private bool isOnStove = false;
    private float timeWhenPickedUp = 0f; // Track when picked up to resume cooking when put down
    private bool wasOnStoveWhenPickedUp = false; // Track if meat was cooking when picked up

    // Components to disable when held (but keep GameObject active for updates)
    private Collider2D meatCollider;

    /// <summary>
    /// Gets the current cooking state of the meat.
    /// </summary>
    public MeatState State => currentState;

    private void Awake()
    {
        // Get SpriteRenderer
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Get Collider2D
        meatCollider = GetComponent<Collider2D>();

        // Set target cook time once - this is fixed for this meat instance
        targetCookTime = Random.Range(cookTimeMin, cookTimeMax);
        Debug.Log($"[RawMeat {gameObject.name}] Initialized with targetCookTime: {targetCookTime:F2}s (range: {cookTimeMin}-{cookTimeMax}s), burnTime: {burnTime}s");
        
        // Initialize color to raw (red)
        if (spriteRenderer != null)
        {
            spriteRenderer.color = rawColor;
        }
    }

    private void Update()
    {
        // Only update cooking if on stove
        if (isOnStove)
        {
            UpdateCooking();
            // Color is updated in UpdateCooking() when state changes
        }
    }

    /// <summary>
    /// Updates cooking timer and state transitions.
    /// </summary>
    private void UpdateCooking()
    {
        // Increment cooking timer
        cookTimer += Time.deltaTime;

        MeatState previousState = currentState;

        // Check state transitions based on single timer
        if (cookTimer >= targetCookTime + burnTime)
        {
            // Burnt
            if (currentState != MeatState.Burnt)
            {
                currentState = MeatState.Burnt;
            }
        }
        else if (cookTimer >= targetCookTime)
        {
            // Cooked
            if (currentState != MeatState.Cooked)
            {
                currentState = MeatState.Cooked;
            }
        }
        else if (cookTimer > 0f)
        {
            // Cooking (transitioning from raw)
            if (currentState == MeatState.Raw)
            {
                currentState = MeatState.Cooking;
            }
        }

        // If state changed, update color and UI
        if (previousState != currentState)
        {
            Debug.Log($"[RawMeat {gameObject.name}] State changed: {previousState} -> {currentState} at cookTimer: {cookTimer:F2}s (targetCookTime: {targetCookTime:F2}s, burnTime: {burnTime}s)");
            UpdateColor();
            RefreshUIForHeldMeat();
        }
    }

    /// <summary>
    /// Updates the sprite color based on current state (discrete jumps, not continuous).
    /// </summary>
    private void UpdateColor()
    {
        if (spriteRenderer == null) return;

        Color targetColor;

        // Set color based on state - discrete jumps, no interpolation
        switch (currentState)
        {
            case MeatState.Burnt:
                targetColor = burntColor;
                break;
            case MeatState.Cooked:
                targetColor = cookedColor;
                break;
            case MeatState.Cooking:
                // Still red while cooking (will jump to brown when cooked)
                targetColor = rawColor;
                break;
            case MeatState.Raw:
            default:
                targetColor = rawColor;
                break;
        }

        spriteRenderer.color = targetColor;
    }

    /// <summary>
    /// Called when the meat is picked up. Keeps GameObject active for cooking updates.
    /// </summary>
    public override void OnPickedUp()
    {
        // Store current time and stove state before hiding
        timeWhenPickedUp = Time.time;
        wasOnStoveWhenPickedUp = isOnStove;

        // Don't call base.OnPickedUp() - we want to keep the GameObject active
        // Instead, hide the sprite and disable the collider
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        if (meatCollider != null)
        {
            meatCollider.enabled = false;
        }

        // Stop cooking when picked up (timer pauses)
        isOnStove = false;
        
        // Refresh UI to show current color
        RefreshUIForHeldMeat();
    }

    /// <summary>
    /// Called when the meat is put down. Detects stove placement and resumes cooking.
    /// </summary>
    /// <param name="position">World position to place the meat</param>
    public override void OnPutDown(Vector3 position)
    {
        // Re-enable sprite and collider (GameObject was kept active)
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        if (meatCollider != null)
        {
            meatCollider.enabled = true;
        }

        // Don't call base.OnPutDown() since we didn't disable the GameObject
        transform.position = position;

        // Timer pauses when picked up - don't add time when put down
        // Reset tracking variables
        if (timeWhenPickedUp > 0f)
        {
            Debug.Log($"[RawMeat {gameObject.name}] Put down. Timer paused at: {cookTimer:F2}s / {targetCookTime:F2}s (was on stove: {wasOnStoveWhenPickedUp})");
            timeWhenPickedUp = 0f;
            wasOnStoveWhenPickedUp = false;
        }

        // Check if position overlaps with a stove
        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter();
        filter.useTriggers = false; // Stove uses non-trigger collider

        Collider2D[] hits = new Collider2D[10];
        int hitCount = Physics2D.OverlapPoint(position, filter, hits);

        bool foundStove = false;
        for (int i = 0; i < hitCount; i++)
        {
            if (hits[i].gameObject.CompareTag("Stove"))
            {
                foundStove = true;
                break;
            }
        }

        isOnStove = foundStove;
        
        if (foundStove)
        {
            Debug.Log($"[RawMeat {gameObject.name}] Placed on stove. Cook timer: {cookTimer:F2}s / {targetCookTime:F2}s, State: {currentState}");
        }

        // Update state and color immediately after placement
        MeatState previousState = currentState;
        UpdateCooking();
        
        // If state changed during UpdateCooking, color and UI were already updated
        // Otherwise, ensure color is set correctly
        if (previousState == currentState)
        {
            UpdateColor();
        }
    }

    /// <summary>
    /// Refreshes the UI to show the current sprite/color when meat is being held.
    /// </summary>
    private void RefreshUIForHeldMeat()
    {
        // Find the player controller to check which hand is holding this meat
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            // Check if this meat is in left or right hand
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
