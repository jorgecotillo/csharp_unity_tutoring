# Week 4: Player Movement with Character Controller 🎮

**Today's Goal:** Create a player that can move with WASD and a camera that smoothly follows!  
**Time:** 1 hour  
**New Concepts:** Character Controller, Input System Actions, Camera Follow, Grounded Detection  
**C# Concepts:** Component architecture, Input Actions, Transform.forward/right, Normalized vectors  

---

## 🎯 What You'll Build Today

A player character that:
- Moves with WASD keys (forward, back, left, right)
- Can walk AND run (hold Shift to sprint!)
- Has gravity and stays on the ground
- Has a camera that smoothly follows from behind

**Success = Moving around smoothly with a camera following you!** ✨

---

## 📅 Session Structure (60 minutes)

### Part 1: Character Controller vs Rigidbody - Which to Use? (10 minutes)
### Part 2: Setting Up Character Controller & Input (15 minutes)
### Part 3: Building the Movement System (15 minutes)
### Part 4: BREAK - Stand up and move! (5 minutes)
### Part 5: Camera Follow System (10 minutes)
### Part 6: Testing & Tweaking (5 minutes)

---

## Part 1: Character Controller vs Rigidbody - The Big Decision (10 minutes)

### 🤔 Why Do We Need to Choose?

In Week 3, you learned about **Rigidbody** - Unity's physics component. It's great for realistic physics!

But for **player characters**, we often use something different: **Character Controller**

Let's understand WHY:

---

### ⚖️ Rigidbody Movement (What You Learned Week 3)

**How it works:**
```csharp
// Week 3 style - Rigidbody physics
rb.velocity = moveDirection * speed;
rb.AddForce(moveDirection * force);
```

**Important:** Rigidbody DOES have physics - you ARE affected by physics forces! That's exactly the point.

**Pros:**
- ✅ Realistic physics (momentum, forces, collisions)
- ✅ Great for objects that should feel "heavy" or "physical"
- ✅ Works with all physics interactions (explosions, forces, etc.)
- ✅ Perfect for: Cars, balls, physics puzzles, ragdolls

**Cons (for player characters):**
- ❌ **Feels "slippery" or "floaty"** - momentum makes you slide when you stop
- ❌ **Harder to control precisely** - need to fight against momentum for tight movements
- ❌ **Can be pushed by other objects** - enemies, explosions, or barrels can shove you around
- ❌ **More complex to tune** - requires lots of drag/friction tweaking to feel responsive

**Real Example - Why This is Frustrating:**
```
You're aiming at an enemy in a shooter:
1. Press D to step sideways to the right
2. Your character accelerates right (momentum building up)
3. Release D to stop
4. Character keeps sliding right for a moment! (momentum!)
5. You miss your shot because you slid past where you wanted to be
6. You get shot by the enemy 😵

Player thinks: "WHY WON'T YOU JUST STOP?!"
```

**When to use:** Racing game car, rolling boulder, anything that SHOULD slide/bounce/feel heavy

---

### 🎮 Character Controller (What We're Learning Today)

**How it works:**
```csharp
// Character Controller style
characterController.Move(moveDirection * speed * Time.deltaTime);
```

**Important:** Character Controller is NOT "no physics" - it's **CONTROLLED physics**! 

You still have:
- ✅ Gravity (we add it manually)
- ✅ Collision (built-in, automatic)
- ✅ Ground detection (built-in)
- ✅ Slope handling (automatic)

But you DON'T have:
- ❌ Momentum/sliding (instant stop when you release keys)
- ❌ Being pushed around by physics objects
- ❌ Needing to fight forces to move precisely

**Pros (for player characters):**
- ✅ **Instant response** - press W, you move forward immediately
- ✅ **Instant stop** - release W, you stop immediately (no sliding!)
- ✅ **YOU control when physics affects you** - won't be randomly pushed around, but you CAN add pushback effects when you want them
- ✅ **Precise control** - perfect for aiming in shooters
- ✅ Built-in ground detection and slope handling
- ✅ **Feels "tight"** - like professional games

