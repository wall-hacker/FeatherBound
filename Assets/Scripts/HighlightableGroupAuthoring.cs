using System.Collections.Generic;
using UnityEngine;
using cakeslice;

[DisallowMultipleComponent]
public class HighlightableGroupAuthoring : MonoBehaviour
{
    [Header("Tooltip")]
    [SerializeField] private string displayName;
    [TextArea]
    [SerializeField] private string description;

    [Header("Outline Settings")]
    [SerializeField] private int outlineColorIndex = 0;
    [SerializeField] private bool rebuildOnStart = true;

    private List<Outline> outlines = new List<Outline>();

    private bool highlightingEnabled = true;
    private bool isInitialized = false;

    private void Awake()
    {
        BuildGroup();
    }

    private void Start()
    {
        // Rebuild in Start to ensure all components are loaded
        if (rebuildOnStart || outlines.Count == 0 || outlines.Contains(null))
        {
            Debug.Log($"[{gameObject.name}] Rebuilding highlight group in Start");
            BuildGroup();
        }
        isInitialized = true;
    }

    public void BuildGroup()
    {
        outlines.Clear();

        // Find all renderers in children (including inactive ones)
        var renderers = GetComponentsInChildren<Renderer>(true);
        
        Debug.Log($"[{gameObject.name}] Building highlight group with {renderers.Length} renderers");

        foreach (var r in renderers)
        {
            // Skip if renderer is null
            if (r == null)
            {
                Debug.LogWarning($"[{gameObject.name}] Null renderer found, skipping");
                continue;
            }

            // Skip invisible / helper renderers if needed
            if (r is ParticleSystemRenderer)
            {
                Debug.Log($"[{gameObject.name}] Skipping ParticleSystemRenderer on {r.gameObject.name}");
                continue;
            }

            Outline outline = r.GetComponent<Outline>();
            if (outline == null)
            {
                outline = r.gameObject.AddComponent<Outline>();
                Debug.Log($"[{gameObject.name}] Added Outline component to {r.gameObject.name}");
            }

            outline.color = outlineColorIndex;
            outline.enabled = false;
            outline.eraseRenderer = true;

            outlines.Add(outline);
        }
        
        Debug.Log($"[{gameObject.name}] Highlight group built with {outlines.Count} outlines");
    }

    public void EnableHighlight()
    {
        if (!highlightingEnabled)
        {
            Debug.Log($"[{gameObject.name}] Highlighting disabled, skipping EnableHighlight");
            return;
        }

        // Lazy initialization if needed
        if (!isInitialized || outlines.Count == 0)
        {
            Debug.LogWarning($"[{gameObject.name}] Outlines not initialized, rebuilding group");
            BuildGroup();
            isInitialized = true;
        }

        int enabledCount = 0;
        foreach (var o in outlines)
        {
            if (o == null)
            {
                Debug.LogWarning($"[{gameObject.name}] Null outline found in EnableHighlight");
                continue;
            }
            o.color = outlineColorIndex;
            o.eraseRenderer = false;
            o.enabled = true;
            enabledCount++;
        }
        
        Debug.Log($"[{gameObject.name}] Enabled {enabledCount} outlines");
    }

    public void DisableHighlight()
    {
        if (!highlightingEnabled)
            return;

        int disabledCount = 0;
        foreach (var o in outlines)
        {
            if (o == null) continue;
            o.eraseRenderer = true;
            o.enabled = false;
            disabledCount++;
        }
        
        Debug.Log($"[{gameObject.name}] Disabled {disabledCount} outlines");
    }

    public string GetTooltipText()
    {
        if (string.IsNullOrEmpty(description))
            return displayName;

        return $"{displayName}\n<color=#AAAAAA>{description}</color>";
    }

    public void SetHighlightingEnabled(bool enabled)
    {
        highlightingEnabled = enabled;

        if (!enabled)
        {
            // Force-disable outlines immediately
            foreach (var o in outlines)
            {
                if (o == null) continue;
                o.eraseRenderer = true;
                o.enabled = false;
            }
        }
    }

    // Editor utility to manually rebuild
    [ContextMenu("Force Rebuild Group")]
    public void ForceRebuild()
    {
        BuildGroup();
        Debug.Log($"[{gameObject.name}] Force rebuild complete");
    }

    // Validation method
    // private void OnValidate()
    // {
    //     // Only rebuild in editor, not during play
    //     if (!Application.isPlaying)
    //     {
    //         // Delay to avoid issues during serialization
    //         UnityEditor.EditorApplication.delayCall += () =>
    //         {
    //             if (this != null && gameObject != null)
    //                 BuildGroup();
    //         };
    //     }
    // }
}