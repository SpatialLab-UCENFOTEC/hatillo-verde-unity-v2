using UnityEngine;

public class InteractableHighlight : MonoBehaviour
{
    [Header("WebGL Optimized")]
    public float hoverScaleMultiplier = 2f;
    public float animationSpeed = 15f;
    public float groundAdjustment = 0.1f;

    [Header("Sprite Highlight")]
    public Color spriteHighlightColor = new Color(1.5f, 1.5f, 1.5f, 1f);

    [Header("Shader Highlight")]
    public Color shaderHighlightColor = Color.white;

    [SerializeField] private string shaderColorProperty = "_ShallowColor";

    private Vector3 originalScale;
    private Vector3 targetScale;

    private Vector3 originalPosition;
    private Vector3 targetPosition;

    // Sprite support
    private SpriteRenderer spriteRenderer;
    private Color originalSpriteColor;

    // Shader support
    private Renderer objectRenderer;
    private Material materialInstance;
    private Color originalShaderColor;
    private bool hasShaderProperty;

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;

        originalPosition = transform.position;
        targetPosition = originalPosition;

        // Sprite Renderer
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            originalSpriteColor = spriteRenderer.color;
        }

        // Mesh/Material Renderer
        objectRenderer = GetComponentInChildren<Renderer>();

        if (objectRenderer != null)
        {
            materialInstance = objectRenderer.material;

            if (materialInstance.HasProperty(shaderColorProperty))
            {
                hasShaderProperty = true;
                originalShaderColor =
                    materialInstance.GetColor(shaderColorProperty);
            }
        }
    }

    void Update()
    {
        // Smooth scale
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.deltaTime * animationSpeed
        );

        // Smooth position
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

        // Sprite highlight
        if (spriteRenderer != null)
        {
            spriteRenderer.color = spriteHighlightColor;
        }

        // Shader Graph highlight
        if (materialInstance != null && hasShaderProperty)
        {
            materialInstance.SetColor(
                shaderColorProperty,
                shaderHighlightColor
            );
        }
    }

    public void OnHoverExit()
    {
        targetScale = originalScale;
        targetPosition = originalPosition;

        // Sprite reset
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalSpriteColor;
        }

        // Shader Graph reset
        if (materialInstance != null && hasShaderProperty)
        {
            materialInstance.SetColor(
                shaderColorProperty,
                originalShaderColor
            );
        }
    }
}