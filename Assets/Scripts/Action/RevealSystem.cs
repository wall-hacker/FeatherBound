using UnityEngine;
using System.Collections.Generic;

public class RevealSystem : MonoBehaviour
{
    public static RevealSystem Instance;

    [Header("Input")]
    [SerializeField] private KeyCode revealKey = KeyCode.V;
    
    [Header("Reveal Settings")]
    [SerializeField] private float baseRevealRadius = 200f;
    [SerializeField] private float revealRadius = 200f; // is modified by buffs/debuffs
    [SerializeField] private float expansionSpeed = 50f; // How fast the sphere expands
    [SerializeField] private float revealDuration = 30f; // How long outlines stay visible
    [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    [Header("Opacity Settings")]
    [SerializeField] private float maxOpacity = 1f; // Closest objects
    [SerializeField] private float minOpacity = 0.3f; // Farthest objects
    
    [Header("References")]
    [SerializeField] private Transform player; // Center of reveal
    
    [Header("Visual Feedback (Optional)")]
    [SerializeField] private GameObject revealEffectPrefab; // Expanding sphere VFX
    
    [Header("Debug")]
    [SerializeField] private bool showDebugSphere = true;
    
    private bool isRevealing = false;
    private float currentRadius = 0f;
    private List<RevealableObject> revealedObjects = new List<RevealableObject>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (player == null)
        {
            // Try to find player
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
                player = transform; // Fallback to this object
        }
    }

    void Update()
    {
        // Check for reveal input
        if (Input.GetKeyDown(revealKey) && !isRevealing)
        {
            StartReveal();
        }
    }

    void StartReveal()
    {
        Debug.Log("[RevealSystem] Starting reveal...");
        
        isRevealing = true;
        currentRadius = 0f;
        revealedObjects.Clear();
        
        // Spawn visual effect if available
        if (revealEffectPrefab != null && player != null)
        {
            GameObject effect = Instantiate(revealEffectPrefab, player.position, Quaternion.identity);
            Destroy(effect, revealDuration + 1f);
        }
        
        StartCoroutine(RevealCoroutine());
    }

    System.Collections.IEnumerator RevealCoroutine()
    {
        // Phase 1: Expand sphere and reveal objects
        while (currentRadius < revealRadius)
        {
            currentRadius += expansionSpeed * Time.deltaTime;
            currentRadius = Mathf.Min(currentRadius, revealRadius);
            
            // Find all RevealableObjects in expanding radius
            FindAndRevealObjects();
            
            yield return null;
        }
        
        Debug.Log($"[RevealSystem] Revealed {revealedObjects.Count} objects");
        
        // Phase 2: Hold at full reveal
        float holdTime = revealDuration * 0.3f; // Hold for 30% of duration
        yield return new WaitForSeconds(holdTime);
        
        // Phase 3: Fade out all revealed objects
        float fadeStartTime = Time.time;
        float fadeDuration = revealDuration * 0.7f; // Fade for 70% of duration
        
        while (Time.time - fadeStartTime < fadeDuration)
        {
            float t = (Time.time - fadeStartTime) / fadeDuration;
            float fadeValue = fadeOutCurve.Evaluate(t);
            
            // Update all revealed objects with fade value
            foreach (var obj in revealedObjects)
            {
                if (obj != null)
                {
                    obj.UpdateFade(fadeValue);
                }
            }
            
            yield return null;
        }
        
        // Phase 4: Disable all outlines
        foreach (var obj in revealedObjects)
        {
            if (obj != null)
            {
                obj.DisableReveal();
            }
        }
        
        revealedObjects.Clear();
        isRevealing = false;
        currentRadius = 0f;
        
        Debug.Log("[RevealSystem] Reveal complete");
    }

    void FindAndRevealObjects()
    {
        if (player == null) return;
        
        // Find all RevealableObjects in the scene
        RevealableObject[] allObjects = FindObjectsOfType<RevealableObject>();
        
        foreach (var obj in allObjects)
        {
            // Skip if already revealed
            if (revealedObjects.Contains(obj))
                continue;
            
            float distance = Vector3.Distance(player.position, obj.transform.position);
            
            // Check if within current expanding radius
            if (distance <= currentRadius)
            {
                // Reveal the object
                obj.Reveal(1f);
                revealedObjects.Add(obj);
            }
        }
    }

    // Gizmos for debugging
    void OnDrawGizmos()
    {
        if (!showDebugSphere || player == null) return;
        
        // Draw max radius
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawWireSphere(player.position, revealRadius);
        
        // Draw current expanding radius when revealing
        if (isRevealing)
        {
            Gizmos.color = new Color(1, 1, 0, 0.5f);
            Gizmos.DrawWireSphere(player.position, currentRadius);
        }
    }

    // Public methods for external control
    public void TriggerReveal()
    {
        if (!isRevealing)
        {
            StartReveal();
        }
    }

    // Buff
    public void SetRevealRadiusBuff(float bonus)
    {
        revealRadius = baseRevealRadius + bonus;
    }

    public bool IsRevealing => isRevealing;
}