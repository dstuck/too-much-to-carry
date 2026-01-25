using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages highlighting of objects based on items held in player's hands.
/// Determines which objects should be highlighted and coordinates the sparkle effects.
/// </summary>
public class HighlightManager : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color leftHandColor = new Color(0f, 1f, 0f, 1f); // Green
    [SerializeField] private Color rightHandColor = new Color(0f, 0.5f, 1f, 1f); // Blue
    
    [Header("Particle Prefab")]
    [Tooltip("Prefab with ParticleSystem component. Configure all particle settings in the prefab itself.")]
    [SerializeField] private GameObject sparkleParticlePrefab;
    
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    
    // Cached lists of highlightable objects
    private List<HighlightableObject> highlightedObjects = new List<HighlightableObject>();
    
    private void Awake()
    {
        // Find player controller if not assigned
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }
    }
    
    private void Start()
    {
        // Initial update of highlights
        UpdateAllHighlights();
    }
    
    private void OnEnable()
    {
        // Subscribe to hand change events
        if (playerController != null)
        {
            playerController.OnHandChanged += OnHandChanged;
        }
    }
    
    private void OnDisable()
    {
        // Unsubscribe from hand change events
        if (playerController != null)
        {
            playerController.OnHandChanged -= OnHandChanged;
        }
    }
    
    /// <summary>
    /// Called when a hand slot changes (item picked up or put down).
    /// Updates highlighting for all objects.
    /// </summary>
    private void OnHandChanged(HandSlot hand, HoldableItem item)
    {
        // Update highlighting whenever any hand changes
        UpdateAllHighlights();
    }
    
    /// <summary>
    /// Updates highlighting for all objects based on current held items.
    /// </summary>
    private void UpdateAllHighlights()
    {
        // Clear all current highlights
        ClearAllHighlights();
        
        // Get current held items
        HoldableItem leftItem = playerController != null ? playerController.GetHeldItem(HandSlot.Left) : null;
        HoldableItem rightItem = playerController != null ? playerController.GetHeldItem(HandSlot.Right) : null;
        
        // Update highlights for left hand
        if (leftItem != null)
        {
            HighlightForItem(leftItem, HandSlot.Left, leftHandColor);
        }
        
        // Update highlights for right hand
        if (rightItem != null)
        {
            HighlightForItem(rightItem, HandSlot.Right, rightHandColor);
        }
    }
    
    /// <summary>
    /// Determines which objects should be highlighted based on the held item.
    /// </summary>
    /// <param name="item">The item held in the hand</param>
    /// <param name="handSlot">Which hand is holding the item</param>
    /// <param name="color">Color to use for highlighting</param>
    private void HighlightForItem(HoldableItem item, HandSlot handSlot, Color color)
    {
        // Uncooked meat → highlights stove
        if (item is RawMeat rawMeat && rawMeat.State == RawMeat.MeatState.Raw)
        {
            HighlightObjectsWithTag("Stove", color, handSlot);
        }
        // Cooked or burnt meat → highlights refrigerator
        else if (item is RawMeat meat && (meat.State == RawMeat.MeatState.Cooked || meat.State == RawMeat.MeatState.Burnt))
        {
            // Highlight objects with "Refrigerator" tag
            HighlightObjectsWithTag("Refrigerator", color, handSlot);
            // Also highlight objects with Refrigerator component (in case tag isn't set)
            HighlightObjectsWithComponent<Refrigerator>(color, handSlot);
        }
        // Poopy baby → highlights changing table and diapers
        else if (item is Baby baby && baby.IsDirty)
        {
            // Highlight changing tables (tag "Crib" + name contains "Changing")
            HighlightObjectsWithTagAndName("Crib", "Changing", color, handSlot);
            // Highlight diapers (objects with Diaper component)
            HighlightObjectsWithComponent<Diaper>(color, handSlot);
        }
        // Unfolded laundry → highlights counters (lightly)
        else if (item is Laundry laundry && laundry.State == Laundry.LaundryState.Unfolded)
        {
            // Counters are objects with "Crib" tag but name does NOT contain "Changing"
            HighlightCounters(color, handSlot);
        }
        // Folded laundry → highlights hamper
        else if (item is Laundry foldedLaundry && foldedLaundry.State == Laundry.LaundryState.Folded)
        {
            HighlightObjectsWithTag("Hamper", color, handSlot);
        }
    }
    
    /// <summary>
    /// Highlights all objects with the specified tag.
    /// </summary>
    private void HighlightObjectsWithTag(string tag, Color color, HandSlot handSlot)
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
        if (objects.Length == 0)
        {
            Debug.LogWarning($"[HighlightManager] No objects found with tag '{tag}'. Make sure objects are tagged correctly.");
        }
        foreach (GameObject obj in objects)
        {
            HighlightObject(obj, color, handSlot);
        }
    }
    
    /// <summary>
    /// Highlights all objects whose name contains the specified string.
    /// </summary>
    private void HighlightObjectsByName(string nameContains, Color color, HandSlot handSlot)
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains(nameContains))
            {
                HighlightObject(obj, color, handSlot);
            }
        }
    }
    
    /// <summary>
    /// Highlights all objects with the specified tag AND whose name contains the specified string.
    /// </summary>
    private void HighlightObjectsWithTagAndName(string tag, string nameContains, Color color, HandSlot handSlot)
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject obj in objects)
        {
            if (obj.name.Contains(nameContains))
            {
                HighlightObject(obj, color, handSlot);
            }
        }
    }
    
    /// <summary>
    /// Highlights all objects with the specified component type.
    /// </summary>
    private void HighlightObjectsWithComponent<T>(Color color, HandSlot handSlot) where T : Component
    {
        T[] components = FindObjectsByType<T>(FindObjectsSortMode.None);
        foreach (T component in components)
        {
            HighlightObject(component.gameObject, color, handSlot);
        }
    }
    
    /// <summary>
    /// Highlights counters (objects with "Crib" tag but name does NOT contain "Changing").
    /// Uses a lighter color for subtle highlighting.
    /// </summary>
    private void HighlightCounters(Color baseColor, HandSlot handSlot)
    {
        // Use lighter color for counters
        Color counterColor = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * 0.5f);
        
        GameObject[] objects = GameObject.FindGameObjectsWithTag("Crib");
        foreach (GameObject obj in objects)
        {
            // Only highlight if name does NOT contain "Changing"
            if (!obj.name.Contains("Changing"))
            {
                HighlightObject(obj, counterColor, handSlot);
            }
        }
    }
    
    /// <summary>
    /// Highlights a specific object by adding/updating its HighlightableObject component.
    /// If both hands highlight the same object, the right hand's color takes precedence.
    /// </summary>
    private void HighlightObject(GameObject obj, Color color, HandSlot handSlot)
    {
        if (obj == null) return;
        
        // Get or add HighlightableObject component
        HighlightableObject highlightable = obj.GetComponent<HighlightableObject>();
        if (highlightable == null)
        {
            highlightable = obj.AddComponent<HighlightableObject>();
        }
        
        // Initialize with prefab from HighlightManager (in case it wasn't initialized yet)
        highlightable.Initialize(sparkleParticlePrefab);
        
        // If already highlighted by the other hand, keep the current highlight
        // (right hand takes precedence if both highlight the same object)
        if (highlightable.IsHighlighted && highlightable.CurrentHandSlot != handSlot)
        {
            // If right hand is highlighting, it takes precedence
            if (handSlot == HandSlot.Right)
            {
                highlightable.ShowHighlight(color, handSlot);
            }
            // Otherwise, keep the existing highlight
        }
        else
        {
            // Show highlight
            highlightable.ShowHighlight(color, handSlot);
        }
        
        // Track in list
        if (!highlightedObjects.Contains(highlightable))
        {
            highlightedObjects.Add(highlightable);
        }
    }
    
    /// <summary>
    /// Clears all current highlights.
    /// </summary>
    private void ClearAllHighlights()
    {
        foreach (HighlightableObject highlightable in highlightedObjects)
        {
            if (highlightable != null)
            {
                highlightable.HideHighlight();
            }
        }
        highlightedObjects.Clear();
    }
    
    /// <summary>
    /// Manually triggers an update of all highlights.
    /// Can be called from external code if needed.
    /// </summary>
    public void RefreshHighlights()
    {
        UpdateAllHighlights();
    }
}
