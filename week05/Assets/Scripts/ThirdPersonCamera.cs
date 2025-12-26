using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Week 5: Advanced camera system with mouse look and collision detection
/// Orbits around player, prevents wall clipping, smooth movement
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("The transform to follow (usually the player)")]
    public Transform target;
    
    [Tooltip("Offset for look-at point (aims at player's head, not feet)")]
    public Vector3 lookAtOffset = new Vector3(0, 1.5f, 0);
    
    [Header("Camera Distance")]
    [Tooltip("How far camera is from player")]
    public float distance = 5f;
    
    [Tooltip("How high camera is above player")]
    public float height = 2f;
    
    [Header("Mouse Look Settings")]
    [Tooltip("Mouse sensitivity (higher = faster rotation)")]
    [Range(0.1f, 10f)]
    public float mouseSensitivity = 2f;
    
    [Tooltip("Minimum vertical angle (looking down)")]
    [Range(-89f, 0f)]
    public float minPitch = -80f;
    
    [Tooltip("Maximum vertical angle (looking up)")]
    [Range(0f, 89f)]
    public float maxPitch = 80f;
    
    [Header("Smoothing")]
    [Tooltip("How quickly camera moves to target position")]
    [Range(1f, 20f)]
    public float positionSmoothSpeed = 10f;
    
    [Tooltip("How quickly camera rotates")]
    [Range(1f, 20f)]
    public float rotationSmoothSpeed = 15f;
    
    [Header("Collision Settings")]
    [Tooltip("Layers that block the camera")]
    public LayerMask collisionMask;
    
    [Tooltip("Minimum distance from player (when blocked by wall)")]
    [Range(0.5f, 3f)]
    public float minDistance = 1f;
    
    [Tooltip("Buffer distance from walls (prevents clipping)")]
    [Range(0.1f, 1f)]
    public float collisionOffset = 0.3f;
    
    [Tooltip("Radius of sphere cast for collision (smoother than raycast)")]
    [Range(0.1f, 0.5f)]
    public float collisionRadius = 0.2f;
    
    [Header("Debug")]
    public bool showDebugGizmos = false;
    public bool showDebugLog = false;
    
    // Input
    private PlayerInputActions inputActions;
    
    // Camera rotation state
    private float yaw = 0f;      // Horizontal rotation
    private float pitch = 20f;   // Vertical rotation (start looking slightly down)
    
    // Current positions (for smoothing)
    private Vector3 currentPosition;
    private Quaternion currentRotation;
    
    // Collision state
    private float currentDistance;
    
    void Awake()
    {
        // Initialize input
        inputActions = new PlayerInputActions();
        
        // Initialize current distance
        currentDistance = distance;
        
        // Initialize rotation from current camera rotation
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
        
        // Normalize pitch to -180 to 180 range
        if (pitch > 180) pitch -= 360;
    }
    
    void OnEnable()
    {
        inputActions.Player.Enable();
    }
    
    void OnDisable()
    {
        inputActions.Player.Disable();
    }
    
    void Start()
    {
        // Safety check
        if (target == null)
        {
            Debug.LogError("ThirdPersonCamera: No target assigned!");
            enabled = false;
            return;
        }
        
        // Initialize current position
        currentPosition = transform.position;
        currentRotation = transform.rotation;
        
        // Lock and hide cursor for better camera control
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        HandleMouseLook();
        HandleCameraPosition();
        HandleCameraRotation();
    }
    
    /// <summary>
    /// Reads mouse input and updates camera rotation angles
    /// </summary>
    private void HandleMouseLook()
    {
        // Read mouse delta
        Vector2 mouseDelta = inputActions.Player.Look.ReadValue<Vector2>();
        
        // Apply sensitivity
        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;
        
        // Update rotation angles
        yaw += mouseX;
        pitch -= mouseY;  // Inverted (mouse up = look up)
        
        // Clamp vertical rotation to prevent flipping camera upside down
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        
        if (showDebugLog)
        {
            Debug.Log($"Mouse: ({mouseX:F2}, {mouseY:F2}) | Yaw: {yaw:F1}° | Pitch: {pitch:F1}°");
        }
    }
    
    /// <summary>
    /// Calculates and applies camera position (with collision detection)
    /// </summary>
    private void HandleCameraPosition()
    {
        // Calculate target look-at point
        Vector3 targetPoint = target.position + lookAtOffset;
        
        // Calculate desired camera rotation
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        
        // Calculate offset direction (back and up from target)
        Vector3 direction = rotation * new Vector3(0, height, -distance);
        
        // Calculate desired position
        Vector3 desiredPosition = targetPoint + direction;
        
        // Check for collisions and adjust position
        Vector3 adjustedPosition = CheckCameraCollision(targetPoint, desiredPosition, direction);
        
        // Smoothly move to adjusted position
        currentPosition = Vector3.Lerp(currentPosition, adjustedPosition, positionSmoothSpeed * Time.deltaTime);
        transform.position = currentPosition;
    }
    
    /// <summary>
    /// Checks for obstacles between target and desired camera position
    /// Returns adjusted position if collision detected
    /// </summary>
    private Vector3 CheckCameraCollision(Vector3 targetPoint, Vector3 desiredPosition, Vector3 direction)
    {
        // Calculate distance and direction from target to desired position
        float desiredDistance = direction.magnitude;
        Vector3 directionNormalized = direction.normalized;
        
        // Perform sphere cast from target toward camera
        // (Sphere cast is smoother than raycast for camera collision)
        RaycastHit hit;
        if (Physics.SphereCast(targetPoint, collisionRadius, directionNormalized, 
                               out hit, desiredDistance, collisionMask))
        {
            // Hit something! Calculate safe distance
            float safeDistance = hit.distance - collisionOffset;
            safeDistance = Mathf.Max(safeDistance, minDistance);
            
            // Store current distance for debug
            currentDistance = safeDistance;
            
            // Calculate adjusted position
            Vector3 adjustedPosition = targetPoint + directionNormalized * safeDistance;
            
            if (showDebugLog)
            {
                Debug.Log($"Camera collision with {hit.collider.name} at distance {hit.distance:F2}");
            }
            
            return adjustedPosition;
        }
        else
        {
            // No collision, use desired position
            currentDistance = desiredDistance;
            return desiredPosition;
        }
    }
    
    /// <summary>
    /// Smoothly rotates camera to look at target
    /// </summary>
    private void HandleCameraRotation()
    {
        // Calculate target look-at point
        Vector3 targetPoint = target.position + lookAtOffset;
        
        // Calculate desired rotation (looking at target)
        Quaternion desiredRotation = Quaternion.LookRotation(targetPoint - transform.position);
        
        // Smoothly rotate toward desired rotation
        currentRotation = Quaternion.Slerp(currentRotation, desiredRotation, rotationSmoothSpeed * Time.deltaTime);
        transform.rotation = currentRotation;
    }
    
    /// <summary>
    /// Draws debug visualization in Scene view
    /// </summary>
    void OnDrawGizmos()
    {
        if (!showDebugGizmos || target == null) return;
        
        Vector3 targetPoint = target.position + lookAtOffset;
        
        // Draw line from camera to target
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, targetPoint);
        
        // Draw target look-at point
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(targetPoint, 0.2f);
        
        // Draw collision sphere
        Vector3 direction = (transform.position - targetPoint).normalized;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(targetPoint + direction * currentDistance, collisionRadius);
        
        // Draw desired camera position (without collision)
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 offset = rotation * new Vector3(0, height, -distance);
        Vector3 desiredPos = targetPoint + offset;
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(desiredPos, 0.3f);
    }
}
