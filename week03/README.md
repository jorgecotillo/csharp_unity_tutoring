# Week 3: Physics & Forces - Rigidbody Fundamentals 🚀

**Today's Goal:** Understand Unity's physics system and make objects move realistically using forces and velocity!  
**Time:** 1 hour  
**New Concepts:** Rigidbody, Velocity, Forces, Custom Gravity, Physics Simulation  
**C# Concepts:** Component references, FixedUpdate, ForceMode, Vector3 physics  

---

## ⚠️ IMPORTANT: Prerequisites

This week builds on concepts from Weeks 1 & 2:
- ✅ Unity's new Input System (installed Week 2)
- ✅ Understanding of `Time.deltaTime`
- ✅ Comfortable with `Vector3` and basic math
- ✅ Know how to use `GetComponent<>()`

**New concept:** Instead of directly changing `transform.position` or `transform.localScale`, we'll use **physics** to create realistic movement with momentum, gravity, and forces!

---

## What You'll Build Today

Three different physics-based controllers:
1. **Velocity Controller** - Direct speed control (like a hovercraft)
2. **Force Controller** - Push-based movement with momentum (like a car accelerating)
3. **Custom Gravity** - Create your own planetary gravity! (like Super Mario Galaxy)

**Success = Understanding the difference between velocity and forces, and making objects orbit a planet!** ✨

---

## Session Structure (60 minutes)

### Part 1: Week 2 Recap & Physics Introduction (10 minutes)
### Part 2: Understanding Rigidbody & Velocity (15 minutes)
### Part 3: Forces vs Velocity (10 minutes)
### Part 4: BREAK - Stand up and move! (5 minutes)
### Part 5: Custom Gravity Simulation (15 minutes)
### Part 6: Challenges & Experiments (5 minutes)

---

## Part 1: Week 2 Recap & Physics Introduction (10 minutes)

### What We Learned Last Week

Last week you made objects **grow and shrink** using the spacebar! Let's review key concepts.

---

### 📌 Quick Recap: Transform vs Rigidbody

**Last week we changed `transform.localScale`:**
```csharp
transform.localScale += Vector3.one * scaleSpeed * Time.deltaTime;
```

**This directly changed the object's size** - instant, no physics involved.

**This week is different!**

Instead of directly changing `transform.position`, we'll use **Rigidbody** to create realistic physics-based movement!

---

### What is a Rigidbody?

**Rigidbody** = Unity's "physics brain" for GameObjects

Think of it like this:
- **Without Rigidbody:** Object is a ghost - floats in space, ignores gravity, passes through walls
- **With Rigidbody:** Object is physical - affected by gravity, collides with things, has momentum

**Key Properties:**
- **Mass** - How heavy it is (kg)
- **Drag** - Air resistance (how quickly it slows down)
- **Velocity** - Current speed and direction (m/s)
- **Use Gravity** - Should Unity's gravity pull it down?

---

### The Golden Rule of Physics in Unity

**⚠️ NEVER directly change `transform.position` when using Rigidbody!**

**BAD (breaks physics):**
```csharp
transform.position += moveDirection * speed * Time.deltaTime;
```

**GOOD (uses physics):**
```csharp
rb.velocity = moveDirection * speed;  // Set velocity (NO Time.deltaTime!)
// OR
rb.AddForce(moveDirection * force);  // Apply force (NO Time.deltaTime!)
```

**Why?** When you directly change `transform.position`, Unity's physics engine gets confused and collisions/physics stop working properly!

---

### 🔍 Deep Dive: Why No Time.deltaTime with Physics?

**With transform (Week 1 & 2):**
```csharp
void Update()  // Runs every frame (varies: 60fps, 120fps, 30fps...)
{
    transform.position += Vector3.forward * speed * Time.deltaTime;
    //                                              ^^^^^^^^^^^^^^
    //                                              NEEDED! Converts "per frame" to "per second"
}
```

**Wait, what does "converts per frame to per second" mean?**

The object **moves every frame**, but Time.deltaTime ensures it moves at a **consistent rate** regardless of frame rate!

**Example: Moving at 5 units per second**

**At 60 FPS (fast computer):**
- Update() runs **60 times per second**
- Time.deltaTime = **0.0167 seconds** (1/60)
- Each frame: `position += 5 * 0.0167` = **0.0835 units**
- After 1 second (60 frames): Total = **0.0835 × 60 = 5 units** ✅

**At 30 FPS (slower computer):**
- Update() runs **30 times per second**
- Time.deltaTime = **0.0333 seconds** (1/30)
- Each frame: `position += 5 * 0.0333` = **0.1667 units**
- After 1 second (30 frames): Total = **0.1667 × 30 = 5 units** ✅

**Result:** Both computers move the object **5 units in 1 second**, even though one updates 60 times and the other 30 times!

**Without Time.deltaTime (BAD!):**
```csharp
transform.position += Vector3.forward * speed;  // NO Time.deltaTime!
```

- At 60 FPS: Moves `5 × 60 = 300 units/second` (super fast!)
- At 30 FPS: Moves `5 × 30 = 150 units/second` (half speed!)
- **Problem:** Game speed depends on computer performance! 😱

**The key insight:**
- Object moves **smoothly every frame** (not once per second!)
- Time.deltaTime makes the **total distance traveled** consistent
- Think of it as: "How much should I move **this frame** to achieve X units **per second**?"
```

**With Rigidbody physics (Week 3):**
```csharp
void FixedUpdate()  // Runs at FIXED interval (default: 50 times/second = 0.02s)
{
    rb.velocity = Vector3.forward * speed;  // NO Time.deltaTime!
    //                                      Velocity is ALREADY "meters per second"
    
    // OR
    rb.AddForce(Vector3.forward * force);  // NO Time.deltaTime!
    //                                     Unity applies force over the physics timestep automatically
}
```

**Wait... why is the fixed interval enough? Why no Time.deltaTime?**

**Great question!** You're absolutely right - the fixed interval IS the key! Here's the complete explanation:

**The crucial difference:**

| | Update() | FixedUpdate() |
|---|----------|---------------|
| **Timing** | Variable (30fps, 60fps, 120fps...) | **FIXED** (always 50 fps) |
| **Interval varies?** | ✅ YES - depends on computer speed | ❌ NO - always 0.02 seconds |
| **Need Time.deltaTime?** | ✅ YES - compensates for varying intervals | ❌ NO - interval is constant! |

**In FixedUpdate() with transform (if you were doing it wrong):**
```csharp
void FixedUpdate()  // Always runs at 0.02s intervals
{
    // If you were moving transform directly (DON'T DO THIS!):
    transform.position += Vector3.forward * 5f;  // NO Time.deltaTime needed!
    
    // Why? Because FixedUpdate ALWAYS runs 50 times/second:
    // Each frame: move 5 units
    // After 1 second (50 frames): 5 × 50 = 250 units total
    // This is CONSISTENT because FixedUpdate timing is FIXED!
}
```

**But we don't do that! We use rb.velocity instead because:**

1. **Velocity is a RATE (m/s)**, not a distance
2. Unity's physics engine automatically applies the velocity over time
3. The physics engine internally uses the fixed timestep (0.02s)

**Here's what actually happens internally:**

```csharp
void FixedUpdate()
{
    rb.velocity = Vector3.forward * 5f;  // Set speed to 5 m/s
    
    // Unity's physics engine INTERNALLY does:
    // newPosition = currentPosition + (velocity * Time.fixedDeltaTime)
    // newPosition = currentPosition + (5 m/s * 0.02s)
    // newPosition = currentPosition + 0.1 meters
    
    // This happens automatically every FixedUpdate!
}
```

**The math breakdown:**

```
rb.velocity = 5 m/s  (you set this once)

FixedUpdate call 1 (t=0.00s): Physics moves object 5 * 0.02 = 0.1m  → Position = 0.1m
FixedUpdate call 2 (t=0.02s): Physics moves object 5 * 0.02 = 0.1m  → Position = 0.2m
FixedUpdate call 3 (t=0.04s): Physics moves object 5 * 0.02 = 0.1m  → Position = 0.3m
...
FixedUpdate call 50 (t=1.00s): Physics moves object 5 * 0.02 = 0.1m → Position = 5.0m
```

**Total after 1 second: 5 meters** ✅

**Why no Time.deltaTime needed:**

1. ✅ FixedUpdate runs at **fixed intervals** (always 0.02s)
2. ✅ Velocity is a **rate** (m/s), not a per-frame amount
3. ✅ Unity's physics engine **internally multiplies** velocity by Time.fixedDeltaTime (0.02s)
4. ✅ You just set the **speed**, physics handles the **movement**

**Comparison summary:**

```csharp
// Week 1-2: Transform in Update() - VARIABLE timing
void Update()
{
    transform.position += Vector3.forward * 5f * Time.deltaTime;
    // YOU multiply by Time.deltaTime because Update() timing varies
    // You're calculating: "how far to move THIS FRAME"
}

