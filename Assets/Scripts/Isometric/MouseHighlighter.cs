using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MouseHighlighter : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private float raycastDistance = 100f;
    [SerializeField] private LayerMask raycastMask = ~0; // Everything by default
    
    [Header("UI Settings")]
    [SerializeField] private Canvas tooltipCanvas;
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private RectTransform tooltipPanel;
    [SerializeField] private Vector2 tooltipOffset = new Vector2(15f, 15f);
    
    [Header("Visual Feedback - Cursor")]
    [SerializeField] private bool changeMouseCursor = true;
    [SerializeField] private Texture2D hoverCursor;
    [SerializeField] private Texture2D defaultCursor;
    
    // State
    private Camera mainCamera;
    private GameObject lastHighlightedObject;
    private bool isHoveringHighlightable;
    private HighlightableGroupAuthoring currentHighlightable;

    void Start()
    {
        mainCamera = Camera.main;
        
        // Make sure tooltip is hidden initially
        if (tooltipPanel != null)
        {
            tooltipPanel.gameObject.SetActive(false);
        }
        
        // Make sure cursor is visible and unlocked
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        // Only set cursor if textures are valid
        if (changeMouseCursor && defaultCursor != null)
        {
            // Check if texture is readable (CPU accessible)
            try
            {
                Vector2 hotspot = new Vector2(
                    defaultCursor.width * 0.5f,
                    defaultCursor.height * 0f
                );
                Cursor.SetCursor(defaultCursor, hotspot, CursorMode.Auto);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Failed to set cursor. Make sure cursor textures have 'Read/Write Enabled' in import settings. Error: " + e.Message);
                changeMouseCursor = false; // Disable cursor changing
            }
        }
    }

    void OnDisable()
    {
        // Force clear highlight when script is disabled
        ClearHighlight();
    }

    void Update()
    {
        // Keep cursor visible and unlocked
        if (!Cursor.visible || Cursor.lockState != CursorLockMode.None)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        
        PerformRaycast();
    }

    void PerformRaycast()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, raycastMask))
        {

            HighlightableGroupAuthoring highlightable =
                hit.collider.GetComponentInParent<HighlightableGroupAuthoring>();

            // Check if hit object has the highlightable tag
            if (highlightable != null)
            {
                isHoveringHighlightable = true;
                
                // If this is a new object, update highlight
                if (hit.collider.gameObject != lastHighlightedObject)
                {
                    // Remove highlight from previous object
                    RemoveHighlight();
                    
                    // Add highlight to new object
                    AddHighlight(hit.collider.gameObject);
                    
                    lastHighlightedObject = hit.collider.gameObject;
                }
                
                // Show tooltip
                ShowTooltip(hit.collider.gameObject.GetComponentInParent<HighlightableGroupAuthoring>());
                
                // Change cursor
                if (changeMouseCursor && hoverCursor != null)
                {
                    try
                    {
                        Vector2 hotspot = new Vector2(
                            hoverCursor.width * 0.5f,
                            hoverCursor.height * 0f
                        );
                        Cursor.SetCursor(hoverCursor, hotspot, CursorMode.Auto);
                    }
                    catch
                    {
                        // Silently fail if cursor can't be set
                    }
                }
            }
            else
            {
                // Hit something but not highlightable
                ClearHighlight();
            }
        }
        else
        {
            // Didn't hit anything
            ClearHighlight();
        }
    }

    void AddHighlight(GameObject obj)
    {
        HighlightableGroupAuthoring highlightable =
            obj.GetComponentInParent<HighlightableGroupAuthoring>();

        if (highlightable == null)
            return;

        highlightable.EnableHighlight();
        currentHighlightable = highlightable;
    }

    void RemoveHighlight()
    {
        if (currentHighlightable != null)
        {
            currentHighlightable.DisableHighlight();
            currentHighlightable = null;
        }
    }

    void ClearHighlight()
    {
        if (isHoveringHighlightable)
        {
            isHoveringHighlightable = false;
            
            // Remove highlight
            RemoveHighlight();
            
            // Hide tooltip
            HideTooltip();
            
            // Reset cursor
            if (changeMouseCursor)
            {
                try
                {
                    if (defaultCursor != null)
                    {
                        Vector2 hotspot = new Vector2(
                            defaultCursor.width * 0.5f,
                            defaultCursor.height * 0f
                        );
                        Cursor.SetCursor(defaultCursor, hotspot, CursorMode.Auto);
                    }
                    else
                    {
                        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                    }
                }
                catch
                {
                    // Silently fail if cursor can't be set
                }
            }
            
            lastHighlightedObject = null;
        }
    }

    void ShowTooltip(HighlightableGroupAuthoring highlightable)
{
    if (tooltipPanel == null || tooltipText == null)
        return;

    tooltipText.text = highlightable.GetTooltipText();
    tooltipPanel.gameObject.SetActive(true);
}

    void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.gameObject.SetActive(false);
        }
    }

    // Gizmos for debugging
    void OnDrawGizmos()
    {
        if (mainCamera == null) return;
        
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * raycastDistance);
    }
}