**Cons:**
- ❌ Not affected by physics forces automatically (but you CAN add them manually if needed!)
- ❌ Can't push Rigidbody objects directly (but can be added via code)
- ❌ Less "realistic" physics (but more fun to play!)

**Real Example - Why This Feels Better:**
```
You're aiming at an enemy in a shooter:
1. Press D to step sideways to the right
2. Character moves right INSTANTLY (no acceleration)
3. Release D to stop
4. Character stops INSTANTLY (no sliding!)
5. You stay exactly where you want to be
6. You shoot the enemy perfectly 🎯

Player thinks: "This feels great! I'm in control!"
2. Player shouldn't be pushed around by explosions or enemies (frustrating!)
3. We need precise movement for combat (stop = STOP, not slide)
4. It's easier to make feel "good" and professional

**Think about these popular shooters:**
- Fortnite - Character Controller
- Call of Duty - Character Controller  
- Valorant - Character Controller
- Apex Legends - Character Controller

They all use instant-stop movement because it feels better for competitive gameplay!

**Note:** Our BULLETS will still use Rigidbody! (We'll do that in Week 6-7)

---

### 🤔 "But wait, I want SOME physics on my player!"

**Great question!** No, you DON'T need to switch to Rigidbody just to add pushback effects!

**Character Controller gives you the BEST of both worlds:**
- Keep tight, instant-stop controls for normal movement
- Add physics pushback effects when YOU want them

```csharp
// Example: Getting hit by explosion
void OnExplosionHit(Vector3 explosionForce)
{
    // Apply knockback by moving the character
    Vector3 knockback = explosionForce;
    characterController.Move(knockback * Time.deltaTime);
    
    // You control:
    // - How strong the push is
    // - How long it lasts
    // - Whether player can fight against it
}