// Week 3: Rigidbody in FixedUpdate() - FIXED timing
void FixedUpdate()
{
    rb.velocity = Vector3.forward * 5f;  // NO Time.deltaTime
    // PHYSICS multiplies by Time.fixedDeltaTime internally
    // You're setting: "how fast to move CONTINUOUSLY"
}
```

**The key insight:**
- **Update()**: You calculate the movement → Need Time.deltaTime
- **FixedUpdate()**: Physics engine calculates the movement → It uses Time.fixedDeltaTime internally

**Key Differences:**

| Approach | Function | Needs Time.deltaTime? | Why? |
|----------|----------|----------------------|------|
| `transform.position +=` | `Update()` | ✅ **YES** | Converts "per frame" to "per second" |
| `rb.velocity =` | `FixedUpdate()` | ❌ **NO** | Velocity is already in m/s |
| `rb.AddForce()` | `FixedUpdate()` | ❌ **NO** | Physics engine handles timestep |

---

### 📊 Visual Example: What Happens?

**Scenario:** Move object forward at 5 m/s

**❌ BAD - Transform approach (breaks physics):**
```csharp
void Update()  // Runs ~60 times per second (varies!)
{
    // Manually move position - physics engine doesn't know about this!
    transform.position += Vector3.forward * 5f * Time.deltaTime;
    
    // Result when hitting a wall:
    // - Might pass through (physics didn't "see" the movement)
    // - Collider is out of sync with visual position
    // - OnCollisionEnter might not trigger
    // - Physics forces don't affect it correctly
}
```

**✅ GOOD - Rigidbody velocity:**
```csharp
void FixedUpdate()  // Runs exactly 50 times per second (fixed!)
{
    // Tell physics engine: "move at 5 meters per second"
    rb.velocity = Vector3.forward * 5f;  // Physics handles the rest!
    
    // Result when hitting a wall:
    // - Stops correctly (collision detected)
    // - OnCollisionEnter triggers
    // - Can be pushed by other objects
    // - Forces affect it properly
}
```

**✅ GOOD - Rigidbody force:**
```csharp
void FixedUpdate()
{
    // Tell physics engine: "apply 5 Newtons of force forward"
    rb.AddForce(Vector3.forward * 5f, ForceMode.Force);
    
    // Result:
    // - Gradual acceleration (more realistic!)
    // - Affected by mass (heavier = slower acceleration)
    // - Collisions work perfectly
    // - Can be pushed by other forces
}
```

---

### ⏱️ Update() vs FixedUpdate() - Input Detection

**Important:** Where you check for input matters!

**The Problem: Missing Button Presses**

`FixedUpdate()` runs at **50 times/second** (every 0.02s), while `Update()` runs at **60-120+ times/second**. This means button presses can happen **between** FixedUpdate calls!

**Visual Timeline:**
```
Time:     0.00s    0.01s    0.02s    0.03s    0.04s
          |        |        |        |        |
Update:   ✓    ✓   ✓   ✓    ✓   ✓    ✓   ✓    ✓   (60+ times/sec)
          
Player presses jump:     👆 (at 0.015s)
                         
FixedUpdate:  ✓                ✓                ✓   (50 times/sec)
              ^                ^
              0.00s            0.02s
              
Result: Jump press at 0.015s is BETWEEN FixedUpdate calls!
        Might be missed! ❌
```

**❌ BAD - Input in FixedUpdate (can miss presses):**
```csharp
void FixedUpdate()  // Runs 50 times/sec
{
    if (Keyboard.current.spaceKey.wasPressedThisFrame)
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }
    // Problem: wasPressedThisFrame checks for events
    // Events between FixedUpdate calls might be missed!
}
```

**✅ GOOD - Input in Update, physics in FixedUpdate:**
```csharp
private bool shouldJump = false;

void Update()  // Runs 60+ times/sec - catches every press!
{
    if (Keyboard.current.spaceKey.wasPressedThisFrame)
    {
        shouldJump = true;  // Flag for FixedUpdate
    }
}

void FixedUpdate()  // Runs 50 times/sec - stable physics
{
    if (shouldJump)
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        shouldJump = false;  // Reset flag
    }
}
// Result: Every button press is caught, physics stays stable! ✅
```

**Exception: Held Keys Are OK in FixedUpdate**

```csharp
void FixedUpdate()
{
    // ✅ This is fine - isPressed checks current STATE, not events
    Vector3 moveDir = Vector3.zero;
    if (Keyboard.current.wKey.isPressed)  // Held down check
        moveDir += Vector3.forward;
    
    rb.velocity = new Vector3(moveDir.x * speed, rb.velocity.y, moveDir.z * speed);
}
```

**Key difference:**
- `wasPressedThisFrame` / `wasReleasedThisFrame` = **Events** (can be missed) → Check in Update()
- `isPressed` = **State** (persists across frames) → OK in either Update() or FixedUpdate()

**The Golden Rule:**
- **Detect** one-time button presses in `Update()` (responsive, no misses)
- **Apply** physics changes in `FixedUpdate()` (stable simulation)
- **Bridge** them with a bool flag (communication)

---

### 🧪 Try It Yourself: TransformVsPhysicsDemo

We've included a demo script that visually shows the difference!

**To run the demo:**

1. Create an empty GameObject in your scene (Right-click in Hierarchy → Create Empty)
2. Name it "Physics Demo Manager"
3. Attach the `TransformVsPhysicsDemo.cs` script to it
4. Press Play

**Note:** The script automatically creates the cubes, wall, and labels - you don't need to create them manually!

**What you'll see:**

- **RED CUBE (left):** Moved with `transform.position` (BAD approach)
  - When it hits the wall, it might pass through or behave weirdly!
  - Physics is "broken" because we're bypassing the physics engine
  
- **GREEN CUBE (right):** Moved with `rb.velocity` (GOOD approach)
  - When it hits the wall, it stops correctly!
  - Physics works as expected - collisions are detected

**💡 If the red cube isn't passing through:**

The tunneling effect depends on speed, frame rate, and wall thickness. If both cubes stop at the wall:

1. Select the "Physics Demo Manager" in the Hierarchy
2. In Inspector, increase "Speed Multiplier" to 2, 3, or higher
3. Press Play again

At higher speeds, the red cube will "teleport" past the wall between physics checks, while the green cube will always collide properly because physics tracks its continuous movement!

**Why the difference?**

- **Transform approach:** You're "teleporting" the object each frame - physics engine doesn't see smooth movement
- **Rigidbody approach:** Physics engine controls movement - it knows exactly where the object is and how fast it's moving

---

### 🎓 The Bottom Line

**When using Rigidbody:**
- ✅ Use `rb.velocity` (no Time.deltaTime)
- ✅ Use `rb.AddForce()` (no Time.deltaTime)
- ✅ Put **physics changes** in `FixedUpdate()` (changing velocity, applying forces)
- ✅ Put **input detection** in `Update()` for responsiveness (see section above for why!)
- ❌ Never modify `transform.position` directly
- ❌ Don't multiply by Time.deltaTime (physics already handles time)

**When NOT using Rigidbody (Weeks 1 & 2):**
- ✅ Modify `transform.position` or `transform.localScale`
- ✅ Always multiply by `Time.deltaTime`
- ✅ Put code in `Update()`

---

### 🎮 Real-World Example: Mario Bros

**"In a complex game like Mario, when do I use Time.deltaTime?"**

Great question! Let's break down a Mario-style platformer:

#### ❌ Mario (Player Character) - NO Time.deltaTime
```csharp
using UnityEngine.InputSystem;  // New Input System!

