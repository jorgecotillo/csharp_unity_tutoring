using UnityEngine;

public class PulsingScaler : MonoBehaviour
{
    public float scaleSpeed = 1f;
    public float minScale = 0.5f;
    public float maxScale = 3f;
    
    private bool growing = true;
    
    void Update()
    {
        Vector3 currentScale = transform.localScale;
        
        if (growing)
        {
            currentScale += Vector3.one * scaleSpeed * Time.deltaTime;
            
            // Hit max size? Start shrinking!
            if (currentScale.x >= maxScale)
            {
                growing = false;
            }
        }
        else
        {
            currentScale -= Vector3.one * scaleSpeed * Time.deltaTime;
            
            // Hit min size? Start growing!
            if (currentScale.x <= minScale)
            {
                growing = true;
            }
        }
        
        float clampedScale = Mathf.Clamp(currentScale.x, minScale, maxScale);
        transform.localScale = Vector3.one * clampedScale;
    }
}
