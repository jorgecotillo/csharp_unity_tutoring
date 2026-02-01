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

```csharp
// Magnitude (length) of (1, 0, 1):
magnitude = √(1² + 0² + 1²) = √2 = 1.414
```

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

### 🎯 Lerp for Smooth Following

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
1. Setting up Input Actions
2. Configuring the scene
3. Attaching and configuring scripts

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
1. Is PlayerController script attached to Player?
2. Does PlayerInputActions.cs exist? (Generate C# Class checked?)
3. Did you click in the Game window? (Unity needs focus)
4. Any errors in Console window?

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

## 🔜 Next Week Preview: Week 6

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

### 🌐 Why is Rotation Confusing?

In 2D, rotation is simple: one number (angle in degrees).

In 3D, rotation is complicated:
- THREE axes to rotate around (X, Y, Z)
- Order of rotations matters!
- Multiple ways to represent rotation

Let's break it down simply.

---

### 📐 Euler Angles - The Human-Friendly Way

**Euler Angles** = Three rotation values (X, Y, Z) in degrees

```csharp
// Euler angles are just three numbers
Vector3 rotation = new Vector3(30, 45, 0);
//                              ↑   ↑   ↑
//                              X   Y   Z
```

**What each axis does (imagine a person):**

| Axis | Name | Movement | Human Example |
|------|------|----------|---------------|
| X | **Pitch** | Look up/down | Nodding "yes" |
| Y | **Yaw** | Look left/right | Shaking head "no" |
| Z | **Roll** | Tilt head | Confused dog look |

**For a camera, we only use Pitch (X) and Yaw (Y)!**

---

### 🎯 Practical Example

```csharp
// Look 30 degrees up
transform.eulerAngles = new Vector3(-30, 0, 0);

// Turn 45 degrees right
transform.eulerAngles = new Vector3(0, 45, 0);

// Look up AND turn right
transform.eulerAngles = new Vector3(-30, 45, 0);
```

**Note:** Looking UP is NEGATIVE pitch (counterintuitive!)
- Pitch = -30 → Looking up
- Pitch = 0 → Looking at horizon
- Pitch = +30 → Looking down

---

### 🔮 Quaternions - What Unity Uses Internally

Unity stores rotations as **Quaternions** (4D math objects).

**You don't need to understand the math!** Just know:
- Quaternions avoid weird rotation problems
- Unity handles them automatically
- You convert Euler angles to Quaternions when needed

```csharp
// We think in Euler angles (easy!)
float pitch = 30f;  // Look down 30 degrees
float yaw = 45f;    // Turn right 45 degrees

// Convert to Quaternion when applying
transform.rotation = Quaternion.Euler(pitch, yaw, 0);
```

---

### 💡 Key Takeaway

```
YOU work with: Euler angles (degrees)
UNITY stores: Quaternions

Use Quaternion.Euler() to convert!
```

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

**Replace your entire CameraFollow.cs with this upgraded version:**

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Week 5B: Camera with mouse look and smooth follow
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
    
    [Tooltip("Minimum vertical angle (looking down) - can't go below this")]
    [Range(-89f, 0f)]
    public float minPitch = -60f;
    
    [Tooltip("Maximum vertical angle (looking up) - can't go above this")]
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
        // Create input actions
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
        // Safety check
        if (target == null)
        {
            Debug.LogError("CameraFollow: No target assigned! Drag Player to Target field.");
            enabled = false;
            return;
        }
        
        // Lock cursor to center of screen (for FPS/TPS controls)
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
    
    /// <summary>
    /// Reads mouse input and updates rotation angles
    /// </summary>
    private void HandleMouseLook()
    {
        // Read mouse movement
        Vector2 mouseDelta = inputActions.Player.Look.ReadValue<Vector2>();
        
        // Apply sensitivity (scale the movement)
        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;
        
        // Update our rotation angles
        yaw += mouseX;         // Add horizontal movement to yaw
        pitch -= mouseY;       // Subtract vertical (mouse up = look up = negative pitch)
        
        // Clamp pitch to prevent camera flipping
        // Mathf.Clamp keeps a value between min and max
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }
    
    // ============================================
    // CAMERA POSITION
    // ============================================
    
    /// <summary>
    /// Positions camera behind player based on rotation angles
    /// </summary>
    private void UpdateCameraPosition()
    {
        // 1. Create rotation from our angles
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        
        // 2. Calculate offset position
        //    We start with a vector pointing backward and up
        //    Then rotate it by our rotation
        Vector3 offset = new Vector3(0, height, -distance);
        Vector3 rotatedOffset = rotation * offset;
        
        // 3. Calculate desired camera position
        Vector3 desiredPosition = target.position + rotatedOffset;
        
        // 4. Smoothly move to desired position
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );
        
        // 5. Look at player
        Vector3 lookAtPoint = target.position + lookAtOffset;
        transform.LookAt(lookAtPoint);
    }
    
    // ============================================
    // PUBLIC METHODS
    // ============================================
    
    /// <summary>
    /// Call this to unlock cursor (for menus, pause, etc.)
    /// </summary>
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    /// <summary>
    /// Call this to lock cursor again (when unpausing)
    /// </summary>
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