// Example: Getting hit by enemy
void OnEnemyHit(Vector3 hitDirection, float pushForce)
{
    // Small push backward
    Vector3 pushback = hitDirection * pushForce;
    characterController.Move(pushback * Time.deltaTime);
    
    // Player staggers for 0.5 seconds then regains control
}
```

**The key difference:**

**With Rigidbody (automatic physics):**
```
❌ Explosion happens → You fly backward uncontrollably
❌ Enemy bumps you → You slide away helplessly  
❌ Barrel rolls into you → You get shoved around
❌ Player frustrated: "I can't control my character!"
```

**With Character Controller (controlled physics):**
```
✅ Explosion happens → You add knockback in your code (you decide how much)
✅ Enemy bumps you → Nothing happens (or small push if you coded it)
✅ Barrel rolls into you → Nothing happens (you're not affected by random physics)
✅ Player happy: "I'm in control, but explosions still feel impactful!"
```

**Real-world example:**
- **Fortnite**: Character Controller + coded pushback for explosions
- **Call of Duty**: Character Controller + coded flinch when hit
- **Apex Legends**: Character Controller + coded knockback for abilities

All these games have tight controls but ALSO have pushback effects - they just code them manually for precise control!

**So to answer your question:** Keep Character Controller and add physics effects in code when you want them. You get precise control AND cool physics effects!

### 🎯 The Rule of Thumb

**Use Character Controller when:**
- Making a player-controlled character
- Want tight, responsive controls
- Don't need realistic physics interactions
- Making a shooter, platformer, or action game

**Use Rigidbody when:**
- Object should feel physical/heavy
- Need realistic momentum
- Want to interact with physics world
- Making vehicles, projectiles, or physics-based gameplay

---

### 🎮 For Our Shooter Game?

We're using **Character Controller** because:
1. We want **tight, responsive controls** for aiming and shooting
2. Player shouldn't be pushed around by explosions or enemies
3. We need precise movement for combat
4. It's easier to make feel "good"

**Note:** Our BULLETS will still use Rigidbody! (We'll do that in Week 6-7)

---

## Part 2: Understanding Character Controller Component (15 minutes)

### 📦 What is a Character Controller?

A **Character Controller** is a Unity component that:
- Handles movement and collision
- Keeps the character on the ground
- Prevents walking through walls
- Handles slopes and steps automatically

Think of it as a **"smart capsule"** that knows how to move around a level!

---

### 🔧 Key Properties of Character Controller

When you add a Character Controller component, you'll see these settings:

#### **1. Center** (Vector3)
```
What: Center point of the capsule
Default: (0, 1, 0)
Why: Positions the capsule collider relative to GameObject
```

#### **2. Radius** (float)
```
What: How wide the capsule is
Default: 0.5
Why: Determines how much space character takes up
Gotcha: Too small = can fall through gaps, Too large = can't fit through doors
```

#### **3. Height** (float)
```
What: How tall the capsule is
Default: 2
Why: Determines character height
Gotcha: Should match your character model height!
```

#### **4. Slope Limit** (float)
```
What: Maximum slope angle character can walk up (degrees)
Default: 45°
Why: Prevents walking up walls
Example: 45° = moderate hill, 60° = steep hill, 90° = wall
```

#### **5. Step Offset** (float)
```
What: Maximum step height character can climb automatically
Default: 0.3
Why: Lets character walk up stairs smoothly
Gotcha: Too high = can climb things that look too tall, Too low = stuck on small bumps
```

#### **6. Skin Width** (float)
```
What: Small buffer around the collider
Default: 0.08
Why: Prevents character from getting stuck in walls (numerical precision buffer)
Gotcha: Should be at least 1% of radius. Don't set to 0!
```

#### **7. Min Move Distance** (float)
```
What: Minimum distance to move before actually moving
Default: 0.001
Why: Performance optimization
Gotcha: Rarely need to change this
```

---

### 🎓 Understanding the Capsule Shape

Character Controller uses a **capsule** shape (cylinder with rounded ends):

```
     ___
    /   \     <- Top hemisphere (Radius)
   |     |    <- Cylinder (Height - 2*Radius)
    \___/     <- Bottom hemisphere (Radius)
    
   <----->    Radius
   
   Total Height = Height property
```

**Why a capsule?**
- Rounded ends = smooth collision (won't get stuck on edges)
- Cylinder = represents human body shape well
- Efficient for collision detection

---

### ⚠️ Important Gotchas with Character Controller

#### **Gotcha #1: It's NOT a Rigidbody**
```csharp
// ❌ DON'T DO THIS - Won't work!
GetComponent<Rigidbody>().velocity = moveDirection * speed;

// ✅ DO THIS - Character Controller way
characterController.Move(moveDirection * speed * Time.deltaTime);
```

#### **Gotcha #2: You Must Handle Gravity Yourself**
Unlike Rigidbody (which has built-in gravity), Character Controller requires YOU to apply gravity:

```csharp
// You need to do this yourself!
if (!characterController.isGrounded)
{
    verticalVelocity += Physics.gravity.y * Time.deltaTime;
}
```

#### **Gotcha #3: Use Move() Not transform.position**
```csharp
// ❌ BAD - Breaks collision detection
transform.position += moveDirection * speed * Time.deltaTime;

// ✅ GOOD - Uses proper collision
characterController.Move(moveDirection * speed * Time.deltaTime);
```

#### **Gotcha #4: Movement is Relative to Current Position**
```csharp
// Move() moves FROM current position
// If you want to move 1 meter forward:
Vector3 movement = transform.forward * 1f * Time.deltaTime;
characterController.Move(movement);
// This moves forward relative to current rotation!
```

---

## Part 3: Setting Up Input Actions (15 minutes)

### 🎮 Input System Refresher (Week 2 Callback)

In Week 2, you learned Unity's **New Input System** with keyboard:

```csharp
// Week 2 style - checking key directly
if (Keyboard.current.spaceKey.isPressed)
{
    // Do something
}
```

This works, but there's a **better way** for complex games: **Input Actions**

---

### 🎯 What are Input Actions?

**Input Actions** = Pre-configured input mappings that work across devices

Think of it like this:
- **Old way:** "Check if W key is pressed"
- **New way:** "Check if Move Forward action is active" (could be W, gamepad stick, or arrow key)

**Benefits:**
1. Works with keyboard, mouse, gamepad automatically
2. Can rebind keys easily
3. More organized for big projects
4. Better performance

---

### 📝 Creating an Input Actions Asset

**Step 1: Create the Asset**
1. In Project window, right-click in Assets folder
2. Create → Input Actions
3. Name it "PlayerInputActions"
4. Double-click to open Input Actions editor

**Step 2: Create Action Map**
An **Action Map** is a group of related actions (like "Player", "UI", "Vehicle")

1. Click the + next to "Action Maps"
2. Name it "Player"

**Step 3: Create Actions**
Create these actions in the "Player" action map:

**Action: "Move"**
- Click + next to "Actions"
- Name: "Move"
- Action Type: "Value"
- Control Type: "Vector2"
- Click + next to "Move" to add binding
- Select "2D Vector Composite" (lets us use WASD as a single input)
- Configure composite:
  - Up: W
  - Down: S
  - Left: A
  - Right: D

**Action: "Sprint"**
- Click + next to "Actions"
- Name: "Sprint"
- Action Type: "Button"
- Add binding: Left Shift

**Step 4: Save and Generate C# Class**
1. Click "Save Asset" at top
2. Check "Generate C# Class" in Inspector
3. Click "Apply"
4. Unity will create a C# file with your input actions

---

### 🎓 Understanding Vector2 for Movement

**What is Vector2?**
A Vector2 stores two numbers: X and Y

```csharp
Vector2 input = new Vector2(1, 0);
//                          ↑  ↑
//                          X  Y
```

**For movement input:**
- **X axis** = Left/Right (A/D keys)
  - -1 = Left (A)
  - 0 = No input
  - +1 = Right (D)

- **Y axis** = Forward/Back (W/S keys)
  - -1 = Back (S)
  - 0 = No input
  - +1 = Forward (W)

**Examples:**
```csharp
// Pressing W
Vector2(0, 1)   // Moving forward

// Pressing D
Vector2(1, 0)   // Moving right

// Pressing W + D (diagonal)
Vector2(1, 1)   // Moving forward-right
// Note: This has magnitude 1.414, not 1! Need to normalize!

// Pressing S + A
Vector2(-1, -1) // Moving back-left
```

---

### 🎮 Using Input Actions in Code

**Step 1: Create reference**
```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // Reference to our input actions
    private PlayerInputActions inputActions;
    
    void Awake()
    {
        // Create instance
        inputActions = new PlayerInputActions();
    }
    
    void OnEnable()
    {
        // Enable the Player action map
        inputActions.Player.Enable();
    }
    
    void OnDisable()
    {
        // Disable when script is disabled (good practice)
        inputActions.Player.Disable();
    }
}
```

**Step 2: Read input values**
```csharp
void Update()
{
    // Read Move action (returns Vector2)
    Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();
    
    // Check Sprint action (returns bool)
    bool isSprinting = inputActions.Player.Sprint.IsPressed();
    
    Debug.Log($"Move: {moveInput}, Sprint: {isSprinting}");
}
```

---

### ⚠️ Important: Input System Gotchas

**Gotcha #1: Must Enable Actions**
```csharp
// ❌ Won't work - forgot to enable!
inputActions = new PlayerInputActions();
Vector2 input = inputActions.Player.Move.ReadValue<Vector2>(); // Returns (0,0)

