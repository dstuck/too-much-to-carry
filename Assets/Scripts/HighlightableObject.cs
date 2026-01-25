using UnityEngine;

/// <summary>
/// Component that can be attached to objects to enable highlighting with sparkle effects.
/// Uses a prefab particle system - all particle settings should be configured in the prefab itself.
/// Code only instantiates the prefab and changes the color.
/// </summary>
public class HighlightableObject : MonoBehaviour
{
    private ParticleSystem sparkleParticles;
    private GameObject particleInstance;
    private bool isInitialized = false;
    
    // Current highlight state
    private bool isHighlighted = false;
    private Color currentHighlightColor = Color.white;
    private HandSlot currentHandSlot = HandSlot.Left;
    
    /// <summary>
    /// Initializes the particle system using a prefab. Called by HighlightManager when component is first added.
    /// The prefab should have all particle settings configured - this only instantiates it.
    /// </summary>
    public void Initialize(GameObject particlePrefab)
    {
        if (particlePrefab == null)
        {
            Debug.LogWarning($"[HighlightableObject] No particle prefab provided for {gameObject.name}. Highlighting will not work.");
            return;
        }
        
        if (isInitialized)
        {
            return; // Already initialized
        }
        
        // Create particle system from prefab if not already created
        if (sparkleParticles == null)
        {
            CreateSparkleParticleSystem(particlePrefab);
            isInitialized = true;
        }
    }
    
    /// <summary>
    /// Instantiates the particle prefab. All settings should be configured in the prefab.
    /// </summary>
    private void CreateSparkleParticleSystem(GameObject particlePrefab)
    {
        // Instantiate the prefab as a child
        particleInstance = Instantiate(particlePrefab, transform);
        particleInstance.name = "SparkleParticles";
        particleInstance.transform.localPosition = Vector3.zero;
        particleInstance.transform.localRotation = Quaternion.identity;
        particleInstance.transform.localScale = Vector3.one;
        
        // Get the ParticleSystem component
        sparkleParticles = particleInstance.GetComponent<ParticleSystem>();
        if (sparkleParticles == null)
        {
            Debug.LogError($"[HighlightableObject] Particle prefab {particlePrefab.name} does not have a ParticleSystem component!");
            Destroy(particleInstance);
            return;
        }
        
        // Start disabled - will be enabled when highlighted
        sparkleParticles.Stop();
    }
    
    /// <summary>
    /// Shows the highlight effect with the specified color.
    /// </summary>
    /// <param name="color">Color of the sparkle effect (green for left hand, blue for right hand)</param>
    /// <param name="handSlot">Which hand is highlighting this object</param>
    public void ShowHighlight(Color color, HandSlot handSlot)
    {
        // Ensure particle system is created
        if (sparkleParticles == null)
        {
            Debug.LogWarning($"[HighlightableObject] Particle system not initialized for {gameObject.name}. Make sure HighlightManager is set up correctly.");
            return;
        }
        
        isHighlighted = true;
        currentHighlightColor = color;
        currentHandSlot = handSlot;
        
        // Update particle color - set start color
        var main = sparkleParticles.main;
        var startColor = main.startColor;
        startColor.color = color;
        main.startColor = startColor;
        
        // Update renderer material to ensure color is applied (fixes pink issue)
        var renderer = sparkleParticles.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            // If material exists, update its color
            if (renderer.material != null)
            {
                // Create a new material instance to avoid affecting the prefab
                if (renderer.material.name.Contains("(Instance)") == false)
                {
                    renderer.material = new Material(renderer.material);
                }
                renderer.material.color = color;
            }
        }
        
        // Update color over lifetime to use the new color
        // If the prefab has color over lifetime configured, we'll update it to use the highlight color
        var colorOverLifetime = sparkleParticles.colorOverLifetime;
        if (colorOverLifetime.enabled)
        {
            // Try to preserve existing alpha keys if it's a gradient
            ParticleSystem.MinMaxGradient currentGradient = colorOverLifetime.color;
            GradientAlphaKey[] alphaKeys;
            
            if (currentGradient.mode == ParticleSystemGradientMode.Gradient && currentGradient.gradient != null)
            {
                alphaKeys = currentGradient.gradient.alphaKeys;
            }
            else
            {
                // Default alpha fade
                alphaKeys = new GradientAlphaKey[] { 
                    new GradientAlphaKey(1.0f, 0.0f), 
                    new GradientAlphaKey(0.0f, 1.0f) 
                };
            }
            
            // Create new gradient with the highlight color but preserve alpha
            Gradient newGradient = new Gradient();
            newGradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(color, 0.0f), 
                    new GradientColorKey(color, 1.0f) 
                },
                alphaKeys
            );
            colorOverLifetime.color = newGradient;
        }
        
        // Clear and restart particles to ensure color is applied
        sparkleParticles.Stop();
        sparkleParticles.Clear();
        sparkleParticles.Play();
    }
    
    /// <summary>
    /// Hides the highlight effect.
    /// </summary>
    public void HideHighlight()
    {
        isHighlighted = false;
        
        if (sparkleParticles != null && sparkleParticles.isPlaying)
        {
            sparkleParticles.Stop();
            sparkleParticles.Clear();
        }
    }
    
    private void OnDestroy()
    {
        // Clean up particle instance
        if (particleInstance != null)
        {
            Destroy(particleInstance);
        }
    }
    
    /// <summary>
    /// Gets whether this object is currently highlighted.
    /// </summary>
    public bool IsHighlighted => isHighlighted;
    
    /// <summary>
    /// Gets the current highlight color.
    /// </summary>
    public Color CurrentHighlightColor => currentHighlightColor;
    
    /// <summary>
    /// Gets which hand slot is highlighting this object.
    /// </summary>
    public HandSlot CurrentHandSlot => currentHandSlot;
}
