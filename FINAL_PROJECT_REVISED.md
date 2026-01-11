# 🎮 FINAL PROJECT REVISED: NPC Shooter Game
## Weekly Learning Plan for 1-Hour Sunday Sessions

---

## 📊 Current Status (Week 5)
**Where We Are**: Completed Weeks 1-3 foundational content  
**Next Session**: Week 4 (Tomorrow - Sunday)  
**Session Format**: 30 min teaching + 30 min hands-on implementation  
**Current Implementation**: Full game systems exist in codebase but need to be understood and built progressively

---

## ✅ What We've Learned So Far (Weeks 1-3)

### Week 1: Unity Fundamentals ✅
**Concepts Covered:**
- Unity interface and Editor basics
- GameObjects and Components
- MonoBehaviour lifecycle (Start, Update)
- Variables and basic C#
- Time.deltaTime for frame-rate independence
- Color manipulation

**Skills Acquired:**
- Navigate Unity Editor
- Understand component-based architecture
- Write basic C# scripts
- Use Update() for frame-dependent logic

---

### Week 2: Input & Transforms ✅
**Concepts Covered:**
- Unity's New Input System
- Keyboard input handling
- Vector3 and transform manipulation
- Conditional statements (if/else)
- Clamping values
- Scale manipulation

**Skills Acquired:**
- Handle player input
- Move and scale objects via code
- Use Vector3 for positions
- Implement basic game logic

---

### Week 3: Physics & Forces ✅
**Concepts Covered:**
- Rigidbody component
- Physics simulation basics
- Velocity vs Forces
- FixedUpdate for physics
- ForceMode types
- Custom gravity implementation
- Vector math and directions
- GetComponent<>()
- Raycasting for ground detection

**Skills Acquired:**
- Apply physics to GameObjects
- Use Rigidbody for movement
- Understand physics update loop
- Implement raycasting
- Work with Vector3 math

---

## 📅 WEEKLY BREAKDOWN (30 Min Teaching + 30 Min Building)

---

## 🚀 WEEK 4: Character Controller & Basic Movement
**Date**: Tomorrow (Sunday Session)  
**Goal**: Get player moving with keyboard controls

### 📖 Teaching Block (30 minutes)

**Topics to Cover:**
1. **CharacterController vs Rigidbody** (5 min)
   - When to use each
   - CharacterController benefits (no physics jitter, built-in collision)
   - Why we're switching from Rigidbody

2. **Input System Review** (5 min)
   - Quick recap of Input System from Week 2
   - PlayerInput component
   - Input Actions and Action Maps

3. **Transform.Translate vs CharacterController.Move** (10 min)
   - Difference between methods
   - Why CharacterController.Move is better for character movement
   - Collision detection with CharacterController

4. **Velocity and Speed** (10 min)
   - Calculating movement direction from input
   - Speed variables (walk, sprint)
   - Time.deltaTime for smooth movement

**Key Code Concepts:**
```csharp
// Getting input direction
Vector2 input = moveAction.ReadValue<Vector2>();
Vector3 direction = new Vector3(input.x, 0, input.y);

// Moving the character
controller.Move(direction * speed * Time.deltaTime);
```

### 🔨 Building Block (30 minutes)

**What We'll Build:**
1. Add CharacterController component to player
2. Create basic PlayerController script
3. Implement WASD movement
4. Add sprint functionality (Shift key)
5. Test and adjust movement speed

**Deliverable:**
- Player capsule that moves with WASD
- Sprint works with Shift
- Movement feels responsive

**Files to Create/Modify:**
- `PlayerController.cs` (basic version - just movement)

---

## 📅 WEEK 5: Gravity & Ground Detection
**Goal**: Make movement feel grounded and realistic

### 📖 Teaching Block (30 minutes)

**Topics to Cover:**
1. **Gravity Concepts** (10 min)
   - How gravity works in real life vs games
   - Gravity as constant acceleration
   - CharacterController doesn't auto-handle gravity

2. **Raycasting Review** (10 min)
   - Review raycasting from Week 3
   - Using raycast to detect ground
   - LayerMasks for targeted raycasting

3. **Velocity Accumulation** (10 min)
   - Building up downward velocity
   - Resetting velocity when grounded
   - Smooth falling vs instant snap

**Key Code Concepts:**
```csharp
// Apply gravity
if (!isGrounded) {
    verticalVelocity += gravity * Time.deltaTime;
} else {
    verticalVelocity = -2f; // Small negative to stay grounded
}

// Check if grounded with raycast
isGrounded = Physics.Raycast(transform.position, Vector3.down, groundDistance);
```

### 🔨 Building Block (30 minutes)

**What We'll Build:**
1. Add ground detection raycast
2. Implement gravity system
3. Add isGrounded variable
4. Fine-tune gravity strength
5. Test on platforms of different heights

**Deliverable:**
- Player falls realistically
- Proper ground detection
- No floating or jittering

**Files to Modify:**
- `PlayerController.cs` (add gravity and ground check)

---

## 📅 WEEK 6: Mouse Look & Camera Basics
**Goal**: Look around with the mouse

### 📖 Teaching Block (30 minutes)

