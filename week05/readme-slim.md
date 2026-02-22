# Weeks 5-7: Completing Player Movement & Camera System 🎮

⚠️ **IMPORTANT:** This continues directly from Week 4. You should have:
- ✅ Week04 Unity project open
- ✅ Player GameObject with Character Controller component
- ✅ Ground Plane created
- ✅ Basic scene set up

If you're missing any of these, go back to Week 4 README, Part 6, Step 1 first!

---

## 🎯 What You'll Build Over 3 Weeks

**Week 5 (This Week - 1 hour):**
- Setup Input Actions asset (the file that defines your controls)
- Create PlayerController.cs script (WASD movement + sprint)
- Create CameraFollow.cs script (camera follows player)
- Test everything works!

**Week 6 (Next Week - 1 hour):**
- Understanding Rotations & Quaternions (math for 3D rotation)
- Adding Mouse Look to camera

**Week 7 (Week After - 1 hour):**
- Camera Collision Detection (camera doesn't go through walls)
- Polish and fine-tuning

---

# Week 5: Input System & Player Movement

## 📅 Week 5 Structure (60 minutes)

| Time | Topic |
|------|-------|
| 0-5 min | Review: Character Controller Recap |
| 5-15 min | **Deep Dive: Unity's Input System** |
| 15-25 min | **Deep Dive: Vector Math for Movement** |
| 25-35 min | **Deep Dive: Understanding the PlayerController Script** |
| 35-45 min | **Deep Dive: Camera Follow & LateUpdate** |
| 45-55 min | **Demo: Setting Up Your Project** |
| 55-60 min | Testing & Troubleshooting |

---

## Part 1: Character Controller Recap (5 minutes)

### 🎮 What is Character Controller?

Remember from Week 4: **Character Controller** is a Unity component that:
- Handles player movement and collision
- Keeps the player on the ground
- Prevents walking through walls
- Handles slopes and steps automatically

**Key difference from Rigidbody:**
- **Rigidbody** = Realistic physics (momentum, sliding, being pushed around)
- **Character Controller** = Tight, responsive controls (instant stop, you're in control)

**We use Character Controller because:**
- Player stops instantly when you release keys (no sliding!)
- Player won't be randomly pushed by physics objects
- Movement feels "tight" like professional shooter games

---

## Part 2: Deep Dive - Unity's Input System (10 minutes)

### 🎮 Old Input vs New Input System

Unity has TWO ways to handle input:

#### **Old Input System (Legacy)**
```csharp
// Checking keys directly - simple but limited
if (Input.GetKey(KeyCode.W))
{
    // Move forward
}

float horizontal = Input.GetAxis("Horizontal");  // Returns -1 to 1
float vertical = Input.GetAxis("Vertical");      // Returns -1 to 1
```

**Problems with Old System:**
- ❌ Hard to support multiple devices (keyboard + gamepad)
- ❌ Key rebinding requires custom code
- ❌ All input checks happen every frame (wasteful)
- ❌ Settings scattered across Edit → Project Settings → Input Manager

#### **New Input System (What We Use)**
```csharp
// Using Input Actions - flexible and powerful
Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();
bool isSprinting = inputActions.Player.Sprint.IsPressed();
```

**Benefits of New System:**
- ✅ One action works with keyboard, mouse, AND gamepad
- ✅ Built-in rebinding support
- ✅ Event-driven (more efficient)
- ✅ Visual editor for configuration
- ✅ Better for complex games

---

### 🎯 Understanding Input Actions Architecture

Think of Input Actions like a restaurant menu:

```
📋 PlayerInputActions (The Menu)
│
├── 🗂️ Player (Action Map = Category like "Appetizers")
│   ├── 🎮 Move (Action = Menu Item)
│   │   └── ⌨️ WASD Composite (Binding = How to order it)
│   ├── 🎮 Sprint (Action)
│   │   └── ⌨️ Left Shift (Binding)
│   └── 🎮 Look (Action)
│       └── 🖱️ Mouse Delta (Binding)
│
└── 🗂️ UI (Action Map = Another Category)
    ├── 🎮 Navigate
    ├── 🎮 Submit
    └── 🎮 Cancel
```

**Key Concepts:**

| Term | What It Is | Example |
|------|-----------|---------|
| **Input Actions Asset** | The file that holds all your controls | `PlayerInputActions` |
| **Action Map** | A group of related actions | "Player", "UI", "Vehicle" |
| **Action** | A single input concept | "Move", "Jump", "Shoot" |
| **Binding** | What physical input triggers an action | W key, Left Stick, Mouse Button |
| **Composite** | Multiple inputs combined into one | WASD → Vector2 |

---

### 🎓 Action Types Explained

When you create an action, you choose its **Action Type**:

#### **1. Button** (For on/off inputs)
```csharp
// Example: Sprint, Jump, Shoot
bool isPressed = inputActions.Player.Sprint.IsPressed();      // Is it held down NOW?
bool wasPressed = inputActions.Player.Sprint.WasPressedThisFrame();  // Just pressed?
bool wasReleased = inputActions.Player.Sprint.WasReleasedThisFrame(); // Just released?
```

**Use for:** Jump, Shoot, Interact, Sprint toggle

#### **2. Value** (For continuous inputs)
```csharp
// Example: Movement, Mouse look
Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();
float triggerValue = inputActions.Player.Accelerate.ReadValue<float>();
```

**Control Types for Value actions** (in Unity's UI, look for "Control Type" dropdown):

| Unity UI Name | C# Type | Values | Use Case |
|---------------|---------|--------|----------|
| **Axis** | `float` | -1 to +1 | Single direction: trigger, throttle, steering wheel |
| **Vector2** | `Vector2` | (x, y) each -1 to +1 | Two directions combined: WASD, joystick, mouse movement |
| **Vector3** | `Vector3` | (x, y, z) | Rare: VR controller position tracking |

> 💡 **Why "Axis" = float?** An axis is a single line of values from -1 to +1. In C#, a decimal number is a `float`, so Unity calls it "Axis" in the UI but it's just a float value.

> 💡 **Why Vector2 for movement?** Movement needs TWO axes working together (left/right AND forward/back). Vector2 packages two floats into one convenient value. This also means WASD and gamepad sticks output identical data!

**Use for:** Movement, Camera look, Driving acceleration

#### **3. Pass Through** (For raw, unprocessed input)

**The difference from Value:**

| Scenario | Value | Pass Through |
|----------|-------|--------------|
| Two gamepads connected, both press A | Only ONE value reported (the "active" controller) | BOTH inputs reported separately |
| Mouse moves while key held | Might miss some events | Every single event comes through |

```csharp
// Pass Through gives you EVERY input event from ALL devices
// Value "filters" to give you one clean value
```

**When to use Pass Through:**
- **Local multiplayer** - Multiple controllers need to be tracked separately
- **Input recording/playback** - Need to capture every single input event
- **Debugging** - See exactly what inputs are happening

**For single-player games, you almost always want Button or Value, not Pass Through.**

---

### 🔄 The Input Lifecycle: Enable/Disable

**Critical concept:** Input Actions are OFF by default!

```csharp
// 1. CREATE the input actions (they're OFF)
inputActions = new PlayerInputActions();

// 2. ENABLE them (now they listen for input!)
inputActions.Player.Enable();

// 3. READ input in Update()
Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();

// 4. DISABLE when done (prevents memory leaks)
inputActions.Player.Disable();
```

**Why this pattern?**

```csharp
void Awake()
{
    // Create ONCE when script loads
    inputActions = new PlayerInputActions();
}

void OnEnable()
{
    // Turn ON when script becomes active
    inputActions.Player.Enable();
}

void OnDisable()
{
    // Turn OFF when script becomes inactive
    // IMPORTANT: Prevents memory leaks!
    inputActions.Player.Disable();
}
```

**Real-world scenario:**
```
1. Player opens pause menu → OnDisable() → Input stops
2. Player closes menu → OnEnable() → Input resumes
3. Player dies → Object destroyed → OnDisable() → Clean up
```

---

## Part 3: Deep Dive - Vector Math for Movement (10 minutes)

### 📐 Understanding Vector2 Input

When you read movement input, you get a **Vector2**:

```csharp
Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
// input.x = left/right (A/D keys)
// input.y = forward/back (W/S keys)
```

**What values do you get?**

| Keys Pressed | input.x | input.y | Meaning |
|--------------|---------|---------|---------|
| None | 0 | 0 | Standing still |
| W | 0 | 1 | Forward |
| S | 0 | -1 | Backward |
| A | -1 | 0 | Left |
| D | 1 | 0 | Right |
| W + D | 1 | 1 | Forward-Right (diagonal) |
| S + A | -1 | -1 | Backward-Left (diagonal) |

---

### 🧭 Transform Directions: forward, right, up

Every GameObject has a **Transform** component - it's the most fundamental component in Unity! It stores:
- **Position** - Where the object is in 3D space
- **Rotation** - Which way the object is facing
- **Scale** - How big the object is

**Why Transform matters for movement:**

Without Transform, you'd have to manually track position and calculate directions using complex trigonometry. Transform gives you ready-to-use direction vectors that automatically update when the object rotates!

Every Transform has 3 direction vectors:

```csharp
transform.forward  // Blue arrow (Z+) - where object faces
transform.right    // Red arrow (X+) - object's right side
transform.up       // Green arrow (Y+) - object's top
```

**These are ALWAYS normalized (length = 1) and in WORLD SPACE.**

**Visual:**
```
        up (green)
         ↑
         |
         |
    ←----●----→ right (red)
        /
       /
      ↓
   forward (blue)
```

---

### ❓ Why No `.backward` or `.left`?

Unity only provides `forward`, `right`, and `up`. But you don't need the opposites - just negate them!

```csharp
// The 3 built-in directions:
transform.forward   // (0, 0, 1) when facing north
transform.right     // (1, 0, 0) when facing north
transform.up        // (0, 1, 0) always points up

// Get the opposites by negating:
-transform.forward  // BACKWARD - same as (0, 0, -1)
-transform.right    // LEFT - same as (-1, 0, 0)
-transform.up       // DOWN - same as (0, -1, 0)
```

**Why this design?**
1. **Less redundancy** - Why store 6 vectors when 3 can do the job?
2. **Math is simple** - Negating a vector is trivial: `(x, y, z)` becomes `(-x, -y, -z)`
3. **Input already handles it** - When you press S or A, input values are NEGATIVE, so the math works automatically!

```csharp
// When pressing W: input.y = 1
transform.forward * 1 = forward (move forward) ✓

// When pressing S: input.y = -1
transform.forward * -1 = -forward (move backward) ✓

// When pressing D: input.x = 1
transform.right * 1 = right (move right) ✓

// When pressing A: input.x = -1
transform.right * -1 = -right (move left) ✓
```

**The negative input value automatically gives you the opposite direction!**

---

### 🔄 Why Do These Directions Matter?

**The magic:** These directions change when your player turns!

**Think of it like holding a flashlight:**
- `transform.forward` = where the flashlight beam points
- `transform.right` = your right hand holding the flashlight

When YOU turn, the beam turns with you. Same thing happens in Unity!

---

**Example: Your player is standing in a room**

```
         NORTH
           ↑
           |
   WEST ←--😀--→ EAST     (Player facing NORTH)
           |
         SOUTH
```

Right now:
- `transform.forward` points **NORTH** (where player's face is)
- `transform.right` points **EAST** (player's right hand side)

---

**Now the player turns 90° to face EAST:**

```
         NORTH
           ↑
           |
   WEST ←--😀→→ EAST     (Player now facing EAST)
           |
         SOUTH
```

Now:
- `transform.forward` points **EAST** (where player's face is now)
- `transform.right` points **SOUTH** (player's right hand side now)

---

**Why this is awesome:**

When you write movement code:
```csharp
// "Move forward" ALWAYS means "move where I'm facing"
transform.forward * speed
```

You don't need to know if the player faces north, east, or any direction. Unity figures it out for you!

**The directions ROTATE WITH THE PLAYER!**

---

### 🔢 Converting 2D Input to 3D Movement

This is the KEY formula for movement:

```csharp
Vector3 moveDirection = transform.forward * input.y + transform.right * input.x;
```

**Let's break this down:**

```
Player faces NORTH (forward = (0,0,1), right = (1,0,0))

Press W (input = (0, 1)):
  moveDirection = (0,0,1) * 1 + (1,0,0) * 0
                = (0,0,1) + (0,0,0)
                = (0, 0, 1)  → Move NORTH ✓

Press D (input = (1, 0)):
  moveDirection = (0,0,1) * 0 + (1,0,0) * 1
                = (0,0,0) + (1,0,0)
                = (1, 0, 0)  → Move EAST ✓

Press W+D (input = (1, 1)):
  moveDirection = (0,0,1) * 1 + (1,0,0) * 1
                = (0,0,1) + (1,0,0)
                = (1, 0, 1)  → Move NORTHEAST ✓
```

---

### ⚠️ The Diagonal Speed Problem

**Problem:** When pressing W+D, the vector is (1, 0, 1).

**How do we calculate the length of this vector?**

You know the 2D Pythagorean theorem: `a² + b² = c²`

In 3D, we just add the third dimension: `x² + y² + z² = length²`

Our vector (1, 0, 1) means:
- **x = 1** (moving right)
- **y = 0** (not moving up/down - staying on the ground)
- **z = 1** (moving forward)

```csharp
// Length (magnitude) of vector (1, 0, 1):
length = √(x² + y² + z²)
       = √(1² + 0² + 1²)
       = √(1 + 0 + 1)
       = √2
       = 1.414
```

> 💡 **Why is there a 0² in there?** Because we always measure all 3 directions in 3D, even when one is zero. The Y (up/down) is 0 because we're moving on flat ground, not jumping or falling.

**This means diagonal movement is 41% faster than straight movement!**

```
  Speed = 1.0     Speed = 1.414
      ↑               ↗
      |              /
      |             / (faster!)
      |            /
      ●           ●
```

**Solution: Normalize the vector!**

```csharp
if (moveDirection.magnitude > 0.1f)
{
    moveDirection.Normalize();  // Makes length = 1
}
```

**After normalizing (1, 0, 1):**
```csharp
// Normalized (1, 0, 1):
// Each component divided by magnitude (1.414)
normalized = (0.707, 0, 0.707)
magnitude = √(0.707² + 0² + 0.707²) = 1.0  ✓
```

**Now all directions have the same speed!**

---

## Part 4: Deep Dive - Understanding PlayerController (10 minutes)

### 🏗️ Script Architecture Overview

The PlayerController has 4 main responsibilities:

```
┌─────────────────────────────────────────────┐
│              PlayerController               │
├─────────────────────────────────────────────┤
│  1. INPUT          → Read WASD/Sprint       │
│  2. DIRECTION      → Calculate move vector  │
│  3. GRAVITY        → Apply falling force    │
│  4. MOVEMENT       → Actually move player   │
└─────────────────────────────────────────────┘
```

---

### 📖 Code Walkthrough: HandleMovement()

```csharp
private void HandleMovement()
{
    // STEP 1: Read raw input from Input Actions
    Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
    
    // STEP 2: Check sprint state
    bool isSprinting = inputActions.Player.Sprint.IsPressed();
    
    // STEP 3: Convert 2D input → 3D world direction
    // This makes movement relative to where player FACES
    moveDirection = transform.forward * input.y + transform.right * input.x;
    
    // STEP 4: Fix diagonal speed (normalize)
    if (moveDirection.magnitude > 0.1f)
    {
        moveDirection.Normalize();
    }
    
    // STEP 5: Apply speed multiplier
    float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
    moveDirection *= currentSpeed;
}
```

**Flow visualization:**
```
Input (0,1)          →  Direction (0,0,1)      →  Normalized (0,0,1)  →  Final (0,0,3)
(pressing W)            (forward)                  (length = 1)          (speed = 3)
```

---

### 📖 Code Walkthrough: HandleGravity()

**Why do we need manual gravity?**

Character Controller does NOT have built-in gravity! (Unlike Rigidbody)

```csharp
private float verticalVelocity = 0f;  // Tracks falling speed

private void HandleGravity()
{
    if (characterController.isGrounded)
    {
        // GROUNDED: Apply small downward force
        verticalVelocity = groundedGravity;  // -2f
        
        // WHY -2 and not 0?
        // - Ensures isGrounded stays TRUE
        // - Prevents "bouncing" on slopes
        // - Keeps player "stuck" to ground
    }
    else
    {
        // IN AIR: Accelerate downward
        verticalVelocity += Physics.gravity.y * Time.deltaTime;
        // Physics.gravity.y = -9.81 (Earth's gravity)
        
        // Frame 1: velocity = 0 + (-9.81 * 0.016) = -0.16
        // Frame 2: velocity = -0.16 + (-9.81 * 0.016) = -0.32
        // Frame 3: velocity = -0.32 + (-9.81 * 0.016) = -0.48
        // ... keeps accelerating!
        
        // Clamp to prevent extreme speeds
        verticalVelocity = Mathf.Max(verticalVelocity, -50f);
    }
}
```

---

### ⚠️ Why Clamp Falling Speed to -50?

**The problem:** Without a limit, falling speed keeps increasing forever!

```
Time falling:    1 sec    5 sec    10 sec    60 sec
Velocity:        -9.8     -49      -98       -588 m/s ← CRAZY FAST!
```

At -588 m/s, the player falls **faster than the speed of sound**! 🚀

**Why -50 specifically?**

`-50 m/s ≈ 112 mph` - This is called **terminal velocity** in real life.

In the real world, air resistance stops you from falling infinitely fast. A skydiver in free-fall maxes out around 120 mph (about 53 m/s). We use -50 as a nice round number.

| Value | Speed | Real-World Comparison |
|-------|-------|----------------------|
| -10 | 22 mph | Running fast |
| -50 | 112 mph | Skydiver / Fast car |
| -100 | 224 mph | Race car |
| No limit | ∞ | Breaks the game! |

---

### ❓ Why `Mathf.Max` and not `Mathf.Min`?

This is tricky because of **negative numbers**!

```csharp
// We want: velocity can't go BELOW -50 (can't be MORE negative)
// But -50 is "bigger" than -100 on the number line!

Number line:  -100 ← -50 ← -25 ← 0 → 25 → 50 → 100
              smaller ←──────────────────→ bigger

Mathf.Max(-100, -50) = -50  ← Returns the "bigger" (less negative) one ✓
Mathf.Max(-30, -50) = -30   ← -30 is already above our limit, keep it ✓
```

**So `Mathf.Max(velocity, -50)` means: "Don't let velocity go below -50"**

---

### 💥 What Happens Without This Clamp?

1. Player falls for a long time
2. Velocity becomes -500, -1000, -5000...
3. Player moves SO fast they **pass through the ground** (collision missed!)
4. Player falls into the void forever 💀

**The clamp prevents this catastrophic bug!**

---

**Gravity over time (visualized):**
```
Time:      0s    0.1s   0.2s   0.3s   0.4s   0.5s
Velocity:  0    -0.98  -1.96  -2.94  -3.92  -4.90  (m/s)

The player falls FASTER the longer they're in the air!
This is realistic gravity acceleration.
```

---

### 📖 Code Walkthrough: ApplyMovement()

```csharp
private void ApplyMovement()
{
    // STEP 1: Create horizontal movement vector
    Vector3 movement = moveDirection * Time.deltaTime;
    
    // STEP 2: Add vertical (gravity) movement
    movement.y = verticalVelocity * Time.deltaTime;
    
    // STEP 3: Apply to Character Controller
    characterController.Move(movement);
}
```

**Why `* Time.deltaTime`?**

`Time.deltaTime` = time since last frame (usually ~0.016 seconds at 60 FPS)

```csharp
// WITHOUT deltaTime (BAD):
speed = 5;  // units per FRAME
// At 60 FPS: 5 * 60 = 300 units/second
// At 30 FPS: 5 * 30 = 150 units/second  ← DIFFERENT SPEED!

// WITH deltaTime (GOOD):
speed = 5;  // units per SECOND
movement = speed * Time.deltaTime;
// At 60 FPS: 5 * 0.016 = 0.08 per frame → 5 units/second
// At 30 FPS: 5 * 0.033 = 0.16 per frame → 5 units/second  ← SAME SPEED!
```

**deltaTime makes your game frame-rate independent!**

---

## Part 5: Deep Dive - Camera Follow & LateUpdate (10 minutes)

### 🎥 Why Cameras Are Special

Cameras need special handling because of **update order**:

```
Frame Timeline:
┌──────────────────────────────────────────────────────┐
│ FixedUpdate → Update → LateUpdate → Render          │
│     ↑            ↑          ↑                        │
│  Physics     Movement    Camera                      │
│              (Player)    (Follows)                   │
└──────────────────────────────────────────────────────┘
```

**The Problem with Update():**
```csharp
// If camera uses Update():
void Update()
{
    // Camera might update BEFORE player!
    // Camera sees player's OLD position
    // Result: Jittery camera
}
```

**The Solution: LateUpdate():**
```csharp
// LateUpdate runs AFTER all Update() calls
void LateUpdate()
{
    // Player has already moved
    // Camera sees player's NEW position
    // Result: Smooth camera!
}
```

---

### 🧮 Understanding Lerp (Linear Interpolation)

**Lerp** blends between two values:

```csharp
float result = Mathf.Lerp(a, b, t);
// t = 0  →  result = a (start)
// t = 0.5 →  result = (a+b)/2 (middle)
// t = 1  →  result = b (end)
```

**For vectors:**
```csharp
Vector3 result = Vector3.Lerp(start, end, t);
```

**Visual:**
```
t = 0.0      t = 0.25     t = 0.5      t = 0.75     t = 1.0
  A────────────────────────────────────────────────────B
  ●            ●            ●            ●            ●
result      result       result       result       result
```

---

### 🪢 Lerp Does NOT Always Move "Fast Then Slow"

> **Important:** Lerp by itself has NO built-in motion style. It's just a formula:
> `result = start + (end - start) * t` — a blend knob between two values. That's it.
>
> The "fast then slow" behavior comes from **how WE choose to use it** for the camera.

There are different ways to use Lerp, and each creates a different feel:

| How you use Lerp | What happens | Motion style |
|---|---|---|
| **Every frame, current→target** (our camera) | Moves a % of remaining gap | Fast → slow (ease-out) |
| **Fixed `t` sliding 0→1 over time** | Even progress each step | Constant speed (linear) |
| **`t` starts small, gets bigger** | Slow start, speeds up | Slow → fast (ease-in) |

**We CHOSE the first way for our camera.** But someone else could use Lerp differently for a different effect. Lerp is just a tool — the behavior depends on how you configure it.

---

### 🪢 Why Our Camera Uses the "Rubber Band" Style

In our code, we call Lerp **every frame** with `current position → target position`. This means each frame we say:

> "Move 10% of the **remaining** gap."

**Think of it like a rubber band** tied between the camera and the player:

```
Player runs right →

[Camera]━━━━━━━━━━━━━━━━━━━━[Player]
         LONG stretch = STRONG pull → BIG movement

[Camera]━━━━━━[Player]
         SHORT stretch = WEAK pull → small movement

[Camera]━[Player]
         TINY stretch = barely pulls → tiny nudge
```

The rubber band doesn't "choose" to go fast first. It just pulls **proportionally to how stretched it is**. Big gap = strong pull. Small gap = weak pull.

**The numbers:**
```
Frame 1: Gap = 100  → Move 10% → moves 10   (fast — big gap!)
Frame 2: Gap = 90   → Move 10% → moves 9
Frame 3: Gap = 81   → Move 10% → moves 8.1  (slowing down...)
Frame 5: Gap = 65.6 → Move 10% → moves 6.6
Frame 10: Gap = 34.9 → Move 10% → moves 3.5 (gentle now)
Frame 20: Gap = 12.2 → Move 10% → moves 1.2 (crawling in)
```

---

### ❓ Why This Style Works for a Camera (Not Slow→Fast)

If the camera moved **slow first, then fast**, it would feel broken:

```
Player moves right →

Frame 1: Player runs away... camera barely moves     😐 "hello? follow me?"
Frame 2: Player is far away... camera still slow      😠 "come ON!"
Frame 3: Camera FINALLY speeds up                     🤨 "why did you wait??"
Frame 4: Camera zooms past at full speed              😵 "WHOA too fast!"
```

**The rubber band way (fast first) feels natural for a camera:**

```
Frame 1: Player moves... camera SNAPS to chase!       ✓ Responsive!
Frame 2: Camera closing in quickly...                  ✓ Keeping up!
Frame 3: Camera getting close, slowing gently...       ✓ Smooth!
Frame 4: Camera settles into position softly           ✓ No jerk!
```

**Real-life things that move fast→slow (like our camera):**
- Catching a ball — hand rushes toward it, then gently closes
- A door closing — swings fast, then settles gently
- Sitting in a chair — you drop toward it fast, then ease into place

**But other things in games DO use slow→fast (different Lerp configuration):**
- A car accelerating from a stop
- A rocket launching
- A charging attack winding up

**The point:** Lerp is a general-purpose tool. We configure it the rubber-band way because that's what feels right for a follow-camera. A different game feature might configure Lerp completely differently.

---

### � When to Use Each Lerp Style in Games

Here's a cheat sheet — pick the motion style that matches what you're building:

#### **Ease-Out (Fast → Slow)** — "Rubber band / Catching up"

Use when something needs to **react immediately** then **settle gently**:

| Game Feature | Why ease-out works |
|---|---|
| Camera following player | Snaps to chase, settles smoothly |
| Health bar going down | Drops fast, then creeps to final value |
| UI panel sliding in | Flies in, then gently stops in place |
| Cursor snapping to a menu button | Jumps toward it, eases into position |
| Recoil recovery | Gun kicks up fast, slowly returns to center |

```csharp
// Ease-out: call Lerp every frame from current → target
position = Vector3.Lerp(position, target, 5f * Time.deltaTime);
```

---

#### **Linear (Constant Speed)** — "Steady and predictable"

Use when something should move at an **even, mechanical pace**:

| Game Feature | Why linear works |
|---|---|
| Loading bar / progress bar | Should fill at a steady rate |
| Elevator moving between floors | Constant speed, no speeding up or slowing down |
| Timer countdown visual | Even ticking |
| Conveyor belt objects | Steady mechanical movement |
| Lerping a color over exactly 2 seconds | Even fade from red to blue |

```csharp
// Linear: slide t from 0 to 1 over a fixed duration
float t = elapsedTime / totalDuration;  // goes 0 → 1 evenly
color = Color.Lerp(startColor, endColor, t);
```

---

#### **Ease-In (Slow → Fast)** — "Winding up / Building power"

Use when something needs to **build momentum** before reaching full speed:

| Game Feature | Why ease-in works |
|---|---|
| Car accelerating from stop | Slow start, then vroom! |
| Charging a punch/attack | Slow windup, then STRIKE |
| Rocket launch | Rumbles slowly, then shoots up |
| Bowling ball rolling | Slow push, picks up speed down the lane |
| Trap door opening | Creaks open slowly, then swings wide |

```csharp
// Ease-in: make t grow slowly at first, then fast
float t = elapsedTime / totalDuration;
float easedT = t * t;  // squaring makes small values SMALLER (slow start)
position = Vector3.Lerp(start, end, easedT);
```

**Why does `t * t` work?**
```
t = 0.1  → t*t = 0.01   (barely moved!)
t = 0.3  → t*t = 0.09   (still slow)
t = 0.5  → t*t = 0.25   (only 25% there at halfway point)
t = 0.9  → t*t = 0.81   (NOW it's moving fast!)
t = 1.0  → t*t = 1.0    (arrived)
```

---

#### **Ease-In-Out (Slow → Fast → Slow)** — "Smooth start and stop"

Use when something should **gently start AND gently stop**:

| Game Feature | Why ease-in-out works |
|---|---|
| Camera panning in a cutscene | Smooth dramatic movement |
| Door opening and closing | Starts slow, speeds up, slows at end |
| Character turning 180° | Doesn't snap — eases into and out of the turn |
| Menu transition / page flip | Feels polished and professional |
| Moving platform (ride-on) | Doesn't jerk the player at start or stop |

```csharp
// Ease-in-out: slow at both ends, fast in the middle
float t = elapsedTime / totalDuration;
float easedT = t < 0.5f
    ? 2f * t * t                        // first half: ease-in (slow start)
    : 1f - Mathf.Pow(-2f * t + 2f, 2) / 2f;  // second half: ease-out (slow stop)
position = Vector3.Lerp(start, end, easedT);
```

---

#### **Quick Reference**

```
Ease-out:     ●●●●●●●●━━━━━━━━━━━────────────  (our camera)
              FAST start          SLOW end

Linear:       ●───●───●───●───●───●───●───●───  (progress bar)
              constant speed throughout

Ease-in:      ────────────━━━━━━━━━━━●●●●●●●●●  (charging attack)
              SLOW start            FAST end

Ease-in-out:  ────━━━━●●●●●●●●●●●●●●━━━━────── (cutscene camera)
              slow   FAST middle    slow
```

---

### �🎯 Lerp for Smooth Following

In our camera:

```csharp
Vector3 smoothedPosition = Vector3.Lerp(
    transform.position,           // Where camera IS now
    desiredPosition,              // Where camera WANTS to be
    smoothSpeed * Time.deltaTime  // How much to move (0-1)
);
```

**How it works each frame:**

```
Frame 1: Camera at 0, Target at 10, t = 0.1
         New position = Lerp(0, 10, 0.1) = 1

Frame 2: Camera at 1, Target at 10, t = 0.1
         New position = Lerp(1, 10, 0.1) = 1.9

Frame 3: Camera at 1.9, Target at 10, t = 0.1
         New position = Lerp(1.9, 10, 0.1) = 2.71

... Camera gets closer but SLOWS DOWN as it approaches!
```

**This creates "easing" - fast at first, slow at end:**
```
Camera position over time:
10 |                                    ●●●●●●●●
   |                            ●●●●●
   |                      ●●●●
   |                 ●●●
   |            ●●
   |        ●●
   |     ●
   |  ●
 0 |●
   └─────────────────────────────────────────── Time
```

---

### 📏 Understanding Camera Offset

The offset determines where camera sits relative to player:

```csharp
public Vector3 offset = new Vector3(0, 2, -5);
//                                  X  Y   Z
//                               Left Up Back
//                               Right Down Forward
```

**Visual (top-down view):**
```
                 Z+ (forward)
                    ↑
                    |
                    |
                 [Player]
                    |
                    | 5 units
                    |
                 [Camera] ← Also 2 units UP (Y)
                    
          ← X- (left)    X+ (right) →
```

**Different offset examples:**
```csharp
(0, 2, -5)   // Default: Behind and above
(3, 2, -4)   // Over-the-shoulder (right)
(-3, 2, -4)  // Over-the-shoulder (left)
(0, 10, -10) // High and far (strategy game)
(0, 1, -2)   // Close follow (action game)
```

---

## Part 6: Demo - Setting Up Your Project (10 minutes)

Now let's apply everything we learned! Since you already have the scripts, we'll focus on:
1. Verifying the Input System Package is active
2. Setting up Input Actions
3. Configuring the scene
4. Attaching and configuring scripts

---

### 📝 Step 0: Verify Active Input Handler

> **⚠️ Critical:** Unity can use the **old Input Manager**, the **new Input System Package**, or **Both**. Our scripts use the new Input System Package exclusively. If this setting is wrong, your controls won't work!

**In Unity Editor:**

1. Go to **Edit → Project Settings → Player**
2. Expand **Other Settings**
3. Scroll down to **Configuration**
4. Find **Active Input Handling** and set it to **Input System Package (New)**
   - Do NOT leave it on "Input Manager (Old)" or "Both"
   - Unity will ask to restart the Editor — click **Yes**

**Why not "Both"?**
- "Both" loads the legacy Input Manager alongside the new Input System, which wastes resources and can cause confusing warnings in the Console
- Since our scripts only use `UnityEngine.InputSystem`, we only need the new system

---

### 📝 Step 1: Create the Input Actions Asset

> **⚠️ Important:** `PlayerInputActions` is NOT a package you install! It's a file YOU create in Unity.

**In Unity Editor:**

1. **Look at the Project window** (usually at the bottom of Unity)
   - This shows all your files and folders
   - You should see folders like "Assets", "Scenes", etc.

2. **Right-click on the Assets folder**
   - A context menu appears

3. **Click: Create → Input Actions**
   - A new file appears with a highlighted name

4. **Type the name:** `PlayerInputActions`
   - Press Enter to confirm

5. **You should now see** a file called `PlayerInputActions` in your Assets folder
   - It has a special icon that looks like a gamepad

**Screenshot of what to look for:**
```
Project Window:
📁 Assets
  📄 PlayerInputActions  ← You just created this!
  📁 Scenes
  📁 Scripts (you'll create this later)
```

---

### 📝 Step 1: Create the Input Actions Asset

> **⚠️ Important:** `PlayerInputActions` is NOT a package you install! It's a file YOU create in Unity.

**In Unity Editor:**

1. **Look at the Project window** (usually at the bottom of Unity)
   - This shows all your files and folders
   - You should see folders like "Assets", "Scenes", etc.

2. **Right-click on the Assets folder**
   - A context menu appears

3. **Click: Create → Input Actions**
   - A new file appears with a highlighted name

4. **Type the name:** `PlayerInputActions`
   - Press Enter to confirm

5. **You should now see** a file called `PlayerInputActions` in your Assets folder
   - It has a special icon that looks like a gamepad

**Screenshot of what to look for:**
```
Project Window:
📁 Assets
  📄 PlayerInputActions  ← You just created this!
  📁 Scenes
  📁 Scripts (you'll create this later)
```

---

### 📝 Step 2: Open the Input Actions Editor

1. **Double-click on `PlayerInputActions`** in the Project window
   - This opens a special editor window for configuring controls

2. **You should see a window with 3 columns:**
   - Left: "Action Maps" (groups of controls)
   - Middle: "Actions" (individual controls)
   - Right: "Properties" (settings for selected item)

---

### 📝 Step 3: Create Action Map and Actions

**Create the "Player" action map:**
1. Click + next to "Action Maps"
2. Name it: `Player`

**Create "Move" action:**
1. Click + next to "Actions"
2. Name it: `Move`
3. In Properties: Action Type = **Value**, Control Type = **Vector2**
4. Click + next to Move → "Add Up/Down/Left/Right Composite"
5. Configure each direction (click → Path → Listen → press the key):
   - Up: W
   - Down: S
   - Left: A
   - Right: D

**Create "Sprint" action:**
1. Click + next to "Actions"
2. Name it: `Sprint`
3. Action Type: **Button** (default)
4. Click + → "Add Binding"
5. Path → Listen → press **Left Shift**

---

### 📝 Step 4: Save and Generate C# Code

1. Click **"Save Asset"** at the top
2. Close the Input Actions window
3. Select `PlayerInputActions` in Project window (single click)
4. In Inspector, check ☑️ **"Generate C# Class"**
5. Click **"Apply"**

**Unity creates `PlayerInputActions.cs` automatically!**

---

### 📝 Step 5: Set Up Your Scene

**Verify you have:**
- [ ] Player GameObject with Character Controller (Height: 2, Radius: 0.5, Center: (0, 1, 0))
- [ ] Ground Plane (with a Collider!)
- [ ] Main Camera

---

### 📝 Step 6: Create Scripts Folder and Add Scripts

1. **Right-click Assets → Create → Folder → name it `Scripts`**

2. **Copy the provided scripts:**
   - `PlayerController.cs` → into Scripts folder
   - `CameraFollow.cs` → into Scripts folder

3. **Attach scripts:**
   - Select **Player** → Add Component → PlayerController
   - Select **Main Camera** → Add Component → CameraFollow

4. **Configure CameraFollow:**
   - Drag **Player** to the **Target** field
   - Set Offset: (0, 2, -5)
   - Set Smooth Speed: 10

---

### 📝 Step 7: Position Camera

1. Select **Main Camera**
2. Set Position: (0, 3, -5)
3. Set Rotation: (10, 0, 0)

---

## Part 7: Testing Your Game! (5 minutes)

### ▶️ How to Test

1. **Click the Play button** (▶️ at top center of Unity)

2. **Test these controls:**

| Key | What Should Happen |
|-----|-------------------|
| W | Player moves forward |
| S | Player moves backward |
| A | Player moves left |
| D | Player moves right |
| W+D | Player moves diagonally (same speed as straight!) |
| Shift + WASD | Player moves FASTER (sprinting) |

3. **Test the camera:**
   - Walk around - camera should follow smoothly
   - Camera should stay behind the player
   - Walk off edge - player should fall (gravity works!)

4. **Press Play again** to stop testing

---

### 🐛 Troubleshooting Common Issues

#### Problem: Player doesn't move at all

**Check:**
1. Is **Active Input Handling** set to **Input System Package (New)**? (Edit → Project Settings → Player → Other Settings → Configuration). If it's set to "Input Manager (Old)" or "Both", change it to "Input System Package (New)" and restart Unity.
2. Is PlayerController script attached to Player?
3. Does PlayerInputActions.cs exist? (Generate C# Class checked?)
4. Did you click in the Game window? (Unity needs focus)
5. Any errors in Console window?

---

#### Problem: Player falls through the ground

**Check:**
1. Does Ground have a Collider? (Box Collider or Mesh Collider)
2. Is Character Controller on Player with correct settings?

---

#### Problem: Camera doesn't follow

**Check:**
1. Is CameraFollow script on Main Camera?
2. Is the Target field set to Player?

---

### ✅ Success Criteria

You've completed Week 5 if:
- [ ] Player moves with WASD keys
- [ ] Player moves faster when holding Shift
- [ ] Player falls when walking off edge (gravity works)
- [ ] Camera smoothly follows the player
- [ ] No errors in the Console window

---

## 🎉 Week 5 Complete!

### What You Built:
- ✅ Input Actions asset with Move and Sprint
- ✅ PlayerController with WASD movement, sprint, and gravity
- ✅ CameraFollow with smooth following

### Key Concepts You Learned:
- **Input System architecture** (Actions, Maps, Bindings)
- **Action Types** (Button vs Value)
- **Vector math** (forward, right, normalization)
- **Gravity implementation** with Character Controller
- **LateUpdate** for cameras
- **Lerp** for smooth interpolation
- **Time.deltaTime** for frame-rate independence

---

## 📚 Quick Reference

### Input Reading
```csharp
Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
bool sprinting = inputActions.Player.Sprint.IsPressed();
```

### Direction Calculation
```csharp
Vector3 dir = transform.forward * input.y + transform.right * input.x;
if (dir.magnitude > 0.1f) dir.Normalize();
```

### Smooth Following
```csharp
Vector3 smoothed = Vector3.Lerp(current, target, speed * Time.deltaTime);
```

---

## � Complete Scripts with Implementation Notes

Below are the complete scripts. Each section is annotated with **what concept it applies** from this week's learning.

---

### PlayerController.cs - Full Implementation

Create this file at: `Assets/Scripts/PlayerController.cs`

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Week 5: Basic player movement using Character Controller
/// Handles WASD movement, sprint, and gravity
/// </summary>
public class PlayerController : MonoBehaviour
{
    // =========================================================
    // INSPECTOR FIELDS (Appear in Unity's Inspector panel)
    // =========================================================
    // [Header] creates a label in Inspector
    // [Tooltip] shows help when you hover over the field
    
    [Header("Movement Settings")]
    [Tooltip("Normal walking speed (units per second)")]
    public float walkSpeed = 3f;
    
    [Tooltip("Sprint speed when holding Shift (units per second)")]
    public float sprintSpeed = 6f;
    
    [Header("Gravity Settings")]
    [Tooltip("Downward force when grounded (keeps player stuck to ground)")]
    public float groundedGravity = -2f;
    
    [Header("Debug")]
    [Tooltip("Show debug info in console")]
    public bool showDebugInfo = false;
    
    // =========================================================
    // PRIVATE VARIABLES (Only this script can access these)
    // =========================================================
    
    // Components we need to access
    private CharacterController characterController;  // Handles collision & movement
    private PlayerInputActions inputActions;          // Our input (from the asset you created!)
    
    // State tracking
    private float verticalVelocity = 0f;       // For gravity (falling speed)
    private Vector3 moveDirection = Vector3.zero;  // Current movement direction
    
    // =========================================================
    // AWAKE - Runs ONCE when object is created (before Start)
    // =========================================================
    // USE FOR: Getting component references, creating objects
    
    void Awake()
    {
        // GET COMPONENT: Find the CharacterController on this same GameObject
        // CONCEPT: GetComponent<T>() searches THIS object for a component of type T
        characterController = GetComponent<CharacterController>();
        
        // Safety check - if no CharacterController, the script won't work
        if (characterController == null)
        {
            Debug.LogError("PlayerController requires a CharacterController component!");
            enabled = false;  // Disable this script
            return;
        }
        
        // CREATE the input actions instance
        // CONCEPT: This is the C# class Unity generated from your PlayerInputActions asset
        // The actions are DISABLED by default - we enable them in OnEnable()
        inputActions = new PlayerInputActions();
    }
    
    // =========================================================
    // ON ENABLE - Runs when script becomes active
    // =========================================================
    // USE FOR: Turning ON things that need to be turned off later
    
    void OnEnable()
    {
        // ENABLE the Player action map so it listens for input
        // CONCEPT: Input Actions are OFF by default - you must turn them on!
        inputActions.Player.Enable();
    }
    
    // =========================================================
    // ON DISABLE - Runs when script becomes inactive
    // =========================================================
    // USE FOR: Cleanup, preventing memory leaks
    
    void OnDisable()
    {
        // DISABLE the Player action map
        // CONCEPT: Prevents memory leaks! The Input System holds references to enabled actions.
        // If you don't disable them, those references stick around even after the object is destroyed.
        inputActions.Player.Disable();
    }
    
    // =========================================================
    // UPDATE - Runs every frame (60+ times per second)
    // =========================================================
    // USE FOR: Reading input, calculating movement, game logic
    
    void Update()
    {
        HandleMovement();  // Step 1: Read input, calculate direction
        HandleGravity();   // Step 2: Apply gravity
        ApplyMovement();   // Step 3: Actually move the player
        
        if (showDebugInfo)
        {
            DebugInfo();
        }
    }
    
    // =========================================================
    // HANDLE MOVEMENT - Read input and calculate direction
    // =========================================================
    
    private void HandleMovement()
    {
        // STEP 1: READ INPUT
        // CONCEPT: ReadValue<Vector2>() gets the current input as a Vector2
        // - input.x = A/D keys (-1 to +1)
        // - input.y = W/S keys (-1 to +1)
        Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
        
        // CONCEPT: IsPressed() returns true if the key is currently held down
        bool isSprinting = inputActions.Player.Sprint.IsPressed();
        
        // STEP 2: CONVERT 2D INPUT → 3D DIRECTION
        // CONCEPT: transform.forward = where player faces (blue arrow in Unity)
        //          transform.right = player's right side (red arrow in Unity)
        // 
        // This formula combines them:
        // - input.y = forward/back → multiply by transform.forward
        // - input.x = left/right → multiply by transform.right
        // - Add them together = diagonal movement works automatically!
        moveDirection = transform.forward * input.y + transform.right * input.x;
        
        // STEP 3: NORMALIZE DIAGONAL MOVEMENT
        // CONCEPT: Without this, moving diagonally is 1.414x faster!
        // magnitude = √(x² + y² + z²) - length of the vector
        // Normalize() makes the length = 1, keeping direction the same
        if (moveDirection.magnitude > 0.1f)  // 0.1 deadzone to ignore tiny movements
        {
            moveDirection.Normalize();
        }
        
        // STEP 4: APPLY SPEED
        // CONCEPT: The ternary operator: condition ? valueIfTrue : valueIfFalse
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
        moveDirection *= currentSpeed;  // Multiply vector by speed
    }
    
    // =========================================================
    // HANDLE GRAVITY - Make player fall when in air
    // =========================================================
    
    private void HandleGravity()
    {
        // CONCEPT: CharacterController doesn't have built-in gravity like Rigidbody
        // We have to implement it ourselves!
        
        if (characterController.isGrounded)
        {
            // WHEN GROUNDED: Apply small downward force
            // WHY -2 instead of 0?
            // - Keeps isGrounded = true reliably
            // - Prevents player from "bouncing" on slopes
            // - Pushes player down into the ground slightly
            verticalVelocity = groundedGravity;  // -2f
        }
        else
        {
            // WHEN IN AIR: Accelerate downward (gravity)
            // CONCEPT: Physics.gravity.y = -9.81 (Earth's gravity in m/s²)
            // CONCEPT: Time.deltaTime = time since last frame (makes it frame-rate independent)
            //
            // Each frame, we ADD more downward velocity:
            // Frame 1: 0 + (-9.81 × 0.016) = -0.16
            // Frame 2: -0.16 + (-9.81 × 0.016) = -0.32
            // ... velocity increases = falls faster over time!
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
            
            // CLAMP: Don't fall faster than 50 m/s (prevents crazy speeds)
            // CONCEPT: Mathf.Max returns the larger value
            // -50 is "larger" than -100, so this limits falling speed
            //
            // WHY Mathf instead of Math?
            // - Math (System.Math) uses DOUBLE precision (64-bit)
            // - Mathf (UnityEngine.Mathf) uses FLOAT precision (32-bit)
            // - Unity uses floats everywhere (Vector3, positions, etc.)
            // - GPUs are optimized for floats, not doubles
            // - If you used Math.Max, you'd need: (float)Math.Max(...) ← annoying cast!
            // - Mathf.Max works directly with floats - cleaner code!
            verticalVelocity = Mathf.Max(verticalVelocity, -50f);
        }
    }
    
    // =========================================================
    // APPLY MOVEMENT - Actually move the player
    // =========================================================
    
    private void ApplyMovement()
    {
        // STEP 1: Calculate horizontal movement
        // CONCEPT: Multiply by Time.deltaTime to make frame-rate independent
        // Without it: 60 FPS moves 2x faster than 30 FPS!
        Vector3 movement = moveDirection * Time.deltaTime;
        
        // STEP 2: Add vertical movement (gravity)
        movement.y = verticalVelocity * Time.deltaTime;
        
        // STEP 3: Tell CharacterController to move
        // CONCEPT: CharacterController.Move() handles collision automatically
        // It won't let you walk through walls or fall through floors
        characterController.Move(movement);
    }
    
    // =========================================================
    // DEBUG - Show info in console
    // =========================================================
    
    private void DebugInfo()
    {
        Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
        Debug.Log($"Input: {input} | Grounded: {characterController.isGrounded} | " +
                  $"Vertical Velocity: {verticalVelocity:F2} | " +
                  $"Speed: {moveDirection.magnitude:F2}");
    }
    
    // =========================================================
    // PUBLIC GETTERS - Let other scripts read our state
    // =========================================================
    // CONCEPT: "=>" is a short way to write a read-only property
    // These let other scripts check our state without modifying it
    
    public bool IsGrounded => characterController.isGrounded;
    public bool IsMoving => moveDirection.magnitude > 0.1f;
    public float CurrentSpeed => moveDirection.magnitude;
}
```

---

### CameraFollow.cs - Full Implementation

Create this file at: `Assets/Scripts/CameraFollow.cs`

```csharp
using UnityEngine;

/// <summary>
/// Week 5: Basic camera follow system
/// Makes the camera smoothly follow the player from behind
/// </summary>
public class CameraFollow : MonoBehaviour
{
    // =========================================================
    // INSPECTOR FIELDS
    // =========================================================
    
    [Header("Target Settings")]
    [Tooltip("The transform to follow (usually the player)")]
    public Transform target;  // Drag your Player here in Unity Inspector!
    
    [Header("Camera Position")]
    [Tooltip("Offset from target position (X=left/right, Y=up/down, Z=forward/back)")]
    public Vector3 offset = new Vector3(0, 2, -5);
    // CONCEPT: offset = (0, 2, -5) means:
    // - X = 0: centered horizontally
    // - Y = 2: 2 units above the player
    // - Z = -5: 5 units behind the player
    
    [Header("Smoothing")]
    [Tooltip("How quickly camera catches up to target (higher = faster)")]
    [Range(1f, 20f)]  // Creates a slider in Inspector, limits 1 to 20
    public float smoothSpeed = 10f;
    
    [Header("Look At")]
    [Tooltip("Should camera always look at target?")]
    public bool lookAtTarget = true;
    
    [Tooltip("Offset for look-at target (look at player's head, not feet)")]
    public Vector3 lookAtOffset = new Vector3(0, 1, 0);  // 1 unit up from player's position
    
    [Header("Debug")]
    public bool showDebugGizmos = false;
    
    // =========================================================
    // LATE UPDATE - Runs AFTER all Update() calls
    // =========================================================
    // CONCEPT: Why LateUpdate instead of Update?
    // 
    // Frame order: FixedUpdate → Update → LateUpdate → Render
    // 
    // - Player moves in Update()
    // - Camera follows in LateUpdate()
    // 
    // This ensures camera always sees player's NEW position!
    // If camera used Update(), it might run BEFORE player moves = jittery camera
    
    void LateUpdate()
    {
        // Safety check - do nothing if no target assigned
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
    
    // =========================================================
    // FOLLOW TARGET - Smoothly move camera to follow player
    // =========================================================
    
    private void FollowTarget()
    {
        // STEP 1: Calculate where camera WANTS to be
        // CONCEPT: target.position = player's current position
        //          offset = how far from player we want to be
        Vector3 desiredPosition = target.position + offset;
        
        // STEP 2: Smoothly move from current position to desired position
        // CONCEPT: Vector3.Lerp does "linear interpolation"
        // 
        // Lerp(a, b, t) blends between a and b:
        // - t = 0 → returns a
        // - t = 0.5 → returns halfway between a and b
        // - t = 1 → returns b
        //
        // By using smoothSpeed * Time.deltaTime as t:
        // - Each frame, we move a FRACTION of the remaining distance
        // - This creates smooth "easing" (fast at first, slow as we get close)
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position,              // Where camera is NOW
            desiredPosition,                 // Where camera WANTS to be
            smoothSpeed * Time.deltaTime     // How much to move (0-1 range)
        );
        
        // STEP 3: Apply the new position
        transform.position = smoothedPosition;
    }
    
    // =========================================================
    // LOOK AT TARGET - Point camera at player
    // =========================================================
    
    private void LookAtTarget()
    {
        // Calculate where to look (player position + offset to look at head)
        Vector3 lookAtPoint = target.position + lookAtOffset;
        
        // CONCEPT: LookAt() rotates this object to face a point
        // Camera will always point at the player's head area
        transform.LookAt(lookAtPoint);
    }
    
    // =========================================================
    // ON DRAW GIZMOS - Draw debug visuals in Scene view
    // =========================================================
    // CONCEPT: Gizmos are visual debugging tools that only show in Scene view
    // They don't appear in the actual game!
    
    void OnDrawGizmos()
    {
        if (!showDebugGizmos || target == null) return;
        
        // Yellow line: from camera to player
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, target.position);
        
        // Green sphere: where camera wants to be
        Gizmos.color = Color.green;
        Vector3 desiredPos = target.position + offset;
        Gizmos.DrawWireSphere(desiredPos, 0.3f);
        
        // Red sphere: where camera is looking
        Gizmos.color = Color.red;
        Vector3 lookAtPoint = target.position + lookAtOffset;
        Gizmos.DrawWireSphere(lookAtPoint, 0.2f);
    }
}
```

---

### 📋 Setup Checklist

After creating both scripts:

1. **PlayerController.cs:**
   - [ ] Create script in `Assets/Scripts/`
   - [ ] Select **Player** GameObject in Hierarchy
   - [ ] Click **Add Component** → search "PlayerController"
   - [ ] Make sure Player also has **CharacterController** component

2. **CameraFollow.cs:**
   - [ ] Create script in `Assets/Scripts/`
   - [ ] Select **Main Camera** in Hierarchy
   - [ ] Click **Add Component** → search "CameraFollow"
   - [ ] Drag **Player** from Hierarchy to the **Target** field

3. **Test:**
   - [ ] Press Play
   - [ ] WASD should move player
   - [ ] Shift should sprint
   - [ ] Camera should smoothly follow

---

## �🔜 Next Week Preview: Week 6

**Next week you'll learn:**
- 3D rotation concepts (Euler angles vs Quaternions)
- Mouse look camera control
- Camera orbit around player
- Vertical angle clamping

**Homework (Optional):**
- Try different walk/sprint speeds - what feels good?
- Try different camera offsets - what looks best?
- Try different smooth speeds - what feels natural?

---

# Week 6: Mouse Look & Camera Orbit

⚠️ **PREREQUISITE:** Complete Week 5 first!

---

## 🎯 What You'll Build This Session

Add mouse control to your camera:
- Move mouse left/right → Camera orbits horizontally around player
- Move mouse up/down → Camera looks up/down
- Vertical limits → Can't flip camera upside down
- Smooth, professional feel

**By the end:** Your camera will feel like a real third-person game!

---

## 📅 Week 6 Structure (60 minutes)

**Today's Goal:** Last week we got our player moving with WASD and a camera that follows from behind. This week, we're adding **mouse control** to the camera so you can look around — just like in real games. You'll learn how 3D rotation works (Euler angles & Quaternions), read mouse input through the Input System, and upgrade your `CameraFollow.cs` to orbit around the player. By the end, moving the mouse will spin the camera around your character, with limits so it can't flip upside down.

| Time | Topic |
|------|-------|
| 0-10 min | Understanding 3D Rotation (Euler vs Quaternions) |
| 10-20 min | Reading Mouse Input |
| 20-35 min | Implementing Mouse Look |
| 35-45 min | Adding Vertical Limits |
| 45-55 min | Integration & Testing |
| 55-60 min | Troubleshooting |

---

## Part 1: Understanding 3D Rotation (10 minutes)

### 🌐 2D vs 3D Rotation

In 2D games, rotation is just **one number** — an angle in degrees (like turning a steering wheel).

In 3D, you need **three numbers** because you can rotate around three axes (X, Y, Z). That's all — just more axes to think about. Unity gives us a simple way to handle it, so let's learn the names.

---

### 📐 Euler Angles - The Human-Friendly Way

**Euler Angles** = Three rotation values (X, Y, Z) in degrees

**What each axis does — try these with your own head right now!**

| Axis | Name | Try This! | What Happens |
|------|------|-----------|-------------|
| X | **Pitch** | Nod your head "yes" | Your head tilts up and down |
| Y | **Yaw** | Shake your head "no" | Your head turns left and right |
| Z | **Roll** | Tilt your head like a confused dog | Your head tilts sideways |

**For our camera, we only care about Pitch and Yaw!** (No Roll — we don't want a tilted camera.)

---

### 🎯 Using Euler Angles in Code

```csharp
// Turn 45 degrees to the right
transform.eulerAngles = new Vector3(0, 45, 0);
//                                  ↑   ↑   ↑
//                               Pitch Yaw Roll

// Look up 30 degrees AND turn right 45 degrees
transform.eulerAngles = new Vector3(-30, 45, 0);
```

**⚠️ One weird thing to memorize:**

Looking **UP** uses a **negative** number. Looking **DOWN** uses a positive number. It's backwards from what you'd expect — that's just how Unity works. Don't worry about *why*, just remember:

```
Pitch = -30  → Looking UP      ↑
Pitch =   0  → Looking straight →
Pitch = +30  → Looking DOWN    ↓
```

**Think of it like this:** positive pitch = your chin drops toward your chest (looking down). Negative = chin goes up (looking up).

---

### 🔮 Quaternions - Unity's Secret Math

Behind the scenes, Unity doesn't actually store rotations as Euler angles. It uses something called **Quaternions** — a math format that prevents weird glitches.

**You do NOT need to understand how Quaternions work.** Just know one thing:

```csharp
// YOU think in degrees (easy for humans):
float pitch = 30f;
float yaw = 45f;

// Then hand it to Unity using this converter:
transform.rotation = Quaternion.Euler(pitch, yaw, 0);
```

`Quaternion.Euler()` is your translator — you give it degrees, it gives Unity what it needs.

---

### 💡 Key Takeaway

```
YOU think in:    degrees (Euler angles)
UNITY needs:     Quaternions

Translator:      Quaternion.Euler(pitch, yaw, 0)
```

That's it! You'll use `Quaternion.Euler()` in the camera script and it'll just work.

---

## Part 2: Reading Mouse Input (10 minutes)

### 🖱️ What is Mouse Delta?

**Mouse Delta** = How much the mouse moved SINCE LAST FRAME

- It's NOT the mouse position on screen
- It's the CHANGE in position
- Perfect for camera rotation!

```
Frame 1: Mouse at (100, 200)
Frame 2: Mouse at (105, 198)
Delta = (5, -2) ← Mouse moved 5 right, 2 down
```

---

### 📝 Step 1: Add "Look" Action to Input Actions

We need to add mouse input to our Input Actions!

1. **In Project window, double-click `PlayerInputActions`**

2. **Make sure "Player" action map is selected** (left column)

3. **Click + next to "Actions"** (middle column)

4. **Name it:** `Look`

5. **In Properties (right column):**
   - Action Type: **Value**
   - Control Type: **Vector2**

6. **Add the mouse binding:**
   - Click + next to "Look" action
   - Select "Add Binding"
   - Click the new binding
   - In Properties, click Path → Mouse → Delta
   - It should show "[Mouse]/delta"

7. **Click "Save Asset"** at the top

**Your actions should now be:**
```
Player (Action Map)
  ├── Move (Vector2) - WASD
  ├── Sprint (Button) - Left Shift  
  └── Look (Vector2) - Mouse Delta    ← NEW!
```

---

### 📝 Step 2: Read Mouse Input in Code

Add this to any script to read mouse movement:

```csharp
void Update()
{
    // Read mouse movement (how much it moved since last frame)
    Vector2 mouseDelta = inputActions.Player.Look.ReadValue<Vector2>();
    
    // mouseDelta.x = horizontal movement (left/right)
    // mouseDelta.y = vertical movement (up/down)
    
    Debug.Log($"Mouse moved: {mouseDelta}");
}
```

---

## Part 3: Implementing Mouse Look (15 minutes)

### 🎯 The Plan

1. Track rotation angles (yaw and pitch)
2. Add mouse movement to angles each frame
3. Convert angles to rotation
4. Apply to camera (and optionally player)

---

### 📝 Step 3: Upgrade CameraFollow Script

Open `CameraFollow.cs` and we'll add mouse look!

> **📌 Note:** This step only upgrades the **camera**. After this, the camera will orbit with the mouse, but WASD movement will still go in the player's own forward direction — not the camera's. That's expected! We'll upgrade `HandleMovement()` in `PlayerController.cs` in **Part 5** to make movement follow the camera.

**Replace your entire CameraFollow.cs with this upgraded version:**

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Week 6: Camera with mouse look and smooth follow
/// Orbits around player based on mouse input
/// </summary>
public class CameraFollow : MonoBehaviour
{
    // ============================================
    // TARGET SETTINGS
    // ============================================
    
    [Header("Target Settings")]
    [Tooltip("The player to follow")]
    public Transform target;
    
    [Tooltip("Offset for look-at point (look at head, not feet)")]
    public Vector3 lookAtOffset = new Vector3(0, 1.5f, 0);
    
    // ============================================
    // CAMERA DISTANCE
    // ============================================
    
    [Header("Camera Distance")]
    [Tooltip("How far camera is behind player")]
    public float distance = 5f;
    
    [Tooltip("How high camera is above player")]
    public float height = 2f;
    
    // ============================================
    // MOUSE LOOK SETTINGS
    // ============================================
    
    [Header("Mouse Look Settings")]
    [Tooltip("How fast camera rotates with mouse (1-10)")]
    [Range(0.5f, 10f)]
    public float mouseSensitivity = 2f;
    
    [Tooltip("Minimum vertical angle (looking up) - can't go above this")]
    [Range(-89f, 0f)]
    public float minPitch = -60f;
    
    [Tooltip("Maximum vertical angle (looking down) - can't go below this")]
    [Range(0f, 89f)]
    public float maxPitch = 60f;
    
    // ============================================
    // SMOOTHING
    // ============================================
    
    [Header("Smoothing")]
    [Tooltip("How quickly camera moves to target position")]
    [Range(1f, 20f)]
    public float smoothSpeed = 10f;
    
    // ============================================
    // PRIVATE VARIABLES
    // ============================================
    
    // Input system
    private PlayerInputActions inputActions;
    
    // Current rotation angles (we track these ourselves)
    private float yaw = 0f;      // Horizontal rotation (left/right)
    private float pitch = 20f;   // Vertical rotation (up/down) - start looking slightly down
    
    // ============================================
    // UNITY LIFECYCLE
    // ============================================
    
    void Awake()
    {
        inputActions = new PlayerInputActions();
    }
    
    void OnEnable()
    {
        inputActions.Player.Enable();
    }
    
    void OnDisable()
    {
        inputActions.Player.Disable();
    }
    
    void Start()
    {
        if (target == null)
        {
            Debug.LogError("CameraFollow: No target assigned! Drag Player to Target field.");
            enabled = false;
            return;
        }
        
        // Lock cursor to center of screen (for TPS controls)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        HandleMouseLook();
        UpdateCameraPosition();
    }
    
    // ============================================
    // MOUSE LOOK
    // ============================================
    
    private void HandleMouseLook()
    {
        // Read mouse movement
        Vector2 mouseDelta = inputActions.Player.Look.ReadValue<Vector2>();
        
        // Apply sensitivity
        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;
        
        // Update rotation angles
        // NOTE: Mouse axis and rotation axis are PERPENDICULAR:
        //   Mouse X (left/right) → Yaw   (rotation around Y axis) — like shaking head "no"
        //   Mouse Y (up/down)    → Pitch  (rotation around X axis) — like nodding head "yes"
        //   Sliding along one axis spins around the OTHER axis!
        yaw += mouseX;         // Horizontal: add mouse X
        pitch -= mouseY;       // Vertical: subtract mouse Y (mouse up = look up = negative pitch)
        
        // Clamp pitch so camera can't flip upside down
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }
    
    // ============================================
    // CAMERA POSITION
    // ============================================
    
    private void UpdateCameraPosition()
    {
        // 1. Create rotation from our angles (this is Quaternion.Euler!)
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        
        // 2. Start with offset pointing backward and up, then rotate it
        //    Think of this as a "selfie stick" attached to the player:
        //      -Z = behind player, +Y = above player (default 3rd person view)
        //    If we didn't push the camera out, it would sit ON the player!
        //
        //    Then rotation * offset "swings the stick" around the player:
        //      yaw = 0°   → camera directly behind
        //      yaw = 90°  → camera to the right
        //      yaw = 180° → camera in front
        //      yaw = 270° → camera to the left
        Vector3 offset = new Vector3(0, height, -distance);
        Vector3 rotatedOffset = rotation * offset;
        
        // 3. Position camera at player + rotated offset
        Vector3 desiredPosition = target.position + rotatedOffset;
        
        // 4. Smooth follow (rubber band style)
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );
        
        // 5. Always look at the player
        Vector3 lookAtPoint = target.position + lookAtOffset;
        transform.LookAt(lookAtPoint);
    }
    
    // ============================================
    // CURSOR HELPERS
    // ============================================
    
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
```

---

### 📝 Step 4: Save and Test

1. **Save the script** (Ctrl+S)

2. **Go back to Unity**

3. **Press Play**

4. **Test mouse look:**
   - Move mouse left/right → Camera orbits around player
   - Move mouse up/down → Camera looks up/down
   - Try looking straight up → Should stop at max angle
   - Try looking straight down → Should stop at min angle
   - ⚠️ WASD still moves in the player's direction, not the camera's — **this is expected for now** (fixed in Part 5)

5. **To exit Play mode and get your cursor back:**
   - Press **Escape** key, OR
   - Press the **Play button** again

---

### 🐛 Troubleshooting Mouse Look

#### Problem: Mouse doesn't move camera

**Check:**
1. Did you add "Look" action to PlayerInputActions?
2. Did you save the Input Actions asset?
3. Is the cursor locked? (should be invisible in Game view)

---

#### Problem: Camera moves too fast/slow

**Fix:** Adjust Mouse Sensitivity in Inspector
- Too fast? Lower to 0.5-1.0
- Too slow? Raise to 3.0-5.0

---

#### Problem: Camera flips upside down

**Check:** Are minPitch and maxPitch set correctly?
- minPitch should be negative (like -60)
- maxPitch should be positive (like 60)

---

#### Problem: Cursor is stuck / can't click

**How to get cursor back:**
- Press Escape while in Play mode
- OR stop Play mode

**To add Escape key to unlock cursor, add this to the script:**
```csharp
void Update()
{
    // Press Escape to unlock cursor
    if (Keyboard.current.escapeKey.wasPressedThisFrame)
    {
        UnlockCursor();
    }
}
```

---

## Part 4: Understanding the Orbit System (10 minutes)

### 🎓 How Does Camera Orbit Work?

Imagine the camera attached to a invisible boom arm around the player:

```
         🎥 Camera (pitch = 0, yaw = 0)
          \
           \  (distance = 5)
            \
             🧍 Player

Mouse move right → yaw increases → boom arm rotates right:

                   🎥 Camera (pitch = 0, yaw = 45)
                  /
                 / (distance = 5)
                /
             🧍 Player
```

**The code that does this:**
```csharp
// Create rotation from our angles
Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

// Start with offset pointing backward
Vector3 offset = new Vector3(0, height, -distance);  // (0, 2, -5)

// Rotate that offset by our rotation
Vector3 rotatedOffset = rotation * offset;

// Position camera there
Vector3 cameraPos = player.position + rotatedOffset;
```

---

### 💡 What Does `rotation * offset` Mean?

When you multiply a Quaternion by a Vector3, it **rotates the vector**!

```csharp
Vector3 offset = new Vector3(0, 0, -5);  // Points backward

// No rotation
Quaternion rotation = Quaternion.Euler(0, 0, 0);
Vector3 result = rotation * offset;  // Still (0, 0, -5)

// 90 degrees right
Quaternion rotation = Quaternion.Euler(0, 90, 0);
Vector3 result = rotation * offset;  // Now (5, 0, 0) - points right!
```

---

> **⚠️ Notice something weird?** After testing Part 4, you'll see the camera orbits around the player with the mouse — great! But WASD movement still goes in the **player's** forward direction, not the **camera's** direction. Press W and the player walks "north" no matter where you point the camera. That's because `HandleMovement()` in `PlayerController.cs` still uses `transform.forward` / `transform.right` (the player's own facing direction) from the Week 5 version. Part 5 below fixes this by switching to `cameraTransform.forward` / `cameraTransform.right` so movement follows the camera.

---

## Part 5: Making Player Face Camera Direction

Right now, the player doesn't turn — only the camera orbits. Try it: rotate the camera to look left, then press W. The player still walks "north" instead of to the left. That feels awful!

### 🔍 Understanding The Problem

Look at your Week 5 `HandleMovement()`:

```csharp
// Week 5 version — movement relative to PLAYER
moveDirection = transform.forward * input.y + transform.right * input.x;
```

`transform.forward` is the **player's** facing direction. It never changes when you move the camera! That's why WASD always moves the same world direction no matter where the camera points.

**The fix:** Replace `transform.forward` / `transform.right` with `cameraTransform.forward` / `cameraTransform.right`. Then pressing W always walks toward where the **camera** is looking — exactly how third-person games work.

We also need:
- A **reference to the camera's Transform** so we can read its direction
- **Flatten** the camera's forward/right to the horizontal plane (remove Y) — otherwise looking down would push the player into the ground!
- **Rotate the player** to face the movement direction (so the character model turns to match)

### 📝 Replace Your Full PlayerController.cs

Replace your **entire** `PlayerController.cs` with this upgraded version. The key changes from Week 5 are:

1. **New field:** `cameraTransform` — reference to the camera
2. **New in Awake():** Auto-find the camera if not assigned in Inspector
3. **Upgraded HandleMovement():** Uses camera directions instead of player directions, adds dead zone, adds player rotation

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Week 5: Basic player movement using Character Controller
/// Handles WASD movement, sprint, and gravity
/// </summary>
public class PlayerController : MonoBehaviour
{
    // =========================================================
    // INSPECTOR FIELDS (Appear in Unity's Inspector panel)
    // =========================================================

    // ★ NEW — Week 6: Camera reference for camera-relative movement
    [Header("Camera Reference")]
    [Tooltip("The camera to use for movement direction (leave empty to auto-find)")]
    public Transform cameraTransform;
    
    [Header("Movement Settings")]
    [Tooltip("Normal walking speed (units per second)")]
    public float walkSpeed = 3f;
    
    [Tooltip("Sprint speed when holding Shift (units per second)")]
    public float sprintSpeed = 6f;
    
    [Header("Gravity Settings")]
    [Tooltip("Downward force when grounded (keeps player stuck to ground)")]
    public float groundedGravity = -2f;
    
    [Header("Debug")]
    [Tooltip("Show debug info in console")]
    public bool showDebugInfo = false;
    
    // =========================================================
    // PRIVATE VARIABLES (Only this script can access these)
    // =========================================================
    
    private CharacterController characterController;
    private PlayerInputActions inputActions;
    
    private float verticalVelocity = 0f;
    private Vector3 moveDirection = Vector3.zero;
    
    // =========================================================
    // AWAKE
    // =========================================================
    
    void Awake()
    {
        // ★ NEW — Week 6: Auto-find camera if not assigned in Inspector
        if (cameraTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                cameraTransform = mainCam.transform;
            }
        }

        characterController = GetComponent<CharacterController>();
        
        if (characterController == null)
        {
            Debug.LogError("PlayerController requires a CharacterController component!");
            enabled = false;
            return;
        }
        
        inputActions = new PlayerInputActions();
    }
    
    // =========================================================
    // ON ENABLE / ON DISABLE
    // =========================================================
    
    void OnEnable()
    {
        inputActions.Player.Enable();
    }
    
    void OnDisable()
    {
        inputActions.Player.Disable();
    }
    
    // =========================================================
    // UPDATE
    // =========================================================
    
    void Update()
    {
        HandleMovement();
        HandleGravity();
        ApplyMovement();
        
        if (showDebugInfo)
        {
            DebugInfo();
        }
    }
    
    // =========================================================
    // ★ UPGRADED — Week 6: Camera-Relative Movement
    // =========================================================
    // Was: moveDirection = transform.forward * input.y + transform.right * input.x;
    // Now: moveDirection = cameraForward * input.y + cameraRight * input.x;
    
    private void HandleMovement()
    {
        Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
        bool isSprinting = inputActions.Player.Sprint.IsPressed();

        // ★ NEW — Dead zone check
        // Gamepad sticks drift slightly when idle (e.g., 0.02, -0.01).
        // Without this, the character would creep in random directions.
        // For keyboard this is less critical (values are 0 or 1), but
        // good practice since the Input System supports both.
        if (input.magnitude < 0.1f)
        {
            moveDirection = Vector3.zero;
            return;
        }

        // ★ CHANGED — Get CAMERA forward/right instead of player's
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        // Flatten to horizontal plane (remove Y component)
        // Why? If the camera is pitched down 45°, its forward vector
        // points into the ground. We only want the horizontal part
        // so the player moves along the ground, not into it.
        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();
        
        // ★ CHANGED — Use camera directions instead of transform directions
        // Week 5: moveDirection = transform.forward * input.y + transform.right * input.x;
        // Week 6: moveDirection = cameraForward * input.y + cameraRight * input.x;
        moveDirection = cameraForward * input.y + cameraRight * input.x;
        
        if (moveDirection.magnitude > 0.1f)
        {
            moveDirection.Normalize();

            // ★ NEW — Rotate player to face movement direction
            // LookRotation: "give me a rotation that faces this direction"
            // Slerp: smoothly interpolate between current and target rotation
            //
            // The third parameter (10f * Time.deltaTime) controls turn speed:
            //   At 60 FPS: 10 * 0.016 = 0.16 → each frame, rotate 16% of
            //   the remaining angle toward the target.
            //
            //   This creates a smooth curve: fast at first (large angle left),
            //   slower as it arrives (small angle left). The result is a turn
            //   that feels quick and responsive, but not an instant jump.
            //
            //   Lower values (e.g. 2f) = slow, floaty turn
            //   Higher values (e.g. 50f) = almost instant snap
            //   10f = a good middle ground — fast but still visibly smooth
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRotation, 10f * Time.deltaTime
            );
        }
        
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
        moveDirection *= currentSpeed;
    }
    
    // =========================================================
    // HANDLE GRAVITY (unchanged from Week 5)
    // =========================================================
    
    private void HandleGravity()
    {
        if (characterController.isGrounded)
        {
            verticalVelocity = groundedGravity;
        }
        else
        {
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
            verticalVelocity = Mathf.Max(verticalVelocity, -50f);
        }
    }
    
    // =========================================================
    // APPLY MOVEMENT (unchanged from Week 5)
    // =========================================================
    
    private void ApplyMovement()
    {
        Vector3 movement = moveDirection * Time.deltaTime;
        movement.y = verticalVelocity * Time.deltaTime;
        characterController.Move(movement);
    }
    
    // =========================================================
    // DEBUG (unchanged from Week 5)
    // =========================================================
    
    private void DebugInfo()
    {
        Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
        Debug.Log($"Input: {input} | Grounded: {characterController.isGrounded} | " +
                  $"Vertical Velocity: {verticalVelocity:F2} | " +
                  $"Speed: {moveDirection.magnitude:F2}");
    }
    
    // =========================================================
    // PUBLIC GETTERS (unchanged from Week 5)
    // =========================================================
    
    public bool IsGrounded => characterController.isGrounded;
    public bool IsMoving => moveDirection.magnitude > 0.1f;
    public float CurrentSpeed => moveDirection.magnitude;
}
```

### What Changed (Summary)

| Area | Week 5 | Week 6 |
|------|--------|--------|
| **Camera ref** | None | `public Transform cameraTransform;` |
| **Awake** | — | Auto-finds `Camera.main` |
| **Move direction** | `transform.forward` / `transform.right` | `cameraForward` / `cameraRight` (flattened) |
| **Dead zone** | None | `if (input.magnitude < 0.1f) return;` |
| **Player rotation** | None | `Quaternion.LookRotation` + `Slerp` |

### ✅ Test It

1. **Play** the scene
2. **Rotate** the camera with the mouse
3. **Press W** — player should now walk toward where the camera is pointing
4. **Move diagonally** — player turns smoothly to face that direction
5. The character model should always face the direction it's moving

---

## ✅ Week 6 Complete!

### What You Built:
- ✅ Mouse look camera controls
- ✅ Camera orbits around player
- ✅ Vertical angle limits (no flipping)
- ✅ Cursor locking
- ✅ Camera-relative player movement (HandleMovement upgrade)

### What You Learned:
- Euler angles (pitch, yaw, roll) for 3D rotation
- Quaternion.Euler() to convert to Unity's rotation format
- Mouse delta for smooth camera control
- Mathf.Clamp() to limit values
- How to orbit a camera around a target

---

## 🔜 Next Week Preview: Week 7

**Next week you'll add:**
- Camera collision detection (doesn't go through walls!)
- Smoother camera movement
- Professional polish

**Concepts you'll learn:**
- Raycasting (shooting invisible rays to detect objects)
- LayerMasks (filtering what to detect)
- SphereCast (smoother collision detection)

---

# Week 7: Camera Collision & Polish

⚠️ **PREREQUISITE:** Complete Weeks 5 and 6 first!

---

## 🎯 What You'll Build This Session

Stop the camera from going through walls:
- Camera pulls forward when blocked by wall
- Smooth transition (not instant snapping)
- Returns to normal distance when clear
- Professional, polished feel

---

## 📅 Week 7 Structure (60 minutes)

| Time | What | Format |
|------|------|--------|
| 0-5 min | See the problem — walk camera into a wall | **Demo + Talk** |
| 5-20 min | Add collision detection (SphereCast) | **Code** |
| 20-30 min | Test with walls, tweak settings | **Experiment** |
| 30-40 min | Add smooth distance transitions | **Code** |
| 40-50 min | Test smooth vs snappy, tune values | **Experiment** |
| 50-60 min | Troubleshooting + homework | **Wrap up** |

---

## Part 1: See the Problem (5 minutes)

### 🎯 What's Wrong Right Now?

Before writing any code, let's see the bug:

1. **Add a wall** to your scene (if you don't have one):
   - GameObject → 3D Object → Cube
   - Scale: (5, 3, 1)
   - Position: (0, 1.5, 5)

2. **Press Play** and walk toward the wall

3. **Rotate the camera** so the wall is between you and the camera

**What happens:** The camera goes *through* the wall. You see the inside of the wall, or nothing at all.

```
    ┌──────────────────────────┐
    │          ROOM            │
    │     🧍 Player            │
    │      ·                   │
    │       ·  (5m back)       │
    ██████████████████████████████  ← WALL
    │          🎥 Camera       │  ← On the WRONG side!
    └──────────────────────────┘
```

**The fix:** Before placing the camera, shoot an invisible sphere from the player toward the camera position. If it hits a wall, shorten the camera distance:

```
    ┌──────────────────────────┐
    │          ROOM            │
    │     🧍 Player            │
    │      ·                   │
    │       🎥 Camera (moved)  │  ← Stopped at the wall!
    ██████████████████████████████  ← SphereCast hit here
    └──────────────────────────┘
```

### 💡 Key Concept: SphereCast

`Physics.SphereCast` shoots a sphere along a line and tells you if it hits a collider. It's like a thick raycast — catches wall edges and corners that a thin ray would miss.

```csharp
// Shoot a sphere from point A in a direction
RaycastHit hit;
bool blocked = Physics.SphereCast(
    origin,       // Where to start
    radius,       // How thick the sphere is
    direction,    // Which way to shoot
    out hit,      // Info about what was hit
    maxDistance,   // How far to check
    layerMask     // Which layers to collide with
);

if (blocked)
{
    // hit.distance = how far before we hit the wall
    // hit.point   = exact position of impact
}
```

That's all you need to know. Let's add it to the camera.

---

## Part 2: Add Collision Detection (15 minutes)

### 📝 Replace Your Full CameraFollow.cs

Replace your **entire** `CameraFollow.cs` with this upgraded version. Changes from Week 6 are marked with ★:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Week 7: Camera with mouse look, orbit, and collision detection
/// Orbits around player, pulls forward when walls block the view
/// </summary>
public class CameraFollow : MonoBehaviour
{
    // ============================================
    // TARGET SETTINGS
    // ============================================
    
    [Header("Target Settings")]
    [Tooltip("The player to follow")]
    public Transform target;
    
    [Tooltip("Offset for look-at point (look at head, not feet)")]
    public Vector3 lookAtOffset = new Vector3(0, 1.5f, 0);
    
    // ============================================
    // CAMERA DISTANCE
    // ============================================
    
    [Header("Camera Distance")]
    [Tooltip("How far camera is behind player")]
    public float distance = 5f;
    
    [Tooltip("How high camera is above player")]
    public float height = 2f;
    
    // ============================================
    // MOUSE LOOK SETTINGS
    // ============================================
    
    [Header("Mouse Look Settings")]
    [Tooltip("How fast camera rotates with mouse (1-10)")]
    [Range(0.5f, 10f)]
    public float mouseSensitivity = 2f;
    
    [Tooltip("Minimum vertical angle (looking up)")]
    [Range(-89f, 0f)]
    public float minPitch = -60f;
    
    [Tooltip("Maximum vertical angle (looking down)")]
    [Range(0f, 89f)]
    public float maxPitch = 60f;
    
    // ============================================
    // ★ NEW — Week 7: Collision Settings
    // ============================================
    
    [Header("Collision Settings")]
    [Tooltip("What layers should block the camera? (walls, ground, etc.)")]
    public LayerMask collisionMask;
    
    [Tooltip("Minimum distance from player when blocked")]
    [Range(0.5f, 2f)]
    public float minDistance = 1f;
    
    [Tooltip("How far to stay from walls (prevents clipping)")]
    [Range(0.1f, 0.5f)]
    public float collisionBuffer = 0.2f;
    
    [Tooltip("Radius of collision sphere (larger = catches corners better)")]
    [Range(0.1f, 0.5f)]
    public float collisionRadius = 0.2f;
    
    // ============================================
    // SMOOTHING
    // ============================================
    
    [Header("Smoothing")]
    [Tooltip("How quickly camera moves to target position")]
    [Range(1f, 20f)]
    public float smoothSpeed = 10f;
    
    // ============================================
    // PRIVATE VARIABLES
    // ============================================
    
    private PlayerInputActions inputActions;
    
    private float yaw = 0f;
    private float pitch = 20f;
    
    // ★ NEW — Week 7: Smooth distance tracking
    private float currentDistance;
    
    // ============================================
    // UNITY LIFECYCLE
    // ============================================
    
    void Awake()
    {
        inputActions = new PlayerInputActions();
    }
    
    void OnEnable()
    {
        inputActions.Player.Enable();
    }
    
    void OnDisable()
    {
        inputActions.Player.Disable();
    }
    
    void Start()
    {
        if (target == null)
        {
            Debug.LogError("CameraFollow: No target assigned! Drag Player to Target field.");
            enabled = false;
            return;
        }
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // ★ NEW — Week 7: Start at full distance
        currentDistance = distance;
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        HandleMouseLook();
        UpdateCameraPosition();
    }
    
    // ============================================
    // MOUSE LOOK (unchanged from Week 6)
    // ============================================
    
    private void HandleMouseLook()
    {
        Vector2 mouseDelta = inputActions.Player.Look.ReadValue<Vector2>();
        
        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;
        
        yaw += mouseX;
        pitch -= mouseY;
        
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }
    
    // ============================================
    // ★ UPGRADED — Week 7: Camera Position with Collision
    // ============================================
    
    private void UpdateCameraPosition()
    {
        // 1. Create rotation from our angles (same as Week 6)
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        
        // 2. Calculate the direction from player to where camera wants to be
        //    offset points backward and up; rotation swings it around the player
        Vector3 offset = new Vector3(0, height, -distance);
        Vector3 direction = (rotation * offset).normalized;
        
        // 3. ★ NEW — Check for walls between player and camera
        Vector3 playerHead = target.position + lookAtOffset;
        float safeDistance = GetCollisionAdjustedDistance(playerHead, direction);
        
        // 4. ★ NEW — Smooth the distance change (no jarring snaps)
        //    When wall appears: camera pulls in quickly
        //    When wall clears: camera eases back out
        currentDistance = Mathf.Lerp(currentDistance, safeDistance, smoothSpeed * Time.deltaTime);
        
        // 5. Position camera at the (possibly shortened) distance
        Vector3 desiredPosition = playerHead + direction * currentDistance;
        
        // 6. Smooth position follow (rubber band)
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );
        
        // 7. Always look at the player
        transform.LookAt(playerHead);
    }
    
    // ============================================
    // ★ NEW — Week 7: Collision Detection
    // ============================================
    
    /// <summary>
    /// Shoots a sphere from the player toward the camera.
    /// If it hits a wall, returns a shorter distance so the camera
    /// stays in front of the wall instead of going through it.
    /// </summary>
    private float GetCollisionAdjustedDistance(Vector3 startPosition, Vector3 direction)
    {
        // Start with the full desired distance
        // (distance² + height² gives the actual length of the offset vector)
        float maxDist = Mathf.Sqrt(distance * distance + height * height);
        float adjustedDistance = maxDist;
        
        RaycastHit hit;
        
        // SphereCast = "thick raycast" — a sphere travels along a line
        // Why not a regular Raycast? A thin ray can slip past wall edges
        // and corners. A sphere has width, so it catches those cases.
        if (Physics.SphereCast(
            startPosition,       // Start at the player's head
            collisionRadius,     // How thick the sphere is
            direction,           // Toward where camera wants to be
            out hit,             // Info about what was hit
            maxDist,             // How far to check
            collisionMask))      // Only hit walls/ground, not the player
        {
            // Wall detected! Shorten distance.
            // Subtract collisionBuffer so camera stays a bit in front of wall
            // (without the buffer, the camera would sit exactly ON the wall
            //  and you'd still see clipping)
            adjustedDistance = hit.distance - collisionBuffer;
            
            // Don't let camera get closer than minDistance
            // (prevents camera ending up inside the player's head)
            adjustedDistance = Mathf.Max(adjustedDistance, minDistance);
        }
        
        return adjustedDistance;
    }
    
    // ============================================
    // CURSOR HELPERS (unchanged from Week 6)
    // ============================================
    
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
```

---

### 📝 Configure Layer Mask in Unity

The camera needs to know **what counts as a wall**. That's what `collisionMask` does — it filters which layers the SphereCast can hit.

1. Select **Main Camera** in Hierarchy
2. Find **CameraFollow → Collision Settings** in Inspector
3. Click the **Collision Mask** dropdown and check:
   - ✅ **Default** (your walls/ground are probably on this layer)
   - ☐ Player (don't collide with the player itself!)

```
Collision Mask:
  ☑️ Default        ← walls, ground, cubes
  ☐ Player          ← skip the player
  ☐ Ignore Raycast  ← skip triggers, UI colliders
```

> **💡 LayerMask explained:** Every GameObject in Unity lives on a "layer" (a label like Default, Player, UI). A LayerMask is just a checkbox filter — the SphereCast only detects objects on checked layers. This lets you ignore the player's own collider so the camera doesn't think the player is a wall.

---

## Part 3: Test Collision (10 minutes)

### ✅ Test It

1. **Make sure you have a wall** (Cube scaled to (5, 3, 1), position (0, 1.5, 5))

2. **Press Play**

3. **Walk toward the wall** and orbit the camera behind you:
   - Camera should pull forward when the wall blocks it
   - Camera should stay in front of the wall, not clip through

4. **Walk away from the wall:**
   - Camera should smoothly return to normal distance

5. **Try tight corners** — orbit the camera into a corner between two walls. The SphereCast should catch both.

### 🎛️ Tweak Settings

Try adjusting these in the Inspector **while Play mode is running** (values reset when you stop):

| Setting | Try This | What Changes |
|---------|----------|-------------|
| Collision Buffer | 0.1 → 0.5 | How much space between camera and wall |
| Min Distance | 0.5 → 2.0 | Closest the camera can get to player |
| Collision Radius | 0.1 → 0.5 | Larger = catches corners better, but pulls in sooner |

When you find values you like, **write them down** and set them again after stopping Play mode (or edit them while NOT in Play mode to keep them).

---

### 🐛 Troubleshooting

#### Camera still goes through walls
1. Is **Collision Mask** set? (check Default is ticked)
2. Does the wall have a **Collider** component? (Box Collider, Mesh Collider, etc.)
3. Is `collisionBuffer > 0`?

#### Camera stays close even without walls
1. Is there an invisible collider in the scene? (check with Edit → Select All, look for hidden objects)
2. Is the **ground** being detected? If so, increase `lookAtOffset.y` so the ray starts above the player, or uncheck the ground's layer in the mask.

#### Camera jitters near walls
- Increase `Smooth Speed` (try 12-15)
- Increase `Collision Buffer` (try 0.3-0.5)

---

## Part 4: Understanding What We Built (10 minutes)

### 🎓 SphereCast vs Raycast

We went straight to SphereCast. Here's why:

```
RAYCAST (thin line):              SPHERECAST (thick sphere):

      Wall edge                        Wall edge
        |                                |
   ──→  |         ← misses!        ⚪→   |        ← catches it!
        |🎥  ← clips corner             |🎥  ← stays outside
```

`Physics.Raycast` is a laser-thin line. It works for open walls but slips past edges and corners. `Physics.SphereCast` is the same concept but with a sphere traveling along the line — it has **width**, so it catches geometry that a thin ray would miss.

> **📌 When to use which:**
> - **Raycast** — shooting bullets, line-of-sight checks, clicking on objects (you want precision)
> - **SphereCast** — camera collision, character grounding, anything where you want tolerance for imprecise geometry

### 🎓 RaycastHit — What Comes Back

When a SphereCast (or Raycast) hits something, Unity fills a `RaycastHit` struct:

| Property | What It Is | How We Use It |
|----------|-----------|---------------|
| `hit.distance` | How far the sphere traveled before hitting | Shorten camera distance to this |
| `hit.point` | Exact world position of impact | Useful for debug drawing |
| `hit.normal` | Direction pointing out of the surface | Useful for bouncing/reflecting |
| `hit.collider` | The Collider component that was hit | Check what we hit |

We only use `hit.distance` — subtract the buffer, clamp to min, and that's our safe camera distance.

### 🎓 Smooth Distance (Mathf.Lerp)

Without smoothing, the camera snaps instantly when a wall appears/disappears. We use the same trick as camera follow — `Mathf.Lerp` on the distance:

```csharp
currentDistance = Mathf.Lerp(currentDistance, safeDistance, smoothSpeed * Time.deltaTime);
```

Each frame, `currentDistance` moves a fraction toward `safeDistance`. Wall appears → distance shrinks smoothly. Wall clears → distance grows back smoothly. Same concept as `Vector3.Lerp` for position, but applied to a single float.

---

## Part 5: Final Polish & Testing (10 minutes)

### 🎨 Recommended Settings

These values give a professional third-person camera feel:

| Setting | Value | Why |
|---------|-------|-----|
| Distance | 5 | Far enough to see the player and surroundings |
| Height | 2 | Slightly above player — natural over-the-shoulder angle |
| Mouse Sensitivity | 2 | Not too twitchy, not too sluggish |
| Min Pitch | -60 | Can look down enough to see the ground |
| Max Pitch | 60 | Can look up enough to see the sky |
| Smooth Speed | 10 | Responsive but not instant |
| Min Distance | 1 | Doesn't jam into the player's head |
| Collision Buffer | 0.3 | Comfortable gap from walls |
| Collision Radius | 0.2 | Catches corners without being too aggressive |

### ✅ Final Test Checklist

Run through all of these:

- [ ] Walk into a wall — camera pulls forward
- [ ] Walk away — camera smoothly returns to full distance
- [ ] Orbit camera into a corner — no clipping
- [ ] Look straight up — angle clamps, no flip
- [ ] Look straight down — angle clamps
- [ ] WASD moves relative to camera (Week 6 Part 5)
- [ ] Sprint works
- [ ] Gravity works (walk off edge)

---

## ✅ Week 7 Complete!

### What You Built:
- ✅ Camera collision with SphereCast
- ✅ Smooth distance transitions
- ✅ LayerMask filtering
- ✅ Professional third-person camera system!

### What You Learned:
- **SphereCast** — shooting a thick sphere to detect walls
- **LayerMask** — filtering what the cast can hit
- **RaycastHit** — reading collision information (distance, point, normal)
- **Mathf.Lerp** on a float — smoothing a single value over time

---

## 🎉 Weeks 5-7 Complete!

**Congratulations!** You've built a complete player movement and camera system!

### Full Feature List:
- ✅ WASD movement with Input System
- ✅ Sprint with Shift
- ✅ Gravity and ground detection
- ✅ Mouse look camera orbit
- ✅ Vertical angle limits
- ✅ Camera-relative player movement
- ✅ Camera collision detection
- ✅ Smooth, professional feel

### This is the foundation for your NPC Shooter game!

**Next up:** Week 8 - Shooting mechanics, bullets, and crosshairs! 🔫

---

## 📚 Complete Script Reference

### Final PlayerController.cs Location
`Assets/Scripts/PlayerController.cs`

### Final CameraFollow.cs Location  
`Assets/Scripts/CameraFollow.cs`

### PlayerInputActions Asset Location
`Assets/PlayerInputActions` (and auto-generated `.cs` file)

---

## 💡 Homework Challenges (Optional)

1. **Add jump:** When Space is pressed, set verticalVelocity to a positive number
2. **Camera shake:** Wiggle the camera when landing from a jump
3. **Sprint FOV:** Increase field of view when sprinting
4. **Crouch:** Lower the camera when pressing Ctrl
5. **Shoulder swap:** Press Q to move camera to other shoulder
