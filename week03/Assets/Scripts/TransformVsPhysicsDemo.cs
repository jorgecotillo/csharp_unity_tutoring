using UnityEngine;

/// <summary>
/// DEMONSTRATION: Shows the difference between moving with transform.position vs Rigidbody
/// 
/// This script creates a visual comparison by spawning two cubes side by side:
/// - LEFT CUBE: Moved by directly changing transform.position (BAD with physics)
/// - RIGHT CUBE: Moved using Rigidbody.velocity (GOOD with physics)
/// 
/// Both cubes will try to move forward, but when they hit obstacles:
/// - Left cube PASSES THROUGH or behaves incorrectly
/// - Right cube COLLIDES PROPERLY and reacts realistically
/// </summary>
public class TransformVsPhysicsDemo : MonoBehaviour
{
    [Header("Demo Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private bool createObstacle = true;

    private GameObject transformCube;   // Moved with transform.position
    private GameObject physicsCube;     // Moved with Rigidbody
    
    private Rigidbody transformRb;      // This one will have physics but we'll break it
    private Rigidbody physicsRb;        // This one will use physics correctly

    void Start()
    {
        CreateDemoScene();
    }

    void CreateDemoScene()
    {
        // Create LEFT cube - Transform approach (BAD)
        transformCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        transformCube.name = "BAD - Transform.position";
        transformCube.transform.position = new Vector3(-2f, 1f, 0f);
        transformCube.GetComponent<Renderer>().material.color = Color.red;
        
        transformRb = transformCube.AddComponent<Rigidbody>();
        transformRb.useGravity = false; // Disable for clearer demo
        
        // Label above
        CreateLabel("BAD: transform.position", new Vector3(-2f, 2f, 0f), Color.red);

        // Create RIGHT cube - Physics approach (GOOD)
        physicsCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        physicsCube.name = "GOOD - Rigidbody.velocity";
        physicsCube.transform.position = new Vector3(2f, 1f, 0f);
        physicsCube.GetComponent<Renderer>().material.color = Color.green;
        
        physicsRb = physicsCube.AddComponent<Rigidbody>();
        physicsRb.useGravity = false;
        
        // Label above
        CreateLabel("GOOD: rb.velocity", new Vector3(2f, 2f, 0f), Color.green);

        // Create obstacle wall
        if (createObstacle)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Obstacle Wall";
            wall.transform.position = new Vector3(0f, 1f, 5f);
            wall.transform.localScale = new Vector3(10f, 3f, 0.5f);
            wall.GetComponent<Renderer>().material.color = Color.gray;
            
            CreateLabel("OBSTACLE", new Vector3(0f, 3f, 5f), Color.yellow);
        }

        Debug.Log("=== DEMO CREATED ===");
        Debug.Log("RED CUBE: Uses transform.position (breaks physics)");
        Debug.Log("GREEN CUBE: Uses Rigidbody.velocity (works correctly)");
        Debug.Log("Watch what happens when they hit the wall!");
    }

    void CreateLabel(string text, Vector3 position, Color color)
    {
        GameObject label = new GameObject(text);
        label.transform.position = position;
        
        TextMesh textMesh = label.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.fontSize = 20;
        textMesh.color = color;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.1f;
    }

    void Update()
    {
        // BAD APPROACH: Directly modifying transform.position
        // This BREAKS physics! Collisions won't work properly!
        Vector3 newPosition = transformCube.transform.position;
        newPosition.z += moveSpeed * Time.deltaTime;  // Notice: We NEED Time.deltaTime here
        transformCube.transform.position = newPosition;
        
        // What happens:
        // - Physics engine doesn't "see" the movement
        // - Rigidbody state gets out of sync
        // - Collisions may not trigger or behave incorrectly
        // - Object might pass through walls!
    }

    void FixedUpdate()
    {
        // GOOD APPROACH: Using Rigidbody.velocity
        // This works WITH physics!
        physicsRb.velocity = new Vector3(0f, 0f, moveSpeed);  // Notice: NO Time.deltaTime!
        
        // Why no Time.deltaTime?
        // - Velocity is ALREADY measured in "units per second"
        // - Physics engine handles time automatically
        // - FixedUpdate runs at fixed timestep (0.02 seconds by default)
        
        // What happens:
        // - Physics engine tracks the movement
        // - Collisions work correctly
        // - Object stops when hitting walls
        // - Forces and physics interactions work!
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 16;
        style.normal.textColor = Color.white;
        
        GUI.Label(new Rect(10, 10, 600, 30), "RED CUBE (BAD): Moved with transform.position", style);
        GUI.Label(new Rect(10, 35, 600, 30), "GREEN CUBE (GOOD): Moved with Rigidbody.velocity", style);
        GUI.Label(new Rect(10, 60, 600, 30), "Watch what happens when they hit the wall!", style);
        
        style.normal.textColor = Color.yellow;
        GUI.Label(new Rect(10, 90, 600, 30), "Red cube might pass through or behave strangely!", style);
        GUI.Label(new Rect(10, 115, 600, 30), "Green cube will collide and stop correctly!", style);
    }
}
