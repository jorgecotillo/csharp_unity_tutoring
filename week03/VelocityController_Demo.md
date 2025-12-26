# How to Apply Scripts to GameObjects in Unity

This guide explains how to take code examples from the README and actually use them in Unity.

---

## The Basic Process

**Every script needs to be attached to a GameObject to work!**

```
Code Example (README) → Create Script File → Attach to GameObject → Configure → Press Play
```

---

## Step-by-Step: Applying a Movement Script

### Step 1: Create a GameObject in Unity

**In Unity Editor:**
1. Right-click in **Hierarchy** panel
2. Select **3D Object → Cube** (or Capsule for a character)
3. Name it "Player"

**Result:** You now have a visible object in your scene

---

### Step 2: Add a Rigidbody Component

**Why?** Physics scripts need a Rigidbody to work!

1. Select the **Player** object in Hierarchy
2. In Inspector, click **Add Component**
3. Type "Rigidbody" and select it
4. Configure Rigidbody settings:
   - **Mass** = 1
   - **Use Gravity** = ☐ (unchecked) for now - we'll test without gravity first
   - **Constraints** → Freeze Rotation X, Y, Z (prevents unwanted spinning)

**⚠️ Important:** If you enable gravity later, you MUST create a floor (see troubleshooting section)!

---

### Step 3: Create the Script File

**Option A: Create New Script**
1. In **Project** panel, navigate to `Assets/Scripts/`
2. Right-click → **Create → C# Script**
3. Name it `VelocityController.cs`
4. Double-click to open in your code editor
5. Copy this code:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class VelocityController : MonoBehaviour
{
    private Rigidbody rb;  // Reference to the Rigidbody component
    
    [SerializeField] private float moveSpeed = 5f;
    
    void Start()
    {
        // Get the Rigidbody component attached to this GameObject
        rb = GetComponent<Rigidbody>();
    }
    
    void FixedUpdate()
    {
        // Get input direction
        Vector3 moveDirection = Vector3.zero;
        
        if (Keyboard.current.wKey.isPressed)
            moveDirection += Vector3.forward;
        if (Keyboard.current.sKey.isPressed)
            moveDirection += Vector3.back;
        if (Keyboard.current.aKey.isPressed)
            moveDirection += Vector3.left;
        if (Keyboard.current.dKey.isPressed)
            moveDirection += Vector3.right;
        
        // Normalize (prevents faster diagonal movement)
        if (moveDirection.magnitude > 0)
            moveDirection.Normalize();
        
        // Set velocity directly
        Vector3 newVelocity = rb.velocity;
        newVelocity.x = moveDirection.x * moveSpeed;
        newVelocity.z = moveDirection.z * moveSpeed;
        rb.velocity = newVelocity;  // Keep Y velocity for gravity!
    }
}
```

6. Save the file

**Option B: Use Existing Script**
- If the script already exists in `Assets/Scripts/`, skip to Step 4

---

### Step 4: Attach Script to GameObject

**Method 1: Drag and Drop**
1. Find your script in Project panel (`Assets/Scripts/VelocityController.cs`)
2. Drag it onto the **Player** object in Hierarchy
3. Done!

**Method 2: Add Component**
1. Select **Player** object
2. In Inspector, click **Add Component**
3. Type the script name: "VelocityController"
4. Click to add it

**Result:** You'll see the script component appear in the Inspector

---

### Step 5: Configure Parameters in Inspector

**After attaching the script:**

1. Select the **Player** object
2. Look in Inspector for your script component
3. You'll see fields like:
   - **Move Speed** = 5 (adjust as needed)
   - **Max Speed** = 10 (optional)

**These match the `[SerializeField]` variables in your code!**

```csharp
[SerializeField] private float moveSpeed = 5f;  // ← Shows in Inspector as "Move Speed"
[SerializeField] private float maxSpeed = 10f;  // ← Shows in Inspector as "Max Speed"
```

**Tip:** Adjust these values while the game is playing to experiment!

---

### Step 6: Press Play and Test!

1. Click the **Play** button at top of Unity Editor
2. Test the controls (WASD for movement)
3. If it doesn't work, check the Console for errors

---

## Visual Guide: Inspector View

```
Inspector (Player selected):
┌─────────────────────────────────────┐
│ Transform                           │
│ ├─ Position: (0, 1, 0)              │
│ ├─ Rotation: (0, 0, 0)              │
│ └─ Scale: (1, 1, 1)                 │
├─────────────────────────────────────┤
│ Cube (Mesh Filter)                  │
├─────────────────────────────────────┤
│ Mesh Renderer                       │
│ └─ Material: Default-Material       │
├─────────────────────────────────────┤
│ Box Collider                        │
│ └─ Is Trigger: ☐                    │
├─────────────────────────────────────┤
│ Rigidbody                    ← ADD THIS!
│ ├─ Mass: 1                          │
│ ├─ Drag: 0                          │
│ ├─ Use Gravity: ✓                   │
│ └─ Constraints:                     │
│    └─ Freeze Rotation: X Y Z ✓      │
├─────────────────────────────────────┤
│ Velocity Controller          ← ATTACH SCRIPT!
│ ├─ Move Speed: 5                    │
│ └─ Max Speed: 10                    │
└─────────────────────────────────────┘
```

---

## Complete Example: Velocity-Based Movement

### The Script (VelocityController.cs)

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class VelocityController : MonoBehaviour
{
    private Rigidbody rb;
    
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float maxSpeed = 10f;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Make sure object has Rigidbody!
        if (rb == null)
        {
            Debug.LogError("VelocityController needs a Rigidbody component!");
        }
    }
    
    void FixedUpdate()
    {
        // Get input direction
        Vector3 moveDirection = Vector3.zero;
        
        if (Keyboard.current.wKey.isPressed)
            moveDirection += Vector3.forward;
        if (Keyboard.current.sKey.isPressed)
            moveDirection += Vector3.back;
        if (Keyboard.current.aKey.isPressed)
            moveDirection += Vector3.left;
        if (Keyboard.current.dKey.isPressed)
            moveDirection += Vector3.right;
        
        // Normalize (prevents faster diagonal movement)
        if (moveDirection.magnitude > 0)
            moveDirection.Normalize();
        
        // Set velocity directly
        Vector3 newVelocity = rb.velocity;
        newVelocity.x = moveDirection.x * moveSpeed;
        newVelocity.z = moveDirection.z * moveSpeed;
        rb.velocity = newVelocity;  // Keep Y velocity for gravity!
        
        // Optional: Limit maximum speed
        ClampVelocity();
    }
    
    void ClampVelocity()
    {
        Vector3 velocity = rb.velocity;
        
        // Get horizontal velocity (X and Z only)
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
        
        // If too fast, clamp it
        if (horizontalVelocity.magnitude > maxSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * maxSpeed;
            rb.velocity = new Vector3(horizontalVelocity.x, velocity.y, horizontalVelocity.z);
        }
    }
}
```

