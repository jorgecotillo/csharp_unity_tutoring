using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Week 5: Enhanced player controller that rotates with camera direction
/// Combines Week 4 movement with camera-relative controls
/// </summary>
public class PlayerControllerWeek5 : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Normal walking speed")]
    public float walkSpeed = 3f;
    
    [Tooltip("Sprint speed when holding Shift")]
    public float sprintSpeed = 6f;
    
    [Tooltip("How fast player rotates to face movement direction")]
    [Range(1f, 20f)]
    public float rotationSpeed = 10f;
    
    [Header("Gravity Settings")]
    [Tooltip("Downward force when grounded")]
    public float groundedGravity = -2f;
    
    [Header("Camera Reference")]
    [Tooltip("Reference to camera transform for camera-relative movement")]
    public Transform cameraTransform;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    public bool showDebugGizmos = false;
    
    // Components
    private CharacterController characterController;
    private PlayerInputActions inputActions;
    
    // Movement state
    private float verticalVelocity = 0f;
    private Vector3 moveDirection = Vector3.zero;
    private Vector3 lastMoveDirection = Vector3.zero;
    
    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        
        if (characterController == null)
        {
            Debug.LogError("PlayerControllerWeek5 requires a CharacterController component!");
            enabled = false;
            return;
        }
        
        inputActions = new PlayerInputActions();
        
        // Auto-find camera if not assigned
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main?.transform;
            if (cameraTransform != null)
            {
                Debug.Log("Auto-assigned Main Camera");
            }
        }
    }
    
    void OnEnable()
    {
        inputActions.Player.Enable();
    }
    
    void OnDisable()
    {
        inputActions.Player.Disable();
    }
    
    void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleGravity();
        ApplyMovement();
        
        if (showDebugInfo)
        {
            DebugInfo();
        }
    }
    
    /// <summary>
    /// Reads input and calculates movement direction relative to camera
    /// </summary>
    private void HandleMovement()
    {
        // Read input
        Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
        bool isSprinting = inputActions.Player.Sprint.IsPressed();
        
        // If no input, no movement
        if (input.magnitude < 0.1f)
        {
            moveDirection = Vector3.zero;
            return;
        }
        
        // Get camera forward and right (flattened to horizontal plane)
        Vector3 cameraForward = GetCameraForward();
        Vector3 cameraRight = GetCameraRight();
        
        // Calculate movement direction relative to camera
        moveDirection = cameraForward * input.y + cameraRight * input.x;
        
        // Normalize diagonal movement
        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }
        
        // Store for rotation
        if (moveDirection.magnitude > 0.1f)
        {
            lastMoveDirection = moveDirection;
        }
        
        // Apply speed
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
        moveDirection *= currentSpeed;
    }
    
    /// <summary>
    /// Rotates player to face movement direction
    /// </summary>
    private void HandleRotation()
    {
        // Only rotate if moving
        if (lastMoveDirection.magnitude < 0.1f) return;
        
        // Calculate target rotation (facing movement direction)
        Quaternion targetRotation = Quaternion.LookRotation(lastMoveDirection);
        
        // Smoothly rotate toward target
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
    
    /// <summary>
    /// Applies gravity
    /// </summary>
    private void HandleGravity()
    {
        if (characterController.isGrounded)
        {
            verticalVelocity = groundedGravity;
        }
        else
        {
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
            verticalVelocity = Mathf.Max(verticalVelocity, -50f);  // Terminal velocity
        }
    }
    
    /// <summary>
    /// Applies movement to Character Controller
    /// </summary>
    private void ApplyMovement()
    {
        Vector3 movement = moveDirection * Time.deltaTime;
        movement.y = verticalVelocity * Time.deltaTime;
        
        characterController.Move(movement);
    }
    
    /// <summary>
    /// Gets camera forward direction projected onto horizontal plane
    /// </summary>
    private Vector3 GetCameraForward()
    {
        if (cameraTransform == null)
        {
            return transform.forward;  // Fallback
        }
        
        // Get camera forward
        Vector3 forward = cameraTransform.forward;
        
        // Project onto horizontal plane (remove Y component)
        forward.y = 0;
        
        // Normalize
        if (forward.magnitude > 0.001f)
        {
            forward.Normalize();
        }
        else
        {
            // Camera is looking straight down/up, use camera's right as fallback
            forward = cameraTransform.right;
            forward.y = 0;
            forward.Normalize();
        }
        
        return forward;
    }
    
    /// <summary>
    /// Gets camera right direction projected onto horizontal plane
    /// </summary>
    private Vector3 GetCameraRight()
    {
        if (cameraTransform == null)
        {
            return transform.right;  // Fallback
        }
        
        // Get camera right
        Vector3 right = cameraTransform.right;
        
        // Project onto horizontal plane
        right.y = 0;
        right.Normalize();
        
        return right;
    }
    
    /// <summary>
    /// Debug logging
    /// </summary>
    private void DebugInfo()
    {
        Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
        Debug.Log($"Input: {input} | Move Dir: {moveDirection} | " +
                  $"Grounded: {characterController.isGrounded} | " +
                  $"Speed: {moveDirection.magnitude:F2}");
    }
    
    /// <summary>
    /// Debug gizmos
    /// </summary>
    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        // Draw movement direction
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position + Vector3.up, moveDirection.normalized * 2f);
        
        // Draw camera forward (flattened)
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position + Vector3.up, GetCameraForward() * 1.5f);
        
        // Draw camera right (flattened)
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + Vector3.up, GetCameraRight() * 1.5f);
    }
    
    // Public properties
    public bool IsGrounded => characterController.isGrounded;
    public bool IsMoving => moveDirection.magnitude > 0.1f;
    public float CurrentSpeed => moveDirection.magnitude;
    public Vector3 Velocity => moveDirection;
}
