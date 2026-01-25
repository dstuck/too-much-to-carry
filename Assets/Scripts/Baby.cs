using UnityEngine;

/// <summary>
/// Baby-specific implementation of HoldableItem.
/// Tracks baby emotional state (loneliness, hunger, diaper) and handles crying behavior.
/// </summary>
public class Baby : HoldableItem, IInteractable
{
    /// <summary>
    /// Enum representing where the baby is currently located.
    /// </summary>
    public enum BabyLocation
    {
        Held,
        OnGround,
        InCrib,
        OnChangingTable
    }

    [Header("Loneliness Settings")]
    [SerializeField] private float lonelinessRateOnGround = 1.0f; // per second
    [SerializeField] private float lonelinessRateInCrib = 0.5f; // per second (half rate)
    
    [Header("Crying Settings")]
    [SerializeField] private float cryingThreshold = 3.0f; // anger level that triggers crying

    [Header("Sprites")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite cryingSprite;

    [Header("Audio")]
    [SerializeField] private AudioClip[] cryClips;

    [Header("Crawling Settings")]
    [SerializeField] private float crawlSpeed = 0.05f; // Slower than player
    [SerializeField] private float turnRate = 50f; // Degrees per second
    [SerializeField] private float turnChangeInterval = 1f; // How often to change direction (seconds)

    [Header("Diaper Settings")]
    [SerializeField] private float pooTimeMin = 20f; // Minimum time before poo (seconds)
    [SerializeField] private float pooTimeMax = 60f; // Maximum time before poo (seconds)
    private const float dirtyDiaperAngerContribution = 8f; // Anger added when diaper is dirty

    // Hidden metrics (only loneliness used for v0.2)
    private float hunger = 0f;
    private float diaper = 0f;
    private float loneliness = 0f;

    // Diaper state
    private bool isDirty = false;
    private float timeSinceLastChange = 0f;
    private float nextPooTime;
    
    // Components to disable when held (but keep GameObject active for updates)
    private Collider2D babyCollider;

    // State tracking
    private BabyLocation currentLocation = BabyLocation.OnGround;
    private bool isCrying = false;
    private AudioSource audioSource;

    // Crawling state
    private Vector2 crawlDirection; // Current crawling direction
    private float timeSinceLastTurn = 0f;

    /// <summary>
    /// Calculates the current anger/mad level from all metrics.
    /// Combines loneliness and dirty diaper state.
    /// </summary>
    private float CalculateAnger()
    {
        float anger = loneliness;
        
        // Add dirty diaper contribution
        if (isDirty)
        {
            anger += dirtyDiaperAngerContribution;
        }
        
        return anger;
    }

    /// <summary>
    /// Gets the current "mad" state (visible metric). This is the calculated anger level.
    /// </summary>
    public float Mad => CalculateAnger();

    /// <summary>
    /// Gets the current location of the baby.
    /// </summary>
    public BabyLocation Location => currentLocation;
    
    /// <summary>
    /// Gets whether the baby's diaper is dirty.
    /// </summary>
    public bool IsDirty => isDirty;

    /// <summary>
    /// Gets whether the baby is currently crying.
    /// </summary>
    public bool IsCrying => isCrying;

    private void Awake()
    {
        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.loop = true; // Will be used for looping cry sounds

        // Get SpriteRenderer
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // Store normal sprite if not set
        if (normalSprite == null && spriteRenderer.sprite != null)
        {
            normalSprite = spriteRenderer.sprite;
        }
        
        // Get Collider2D
        babyCollider = GetComponent<Collider2D>();

        // Initialize crawl direction randomly
        float randomAngle = Random.Range(0f, 360f);
        crawlDirection = new Vector2(Mathf.Cos(randomAngle * Mathf.Deg2Rad), Mathf.Sin(randomAngle * Mathf.Deg2Rad));

        // Initialize poo timer with random time
        nextPooTime = Random.Range(pooTimeMin, pooTimeMax);
        timeSinceLastChange = 0f;
        Debug.Log($"[Baby {gameObject.name}] Initialized next poo time: {nextPooTime:F2} seconds");
    }

    private void Update()
    {
        // Always update diaper state and check crying (even when held)
        UpdateDiaperState();
        CheckCryingThreshold();
        
        // Only update loneliness and crawling if baby is not held
        if (currentLocation != BabyLocation.Held)
        {
            UpdateLoneliness();
            
            // Only crawl if on ground (not in crib or changing table)
            if (currentLocation == BabyLocation.OnGround)
            {
                UpdateCrawling();
            }
        }
    }

    /// <summary>
    /// Updates loneliness based on current location.
    /// </summary>
    private void UpdateLoneliness()
    {
        float rate = 0f;
        
        switch (currentLocation)
        {
            case BabyLocation.OnGround:
                rate = lonelinessRateOnGround;
                break;
            case BabyLocation.InCrib:
            case BabyLocation.OnChangingTable:
                rate = lonelinessRateInCrib; // Same rate for crib and changing table
                break;
            case BabyLocation.Held:
                // Shouldn't reach here, but just in case
                return;
        }

        loneliness += rate * Time.deltaTime;
    }

    /// <summary>
    /// Updates diaper state: tracks time since last change and triggers poo event.
    /// </summary>
    private void UpdateDiaperState()
    {
        // Only update timer if not already dirty
        if (!isDirty)
        {
            timeSinceLastChange += Time.deltaTime;
            
            // Check if it's time to poo
            if (timeSinceLastChange >= nextPooTime)
            {
                isDirty = true;
                timeSinceLastChange = 0f;
                Debug.Log($"[Baby {gameObject.name}] POO! Diaper is now dirty. Time since last change: {nextPooTime:F2} seconds");
                // Reset timer for next poo (will be reset when changed)
            }
            else
            {
                // Debug every 10 seconds to track progress
                if (Mathf.FloorToInt(timeSinceLastChange) % 10 == 0 && Mathf.FloorToInt((timeSinceLastChange - Time.deltaTime)) % 10 != 0)
                {
                    Debug.Log($"[Baby {gameObject.name}] Poo timer: {timeSinceLastChange:F1}s / {nextPooTime:F1}s ({(timeSinceLastChange / nextPooTime * 100f):F1}%)");
                }
            }
        }
    }

    /// <summary>
    /// Checks if anger exceeds threshold and starts/stops crying accordingly.
    /// </summary>
    private void CheckCryingThreshold()
    {
        float anger = CalculateAnger();
        if (anger >= cryingThreshold && !isCrying)
        {
            StartCrying();
        }
        else if (anger < cryingThreshold && isCrying)
        {
            StopCrying();
        }
    }

    /// <summary>
    /// Starts the crying state: changes sprite and plays looping audio.
    /// </summary>
    private void StartCrying()
    {
        if (isCrying) return;

        isCrying = true;

        // Change sprite to crying
        if (cryingSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = cryingSprite;
        }

        // Start looping cry audio
        if (cryClips != null && cryClips.Length > 0 && audioSource != null)
        {
            AudioClip selectedClip = cryClips[Random.Range(0, cryClips.Length)];
            audioSource.clip = selectedClip;
            audioSource.loop = true;
            audioSource.Play();
        }
        
        // Notify UI to update if baby is being held
        if (currentLocation == BabyLocation.Held)
        {
            RefreshUIForHeldBaby();
        }
    }

    /// <summary>
    /// Stops the crying state: changes sprite back and stops audio.
    /// </summary>
    private void StopCrying()
    {
        if (!isCrying) return;

        isCrying = false;

        // Change sprite back to normal
        if (normalSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = normalSprite;
        }

        // Stop audio and clear clip to prevent it from resuming
        if (audioSource != null)
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            audioSource.clip = null; // Clear the clip to prevent it from resuming
        }
        
        // Notify UI to update if baby is being held
        if (currentLocation == BabyLocation.Held)
        {
            RefreshUIForHeldBaby();
        }
    }