// ✅ Must enable first
inputActions = new PlayerInputActions();
inputActions.Player.Enable();
Vector2 input = inputActions.Player.Move.ReadValue<Vector2>(); // Works!
```

**Gotcha #2: Disable in OnDisable()**
```csharp
// Always pair Enable/Disable
void OnEnable() => inputActions.Player.Enable();
void OnDisable() => inputActions.Player.Disable();
// Prevents memory leaks!
```

**Gotcha #3: Input is in Update(), Movement is... Complicated**
```csharp
// ⚠️ Character Controller can be used in Update()
// Unlike Rigidbody which MUST use FixedUpdate()
void Update()
{
    Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
    // Process movement here - this is OK for Character Controller!
}
```

---

## Part 4: Building the Movement System (15 minutes)

### 🎯 The Movement Formula

For Character Controller, we need to:
1. Read input (WASD)
2. Convert 2D input to 3D movement direction
3. Apply speed
4. Apply gravity
5. Move using `characterController.Move()`

Let's break it down step by step!

---

### 📐 Step 1: Converting Input to World Direction

**The Problem:**
- Input gives us Vector2 (X, Y)
- Movement needs Vector3 (X, Y, Z)
- Input is "screen-relative" but we need "world-relative"

**What does "relative to camera" mean?**
When you press W, you want to move in the direction the camera is facing, not "north" in the world.

```
Camera looking NORTH:     Camera looking EAST:
W = move north            W = move east
D = move east             D = move south
```

**The Solution:**
Use `transform.forward` and `transform.right`!

```csharp
// Get input
Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();

