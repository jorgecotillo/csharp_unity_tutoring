using UnityEngine;

/// <summary>
/// Week 4: Basic camera follow system
/// Makes the camera smoothly follow the player from behind
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("The transform to follow (usually the player)")]
    public Transform target;
    
    [Header("Camera Position")]
    [Tooltip("Offset from target position (X=left/right, Y=up/down, Z=forward/back)")]
    public Vector3 offset = new Vector3(0, 2, -5);
    
    [Header("Smoothing")]
    [Tooltip("How quickly camera catches up to target (higher = faster)")]
    [Range(1f, 20f)]
    public float smoothSpeed = 10f;
    
    [Header("Look At")]
    [Tooltip("Should camera always look at target?")]
    public bool lookAtTarget = true;
    
    [Tooltip("Offset for look-at target (useful for looking at player's head instead of feet)")]
    public Vector3 lookAtOffset = new Vector3(0, 1, 0);
    
    [Header("Debug")]
    public bool showDebugGizmos = false;
    
    void LateUpdate()
    {
        // Safety check
        if (target == null)
        {
            Debug.LogWarning("CameraFollow: No target assigned!");
            return;
        }
        
        FollowTarget();
        
        if (lookAtTarget)
        {
            LookAtTarget();
        }
    }
    
    /// <summary>
    /// Smoothly moves camera to follow target
    /// </summary>
    private void FollowTarget()
    {
        // Calculate desired position (target position + offset)
        Vector3 desiredPosition = target.position + offset;
        
        // Smoothly interpolate between current position and desired position
        // This creates a smooth, lag-behind effect
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position,              // Where we are now
            desiredPosition,                 // Where we want to be
            smoothSpeed * Time.deltaTime     // How much to move (0-1 range)
        );
        
        // Apply the smoothed position
        transform.position = smoothedPosition;
    }
    
    /// <summary>
    /// Makes camera look at the target
    /// </summary>
    private void LookAtTarget()
    {
        // Calculate look-at point (slightly above target's feet)
        Vector3 lookAtPoint = target.position + lookAtOffset;
        
        // Rotate camera to look at the target
        transform.LookAt(lookAtPoint);
    }
    
    /// <summary>
    /// Draws helpful debug visualizations in Scene view
    /// </summary>
    void OnDrawGizmos()
    {
        if (!showDebugGizmos || target == null) return;
        
        // Draw line from camera to target
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, target.position);
        
        // Draw desired position
        Gizmos.color = Color.green;
        Vector3 desiredPos = target.position + offset;
        Gizmos.DrawWireSphere(desiredPos, 0.3f);
        
        // Draw look-at point
        Gizmos.color = Color.red;
        Vector3 lookAtPoint = target.position + lookAtOffset;
        Gizmos.DrawWireSphere(lookAtPoint, 0.2f);
    }
}
