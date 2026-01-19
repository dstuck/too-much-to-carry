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
    [SerializeField] private float lineWidth = 0.02f;
    
    private LineRenderer lineRenderer;
    private Vector3Int lastHighlightedCell = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
    
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
        
        // Update if the cell changed (optimization: only update when needed)
        if (currentCell != lastHighlightedCell)
        {
            UpdateHighlight(currentCell);
            lastHighlightedCell = currentCell;
        }
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
}