## Part 5: Making Player Face Camera Direction (Optional)

Right now, the player doesn't turn - only the camera orbits. 

For many games, you want the player to face the direction they're moving (relative to camera).

### 📝 Upgrade PlayerController for Camera-Relative Movement

**Open `PlayerController.cs` and make these changes:**

**1. Add a camera reference at the top:**
```csharp
[Header("Camera Reference")]
[Tooltip("The camera to use for movement direction (leave empty to auto-find)")]
public Transform cameraTransform;
```

**2. Add this to Awake():**
```csharp
// Auto-find camera if not assigned
if (cameraTransform == null)
{
    Camera mainCam = Camera.main;
    if (mainCam != null)
    {
        cameraTransform = mainCam.transform;
    }
}
```

**3. Update HandleMovement() to use camera direction:**
```csharp
private void HandleMovement()
{
    Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
    bool isSprinting = inputActions.Player.Sprint.IsPressed();
    
    if (input.magnitude < 0.1f)
    {
        moveDirection = Vector3.zero;
        return;
    }
    
    // Get camera forward/right (flattened to horizontal plane)
    Vector3 cameraForward = cameraTransform.forward;
    Vector3 cameraRight = cameraTransform.right;
    
    // Remove vertical component (we only want horizontal movement)
    cameraForward.y = 0;
    cameraRight.y = 0;
    cameraForward.Normalize();
    cameraRight.Normalize();
    
    // Calculate movement relative to camera
    moveDirection = cameraForward * input.y + cameraRight * input.x;
    
    if (moveDirection.magnitude > 0.1f)
    {
        moveDirection.Normalize();
        
        // Rotate player to face movement direction
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
    }
    
    float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
    moveDirection *= currentSpeed;
}
```

**Now:**
- W moves toward where camera is looking
- Player turns to face movement direction
- Feels like a real third-person game!

---

## ✅ Week 6 Complete!

### What You Built:
- ✅ Mouse look camera controls
- ✅ Camera orbits around player
- ✅ Vertical angle limits (no flipping)
- ✅ Cursor locking
- ✅ (Optional) Camera-relative movement

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

| Time | Topic |
|------|-------|
| 0-10 min | Understanding Raycasting |
| 10-25 min | Basic Camera Collision |
| 25-40 min | Smoother Collision with SphereCast |
| 40-50 min | Polish & Fine-tuning |
| 50-60 min | Testing & Integration |

---

## Part 1: Understanding Raycasting (10 minutes)

### 🎯 What is a Raycast?

A **Raycast** shoots an invisible ray from one point in a direction and tells you if it hits something.

```
     Start (Player)
         |
         |  ← Ray travels this direction
         |
         ↓
       Wall  ← Ray HIT here!
         
       [X]   ← Camera WOULD be here, but wall blocks it
```

**We use it to:**
1. Shoot ray from player toward camera position
2. If ray hits wall, put camera at hit point
3. If ray hits nothing, camera goes to normal position

---

### 📊 Raycast in Code

```csharp
// The basic raycast
RaycastHit hit;
bool didHit = Physics.Raycast(
    origin,      // Where to start (player position)
    direction,   // Which way to shoot
    out hit,     // Info about what was hit
    distance     // Maximum distance to check
);

if (didHit)
{
    // We hit something!
    Debug.Log("Hit: " + hit.collider.name);
    Debug.Log("Hit distance: " + hit.distance);
    Debug.Log("Hit point: " + hit.point);
}
```