public class MarioController : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;
    private bool shouldJump = false;  // Flag to communicate between Update and FixedUpdate
    
    void Update()
    {
        // Check input EVERY frame for responsiveness
        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            shouldJump = true;  // Set flag
        }
    }
    
    void FixedUpdate()
    {
        // Movement - NO Time.deltaTime (uses Rigidbody)
        Vector3 moveDirection = Vector3.zero;
        if (Keyboard.current.aKey.isPressed) moveDirection.x -= 1;
        if (Keyboard.current.dKey.isPressed) moveDirection.x += 1;
        
        rb.velocity = new Vector3(moveDirection.x * moveSpeed, rb.velocity.y, 0f);
        //                                                       ^^^ NO Time.deltaTime!
        
        // Apply jump force in FixedUpdate (physics code)
        if (shouldJump)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            //                                  ^^^ NO Time.deltaTime!
            shouldJump = false;  // Reset flag
        }
    }
}
```
**Why input in Update() but physics in FixedUpdate()?**
- **Update()**: Check input every frame for responsive controls (no missed button presses)
- **FixedUpdate()**: Apply physics forces at fixed intervals for stable simulation
- **Communication**: Use a bool flag to pass input from Update() to FixedUpdate()

**Why no Time.deltaTime?** Mario has a Rigidbody - physics handles timing automatically!

---

#### ✅ Camera Follow - YES Time.deltaTime
```csharp
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;  // Mario
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0, 2, -10);
    
    void Update()  // Camera in Update, not FixedUpdate!
    {
        // Smooth camera movement - YES Time.deltaTime!
        Vector3 targetPosition = target.position + offset;
        transform.position = Vector3.Lerp(
            transform.position, 
            targetPosition, 
            smoothSpeed * Time.deltaTime  // ← YES! Camera doesn't use physics!
        );
    }
}
```
**Why Time.deltaTime?** Camera doesn't have Rigidbody - we're directly changing `transform.position`!

---

#### ✅ Rotating Coins - YES Time.deltaTime
```csharp
public class RotatingCoin : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 90f;  // Degrees per second
    
    void Update()
    {
        // Rotate coin - YES Time.deltaTime!
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        //                                          ^^^ YES! Transform rotation needs it!
    }
}
```
**Why Time.deltaTime?** We're modifying `transform.rotation` directly (no Rigidbody)!

**Important: Why don't coins use Rigidbody?**

In Mario games, coins **float in mid-air** - they're not affected by gravity! They're **decorative collectibles**, not physics objects.

**Two coin approaches:**

| Coin Type | Has Rigidbody? | Affected by Gravity? | Use Case |
|-----------|----------------|---------------------|----------|
| **Floating coin** (Mario style) | ❌ No | ❌ No | Decorative collectibles that stay in place and spin |
| **Dropped coin** (rare) | ✅ Yes | ✅ Yes | Coins that fall, bounce, and roll on ground |

**Example: If coins SHOULD fall:**
```csharp
public class PhysicsCoin : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private float torque = 50f;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;  // Falls like a real coin!
    }
    
    void FixedUpdate()
    {
        // Rotate using physics torque (NO Time.deltaTime!)
        rb.AddTorque(Vector3.up * torque);
    }
}
```

**Why most games use floating coins:**
- Easier to place in levels (don't fall through floor)
- Players know exactly where they are
- Less performance overhead (no physics calculations)
- Classic game design (coins = visual rewards, not realistic objects)

---

#### ❌ Moving Platform - NO Time.deltaTime (if using Rigidbody)
```csharp
public class MovingPlatform : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private Vector3 moveDirection = Vector3.right;
    [SerializeField] private float speed = 2f;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;  // Kinematic = controlled by code, not forces
    }
    
    void FixedUpdate()
    {
        // Move platform - NO Time.deltaTime (uses Rigidbody)
        rb.MovePosition(rb.position + moveDirection * speed * Time.fixedDeltaTime);
        //                                                    ^^^^^^^^^^^^^^^^^^
        //                                                    Use fixedDeltaTime, not deltaTime!
    }
}
```
**Special case:** Kinematic Rigidbodies use `Time.fixedDeltaTime` (not `Time.deltaTime`)!

---

### 🔍 What is Kinematic?

**Kinematic Rigidbody = "You control movement, physics watches for collisions"**

**Three types of Rigidbody physics:**

| Type | Affected by Forces? | Affected by Gravity? | You Control | Use Case |
|------|---------------------|---------------------|-------------|----------|
| **Dynamic** (default) | ✅ Yes | ✅ Yes | Set forces/velocity | Mario, enemies, balls, realistic objects |
| **Kinematic** | ❌ No | ❌ No | Move directly with code | Moving platforms, elevators, doors |
| **Static** (no Rigidbody) | ❌ No | ❌ No | Never moves | Walls, floors, buildings |

**Think of it like this:**
- **Dynamic** = Physics is the driver (forces push it around)
- **Kinematic** = You're the driver, physics is the passenger (you move it, physics just checks for collisions)
- **Static** = Parked car (never moves)

**Example: Moving platform in Super Mario**

```csharp
public class MovingPlatform : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private float speed = 2f;
    private Vector3 startPos;
    private Vector3 endPos;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;  // ← Make it kinematic!
        //                         Now forces/gravity don't affect it
        
        startPos = transform.position;
        endPos = startPos + Vector3.right * 10f;  // Move 10m to the right
    }
    
    void FixedUpdate()
    {
        // Move platform back and forth
        float t = Mathf.PingPong(Time.time * speed, 1f);
        Vector3 targetPos = Vector3.Lerp(startPos, endPos, t);
        
        rb.MovePosition(targetPos);  // ← Use MovePosition, not transform.position!
        //                              This tells physics engine where it's moving
    }
}
```

**Why use kinematic instead of just moving `transform.position`?**

| Approach | Collision Detection? | Players can stand on it? | Physics-aware? |
|----------|---------------------|-------------------------|----------------|
| `transform.position =` | ❌ Unreliable | ❌ Player falls through | ❌ Physics doesn't "see" movement |
| `rb.MovePosition()` (kinematic) | ✅ Perfect | ✅ Player moves with platform | ✅ Physics knows exactly where it is |

**Real-world example:**
```csharp
// ❌ BAD - Moving platform using transform (breaks physics)
void Update()
{
    transform.position = Vector3.Lerp(startPos, endPos, t);
    // Result: Mario stands on platform → platform moves → Mario stays in air!
    // Physics didn't know platform moved!
}

// ✅ GOOD - Moving platform using kinematic Rigidbody
void FixedUpdate()
{
    rb.MovePosition(Vector3.Lerp(startPos, endPos, t));
    // Result: Mario stands on platform → platform moves → Mario moves with it!
    // Physics engine knows platform moved and pushes Mario along!
}
```

**Why use Time.fixedDeltaTime with kinematic?**

When you calculate movement speed in `FixedUpdate()`, use `Time.fixedDeltaTime`:

```csharp
void FixedUpdate()  // Runs 50 times/second (every 0.02s)
{
    Vector3 movement = moveDirection * speed * Time.fixedDeltaTime;
    //                                        ^^^^^^^^^^^^^^^^^^^^
    //                                        Always 0.02s - matches FixedUpdate interval!
    
    rb.MovePosition(rb.position + movement);
}
```

**Why not Time.deltaTime?**

```csharp
// ❌ WRONG - Using Time.deltaTime in FixedUpdate
void FixedUpdate()  // Runs every 0.02s (physics timestep)
{
    Vector3 movement = moveDirection * speed * Time.deltaTime;
    //                                        ^^^^^^^^^^^^^^^
    //                                        Varies with render FPS (0.016s @ 60fps)
    //                                        Doesn't match FixedUpdate timing!
    
    // Result: Jittery, inconsistent movement
}

// ✅ CORRECT - Using Time.fixedDeltaTime in FixedUpdate
void FixedUpdate()  // Runs every 0.02s
{
    Vector3 movement = moveDirection * speed * Time.fixedDeltaTime;
    //                                        ^^^^^^^^^^^^^^^^^^^^
    //                                        Always 0.02s - perfectly synchronized!
    
    // Result: Smooth, consistent movement
}
```

**Summary:**
- **Kinematic Rigidbody** = You move it, physics handles collisions
- Use `rb.MovePosition()` instead of `transform.position =`
- In `FixedUpdate()`, always use `Time.fixedDeltaTime`
- Perfect for moving platforms, elevators, doors, scripted movement

---

#### ✅ Power-Up Timer - YES Time.deltaTime
```csharp
public class PowerUpManager : MonoBehaviour
{
    private float powerUpTimer = 0f;
    [SerializeField] private float powerUpDuration = 10f;  // 10 seconds
    
    void Update()
    {
        if (isPoweredUp)
        {
            // Count down timer - YES Time.deltaTime!
            powerUpTimer -= Time.deltaTime;  // ← YES! Timing/countdown needs it!
            
            if (powerUpTimer <= 0f)
            {
                isPoweredUp = false;
                // Revert to normal Mario
            }
        }
    }
}
```
**Why Time.deltaTime?** We're tracking time, not moving objects!

---

#### ✅ Animation Blending - YES Time.deltaTime
```csharp
public class MarioAnimator : MonoBehaviour
{
    private float currentBlend = 0f;
    [SerializeField] private float blendSpeed = 5f;
    
