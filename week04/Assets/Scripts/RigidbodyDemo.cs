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
    [Range(0f, 10f)]
    public float drag = 0.5f;  // LOW drag = MORE sliding (better for demo!)
    
    // Reference to the Rigidbody component
    private Rigidbody rb;
    
    // Input from WASD keys
    private Vector2 moveInput;
    
    void Start()
    {
        // Get the Rigidbody component
        rb = GetComponent<Rigidbody>();
        
        // Configure Rigidbody for player movement (still feels bad!)
        rb.linearDamping = drag;  // Apply the drag setting
        rb.angularDamping = 0.05f;  // Very low to show physics clearly
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
        
        // Use AddForce instead of setting velocity directly
        // This allows drag to create the sliding effect!
        
        // 🎓 TEACHING MOMENT: We use AddForce HERE to demonstrate the sliding problem.
        // If we used: rb.linearVelocity = moveDirection * moveSpeed;
        // → It would stop INSTANTLY (no sliding!) - just like Character Controller!
        // 
        // So why NOT just use Character Controller then? The REAL differences:
        // 1. ✅ Character Controller has built-in ground detection (isGrounded)
        // 2. ✅ Character Controller handles slopes/steps automatically  
        // 3. ✅ Character Controller WON'T be pushed by physics (you stay in control!)
        // 4. ✅ Character Controller is more performant (optimized for characters)
        // 5. ✅ Character Controller is SIMPLER - no AddForce/drag tuning needed
        //
        // Bottom line: Sliding is OPTIONAL with Rigidbody (and usually unwanted!).
        // The REAL benefits of Character Controller are all the built-in features!
        
        Vector3 targetVelocity = moveDirection * moveSpeed;
        Vector3 currentVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        Vector3 velocityChange = targetVelocity - currentVelocity;
        
        // Apply force to reach target velocity (creates momentum/sliding!)
        rb.AddForce(velocityChange * 10f, ForceMode.Force);
        
        // ⚠️ NOTICE: Even when you release keys (targetVelocity = 0), 
        // you don't stop instantly! The drag slows you down gradually.
        // This is realistic physics, but BAD for player control!
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Rigidbody gets pushed by other physics objects automatically
        Debug.Log("Rigidbody BUMPED into: " + collision.gameObject.name);
    }
}