---

### 🎓 RaycastHit - What Did We Hit?

When a raycast hits something, it fills a `RaycastHit` with info:

| Property | What It Is |
|----------|-----------|
| `hit.point` | Exact position where ray hit |
| `hit.distance` | How far from start to hit |
| `hit.normal` | Direction pointing out of surface |
| `hit.collider` | The collider that was hit |
| `hit.transform` | The transform of object hit |

---

## Part 2: Basic Camera Collision (15 minutes)

### 📝 Step 1: Add Collision Detection to Camera

**Open `CameraFollow.cs` and add these fields:**

```csharp
// Add at top with other fields

[Header("Collision Settings")]
[Tooltip("What layers should block the camera? (walls, ground, etc.)")]
public LayerMask collisionMask;

[Tooltip("Minimum distance from player when blocked")]
[Range(0.5f, 2f)]
public float minDistance = 1f;

[Tooltip("How far to stay from walls (prevents clipping)")]
[Range(0.1f, 0.5f)]
public float collisionBuffer = 0.2f;
```

---

### 📝 Step 2: Create Collision Method

Add this method to `CameraFollow.cs`:

```csharp
/// <summary>
/// Checks for walls between player and camera
/// Returns the safe distance (closer if wall is blocking)
/// </summary>
private float GetCollisionAdjustedDistance(Vector3 playerPosition, Vector3 direction)
{
    // Default to full distance
    float adjustedDistance = distance;
    
    // Cast ray from player toward camera
    RaycastHit hit;
    if (Physics.Raycast(playerPosition, direction, out hit, distance, collisionMask))
    {
        // Hit something! Use hit distance minus buffer
        adjustedDistance = hit.distance - collisionBuffer;
        
        // Don't get closer than minimum
        adjustedDistance = Mathf.Max(adjustedDistance, minDistance);
    }
    
    return adjustedDistance;
}
```

---

### 📝 Step 3: Update Camera Position Method

**Replace the `UpdateCameraPosition()` method:**

```csharp
private void UpdateCameraPosition()
{
    // 1. Create rotation from our angles
    Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
    
    // 2. Calculate direction from player to camera
    Vector3 offset = new Vector3(0, height, -distance);
    Vector3 direction = rotation * offset.normalized;
    
    // 3. Check for collision and get adjusted distance
    Vector3 playerHead = target.position + lookAtOffset;
    float adjustedDistance = GetCollisionAdjustedDistance(playerHead, direction);
    
    // 4. Calculate final camera position
    Vector3 finalOffset = direction * adjustedDistance + Vector3.up * height;
    Vector3 desiredPosition = target.position + finalOffset;
    
    // 5. Smoothly move to desired position
    transform.position = Vector3.Lerp(
        transform.position,
        desiredPosition,
        smoothSpeed * Time.deltaTime
    );
    
    // 6. Look at player
    transform.LookAt(playerHead);
}
```

---

### 📝 Step 4: Configure Layer Mask in Unity

Now we need to tell the camera what objects to collide with!

1. **Go back to Unity**

2. **Select Main Camera** in Hierarchy

3. **Find CameraFollow component** in Inspector

4. **Find "Collision Mask" field**
   - It shows a dropdown of layers

5. **Click the dropdown and select:**
   - ✅ Default (or whatever layer your walls/ground use)
   - Make sure Player layer is NOT checked (if you have one)

**Common setup:**
```
Collision Mask:
  ☑️ Default
  ☑️ Environment (if you have this layer)
  ☐ Player
  ☐ Enemies
  ☐ Ignore Raycast
```

---

### 📝 Step 5: Test Collision

1. **Make sure you have a wall** to test with:
   - If not: GameObject → 3D Object → Cube
   - Scale it to (5, 3, 1) to make a wall
   - Position it at (0, 1.5, 5) to put it in front of player

2. **Press Play**

3. **Walk toward the wall**
   - Camera should pull forward when wall blocks it
   - Camera should stay in front of wall, not inside it

4. **Walk away from wall**
   - Camera should return to normal distance

---

### 🐛 Troubleshooting Collision

