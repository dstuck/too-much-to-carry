using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Visual highlight that shows the grid square in front of the player.
/// Displays a black outline with transparency to indicate the interaction target.
/// </summary>
public class FrontTileHighlight : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Grid grid;
    
    [Header("Visual Settings")]
    [SerializeField] private Color outlineColor = new Color(0, 0, 0, 0.3f); // Black with 30% alpha
    [SerializeField] private Color blockedColor = new Color(1, 0, 0, 0.5f); // Red with 50% alpha
    [SerializeField] private float lineWidth = 0.02f;
    
    private LineRenderer lineRenderer;
    private Vector3Int lastHighlightedCell = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
    private System.Collections.Generic.HashSet<GameObject> blockingObjects = new System.Collections.Generic.HashSet<GameObject>();
    
    private void Awake()
    {
        // Find player controller if not assigned
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }
        
        // Find grid if not assigned
        if (grid == null)
        {
            grid = FindFirstObjectByType<Grid>();
        }
        
        // Set up LineRenderer
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }
        
        ConfigureLineRenderer();
    }
    
    private void ConfigureLineRenderer()
    {
        // Create a simple material for the line
        Material lineMaterial = new Material(Shader.Find("Sprites/Default"));
        lineMaterial.color = outlineColor;
        
        lineRenderer.material = lineMaterial;
        lineRenderer.startColor = outlineColor;
        lineRenderer.endColor = outlineColor;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = true;
        lineRenderer.sortingOrder = 10; // Render on top
        lineRenderer.positionCount = 5; // 4 corners + back to start for loop
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
    }
    
    private void Update()
    {
        if (playerController == null || grid == null)
        {
            lineRenderer.enabled = false;
            return;
        }
        
        // Get the front tile position from player controller
        Vector3 frontTilePosition = GetFrontTilePosition();
        Vector3Int currentCell = grid.WorldToCell(frontTilePosition);
        
        // Update highlight position to match front tile
        Vector3 oldPosition = transform.position;
        transform.position = frontTilePosition;
        
        // Update if the cell changed (optimization: only update when needed)
        if (currentCell != lastHighlightedCell)
        {
            // Clear blocking objects when moving to a new cell
            blockingObjects.Clear();
            UpdateHighlight(currentCell);
            lastHighlightedCell = currentCell;
            
            // Manually check for overlapping objects at the new position
            // OnTriggerEnter won't fire for objects already overlapping when we move the trigger
            CheckOverlappingObjects();
        }
        
        // Always update color every frame to reflect changes in blockingObjects
        // (objects might enter/exit the trigger via OnTriggerEnter/Exit)
        UpdateHighlightColor();
    }
    
    private Vector3 GetFrontTilePosition()
    {
        // Use the public method from PlayerController
        if (playerController != null)
        {
            return playerController.GetFrontTileWorldPosition();
        }
        
        // Fallback if player controller not found
        if (grid == null)
        {
            return Vector3.zero;
        }
        
        return grid.GetCellCenterWorld(Vector3Int.zero);
    }
    
    private void UpdateHighlight(Vector3Int cell)
    {
        if (grid == null)
        {
            lineRenderer.enabled = false;
            return;
        }
        
        // Get the cell center and size
        Vector3 cellCenter = grid.GetCellCenterWorld(cell);
        Vector3 cellSize = grid.cellSize;
        
        // Calculate the four corners of the cell
        float halfWidth = cellSize.x * 0.5f;
        float halfHeight = cellSize.y * 0.5f;
        
        Vector3[] corners = new Vector3[5]
        {
            new Vector3(cellCenter.x - halfWidth, cellCenter.y - halfHeight, 0), // Bottom-left
            new Vector3(cellCenter.x + halfWidth, cellCenter.y - halfHeight, 0), // Bottom-right
            new Vector3(cellCenter.x + halfWidth, cellCenter.y + halfHeight, 0), // Top-right
            new Vector3(cellCenter.x - halfWidth, cellCenter.y + halfHeight, 0), // Top-left
            new Vector3(cellCenter.x - halfWidth, cellCenter.y - halfHeight, 0)  // Back to start (for loop)
        };
        
        lineRenderer.SetPositions(corners);
        lineRenderer.enabled = true;
    }

    /// <summary>
    /// Updates the highlight color based on whether placement is blocked.
    /// </summary>
    private void UpdateHighlightColor()
    {
        // Check if any blocking objects are in the trigger
        bool canPlace = blockingObjects.Count == 0;
        Color currentColor = canPlace ? outlineColor : blockedColor;

        // Debug logging
        if (blockingObjects.Count > 0)
        {
            Debug.Log($"UpdateHighlightColor: Blocked! Count={blockingObjects.Count}, Color={currentColor}");
        }

        // Update line renderer colors
        lineRenderer.startColor = currentColor;
        lineRenderer.endColor = currentColor;

        // Update material color if it exists
        if (lineRenderer.material != null)
        {
            lineRenderer.material.color = currentColor;
        }
    }

    /// <summary>
    /// Called when an object enters the trigger collider.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Track objects with BlocksPlacement tag
        if (other.gameObject.CompareTag("BlocksPlacement"))
        {
            blockingObjects.Add(other.gameObject);
            Debug.Log($"OnTriggerEnter2D: {other.gameObject.name} entered highlight at {transform.position}");
        }
    }

    /// <summary>
    /// Called when an object exits the trigger collider.
    /// </summary>
    private void OnTriggerExit2D(Collider2D other)
    {
        // Remove objects that leave the trigger
        if (other.gameObject.CompareTag("BlocksPlacement"))
        {
            blockingObjects.Remove(other.gameObject);
            Debug.Log($"OnTriggerExit2D: {other.gameObject.name} exited highlight at {transform.position}");
        }
    }

    /// <summary>
    /// Manually checks for overlapping objects in the trigger collider.
    /// Called when the highlight moves to a new cell to detect objects already at that position
    /// (since moving a trigger doesn't automatically call OnTriggerEnter for objects already overlapping).
    /// </summary>
    private void CheckOverlappingObjects()
    {
        Collider2D highlightCollider = GetComponent<Collider2D>();
        if (highlightCollider == null)
        {
            Debug.LogWarning("FrontTileHighlight: No Collider2D found! Make sure the highlight has a trigger collider.");
            return;
        }

        // Get all colliders currently overlapping with this trigger collider
        Collider2D[] hits = new Collider2D[20];
        int hitCount = highlightCollider.Overlap(new ContactFilter2D().NoFilter(), hits);

        for (int i = 0; i < hitCount; i++)
        {
            GameObject hitObject = hits[i].gameObject;
            
            // Only check objects with BlocksPlacement tag
            if (hitObject.CompareTag("BlocksPlacement"))
            {
                blockingObjects.Add(hitObject);
                Debug.Log($"CheckOverlappingObjects: {hitObject.name} is blocking");
            }
        }
    }

    /// <summary>
    /// Checks if placement is currently blocked by objects in the trigger.
    /// </summary>
    public bool IsPlacementBlocked()
    {
        bool blocked = blockingObjects.Count > 0;
        if (blocked)
        {
            Debug.Log($"IsPlacementBlocked: true, {blockingObjects.Count} blocking objects");
        }
        return blocked;
    }
}
