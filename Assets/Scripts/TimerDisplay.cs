using UnityEngine;
using TMPro;

/// <summary>
/// Displays the game timer countdown in MM:SS format.
/// Subscribes to GameManager's OnTimerUpdate event.
/// </summary>
public class TimerDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText;
    
    private GameManager gameManager;

    private void Awake()
    {
        // Find GameManager if not assigned
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        // Find text component if not assigned
        if (timerText == null)
        {
            timerText = GetComponent<TextMeshProUGUI>();
            if (timerText == null)
            {
                timerText = GetComponentInChildren<TextMeshProUGUI>();
            }
        }
    }

    private void OnEnable()
    {
        // Subscribe to timer updates
        if (gameManager != null)
        {
            gameManager.OnTimerUpdate += HandleTimerUpdate;
            
            // Update immediately with current time
            if (gameManager.TimeRemaining > 0f)
            {
                UpdateTimerDisplay(gameManager.TimeRemaining);
            }
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from timer updates
        if (gameManager != null)
        {
            gameManager.OnTimerUpdate -= HandleTimerUpdate;
        }
    }

    /// <summary>
    /// Handles timer update events from GameManager.
    /// </summary>
    private void HandleTimerUpdate(float timeRemaining)
    {
        UpdateTimerDisplay(timeRemaining);
    }

    /// <summary>
    /// Updates the timer display with the given time remaining.
    /// </summary>
    private void UpdateTimerDisplay(float timeRemaining)
    {
        if (timerText == null)
        {
            return;
        }

        // Format time as MM:SS
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}
