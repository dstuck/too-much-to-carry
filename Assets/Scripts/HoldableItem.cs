using UnityEngine;

/// <summary>
/// Base class for items that can be picked up and held by the player.
/// Handles visual representation and GameObject state when held.
/// </summary>
public class HoldableItem : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    /// <summary>
    /// Gets the sprite to display in the UI when this item is held.
    /// </summary>
    public Sprite GetSprite()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        return spriteRenderer != null ? spriteRenderer.sprite : null;
    }

    /// <summary>
    /// Called when the item is picked up. Disables the GameObject.
    /// </summary>
    public void OnPickedUp()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Called when the item is put down. Enables the GameObject at the specified position.
    /// </summary>
    /// <param name="position">World position to place the item</param>
    public void OnPutDown(Vector3 position)
    {
        transform.position = position;
        gameObject.SetActive(true);
    }

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
}
