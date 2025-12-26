using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Demonstrates physics-based movement using Rigidbody.velocity
/// This is more realistic than directly changing transform.position
/// </summary>
public class PhysicsMover : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How fast the object moves (units per second)")]
    public float moveSpeed = 5f;
    
    [Tooltip("Maximum speed the object can reach")]
    public float maxSpeed = 10f;
    
    [Header("Physics Settings")]
    [Tooltip("Should we use custom gravity instead of Unity's built-in?")]
    public bool useCustomGravity = false;
    
    [Tooltip("Custom gravity strength (negative pulls down)")]
    public float customGravityStrength = -9.81f;
    
    // Reference to the Rigidbody component
    private Rigidbody rb;
    
    void Start()
    {
        // Get the Rigidbody component attached to this GameObject
        rb = GetComponent<Rigidbody>();
        
        // Safety check - make sure Rigidbody exists!
        if (rb == null)
        {
            Debug.LogError("PhysicsMover requires a Rigidbody component!");
            enabled = false; // Disable this script
            return;
        }
        
        // If using custom gravity, disable Unity's built-in gravity
        if (useCustomGravity)
        {
            rb.useGravity = false;
        }
    }
    
    void FixedUpdate()
    {
        // Apply custom gravity if enabled
        if (useCustomGravity)
        {
            ApplyCustomGravity();
        }
        
        // Handle movement input
        HandleMovement();
        
        // Limit maximum speed
        ClampVelocity();
    }
    
    void ApplyCustomGravity()
    {
        // Gravity force = mass × gravity strength
        Vector3 gravityForce = Vector3.up * customGravityStrength * rb.mass;
        rb.AddForce(gravityForce, ForceMode.Force);
    }
    
    void HandleMovement()
    {
        // Safety check for keyboard
        if (Keyboard.current == null) return;
        
        // Read WASD input
        Vector3 moveDirection = Vector3.zero;
        
        if (Keyboard.current.wKey.isPressed)
            moveDirection += Vector3.forward;
        if (Keyboard.current.sKey.isPressed)
            moveDirection += Vector3.back;
        if (Keyboard.current.aKey.isPressed)
            moveDirection += Vector3.left;
        if (Keyboard.current.dKey.isPressed)
            moveDirection += Vector3.right;
        
        // Normalize to prevent faster diagonal movement
        if (moveDirection.magnitude > 0)
        {
            moveDirection.Normalize();
        }
        
        // Change velocity directly (simpler than AddForce for this case)
        Vector3 newVelocity = rb.velocity;
        newVelocity.x = moveDirection.x * moveSpeed;
        newVelocity.z = moveDirection.z * moveSpeed;
        rb.velocity = newVelocity;
    }
    
    void ClampVelocity()
    {
        // Get current velocity
        Vector3 velocity = rb.velocity;
        
        // Clamp horizontal velocity (X and Z) to maxSpeed
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
        if (horizontalVelocity.magnitude > maxSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * maxSpeed;
            rb.velocity = new Vector3(horizontalVelocity.x, velocity.y, horizontalVelocity.z);
        }
    }
}
