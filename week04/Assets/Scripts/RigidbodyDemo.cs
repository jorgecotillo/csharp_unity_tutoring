using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Demo script showing Rigidbody movement with realistic physics.
/// IMPORTANT: This demonstrates WHY we don't use Rigidbody for player characters!
/// 
/// What you'll notice:
/// - Character slides when you stop (momentum!)
/// - Harder to stop exactly where you want
/// - Feels "floaty" or "slippery"
/// - Gets pushed by other physics objects
/// </summary>
public class RigidbodyDemo : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How fast the character moves")]
    public float moveSpeed = 5f;
    
    [Header("Physics Settings")]
    [Tooltip("Higher drag = less sliding (but still slides!)")]
    public float drag = 2f;
    
    // Reference to the Rigidbody component
    private Rigidbody rb;
    
    // Input from WASD keys
    private Vector2 moveInput;
    
    void Start()
    {
        // Get the Rigidbody component
        rb = GetComponent<Rigidbody>();
        
        // Configure Rigidbody for player movement (still feels bad!)
        rb.drag = drag;
        rb.constraints = RigidbodyConstraints.FreezeRotation; // Don't tip over
        
        Debug.Log("RIGIDBODY DEMO: Use Arrow Keys to move. Notice the SLIDING when you stop!");
    }
    
    void Update()
    {
        // Read input from Arrow Keys (so it doesn't conflict with WASD for Character Controller)
        moveInput.x = 0;
        moveInput.y = 0;
        
        if (Keyboard.current.upArrowKey.isPressed) moveInput.y = 1;
        if (Keyboard.current.downArrowKey.isPressed) moveInput.y = -1;
        if (Keyboard.current.leftArrowKey.isPressed) moveInput.x = -1;
        if (Keyboard.current.rightArrowKey.isPressed) moveInput.x = 1;
    }
    
    void FixedUpdate()
    {
        // IMPORTANT: Rigidbody MUST use FixedUpdate for physics!
        
        // Calculate movement direction
        Vector3 moveDirection = new Vector3(moveInput.x, 0, moveInput.y);
        
        // Normalize to prevent faster diagonal movement
        if (moveDirection.magnitude > 0.1f)
        {
            moveDirection.Normalize();
        }
        
        // Apply velocity (this is what causes the sliding!)
        // Even with no input, momentum keeps you moving
        rb.velocity = new Vector3(
            moveDirection.x * moveSpeed,
            rb.velocity.y, // Keep existing Y velocity (gravity)
            moveDirection.z * moveSpeed
        );
        
        // ⚠️ NOTICE: Even when you release keys, you don't stop instantly!
        // The 'drag' slows you down, but there's still a slide.
        // This is realistic physics, but BAD for player control!
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Rigidbody gets pushed by other physics objects automatically
        Debug.Log("Rigidbody BUMPED into: " + collision.gameObject.name);
    }
}