// Convert to 3D direction (relative to player's forward)
Vector3 moveDirection = transform.forward * input.y +  // Forward/Back
                        transform.right * input.x;      // Left/Right

// input.y = W/S keys (forward/back)
// input.x = A/D keys (left/right)
```

**Why this works:**
- `transform.forward` = Vector3 pointing where object faces (normalized)
- `transform.right` = Vector3 pointing to object's right side (normalized)
- Multiplying by input values scales these directions

---

### 🎓 Understanding Transform.forward and Transform.right

Every Transform has 3 directional vectors:

```csharp
transform.forward  // Blue arrow in Unity (Z axis)
transform.right    // Red arrow in Unity (X axis)
transform.up       // Green arrow in Unity (Y axis)
```

These are **normalized** (length = 1) and point in **world space**.

**Example:**
```csharp
// If player is rotated 45° to the right:
transform.forward = new Vector3(0.707, 0, 0.707)  // Northeast
transform.right   = new Vector3(0.707, 0, -0.707) // Southeast
transform.up      = new Vector3(0, 1, 0)          // Always up (unless rotated)
```

---

### ⚡ Step 2: Normalizing Diagonal Movement

**The Problem:**
```csharp
// Pressing W only
Vector3 move = transform.forward * 1;  // Magnitude = 1 ✅

// Pressing W + D (diagonal)
Vector3 move = transform.forward * 1 + transform.right * 1;
// Magnitude = 1.414 (√2) ❌ 
// Player moves 41% faster diagonally!
```

**The Solution: Normalize!**
```csharp
Vector3 moveDirection = transform.forward * input.y + transform.right * input.x;

// Normalize only if there's input (avoid normalizing zero vector)
if (moveDirection.magnitude > 0.1f)
{
    moveDirection.Normalize();  // Makes magnitude = 1
}
```

**What does Normalize() do?**
```csharp
Vector3 v = new Vector3(3, 4, 0);
Debug.Log(v.magnitude);  // 5

v.Normalize();  // Divides by magnitude
Debug.Log(v);            // (0.6, 0.8, 0)
Debug.Log(v.magnitude);  // 1 ✅
```

---

### 🏃 Step 3: Walk vs Sprint

Simple: just multiply by different speeds!

```csharp
[Header("Movement Settings")]
public float walkSpeed = 3f;
public float sprintSpeed = 6f;

void Update()
{
    // Get input
    Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
    bool isSprinting = inputActions.Player.Sprint.IsPressed();
    
    // Choose speed
    float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
    
    // Calculate movement
    Vector3 moveDirection = transform.forward * input.y + transform.right * input.x;
    if (moveDirection.magnitude > 0.1f)
    {
        moveDirection.Normalize();
    }
    
    // Apply speed
    Vector3 movement = moveDirection * currentSpeed * Time.deltaTime;
}
```

---

### 🌍 Step 4: Applying Gravity

**The Problem:**
Character Controller doesn't have built-in gravity. We must do it ourselves!

**The Solution:**
Track vertical velocity separately and apply it each frame.

```csharp
private float verticalVelocity = 0f;

