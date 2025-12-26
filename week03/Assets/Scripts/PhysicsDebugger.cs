using UnityEngine;

/// <summary>
/// Visualizes physics properties like velocity and forces
/// Helpful for understanding what's happening in physics simulations
/// </summary>
public class PhysicsDebugger : MonoBehaviour
{
    [Header("Visualization Settings")]
    [Tooltip("Show velocity arrow")]
    public bool showVelocity = true;
    
    [Tooltip("Show angular velocity")]
    public bool showAngularVelocity = false;
    
    [Tooltip("Scale factor for velocity arrows")]
    public float velocityScale = 1f;
    
    [Tooltip("Show current speed as on-screen text")]
    public bool showSpeedText = true;
    
    private Rigidbody rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (rb == null)
        {
            Debug.LogWarning("PhysicsDebugger: No Rigidbody found!");
        }
    }
    
    void Update()
    {
        if (rb == null) return;
        
        // Draw velocity arrow
        if (showVelocity)
        {
            Debug.DrawRay(transform.position, rb.velocity * velocityScale, Color.green);
        }
        
        // Draw angular velocity
        if (showAngularVelocity)
        {
            Debug.DrawRay(transform.position, rb.angularVelocity * velocityScale, Color.blue);
        }
    }
    
    void OnGUI()
    {
        if (!showSpeedText || rb == null) return;
        
        // Calculate speeds
        float speed = rb.velocity.magnitude;
        float horizontalSpeed = new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude;
        
        // Display on screen
        GUI.color = Color.white;
        GUIStyle style = new GUIStyle();
        style.fontSize = 16;
        style.normal.textColor = Color.white;
        
        GUI.Label(new Rect(10, 10, 300, 30), 
            $"Speed: {speed:F2} m/s", style);
        GUI.Label(new Rect(10, 30, 300, 30), 
            $"Horizontal Speed: {horizontalSpeed:F2} m/s", style);
        GUI.Label(new Rect(10, 50, 300, 30), 
            $"Velocity: ({rb.velocity.x:F2}, {rb.velocity.y:F2}, {rb.velocity.z:F2})", style);
    }
}