    /// <summary>
    /// Sets the baby's location and handles state changes.
    /// </summary>
    /// <param name="location">The new location</param>
    public void SetLocation(BabyLocation location)
    {
        currentLocation = location;

        // If held, reset loneliness immediately
        if (location == BabyLocation.Held)
        {
            loneliness = 0f;
            StopCrying();
        }
    }

    /// <summary>
    /// Called when the baby is picked up. Sets location to Held.
    /// Keeps GameObject active so Update() continues to run for poo timer and crying.
    /// </summary>
    public override void OnPickedUp()
    {
        Debug.Log($"[Baby {gameObject.name}] Picked up. Poo timer: {timeSinceLastChange:F2}s / {nextPooTime:F2}s");
        
        // Don't call base.OnPickedUp() - we want to keep the GameObject active
        // Instead, hide the sprite and disable the collider
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
        
        if (babyCollider != null)
        {
            babyCollider.enabled = false;
        }
        
        SetLocation(BabyLocation.Held);
    }

    /// <summary>
    /// Called when the baby is put down. Detects crib placement and sets location accordingly.
    /// </summary>
    /// <param name="position">World position to place the baby</param>
    public override void OnPutDown(Vector3 position)
    {
        // Re-enable sprite and collider (GameObject was kept active)
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
        
        if (babyCollider != null)
        {
            babyCollider.enabled = true;
        }
        
        // Don't call base.OnPutDown() since we didn't disable the GameObject
        transform.position = position;
        
        // Note: gameObject.SetActive(true) is not needed since it was never disabled

        // Check if position overlaps with a crib or changing table (both use "Crib" tag)
        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter();
        filter.useTriggers = false; // Cribs use non-trigger colliders

        Collider2D[] hits = new Collider2D[10];
        int hitCount = Physics2D.OverlapPoint(position, filter, hits);

        bool isOnCribObject = false;
        bool isChangingTable = false;
        
        for (int i = 0; i < hitCount; i++)
        {
            if (hits[i].gameObject.CompareTag("Crib"))
            {
                isOnCribObject = true;
                // Differentiate changing table from crib by GameObject name
                // Changing tables should be named "ChangingTable" or contain "Changing" in the name
                if (hits[i].gameObject.name.Contains("Changing") || hits[i].gameObject.name.Contains("ChangingTable"))
                {
                    isChangingTable = true;
                }
                break;
            }
        }

        // Set location based on detection
        if (isOnCribObject)
        {
            SetLocation(isChangingTable ? BabyLocation.OnChangingTable : BabyLocation.InCrib);
        }
        else
        {
            SetLocation(BabyLocation.OnGround);
        }

        // Ensure crying state matches current anger level after being put down
        // This prevents audio from resuming if it was playing when GameObject was disabled
        float anger = CalculateAnger();
        if (anger < cryingThreshold && isCrying)
        {
            StopCrying();
        }
        else if (anger >= cryingThreshold && !isCrying)
        {
            StartCrying();
        }
    }

