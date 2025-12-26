using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Demonstrates force-based movement - more realistic physics simulation
/// Forces accumulate over time creating acceleration and momentum
/// </summary>
public class ForceController : MonoBehaviour
{
    [Header("Force Settings")]
    [Tooltip("How much force to apply when moving")]
    public float movementForce = 10f;
    
    [Tooltip("Maximum speed the object can reach")]
    public float maxSpeed = 8f;
    
    [Header("Jump Settings")]
    [Tooltip("How much upward force for jumping")]
    public float jumpForce = 5f;
    
    [Tooltip("Minimum time between jumps (seconds)")]
    public float jumpCooldown = 0.5f;
    
    [Header("Drag Settings")]
    [Tooltip("How quickly the object slows down (0=no drag, 1=stops instantly)")]
    public float groundDrag = 5f;
    
    [Tooltip("Air drag when not grounded")]
    public float airDrag = 0.5f;
    
    // Private variables
    private Rigidbody rb;
    private bool isGrounded = false;
    private float lastJumpTime = 0f;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (rb == null)
        {
            Debug.LogError("ForceController requires a Rigidbody component!");
            enabled = false;
        }
    }
    
    void Update()
    {
        // Handle jump input in Update (for responsive input)
        HandleJumpInput();
    }
    
    void FixedUpdate()
    {
        // Check if we're on the ground
        CheckGroundStatus();
        
        // Apply movement forces
        HandleMovementForces();
        
        // Limit maximum speed
        LimitSpeed();
        
        // Update drag based on ground status
        UpdateDrag();
    }
    
    void CheckGroundStatus()
    {
        // Raycast downward to check if we're touching the ground
        float rayDistance = 0.6f; // Slightly longer than capsule's half-height
        isGrounded = Physics.Raycast(transform.position, Vector3.down, rayDistance);
        
        // Visual debug - draw the raycast in Scene view
        Debug.DrawRay(transform.position, Vector3.down * rayDistance, 
            isGrounded ? Color.green : Color.red);
    }
    
    void HandleJumpInput()
    {
        if (Keyboard.current == null) return;
        
        // Check if space was pressed this frame
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            // Can we jump? (on ground + cooldown expired)
            bool canJump = isGrounded && (Time.time - lastJumpTime > jumpCooldown);
            
            if (canJump)
            {
                // Apply upward impulse force
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                lastJumpTime = Time.time;
                
                Debug.Log($"Jumped! Force: {jumpForce}");
            }
        }
    }
    
    void HandleMovementForces()
    {
        if (Keyboard.current == null) return;
        
        // Get movement direction from input
        Vector3 moveDirection = Vector3.zero;
        
        if (Keyboard.current.wKey.isPressed)
            moveDirection += Vector3.forward;
        if (Keyboard.current.sKey.isPressed)
            moveDirection += Vector3.back;
        if (Keyboard.current.aKey.isPressed)
            moveDirection += Vector3.left;
        if (Keyboard.current.dKey.isPressed)
            moveDirection += Vector3.right;
        
        // Normalize direction
        if (moveDirection.magnitude > 0)
        {
            moveDirection.Normalize();
            
            // Apply force in the movement direction
            rb.AddForce(moveDirection * movementForce, ForceMode.Force);
        }
    }
    
    void LimitSpeed()
    {
        // Get horizontal velocity (ignore Y for jumping)
        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        
        // If moving too fast, clamp it
        if (horizontalVelocity.magnitude > maxSpeed)
        {
            Vector3 limitedVelocity = horizontalVelocity.normalized * maxSpeed;
            rb.velocity = new Vector3(limitedVelocity.x, rb.velocity.y, limitedVelocity.z);
        }
    }
    
    void UpdateDrag()
    {
        // Use different drag values based on whether we're on ground or in air
        rb.drag = isGrounded ? groundDrag : airDrag;
    }
}