    void Update()
    {
        // Smoothly transition between idle and run animation - YES Time.deltaTime!
        float targetBlend = isMoving ? 1f : 0f;
        currentBlend = Mathf.Lerp(currentBlend, targetBlend, blendSpeed * Time.deltaTime);
        //                                                                ^^^ YES! Smooth transitions need it!
        
        animator.SetFloat("Speed", currentBlend);
    }
}
```
**Why Time.deltaTime?** We're smoothly interpolating a value over time!

---

#### ❌ Goomba Enemy - NO Time.deltaTime (if using Rigidbody)
```csharp
public class GoombaEnemy : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private float moveSpeed = 2f;
    
    void FixedUpdate()
    {
        // Goomba walks left/right - NO Time.deltaTime
        rb.velocity = new Vector3(moveDirection * moveSpeed, rb.velocity.y, 0f);
        //                                                   ^^^ NO Time.deltaTime!
    }
}
```
**Why no Time.deltaTime?** Goomba has Rigidbody for collision detection!

---

### 📊 Summary Table: Mario Bros Components

| Object | Has Rigidbody? | Uses Time.deltaTime? | Why? |
|--------|----------------|---------------------|------|
| **Mario (player)** | ✅ Yes | ❌ No | Physics movement (`rb.velocity`, `rb.AddForce`) |
| **Camera** | ❌ No | ✅ Yes | Direct transform manipulation (Lerp position) |
| **Coins (floating)** | ❌ No | ✅ Yes | Decorative - floats in mid-air, just spins visually |
| **Coins (dropped)** | ✅ Yes | ❌ No | If coins should fall/bounce (rare) |
| **Moving Platforms** | ✅ Yes (Kinematic) | ⚠️ Use `fixedDeltaTime` | Kinematic Rigidbody movement |
| **Power-Up Timers** | N/A | ✅ Yes | Time tracking/countdown |
| **Animation Blending** | N/A | ✅ Yes | Smooth value interpolation |
| **Goombas (enemies)** | ✅ Yes | ❌ No | Physics movement |
| **Fireballs** | ✅ Yes | ❌ No | Physics projectiles (`rb.velocity`) |
| **UI Fading** | N/A | ✅ Yes | Changing alpha over time |
| **Particle Effects** | N/A | ✅ Yes | Custom particle movement |

**Key insight about coins:**
- **Floating coins** (99% of games): No Rigidbody - they're decorative objects that stay in place and spin
- **Dropped coins** (rare): With Rigidbody - they fall, bounce, and roll realistically

---

### 🔢 Unity Physics Units

**Yes, Unity uses real-world units:**

| Property | Unit | Example |
|----------|------|---------|
| **Mass** | Kilograms (kg) | `rb.mass = 1f` (1 kg) |
| **Velocity** | Meters/second (m/s) | `rb.velocity = Vector3(5, 0, 0)` (5 m/s to the right) |
| **Force** | Newtons (N) | `rb.AddForce(Vector3.up * 10f)` (10 Newtons upward) |
| **Distance** | Meters (m) | `Vector3(0, 10, 0)` (10 meters up) |
| **Gravity** | Meters/second² (m/s²) | Default: `-9.81 m/s²` (Earth's gravity) |
| **Time** | Seconds (s) | `Time.deltaTime` (seconds since last frame) |

**Real-world comparison:**
- **Mario's mass = 1 kg** (light, for floaty jumps)
- **Goomba's mass = 0.5 kg** (even lighter)
- **Moving platform = 100 kg** (heavy, doesn't get pushed by Mario)
- **Jump force = 10 N** (10 Newtons upward)
- **Move speed = 5 m/s** (5 meters per second, about 11 mph)

---

### 🧠 The Golden Rule (Expanded)

**Use Time.deltaTime when:**
- ✅ Directly modifying **transform** (position, rotation, scale)
- ✅ **Timers** and countdowns
- ✅ **Smooth interpolation** (Lerp, Slerp)
- ✅ **Animation blending** values
- ✅ **UI animations** (fading, sliding)
- ✅ Code in **Update()**

**DON'T use Time.deltaTime when:**
- ❌ Setting **rb.velocity** (already in m/s)
- ❌ Calling **rb.AddForce()** (physics handles it)
- ❌ Code in **FixedUpdate()** with Rigidbody
- ❌ Using **rb.MovePosition()** (use `Time.fixedDeltaTime` instead)

**Special case: Kinematic Rigidbodies**
```csharp
// Moving platforms, elevators, doors
rb.MovePosition(rb.position + moveVector * Time.fixedDeltaTime);
//                                         ^^^^^^^^^^^^^^^^^^^^
//                                         Use fixedDeltaTime for kinematic!
```

---

### Quick Quiz (Ask Student):

1. **What does a Rigidbody component do?**
   - Answer: Gives an object physics properties (gravity, mass, velocity, collisions)

2. **Should we change `transform.position` when using Rigidbody?**
   - Answer: No! Use `rb.velocity` or `rb.AddForce()` instead

3. **Where should physics code go: Update() or FixedUpdate()?**
   - Answer: FixedUpdate() - physics runs at fixed timesteps

**Great! Now let's dive into velocity and forces!**

---

## Part 2: Understanding Rigidbody & Velocity (15 minutes)

### What is Velocity?

**Velocity** = Speed + Direction

In Unity:
```csharp
Vector3 velocity = rb.velocity;
```

This gives you three numbers:
- **X velocity** - How fast moving left/right (m/s)
- **Y velocity** - How fast moving up/down (m/s)
- **Z velocity** - How fast moving forward/back (m/s)

**Examples:**

| Velocity | What's happening |
|----------|------------------|
| (0, 0, 0) | Not moving (stationary) |
| (5, 0, 0) | Moving right at 5 m/s |
| (0, -10, 0) | Falling down at 10 m/s |
| (3, 0, 4) | Moving diagonally (right + forward) |

---

### Changing Velocity Directly

**Method 1: Set velocity directly** (good for precise control)

```csharp
rb.velocity = new Vector3(5, 0, 0);  // Move right at 5 m/s
```

**Use when:** You want exact speed control (hovercraft, spaceship in space)

**Pros:**
- ✅ Precise control
- ✅ Immediate response
- ✅ Simple to understand

**Cons:**
- ⚠️ No acceleration/deceleration (feels "floaty")
- ⚠️ Ignores momentum (not realistic)

---

### Real Example: Velocity-Based Movement

**Let's build a simple controller:**

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

**🔍 Why do we need these three lines? Let's break it down:**

---

### Understanding the Y Velocity Problem

**Question 1: Why can't we just do this?**

```csharp
// ❌ BAD - This breaks gravity!
rb.velocity = new Vector3(moveDirection.x * moveSpeed, 0, moveDirection.z * moveSpeed);
//                                                      ^
//                                                      Setting Y to 0!
```

**What happens:**
- Frame 1: Gravity makes you fall → Y velocity = -2 m/s (falling)
- Frame 2: Your code sets Y = 0 → Y velocity = 0 (stops falling!)
- Frame 3: Gravity makes you fall → Y velocity = -2 m/s (falling)
- Frame 4: Your code sets Y = 0 → Y velocity = 0 (stops falling!)

**Result:** You hover in mid-air like a glitching game! 🐛

**Visual:**
```
Without preserving Y velocity:
[Cube] ↓ (gravity pulls down)
       ↑ (your code resets to 0)
       ↓ (gravity pulls down)
       ↑ (your code resets to 0)
= Jittery hovering! ❌

With preserving Y velocity:
[Cube] ↓ (gravity pulls down)
       ↓ (your code keeps it falling)
       ↓ (gravity continues to pull)
= Smooth falling! ✅
```

---

### Understanding Why We Read rb.velocity First

**Question 2: Why do we need `Vector3 newVelocity = rb.velocity`?**

**The key insight: Velocity has THREE separate values (X, Y, Z) that change independently!**

**What's actually happening each frame:**

```
Frame 1:
- Gravity is pulling down → Y velocity = -1 m/s
- You press W → Need X velocity = 0, Z velocity = 5 m/s
- Current rb.velocity = (0, -1, 0)  ← Y is falling!

Frame 2:
- Gravity keeps pulling → Y velocity = -2 m/s (falling faster!)
- You're still pressing W → Need X = 0, Z = 5
- Current rb.velocity = (0, -2, 5)  ← Y is falling faster!

Frame 3:
- Gravity keeps pulling → Y velocity = -3 m/s
- Now you press D too → Need X = 5, Z = 5
- Current rb.velocity = (0, -3, 5)  ← Y keeps changing!
```

**We need to:**
1. **Read** the current velocity (especially Y, which gravity is changing)
2. **Modify** only X and Z (horizontal movement)
3. **Keep** Y unchanged (let gravity do its job)

---

### The Code Step-by-Step

```csharp
// Step 1: Get the CURRENT velocity (includes gravity's effect on Y)
Vector3 newVelocity = rb.velocity;  
// Example: newVelocity = (0, -2.5, 3)
//                          ^   ^    ^
//                          X   Y    Z
//                          
// Y = -2.5 means "falling at 2.5 m/s downward"
// We NEED to keep this value!

// Step 2: Change ONLY X and Z based on input
newVelocity.x = moveDirection.x * moveSpeed;  // You control left/right
newVelocity.z = moveDirection.z * moveSpeed;  // You control forward/back
// Example after pressing W+D:
// newVelocity = (5, -2.5, 5)
//                ^   ^    ^
//              NEW  OLD  NEW
//              
// X and Z = your movement input
// Y = still -2.5 (gravity still working!)

