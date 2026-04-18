using UnityEngine;

public class InteractableHighlight : MonoBehaviour
{
    [Header("WebGL Optimized")]
    public float hoverScaleMultiplier = 2f;
    public float animationSpeed = 15f;
    public float groundAdjustment = 0.1f; // prevent clipping with the ground when scaling up

    // Instead of just white, try a very slight "Glow" color or 
    // a color that contrasts with your background.
    public Color highlightColor = new Color(1.5f, 1.5f, 1.5f, 1f);

    private Vector3 originalScale;
    private Vector3 targetScale;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    void Update()
    {
        // Smooth scaling is very cheap for WebGL
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);
    }

    public void OnHoverEnter()
    {
        targetScale = originalScale * hoverScaleMultiplier;
        //Move vertically to avoid clipping with the ground. Adjust as needed.
        if (groundAdjustment != 0f)
        {
            transform.position += Vector3.up * groundAdjustment;
        }
            
        if (spriteRenderer != null)
        {
            // If the sprite is already white, setting it to white does nothing.
            // Try setting it to a slight tint OR use a custom shader with a "Flash" property.
            spriteRenderer.color = highlightColor;
            //Increase 3D emmission if using a shader that supports it, or add a subtle glow effect.

        }
    }

    public void OnHoverExit()
    {
        targetScale = originalScale;
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
        // Move back down to original position 
        if (groundAdjustment != 0f)
        { 
           transform.position -= Vector3.up * groundAdjustment; 
        }
            
    }
}