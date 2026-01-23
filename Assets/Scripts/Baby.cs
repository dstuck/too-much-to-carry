using UnityEngine;

/// <summary>
/// Baby-specific implementation of HoldableItem.
/// Tracks baby emotional state (loneliness, hunger, diaper) and handles crying behavior.
/// </summary>
public class Baby : HoldableItem
{
    /// <summary>
    /// Enum representing where the baby is currently located.
    /// </summary>
    public enum BabyLocation
    {
        Held,
        OnGround,
        InCrib
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
    [SerializeField] private float crawlSpeed = 0.3f; // Slower than player
    [SerializeField] private float turnRate = 30f; // Degrees per second
    [SerializeField] private float turnChangeInterval = 2f; // How often to change direction (seconds)

    // Hidden metrics (only loneliness used for v0.2)
    private float hunger = 0f;
    private float diaper = 0f;
    private float loneliness = 0f;

    // State tracking
    private BabyLocation currentLocation = BabyLocation.OnGround;
    private bool isCrying = false;
    private AudioSource audioSource;

    // Crawling state
    private Vector2 crawlDirection; // Current crawling direction
    private float timeSinceLastTurn = 0f;

    /// <summary>
    /// Calculates the current anger/mad level from all metrics.
    /// For v0.2, this equals loneliness. In the future, can combine hunger, diaper, etc.
    /// </summary>
    private float CalculateAnger()
    {
        // For v0.2, anger is just loneliness
        // Future: return Mathf.Max(loneliness, hunger, diaper) or some combination
        return loneliness;
    }

    /// <summary>
    /// Gets the current "mad" state (visible metric). This is the calculated anger level.
    /// </summary>
    public float Mad => CalculateAnger();

    /// <summary>
    /// Gets the current location of the baby.
    /// </summary>
    public BabyLocation Location => currentLocation;

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

        // Initialize crawl direction randomly
        float randomAngle = Random.Range(0f, 360f);
        crawlDirection = new Vector2(Mathf.Cos(randomAngle * Mathf.Deg2Rad), Mathf.Sin(randomAngle * Mathf.Deg2Rad));
    }

    private void Update()
    {
        // Only update loneliness and crawling if baby is not held
        if (currentLocation != BabyLocation.Held)
        {
            UpdateLoneliness();
            CheckCryingThreshold();
            
            // Only crawl if on ground (not in crib)
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
                rate = lonelinessRateInCrib;
                break;
            case BabyLocation.Held:
                // Shouldn't reach here, but just in case
                return;
        }

        loneliness += rate * Time.deltaTime;
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
    /// </summary>
    public override void OnPickedUp()
    {
        base.OnPickedUp();
        SetLocation(BabyLocation.Held);
    }

    /// <summary>
    /// Called when the baby is put down. Detects crib placement and sets location accordingly.
    /// </summary>
    /// <param name="position">World position to place the baby</param>
    public override void OnPutDown(Vector3 position)
    {
        base.OnPutDown(position);

        // Check if position overlaps with a crib
        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter();
        filter.useTriggers = false; // Cribs use non-trigger colliders

        Collider2D[] hits = new Collider2D[10];
        int hitCount = Physics2D.OverlapPoint(position, filter, hits);

        bool isInCrib = false;
        for (int i = 0; i < hitCount; i++)
        {
            if (hits[i].gameObject.CompareTag("Crib"))
            {
                isInCrib = true;
                break;
            }
        }

        // Set location based on crib detection
        SetLocation(isInCrib ? BabyLocation.InCrib : BabyLocation.OnGround);

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
        }

        return true;
    }
}