// Step 3: Apply the modified velocity back
rb.velocity = newVelocity;
// Object now moves right+forward while falling!
```

---

### Visual Comparison

**❌ WRONG WAY - Creating new Vector3:**
```csharp
// This ALWAYS sets Y to 0, losing gravity's effect!
rb.velocity = new Vector3(moveDirection.x * moveSpeed, 
                         0,  // ← ALWAYS ZERO! Gravity lost!
                         moveDirection.z * moveSpeed);

// What happens:
// Frame 1: Gravity → Y = -1, Your code → Y = 0 (fight!)
// Frame 2: Gravity → Y = -1, Your code → Y = 0 (fight!)
// Frame 3: Gravity → Y = -1, Your code → Y = 0 (fight!)
// Result: Hovering bug! ❌
```

**✅ CORRECT WAY - Reading and modifying:**
```csharp
Vector3 newVelocity = rb.velocity;        // Read current (Y = -2.5)
newVelocity.x = moveDirection.x * moveSpeed;  // Change X
newVelocity.z = moveDirection.z * moveSpeed;  // Change Z
rb.velocity = newVelocity;                // Y still -2.5!

// What happens:
// Frame 1: Gravity → Y = -1, Your code → Y stays -1 ✅
// Frame 2: Gravity → Y = -2, Your code → Y stays -2 ✅
// Frame 3: Gravity → Y = -3, Your code → Y stays -3 ✅
// Result: Falls naturally while moving! ✅
```

---

### Real-World Analogy

Think of velocity like a car's speed on a hill:

**Bad approach (setting Y to 0):**
- Gravity makes car roll down hill
- You keep hitting the brakes every millisecond
- Car judders and fights itself
- Feels broken!

**Good approach (preserving Y):**
- Gravity makes car roll down hill naturally
- You only control steering (X) and gas pedal (Z)
- Let gravity handle the downward motion
- Feels smooth!

---

### When Would You Set Y?

**You DO set Y when implementing jumping:**

```csharp
void FixedUpdate()
{
    // Movement (keep Y)
    Vector3 newVelocity = rb.velocity;
    newVelocity.x = moveDirection.x * moveSpeed;
    newVelocity.z = moveDirection.z * moveSpeed;
    
    // Jumping (SET Y to override gravity temporarily)
    if (shouldJump && isGrounded)
    {
        newVelocity.y = jumpSpeed;  // ← Override Y for jump!
        shouldJump = false;
    }
    
    rb.velocity = newVelocity;
}
```

**When jumping, you WANT to override Y** - that's the whole point! But during normal movement, leave it alone.

---

### Summary

**Why we do this:**
```csharp
Vector3 newVelocity = rb.velocity;        // 1. Read current velocity (includes gravity)
newVelocity.x = moveDirection.x * moveSpeed;  // 2. Change horizontal (X)
newVelocity.z = moveDirection.z * moveSpeed;  // 3. Change forward/back (Z)
rb.velocity = newVelocity;                // 4. Apply (Y unchanged!)
```

**Key insights:**
- ✅ Velocity has 3 independent components: X, Y, Z
- ✅ Gravity changes Y every frame (makes it more negative)
- ✅ Your movement controls X and Z (horizontal)
- ✅ You must preserve Y so gravity keeps working
- ✅ Reading `rb.velocity` first gives you the current Y value
- ✅ Only override Y when you have a specific reason (jumping, flying, etc.)

**Think of it as:** "I'm the captain of horizontal movement (X, Z), gravity is the captain of vertical movement (Y)"

---

### Limiting Speed

**Why do we need this?**

We actually need TWO different solutions for TWO different problems:

1. **Normalization** - Makes all directions move at the same speed (fixes diagonal being faster)
2. **Speed Capping** - Prevents velocity from growing infinitely (fixes forces/slopes accelerating forever)

Let's understand both problems and their solutions:

---

When you set velocity directly, nothing stops the player from going infinitely fast! Consider these scenarios:

**Problem 1: Diagonal movement WITHOUT normalization is faster**

Imagine if you DIDN'T normalize the direction (we do in our code, but let's see why it matters):

```csharp
// Player presses W only (forward)
moveDirection = Vector3.forward;           // (0, 0, 1)
velocity = moveDirection * 5;              // (0, 0, 5)
// Speed = 5 m/s ✅

// Player presses D only (right)  
moveDirection = Vector3.right;             // (1, 0, 0)
velocity = moveDirection * 5;              // (5, 0, 0)
// Speed = 5 m/s ✅

// Player presses W + D (diagonal)
moveDirection = Vector3.forward + Vector3.right;  // (1, 0, 1)
velocity = moveDirection * 5;              // (5, 0, 5)
// Speed = √(5² + 5²) = √50 = 7.07 m/s ❌ FASTER!
```

**📐 Quick Math Lesson: Pythagorean Theorem**

The Pythagorean theorem tells us how to find the length of the longest side of a right triangle (the diagonal):

```
        c (hypotenuse - diagonal)
       /|
      / |
     /  | b (vertical)
    /   |
   /    |
  ------
    a (horizontal)

Formula: a² + b² = c²
Or: c = √(a² + b²)
```

**Real-world example:** Walking diagonally across a rectangular field
- Walking along one edge = 3 meters
- Walking along the other edge = 4 meters
- Walking diagonally = √(3² + 4²) = √(9 + 16) = √25 = **5 meters**

**In our game:**
- Forward movement = 5 m/s
- Right movement = 5 m/s
- Diagonal movement = √(5² + 5²) = √(25 + 25) = √50 = **7.07 m/s**

---

**Why is 7.07 a problem?**

7.07 m/s is mathematically correct, but it creates an **unfair gameplay problem**:

```
Forward only (W):       Diagonal (W+D):
    5 m/s                  7.07 m/s
      ↑                       ↗

Player A goes straight: 5 m/s
Player B goes diagonal: 7.07 m/s (41% FASTER!)

In a race, Player B wins just by moving diagonally! 🏁
```

**The gameplay problem:**
- You want moveSpeed = 5 to mean "player moves at 5 m/s in ANY direction"
- But without normalization: straight = 5 m/s, diagonal = 7.07 m/s
- Players learn to ONLY move diagonally (never straight!) because it's faster
- This feels broken and exploitable!

**Real game example:**
```
Imagine a race across a field:
- Player 1: Runs straight north → Takes 10 seconds
- Player 2: Runs diagonal northeast → Takes 7 seconds (FASTER!)
- Player 2 wins even though both have same "speed" setting!

This is like a racing game where turning makes you go faster - broken! 🏎️
```

**Visual comparison:**
```
WITHOUT Normalize (BROKEN):          WITH Normalize (CORRECT):
┌─────────┐                         ┌─────────┐
│ Finish  │                         │ Finish  │
└─────────┘                         └─────────┘
     ↑                                   ↑
     │ 5 m/s                             │ 5 m/s
     │ 10 sec                            │ 10 sec
     │                                ↗ ↑
     │                          7.07/  │ Both arrive
 ↗ 7.07 m/s                    5m/s  │ at SAME TIME!
/  │ 7 sec (FASTER!)                 │
   │                                  │
   Start                              Start

Left: Diagonal is faster (unfair!)
Right: All directions same speed (fair!)
```

**That's why we normalize!** 

---

### 📏 Understanding Normalization - The Simple Truth

**The problem:** When you press W+D (diagonal), you're adding two vectors that create a LONGER arrow than pressing just W or just D.

**Simple explanation:**
```csharp
// Pressing W only:
direction = (0, 0, 1)  // Arrow length = 1
velocity = direction * 5 = (0, 0, 5)  
// Speed = 5 m/s ✅

// Pressing W+D (diagonal):
direction = (0, 0, 1) + (1, 0, 0) = (1, 0, 1)  // Arrow length = 1.414 (LONGER!)
velocity = direction * 5 = (5, 0, 5)
// Speed = 7.07 m/s ❌ TOO FAST because arrow was longer!
```

**The solution: Make all direction arrows the SAME LENGTH (1.0)**

```csharp
direction = (1, 0, 1)           // Arrow length = 1.414 (too long)
direction.Normalize()           // Shrink arrow to length = 1.0

// How? Divide each component by the length:
// X: 1 / 1.414 = 0.707
// Y: 0 / 1.414 = 0
// Z: 1 / 1.414 = 0.707

// Now: (0.707, 0, 0.707)       // Arrow length = 1.0 ✅

velocity = direction * 5        // All directions now multiply by same amount!
// Speed = 5 m/s ✅ FAIR!
```

**Key insight:**
- **Normalize = Resize all direction arrows to length 1.0**
- **Then multiply by speed → ALL directions move at the SAME speed**
- **Result: Fair gameplay - diagonal isn't faster!**

**Think of it like this:**
- Without normalize: Different directions have different arrow lengths → unfair speeds
- With normalize: All directions have length 1.0 → multiply by speed = fair speeds

---

Normalizing makes ALL directions the same speed:
```csharp
// WITHOUT normalize:
moveDirection = Vector3.forward + Vector3.right;  // (1, 0, 1)
// Direction arrow length = √(1² + 1²) = 1.414 (LONGER than other directions!)
velocity = moveDirection * 5;                     // (5, 0, 5)
// Speed = √(5² + 5²) = 7.07 m/s ❌ TOO FAST! (because we multiplied a longer arrow by 5)

