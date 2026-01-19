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
        
        // Initialize item image
        if (itemImage != null)
        {
            itemImage.enabled = false;
        }
    }

    /// <summary>
    /// Sets the sprite to display in this hand slot.
    /// </summary>
    /// <param name="sprite">Sprite to display (null to hide)</param>
    public void SetItemSprite(Sprite sprite)
    {
        if (itemImage != null)
        {
            itemImage.sprite = sprite;
            itemImage.enabled = sprite != null;
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
