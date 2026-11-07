using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class PointClickController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float stoppingDistance = 0.5f;
    [SerializeField] private float rotationSpeed = 10f;
    
    [Header("Input Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private bool runWithShift = true;
    
    [Header("Animation Settings")]
    [SerializeField] private float speedDampTime = 0.1f;
    [SerializeField] private Animator animator;
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject destinationMarker;
    [SerializeField] private float markerLifetime = 1f;
    
    // Components
    private NavMeshAgent agent;
    private Camera mainCamera;
    
    // Animation
    private int speedHash;
    
    // State
    private bool isMoving;
    private bool isRunning;
    private GameObject currentMarker;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        mainCamera = Camera.main;
        
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        
        // Cache animation hash
        speedHash = Animator.StringToHash("Speed");
        
        // Configure NavMeshAgent
        agent.speed = walkSpeed;
        agent.angularSpeed = 360f;
        agent.acceleration = 8f;
        agent.stoppingDistance = stoppingDistance;
    }

    void Start()
    {
        // Make sure cursor is visible and unlocked
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        HandleInput();
        HandleMovement();
        HandleAnimations();
    }

    void HandleInput()
    {
        // Check if shift is held for running
        isRunning = runWithShift && Input.GetKey(KeyCode.LeftShift);
        
        // Update agent speed based on run state
        agent.speed = isRunning ? runSpeed : walkSpeed;
        
        // Left click to move
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayer))
            {
                // Set destination
                agent.SetDestination(hit.point);
                
                // Spawn destination marker
                SpawnDestinationMarker(hit.point);
            }
        }
    }

    void HandleMovement()
    {
        // Check if agent is moving
        if (agent.hasPath && agent.remainingDistance > agent.stoppingDistance)
        {
            isMoving = true;
        }
        else
        {
            isMoving = false;
            
            // Stop agent completely when reached destination
            if (agent.hasPath && agent.remainingDistance <= agent.stoppingDistance)
            {
                agent.ResetPath();
            }
        }
    }

    void HandleAnimations()
    {
        // Calculate speed for animator (0-1 normalized)
        float speed = 0f;

        if (isMoving)
        {
            if (isRunning)
                speed = 1f; // Sprint speed: 0.8-1.0
            else
                speed = 0.5f; // Walk speed: 0.4-0.6
        }

        // Smooth speed transition
        if (animator != null)
        {
            animator.SetFloat(speedHash, speed, speedDampTime, Time.deltaTime);
        }
    }

    void SpawnDestinationMarker(Vector3 position)
    {
        if (destinationMarker == null) return;
        
        // Destroy old marker
        if (currentMarker != null)
        {
            Destroy(currentMarker);
        }
        
        // Spawn new marker
        currentMarker = Instantiate(destinationMarker, position, Quaternion.identity);
        
        // Destroy after lifetime
        Destroy(currentMarker, markerLifetime);
    }

    // Method to make player walk to a specific location (for scripted movement)
    public void WalkToPosition(Vector3 targetPosition)
    {
        agent.SetDestination(targetPosition);
    }

    // Method to make player walk to a specific transform
    public void WalkToTransform(Transform target)
    {
        if (target != null)
        {
            agent.SetDestination(target.position);
        }
    }

    // Check if player has reached destination
    public bool HasReachedDestination()
    {
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
    }

    // Public getters
    public bool IsMoving => isMoving;
    public bool IsRunning => isRunning;
    public Vector3 Destination => agent.destination;
    public float RemainingDistance => agent.remainingDistance;
    
    // Gizmos for debugging
    void OnDrawGizmosSelected()
    {
        if (agent != null && agent.hasPath)
        {
            Gizmos.color = Color.green;
            Vector3[] corners = agent.path.corners;
            
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
            
            // Draw destination
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(agent.destination, 0.5f);
        }
    }
}