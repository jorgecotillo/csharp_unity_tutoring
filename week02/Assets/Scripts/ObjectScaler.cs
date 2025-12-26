using UnityEngine;
using UnityEngine.InputSystem;

public class ObjectScaler : MonoBehaviour
{
    // Settings you can change in Inspector
    public float scaleSpeed = 1f;
    public float minScale = 0.5f;
    public float maxScale = 5f;
    
    void Update()
    {
        // Get the current size
        Vector3 currentScale = transform.localScale;
        
        // Check if spacebar is held down (new Input System)
        if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
        {
            // Grow bigger!
            currentScale += Vector3.one * scaleSpeed * Time.deltaTime;
        }
        else
        {
            // Shrink smaller!
            currentScale -= Vector3.one * scaleSpeed * Time.deltaTime;
        }
        
        // Make sure we don't go too big or too small
        // (Keep all dimensions the same for uniform scaling)
        float clampedScale = Mathf.Clamp(currentScale.x, minScale, maxScale);
        transform.localScale = Vector3.one * clampedScale;
    }
}
