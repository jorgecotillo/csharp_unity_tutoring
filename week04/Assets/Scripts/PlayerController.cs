using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Week 4: Basic player movement using Character Controller
/// Handles WASD movement, sprint, and gravity
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Normal walking speed (units per second)")]
    public float walkSpeed = 3f;
    
    [Tooltip("Sprint speed when holding Shift (units per second)")]
    public float sprintSpeed = 6f;
    
    [Header("Gravity Settings")]
    [Tooltip("Downward force when grounded (keeps player stuck to ground)")]
    public float groundedGravity = -2f;
    
    [Header("Debug")]
    [Tooltip("Show debug info in console")]
    public bool showDebugInfo = false;
    
    // Components
    private CharacterController characterController;
    private PlayerInputActions inputActions;
    
    // State
    private float verticalVelocity = 0f;
    private Vector3 moveDirection = Vector3.zero;
    
    void Awake()
    {
        // Get the Character Controller component
        characterController = GetComponent<CharacterController>();
        
        // Safety check
        if (characterController == null)
        {
            Debug.LogError("PlayerController requires a CharacterController component!");
            enabled = false;
            return;
        }
        
        // Create input actions instance
        inputActions = new PlayerInputActions();
    }
    
    void OnEnable()
    {
        // Enable the Player action map
        inputActions.Player.Enable();
    }
    
    void OnDisable()
    {
        // Disable the Player action map (important for preventing memory leaks!)
        inputActions.Player.Disable();
    }
    
    void Update()
    {
        HandleMovement();
        HandleGravity();
        ApplyMovement();
        
        if (showDebugInfo)
        {
            DebugInfo();
        }
    }
    
    /// <summary>
    /// Reads input and calculates horizontal movement direction
    /// </summary>
    private void HandleMovement()
    {
        // 1. Read input from Input Actions
        Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
        bool isSprinting = inputActions.Player.Sprint.IsPressed();
        
        // 2. Convert 2D input to 3D world direction
        // input.y = forward/back (W/S)
        // input.x = left/right (A/D)
        moveDirection = transform.forward * input.y + transform.right * input.x;
        
        // 3. Normalize diagonal movement (prevents moving faster diagonally)
        if (moveDirection.magnitude > 0.1f)
        {
            moveDirection.Normalize();
        }
        
        // 4. Apply speed (walk or sprint)
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
        moveDirection *= currentSpeed;
    }
    
    /// <summary>
    /// Applies gravity to the player
    /// </summary>
    private void HandleGravity()
    {
        if (characterController.isGrounded)
        {
            // When grounded, apply small downward force to keep player "stuck" to ground
            // This prevents the player from bouncing or floating
            verticalVelocity = groundedGravity;
        }
        else
        {
            // When in air, apply gravity (accumulates over time = acceleration)
            // Physics.gravity.y is typically -9.81 (Earth's gravity in m/s²)
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
            
            // Optional: Clamp falling speed to prevent falling infinitely fast
            verticalVelocity = Mathf.Max(verticalVelocity, -50f);
        }
    }
    
    /// <summary>
    /// Applies the calculated movement to the Character Controller
    /// </summary>
    private void ApplyMovement()
    {
        // Combine horizontal movement with vertical velocity
        Vector3 movement = moveDirection * Time.deltaTime;
        movement.y = verticalVelocity * Time.deltaTime;
        
        // Move the character using Character Controller
        // This handles collision detection automatically
        characterController.Move(movement);
    }
    
    /// <summary>
    /// Displays debug information in the console
    /// </summary>
    private void DebugInfo()
    {
        Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
        Debug.Log($"Input: {input} | Grounded: {characterController.isGrounded} | " +
                  $"Vertical Velocity: {verticalVelocity:F2} | " +
                  $"Speed: {moveDirection.magnitude:F2}");
    }
    
    // Public getters for other scripts to access
    public bool IsGrounded => characterController.isGrounded;
    public bool IsMoving => moveDirection.magnitude > 0.1f;
    public float CurrentSpeed => moveDirection.magnitude;
}
