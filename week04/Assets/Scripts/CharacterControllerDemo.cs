using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Demo script showing Character Controller movement with instant response.
/// This demonstrates WHY we use Character Controller for player characters!
/// 
/// What you'll notice:
/// - Character stops INSTANTLY when you release keys (no sliding!)
/// - Precise control - stops exactly where you want
/// - Feels "tight" and responsive
/// - Doesn't get pushed by other objects (you're in control!)
/// </summary>
public class CharacterControllerDemo : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How fast the character moves")]
    public float moveSpeed = 5f;
    
    [Header("Gravity Settings")]
    [Tooltip("Gravity force (Character Controller needs manual gravity!)")]
    public float gravity = -9.81f;
    
    // Reference to Character Controller component
    private CharacterController controller;
    
    // Vertical velocity for gravity
    private float verticalVelocity = 0f;
    
    // Input from WASD keys
    private Vector2 moveInput;
    
    void Start()
    {
        // Get the Character Controller component
        controller = GetComponent<CharacterController>();
        
        Debug.Log("CHARACTER CONTROLLER DEMO: Use WASD to move. Notice the INSTANT STOP when you release keys!");
    }
    
    void Update()
    {
        // Read input from WASD keys
        moveInput.x = 0;
        moveInput.y = 0;
        
        if (Keyboard.current.wKey.isPressed) moveInput.y = 1;
        if (Keyboard.current.sKey.isPressed) moveInput.y = -1;
        if (Keyboard.current.aKey.isPressed) moveInput.x = -1;
        if (Keyboard.current.dKey.isPressed) moveInput.x = 1;
        
        // Handle Movement
        HandleMovement();
        
        // Handle Gravity
        HandleGravity();
    }
    
    void HandleMovement()
    {
        // Calculate movement direction
        Vector3 moveDirection = new Vector3(moveInput.x, 0, moveInput.y);
        
        // Normalize to prevent faster diagonal movement
        if (moveDirection.magnitude > 0.1f)
        {
            moveDirection.Normalize();
        }
        
        // Calculate movement vector
        Vector3 movement = moveDirection * moveSpeed * Time.deltaTime;
        
        // Add vertical velocity (gravity)
        movement.y = verticalVelocity * Time.deltaTime;
        
        // Move the character
        controller.Move(movement);
        
        // ✅ NOTICE: When you release keys, movement is ZERO instantly!
        // No momentum, no sliding, INSTANT STOP!
        // This is what makes shooters feel good!
    }
    
    void HandleGravity()
    {
        // Check if on ground
        if (controller.isGrounded)
        {
            // Small downward force to keep grounded
            verticalVelocity = -2f;
        }
        else
        {
            // Apply gravity
            verticalVelocity += gravity * Time.deltaTime;
        }
    }
    
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Character Controller does NOT get pushed automatically
        // You have full control - only physics when YOU decide!
        Debug.Log("Character Controller touched: " + hit.gameObject.name + " (but wasn't pushed!)");
    }
}