### Setup Checklist

- [ ] Created GameObject (Cube named "Player")
- [ ] Added Rigidbody component
- [ ] Set Rigidbody Mass = 1
- [ ] Froze Rigidbody rotation (X, Y, Z)
- [ ] Created `VelocityController.cs` in Assets/Scripts
- [ ] Copied code into the script file
- [ ] Saved the script file
- [ ] Attached script to Player object
- [ ] Verified script appears in Inspector
- [ ] Adjusted Move Speed if needed
- [ ] Pressed Play
- [ ] Tested with WASD keys

---

## Common Mistakes & Fixes

### ❌ "The script doesn't do anything!"
**Fix:** Did you attach the script to a GameObject? Scripts don't run unless attached!

### ❌ "NullReferenceException: Object reference not set"
**Fix:** The GameObject needs a Rigidbody component! Add it in Inspector.

### ❌ "Object spins wildly when moving"
**Fix:** In Rigidbody → Constraints → Freeze Rotation X, Y, Z

### ❌ "Can't find Keyboard.current"
**Fix:** Make sure you have `using UnityEngine.InputSystem;` at the top and the Input System package installed (covered in Week 2)

### ❌ "Script fields don't show in Inspector"
**Fix:** Make sure variables have `[SerializeField]` before them:
```csharp
[SerializeField] private float moveSpeed = 5f;  // ✅ Shows in Inspector
private float moveSpeed = 5f;                   // ❌ Doesn't show
```

### ❌ "Object falls and does nothing when I press keys"
**This is the most common issue!** You need a floor or disable gravity:

**Option 1: Create a Floor (Recommended)**
1. In Hierarchy, Right-click → 3D Object → Plane
2. Name it "Floor"
3. Set Position to (0, 0, 0)
4. Set Scale to (10, 1, 10) for a bigger floor
5. The Plane automatically has a Mesh Collider

**Option 2: Disable Gravity (for testing)**
1. Select your Player object
2. In Rigidbody component, **uncheck "Use Gravity"**
3. Now the object won't fall and you can test movement

**Why this happens:**
- Your script only controls X and Z velocity (left/right, forward/back)
- It keeps Y velocity unchanged for gravity/jumping
- If Use Gravity is ON but there's no floor, the object falls forever
- Movement works, but you can't see it because the object is falling out of view!

### ❌ "Object falls through the floor"
**Fix:** 
1. Make sure the floor has a Collider component (Plane has Mesh Collider by default)
2. Make sure Player has a Collider (Cube has Box Collider by default)
3. Check that Player's Y position is above the floor (e.g., Y = 1, Floor Y = 0)

---

## Quick Reference: Component Requirements

| Script Type | Requires | Optional |
|-------------|----------|----------|
| **Velocity Controller** | Rigidbody, Collider | - |
| **Force Controller** | Rigidbody, Collider | - |
| **Custom Gravity** | Rigidbody, Gravity Source object | - |
| **Transform Movement** | - | Rigidbody (if you want collisions) |
| **Camera Follow** | Target object | - |

---

## Testing Your Script

**Basic test:**
1. Press Play
2. Try all inputs (W, A, S, D)
3. Check if object moves smoothly
4. Check Console for errors (red messages)

**Physics test:**
1. Add a Cube as a wall in front of player
2. Press Play and move into it
3. Should collide and stop (not pass through)

**Speed test:**
1. While game is running, select Player
2. In Inspector, change "Move Speed" to 20
3. Object should move faster immediately
4. Change back to 5 when done testing

---

## Next Steps

Once you have basic movement working:
- Try adding jump functionality (Week 3 examples)
- Experiment with different speeds and forces
- Add multiple objects with different scripts
- Create obstacles and test collisions

**Remember:** You can always adjust values in the Inspector while the game is running to experiment!

---

## Summary

**The golden rule:** 
```
Script File + GameObject + Rigidbody (if using physics) = Working Game Mechanic
```

Every code example in the README follows this same process:
1. Create or use existing GameObject
2. Add required components (Rigidbody, Collider, etc.)
3. Create script file with the code
4. Attach script to GameObject
5. Configure parameters in Inspector
6. Press Play and test!
