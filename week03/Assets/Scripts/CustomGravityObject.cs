using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Demonstrates custom gravity simulation - create your own planets!
/// Objects are attracted to a central point (like a planet's gravity well)
/// </summary>
public class CustomGravityObject : MonoBehaviour
{
    [Header("Gravity Target")]
    [Tooltip("The object that attracts this object (e.g., a planet)")]
    public Transform gravitySource;
    
    [Header("Gravity Settings")]
    [Tooltip("Strength of gravitational attraction")]
    public float gravitationalConstant = 10f;
    
    [Tooltip("Use inverse square law like real gravity? (weaker at distance)")]
    public bool useInverseSquare = true;
    
    [Header("Movement Settings")]
    [Tooltip("Force applied when moving")]
    public float movementForce = 5f;
    
    // Private variables
    private Rigidbody rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (rb == null)
        {
            Debug.LogError("CustomGravityObject requires a Rigidbody!");
            enabled = false;
            return;
        }
        
        // Disable Unity's built-in gravity since we're making our own
        rb.useGravity = false;
        
        // If no gravity source assigned, try to find one
        if (gravitySource == null)
        {
            GameObject planet = GameObject.Find("Planet");
            if (planet != null)
            {
                gravitySource = planet.transform;
                Debug.Log("Found gravity source: Planet");
            }
            else
            {
                Debug.LogWarning("No gravity source assigned! Assign one in the Inspector.");
            }
        }
    }
    
    void FixedUpdate()
    {
        // Apply custom gravity
        ApplyGravitationalForce();
        
        // Handle player input
        HandleMovement();
    }
    
    void ApplyGravitationalForce()
    {
        if (gravitySource == null) return;
        
        // Calculate direction from this object TO the gravity source
        Vector3 directionToSource = gravitySource.position - transform.position;
        float distance = directionToSource.magnitude;
        
        // Prevent division by zero or extreme forces at very close range
        if (distance < 0.1f) return;
        
        // Normalize to get just the direction
        Vector3 forceDirection = directionToSource.normalized;
        
        // Calculate force magnitude
        float forceMagnitude;
        
        if (useInverseSquare)
        {
            // Real gravity: F = G × m₁ × m₂ / r²
            // Simplified: F = G / r²
            forceMagnitude = gravitationalConstant / (distance * distance);
        }
        else
        {
            // Simpler: constant pull regardless of distance
            forceMagnitude = gravitationalConstant;
        }
        
        // Calculate final force vector
        Vector3 gravitationalForce = forceDirection * forceMagnitude * rb.mass;
        
        // Apply the force
        rb.AddForce(gravitationalForce, ForceMode.Force);
        
        // Debug visualization
        Debug.DrawRay(transform.position, forceDirection * 2f, Color.yellow);
    }
    
    void HandleMovement()
    {
        if (Keyboard.current == null) return;
        
        // Get camera's forward and right directions (for relative movement)
        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;
        
        // Flatten to horizontal plane (remove Y component)
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();
        
        // Calculate movement direction relative to camera
        Vector3 moveDirection = Vector3.zero;
        
        if (Keyboard.current.wKey.isPressed)
            moveDirection += forward;
        if (Keyboard.current.sKey.isPressed)
            moveDirection -= forward;
        if (Keyboard.current.dKey.isPressed)
            moveDirection += right;
        if (Keyboard.current.aKey.isPressed)
            moveDirection -= right;
        
        // Apply movement force
        if (moveDirection.magnitude > 0)
        {
            moveDirection.Normalize();
            rb.AddForce(moveDirection * movementForce, ForceMode.Force);
        }
    }
    
    /// <summary>
    /// Visualize the gravity well in the Scene view
    /// </summary>
    void OnDrawGizmos()
    {
        if (gravitySource == null) return;
        
        // Draw a line from this object to the gravity source
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, gravitySource.position);
    }
}