    /// <summary>
    /// Updates crawling behavior: moves baby slowly and turns over time.
    /// </summary>
    private void UpdateCrawling()
    {
        // Update turning over time
        timeSinceLastTurn += Time.deltaTime;
        if (timeSinceLastTurn >= turnChangeInterval)
        {
            // Randomly change direction slightly
            float randomAngle = Random.Range(-45f, 45f);
            crawlDirection = Quaternion.Euler(0, 0, randomAngle) * crawlDirection;
            crawlDirection.Normalize();
            timeSinceLastTurn = 0f;
        }
        else
        {
            // Gradually turn a little bit
            float turnAmount = turnRate * Time.deltaTime;
            float randomTurn = Random.Range(-turnAmount, turnAmount);
            crawlDirection = Quaternion.Euler(0, 0, randomTurn) * crawlDirection;
            crawlDirection.Normalize();
        }

        // Calculate movement
        Vector3 movement = new Vector3(crawlDirection.x, crawlDirection.y, 0) * crawlSpeed * Time.deltaTime;
        Vector3 newPosition = transform.position + movement;

        // Check if movement is blocked by walls
        if (CanCrawlTo(newPosition))
        {
            transform.position = newPosition;
        }
        else
        {
            // If blocked, turn away from the obstacle
            crawlDirection = -crawlDirection;
            crawlDirection.Normalize();
        }
    }

