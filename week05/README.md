# Week 5: Mouse Look & Camera Polish 🎥

⚠️ **PREREQUISITE:** Complete Week 4 first! This week builds directly on Week 4's player movement.

**Today's Goal:** Add mouse camera control and prevent camera from going through walls!  
**Time:** 1 hour  
**New Concepts:** Quaternions, Mouse Input, Camera Collision, Cinemachine (optional)  
**C# Concepts:** Euler angles, Quaternion.Euler, Raycast for collision, Mathf.Clamp  

---

## 🎯 What You'll Build Today

Upgrade your camera system with:
- Mouse look (move mouse to rotate camera around player!)
- Vertical angle limits (can't flip camera upside down)
- Camera collision detection (doesn't go through walls)
- Smooth, professional-feeling camera

**Success = Camera that feels like a real third-person game!** ✨

---

## 📅 Session Structure (60 minutes)

### Part 1: Understanding Rotations & Quaternions (10 minutes)
### Part 2: Implementing Mouse Look (15 minutes)
### Part 3: Camera Collision Detection (15 minutes)
### Part 4: BREAK - Stand up and move! (5 minutes)
### Part 5: Polish & Integration with Project (10 minutes)
### Part 6: Testing & Wrap-up (5 minutes)

---

## Part 1: Understanding Rotations & Quaternions (10 minutes)

### 🌐 The Rotation Problem

In 3D, rotation is HARD. There are multiple ways to represent rotation:

1. **Euler Angles** (Easy to understand)
2. **Quaternions** (What Unity uses internally)
3. **Rotation Matrices** (Low-level math)

Let's understand each!

---

### 📐 Euler Angles - The Human Way

**Euler Angles** = Three rotations around X, Y, Z axes (in degrees)

```csharp
Vector3 rotation = new Vector3(30, 45, 0);
//                              ↑   ↑   ↑
//                              X   Y   Z
//                           Pitch Yaw Roll
```

**The three rotations:**
- **X rotation (Pitch)** = Looking up/down (nodding your head)
- **Y rotation (Yaw)** = Looking left/right (shaking your head "no")
- **Z rotation (Roll)** = Tilting head side-to-side (confused dog look)

**Example:**
```csharp
// Look 30° up, 45° to the right, no roll
transform.eulerAngles = new Vector3(30, 45, 0);
```

**Pros:**
- ✅ Easy to understand
- ✅ Easy to modify (just add/subtract angles)
- ✅ Human-readable

**Cons:**
- ❌ Gimbal lock (rotations can get stuck)
- ❌ Interpolation problems
- ❌ Order matters (XYZ vs ZYX = different results!)

---

### 🎓 What is Gimbal Lock?

**Gimbal Lock** = When two rotation axes align and you lose a degree of freedom.

Imagine airplane controls:
1. Pitch nose 90° up (pointing straight up)
2. Now yaw and roll do the SAME thing! (You've "lost" a dimension)

This is bad for cameras because it can cause sudden jumps.

---

### 🔮 Quaternions - The Unity Way

**Quaternion** = A 4D mathematical object that represents rotation

```csharp
Quaternion rotation = new Quaternion(x, y, z, w);
//                                   ↑  ↑  ↑  ↑
//                              Complex 4D values
```

**Don't try to understand the math!** Just know:
- Unity uses Quaternions internally
- They avoid gimbal lock
- They interpolate smoothly
- They're faster for calculations

**You work with them like this:**
```csharp
// Create quaternion from Euler angles
Quaternion rotation = Quaternion.Euler(30, 45, 0);

// Apply to transform
transform.rotation = rotation;
```

---

### 🎯 The Practical Workflow

**For camera rotation, we:**
1. Store rotation as Euler angles (easy to modify)
2. Convert to Quaternion when applying (Unity's internal format)

```csharp
// We work with these (easy!)
private float yaw = 0f;      // Horizontal rotation
private float pitch = 0f;    // Vertical rotation

void Update()
{
    // Modify angles based on mouse input
    yaw += mouseX;
    pitch -= mouseY;  // Note: minus because mouse Y is inverted
    
    // Convert to Quaternion and apply
    transform.rotation = Quaternion.Euler(pitch, yaw, 0);
}
```

---

### ⚠️ Rotation Gotchas

**Gotcha #1: Euler angle ranges**
```csharp
// Unity wraps angles to 0-360
transform.eulerAngles = new Vector3(370, 0, 0);
Debug.Log(transform.eulerAngles.x);  // Prints: 10 (not 370!)

// For smooth rotation, keep your own angle variables:
float myAngle = 370;  // Can be any value
transform.eulerAngles = new Vector3(myAngle, 0, 0);  // Unity wraps it
```

**Gotcha #2: Mouse Y is inverted**
```csharp
// Mouse moves UP → mouseY is POSITIVE
// But looking UP → angle should DECREASE
// So we use MINUS:
pitch -= mouseY;  // Not += !
```

**Gotcha #3: Don't modify Quaternion values directly**
```csharp
// ❌ DON'T DO THIS - Makes no sense!
transform.rotation.x += 0.1f;

// ✅ DO THIS - Use Euler or Quaternion.Euler
Vector3 euler = transform.eulerAngles;
euler.x += 10;
transform.eulerAngles = euler;
```

---

## Part 2: Implementing Mouse Look (15 minutes)

### 🎮 The Mouse Look Formula

We need to:
1. Read mouse movement (delta, not position!)
2. Convert to rotation angles
3. Clamp vertical angle (prevent flipping upside down)
4. Apply rotation to camera

---

### 📊 Step 1: Reading Mouse Input

**Mouse Delta** = How much the mouse moved since last frame

```csharp
// Get mouse delta
Vector2 mouseDelta = inputActions.Player.Look.ReadValue<Vector2>();
// Or using old Input system:
Vector2 mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

// mouseDelta.x = horizontal movement (left/right)
// mouseDelta.y = vertical movement (up/down)
```

**Important:** This is MOVEMENT, not position!
- Mouse doesn't move: (0, 0)
- Mouse moves right: (positive, 0)
- Mouse moves left: (negative, 0)
- Mouse moves up: (0, positive)
- Mouse moves down: (0, negative)

---

### 🎚️ Step 2: Mouse Sensitivity

Mouse delta is usually small (like 0.5 to 5 pixels). We need to scale it:

```csharp
[Header("Mouse Settings")]
public float mouseSensitivity = 2f;  // Adjust to taste

void Update()
{
    Vector2 mouseDelta = GetMouseDelta();
    
    // Scale by sensitivity
    float mouseX = mouseDelta.x * mouseSensitivity;
    float mouseY = mouseDelta.y * mouseSensitivity;
}
```

**Typical sensitivity values:**
- 0.5 = Very slow (for precise aiming)
- 2.0 = Default (comfortable)
- 5.0 = Fast (for quick reactions)
- 10.0 = Super fast (maybe too much!)

---

### 🎯 Step 3: Accumulating Rotation

We accumulate rotation over time (not set it directly):

```csharp
private float yaw = 0f;    // Horizontal rotation (around Y axis)
private float pitch = 0f;  // Vertical rotation (around X axis)

void Update()
{
    Vector2 mouseDelta = GetMouseDelta();
    float mouseX = mouseDelta.x * mouseSensitivity;
    float mouseY = mouseDelta.y * mouseSensitivity;
    
    // Accumulate rotation
    yaw += mouseX;      // Add horizontal movement
    pitch -= mouseY;    // Subtract vertical (inverted!)
    
    // Note: yaw can be any value (360°, 720°, -180°, etc.)
    // pitch should be clamped (we'll do this next)
}
```

**Why accumulate?**
```csharp
// BAD - Sets rotation each frame (loses previous rotation)
yaw = mouseX;  // Only looks at THIS frame's input

// GOOD - Accumulates rotation (smooth, continuous)
yaw += mouseX;  // Adds THIS frame's input to total rotation
```

---

### 📏 Step 4: Clamping Vertical Angle

We don't want to look TOO far up or down (prevents flipping camera upside down):

```csharp
[Header("Camera Limits")]
public float minPitch = -80f;  // How far can look down
public float maxPitch = 80f;   // How far can look up

void Update()
{
    // ... accumulate rotation ...
    
    // Clamp pitch to prevent flipping
    pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    
    // yaw is NOT clamped (can spin 360°)
}
```

**Why these values?**
```
pitch = -80° : Looking almost straight down
pitch = 0°   : Looking at horizon
pitch = 80°  : Looking almost straight up
pitch = 90°  : Looking EXACTLY up (can cause gimbal lock!)
```

We use ±80° instead of ±90° to avoid the gimbal lock problem!

---

### 🔄 Step 5: Applying Rotation

Now we convert our Euler angles to Quaternion and apply:

```csharp
void Update()
{
    // ... get mouse input and accumulate rotation ...
    
    // Convert Euler angles to Quaternion
    Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
    
    // Apply to camera transform
    transform.rotation = rotation;
}
```

**Why Quaternion.Euler?**
- We think in Euler angles (easy!)
- Unity uses Quaternions (no gimbal lock!)
- Quaternion.Euler converts between them

---

### 🎮 Step 6: Camera Orbit Around Player

For third-person camera, we want to orbit AROUND the player:

```csharp
void LateUpdate()
{
    if (target == null) return;
    
    // 1. Calculate rotation based on mouse
    Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
    
    // 2. Calculate offset position (rotated around player)
    Vector3 rotatedOffset = rotation * offset;
    //                      ↑         ↑
    //                   rotation  original offset
    
    // 3. Position camera at rotated offset from player
    Vector3 desiredPosition = target.position + rotatedOffset;
    transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    
    // 4. Look at player
    transform.LookAt(target.position + lookAtOffset);
}
```

**What does `rotation * offset` do?**
It rotates the offset vector by the rotation quaternion!

```csharp
// Original offset (behind player)
Vector3 offset = new Vector3(0, 2, -5);

// Rotate by 90° to the right
Quaternion rotation = Quaternion.Euler(0, 90, 0);
Vector3 rotatedOffset = rotation * offset;
// Result: (5, 2, 0) - now to the RIGHT of player!
```

---

### 🎓 Understanding the Full Orbit System

Imagine the camera on a sphere around the player:

```
        🎥 (pitch = +45°)
       /
      /
   Player --- 🎥 (pitch = 0°, yaw = 90°)
      \
       \
        🎥 (pitch = -45°)
```

- **Yaw** rotates around this sphere horizontally (orbit left/right)
- **Pitch** rotates vertically (look up/down on sphere)
- Distance is controlled by offset magnitude

---

## Part 3: Camera Collision Detection (15 minutes)

### 🧱 The Wall Problem

Current problem: Camera goes THROUGH walls!

```
Wall
 |
 | Player   (Camera is inside wall!)
 |  🧍 ← 🎥
 |
```

We need to detect walls and pull camera forward!

---

### 🎯 The Solution: Raycasting

**Raycast** = Shoot invisible ray and check what it hits

```csharp
// Cast ray from player to desired camera position
Ray ray = new Ray(startPoint, direction);
RaycastHit hit;

if (Physics.Raycast(ray, out hit, distance))
{
    // Ray hit something!
    Debug.Log("Hit: " + hit.collider.name);
    Debug.Log("Distance: " + hit.distance);
}
```

**For camera collision:**
1. Cast ray from player to desired camera position
2. If ray hits something, place camera at hit point
3. If ray hits nothing, place camera at desired position

---

### 📊 Step-by-Step Camera Collision

```csharp
void HandleCameraCollision()
{
    if (target == null) return;
    
    // 1. Calculate desired camera position (without collision)
    Vector3 desiredPosition = target.position + rotatedOffset;
    
    // 2. Calculate direction from player to desired position
    Vector3 direction = desiredPosition - target.position;
    float desiredDistance = direction.magnitude;
    
    // 3. Cast ray from player toward camera
    RaycastHit hit;
    if (Physics.Raycast(target.position, direction.normalized, out hit, desiredDistance, collisionMask))
    {
        // Ray hit something! Place camera at hit point (slightly in front)
        float safeDistance = hit.distance - collisionOffset;
        safeDistance = Mathf.Max(safeDistance, minDistance);  // Don't get too close
        
        Vector3 safePosition = target.position + direction.normalized * safeDistance;
        transform.position = safePosition;
    }
    else
    {
        // No collision, use desired position
        transform.position = desiredPosition;
    }
}
```

---

### 🎓 Understanding Raycast Parameters

```csharp
Physics.Raycast(origin, direction, out hit, maxDistance, layerMask);
```

**Parameters:**
1. **origin** (Vector3): Starting point of ray
   - For camera: player position

2. **direction** (Vector3): Direction to shoot ray (MUST be normalized!)
   - For camera: direction from player to camera

3. **out hit** (RaycastHit): Information about what was hit
   - hit.distance = how far away
   - hit.point = exact hit position
   - hit.collider = what we hit

4. **maxDistance** (float): How far to check
   - For camera: distance from player to desired camera position

5. **layerMask** (LayerMask): What layers to check
   - Example: only check walls, ignore player

---

### 🎯 Setting Up Layer Mask

**Why Layer Mask?**
We don't want camera to collide with:
- Player itself
- Enemies
- Bullets
- UI elements

We ONLY want to collide with:
- Walls
- Ground
- Static environment

**Setup in Unity:**
1. Create new layer: "CameraCollision"
2. Assign to walls, ground, etc.
3. In script:

```csharp
[Header("Collision Settings")]
public LayerMask collisionMask;  // Assign in Inspector
public float collisionOffset = 0.2f;  // Buffer distance from wall
public float minDistance = 1f;  // Minimum camera distance

// In Inspector:
// Collision Mask → Check only "CameraCollision" layer
```

---

### 🎓 Understanding Layer Mask

**Layer Mask** = Bit mask that specifies which layers to check

```csharp
// Check only layer 8
LayerMask mask = 1 << 8;

// Check multiple layers (8 and 9)
LayerMask mask = (1 << 8) | (1 << 9);

// Check everything EXCEPT layer 8
LayerMask mask = ~(1 << 8);

// In Inspector: Just check boxes for layers you want!
```

**For our camera:**
```csharp
// In Unity Inspector:
// Collision Mask → ☑ Default ☑ CameraCollision ☐ Player ☐ Enemy
// This checks walls but ignores player/enemies
```

---

### 🎨 Step 3: Smooth Collision Response

Make collision feel smooth, not instant:

```csharp
// Current position
private Vector3 currentPosition;

void HandleCameraCollision()
{
    // ... calculate target position with collision ...
    
    // Smoothly move to target position
    currentPosition = Vector3.Lerp(currentPosition, targetPosition, collisionSmoothSpeed * Time.deltaTime);
    transform.position = currentPosition;
}
```

**Why smooth?**
- Instant snapping looks jarring
- Smooth movement feels professional
- Gives player time to react

---

### ⚠️ Camera Collision Gotchas

**Gotcha #1: Camera clips through thin walls**
```csharp
// Solution: Add collision offset
float safeDistance = hit.distance - 0.2f;  // Pull back slightly
```

**Gotcha #2: Camera gets stuck at close range**
```csharp
// Solution: Set minimum distance
safeDistance = Mathf.Max(safeDistance, minDistance);
```

**Gotcha #3: Ray starts inside player collider**
```csharp
// Solution: Start ray slightly in front of player
Vector3 rayStart = target.position + Vector3.up * 0.5f;
```

**Gotcha #4: Camera jitters when walking along walls**
```csharp
// Solution: Use smooth interpolation + larger collision offset
```

---

## Part 5: Building Your Final Project - Iteration 2 (30 minutes)

### 🎯 Final Project Context: Adding Camera Control

This week, you're upgrading your NPC Shooter with **professional camera controls**!

**What you're adding to your existing project:**
- ✅ Mouse look (orbit camera around player)
- ✅ Vertical angle limits (prevent camera flipping)
- ✅ Camera collision (no wall clipping)
- ✅ Professional third-person feel

**Previous work (Week 4):**
- ✅ Player movement with WASD
- ✅ Sprint system
- ✅ Gravity and ground detection
- ✅ Basic camera follow

**What comes next (future weeks):**
- Week 6-7: Shooting mechanics & bullet spawning
- Week 8-9: NPC enemies & AI behavior
- Week 10-11: Health, damage, & win/lose states

---

### 📋 Implementation Checklist

#### Step 1: Open Your Week 4 Project (1 min)

**Make sure you have:**
- ✅ Player GameObject with CharacterController
- ✅ PlayerController script working (WASD movement)
- ✅ Ground plane
- ✅ Test wall (if you created it)
- ✅ Main Camera with CameraFollow script

**If you're missing anything**, go back to Week 4 and complete Part 6 first!

---

#### Step 2: Upgrade PlayerController with Mouse Look (15 min)

**Open PlayerController.cs:**

Add these new fields at the top:
```csharp
[Header("Mouse Look Settings")]
public float mouseSensitivity = 2f;
public float minPitch = -80f;
public float maxPitch = 80f;

private float cameraPitch = 0f;
private Camera mainCamera;
```

In `Awake()`, cache the camera:
```csharp
void Awake() {
    inputActions = new PlayerInputActions();
    mainCamera = Camera.main;
}
```

Add mouse look to `Update()` (before movement code):
```csharp
void Update() {
    HandleMouseLook();
    HandleMovement();
    HandleGravity();
    ApplyMovement();
}

void HandleMouseLook() {
    // Get mouse input
    Vector2 mouseDelta = inputActions.Player.Look.ReadValue<Vector2>();
    
    // Horizontal rotation (rotate player)
    transform.Rotate(Vector3.up * mouseDelta.x * mouseSensitivity);
    
    // Vertical rotation (rotate camera)
    cameraPitch -= mouseDelta.y * mouseSensitivity;
    cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);
    mainCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0, 0);
}
```

**Update Input Actions:**
1. Open PlayerInputActions asset
2. Add new Action: "Look"
   - Action Type: Value
   - Control Type: Vector2
   - Binding: Mouse → Delta
3. Save Asset

---

#### Step 3: Add Camera Collision Detection (10 min)

**Upgrade CameraFollow.cs:**

Add collision detection fields:
```csharp
[Header("Collision Settings")]
public LayerMask collisionMask;
public float collisionOffset = 0.3f;
public float minDistance = 1f;
```

Modify `LateUpdate()` to include raycasting:
```csharp
void LateUpdate() {
    if (target == null) return;
    
    // Calculate desired position
    Vector3 desiredPosition = target.position + offset;
    
    // Check for obstacles
    Vector3 direction = desiredPosition - target.position;
    float distance = direction.magnitude;
    
    RaycastHit hit;
    if (Physics.Raycast(target.position, direction.normalized, out hit, distance, collisionMask)) {
        // Hit something! Move camera closer
        float adjustedDistance = Mathf.Max(hit.distance - collisionOffset, minDistance);
        desiredPosition = target.position + direction.normalized * adjustedDistance;
    }
    
    // Smooth movement
    Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    transform.position = smoothedPosition;
    
    // Look at player
    transform.LookAt(target);
}
```

**Configure collision mask:**
1. Select Main Camera
2. In CameraFollow component, set Collision Mask to "Default" (walls/ground)
3. Set Collision Offset: 0.3
4. Set Min Distance: 1

---

#### Step 4: Make Camera a Child of Player (2 min)

**Important for mouse look to work correctly:**

1. In Hierarchy, **drag Main Camera onto Player GameObject**
   - This makes camera a child of player
   - Camera now inherits player's rotation

2. **Reset camera's local position:**
   - Select Main Camera
   - In Inspector, click gear icon → Reset (on Transform component)
   - This resets to (0, 0, 0) local position

3. **Set camera's starting local position:**
   - Position: (0, 1.6, 0) - at player's head height
   - Rotation: (0, 0, 0)

4. **Update CameraFollow offset:**
   - Now that camera is a child, the offset is relative to player
   - Try offset: (0, 0.4, -3) for over-shoulder view

---

#### Step 5: Test Your Complete System (2 min)

### 📋 Testing Checklist

**Test mouse look:**
1. Move mouse left/right → camera orbits horizontally
2. Move mouse up/down → camera tilts up/down
3. Try looking straight up → stops at ~80°
4. Try looking straight down → stops at ~80°

**Test collision:**
1. Walk toward wall → camera pulls forward
2. Back away from wall → camera returns to normal distance
3. Run along wall → camera stays smooth
4. Look around corners → camera adjusts smoothly

**Adjust settings:**
- Mouse Sensitivity: 1-5 (find what feels good)
- Pitch limits: -80 to 80 (prevents flipping)
- Collision offset: 0.2-0.5 (prevents clipping)
- Min distance: 1-2 (prevents getting too close)

---

## Part 5: Integration with Final Project (10 minutes)

### 🎮 Player + Camera System Complete!

You now have:
- Week 4: Player movement (WASD, sprint, gravity)
- Week 5: Camera system (mouse look, collision)

**This is the foundation of our shooter game!**

---

### 🎯 What's Next?

Next week (Week 6-7), we'll add:
- Shooting mechanics
- Bullet spawning
- Hit detection
- Crosshair

**Important:** Keep your player and camera scripts! We'll build on them for the rest of the project.

---

### 📝 Optimization Tips

**For better performance:**

1. **Cache components:**
```csharp
// ❌ BAD - Gets component every frame
transform.position = ...;

// ✅ GOOD - Cache in Start
private Transform myTransform;
void Start() { myTransform = transform; }
void Update() { myTransform.position = ...; }
```

2. **Use LayerMask properly:**
```csharp
// Only check specific layers, not everything
public LayerMask collisionMask;
```

3. **Limit raycast distance:**
```csharp
// Don't check infinitely far
float maxDistance = offset.magnitude + 1f;
```

---

## 🎯 Today's Achievements

By the end of Week 5, you have:
- ✅ Understood Euler angles vs Quaternions
- ✅ Implemented mouse-controlled camera orbit
- ✅ Added vertical angle clamping
- ✅ Implemented camera collision detection
- ✅ Created a professional-feeling third-person camera
- ✅ Completed the player controller foundation

**You now have a movement and camera system worthy of a commercial game!** 🎊

---

## 💡 Advanced Challenges (Optional)

1. **Camera shake:** Add subtle shake when player lands from a jump
2. **Dynamic FOV:** Increase field of view when sprinting
3. **Camera zones:** Different camera angles in different areas
4. **Shoulder swap:** Press a key to move camera to other shoulder
5. **Zoom:** Mouse scroll wheel to zoom in/out

---

## 📝 Homework (Optional)

1. Tweak mouse sensitivity until it feels perfect
2. Try different min/max pitch values - what feels natural?
3. Add a crosshair UI element (simple UI image at screen center)
4. Experiment with different camera offsets

---

## 🎊 Weeks 4-5 Complete!

**Congratulations!** You've finished the player movement foundation!

📋 **For a complete overview of what you learned:**
- See `COMPLETE_SUMMARY.md` in this folder
- Quick reference: `QUICK_START_GUIDE.md`

**Next week: We start shooting! Week 6-7 is where the action begins!** 🔫🚀
