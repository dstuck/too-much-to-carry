using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the game over screen with performance metrics displayed as bar charts.
/// Shows baby happiness, steaks cooked, and laundry folded.
/// </summary>
public class GameOverScreen : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button restartButton;

    [Header("Bar Chart References")]
    [SerializeField] private Image babyHappinessBar;
    [SerializeField] private Image steaksCookedBar;
    [SerializeField] private Image laundryFoldedBar;

    [Header("Label References (Optional)")]
    [SerializeField] private Text babyHappinessLabel;
    [SerializeField] private Text steaksCookedLabel;
    [SerializeField] private Text laundryFoldedLabel;

    [Header("Bar Chart Settings")]
    [SerializeField] private float maxBarHeight = 200f; // Maximum height of bars in pixels
    [SerializeField] private bool useFilledImage = true; // Use Image.fillAmount instead of scaling
    
    [Header("Bar Colors")]
    [SerializeField] private Color highValueColor = Color.green; // >75%
    [SerializeField] private Color mediumValueColor = Color.yellow; // 50-75%
    [SerializeField] private Color lowValueColor = Color.red; // <50%

    private GameManager gameManager;
    private RectTransform babyHappinessRect;
    private RectTransform steaksCookedRect;
    private RectTransform laundryFoldedRect;

    private void Awake()
    {
        Debug.Log("[GameOverScreen] Awake() called");
        
        // Find GameManager if not assigned
        if (gameManager == null)
        {
            GameManager[] allManagers = FindObjectsByType<GameManager>(FindObjectsSortMode.None);
            if (allManagers.Length == 0)
            {
                Debug.LogError("[GameOverScreen] GameManager not found! Make sure GameManager exists in the scene.");
            }
            else if (allManagers.Length > 1)
            {
                Debug.LogError($"[GameOverScreen] WARNING: Found {allManagers.Length} GameManager instances! Using the first one. Please remove duplicates.");
                gameManager = allManagers[0];
                Debug.Log($"[GameOverScreen] Using GameManager on: {gameManager.gameObject.name}");
            }
            else
            {
                gameManager = allManagers[0];
                Debug.Log($"[GameOverScreen] Found GameManager on: {gameManager.gameObject.name}");
            }
        }

        // Subscribe to game over event in Awake (works even if panel starts inactive)
        SubscribeToGameOverEvent();

        // Configure all bars using helper method
        ConfigureBar(babyHappinessBar, ref babyHappinessRect);
        ConfigureBar(steaksCookedBar, ref steaksCookedRect);
        ConfigureBar(laundryFoldedBar, ref laundryFoldedRect);

        // Set up restart button
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        // Initially hide the panel
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void Start()
    {
        Debug.Log("[GameOverScreen] Start() called - ensuring subscription");
        // Ensure subscription in Start as well (in case Awake didn't run if GameObject was inactive)
        SubscribeToGameOverEvent();
    }

    private void OnEnable()
    {
        // Ensure subscription when component becomes active
        SubscribeToGameOverEvent();
    }

    private void OnDisable()
    {
        // Unsubscribe from game over event
        UnsubscribeFromGameOverEvent();
    }

    /// <summary>
    /// Subscribes to the game over event from GameManager.
    /// </summary>
    private void SubscribeToGameOverEvent()
    {
        if (gameManager != null)
        {
            // Unsubscribe first to avoid duplicate subscriptions
            gameManager.OnGameOver -= HandleGameOver;
            gameManager.OnGameOver += HandleGameOver;
            Debug.Log("[GameOverScreen] Subscribed to GameManager.OnGameOver event");
        }
        else
        {
            Debug.LogWarning("[GameOverScreen] Cannot subscribe - GameManager is null!");
        }
    }

    /// <summary>
    /// Unsubscribes from the game over event.
    /// </summary>
    private void UnsubscribeFromGameOverEvent()
    {
        if (gameManager != null)
        {
            gameManager.OnGameOver -= HandleGameOver;
        }
    }

    /// <summary>
    /// Handles the game over event from GameManager.
    /// </summary>
    private void HandleGameOver(float babyHappinessPercentage, int steaksCooked, int laundryFolded)
    {
        Debug.Log($"[GameOverScreen] Game over event received! Baby: {babyHappinessPercentage:F1}%, Steaks: {steaksCooked}, Laundry: {laundryFolded}");
        ShowGameOverScreen(babyHappinessPercentage, steaksCooked, laundryFolded);
    }

    /// <summary>
    /// Shows the game over screen with the given metrics.
    /// </summary>
    public void ShowGameOverScreen(float babyHappinessPercentage, int steaksCooked, int laundryFolded)
    {
        Debug.Log($"[GameOverScreen] ShowGameOverScreen called. Panel: {(gameOverPanel != null ? gameOverPanel.name : "null")}, Bars: Baby={(babyHappinessBar != null)}, Steaks={(steaksCookedBar != null)}, Laundry={(laundryFoldedBar != null)}");

        // Ensure we're subscribed (safety check)
        SubscribeToGameOverEvent();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Debug.Log("[GameOverScreen] Panel activated");
        }
        else
        {
            Debug.LogError("[GameOverScreen] gameOverPanel is null! Make sure it's assigned in the inspector.");
        }

        // Update bar charts
        UpdateBarCharts(babyHappinessPercentage, steaksCooked, laundryFolded);

        // Update labels if available
        UpdateLabels(babyHappinessPercentage, steaksCooked, laundryFolded);
    }

    /// <summary>
    /// Configures a bar chart Image component for use as a fillable bar.
    /// Sets up Image type and stores RectTransform reference if needed.
    /// </summary>
    private void ConfigureBar(Image bar, ref RectTransform rectTransform)
    {
        if (bar == null) return;

        if (!useFilledImage)
        {
            rectTransform = bar.GetComponent<RectTransform>();
        }
        else
        {
            // Set Image type to Filled if using filled method
            bar.type = Image.Type.Filled;
            bar.fillMethod = Image.FillMethod.Vertical;
            bar.fillOrigin = 0; // Bottom
        }
    }

    /// <summary>
    /// Updates a single bar chart with a normalized value (0-1) and sets color based on value.
    /// </summary>
    private void UpdateBar(Image bar, RectTransform rectTransform, float normalizedValue)
    {
        if (bar == null) return;

        // Update bar fill amount or size
        if (useFilledImage)
        {
            bar.fillAmount = normalizedValue;
        }
        else if (rectTransform != null)
        {
            Vector2 size = rectTransform.sizeDelta;
            size.y = maxBarHeight * normalizedValue;
            rectTransform.sizeDelta = size;
        }

        // Set color based on value (as percentage)
        float percentage = normalizedValue * 100f;
        if (percentage > 75f)
        {
            bar.color = highValueColor;
        }
        else if (percentage < 50f)
        {
            bar.color = lowValueColor;
        }
        else
        {
            bar.color = mediumValueColor;
        }
    }

    /// <summary>
    /// Hides the game over screen.
    /// </summary>
    public void HideGameOverScreen()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Updates the bar charts with normalized values.
    /// </summary>
    private void UpdateBarCharts(float babyHappinessPercentage, int steaksCooked, int laundryFolded)
    {
        if (gameManager == null)
        {
            return;
        }

        // Get normalized values from GameManager
        var (normalizedBabyHappiness, normalizedSteaks, normalizedLaundry) = gameManager.GetNormalizedMetrics();

        // Update all bars using helper method
        UpdateBar(babyHappinessBar, babyHappinessRect, normalizedBabyHappiness);
        UpdateBar(steaksCookedBar, steaksCookedRect, normalizedSteaks);
        UpdateBar(laundryFoldedBar, laundryFoldedRect, normalizedLaundry);
    }

    /// <summary>
    /// Updates the text labels with metric values.
    /// </summary>
    private void UpdateLabels(float babyHappinessPercentage, int steaksCooked, int laundryFolded)
    {
        if (babyHappinessLabel != null)
        {
            babyHappinessLabel.text = string.Format("Baby Happiness: {0:F1}%", babyHappinessPercentage);
        }

        if (steaksCookedLabel != null)
        {
            steaksCookedLabel.text = string.Format("Steaks Cooked: {0}", steaksCooked);
        }

        if (laundryFoldedLabel != null)
        {
            laundryFoldedLabel.text = string.Format("Laundry Folded: {0}", laundryFolded);
        }
    }

    /// <summary>
    /// Handles restart button click.
    /// </summary>
    private void OnRestartClicked()
    {
        if (gameManager != null)
        {
            gameManager.RestartGame();
        }
    }
}