    /// <summary>
    /// Checks if the baby can crawl to the specified position.
    /// Returns false if there's a wall blocking movement.
    /// </summary>
    /// <param name="position">Target position to check</param>
    /// <returns>True if movement is allowed, false if blocked</returns>
    private bool CanCrawlTo(Vector3 position)
    {
        // Get baby's collider to determine check size
        Collider2D babyCollider = GetComponent<Collider2D>();
        Vector2 boxSize = Vector2.zero;

        if (babyCollider != null)
        {
            Bounds bounds = babyCollider.bounds;
            boxSize = new Vector2(bounds.size.x * 0.8f, bounds.size.y * 0.8f);
        }
        else
        {
            boxSize = new Vector2(0.1f, 0.1f);
        }

        // Use box cast to check for walls
        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter();
        filter.useTriggers = false; // Don't check trigger colliders for movement blocking

        Vector2 direction = (position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, position);

        RaycastHit2D[] hits = new RaycastHit2D[10];
        int hitCount = Physics2D.BoxCast(
            transform.position,
            boxSize,
            0f,
            direction,
            filter,
            hits,
            distance
        );

        // Check if any hit is a blocking object (walls with BlocksPlacement tag)
        for (int i = 0; i < hitCount; i++)
        {
            // Skip the baby itself
            if (hits[i].collider.gameObject == gameObject)
            {
                continue;
            }

            // Skip the player (babies can pass through player)
            if (hits[i].collider.GetComponent<PlayerController>() != null)
            {
                continue;
            }

            // Check if this is a wall or other blocking object
            if (hits[i].collider.gameObject.CompareTag("BlocksPlacement"))
            {
                return false;
            }

            // Check if this is a crib (babies shouldn't crawl into cribs, only be placed there)
            if (hits[i].collider.gameObject.CompareTag("Crib"))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if this baby can interact with the given held item.
    /// </summary>
    /// <param name="heldItem">The item currently held by the player (null if hand is empty)</param>
    /// <returns>True if this baby can interact with the held item</returns>
    public bool CanInteractWith(HoldableItem heldItem)
    {
        // Can interact if holding a diaper (regardless of dirty state - allows resetting timer)
        if (heldItem != null && heldItem is Diaper)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Performs the interaction with the given held item.
    /// Changes the baby's diaper if a clean diaper is held.
    /// </summary>
    /// <param name="heldItem">The item currently held by the player (should be a Diaper)</param>
    public void InteractWith(HoldableItem heldItem)
    {
        if (CanInteractWith(heldItem) && heldItem is Diaper)
        {
            // Clean the baby (if dirty) and reset timer
            bool wasDirty = isDirty;
            isDirty = false;
            timeSinceLastChange = 0f;
            nextPooTime = Random.Range(pooTimeMin, pooTimeMax);
            Debug.Log($"[Baby {gameObject.name}] Diaper changed! {(wasDirty ? "Was dirty, now clean." : "Was already clean.")} Next poo time set to: {nextPooTime:F2} seconds");
            
            // Consume the diaper - disable it
            heldItem.gameObject.SetActive(false);
            
            // The PlayerController will handle clearing the hand reference
        }
    }
    
    /// <summary>
    /// Refreshes the UI to show the current sprite when baby is being held.
    /// </summary>
    private void RefreshUIForHeldBaby()
    {
        // Find the player controller to check which hand is holding this baby
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            // Check if this baby is in left or right hand
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
