using UnityEngine;

public class InteractableHighlight : MonoBehaviour
{
    [Header("WebGL Optimized")]
    public float hoverScaleMultiplier = 2f;
    public float animationSpeed = 15f;
    public float groundAdjustment = 0.1f;

    public Color highlightColor = new Color(1.5f, 1.5f, 1.5f, 1f);

    private Vector3 originalScale;
    private Vector3 targetScale;

    private Vector3 originalPosition;
    private Vector3 targetPosition;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;

        originalPosition = transform.position;
        targetPosition = originalPosition;

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    void Update()
    {
        // Smooth scale
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * animationSpeed
        );

        // Smooth position (NO más acumulación)
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            Time.deltaTime * animationSpeed
        );
    }

    public void OnHoverEnter()
    {
        targetScale = originalScale * hoverScaleMultiplier;
        targetPosition = originalPosition + Vector3.up * groundAdjustment;

        if (spriteRenderer != null)
            spriteRenderer.color = highlightColor;
    }

    public void OnHoverExit()
    {
        targetScale = originalScale;
        targetPosition = originalPosition;

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }
}