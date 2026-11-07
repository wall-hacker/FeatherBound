using UnityEngine;
using System.Collections;
using SunTemple;

[RequireComponent(typeof(Collider))]
public class DoorwayTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Door door; // Using SunTemple.Door
    [SerializeField] private PointClickController player;
    [SerializeField] private Transform insidePosition; // Where player walks when entering
    [SerializeField] private Transform outsidePosition; // Where player walks when exiting
    
    [Header("Settings")]
    [SerializeField] private float doorCloseDelay = 0.1f; // Time to wait before closing door
    [SerializeField] private bool autoFindPlayer = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    [Header("Camera & GUI")]
    [SerializeField] private Transform cameraTarget;        // Where camera should move when entering
    [SerializeField] private float cameraLerpTime = 1f;     // Duration of camera move
    [SerializeField] private Camera mainCamera;             // Main camera
    [SerializeField] private Canvas guiCanvas;             // GUI Canvas
    [SerializeField] private Canvas shopCanvas;             // Shop Canvas
    [SerializeField] private MouseHighlighter mouseHighlighter; // To disable highlights

    [Header("Optional")]
    [SerializeField] private bool isBirdShop = false;
    [SerializeField] private bool isWorkShop = false;
    
    // State tracking
    private enum DoorwayState { Idle, Entering, Exiting }
    private DoorwayState currentState = DoorwayState.Idle;
    private bool isPlayerInside = false;
    private Coroutine activeSequence;

    void Start()
    {
        if (shopCanvas != null) shopCanvas.gameObject.SetActive(false);
        Debug.Log($"[DoorwayTrigger] Starting on {gameObject.name}");
        
        // Auto-find player if not assigned
        if (autoFindPlayer && player == null)
        {
            player = FindObjectOfType<PointClickController>();
            if (player != null)
            {
                Debug.Log($"[DoorwayTrigger] Found player: {player.gameObject.name}");
            }
            else
            {
                Debug.LogError($"[DoorwayTrigger] Could not find PointClickController in scene!");
            }
        }
        
        // Make sure trigger is set up correctly
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"[DoorwayTrigger] No collider found on {gameObject.name}!");
            return;
        }
        
        if (!col.isTrigger)
        {
            Debug.LogWarning($"[DoorwayTrigger] Collider on {gameObject.name} is not set as trigger. Setting it now.");
            col.isTrigger = true;
        }
        else
        {
            Debug.Log($"[DoorwayTrigger] Trigger collider properly set on {gameObject.name}");
        }
        
        // Validate references
        if (door == null)
            Debug.LogWarning($"[DoorwayTrigger] Door reference is missing on {gameObject.name}!");
        else
            Debug.Log($"[DoorwayTrigger] Door reference set: {door.gameObject.name}");
            
        if (insidePosition == null)
            Debug.LogError($"[DoorwayTrigger] Inside Position is missing on {gameObject.name}!");
        else
            Debug.Log($"[DoorwayTrigger] Inside Position set: {insidePosition.name}");
            
        if (outsidePosition == null)
            Debug.LogError($"[DoorwayTrigger] Outside Position is missing on {gameObject.name}!");
        else
            Debug.Log($"[DoorwayTrigger] Outside Position set: {outsidePosition.name}");
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[DoorwayTrigger] Trigger entered by: {other.gameObject.name}");
        
        // Check if it's the player
        PointClickController playerComponent = other.GetComponent<PointClickController>();
        
        if (playerComponent != null)
        {
            Debug.Log($"[DoorwayTrigger] Player detected! isPlayerInside: {isPlayerInside}, currentState: {currentState}");
            
            // Determine if player is entering or exiting based on current state
            if (!isPlayerInside && currentState == DoorwayState.Idle)
            {
                Debug.Log("[DoorwayTrigger] Starting ENTER sequence");
                // Player is entering building
                StartEnterSequence();
            }
            else if (isPlayerInside && currentState == DoorwayState.Idle)
            {
                Debug.Log("[DoorwayTrigger] Starting EXIT sequence");
                // Player is exiting building
                StartExitSequence();
            }
            else
            {
                Debug.Log($"[DoorwayTrigger] Cannot trigger: isPlayerInside={isPlayerInside}, state={currentState}");
            }
        }
        else
        {
            Debug.Log($"[DoorwayTrigger] Not player, has PointClickController: {(other.GetComponent<PointClickController>() != null)}");
        }
    }

    void StartEnterSequence()
    {
        if (currentState != DoorwayState.Idle) return;
        
        if (activeSequence != null)
            StopCoroutine(activeSequence);

        if (isBirdShop)
            BirdShop.Instance.UpdateUI();

        if (isWorkShop)
            CraftingSystem.Instance.PopulateRecipeList();
        
        activeSequence = StartCoroutine(EnterSequence());
    }

    void StartExitSequence()
    {
        if (currentState != DoorwayState.Idle) return;
        
        if (activeSequence != null)
            StopCoroutine(activeSequence);
        
        activeSequence = StartCoroutine(ExitSequence());
    }

    IEnumerator EnterSequence()
    {
        currentState = DoorwayState.Entering;
        Debug.Log("[DoorwayTrigger] Player entering building...");
        
        // Step 1: Open the door (using SunTemple Door script)
        if (door != null)
        {
            // Make sure door is closed before opening
            if (!door.DoorClosed)
            {
                door.Activate(); // Close it first
                yield return new WaitForSeconds((door.LerpTime - 0.2f) / door.RotationSpeed);
            }
            
            // Now open the door
            if (door.DoorClosed)
            {
                door.Activate(); // Open
                
                // Wait for door to open (calculate duration from Door's settings)
                float openDuration = 0.8f / door.RotationSpeed;
                yield return new WaitForSeconds(openDuration);
            }
        }
        
        // Step 2: Make player walk to inside position
        if (player != null && insidePosition != null)
        {
            player.WalkToTransform(insidePosition);
            
            // Wait for player to reach destination
            yield return new WaitForSeconds(0.1f); // Small delay to ensure path is calculated
            
            while (!player.HasReachedDestination())
            {
                yield return null;
            }
        }
        
        // Step 3: Wait a moment before closing door
        yield return new WaitForSeconds(doorCloseDelay);
        
        // Step 4: Close the door
        if (door != null && !door.DoorClosed)
        {
            door.Activate(); // Close
            
            // Wait for door to close
            float closeDuration = 0.8f / door.RotationSpeed;
            yield return new WaitForSeconds(closeDuration);
        }
        
        // Update state
        isPlayerInside = true;
        currentState = DoorwayState.Idle;
        activeSequence = null;
        
        Debug.Log("[DoorwayTrigger] Player successfully entered building.");

        // --- DISABLE INTERACTABLES ---
        if (player != null) {
            player.enabled = false;
            player.gameObject.SetActive(false);
        }
        if (guiCanvas != null) guiCanvas.gameObject.SetActive(false);
        if (shopCanvas != null) shopCanvas.gameObject.SetActive(true);
        if (mainCamera != null) mainCamera.GetComponent<IsometricCamera>().enabled = false;
        if (mouseHighlighter != null) mouseHighlighter.enabled = false;

        // --- LERP CAMERA ---
        if (mainCamera != null && cameraTarget != null)
        {
            Vector3 startPos = mainCamera.transform.position;
            Quaternion startRot = mainCamera.transform.rotation;
            Vector3 endPos = cameraTarget.position;
            Quaternion endRot = cameraTarget.rotation;

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / cameraLerpTime;
                mainCamera.transform.position = Vector3.Lerp(startPos, endPos, t);
                mainCamera.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }
        }
    }

    IEnumerator ExitSequence()
    {
        // --- LERP CAMERA BACK ---
        if (mainCamera != null && cameraTarget != null)
        {
            Vector3 startPos = mainCamera.transform.position;
            Quaternion startRot = mainCamera.transform.rotation;
            Vector3 endPos = cameraTarget.position; // Set this to your original camera position (store at Start)
            Quaternion endRot = cameraTarget.rotation; // Store initial rotation at Start

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / cameraLerpTime;
                mainCamera.transform.position = Vector3.Lerp(startPos, endPos, t);
                mainCamera.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }
        }

        // --- RE-ENABLE INTERACTABLES ---
        if (player != null) {
            player.enabled = true;
            player.gameObject.SetActive(true);
        }
        if (guiCanvas != null) guiCanvas.gameObject.SetActive(true);
        if (shopCanvas != null) shopCanvas.gameObject.SetActive(false);
        if (mainCamera != null) mainCamera.GetComponent<IsometricCamera>().enabled = true;
        if (mouseHighlighter != null) mouseHighlighter.enabled = true;

        currentState = DoorwayState.Exiting;
        Debug.Log("[DoorwayTrigger] Player exiting building...");
        
        // Step 1: Open the door
        if (door != null)
        {
            // Make sure door is closed before opening
            if (!door.DoorClosed)
            {
                door.Activate(); // Close it first
                yield return new WaitForSeconds(0.8f / door.RotationSpeed);
            }
            
            // Now open the door
            if (door.DoorClosed)
            {
                door.Activate(); // Open
                
                // Wait for door to open
                float openDuration = 0.8f / door.RotationSpeed;
                yield return new WaitForSeconds(openDuration);
            }
        }
        
        // Step 2: Make player walk to outside position
        if (player != null && outsidePosition != null)
        {
            player.WalkToTransform(outsidePosition);
            
            // Wait for player to reach destination
            yield return new WaitForSeconds(0.1f); // Small delay to ensure path is calculated
            
            while (!player.HasReachedDestination())
            {
                yield return null;
            }
        }
        
        // Step 3: Wait a moment before closing door
        yield return new WaitForSeconds(doorCloseDelay);
        
        // Step 4: Close the door
        if (door != null && !door.DoorClosed)
        {
            door.Activate(); // Close
            
            // Wait for door to close
            float closeDuration = 0.8f / door.RotationSpeed;
            yield return new WaitForSeconds(closeDuration);
        }
        
        // Update state
        isPlayerInside = false;
        currentState = DoorwayState.Idle;
        activeSequence = null;
        
        Debug.Log("[DoorwayTrigger] Player successfully exited building.");
    }

    // Public methods to manually trigger sequences
    public void ForceEnter()
    {
        if (currentState == DoorwayState.Idle && !isPlayerInside)
        {
            StartEnterSequence();
        }
    }

    public void ForceExit()
    {
        if (currentState == DoorwayState.Idle && isPlayerInside)
        {
            StartExitSequence();
        }
    }

    // Public getters
    public bool IsPlayerInside => isPlayerInside;
    public bool IsIdle => currentState == DoorwayState.Idle;

    // Debug visualization
    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        // Draw trigger area
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            if (col is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.matrix = Matrix4x4.identity;
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
            }
        }
        
        // Draw inside position
        if (insidePosition != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(insidePosition.position, 0.3f);
            Gizmos.DrawLine(transform.position, insidePosition.position);
            
            // Draw label
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(insidePosition.position, "Inside");
            #endif
        }
        
        // Draw outside position
        if (outsidePosition != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(outsidePosition.position, 0.3f);
            Gizmos.DrawLine(transform.position, outsidePosition.position);
            
            // Draw label
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(outsidePosition.position, "Outside");
            #endif
        }
        
        // Draw door connection
        if (door != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, door.transform.position);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        // Draw detailed info when selected
        if (insidePosition != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(insidePosition.position, 0.5f);
        }
        
        if (outsidePosition != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(outsidePosition.position, 0.5f);
        }
    }
}