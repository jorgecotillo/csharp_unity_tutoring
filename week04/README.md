# Week 4: Player Movement with Character Controller 🎮

**Today's Goal:** Create a player that can move with WASD and a camera that smoothly follows!  
**Time:** 1 hour  
**New Concepts:** Character Controller, Input System Actions, Camera Follow, Grounded Detection  
**C# Concepts:** Component architecture, Input Actions, Transform.forward/right, Normalized vectors  

---

## 📦 Before You Start: Installing Packages

This week uses Unity's **New Input System** package. If you haven't installed it yet:

### How to Add Packages in Unity

1. **Open the Package Manager**
   - Go to menu: Window → Package Manager
   - Or press: `Ctrl + P` (Windows) / `Cmd + P` (Mac)

2. **Find the package**
   - At the top left, make sure "Unity Registry" is selected (not "In Project")
   - Use the search bar to type: "Input System"
   - Click on "Input System" when it appears in the list

3. **Install it**
   - Click the "Install" button in the bottom right
   - Wait for Unity to download and install (takes a few seconds)

4. **Unity will ask to restart**
   - Click "Yes" to restart the editor
   - This is needed because the Input System changes how Unity handles input

**Result:** The Input System package is now installed and ready to use!

**Note:** You can use this same method to install other packages like:
- Cinemachine (for advanced cameras)
- ProBuilder (for level design)
- TextMeshPro (for better text)

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
- ❌ **Can be pushed by other objects** - enemies, explosions, or barrels can push you around
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