void Update()
{
    // Check if on ground
    if (characterController.isGrounded)
    {
        // Reset falling velocity when grounded
        verticalVelocity = -2f;  // Small downward force keeps us "stuck" to ground
    }
    else
    {
        // Apply gravity (accelerate downward)
        verticalVelocity += Physics.gravity.y * Time.deltaTime;
        // Physics.gravity.y = -9.81 (Earth gravity)
    }
    
    // Add vertical velocity to movement
    Vector3 movement = moveDirection * currentSpeed * Time.deltaTime;
    movement.y = verticalVelocity * Time.deltaTime;
    
    characterController.Move(movement);
}
```

**Why -2f when grounded?**
- 0f would make character "float" slightly above ground
- Small negative value ensures `isGrounded` stays true
- Pushes character down into ground slightly (Character Controller handles this correctly)

---

### 🎮 Step 5: Putting It All Together

Here's the complete movement logic:

```csharp
void Update()
{
    // 1. Get input
    Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
    bool isSprinting = inputActions.Player.Sprint.IsPressed();
    
    // 2. Calculate horizontal movement direction
    Vector3 moveDirection = transform.forward * input.y + transform.right * input.x;
    
    // 3. Normalize diagonal movement
    if (moveDirection.magnitude > 0.1f)
    {
        moveDirection.Normalize();
    }
    
    // 4. Choose speed
    float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
    
    // 5. Handle gravity
    if (characterController.isGrounded)
    {
        verticalVelocity = -2f;
    }
    else
    {
        verticalVelocity += Physics.gravity.y * Time.deltaTime;
    }
    
    // 6. Combine horizontal and vertical movement
    Vector3 movement = moveDirection * currentSpeed * Time.deltaTime;
    movement.y = verticalVelocity * Time.deltaTime;
    
    // 7. Apply movement
    characterController.Move(movement);
}
```

---

## Part 5: Camera Follow System (10 minutes)

### 📷 What is a Camera Follow System?

A camera that:
- Follows the player from behind
- Maintains a fixed distance
- Smoothly catches up (not instant)
- Looks at the player

---

### 🎯 The Basic Camera Follow Formula

```csharp
public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;  // The player
    
    [Header("Camera Settings")]
    public Vector3 offset = new Vector3(0, 2, -5);  // Position offset from player
    public float smoothSpeed = 10f;  // How fast camera catches up
    
    void LateUpdate()
    {
        if (target == null) return;
        
        // Calculate desired position
        Vector3 desiredPosition = target.position + offset;
        
        // Smoothly interpolate to desired position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        
        // Apply position
        transform.position = smoothedPosition;
        
        // Look at player
        transform.LookAt(target);
    }
}
```

---

### 🎓 Understanding LateUpdate()

Unity has 3 main update functions:

```csharp
void FixedUpdate()  // For physics (Rigidbody), runs at fixed intervals
void Update()       // For game logic, runs every frame
void LateUpdate()   // Runs AFTER all Update() calls, perfect for cameras!
```

**Why LateUpdate() for cameras?**
1. Player moves in `Update()`
2. ALL objects update
3. Camera updates in `LateUpdate()` - sees player's final position
4. No jittering or lag!

**Bad:**
```csharp
// Camera Update() runs before Player Update()
// Camera sees player's OLD position → jitter!
```

**Good:**
```csharp
// Camera LateUpdate() runs after Player Update()
// Camera sees player's NEW position → smooth!
```

---

### 🎓 Understanding Vector3.Lerp()

**Lerp** = Linear Interpolation = "Blend between two values"

```csharp
Vector3 result = Vector3.Lerp(start, end, t);
// t = 0 → result = start
// t = 0.5 → result = halfway between
// t = 1 → result = end
```

**For smooth camera:**
```csharp
Vector3 smoothedPosition = Vector3.Lerp(
    transform.position,     // Current position
    desiredPosition,        // Where we want to be
    smoothSpeed * Time.deltaTime  // How much to move (0-1)
);
```

**Why `smoothSpeed * Time.deltaTime`?**
- Creates exponential smoothing (fast at first, slow at end)
- `Time.deltaTime` keeps it frame-rate independent
- Higher `smoothSpeed` = faster catch-up

**Example values:**
- `smoothSpeed = 1` → Very slow, laggy feel
- `smoothSpeed = 5` → Moderate smoothness
- `smoothSpeed = 10` → Quick follow, slight smoothness
- `smoothSpeed = 100` → Almost instant (no smoothing)

---

### 🎓 Understanding Offset

The `offset` determines camera position relative to player:

```csharp
Vector3 offset = new Vector3(X, Y, Z);
//                           ↑  ↑  ↑
//                           |  |  Forward/Back
//                           |  Up/Down
//                           Left/Right
```

**Examples:**
```csharp
// Behind player, slightly up
offset = new Vector3(0, 2, -5);