#### Problem: Camera still goes through walls

**Check:**
1. Is Collision Mask set correctly? (should include Default or wall layer)
2. Does the wall have a Collider component?
3. Is collisionBuffer > 0?

---

#### Problem: Camera stays close even without walls

**Check:**
1. Is there an invisible collider in the scene?
2. Is the ray starting inside the player's collider?

**Fix:** Add offset to ray start point:
```csharp
Vector3 playerHead = target.position + lookAtOffset;  // Start higher up
```

---

## Part 3: Smoother Collision with SphereCast (15 minutes)

### 🎯 Problem with Basic Raycast

Raycast is a single thin line. It can miss corners!

```
      Wall edge
        |
   Ray→ |        ← Ray passes by edge
        |🎥      ← Camera clips through corner!
```

**Solution:** Use SphereCast (a thick ray)!

```
      Wall edge
        |
   ⚪→  |        ← Sphere hits edge
        |🎥      ← Camera stays outside wall!
```

---

### 📝 Upgrade to SphereCast

**Replace `GetCollisionAdjustedDistance()` with:**

```csharp
[Header("Collision Settings")]
public LayerMask collisionMask;

[Range(0.5f, 2f)]
public float minDistance = 1f;

[Range(0.1f, 0.5f)]
public float collisionBuffer = 0.2f;

[Tooltip("Radius of collision check sphere (larger = less clipping)")]
[Range(0.1f, 0.5f)]
public float collisionRadius = 0.2f;

/// <summary>
/// Uses SphereCast for smoother collision detection
/// </summary>
private float GetCollisionAdjustedDistance(Vector3 startPosition, Vector3 direction)
{
    float adjustedDistance = distance;
    
    RaycastHit hit;
    // SphereCast is like a thick raycast - better at catching corners!
    if (Physics.SphereCast(
        startPosition,       // Start point
        collisionRadius,     // Sphere radius
        direction,           // Direction
        out hit,             // Hit info
        distance,            // Max distance
        collisionMask))      // What to hit
    {
        adjustedDistance = hit.distance - collisionBuffer;
        adjustedDistance = Mathf.Max(adjustedDistance, minDistance);
    }
    
    return adjustedDistance;
}
```

---

## Part 4: Polish & Fine-tuning (10 minutes)

### 🎨 Smooth Distance Changes

The camera currently snaps to new distances. Let's make it smooth:

```csharp
// Add this field
private float currentDistance;

void Start()
{
    // ... existing code ...
    currentDistance = distance;  // Initialize
}

private void UpdateCameraPosition()
{
    // ... calculate adjustedDistance ...
    
    // Smooth the distance change
    currentDistance = Mathf.Lerp(currentDistance, adjustedDistance, smoothSpeed * Time.deltaTime);
    
    // Use currentDistance instead of adjustedDistance for position
    Vector3 finalOffset = direction * currentDistance + Vector3.up * height;
    // ... rest of method ...
}
```

---

### 🎨 Recommended Settings

Test these values for a professional feel:

| Setting | Value | What It Does |
|---------|-------|--------------|
| Distance | 5 | Normal camera distance |
| Height | 2 | How high above player |
| Mouse Sensitivity | 2 | How fast camera rotates |
| Min Pitch | -60 | How far you can look down |
| Max Pitch | 60 | How far you can look up |
| Smooth Speed | 10 | How fast camera follows |
| Min Distance | 1 | Closest camera can get |
| Collision Buffer | 0.3 | Space from walls |
| Collision Radius | 0.2 | Size of collision sphere |

---

## ✅ Week 7 Complete!

### What You Built:
- ✅ Camera collision detection
- ✅ Smooth distance adjustment
- ✅ SphereCast for better corner detection
- ✅ Professional third-person camera system!

### What You Learned:
- Raycasting (Physics.Raycast)
- SphereCast for smoother collision
- LayerMasks to filter collision
- RaycastHit to get collision information

---

## 🎉 Weeks 5-7 Complete!

**Congratulations!** You've built a complete player movement and camera system!

### Full Feature List:
- ✅ WASD movement
- ✅ Sprint with Shift
- ✅ Gravity and ground detection
- ✅ Mouse look camera
- ✅ Camera orbit around player
- ✅ Vertical angle limits
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