**Topics to Cover:**
1. **Quaternions and Rotation** (10 min)
   - What quaternions are (don't go too deep!)
   - Euler angles vs Quaternions
   - Quaternion.Euler() for converting

2. **Mouse Input** (10 min)
   - Mouse delta (movement amount)
   - Sensitivity settings
   - X-axis for horizontal, Y-axis for vertical

3. **Camera Rotation Clamping** (10 min)
   - Preventing camera from flipping over
   - Mathf.Clamp for angle limits
   - Separate horizontal (player) and vertical (camera) rotation

**Key Code Concepts:**
```csharp
// Get mouse input
Vector2 look = lookAction.ReadValue<Vector2>();

// Rotate player horizontally
transform.Rotate(Vector3.up * look.x * sensitivity);

// Rotate camera vertically (with clamping)
cameraPitch -= look.y * sensitivity;
cameraPitch = Mathf.Clamp(cameraPitch, -80f, 80f);
camera.transform.localRotation = Quaternion.Euler(cameraPitch, 0, 0);
```

### 🔨 Building Block (30 minutes)

**What We'll Build:**
1. Add Camera as child of player
2. Get mouse input from Input System
3. Rotate player left/right
4. Rotate camera up/down
5. Clamp vertical rotation

**Deliverable:**
- Mouse look working smoothly
- Can't flip camera upside down
- Camera follows player rotation

**Files to Modify:**
- `PlayerController.cs` (add mouse look)

---

## 📅 WEEK 7: Third-Person Camera
**Goal**: Camera follows behind player at a distance

### 📖 Teaching Block (30 minutes)

**Topics to Cover:**
1. **Camera Positioning** (10 min)
   - Camera offset from player
   - Following vs parenting
   - Smooth following with Lerp

2. **Vector3.Lerp** (10 min)
   - Linear interpolation explained
   - Smooth camera movement
   - Lerp for both position and rotation

3. **Camera as Separate GameObject** (10 min)
   - Why separate camera from player
   - Creating empty parent for camera pivot
   - Camera arm / camera rig concept

**Key Code Concepts:**
```csharp
// Calculate desired position
Vector3 desiredPosition = player.position - player.forward * distance + Vector3.up * height;

// Smoothly move camera
transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * smoothSpeed);
```

### 🔨 Building Block (30 minutes)

**What We'll Build:**
1. Create ThirdPersonCamera script
2. Separate camera from player hierarchy
3. Implement camera follow logic
4. Add height and distance variables
5. Add smoothing with Lerp

**Deliverable:**
- Camera follows player smoothly
- Can adjust camera distance and height
- No jarring movement

**Files to Create:**
- `ThirdPersonCamera.cs` (basic follow camera)

---

## 📅 WEEK 8: Camera Collision Prevention
**Goal**: Camera doesn't clip through walls

### 📖 Teaching Block (30 minutes)

**Topics to Cover:**
1. **Camera Collision Problem** (5 min)
   - Show the issue: camera going through walls
   - Why this feels bad

2. **Raycasting for Camera** (15 min)
   - Cast ray from player to camera
   - Detect obstacles in between
   - Adjust camera position if hit

3. **LayerMask Advanced** (10 min)
   - Ignoring certain layers
   - Why we don't want to hit the player
   - Setting up proper layer filtering

**Key Code Concepts:**
```csharp
// Raycast from player to desired camera position
if (Physics.Raycast(player.position, direction, out hit, distance, obstacleLayer)) {
    // Obstacle hit! Move camera closer
    currentDistance = hit.distance;
} else {
    // No obstacle, use full distance
    currentDistance = distance;
}
```

### 🔨 Building Block (30 minutes)

**What We'll Build:**
1. Add raycast from player to camera
2. Adjust camera distance when hitting walls
3. Add collision layer mask
4. Test in environment with walls
5. Smooth transition when camera moves

**Deliverable:**
- Camera pushes forward when near walls
- Pulls back when moving away from walls
- No clipping through geometry

**Files to Modify:**
- `ThirdPersonCamera.cs` (add collision detection)

---

## 📅 WEEK 9: Raycasting for Shooting (Hitscan Basics)
**Goal**: Point and shoot with instant hit detection

### 📖 Teaching Block (30 minutes)

**Topics to Cover:**
1. **Hitscan vs Projectile** (10 min)
   - Instant hit (raycast) vs physical bullet
   - When to use each
   - Trade-offs of each method

2. **Shooting Raycast** (15 min)
   - Raycast from camera forward
   - Detecting what we hit
   - RaycastHit information (point, normal, collider)

3. **Mouse Button Input** (5 min)
   - Detecting Fire action
   - Single shot vs hold to shoot
   - Checking button vs reading value

**Key Code Concepts:**
```csharp
// Shoot when fire button pressed
if (fireAction.triggered) {
    Ray ray = camera.ScreenPointToRay(new Vector3(Screen.width/2, Screen.height/2));
    if (Physics.Raycast(ray, out RaycastHit hit, range)) {
        Debug.Log("Hit: " + hit.collider.name);
        // Deal damage to hit.collider
    }
}
```

### 🔨 Building Block (30 minutes)

**What We'll Build:**
1. Create PlayerShooting script
2. Add Fire input action
3. Implement raycast shooting
4. Visual feedback (Debug.DrawRay)
5. Test hitting objects in scene

**Deliverable:**
- Can shoot by clicking
- Raycast detects hit objects
- Console shows what we hit

**Files to Create:**
- `PlayerShooting.cs` (basic shooting)

---

## 📅 WEEK 10: Simple Crosshair UI
**Goal**: Add visual aiming reticle

### 📖 Teaching Block (30 minutes)

**Topics to Cover:**
1. **Unity UI Basics** (15 min)
   - Canvas and EventSystem
   - Screen Space vs World Space
   - UI Image component

2. **Anchors and Pivots** (10 min)
   - Center anchoring for crosshair
   - Making UI scale with screen resolution
   - RectTransform basics

3. **Simple UI Scripting** (5 min)
   - Accessing UI Image
   - Changing color/sprite

**Key Concepts:**
- Canvas Component
- UI Image for simple crosshair (use a +)
- Centered anchors

### 🔨 Building Block (30 minutes)

**What We'll Build:**
1. Create Canvas
2. Add Image for crosshair (simple + shape)
3. Center it on screen
4. Adjust size and color
5. Make it visible but not obtrusive

**Deliverable:**
- Crosshair visible at screen center
- Scales properly with resolution
- Clear visual indicator of aim point

**Files to Create:**
- None (Unity UI setup only)
- Optional: `CrosshairUI.cs` for future dynamic crosshair

---

## 📅 WEEK 11: IDamageable Interface
**Goal**: Create system for anything that can take damage

### 📖 Teaching Block (30 minutes)

**Topics to Cover:**
1. **Interfaces in C#** (15 min)
   - What interfaces are (contract)
   - Why we use them
   - Implementing an interface

2. **Polymorphism Basics** (10 min)
   - Treating different objects the same way
   - "If it can take damage, it implements IDamageable"
   - Checking for interface with GetComponent

3. **Health System Design** (5 min)
   - Separating interface from implementation
   - IDamageable defines TakeDamage()
   - Health class implements it

**Key Code Concepts:**
```csharp
// Interface definition
public interface IDamageable {
    void TakeDamage(float damage, Vector3 hitPoint);
}

// Using the interface
IDamageable damageable = hit.collider.GetComponent<IDamageable>();
if (damageable != null) {
    damageable.TakeDamage(10f, hit.point);
}
```

### 🔨 Building Block (30 minutes)

**What We'll Build:**
1. Create IDamageable interface
2. Create simple Health script that implements it
3. Add Health to some test objects
4. Modify shooting to check for IDamageable
5. Deal damage when shooting objects with Health

**Deliverable:**
- IDamageable interface created
- Basic Health script working
- Shooting deals damage to health objects
- Console logs damage

**Files to Create:**
- `IDamageable.cs` (interface)
- `Health.cs` (basic implementation)

**Files to Modify:**
- `PlayerShooting.cs` (check for IDamageable)

---

## 📅 WEEK 12: Health System - Current/Max HP
**Goal**: Track health values and death

### 📖 Teaching Block (30 minutes)

**Topics to Cover:**
1. **Health Management** (10 min)
   - Current HP vs Max HP
   - Damage reduces current
   - Death when current reaches 0

2. **Properties in C#** (10 min)
   - Getters and setters
   - Encapsulation benefits
   - Read-only properties

3. **Events Introduction** (10 min)
   - What events are
   - Why we use them (loose coupling)
   - Simple UnityEvent setup

**Key Code Concepts:**
```csharp
public class Health : MonoBehaviour, IDamageable {
    public float maxHealth = 100f;
    private float currentHealth;
    
    void Start() {
        currentHealth = maxHealth;
    }
    
    public void TakeDamage(float damage, Vector3 hitPoint) {
        currentHealth -= damage;
        if (currentHealth <= 0) {
            Die();
        }
    }
    
    void Die() {
        Debug.Log(gameObject.name + " died!");
        Destroy(gameObject);
    }
}
```

### 🔨 Building Block (30 minutes)

**What We'll Build:**
1. Add maxHealth and currentHealth to Health script
2. Initialize health in Start()
3. Implement Die() method
4. Destroy object when health reaches 0
5. Test by shooting objects until they die

**Deliverable:**
- Objects track health properly
- Objects destroyed at 0 health
- Can set different max health per object

**Files to Modify:**
- `Health.cs` (add max/current health and death)

---

## 📅 WEEK 13: Simple Enemy Target
**Goal**: Create a stationary enemy to shoot at

### 📖 Teaching Block (30 minutes)

**Topics to Cover:**
1. **Enemy GameObject Setup** (10 min)
   - Using primitives (cube/capsule)
   - Adding Health component
   - Tagging as "Enemy"

2. **Visual Feedback** (10 min)
   - Material color change on hit
   - Using GetComponent<Renderer>()
   - Changing material color

3. **Enemy Prefabs** (10 min)
   - What prefabs are
   - Creating prefab from GameObject
   - Prefab benefits (reusability)

**Key Concepts:**
- Prefabs for reusable enemies
- Materials and colors
- GameObject setup

### 🔨 Building Block (30 minutes)

**What We'll Build:**
1. Create enemy GameObject (red capsule)
2. Add Health component
3. Tag as "Enemy"
4. Add material color change on damage
5. Create prefab from enemy

**Deliverable:**
- Red enemy capsule in scene
- Takes damage when shot
- Flashes when hit
- Can be duplicated easily with prefab

**Files to Modify:**
- `Health.cs` (add optional visual feedback)

---

## 📅 WEEK 14: Humanoid Character Setup (Mixamo Import)
**Goal**: Replace primitives with real 3D character

### 📖 Teaching Block (30 minutes)

**Topics to Cover:**
1. **3D Model Basics** (10 min)
   - FBX file format
   - Meshes, materials, and textures
   - Importing from Mixamo (free site)

2. **Humanoid Rig** (15 min)
   - What rigging is (skeleton)
   - Unity's Humanoid avatar system
   - Why we need rigged characters for animation

3. **Model Import Settings** (5 min)
   - Rig tab settings
   - Animation Type: Humanoid
   - Avatar Definition: Create From This Model

**Key Concepts:**
- 3D model files (.fbx)
- Rigging and avatars
- Mixamo for free character downloads

### 🔨 Building Block (30 minutes)

**What We'll Build:**
1. Download character from Mixamo.com
2. Import FBX into Unity
3. Configure import settings (Rig → Humanoid)
4. Replace player capsule with character model
5. Adjust CharacterController size to fit model

**Deliverable:**
- Humanoid character in project
- Properly configured as Humanoid rig
- Character replaces primitive capsule
- Movement still works correctly

**Files to Modify:**
- None (Unity import settings only)

---

## 📅 WEEK 15: Animation Clips Setup
**Goal**: Import and configure animation clips from Mixamo

### 📖 Teaching Block (30 minutes)

**Topics to Cover:**
1. **Animation Clips** (10 min)
   - What animation clips are
   - Downloading animations from Mixamo
   - Animation for same character (must match rig)

2. **Animation Import Settings** (15 min)
   - Animation tab in import settings
   - Loop Time for continuous animations
   - Avatar source (copy from character)

3. **Which Animations We Need** (5 min)
   - Idle (standing still)
   - Walk (normal movement)
   - Run (sprint)
   - Shoot (firing weapon)

**Key Concepts:**
- Animation clips separate from character
- Loop Time for repeating animations
- Avatar compatibility

### 🔨 Building Block (30 minutes)

**What We'll Build:**
1. Download animations from Mixamo:
   - Rifle Aiming Idle
   - Jogging
   - Run Forward
   - Firing Rifle
2. Import FBX files into Unity
3. Configure each animation:
   - Set Rig to Humanoid
   - Copy Avatar from character
   - Enable Loop Time (except Firing)
4. Verify animations play in preview

**Deliverable:**
- 4 animation clips imported
- All configured with correct settings
- Loop Time enabled where appropriate
- Animations preview correctly

**Files to Create:**
- None (Unity import settings only)

---

## 📅 WEEK 16: Animator Controller Basics
**Goal**: Create animation controller and add states

### 📖 Teaching Block (30 minutes)

**Topics to Cover:**
1. **Animator Controller** (10 min)
   - What it is (state machine for animations)
   - Creating animator controller asset
   - Animator vs Animation component

2. **Animation States** (15 min)
   - State = one animation playing
   - Entry state (default)
   - Creating states by dragging clips

3. **Animator Component** (5 min)
   - Adding to character
   - Assigning controller
   - How it plays animations

**Key Code Concepts:**
```
Animator window:
- States (boxes)
- Transitions (arrows)
- Parameters (variables to control transitions)
```

### 🔨 Building Block (30 minutes)

**What We'll Build:**
1. Create Animator Controller asset
2. Open Animator window
3. Drag animation clips to create states:
   - Idle state
   - Walk state
   - Run state
   - Shoot state
4. Add Animator component to character
5. Assign our controller

**Deliverable:**
- Animator Controller created
- 4 animation states added
- Animator component on character
- Idle animation plays by default

**Files to Create:**
- `PlayerAnimator.controller` (Animator Controller asset)

---

## 📅 WEEK 17: Animation Parameters & Transitions
**Goal**: Control which animation plays based on movement

### 📖 Teaching Block (30 minutes)

**Topics to Cover:**
1. **Parameters** (10 min)
   - Variables in animator (Float, Int, Bool, Trigger)
   - Speed parameter (Float) for movement speed
   - IsMoving parameter (Bool) for idle/moving

2. **Transitions** (15 min)
   - Arrows connecting states
   - Conditions for transitioning
   - Transition duration and offset
   - **Has Exit Time** setting (CRITICAL!)
     - OFF = immediate transition (use for movement)
     - ON = wait for animation to finish (use for actions)

3. **Setting Parameters from Code** (5 min)
   - GetComponent<Animator>()
   - SetFloat(), SetBool(), SetTrigger()

**Key Code Concepts:**
```csharp
Animator animator = GetComponent<Animator>();
animator.SetFloat("Speed", currentSpeed);
animator.SetBool("IsMoving", isMoving);
```

### 🔨 Building Block (30 minutes)

**What We'll Build:**
1. Add parameters to animator:
   - Speed (Float)
   - IsMoving (Bool)
2. Create transitions:
   - Idle → Walk (when IsMoving = true) - **UNCHECK "Has Exit Time"**
   - Walk → Idle (when IsMoving = false) - **UNCHECK "Has Exit Time"**
   - Walk → Run (when Speed > 5) - **UNCHECK "Has Exit Time"**
   - Run → Walk (when Speed ≤ 5) - **UNCHECK "Has Exit Time"**
3. **Set Idle as default state** (right-click → Set as Layer Default State)
4. Modify PlayerController to set parameters
5. Test animations changing with movement

**Deliverable:**
- Parameters created in animator
- Transitions with correct conditions
- Animations change based on movement speed
- Character walks/runs appropriately

**Common Pitfalls:**
- ❌ Forgetting to uncheck "Has Exit Time" → causes sliding/delayed animations
- ❌ Wrong default state → character starts in wrong pose
- ❌ Missing conditions on transitions → animations don't change
- ✅ Solution: Always uncheck "Has Exit Time" for movement, set Idle as default

**Files to Modify:**
- `PlayerController.cs` (set animator parameters)
- `PlayerAnimator.controller` (add transitions)

---

## 📅 WEEK 18: Blend Trees for Smooth Movement
**Goal**: Smoothly blend between idle/walk/run

### 📖 Teaching Block (30 minutes)

**Topics to Cover:**
1. **What are Blend Trees** (15 min)
   - Blending multiple animations
   - Based on parameter value
   - Smooth transitions vs hard cuts

2. **1D Blend Tree** (10 min)
   - One parameter controls blending
   - Threshold values (0 = idle, 3 = walk, 8 = run)
   - Linear blending between

3. **When to Use Blend Trees** (5 min)
   - Better for continuous parameter (Speed)
   - More natural looking transitions
   - Removes need for many transitions

**Key Concepts:**
- Blend Tree node
- Motion fields with thresholds
- Parameter-driven blending

### 🔨 Building Block (30 minutes)

**What We'll Build:**
1. Create new Blend Tree state (replace manual transitions)
2. Set parameter to Speed
3. Add motion fields:
   - 0.0 → Idle
   - 3.0 → Walk
   - 8.0 → Run
4. Connect Entry to Blend Tree
5. Test smooth animation blending

**Deliverable:**
- Blend Tree state created
- Smooth blending between animations
- Movement looks natural at all speeds
- No hard animation cuts

**Files to Modify:**
- `PlayerAnimator.controller` (add blend tree)

---

## 📅 WEEK 19: Shooting Animation Integration
**Goal**: Play shooting animation when firing

### 📖 Teaching Block (30 minutes)

**Topics to Cover:**
1. **Trigger Parameters** (10 min)
   - One-time events vs continuous state
   - Automatically resets after use
   - Perfect for actions like shooting

2. **Any State Transitions** (10 min)
   - Special state that means "from anywhere"
   - Allows shooting during any animation
   - Returns to previous state when done

3. **Animation Layers** (10 min)
   - Upper body vs lower body animation
   - Avatar masks for partial animation
   - Shooting while moving

**Key Code Concepts:**
```csharp
// Trigger shooting animation
animator.SetTrigger("Shoot");
```

### 🔨 Building Block (30 minutes)

**What We'll Build:**
1. Add "Shoot" Trigger parameter
2. Add Shoot animation state
3. Create Any State → Shoot transition (condition: Shoot trigger)
4. Create Shoot → Exit transition (automatic when done)
5. Trigger from PlayerShooting script

**Deliverable:**
- Shooting animation plays when firing
- Returns to movement animation after
- Works while idle, walking, or running

**Files to Modify:**
- `PlayerShooting.cs` (trigger shoot animation)
- `PlayerAnimator.controller` (add shoot trigger and transitions)

---

## 📅 WEEK 20: Enemy Animation Setup
**Goal**: Animate the enemy character

### 📖 Teaching Block (30 minutes)

**Topics to Cover:**
1. **Reusing Animator Controllers** (10 min)
   - Can enemies use same controller?
   - When to create separate controller
   - Sharing animations between characters

2. **Setting Parameters from EnemyAI** (15 min)
   - AI controls animation, not input
   - NavMeshAgent.velocity for speed
   - State machine states → animation states

3. **Animation Events** (5 min)
   - Triggering code from animation
   - When attack animation reaches hit frame
   - Adding events in Animation window

**Key Code Concepts:**
```csharp
// In EnemyAI Update()
float speed = navMeshAgent.velocity.magnitude;
animator.SetFloat("Speed", speed);
```

### 🔨 Building Block (30 minutes)

**What We'll Build:**
1. Add humanoid model to enemy
2. Add Animator component
3. Create EnemyAnimator controller (or reuse player's)
4. Connect EnemyAI state to animator parameters
5. Test enemy animating while patrolling

**Deliverable:**
- Enemy has humanoid model and animations
- Animates while moving
- Idle when stopped
- Basic enemy animation working

**Files to Modify:**
- `EnemyAI.cs` (set animator parameters)

**Files to Create:**
- `EnemyAnimator.controller` (if different from player)

---

## 📅 WEEK 21: NavMesh Basics for Enemy AI
**Goal**: Enemy can navigate around obstacles

### 📖 Teaching Block (30 minutes)

**Topics to Cover:**
1. **What is NavMesh** (10 min)
   - AI navigation mesh
   - Baking walkable areas
   - Obstacles and agents

2. **NavMeshAgent Component** (15 min)
   - Adding to enemy
   - SetDestination() for movement
   - Agent properties (speed, radius, height)

3. **Baking NavMesh** (5 min)
   - Window → AI → Navigation
   - Marking objects as static
   - Bake button

**Key Code Concepts:**
```csharp
NavMeshAgent agent = GetComponent<NavMeshAgent>();
agent.SetDestination(targetPosition);

// Check if reached destination
if (agent.remainingDistance < 0.1f) {
    // Arrived!
}
```

### 🔨 Building Block (30 minutes)

**What We'll Build:**
1. Mark ground as Navigation Static
2. Open Navigation window
3. Bake NavMesh
4. Add NavMeshAgent to enemy
5. Create simple script to make enemy move to player

**Deliverable:**
- NavMesh baked in scene
- Enemy has NavMeshAgent
- Enemy moves toward player
- Enemy navigates around obstacles

**Files to Modify:**
- Create test script or modify `EnemyAI.cs` basics

---

## 📅 WEEK 22: Enemy State Machine - Part 1 (Idle & Chase)
**Goal**: Enemy can detect and chase player

### 📖 Teaching Block (30 minutes)

**Topics to Cover:**
1. **State Machine Pattern** (15 min)
   - What is a state machine
   - States (Idle, Patrol, Chase, Attack)
   - Transitions between states

2. **Enum for States** (10 min)
   - Creating enum for state types
   - Switch statement for state handling
   - Current state variable

3. **Line of Sight Detection** (5 min)
   - Raycast to player
   - Distance check
   - Angle check (field of view)

**Key Code Concepts:**
```csharp
public enum EnemyState { Idle, Patrol, Chase, Attack }
private EnemyState currentState = EnemyState.Idle;

void Update() {
    switch(currentState) {
        case EnemyState.Idle:
            IdleState();
            break;
        case EnemyState.Chase:
            ChaseState();
            break;
    }
}
```

### 🔨 Building Block (30 minutes)

**What We'll Build:**
1. Create EnemyState enum
2. Implement Idle state (stand still)
3. Implement Chase state (move to player)
4. Add CanSeePlayer() method (raycast)
5. Transition Idle → Chase when player spotted

**Deliverable:**
- Enemy idles when player not seen
- Enemy chases when player in sight
- Uses NavMesh to pathfind
- State machine basics working

**Files to Modify:**
- `EnemyAI.cs` (add state machine structure)

---

## 📅 WEEK 23: Enemy State Machine - Part 2 (Patrol)
**Goal**: Enemy patrols between waypoints

### 📖 Teaching Block (30 minutes)

**Topics to Cover:**
1. **Waypoint System** (15 min)
   - Array of Transform waypoints
   - Moving to waypoints sequentially
   - Looping back to start

2. **Checking Arrival** (10 min)
   - NavMeshAgent.remainingDistance
   - Setting next waypoint when arrived
   - Index wrapping (mod operator)

3. **Patrol → Chase Transition** (5 min)
   - Check for player while patrolling
   - Interrupt patrol to chase
   - Return to patrol if lose sight

**Key Code Concepts:**
```csharp
void PatrolState() {
    if (reachedWaypoint) {
        currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
        agent.SetDestination(waypoints[currentWaypoint].position);
    }
    
    if (CanSeePlayer()) {
        currentState = EnemyState.Chase;
    }
}
```

### 🔨 Building Block (30 minutes)

**What We'll Build:**
1. Create waypoint GameObjects in scene
2. Add waypoint array to EnemyAI
3. Implement Patrol state
4. Move between waypoints
5. Transition to Chase when player spotted

**Deliverable:**
- Enemy patrols waypoint path
- Smoothly transitions to chase
- Returns to patrol if loses player
- Waypoints easy to set up in inspector

**Files to Modify:**
- `EnemyAI.cs` (add Patrol state)

---

## 📅 WEEK 24: Enemy State Machine - Part 3 (Attack)
**Goal**: Enemy attacks when close to player

### 📖 Teaching Block (30 minutes)

**Topics to Cover:**
1. **Attack Range** (10 min)
   - Distance check to player
   - Stop moving when in range
   - NavMeshAgent.isStopped

2. **Attack Cooldown** (10 min)
   - Time between attacks
   - Tracking last attack time
   - Time.time for timing

3. **Dealing Damage to Player** (10 min)
   - Getting player Health component
   - Calling TakeDamage()
   - Attack animation trigger

**Key Code Concepts:**
```csharp
void AttackState() {
    agent.isStopped = true;
    
    // Face player
    transform.LookAt(player.position);
    
    // Attack if cooldown ready
    if (Time.time >= lastAttackTime + attackCooldown) {
        playerHealth.TakeDamage(attackDamage, transform.position);
        animator.SetTrigger("Attack");
        lastAttackTime = Time.time;
    }
    
    // Return to chase if out of range
    if (Vector3.Distance(transform.position, player.position) > attackRange) {
        currentState = EnemyState.Chase;
        agent.isStopped = false;
    }
}
```

### 🔨 Building Block (30 minutes)

**What We'll Build:**
1. Add Attack state to state machine
2. Implement distance check for attack range
3. Stop enemy movement in attack state
4. Deal damage to player
5. Add attack cooldown timer

**Deliverable:**
- Enemy stops and attacks when close
- Player takes damage
- Attack has cooldown (can't spam)
- Returns to chase if player runs away

**Files to Modify:**
- `EnemyAI.cs` (add Attack state)

---

## 📅 WEEK 25: Player Health UI
**Goal**: Show player health on screen

### 📖 Teaching Block (30 minutes)

**Topics to Cover:**
1. **UI Canvas Rendering Modes** (10 min)
   - Screen Space Overlay vs Camera vs World
   - Why overlay for HUD elements
   - Canvas Scaler component

2. **UI Image Fill** (15 min)
   - Image Type: Filled
   - Fill Method: Horizontal
   - Fill Amount (0-1 range)

3. **Updating UI from Code** (5 min)
   - Getting Image component
   - Setting fillAmount based on health percentage
   - Color gradient (green → yellow → red)

**Key Code Concepts:**
```csharp
float healthPercent = currentHealth / maxHealth;
healthBar.fillAmount = healthPercent;

// Color gradient
healthBar.color = Color.Lerp(Color.red, Color.green, healthPercent);
```

### 🔨 Building Block (30 minutes)

**What We'll Build:**
1. Create Canvas (Screen Space - Overlay)
2. Add panel for health bar background
3. Add Image for health bar fill
4. Script to update fill based on health
5. Add color gradient

**Deliverable:**
- Health bar visible in corner
- Updates when taking damage
- Color changes based on health level
- Clean and readable

**Files to Create:**
- `HealthBarUI.cs` (or simple update in Health.cs)

---

## 📅 WEEK 26: Enemy Health Bar (World Space)
**Goal**: Show health bar above enemy head

### 📖 Teaching Block (30 minutes)

**Topics to Cover:**
1. **World Space Canvas** (15 min)
   - Canvas attached to enemy
   - Render Mode: World Space
   - Billboard effect (always face camera)

2. **Canvas Positioning** (10 min)
   - Offset above enemy head
   - Scale for proper size
   - Parent/child relationship

3. **Billboard Script** (5 min)
   - LookAt camera each frame
   - Smooth rotation option

**Key Code Concepts:**
```csharp
void Update() {
    // Make health bar face camera
    transform.LookAt(Camera.main.transform);
    transform.Rotate(0, 180, 0); // Flip to face correctly
}
```

### 🔨 Building Block (30 minutes)

**What We'll Build:**
1. Create world space canvas on enemy
2. Add health bar background and fill
3. Position above enemy head
4. Make it face camera always
5. Update fill when enemy takes damage

**Deliverable:**
- Health bar floating above enemy
- Always faces camera
- Updates when enemy damaged
- Visible from all angles

**Files to Modify:**
- `Health.cs` (update world space health bar)

**Files to Create:**
- `Billboard.cs` (simple camera facing)

---

## 📅 WEEK 27: Weapon Data with ScriptableObjects
**Goal**: Create data-driven weapon system

### 📖 Teaching Block (30 minutes)

**Topics to Cover:**
1. **ScriptableObjects** (15 min)
   - Data containers in Unity
   - Creating assets
   - Benefits for game data

2. **Why Data-Driven Design** (10 min)
   - Separate data from logic
   - Easy to create new weapons
   - Designer-friendly

3. **Creating ScriptableObject** (5 min)
   - [CreateAssetMenu] attribute
   - Creating instance in project
   - Referencing in scripts

**Key Code Concepts:**
```csharp
[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject {
    public string weaponName;
    public float damage;
    public float fireRate;
    public float range;
    public int maxAmmo;
}
```

### 🔨 Building Block (30 minutes)

**What We'll Build:**
1. Create WeaponData ScriptableObject script
2. Add weapon properties (damage, fire rate, etc.)
3. Create three weapon data assets:
   - Pistol (fast, low damage)
   - Rifle (medium)
   - Shotgun (slow, high damage, spread)
4. Modify shooting to use WeaponData

**Deliverable:**
- WeaponData ScriptableObject created
- 3 weapon data assets
- Weapons use data for stats
- Easy to tweak values in inspector

**Files to Create:**
- `WeaponData.cs` (ScriptableObject)

**Files to Modify:**
- `PlayerShooting.cs` (use WeaponData)

---

## 📅 WEEK 28: Weapon Switching System
**Goal**: Switch between multiple weapons

### 📖 Teaching Block (30 minutes)

**Topics to Cover:**
1. **Array of Weapons** (10 min)
   - Holding multiple weapon references
   - Current weapon index
   - Switching between weapons

2. **Input for Weapon Switching** (10 min)
   - Number keys (1, 2, 3)
   - Mouse scroll wheel
   - Next/Previous weapon

3. **Activating/Deactivating Weapons** (10 min)
   - Enable/disable weapon scripts
   - Show/hide weapon models (future)
   - Active weapon concept

**Key Code Concepts:**
```csharp
public WeaponData[] weapons;
private int currentWeapon = 0;

void Update() {
    if (Input.GetKeyDown(KeyCode.Alpha1)) {
        SwitchWeapon(0);
    }
    if (Input.GetKeyDown(KeyCode.Alpha2)) {
        SwitchWeapon(1);
    }
}

void SwitchWeapon(int index) {
    currentWeapon = index;
    // Update weapon stats
}
```

### 🔨 Building Block (30 minutes)

**What We'll Build:**
1. Add array of WeaponData to PlayerShooting
2. Add current weapon index tracking
3. Implement number key switching
4. Update shooting to use current weapon
5. Visual feedback for current weapon (UI)

**Deliverable:**
- Can switch weapons with 1, 2, 3 keys
- Each weapon has different stats
- Shooting uses current weapon's data
- UI shows current weapon (simple text)

**Files to Modify:**
- `PlayerShooting.cs` (add weapon switching)

---

## 📅 WEEK 29: Ammo System
**Goal**: Track and limit ammunition

### 📖 Teaching Block (30 minutes)

**Topics to Cover:**
1. **Ammo Tracking** (10 min)
   - Current ammo in magazine
   - Reserve ammo
   - Ammo per magazine (from WeaponData)

2. **Reload Mechanics** (15 min)
   - Coroutines for reload time
   - Transferring reserve to magazine
   - Can't shoot while reloading

3. **UI for Ammo** (5 min)
   - Text showing current/max
   - Update when firing or reloading

**Key Code Concepts:**
```csharp
private int currentAmmo;
private int reserveAmmo;
private bool isReloading = false;

void Shoot() {
    if (isReloading) return;
    if (currentAmmo <= 0) return;
    
    currentAmmo--;
    // Fire weapon
}

IEnumerator Reload() {
    isReloading = true;
    yield return new WaitForSeconds(reloadTime);
    
    int ammoNeeded = maxAmmo - currentAmmo;
    int ammoToReload = Mathf.Min(ammoNeeded, reserveAmmo);
    
    currentAmmo += ammoToReload;
    reserveAmmo -= ammoToReload;
    
    isReloading = false;
}
```

### 🔨 Building Block (30 minutes)

**What We'll Build:**
1. Add ammo variables to PlayerShooting
2. Decrease ammo when shooting
3. Implement Reload() coroutine
4. Add R key for reload
5. Create UI text for ammo counter

**Deliverable:**
- Ammo decreases when shooting
- Can reload with R key
- Can't shoot without ammo
- Ammo UI displays current/reserve

**Files to Modify:**
- `PlayerShooting.cs` (add ammo system)
- `WeaponData.cs` (add ammo properties)

---

## 📅 WEEK 30: Projectile Weapon Type
**Goal**: Physical bullets that travel through space

### 📖 Teaching Block (30 minutes)

**Topics to Cover:**
1. **Hitscan vs Projectile Review** (5 min)
   - When to use projectiles
   - Visible bullet travel
   - Can be dodged

2. **Instantiating Projectiles** (15 min)
   - Spawning bullet prefab
   - Setting velocity with Rigidbody
   - Bullet lifetime and destruction

3. **Projectile Collision** (10 min)
   - OnTriggerEnter for hit detection
   - Dealing damage on hit
   - Destroying bullet on impact

**Key Code Concepts:**
```csharp
void FireProjectile() {
    GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
    Rigidbody rb = bullet.GetComponent<Rigidbody>();
    rb.velocity = firePoint.forward * bulletSpeed;
    Destroy(bullet, 5f); // Auto-destroy after 5 seconds
}

// In Projectile.cs
void OnTriggerEnter(Collider other) {
    IDamageable damageable = other.GetComponent<IDamageable>();
    if (damageable != null) {
        damageable.TakeDamage(damage, transform.position);
    }
    Destroy(gameObject);
}
```

### 🔨 Building Block (30 minutes)

**What We'll Build:**
1. Create bullet prefab (sphere with trail renderer)
2. Create Projectile.cs script
3. Add Rigidbody to bullet
4. Spawn bullets when firing projectile weapon
5. Detect hits and deal damage

**Deliverable:**
- Can shoot physical projectiles
- Bullets fly through air visibly
- Bullets deal damage on hit
- Auto-destroy after time/impact

**Files to Create:**
- `Projectile.cs`

**Files to Modify:**
- `PlayerShooting.cs` (add projectile firing mode)

---

## 📅 WEEK 31-32: Polish & Testing
**Goal**: Fix bugs, balance gameplay, add juice

### Week 31 - Bug Fixing & Balancing

**📖 Teaching Block (30 minutes)**
1. **Playtesting Process** (15 min)
   - How to playtest your own game
   - What to look for
   - Taking notes
2. **Balancing Numbers** (15 min)
   - Tweaking damage values
   - Enemy health and difficulty
   - Weapon fire rates

**🔨 Building Block (30 minutes)**
- Play the game for 15 minutes
- Note any bugs or issues
- Adjust values in ScriptableObjects
- Fix any major bugs found

---

### Week 32 - Juice & Feel

**📖 Teaching Block (30 minutes)**
1. **Game Feel / Juice** (20 min)
   - What makes games feel good
   - Camera shake on shooting
   - Muzzle flash particle
   - Impact effects
2. **Simple Particle Systems** (10 min)
   - Unity's built-in particles
   - Spawning effects on events

**🔨 Building Block (30 minutes)**
- Add simple muzzle flash particle
- Add hit spark particle
- Implement basic camera shake
- Add screen flash on taking damage

**Files to Create:**
- `CameraShake.cs` (simple shake effect)

---

## 🎯 PROJECT MILESTONES

### Milestone 1: Basic Movement (Week 8)
✅ Player moves with WASD  
✅ Sprint with Shift  
✅ Third-person camera  
✅ Camera doesn't clip through walls

### Milestone 2: Basic Combat (Week 13)
✅ Can shoot with mouse  
✅ Raycast hits enemies  
✅ Enemies take damage and die  
✅ Simple crosshair

### Milestone 3: Real Characters (Week 20)
✅ Humanoid player character  
✅ Character animations (idle, walk, run, shoot)  
✅ Smooth animation blending  
✅ Humanoid enemy character

### Milestone 4: Smart Enemies (Week 24)
✅ Enemies patrol waypoints  
✅ Enemies chase and attack player  
✅ State machine AI  
✅ NavMesh pathfinding

### Milestone 5: Full Combat Loop (Week 29)
✅ Multiple weapons with unique stats  
✅ Ammo system with reloading  
✅ Health bars for player and enemies  
✅ Win/lose condition

### Milestone 6: Polished Game (Week 32)
✅ Balanced gameplay  
✅ Visual effects and juice  
✅ Bug-free playable experience  
✅ Ready to show off!

---

## 💡 TEACHING TIPS FOR 1-HOUR SESSIONS

### Session Structure Template:

**Minutes 0-5**: Warm-up
- "What did we do last week?"
- Quick demo of last week's feature
- "Any questions from last time?"

**Minutes 5-30**: Teaching New Concept
- Explain the "why" before the "how"
- Show visual examples
- Draw diagrams if helpful
- Live code small examples
- "Does this make sense?"

**Minutes 30-55**: Hands-On Building
- Student drives (types code)
- You guide and prompt
- Encourage experimentation
- Fix errors together
- Test frequently

**Minutes 55-60**: Wrap-up
- "What did we learn today?"
- Quick demo of what we built
- "Try this on your own this week..."
- Preview next week briefly

---

## 🎓 LEARNING OUTCOMES BY END

### Programming Skills:
- ✅ C# fundamentals (variables, methods, classes)
- ✅ Object-Oriented Programming (inheritance, interfaces)
- ✅ Unity-specific C# (MonoBehaviour, Coroutines, Events)
- ✅ State machines
- ✅ Data structures (arrays, lists)

### Unity Skills:
- ✅ Editor navigation and usage
- ✅ Component system
- ✅ Physics and collision
- ✅ Input System
- ✅ Animation system (humanoid rig, animator, blend trees)
- ✅ NavMesh AI
- ✅ UI system
- ✅ Prefabs and ScriptableObjects
- ✅ Scene management

### Game Design Skills:
- ✅ Game feel and player feedback
- ✅ Balancing gameplay
- ✅ AI behavior design
- ✅ System architecture

### Soft Skills:
- ✅ Problem-solving and debugging
- ✅ Breaking big problems into small steps
- ✅ Testing and iteration
- ✅ Self-directed learning

---

## 🚀 BEYOND WEEK 32 (Optional Extensions)

If we finish early or want to continue:

### Advanced Features:
- **Week 33-34**: Object Pooling (performance optimization)
- **Week 35-36**: Enemy Dodge System (reactive AI)
- **Week 37-38**: Cover System (tactical gameplay)
- **Week 39-40**: Wave Spawner & Game Loop
- **Week 41-42**: Main Menu & UI Polish
- **Week 43-44**: Sound Effects & Music
- **Week 45-46**: Boss Enemy
- **Week 47-48**: Final Polish & Build

---

## 📝 WEEKLY HOMEWORK (Optional)

Between sessions, students can:
- **Tweak values** in ScriptableObjects (weapon stats, enemy health)
- **Add more enemies** to test systems
- **Experiment** with animation timing
- **Play other games** and think about how they work
- **Sketch ideas** for new features

**Keep it light** - the goal is to stay excited, not feel overwhelmed!

---

## 🎊 SUCCESS METRICS

By Week 32, you will have:
- ✅ A complete, playable 3D shooter game
- ✅ Professional-looking humanoid characters with animations
- ✅ Smart enemy AI with state machines
- ✅ Multiple weapons with unique behaviors
- ✅ Full combat loop with health and damage
- ✅ A portfolio piece to be proud of!
- ✅ Solid foundation to build your own games

---

**Let's make something awesome together! 🚀**
