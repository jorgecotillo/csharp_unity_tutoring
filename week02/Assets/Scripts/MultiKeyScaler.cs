using UnityEngine;
using UnityEngine.InputSystem;

public class MultiKeyScaler : MonoBehaviour
{
    public float scaleSpeed = 1f;
    public float minScale = 0.5f;
    public float maxScale = 5f;
    
    void Update()
    {
        // Check if keyboard is available
        // Fail fast
        if (Keyboard.current == null) return;
        
        Vector3 currentScale = transform.localScale;
        
        // SPACE = Grow uniformly
        if (Keyboard.current.spaceKey.isPressed)
        {
            currentScale += Vector3.one * scaleSpeed * Time.deltaTime;
        }
        
        // LEFT SHIFT = Shrink uniformly
        if (Keyboard.current.leftShiftKey.isPressed)
        {
            currentScale -= Vector3.one * scaleSpeed * Time.deltaTime;
        }
        
        // W = Grow taller (Y axis only)
        if (Keyboard.current.wKey.isPressed)
        {
            currentScale.y += scaleSpeed * Time.deltaTime;
        }
        
        // S = Shrink shorter (Y axis only)
        if (Keyboard.current.sKey.isPressed)
        {
            currentScale.y -= scaleSpeed * Time.deltaTime;
        }
        
        // D = Grow wider (X axis only)
        if (Keyboard.current.dKey.isPressed)
        {
            currentScale.x += scaleSpeed * Time.deltaTime;
        }
        
        // A = Shrink thinner (X axis only)
        if (Keyboard.current.aKey.isPressed)
        {
            currentScale.x -= scaleSpeed * Time.deltaTime;
        }
        
        // Clamp each dimension separately
        currentScale.x = Mathf.Clamp(currentScale.x, minScale, maxScale);
        currentScale.y = Mathf.Clamp(currentScale.y, minScale, maxScale);
        currentScale.z = Mathf.Clamp(currentScale.z, minScale, maxScale);
        
        transform.localScale = currentScale;
    }
}
