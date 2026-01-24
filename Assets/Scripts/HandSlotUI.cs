using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Component for displaying an individual hand slot in the UI.
/// Shows a colored square (green for left, blue for right) and the held item sprite.
/// </summary>
public class HandSlotUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool isLeftHand = true;
    
    [Header("Sprites")]
    [SerializeField] private Sprite defaultEmptySprite; // Sprite to show when hand is empty
    
    [Header("UI References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image itemImage;

    private void Awake()
    {
        // Set up UI references if not assigned
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }
        
        if (itemImage == null)
        {
            // Try to find child image for item sprite
            Image[] images = GetComponentsInChildren<Image>();
            foreach (var img in images)
            {
                if (img != backgroundImage)
                {
                    itemImage = img;
                    break;
                }
            }
            
            // If still not found, create one
            if (itemImage == null)
            {
                GameObject itemObj = new GameObject("ItemImage");
                itemObj.transform.SetParent(transform, false);
                itemImage = itemObj.AddComponent<Image>();
            }
        }
        
        // Set background color
        if (backgroundImage != null)
        {
            backgroundImage.color = isLeftHand ? Color.green : Color.blue;
        }
        
        // Initialize item image with default empty sprite if available
        if (itemImage != null)
        {
            if (defaultEmptySprite != null)
            {
                itemImage.sprite = defaultEmptySprite;
                itemImage.enabled = true;
            }
            else
            {
                itemImage.enabled = false;
            }
        }
    }

    /// <summary>
    /// Sets the sprite to display in this hand slot.
    /// </summary>
    /// <param name="sprite">Sprite to display (null to show default empty sprite or hide)</param>
    public void SetItemSprite(Sprite sprite)
    {
        SetItemSprite(sprite, Color.white);
    }
    
    /// <summary>
    /// Sets the sprite and color to display in this hand slot.
    /// </summary>
    /// <param name="sprite">Sprite to display (null to show default empty sprite or hide)</param>
    /// <param name="color">Color tint to apply to the sprite</param>
    public void SetItemSprite(Sprite sprite, Color color)
    {
        if (itemImage != null)
        {
            if (sprite != null)
            {
                // Show the held item sprite with color tint
                itemImage.sprite = sprite;
                itemImage.color = color;
                itemImage.enabled = true;
            }
            else
            {
                // Show default empty sprite or hide
                if (defaultEmptySprite != null)
                {
                    itemImage.sprite = defaultEmptySprite;
                    itemImage.color = Color.white;
                    itemImage.enabled = true;
                }
                else
                {
                    itemImage.enabled = false;
                }
            }
        }
    }

    /// <summary>
    /// Returns whether this is the left hand slot.
    /// </summary>
    public bool IsLeftHand()
    {
        return isLeftHand;
    }
}