**Trade-offs (minor limitations that are actually benefits for player control!):**
- ⚠️ **Physics interactions are opt-in, not automatic** - You choose when to add push back effects in code (giving you precise control instead of random physics pushing you around)
- ⚠️ **Can't push objects automatically** - Walking into a crate won't push it UNLESS you add the code (prevents accidentally pushing things when you don't want to)
- ⚠️ **Instant response instead of realistic momentum** - Stops immediately when you release keys (feels "arcade-y" but this is exactly what makes shooters feel good!)

**Real Example - Why This Feels Better:**

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
- Add push back effects when YOU want them

```csharp
// Example: Getting hit by explosion
void OnExplosionHit(Vector3 explosionForce)
{
    // Apply push back by moving the character
    Vector3 pushBack = explosionForce;
    characterController.Move(pushBack * Time.deltaTime);
    
    // You control:
    // - How strong the push is
    // - How long it lasts
    // - Whether player can fight against it
}

// Example: Getting hit by enemy
private float staggerEndTime = 0f;
private bool isStaggered = false;

void OnEnemyHit(Vector3 hitDirection, float pushForce)
{
    // Apply push back
    Vector3 pushBack = hitDirection * pushForce;
    characterController.Move(pushBack * Time.deltaTime);
    
    // Start stagger - disable player control for 0.5 seconds
    isStaggered = true;
    staggerEndTime = Time.time + 0.5f;
}

void Update()
{
    // Check if stagger period is over
    if (isStaggered && Time.time >= staggerEndTime)
    {
        isStaggered = false;  // Regain control
    }
    
    // Only allow movement input if NOT staggered
    if (!isStaggered)
    {
        // Normal WASD movement code here...
        Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
        // ... rest of movement logic
    }
}
```

**The key difference:**

**With Rigidbody (automatic physics):**
```
❌ Explosion happens → You fly backward uncontrollably
❌ Enemy bumps you → You slide away helplessly  
❌ Barrel rolls into you → You get pushed around
❌ Player frustrated: "I can't control my character!"
```

**With Character Controller (controlled physics):**
```
✅ Explosion happens → You add push back in your code (you decide how much)
✅ Enemy bumps you → Nothing happens (or small push if you coded it)
✅ Barrel rolls into you → Nothing happens (you're not affected by random physics)
✅ Player happy: "I'm in control, but explosions still feel impactful!"
```

**Real-world example:**
- **Fortnite**: Character Controller + coded push back for explosions
- **Call of Duty**: Character Controller + coded camera shake when hit
- **Apex Legends**: Character Controller + coded push back for abilities

All these games have tight controls but ALSO have pushback effects - they just code them manually for precise control!

**The bottom line:** Character Controller gives you the best of both worlds - tight, precise controls for everyday movement, PLUS the ability to add physics effects (like being pushed back) whenever you want them. You're in control of when and how physics affects your player!

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

### 🎯 How to Add a Character Controller

**Step-by-step:**

1. **Select your GameObject** (e.g., your Player capsule)
   - Click on it in the Hierarchy window

2. **Open the Inspector** (usually on the right side of Unity)

3. **Add the component:**
   - Click the "Add Component" button at the bottom of the Inspector
   - Type "Character Controller" in the search box
   - Click on "Character Controller" when it appears

**Alternative method:**
- Select your GameObject
- Go to menu: Component → Physics → Character Controller

**Result:** You'll see the Character Controller component appear in the Inspector with all its properties!

---

### 🔧 Key Properties of Character Controller

When you add a Character Controller component, you'll see these settings:

#### **1. Center** (Vector3)
```
What: Center point of the capsule
Default: (0, 1, 0)
Why: Sets where the capsule is placed, starting from the GameObject's position
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
What: Small gap around the capsule edge
Default: 0.08
Why: Prevents character from getting stuck in walls (like a small safety space)
Gotcha: Should be at least 1% of radius. Don't set to 0!
```

**What happens if you set Skin Width to 0?**
- ❌ Character gets stuck in walls and can't move away
- ❌ Character shakes/vibrates when touching walls
- ❌ Sometimes falls through floors
- ❌ Collision detection fails randomly

**Why?** Computers need a tiny space to calculate "Am I touching the wall?" correctly. Without it, the math doesn't work well and causes bugs!

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

> **⚠️ Important:** `PlayerInputActions` is NOT a package you install! It's a file YOU create in Unity. See detailed instructions in [INPUT_ACTIONS_SETUP.md](INPUT_ACTIONS_SETUP.md) if you get stuck.

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

**Tip: Adding Multiple Keys for Same Direction**

Want both WASD and Arrow keys to work? You can add multiple bindings!

1. Right-click on any direction (e.g., "Up: W [Keyboard]")
2. Select "Duplicate"
3. Configure the duplicate to use a different key (e.g., "Up Arrow [Keyboard]")

Now both keys will work! Your code doesn't change - Unity handles multiple inputs automatically.

Common setup:
- Up: W + Up Arrow
- Down: S + Down Arrow
- Left: A + Left Arrow
- Right: D + Right Arrow

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

**Why use Awake() instead of Start()?**

Unity calls methods in this order:
1. `Awake()` - Called first when script loads
2. `OnEnable()` - Called when GameObject/script is enabled
3. `Start()` - Called before first frame

We create Input Actions in `Awake()` because:
- Input Actions must exist BEFORE `OnEnable()` tries to enable them
- If we used `Start()`, `OnEnable()` would run first and crash (inputActions would be null)
- Best practice: use `Awake()` to initialize things that THIS script owns

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
        // Create instance - must happen before OnEnable()!
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

**Why is Enable() so important?**

When you create Input Actions with `new PlayerInputActions()`, they are **disabled by default** - not listening for input yet!

- **Without Enable():** Input Actions always return (0,0) - no input detected! ❌
- **With Enable():** Input Actions detect your keyboard/gamepad input ✅

**Think of it like a microphone:**
- `new PlayerInputActions()` = Creating the microphone
- `.Enable()` = Turning it ON
- `.Disable()` = Turning it OFF

**Why this design?**
- Lets you temporarily disable input (e.g., during pause menus)
- Saves performance when not needed
- Prevents memory leaks with proper cleanup
- You control exactly when input is active

That's why you always pair `Enable()` in `OnEnable()` with `Disable()` in `OnDisable()`!

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
// Multiply by input values to scale direction: 0 = no movement, 1 = full speed, -1 = opposite direction
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

**Why don't we use `transform.up` for WASD movement?**

Because WASD controls **horizontal movement** (moving on the ground), not vertical movement (flying up/down):

- `transform.forward` and `transform.right` = Horizontal plane (ground movement) ✅
- `transform.up` = Vertical axis (flying/jumping) ❌

**Vertical movement is handled separately by gravity:**
```csharp
// Horizontal: WASD input
Vector3 moveDirection = transform.forward * input.y + transform.right * input.x;

// Vertical: Gravity (handled separately below)
movement.y = verticalVelocity * Time.deltaTime;  // ← Up/down movement
```

If we used `transform.up` for WASD, pressing W would make you fly upward instead of walking forward!

**When you WOULD use `transform.up`:** Flying games, swimming mechanics, or space games where you need full 3D movement.

---

### ⚡ Step 2: Normalizing Diagonal Movement

**First: What is Magnitude?**

**Magnitude** = the **length** of a vector. It's the straight-line distance from the origin (0,0,0) to the point the vector represents.

**How it's calculated:** Using the Pythagorean theorem in 3D:

```csharp
Vector3 v = new Vector3(3, 4, 0);
float magnitude = Mathf.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
// magnitude = √(3² + 4² + 0²) = √(9 + 16 + 0) = √25 = 5

// Or just use Unity's built-in property:
float magnitude = v.magnitude;  // Returns 5
```

**Visual example:**
```
      (3, 4, 0)
         •
        /|
       / |
    5 /  | 4
     /   |
    /    |
   •-----•
  (0,0) 3

The diagonal line = magnitude = 5
```

**Why `transform.forward` always has magnitude = 1:**

Unity **automatically normalizes** `transform.forward` and `transform.right` - they ALWAYS have magnitude = 1, no matter which direction your object is facing:

```csharp
// Facing north (0° rotation)
transform.forward = (0, 0, 1)
magnitude = √(0² + 0² + 1²) = 1 ✅

// Facing northeast (45° rotation)
transform.forward = (0.707, 0, 0.707)
magnitude = √(0.707² + 0² + 0.707²) = √(0.5 + 0.5) = 1 ✅
```

---

**Why do we need to normalize?**

When you press W+D simultaneously, you're telling the game:
- "Move forward at 100% speed" (transform.forward * 1)
- PLUS "Move right at 100% speed" (transform.right * 1)

This **adds two unit vectors together**, creating a longer vector!

**Important:** This has NOTHING to do with which direction the player is facing. Whether your player faces north, northeast, or any other direction, `transform.forward` and `transform.right` are ALWAYS normalized (magnitude = 1). 

The problem happens when you **combine your input** (pressing multiple keys):

**The Problem:**
```csharp
// Pressing W only (moving forward)
Vector3 move = transform.forward * 1;  // Magnitude = 1 ✅

// Pressing W + D (moving diagonally forward-right)
Vector3 move = transform.forward * 1 + transform.right * 1;
// You're ADDING two normalized vectors!
// (0,0,1) + (1,0,0) = (1,0,1)
// Magnitude = √(1² + 0² + 1²) = √2 = 1.414 ❌ 
// Player moves 41% faster diagonally!
```

**Why is this unfair?**
```
Player pressing W only:     Moves 1 unit per second
Player pressing W+D:        Moves 1.414 units per second (41% faster!)

Result: Diagonal movement = speed boost exploit!
```

**The Solution: Normalize!**
```csharp
Vector3 moveDirection = transform.forward * input.y + transform.right * input.x;

// Normalize only if there's input (avoid normalizing zero vector)
// magnitude = hypotenuse (straight-line length from origin to point: √(x² + y² + z²))
if (moveDirection.magnitude > 0.1f)
{
    moveDirection.Normalize();  // Makes magnitude = 1
}
```

**After normalizing:**
```
Player pressing W only:     Moves 1 unit per second ✅
Player pressing W+D:        Moves 1 unit per second ✅

Result: Fair! Same speed in all directions!
```

**What does Normalize() do?**
```csharp
Vector3 v = new Vector3(3, 4, 0);
Debug.Log(v.magnitude);  // 5

v.Normalize();  // Divides each component by magnitude
Debug.Log(v);            // (0.6, 0.8, 0)
Debug.Log(v.magnitude);  // 1 ✅

// The DIRECTION stays the same, but the LENGTH becomes 1
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
    // Line 1: Horizontal movement (WASD) - direction × speed × deltaTime
    Vector3 movement = moveDirection * currentSpeed * Time.deltaTime;
    // Line 2: Vertical movement (gravity) - overwrites Y component only
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

**💡 Note:** The actual PlayerController.cs script in Assets/Scripts organizes this code into separate methods (`HandleMovement()`, `HandleGravity()`, `ApplyMovement()`) for better readability. The logic is the same, just structured differently!

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

**📝 Important: Camera Offset Values Are CUSTOMIZABLE!**

The values `(0, 2, -5)` are just a suggested starting point - you can change them to anything! There's nothing special about these numbers. Try different values to get different camera angles.

**What each value controls:**
- **X** = Left(-) / Right(+)  →  `0` = centered behind player
- **Y** = Down(-) / Up(+)     →  `2` = camera is 2 units above player
- **Z** = Forward(+) / Back(-) →  `-5` = camera is 5 units behind player

**Visual representation (top-down view):**
```
       Player
         🚶
         |
         | (5 units)
         |
       📷 Camera (also 2 units higher)
```

**Try these alternatives:**
- `new Vector3(0, 2, -5)` → Default: behind and slightly above (third-person)
- `new Vector3(0, 10, -8)` → High and far back (strategy game view)
- `new Vector3(3, 2, -3)` → Over-the-shoulder (Resident Evil style)
- `new Vector3(0, 1, -2)` → Close follow (tight third-person)
- `new Vector3(0, 20, 0)` → Directly above (top-down view)

Experiment to find what feels best for YOUR game!

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

// Sweet spot (pick a value between 5 and 15)
smoothSpeed = 10f;  // Just right! Try values between 5-15
```

---

## Part 6: Building Your Final Project - Iteration 1 (30 minutes)

### 🎯 Final Project Context: NPC Shooter Foundation

This week, you're building **the player movement system** for your final NPC Shooter game! This is the foundation everything else will build on.

**What you're implementing:**
- ✅ Player movement (WASD)
- ✅ Sprint system (Shift)
- ✅ Gravity and ground detection
- ✅ Basic camera follow

**What comes next (future weeks):**
- Week 5: Mouse look & camera collision
- Week 6-7: Shooting mechanics & bullets
- Week 8-9: NPCs & AI
- Week 10-11: Health, damage, game loop

---

### 📋 Implementation Checklist

#### Step 1: Create Your Game Scene (5 min)

**In Unity Editor:**

1. **Create new scene** (File → New Scene → Basic)
   - Save as "GameScene" in Assets/Scenes/

2. **Create Player GameObject**
   - GameObject → 3D Object → Capsule
   - Rename to "Player"
   - Position: (0, 1, 0)
   - Add component: Character Controller
   - Configure Character Controller:
     - Height: 2
     - Radius: 0.5
     - Center: (0, 1, 0)
     - Skin Width: 0.08

3. **Create Ground Plane**
   - GameObject → 3D Object → Plane
   - Rename to "Ground"
   - Scale: (10, 1, 10) - makes 100x100 unit ground
   - Position: (0, 0, 0)

4. **Create Test Environment** (for next weeks)
   - GameObject → 3D Object → Cube (rename "TestWall")
   - Position: (0, 1, 10)
   - Scale: (10, 2, 1) - makes a wall
   - This will help test camera collision next week!

---

#### Step 2: Setup Input System (5 min)

**Create Input Actions Asset:**

1. Right-click in Assets folder → Create → Input Actions
2. Name it "PlayerInputActions"
3. Double-click to open Input Actions window
4. Create Action Map: "Player"
5. Add Actions:
   - **Move**: Action Type = Value, Control Type = Vector2
     - Add 2D Vector Composite (WASD)
   - **Sprint**: Action Type = Button
     - Binding = Left Shift
6. Click "Save Asset"
7. Check "Generate C# Class" in Inspector
8. Click "Apply"

---

#### Step 3: Implement Player Controller (10 min)

**Create Scripts folder:**
- Right-click Assets → Create → Folder → Name it "Scripts"

**Create PlayerController.cs:**
- Right-click Scripts folder → Create → C# Script
- Name: "PlayerController"
- Double-click to open in your code editor
- Copy the complete movement logic from Step 5 above (or reference week04/Assets/Scripts/PlayerController.cs)

**Key systems to implement:**
```csharp
// 1. Input reading
Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
bool isSprinting = inputActions.Player.Sprint.IsPressed();

// 2. Movement direction calculation
Vector3 moveDirection = transform.forward * input.y + transform.right * input.x;
if (moveDirection.magnitude > 0.1f) {
    moveDirection.Normalize();
}

// 3. Speed selection
float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

// 4. Gravity system
if (characterController.isGrounded) {
    verticalVelocity = -2f;
} else {
    verticalVelocity += Physics.gravity.y * Time.deltaTime;
}

// 5. Apply movement
Vector3 movement = moveDirection * currentSpeed * Time.deltaTime;
movement.y = verticalVelocity * Time.deltaTime;
characterController.Move(movement);
```

---

#### Step 4: Setup Camera System (5 min)

**Create CameraFollow.cs:**
- In Scripts folder → Create → C# Script
- Name: "CameraFollow"
- Implement basic follow logic (see Part 5 above)

**Configure Camera:**
1. Select Main Camera in Hierarchy
2. Add component: CameraFollow script
3. Drag Player GameObject to "Target" field
4. Set Offset: (0, 2, -5)
5. Set Smooth Speed: 10

---

#### Step 5: Attach Scripts to Player (2 min)

**Select Player GameObject:**
1. Add component: PlayerController
2. Drag PlayerInputActions asset to "Input Actions" field
3. Set Walk Speed: 3
4. Set Sprint Speed: 6

---

#### Step 6: Test Your Implementation (3 min)

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