// WITH normalize:
moveDirection = (Vector3.forward + Vector3.right).normalized;  // (0.707, 0, 0.707)
// Direction arrow length = 1.0 exactly! (SAME as all other directions)
velocity = moveDirection * 5;                     // (3.54, 0, 3.54)  
// Speed = √(3.54² + 3.54²) = 5 m/s ✅ CORRECT! (because we multiplied a length-1 arrow by 5)
```

**The key insight:**
- **Normalize** = Resize direction arrow to length 1.0
- **Then multiply by speed** = Now ALL directions move at exactly 5 m/s
- **Result**: Fair gameplay - direction doesn't matter, only the speed setting!

**✅ Problem 1 SOLVED with normalization!** Our code does this correctly.

---

### 📌 When to Use Normalize - Quick Cheat Sheet

**Simple rule: "Do I care HOW FAR or just WHICH WAY?"**
- **Which way only** → Normalize
- **How far matters** → Don't normalize

#### ✅ Use `.normalized` when you want DIRECTION ONLY (ignore distance/length):

1. **Player input for movement**
   ```csharp
   Vector3 input = new Vector3(moveX, 0, moveZ).normalized;
   rb.velocity = input * speed; 
   // Diagonal (1,0,1) becomes same speed as straight (1,0,0)
   ```

2. **Calculating which way to move/push**
   ```csharp
   Vector3 direction = (target.position - transform.position).normalized;
   transform.position += direction * speed; 
   // Move toward target at consistent speed
   ```

3. **Gravity/forces toward a point**
   ```csharp
   Vector3 direction = (planet.position - transform.position).normalized;
   rb.AddForce(direction * gravityStrength); 
   // Pull toward planet with same strength regardless of distance
   ```

4. **Looking at something**
   ```csharp
   Vector3 direction = (enemy.position - transform.position).normalized;
   transform.forward = direction; 
   // Face enemy
   ```

#### ❌ DON'T Normalize when:

1. **You need the actual distance**
   ```csharp
   float distance = Vector3.Distance(a, b); 
   // Keep the distance value
   ```

2. **You want distance to affect the result**
   ```csharp
   Vector3 offset = target.position - transform.position;
   float strength = 1f / offset.magnitude; 
   // Weaker when farther = need magnitude
   ```

**Why normalize for gravity example?**

Without `.normalized`, the vector length = distance to planet:
```csharp
// Planet is 100 units away
Vector3 direction = planet.position - transform.position; // Length = 100!
rb.AddForce(direction * gravityStrength); 
// Farther objects get STRONGER pull (backwards!)

// With normalize:
Vector3 direction = (planet.position - transform.position).normalized; // Length = 1.0
// Now we control gravity strength separately with our own formula
float distance = Vector3.Distance(planet.position, transform.position);
float strength = gravityStrength / (distance * distance); // Inverse square law
rb.AddForce(direction * strength); 
// Direction (which way) and strength (how hard) are separate! ✅
```

---

**But wait - why do we ALSO need speed capping?**

Normalization fixes diagonal movement, but there are OTHER ways velocity can grow too large:

---

**Problem 2: External forces add to your velocity**
```csharp
// Your movement sets velocity to 5 m/s
rb.velocity = new Vector3(5, 0, 0);  // Moving right at 5 m/s

// But then an explosion pushes you!
rb.AddForce(Vector3.right * 1000f, ForceMode.Impulse);

// Now your velocity could be 50 m/s or more! 
// No speed limit = player zooming uncontrollably! 😱
```

**Problem 3: Forces accumulate over time**
```csharp
// If you use AddForce for movement instead of setting velocity directly:
void FixedUpdate()
{
    if (Keyboard.current.wKey.isPressed)
        rb.AddForce(Vector3.forward * 10f);
    
    // Frame 1: velocity = 0.2 m/s
    // Frame 2: velocity = 0.4 m/s
    // Frame 3: velocity = 0.6 m/s
    // ...
    // Frame 100: velocity = 20 m/s (way too fast!)
    // Frame 500: velocity = 100 m/s (player can't control it!)
}
```

**Problem 4: Slopes and ramps can accelerate you**
```csharp
// Player runs down a steep hill
// Gravity pulls down, ground pushes sideways
// Velocity keeps increasing: 5 m/s → 10 m/s → 20 m/s → 50 m/s!
// Without a speed cap, player slides out of control down any slope!
```

**Real-world example:**
- Mario has a max run speed - you can't run faster no matter how long you hold the button
- Racing games have top speeds - even with boost, you eventually cap out
- Character controllers need limits - otherwise one strong push sends you flying forever

---

**✅ Solution for Problems 2-4: Cap the maximum speed!**

This is DIFFERENT from normalization:
- **Normalization** = Makes all DIRECTIONS equal speed (fixes diagonal)
- **Speed capping** = Prevents velocity from growing too large (fixes forces/slopes)

Both are needed for good gameplay!

---

### Implementing Speed Capping

**Now let's add a maximum speed limit to prevent infinite acceleration:**

```csharp
[SerializeField] private float maxSpeed = 10f;  // Add this at the top with other fields

