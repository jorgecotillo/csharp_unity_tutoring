using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Week 7: Camera with mouse look, orbit, and collision detection
/// Orbits around player, pulls forward when walls block the view
/// </summary>
public class CameraFollow : MonoBehaviour
{
    // ============================================
    // TARGET SETTINGS
    // ============================================
    
    [Header("Target Settings")]
    [Tooltip("The player to follow")]
    public Transform target;
    
    [Tooltip("Offset for look-at point (look at head, not feet)")]
    public Vector3 lookAtOffset = new Vector3(0, 1.5f, 0);
    
    // ============================================
    // CAMERA DISTANCE
    // ============================================
    
    [Header("Camera Distance")]
    [Tooltip("How far camera is behind player")]
    public float distance = 5f;
    
    [Tooltip("How high camera is above player")]
    public float height = 2f;
    
    // ============================================
    // MOUSE LOOK SETTINGS
    // ============================================
    
    [Header("Mouse Look Settings")]
    [Tooltip("How fast camera rotates with mouse (1-10)")]
    [Range(0.5f, 10f)]
    public float mouseSensitivity = 2f;
    
    [Tooltip("Minimum vertical angle (looking up)")]
    [Range(-89f, 0f)]
    public float minPitch = -60f;
    
    [Tooltip("Maximum vertical angle (looking down)")]
    [Range(0f, 89f)]
    public float maxPitch = 60f;
    
    // ============================================
    // COLLISION SETTINGS
    // ============================================
    
    [Header("Collision Settings")]
    [Tooltip("What layers should block the camera? (walls, ground, etc.)")]
    public LayerMask collisionMask;
    
    [Tooltip("Minimum distance from player when blocked")]
    [Range(0.5f, 2f)]
    public float minDistance = 1f;
    
    [Tooltip("How far to stay from walls (prevents clipping)")]
    [Range(0.1f, 0.5f)]
    public float collisionBuffer = 0.2f;
    
    [Tooltip("Radius of collision sphere (larger = catches corners better)")]
    [Range(0.1f, 0.5f)]
    public float collisionRadius = 0.2f;
    
    // ============================================
    // SMOOTHING
    // ============================================
    
    [Header("Smoothing")]
    [Tooltip("How quickly camera moves to target position")]
    [Range(1f, 20f)]
    public float smoothSpeed = 10f;
    
    // ============================================
    // PRIVATE VARIABLES
    // ============================================
    
    private PlayerInputActions inputActions;
    
    private float yaw = 0f;
    private float pitch = 20f;
    
    // Smooth distance tracking for collision
    private float currentDistance;
    
    // ============================================
    // UNITY LIFECYCLE
    // ============================================
    
    void Awake()
    {
        inputActions = new PlayerInputActions();
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
        if (target == null)
        {
            Debug.LogError("CameraFollow: No target assigned! Drag Player to Target field.");
            enabled = false;
            return;
        }
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        currentDistance = distance;
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        HandleMouseLook();
        UpdateCameraPosition();
    }
    
    // ============================================
    // MOUSE LOOK
    // ============================================
    
    /// <summary>
    /// Reads mouse input and updates rotation angles
    /// </summary>
    private void HandleMouseLook()
    {
        Vector2 mouseDelta = inputActions.Player.Look.ReadValue<Vector2>();
        
        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;
        
        yaw += mouseX;
        pitch -= mouseY;
        
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }
    
    // ============================================
    // CAMERA POSITION WITH COLLISION
    // ============================================
    
    /// <summary>
    /// Positions camera behind player, adjusting for wall collision
    /// </summary>
    private void UpdateCameraPosition()
    {
        // 1. Create rotation from our angles
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        
        // 2. Calculate direction from player to where camera wants to be
        Vector3 offset = new Vector3(0, height, -distance);
        Vector3 direction = (rotation * offset).normalized;
        
        // 3. Check for walls between player and camera
        Vector3 playerHead = target.position + lookAtOffset;
        float safeDistance = GetCollisionAdjustedDistance(playerHead, direction);
        
        // 4. Smooth the distance change (no jarring snaps)
        currentDistance = Mathf.Lerp(currentDistance, safeDistance, smoothSpeed * Time.deltaTime);
        
        // 5. Position camera at the (possibly shortened) distance
        Vector3 desiredPosition = playerHead + direction * currentDistance;
        
        // 6. Smooth position follow
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );
        
        // 7. Always look at the player
        transform.LookAt(playerHead);
    }
    
    // ============================================
    // COLLISION DETECTION
    // ============================================
    
    /// <summary>
    /// Shoots a sphere from the player toward the camera.
    /// If it hits a wall, returns a shorter distance so the camera
    /// stays in front of the wall instead of going through it.
    /// </summary>
    private float GetCollisionAdjustedDistance(Vector3 startPosition, Vector3 direction)
    {
        float maxDist = Mathf.Sqrt(distance * distance + height * height);
        float adjustedDistance = maxDist;
        
        RaycastHit hit;
        
        if (Physics.SphereCast(
            startPosition,
            collisionRadius,
            direction,
            out hit,
            maxDist,
            collisionMask))
        {
            adjustedDistance = hit.distance - collisionBuffer;
            adjustedDistance = Mathf.Max(adjustedDistance, minDistance);
        }
        
        return adjustedDistance;
    }
    
    // ============================================
    // CURSOR HELPERS
    // ============================================
    
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}