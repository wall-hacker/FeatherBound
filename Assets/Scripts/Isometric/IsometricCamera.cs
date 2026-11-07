using UnityEngine;

public class IsometricCamera : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target; // The player to follow
    [SerializeField] private Vector3 offset = new Vector3(0f, 10f, -10f); // Default 45-degree angle offset
    
    [Header("Camera Settings")]
    [SerializeField] private float followSpeed = 10f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private bool lockRotation = true; // Keep camera at fixed angle
    
    [Header("Angle Presets")]
    [SerializeField] private CameraAngle cameraAngle = CameraAngle.Isometric45;
    [SerializeField] private float distance = 15f; // Distance from target
    [SerializeField] private float height = 10f; // Height above target
    
    [Header("Optional: Mouse Rotation")]
    [SerializeField] private bool allowMouseRotation = false;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private KeyCode rotateLeftKey = KeyCode.Q;
    [SerializeField] private KeyCode rotateRightKey = KeyCode.E;
    [SerializeField] private float keyboardRotationSpeed = 90f; // Degrees per second
    
    [Header("Zoom Settings")]
    [SerializeField] private bool allowZoom = true;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 25f;
    
    // Camera angle presets
    public enum CameraAngle
    {
        Isometric45,      // 45 degrees (classic isometric)
        TopDown,          // 90 degrees (directly above)
        Isometric60,      // 60 degrees (steeper angle)
        ThreeQuarter30    // 30 degrees (shallower angle)
    }
    
    // Internal variables
    private float currentYaw = 0f;
    private float currentZoom;
    private Vector3 currentVelocity;

    void Start()
    {
        // Don't lock cursor for isometric view
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }
        
        // Initialize zoom
        currentZoom = distance;
        
        // Calculate initial offset based on preset
        UpdateOffsetFromPreset();
        
        // Set initial position and rotation
        if (target != null)
        {
            transform.position = target.position + offset;
            transform.LookAt(target.position);
        }
    }

    void LateUpdate()
    {
        if (target == null) return;
        
        HandleInput();
        HandleCameraPosition();
        HandleCameraRotation();
    }

    void HandleInput()
    {
        // Keyboard rotation
        if (allowMouseRotation)
        {
            if (Input.GetKey(rotateLeftKey))
            {
                currentYaw -= keyboardRotationSpeed * Time.deltaTime;
            }
            if (Input.GetKey(rotateRightKey))
            {
                currentYaw += keyboardRotationSpeed * Time.deltaTime;
            }
            
            // Mouse rotation (middle mouse button)
            if (Input.GetMouseButton(2))
            {
                float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
                currentYaw += mouseX;
            }
        }
        
        // Zoom with mouse wheel
        if (allowZoom)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                currentZoom -= scroll * zoomSpeed;
                currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
            }
        }
    }

    void HandleCameraPosition()
    {
        // Calculate offset based on current settings
        Vector3 targetOffset = CalculateOffset();
        
        // Calculate desired position
        Vector3 desiredPosition = target.position + targetOffset;
        
        // Smooth follow
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, 1f / followSpeed);
    }

    void HandleCameraRotation()
    {
        if (lockRotation && !allowMouseRotation)
        {
            // Fixed rotation - always look at target from same angle
            transform.LookAt(target.position);
        }
        else if (allowMouseRotation)
        {
            // Rotate around target while maintaining angle
            Vector3 lookPosition = target.position;
            lookPosition.y = transform.position.y - (height * 0.5f); // Look slightly below for better view
            
            Quaternion targetRotation = Quaternion.LookRotation(lookPosition - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    Vector3 CalculateOffset()
    {
        // Get the angle settings
        float angle = GetAngleFromPreset();
        float verticalDistance = currentZoom * Mathf.Sin(angle * Mathf.Deg2Rad);
        float horizontalDistance = currentZoom * Mathf.Cos(angle * Mathf.Deg2Rad);
        
        // Calculate offset with optional rotation
        Quaternion rotation = Quaternion.Euler(0, currentYaw, 0);
        Vector3 offset = rotation * new Vector3(0, verticalDistance, -horizontalDistance);
        
        return offset;
    }

    float GetAngleFromPreset()
    {
        switch (cameraAngle)
        {
            case CameraAngle.TopDown:
                return 90f;
            case CameraAngle.Isometric60:
                return 60f;
            case CameraAngle.Isometric45:
                return 45f;
            case CameraAngle.ThreeQuarter30:
                return 30f;
            default:
                return 45f;
        }
    }

    void UpdateOffsetFromPreset()
    {
        float angle = GetAngleFromPreset();
        float verticalDistance = distance * Mathf.Sin(angle * Mathf.Deg2Rad);
        float horizontalDistance = distance * Mathf.Cos(angle * Mathf.Deg2Rad);
        
        offset = new Vector3(0, verticalDistance, -horizontalDistance);
        height = verticalDistance;
    }

    // Public methods to change camera angle at runtime
    public void SetCameraAngle(CameraAngle angle)
    {
        cameraAngle = angle;
        UpdateOffsetFromPreset();
    }

    public void SetDistance(float dist)
    {
        distance = dist;
        currentZoom = dist;
        UpdateOffsetFromPreset();
    }

    public void ResetRotation()
    {
        currentYaw = 0f;
    }

    // Gizmos for debugging
    void OnDrawGizmosSelected()
    {
        if (target == null) return;
        
        // Draw line from camera to target
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, target.position);
        
        // Draw camera frustum direction
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * 5f);
        
        // Draw target sphere
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(target.position, 0.5f);
        
        // Draw zoom range
        Gizmos.color = Color.blue;
        Vector3 minZoomPos = target.position + CalculateOffsetAtDistance(minZoom);
        Vector3 maxZoomPos = target.position + CalculateOffsetAtDistance(maxZoom);
        Gizmos.DrawWireSphere(minZoomPos, 0.3f);
        Gizmos.DrawWireSphere(maxZoomPos, 0.3f);
    }

    Vector3 CalculateOffsetAtDistance(float dist)
    {
        float angle = GetAngleFromPreset();
        float verticalDistance = dist * Mathf.Sin(angle * Mathf.Deg2Rad);
        float horizontalDistance = dist * Mathf.Cos(angle * Mathf.Deg2Rad);
        return new Vector3(0, verticalDistance, -horizontalDistance);
    }
}