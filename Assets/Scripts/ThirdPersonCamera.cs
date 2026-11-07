using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target; // The CameraTarget on player
    [SerializeField] private Vector3 offset = new Vector3(0.5f, 0.3f, -2.5f); // Over-shoulder offset
    [SerializeField] private Vector3 aimOffset = new Vector3(0.3f, 0.2f, -1.5f); // Zoomed in aim offset (NEW)
    [SerializeField] private Vector3 lookAtOffset = new Vector3(0f, 0.2f, 0f); // Slightly above center
    
    [Header("Camera Settings")]
    [SerializeField] private float mouseSensitivity = 3f;
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private float aimSmoothSpeed = 15f; // Faster transition to aim (NEW)
    [SerializeField] private float minVerticalAngle = -30f;
    [SerializeField] private float maxVerticalAngle = 60f;
    
    [Header("Collision Settings")]
    [SerializeField] private bool checkCollision = true;
    [SerializeField] private float collisionOffset = 0.3f;
    [SerializeField] private LayerMask collisionMask;
    
    [Header("References")]
    [SerializeField] private PlayerController playerController; // NEW: Reference to player
    
    // Camera rotation
    private float yaw;
    private float pitch;
    
    // Smooth position
    private Vector3 currentVelocity;
    
    // Aiming state (NEW)
    private bool wasAiming;
    
    void Start()
    {
        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Initialize rotation based on current transform
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
        
        // Find player controller if not assigned (NEW)
        if (playerController == null && target != null)
        {
            playerController = target.GetComponentInParent<PlayerController>();
        }
    }

    void LateUpdate()
    {
        // If inventory is open, unlock cursor and stop camera logic
        if (InventorySystem.Instance != null &&
            InventorySystem.Instance.IsInventoryOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (target == null) return;
        
        HandleCameraRotation();
        HandleCameraPosition();
        
        // Unlock cursor with Escape
        // if (Input.GetKeyDown(KeyCode.Escape))
        // {
        //     Cursor.lockState = CursorLockMode.None;
        //     Cursor.visible = true;
        // }
        
        // Re-lock cursor on click
        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void HandleCameraRotation()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Update rotation
        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
    }

    void HandleCameraPosition()
    {
        // Check if player is aiming (NEW)
        bool isAiming = playerController != null && playerController.IsAiming;
        
        // Calculate desired rotation
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        
        // Choose offset based on aiming state (NEW)
        Vector3 currentOffset = isAiming ? aimOffset : offset;
        float currentSmoothSpeed = isAiming ? aimSmoothSpeed : smoothSpeed;
        
        // Calculate desired position
        Vector3 desiredPosition = target.position + rotation * currentOffset;
        
        // Check for collision
        if (checkCollision)
        {
            Vector3 direction = desiredPosition - target.position;
            float distance = direction.magnitude;
            
            if (Physics.Raycast(target.position, direction.normalized, out RaycastHit hit, distance, collisionMask))
            {
                desiredPosition = hit.point - direction.normalized * collisionOffset;
            }
        }
        
        // Smooth camera movement
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, 1f / currentSmoothSpeed);
        
        // Look at target
        Vector3 lookAtPoint = target.position + lookAtOffset;
        transform.LookAt(lookAtPoint);
        
        // Track aiming state change
        wasAiming = isAiming;
    }
    
    // Public method to set sensitivity (useful for settings menu)
    public void SetSensitivity(float sensitivity)
    {
        mouseSensitivity = sensitivity;
    }
    
    // Public getter for camera forward (useful for shooting) (NEW)
    public Vector3 GetCameraForward()
    {
        return transform.forward;
    }

    public Vector3 ToggleCameraZoom()
    {
        return transform.position;
    }
}