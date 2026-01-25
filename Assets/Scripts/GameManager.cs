using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Main game state manager.
/// Handles timer, game state, metrics tracking, and game over conditions.
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>
    /// Enum representing the current game state.
    /// </summary>
    public enum GameState
    {
        Playing,
        GameOver
    }

    [Header("Game Settings")]
    [SerializeField] private float gameDuration = 120f; // 2 minutes default

    [Header("Metrics Settings")]
    [SerializeField] private int maxSteaksForScoring = 7;
    [SerializeField] private int maxLaundryForScoring = 7;

    // Game state
    private GameState currentState = GameState.Playing;
    private float timeRemaining;
    private bool isPaused = false;

    // Metrics tracking
    private float totalBabyHappinessTime = 0f; // Cumulative time babies spent not crying
    private int initialBabyCount = 0;
    private float gameStartTime = 0f;

    // Final metrics (calculated at game end)
    private float babyHappinessPercentage = 0f;
    private int steaksCooked = 0;
    private int laundryFolded = 0;

    // Events
    /// <summary>
    /// Fired when the game ends. Parameters: babyHappinessPercentage, steaksCooked, laundryFolded
    /// </summary>
    public event System.Action<float, int, int> OnGameOver;

    /// <summary>
    /// Fired when the timer updates. Parameter: timeRemaining (in seconds)
    /// </summary>
    public event System.Action<float> OnTimerUpdate;

    // Properties
    public GameState CurrentState => currentState;
    public float TimeRemaining => timeRemaining;
    public bool IsPaused => isPaused;
    public float BabyHappinessPercentage => babyHappinessPercentage;
    public int SteaksCooked => steaksCooked;
    public int LaundryFolded => laundryFolded;

    private void Awake()
    {
        // Check for duplicate GameManagers
        GameManager[] allManagers = FindObjectsByType<GameManager>(FindObjectsSortMode.None);
        if (allManagers.Length > 1)
        {
            Debug.LogError($"[GameManager] WARNING: Found {allManagers.Length} GameManager instances in the scene! There should only be ONE. This will cause conflicts. Please remove the duplicate(s).");
            foreach (var mgr in allManagers)
            {
                Debug.LogError($"[GameManager] Found GameManager on: {mgr.gameObject.name}");
            }
        }

        // Initialize timer - will be properly set in StartGame()
        timeRemaining = gameDuration;
    }

    private void Start()
    {
        // Count initial number of babies
        Baby[] babies = FindObjectsByType<Baby>(FindObjectsSortMode.None);
        initialBabyCount = babies.Length;
        
        // Start the game
        StartGame();
    }

    private void Update()
    {
        if (currentState != GameState.Playing || isPaused)
        {
            return;
        }

        // Update timer
        timeRemaining -= Time.deltaTime;
        
        // Clamp to 0
        if (timeRemaining < 0f)
        {
            timeRemaining = 0f;
        }

        // Fire timer update event
        OnTimerUpdate?.Invoke(timeRemaining);

        // Track baby happiness
        TrackBabyHappiness();

        // Check if game should end
        if (timeRemaining <= 0f)
        {
            EndGame();
        }
    }

    /// <summary>
    /// Starts the game.
    /// </summary>
    private void StartGame()
    {
        currentState = GameState.Playing;
        isPaused = false;
        timeRemaining = gameDuration;
        gameStartTime = Time.time;
        totalBabyHappinessTime = 0f;
        Time.timeScale = 1f;
        
        Debug.Log($"[GameManager] Game started! Duration: {gameDuration} seconds, Time remaining: {timeRemaining}");
    }

    /// <summary>
    /// Ends the game and calculates final metrics.
    /// </summary>
    private void EndGame()
    {
        if (currentState == GameState.GameOver)
        {
            return; // Already ended
        }

        currentState = GameState.GameOver;

        // Calculate final metrics
        CalculateFinalMetrics();

        // Pause the game
        PauseGame();

        // Fire game over event
        int subscriberCount = OnGameOver?.GetInvocationList().Length ?? 0;
        Debug.Log($"[GameManager] Game Over! Baby: {babyHappinessPercentage:F1}%, Steaks: {steaksCooked}, Laundry: {laundryFolded}. Invoking OnGameOver event. Subscribers: {subscriberCount}");
        
        if (subscriberCount == 0)
        {
            Debug.LogError("[GameManager] WARNING: No subscribers to OnGameOver event! GameOverScreen component may not be set up correctly. Make sure GameOverScreen component is on an ACTIVE GameObject (like Canvas), not on the inactive GameOverPanel.");
        }
        
        OnGameOver?.Invoke(babyHappinessPercentage, steaksCooked, laundryFolded);
    }

    /// <summary>
    /// Calculates final metrics at game end.
    /// </summary>
    private void CalculateFinalMetrics()
    {
        // Calculate baby happiness percentage
        float gameDuration = Time.time - gameStartTime;
        if (initialBabyCount > 0 && gameDuration > 0f)
        {
            // Maximum possible happiness time = number of babies * game duration
            float maxHappinessTime = initialBabyCount * gameDuration;
            babyHappinessPercentage = maxHappinessTime > 0f 
                ? (totalBabyHappinessTime / maxHappinessTime) * 100f 
                : 0f;
        }
        else
        {
            babyHappinessPercentage = 0f;
        }

        // Query steaks cooked from all refrigerators (only cooked, not burnt)
        Refrigerator[] refrigerators = FindObjectsByType<Refrigerator>(FindObjectsSortMode.None);
        steaksCooked = 0;
        foreach (var fridge in refrigerators)
        {
            steaksCooked += fridge.GetCookedCount();
        }

        // Query laundry folded from all hampers
        Hamper[] hampers = FindObjectsByType<Hamper>(FindObjectsSortMode.None);
        laundryFolded = 0;
        foreach (var hamper in hampers)
        {
            laundryFolded += hamper.GetStoredCount();
        }
    }

    /// <summary>
    /// Tracks baby happiness by accumulating time when babies are not crying.
    /// </summary>
    private void TrackBabyHappiness()
    {
        Baby[] babies = FindObjectsByType<Baby>(FindObjectsSortMode.None);
        
        foreach (var baby in babies)
        {
            // If baby is not crying, add delta time to happiness accumulator
            if (!baby.IsCrying)
            {
                totalBabyHappinessTime += Time.deltaTime;
            }
        }
    }

    /// <summary>
    /// Pauses the game.
    /// </summary>
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Unpauses the game.
    /// </summary>
    public void UnpauseGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Restarts the game by reloading the current scene.
    /// </summary>
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Gets normalized values for bar chart display (0-1 range).
    /// </summary>
    public (float babyHappiness, float steaks, float laundry) GetNormalizedMetrics()
    {
        float normalizedSteaks = maxSteaksForScoring > 0 
            ? Mathf.Clamp01((float)steaksCooked / maxSteaksForScoring) 
            : 0f;
        
        float normalizedLaundry = maxLaundryForScoring > 0 
            ? Mathf.Clamp01((float)laundryFolded / maxLaundryForScoring) 
            : 0f;

        float normalizedBabyHappiness = babyHappinessPercentage / 100f;

        return (normalizedBabyHappiness, normalizedSteaks, normalizedLaundry);
    }
}
