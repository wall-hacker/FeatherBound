using UnityEngine;
using cakeslice;

public class RevealableObject : MonoBehaviour
{
    [Header("Outline Settings")]
    [SerializeField] private int outlineColorIndex = 0;
    [SerializeField] private bool useCustomColor = false;
    [SerializeField] private Color customOutlineColor = Color.cyan;
    
    [Header("References")]
    [SerializeField] private cakeslice.OutlineEffect outlineEffect;
    
    private Outline[] outlines;
    private HighlightableGroupAuthoring highlightGroup;
    private bool isRevealed = false;
    private float currentOpacity = 1f;
    private Color originalColor;

    void Start()
    {
        // Find OutlineEffect if not assigned
        if (outlineEffect == null)
        {
            outlineEffect = Camera.main.GetComponent<cakeslice.OutlineEffect>();
            
            if (outlineEffect == null)
            {
                Debug.LogError($"[RevealableObject] No OutlineEffect found on main camera for {gameObject.name}!");
                return;
            }
        }
        
        // Get HighlightableGroupAuthoring if it exists
        highlightGroup = GetComponent<HighlightableGroupAuthoring>();
        
        // Store original outline color
        originalColor = useCustomColor ? customOutlineColor : GetColorFromIndex(outlineColorIndex);
        
        // Get all Outline components in this object and children
        outlines = GetComponentsInChildren<Outline>(true);
        
        // If no outlines exist, create them
        if (outlines.Length == 0)
        {
            CreateOutlines();
        }
        
        // DON'T touch the outlines here - let HighlightableGroupAuthoring manage them
        // Just set the color index for when we DO use them
        foreach (var outline in outlines)
        {
            // Store our preferred color but don't change the state
            // We'll use this color when Reveal() is called
        }
    }

    void CreateOutlines()
    {
        // Find all renderers in this object and children
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        
        foreach (var renderer in renderers)
        {
            // Skip particle systems
            if (renderer is ParticleSystemRenderer)
                continue;
            
            // Add Outline component if it doesn't exist
            Outline outline = renderer.GetComponent<Outline>();
            if (outline == null)
            {
                outline = renderer.gameObject.AddComponent<Outline>();
                // Set initial state to disabled
                outline.enabled = false;
                outline.eraseRenderer = true;
            }
            
            outline.color = outlineColorIndex;
        }
        
        // Refresh outlines array
        outlines = GetComponentsInChildren<Outline>(true);
    }

    public void Reveal(float opacity)
    {
        if (outlineEffect == null) return;
        
        isRevealed = true;
        currentOpacity = opacity;
        
        // Disable proximity highlighting while revealed
        if (highlightGroup != null)
        {
            highlightGroup.SetHighlightingEnabled(false);
            // Force disable the proximity outlines
            highlightGroup.DisableHighlight();
        }
        
        // Enable all outlines for reveal effect
        foreach (var outline in outlines)
        {
            if (outline != null)
            {
                outline.color = outlineColorIndex;
                outline.eraseRenderer = false;
                outline.enabled = true;
            }
        }
        
        // Set outline color with opacity
        UpdateOutlineColor(opacity);
    }

    public void UpdateFade(float fadeMultiplier)
    {
        if (!isRevealed || outlineEffect == null) return;
        
        // Multiply current opacity by fade multiplier
        float finalOpacity = currentOpacity * fadeMultiplier;
        UpdateOutlineColor(finalOpacity);
    }

    public void DisableReveal()
    {
        isRevealed = false;
        
        // Disable all outlines
        foreach (var outline in outlines)
        {
            if (outline != null)
            {
                outline.enabled = false;
                outline.eraseRenderer = true;
            }
        }
        
        // Reset color to full opacity
        ResetOutlineColor();
        
        // Re-enable proximity highlighting AFTER we've disabled outlines
        if (highlightGroup != null)
        {
            highlightGroup.SetHighlightingEnabled(true);
        }
    }

    void UpdateOutlineColor(float opacity)
    {
        if (outlineEffect == null) return;
        
        Color colorWithOpacity = originalColor;
        colorWithOpacity.a = opacity;
        
        // Update the color based on which outline index we're using
        switch (outlineColorIndex)
        {
            case 0:
                outlineEffect.lineColor0 = colorWithOpacity;
                break;
            case 1:
                outlineEffect.lineColor1 = colorWithOpacity;
                break;
            case 2:
                outlineEffect.lineColor2 = colorWithOpacity;
                break;
        }
        
        // Force update materials
        outlineEffect.UpdateMaterialsPublicProperties();
    }

    void ResetOutlineColor()
    {
        if (outlineEffect == null) return;
        
        Color resetColor = originalColor;
        resetColor.a = 1f;
        
        switch (outlineColorIndex)
        {
            case 0:
                outlineEffect.lineColor0 = resetColor;
                break;
            case 1:
                outlineEffect.lineColor1 = resetColor;
                break;
            case 2:
                outlineEffect.lineColor2 = resetColor;
                break;
        }
        
        outlineEffect.UpdateMaterialsPublicProperties();
    }

    Color GetColorFromIndex(int index)
    {
        if (outlineEffect == null) return Color.white;
        
        switch (index)
        {
            case 0:
                return outlineEffect.lineColor0;
            case 1:
                return outlineEffect.lineColor1;
            case 2:
                return outlineEffect.lineColor2;
            default:
                return Color.white;
        }
    }

    // Public getters
    public bool IsRevealed => isRevealed;
    public float CurrentOpacity => currentOpacity;
}