// Above player looking down
offset = new Vector3(0, 10, 0);

// To the right of player
offset = new Vector3(3, 1, -3);
```

**Important:** Offset is in **local space** (relative to player's rotation):
```csharp
Vector3 desiredPosition = target.position + target.rotation * offset;
// This makes offset rotate with player!
```

But for a simple follow cam, we often use **world space offset**:
```csharp
Vector3 desiredPosition = target.position + offset;
// Camera stays at fixed offset (doesn't rotate with player)
```

---

### ⚠️ Camera Follow Gotchas

**Gotcha #1: Camera going through walls**
We'll fix this next week (Week 5) with collision detection!

**Gotcha #2: Target not assigned**
```csharp
// Always check!
if (target == null) 
{
    Debug.LogError("Camera target not assigned!");
    return;
}
```

**Gotcha #3: Camera too smooth = feels laggy**
```csharp
// Too slow
smoothSpeed = 1f;  // Feels like camera is drunk

// Too fast
smoothSpeed = 1000f;  // No smoothing at all, jittery

// Sweet spot
smoothSpeed = 5-15f;  // Just right!
```

---

## Part 6: Putting It All Together (5 minutes)

### 📋 Setup Checklist

**In Unity Editor:**

1. **Create Player GameObject**
   - GameObject → 3D Object → Capsule (rename to "Player")
   - Scale: (1, 1, 1)
   - Add component: Character Controller
   - Adjust Character Controller:
     - Height: 2
     - Radius: 0.5
     - Center: (0, 1, 0)

2. **Create Ground**
   - GameObject → 3D Object → Plane (rename to "Ground")
   - Scale: (10, 1, 10) for big ground
   - Position: (0, 0, 0)

3. **Setup Camera**
   - Find Main Camera
   - Position: (0, 5, -10) to start
   - Add script: CameraFollow
   - Assign Player as target

4. **Add Scripts to Player**
   - Add script: PlayerController
   - Assign Input Actions asset

---

## 🎮 Testing Your Movement

**Things to test:**
1. Press W/A/S/D - player should move
2. Press Shift while moving - should sprint faster
3. Walk off edge - should fall with gravity
4. Camera should follow smoothly
5. Move in circles - camera should orbit around

**Common issues:**
- Player not moving? Check Input Actions are enabled
- Camera not following? Check target is assigned
- Falling through ground? Check Ground has collider
- Moving in wrong direction? Check transform.forward

---

## 💡 Next Week Preview

In Week 5, we'll add:
- Mouse look (rotate camera with mouse!)
- Camera collision (no more going through walls!)
- Polish and fine-tuning
- Start integrating into final project

**📁 Next Steps:** Open `week05/README.md` to continue!

---

## 🎯 Today's Achievements

By the end of today, you have:
- ✅ Understood Character Controller vs Rigidbody
- ✅ Learned Input Actions (better than checking keys directly)
- ✅ Implemented WASD movement with sprint
- ✅ Applied gravity to Character Controller
- ✅ Created smooth camera follow
- ✅ Understood Vector3 directions and Lerp

**You now have the foundation for ANY third-person game!** 🎊

---

## 📝 Homework (Optional)

1. Change the walk and sprint speeds - what feels good?
2. Modify the camera offset - try different angles
3. Adjust camera smooth speed - find your preference
4. Add a jump! (Hint: Set verticalVelocity to positive value when pressing Space)

**Next session, we'll add mouse look and make this feel like a real game!** 🚀