void FixedUpdate()
{
    // ...existing movement code...
    
    // Call this at the end of FixedUpdate
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
```

**What this does:**
1. Extract horizontal velocity (ignore Y for jumping)
2. Check if it's too fast (magnitude > maxSpeed)
3. If too fast: Normalize to length 1.0, then multiply by maxSpeed (scales down while keeping direction)
4. Apply it back (keeping vertical velocity for gravity/jumping)

**Example:**
```csharp
// Player is going too fast from an explosion:
horizontalVelocity = (15, 0, 15)  // Speed = 21.2 m/s (way too fast!)

// Check: 21.2 > 10? Yes, too fast!
// Normalize: (15, 0, 15) → (0.707, 0, 0.707)  // Length = 1.0, same direction
// Multiply by maxSpeed: (0.707, 0, 0.707) * 10 = (7.07, 0, 7.07)
// Final speed: √(7.07² + 7.07²) = 10 m/s ✅ Capped, but same direction!
```

---

## Part 3: Forces vs Velocity (10 minutes)

### What are Forces?

**Force** = A push or pull that changes velocity over time

**Real-world examples:**
- Pushing a shopping cart - force makes it accelerate
- Car engine - force pushes car forward
- Rocket thrust - force accelerates rocket

**In Unity:**
```csharp
rb.AddForce(Vector3.forward * 10f, ForceMode.Force);
```

---

### ForceMode Types

Unity has different ways to apply forces:

| ForceMode | What it does | Use for |
|-----------|--------------|---------|
| `Force` | Continuous push (considers mass) | Engines, rockets, wind |
| `Impulse` | Instant burst (considers mass) | Jumps, explosions, hits |
| `Acceleration` | Continuous push (ignores mass) | Gravity-like effects |
| `VelocityChange` | Instant velocity change (ignores mass) | Direct control |

---

### Force vs Impulse - Visual Example

**Force (continuous push):**
```csharp
void FixedUpdate()
{
    rb.AddForce(Vector3.forward * 10f, ForceMode.Force);
    // Applied every physics frame - gradual acceleration
}
```

**Timeline:**
```
Frame 1: velocity = 0 → 0.2 m/s
Frame 2: velocity = 0.2 → 0.4 m/s
Frame 3: velocity = 0.4 → 0.6 m/s
...gradually speeds up!
```

**Impulse (instant burst):**
```csharp
void Update()
{
    if (Keyboard.current.spaceKey.wasPressedThisFrame)
    {
        rb.AddForce(Vector3.up * 5f, ForceMode.Impulse);
        // Applied ONCE - instant velocity change
    }
}
```

**Timeline:**
```
Before: velocity = (0, 0, 0)
After:  velocity = (0, 5, 0)  ← instant jump!
```

---

### Velocity vs Forces - When to Use Each

**Use Velocity (`rb.velocity =`) when:**
- ✅ You want precise, responsive control
- ✅ Making arcade-style games (not realistic)
- ✅ Top-down games, spaceships, hovercrafts

**Use Forces (`rb.AddForce()`) when:**
- ✅ You want realistic physics with momentum
- ✅ Making cars, characters, physics puzzles
- ✅ Want acceleration/deceleration feel

**Example comparison:**

**Velocity approach (arcade-style):**
```csharp
// Press W = immediately move at 5 m/s
rb.velocity = new Vector3(0, rb.velocity.y, 5);
// Release W = immediately stop
rb.velocity = new Vector3(0, rb.velocity.y, 0);
```
**Feels:** Snappy, responsive, "floaty"

**Force approach (realistic):**
```csharp
// Press W = gradually accelerate
rb.AddForce(Vector3.forward * 10f, ForceMode.Force);
// Release W = gradually decelerate due to drag
```
**Feels:** Smooth, momentum-based, realistic

---

### Understanding Drag

**Drag** = Air resistance that slows objects down

```csharp
rb.drag = 5f;  // Higher = stops faster
rb.drag = 0f;  // No drag = keeps moving forever (like space!)
```

**Practical use:**
- **Ground drag = 5-10** - Object stops quickly when you stop pushing
- **Air drag = 0.5-2** - Less resistance when jumping/flying
- **Space drag = 0** - No air in space, momentum preserved forever!

**Example: Different drag when grounded vs airborne:**
```csharp
void UpdateDrag()
{
    if (isGrounded)
        rb.drag = 5f;  // High drag on ground - stops quickly
    else
        rb.drag = 0.5f;  // Low drag in air - keeps momentum
}
```

---

## Part 4: BREAK TIME! (5 minutes) 🧘

Stand up! Stretch! Rest your eyes!

**Physics stretch:** 
- Jump in place - feel the force your legs apply!
- Push against a wall - that's you applying force!
- Slide on a smooth floor - that's low drag!
- Walk on carpet - that's high drag!

**You just experienced forces, impulses, and drag in real life!** 🎯

---

## Part 5: Custom Gravity Simulation (15 minutes)

### Understanding Gravity

**Unity's built-in gravity:**
- Pulls everything down at `-9.81 m/s²` (Earth's gravity)
- Always points down (Vector3.down)
- Controlled by `rb.useGravity = true/false`

**But what if we want:**
- Gravity that pulls toward a planet? 🪐
- Multiple gravity sources? 🌍🌙
- Stronger or weaker gravity? 🚀

**Answer: Make our own!**

---

### Custom Gravity - The Math

**Real gravity formula:**
```
F = G × (m₁ × m₂) / r²
```

Where:
- `F` = Force
- `G` = Gravitational constant
- `m₁, m₂` = Masses of two objects
- `r` = Distance between them

**Simplified for Unity:**
```csharp
// Direction from object TO planet
Vector3 direction = (planet.position - transform.position).normalized;

// Distance
float distance = Vector3.Distance(transform.position, planet.position);

// Force magnitude (inverse square law)
float forceMagnitude = gravitationalConstant / (distance * distance);

// Final force
Vector3 force = direction * forceMagnitude * rb.mass;

// Apply it!
rb.AddForce(force, ForceMode.Force);
```

---

### Step-by-Step: Building Custom Gravity

**Step 1: Disable Unity's gravity**
```csharp
void Start()
{
    rb = GetComponent<Rigidbody>();
    rb.useGravity = false;  // We're making our own!
}
```

**Step 2: Calculate gravitational force**
```csharp
void FixedUpdate()
{
    // Find direction to gravity source (planet)
    Vector3 directionToPlanet = gravitySource.position - transform.position;
    float distance = directionToPlanet.magnitude;
    
    // Normalize to get just the direction
    Vector3 forceDirection = directionToPlanet.normalized;
    
    // Calculate force magnitude using inverse square law
    float forceMagnitude = gravitationalConstant / (distance * distance);
    
    // Create force vector
    Vector3 gravitationalForce = forceDirection * forceMagnitude * rb.mass;
    
    // Apply the force
    rb.AddForce(gravitationalForce, ForceMode.Force);
}
```

**Step 3: Add safety checks**
```csharp
// Prevent extreme forces at very close distances
if (distance < 0.1f) return;
```

---

### Inverse Square Law - Why?

**Without inverse square (constant force):**
```csharp
float forceMagnitude = gravitationalConstant;  // Always 10
```
- Object 1 meter away: Force = 10
- Object 10 meters away: Force = 10
- **Problem:** Unrealistic! Gravity should be weaker far away!

**With inverse square law:**
```csharp
float forceMagnitude = gravitationalConstant / (distance * distance);
```
- Object 1 meter away: Force = 10 / (1×1) = **10** 
- Object 2 meters away: Force = 10 / (2×2) = **2.5** (weaker!)
- Object 10 meters away: Force = 10 / (10×10) = **0.1** (much weaker!)

**This is how real gravity works!** The Moon pulls on Earth, but less than if it were closer! 🌍🌙

---

### Making Objects Orbit

**To orbit a planet, you need:**
1. Gravity pulling you toward it
2. Sideways velocity to "miss" the planet

**Give initial velocity:**
```csharp
void Start()
{
    rb = GetComponent<Rigidbody>();
    rb.useGravity = false;
    
    // Give sideways velocity for orbit
    rb.velocity = transform.right * 5f;  // Adjust speed to get circular orbit
}
```

**What happens:**
1. Gravity pulls you toward planet
2. Sideways velocity makes you "fall around" the planet
3. If speed is just right = circular orbit! 🛰️

**Experiment with different speeds:**
- Too slow = spirals into planet 💥
- Just right = circular orbit ✅
- Too fast = escapes into space 🚀

---

## Part 6: Build & Experiment! (10 minutes)

### Setup: Create the Scene

**Step 1: Create a Planet**
1. Create a Sphere (GameObject → 3D Object → Sphere)
2. Name it "Planet"
3. Scale it up: `(5, 5, 5)`
4. Position at `(0, 0, 0)`
5. Add a material to make it look like a planet (optional)

**How to add a material:**
1. In **Project** window, right-click → `Create` → `Material`
2. Name it `"PlanetMaterial"`
3. Select the material, then in **Inspector**:
   - Click the color square next to `Base Map` or `Albedo`
   - Choose a color (blue, orange, red, etc.)
4. Drag the material onto the Planet sphere in the Hierarchy or Scene view
5. **Optional:** Adjust `Metallic` (shininess) and `Smoothness` sliders for different looks

**Step 2: Create Player Object**
1. Create a Capsule
2. Name it "Player"
3. Position at `(0, 8, 0)` (above the planet)
4. Add Component → Rigidbody
5. Set Rigidbody Mass = 1

---

### Experiment 1: Velocity Controller

**Attach `PhysicsMover.cs` to Player:**

1. Drag `PhysicsMover` script onto Player
2. In Inspector, set:
   - Move Speed = 5
   - Max Speed = 10
   - Use Custom Gravity = **false** (use Unity's gravity first)

3. Press Play and use WASD to move
4. Watch it fall due to gravity!

**Try changing:**
- Move Speed (1, 5, 10, 20) - how responsive does it feel?
- Max Speed - can you make it cap at 3 m/s?
- Enable Custom Gravity and set strength to -20 - super heavy!

---

### Experiment 2: Force Controller

**Swap to `ForceController.cs`:**

1. Remove PhysicsMover, add ForceController
2. Set:
   - Movement Force = 10
   - Max Speed = 8
   - Jump Force = 5
   - Ground Drag = 5

3. Press Play - use WASD and Space to jump!

**Notice the difference?**
- More momentum (slides when you stop)
- Acceleration feel (takes time to reach full speed)
- Jump feels more "weighty"

**Experiment:**
- Set drag = 0 - it slides forever! (like ice)
- Set drag = 20 - stops instantly (like sticky tar)
- Set jump force = 20 - super jump!

---

### Experiment 3: Planetary Gravity! 🪐

**Create orbital mechanics:**

1. Remove other scripts, add `CustomGravityObject.cs`
2. Drag the Planet object into the "Gravity Source" field
3. Set:
   - Gravitational Constant = 10
   - Use Inverse Square = true
   - Movement Force = 5

4. **IMPORTANT:** In Start() or Inspector, give initial sideways velocity!

**Manually give velocity in Inspector:**
- Play the game
- While playing, change Rigidbody velocity to `(5, 0, 0)`
- Watch it orbit!

**Or modify the script:**
```csharp
void Start()
{
    // ...existing code...
    
    // Give initial orbital velocity
    rb.velocity = transform.right * 5f;
}
```

**Experiment:**
- Try different gravitational constants (5, 10, 20, 50)
- Toggle "Use Inverse Square" on/off - see the difference!
- Change initial velocity (3, 5, 8, 12) - find perfect orbit speed!
- Move player further away - gravity gets weaker!

---

### Challenge: Multi-Planet System

**Create a solar system:**

1. Create 3 spheres (different sizes):
   - Sun (scale 10,10,10) at (0,0,0)
   - Planet 1 (scale 3,3,3) at (15,0,0)
   - Moon (scale 1,1,1) at (20,0,0)

2. Create a small sphere "Spaceship"
3. Attach CustomGravityObject
4. Set Gravity Source to "Sun"
5. Give it sideways velocity

**Can you:**
- Make it orbit the Sun? ☀️
- Switch gravity source to Planet 1 mid-game?
- Create a figure-8 pattern?

---

## Part 7: Wrap-Up & Homework (5 minutes)

### What You Accomplished Today! 🎉

- ✅ Understood Rigidbody and physics simulation
- ✅ Learned difference between Update() and FixedUpdate()
- ✅ Mastered velocity vs forces
- ✅ Implemented ForceMode.Force and ForceMode.Impulse
- ✅ Created custom gravity simulation
- ✅ Understood inverse square law
- ✅ Made objects orbit a planet!
- ✅ Experimented with drag and momentum

---

### Homework Challenges

### Challenge 1: Perfect Circular Orbit 🛰️

**Goal:** Create a stable circular orbit around a planet

1. Create planet (sphere, scale 5,5,5)
2. Create orbiter (sphere, scale 0.5,0.5,0.5)
3. Attach CustomGravityObject
4. Find the perfect combination of:
   - Distance from planet
   - Gravitational constant
   - Initial sideways velocity
5. Make it orbit for 60 seconds without crashing or escaping!

**Bonus:** Create 3 objects orbiting at different distances (like planets around the sun)

---

### Challenge 2: Force-Based Platformer

**Goal:** Create a simple obstacle course using ForceController

1. Build a platform course (use cubes as platforms)
2. Add ramps, gaps to jump over
3. Use ForceController for movement
4. Tune the values until it feels good:
   - How much force for movement?
   - How much for jumping?
   - What drag feels best?
5. Time yourself completing the course!

**Bonus:** Add moving platforms or bounce pads (high impulse upward)

---

### Challenge 3: Zero-Gravity Spaceship

**Goal:** Create a spaceship control in zero gravity (no drag, momentum preserved)

1. Create a capsule "Spaceship"
2. Add Rigidbody
3. Set:
   - Use Gravity = false
   - Drag = 0 (space has no air!)
   - Angular Drag = 0
4. Create a script that applies small forces for movement
5. Try to navigate to waypoints without overshooting!

**This is hard because momentum is preserved - every push keeps you moving!**

**Bonus:** Add rotation controls with `rb.AddTorque()`

---

## Troubleshooting Guide

### "Object passes through colliders!"
**Check:**
- Does object have Rigidbody?
- Does object have Collider?
- Is Rigidbody set to Continuous (not Discrete)?
- Collision Detection → Set to "Continuous Dynamic"

### "Movement is jittery/choppy!"
**Solution:**
- Make sure physics code is in FixedUpdate(), not Update()
- Check Rigidbody → Interpolate = "Interpolate" or "Extrapolate"

### "Object rotates randomly when moving!"
**Solution:**
- Rigidbody → Constraints → Freeze Rotation X, Y, Z

### "Gravity doesn't affect my object!"
**Check:**
- Rigidbody → Use Gravity is checked
- Mass is > 0
- If using custom gravity, is gravity source assigned?

### "Orbital object crashes into planet!"
**Try:**
- Increase initial sideways velocity
- Increase starting distance from planet
- Reduce gravitational constant

### "Orbital object flies away into space!"
**Try:**
- Decrease initial sideways velocity
- Increase gravitational constant
- Make sure "Use Inverse Square" is enabled

---

## Teacher Notes

### Key Teaching Points

1. **Rigidbody Fundamentals**
   - Core of Unity physics
   - Never mix direct transform changes with physics
   - Mass, drag, velocity are key properties

2. **Update vs FixedUpdate**
   - Physics runs at fixed timestep
   - Input can be in Update(), physics changes in FixedUpdate()
   - Critical for stable physics simulation

3. **Velocity vs Forces**
   - Velocity = direct control (arcade feel)
   - Forces = realistic physics (momentum, acceleration)
   - Choose based on desired game feel

4. **ForceMode Understanding**
   - Force = continuous (engines, thrust)
   - Impulse = instant (jumps, explosions)
   - Explain with real-world examples

5. **Custom Gravity**
   - Inverse square law matches real physics
   - Direction matters (toward gravity source)
   - Initial velocity needed for orbits

### Common Student Questions

**"Why use physics instead of just moving transform.position?"**
- Physics gives realistic collisions, momentum, and interactions
- Objects can push each other, respond to explosions, etc.
- Makes game feel more immersive and realistic

**"When should I use velocity vs forces?"**
- Velocity for responsive, arcade-style controls
- Forces for realistic physics simulation
- Try both and feel the difference!

**"Why does my orbit decay/escape?"**
- Need perfect balance of velocity and gravity strength
- Real orbits are stable, but Unity's discrete simulation can drift
- This is actually realistic - satellites need correction thrusters!

**"What's the difference between drag and friction?"**
- Drag = air/fluid resistance (affects all movement)
- Friction = surface interaction (need PhysicMaterial, covered later)

**"Can I have multiple gravity sources?"**
- Yes! Loop through all sources and AddForce from each
- Creates complex gravitational fields
- Great for advanced challenges!

### Pacing Notes

- **Part 1-2 (25 min):** Theory-heavy, watch for glazed eyes
  - Break up with live demos
  - Show velocity arrows in Scene view
  - Let them experiment with values

- **Part 3 (10 min):** Abstract concepts
  - USE REAL-WORLD ANALOGIES
  - Shopping cart = force
  - Jumping = impulse
  - Ice = zero drag

- **Part 5 (15 min):** Most exciting part!
  - Orbital mechanics are fascinating
  - Let them "feel" for the right velocity
  - Encourage experimentation

### Extension Topics (If Time Allows)

1. **AddForce with different application points**
   - `rb.AddForceAtPosition()` for torque
   - Makes objects spin

2. **Angular velocity and torque**
   - `rb.angularVelocity` = rotation speed
   - `rb.AddTorque()` = apply rotation force

3. **Center of mass**
   - `rb.centerOfMass` affects rotation
   - Important for vehicles

4. **Physics materials**
   - Bounciness, friction
   - Create ice, rubber, etc.

### Session Reflection

After the session, answer these:

1. **Did the student understand velocity vs forces?**
2. **Were they able to create a stable orbit?**
3. **Did they grasp FixedUpdate vs Update?**
4. **Engagement level (1-10):**
5. **What to review next week:**
6. **Topics that need reinforcement:**

---

## Next Week Preview

**Week 4:** Character Controllers - We'll combine everything (input, physics, forces) to create a full 3D character controller with ground detection, jumping, and camera controls! 🎮🎮

We'll also explore:
- Raycasting for ground detection
- Camera following
- Turning to face movement direction
- Animations (if time allows)

---

## Parent Summary

**Today your student:**
- Learned Unity's physics system (Rigidbody)
- Understood velocity and forces
- Implemented movement with momentum
- Created custom gravity simulation
- Made objects orbit a planet (like real space physics!)

**Concepts practiced:**
- Physics simulation (Rigidbody)
- Velocity vs forces (different movement styles)
- ForceMode types (Force, Impulse)
- Custom gravity (inverse square law)
- Orbital mechanics

**Homework:** Create orbital systems and physics-based challenges (20-30 min)

**Next week:** Full 3D character controller with advanced movement!

---

## Additional Resources

### Understanding Physics Concepts

**Velocity:**
- Speed in a direction
- Measured in m/s (meters per second)
- Vector3: (x_speed, y_speed, z_speed)

**Force:**
- Push or pull
- Measured in Newtons (N)
- Force = mass × acceleration (F = ma)

**Gravity:**
- Always pulls toward mass
- Earth's gravity = 9.81 m/s²
- F = G × (m₁×m₂)/r² (Newton's law)

**Drag:**
- Resistance to movement
- Higher drag = stops faster
- 0 drag = moves forever (space!)

### Useful Unity Physics Settings

**Edit → Project Settings → Physics:**
- Gravity: (0, -9.81, 0) - Earth's gravity
- Default Material: Physics material for all objects
- Bounce Threshold: Min velocity for bouncing
- Sleep Threshold: When objects "fall asleep" (stop simulating)

**Rigidbody Settings:**
- Mass: Weight (kg)
- Drag: Air resistance
- Angular Drag: Rotation resistance
- Use Gravity: Enable/disable gravity
- Is Kinematic: Controlled by code, not physics
- Interpolate: Smooth movement between physics steps
- Collision Detection: Discrete (fast) vs Continuous (accurate)

### Debugging Physics

**Visualize forces:**
```csharp
Debug.DrawRay(transform.position, rb.velocity, Color.green);  // Velocity
Debug.DrawRay(transform.position, forceDirection * 2, Color.red);  // Force direction
```

**Log values:**
```csharp
Debug.Log($"Velocity: {rb.velocity}, Speed: {rb.velocity.magnitude}");
Debug.Log($"Distance to planet: {distance}, Force: {forceMagnitude}");
```

**Use PhysicsDebugger script:**
- Attach to any Rigidbody object
- Shows velocity, speed, and direction in real-time

---

**Remember: Physics is about experimentation! Try different values, break things on purpose, and learn by doing!** 🚀🔬
