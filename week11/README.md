# Week 11: Smart Enemy — Patrol, Detect & Chase! 🧠

⚠️ **IMPORTANT:** This continues directly from Week 10. You should have:
- ✅ A humanoid player character with walk/run animations (Blend Tree)
- ✅ PlayerController script working with WASD + Shift to sprint
- ✅ Third-person camera working
- ✅ An NPC that patrols between Point A and Point B using **NPCPatrol.cs**
- ✅ The NPC has an Animator Controller with a Blend Tree (Idle/Walk)

---

## 🎯 What You'll Build This Week

One massive upgrade:

**Turn your dumb patrolling NPC into a smart enemy that CHASES you when you get close!**

```
BEFORE (start of this week):         AFTER (end of this week):

  🧟 NPC walks A → B → A → B...       🧟 NPC patrols A → B...
     ignores you completely                🧟👀 "I SEE YOU!"
     you can stand right next              🧟💨💨💨 CHASES the player!
     to it and nothing happens             🏃💨 "RUN!!!"
                                           🧟 ...loses you... back to patrol

  "This NPC is boring"                  "This feels like a REAL enemy!"
```

### Two Big New C# Concepts Today:

1. **Enums** — a way to define a list of named states (like Patrol, Chase, Idle)
2. **Switch statements** — a cleaner way to do multiple if/else checks

**Teaching style:** We explain a concept, then immediately code it. No long lectures — learn by doing! 🔧

---

## 📅 Week 11 Structure (60 minutes)

| Time | Part | What You'll Do |
|------|------|----------------|
| 0-5 min | **Part 1** | 🔁 Quick Recap: What we built last week |
| 5-15 min | **Part 2** | 🎓 Concept: State Machines, Enums & Switch |
| 15-40 min | **Part 3** | 🖐️ Hands-On: Upgrade NPCPatrol → EnemyAI script |
| 40-50 min | **Part 4** | 🎮 Test, debug & tweak the enemy |
| 50-58 min | **Part 5** | 🧪 Experiments & fun challenges |
| 58-60 min | **Part 6** | 👀 Preview: What's coming next! |

---

## Part 1: Quick Recap (5 minutes)

### 🔁 What We Built Last Week

Last week we created an NPC that patrols between two points. Let's quickly remember what it does:

```
What NPCPatrol.cs does:
┌──────────────────────────────────────────────────────┐
│ 1. Has two patrol points (A and B)                   │
│ 2. Walks toward the current target point             │
│ 3. When it arrives, swaps to the other point         │
│ 4. Repeats forever                                   │
│ 5. Sends walkSpeed to the Animator so it animates    │
└──────────────────────────────────────────────────────┘
```

**The big problem:** The NPC is completely blind. You can stand RIGHT in front of it and it walks past you like you don't exist. That's not an enemy — that's a walking mannequin!

```
Right now:

  🏃 Player standing here
        ↓
  ·     ★     ·
  A ──→ 🧟 ──→ B      NPC walks right past you! Rude! 😤
```

**Today we fix this!** We'll give the enemy **eyes** (a detection range) and **a brain** (a state machine) so it knows WHEN to patrol and WHEN to chase.

---

## Part 2: What Is a State Machine? (10 minutes)

### 🎓 Real-World State Machines (2 minutes)

A **state machine** is just a fancy name for something that can be in **one mode at a time** and switches between modes based on rules.

You already know state machines from real life — you just didn't know they had a name!

```
YOU are a state machine:

  ┌──────────┐    bell rings    ┌──────────┐   lunch bell   ┌──────────┐
  │ SLEEPING │ ───────────────→ │ IN CLASS │ ────────────→  │ EATING   │
  └──────────┘                  └──────────┘                └──────────┘
       ↑                              ↑                          │
       │         bell rings           │      bell rings          │
       │  ←────────────────────────   │  ←───────────────────────┘
       │  (end of school day)         │

  You can only be in ONE state at a time!
  You can't be sleeping AND eating AND in class at once.
  Something happens (a bell rings) that makes you SWITCH states.
```

A traffic light is also a state machine:

```
  🔴 RED  ──timer──→  🟢 GREEN  ──timer──→  🟡 YELLOW  ──timer──→  🔴 RED
  (stop)               (go)                   (slow down)
```

### 🎓 Our Enemy's State Machine (3 minutes)

Our enemy will have **two states:**

| State | What the enemy does | When does it switch? |
|-------|--------------------|-----------------------|
| **PATROL** | Walk between Point A and Point B (same as last week) | Switches to CHASE when the player gets **close** |
| **CHASE** | Stop patrolling. Run **directly at the player!** | Switches back to PATROL when the player gets **far away** |

```
Enemy's brain:

  ┌──────────┐   player gets close   ┌──────────┐
  │ PATROL   │ ─────────────────────→ │  CHASE   │
  │ (walk    │                        │ (run at  │
  │ A ↔ B)  │ ←───────────────────── │  player!)│
  └──────────┘   player gets far away └──────────┘
```

**The key question:** How close is "close"? We'll use a number called **`detectionRange`** — a circle around the enemy. If the player steps inside the circle, the enemy switches to CHASE. If the player escapes the circle, the enemy goes back to PATROL.

```
Scene (top view):

                    detectionRange = 8 units
                    ·─────────────────────·
                   ╱                       ╲
                  │      🧟 ENEMY          │
                  │    (center of circle)   │
                   ╲                       ╱
                    ·─────────────────────·

  🏃 Player is OUTSIDE the circle → Enemy state: PATROL (ignores player)
  
  
                    ·─────────────────────·
                   ╱                       ╲
                  │   🧟 ENEMY   🏃Player  │
                  │              ↑          │
                   ╲        inside!        ╱
                    ·─────────────────────·

  🏃 Player is INSIDE the circle → Enemy state: CHASE! (run at player!)
```

### 🎓 New C# Concept #1: Enums (3 minutes)

To represent the enemy's states in code, we need a way to say "the enemy is either patrolling or chasing." We COULD use strings:

```csharp
// ❌ BAD way — using strings
string currentState = "Patrol";

if (currentState == "patrol")   // Oops! lowercase 'p' — bug!
if (currentState == "Petrol")   // Typo! This will never match!
```

Strings are dangerous because **typos compile just fine** but break everything silently. C# has a better way: **enums**.

**Enum** (short for **enumeration**) = a custom type where YOU define the exact list of allowed values.

```csharp
// ✅ GOOD way — using an enum
enum EnemyState
{
    Patrol,    // Value 0
    Chase      // Value 1
}

EnemyState currentState = EnemyState.Patrol;

// Now if you try:
if (currentState == EnemyState.patrol)   // ❌ WON'T COMPILE! Red squiggly line!
if (currentState == EnemyState.Petrol)   // ❌ WON'T COMPILE! Red squiggly line!
if (currentState == EnemyState.Patrol)   // ✅ This is the only correct spelling!
```

> 💡 **The big benefit:** The computer **catches your typos** at compile time instead of letting broken code run. You can only use the values you defined — nothing else!

Think of an enum like a dropdown menu. When Unity asks "What state is the enemy in?", instead of letting you type ANYTHING (where you might misspell it), it gives you a dropdown with ONLY the valid options:

```
Without enum (string):         With enum:
┌──────────────────────┐       ┌──────────────────────┐
│ State: [___________] │       │ State: [▼ Patrol   ] │
│ (type anything...    │       │         ├ Patrol     │
│  hope you spell it   │       │         └ Chase      │
│  right!)             │       │ (can ONLY pick these)│
└──────────────────────┘       └──────────────────────┘
```

### 🎓 New C# Concept #2: Switch Statements (2 minutes)

When you have a state machine, you need to run **different code depending on the current state**. You COULD use if/else:

```csharp
// Works, but gets messy with many states
if (currentState == EnemyState.Patrol)
{
    // patrol code...
}
else if (currentState == EnemyState.Chase)
{
    // chase code...
}
```

C# has a cleaner way for this: the **switch** statement.

```csharp
switch (currentState)
{
    case EnemyState.Patrol:
        // patrol code here
        break;          // ← "break" means "stop here, don't fall through"

    case EnemyState.Chase:
        // chase code here
        break;
}
```

**Why switch is better than if/else for state machines:**

```
if/else chain:                     switch statement:
┌─────────────────────────┐        ┌─────────────────────────┐
│ if (state == Patrol)    │        │ switch (state)           │
│   { ... }               │        │ {                        │
│ else if (state == Chase) │       │   case Patrol: ... break;│
│   { ... }               │        │   case Chase:  ... break;│
│ else if (state == Flee) │        │   case Flee:   ... break;│
│   { ... }               │        │   case Dead)   ... break;│
│ else if (state == Dead) │        │ }                        │
│   { ... }               │        │                          │
│                         │        │ ← Cleaner! Easier to     │
│ ← Gets long and messy   │       │   read! Easier to add    │
│   with many states!     │        │   new states later!      │
└─────────────────────────┘        └─────────────────────────┘
```

> 💡 **The `break;` keyword** is required at the end of each `case`. It tells C#: "I'm done with this case, jump to the end of the switch." If you forget it, the compiler will give you an error.

**Got enums and switch? Great — let's use them to build the enemy AI!** 🔧

---

## Part 3: Hands-On — Build the EnemyAI Script (25 minutes)

We're going to write a **brand new script** called `EnemyAI.cs` that replaces `NPCPatrol.cs`. The new script does everything NPCPatrol did (patrol between A and B) PLUS detects the player and chases them!

### 🎓 The Plan

Here's everything the new script needs to do:

```
EnemyAI.cs — What it does:

  PATROL state:
  ┌──────────────────────────────────────┐
  │ Walk between Point A and Point B     │
  │ (same as NPCPatrol from last week!) │
  │                                      │
  │ EVERY FRAME: Check distance to player│
  │ If player is within detectionRange → │
  │   SWITCH to CHASE state!             │
  └──────────────────────────────────────┘

  CHASE state:
  ┌──────────────────────────────────────┐
  │ IGNORE patrol points                 │
  │ Run directly at the player!          │
  │ (faster than patrol speed!)          │
  │                                      │
  │ EVERY FRAME: Check distance to player│
  │ If player is farther than loseRange →│
  │   SWITCH back to PATROL state!       │
  └──────────────────────────────────────┘
```

> 💡 **Why `detectionRange` and `loseRange` are DIFFERENT numbers:**
>
> We use TWO ranges instead of one. `detectionRange` is how close the player needs to get for the enemy to notice them. `loseRange` is how far the player needs to run before the enemy gives up chasing.
>
> `loseRange` should be **bigger** than `detectionRange`. Why? Imagine if they were the same number (say, 8). The player walks into range at 7.9 units → enemy starts chasing → enemy moves toward player → now distance is 8.1 → enemy stops chasing → distance drops to 7.9 again → chases again → stops → chases... The enemy would **flicker between states** every frame!
>
> ```
> BAD (same range = 8 for both):
>   🏃 Player at distance 7.9  → 🧟 CHASE!
>   🏃 Player at distance 8.1  → 🧟 PATROL  (instant switch!)
>   🏃 Player at distance 7.9  → 🧟 CHASE!  (flickering!)
>
> GOOD (detect = 8, lose = 12):
>   🏃 Player at distance 7.9  → 🧟 CHASE!
>   🏃 Player at distance 8.1  → 🧟 still chasing (need 12 to lose)
>   🏃 Player at distance 10   → 🧟 still chasing
>   🏃 Player at distance 12.1 → 🧟 PATROL (finally gives up)
> ```
>
> This "gap" between detect and lose ranges is called **hysteresis** — a buffer zone that prevents rapid flickering. Game developers use this trick ALL the time.

### Step 1: Create the Script

1. In your Unity Project panel, navigate to `Assets/Scripts/`
2. Right-click → **Create → C# Script**
3. Name it exactly **`EnemyAI`** (capital E, capital A, capital I — this must match the class name inside!)
4. **Double-click** to open it in your code editor
5. **Select ALL the code** (Ctrl+A) and **DELETE it** — we'll write it fresh from scratch

### Step 2: Write the Script — Section by Section

We'll go piece by piece. **Read the explanation, then type the code together.**

---

**🎓 Section 1: The enum and class header (explain → type)**

This goes at the very TOP of the file, before the class:

```csharp
using UnityEngine;

/// <summary>
/// Week 11: Enemy AI with Patrol and Chase states.
/// The enemy patrols between two points. When the player gets close,
/// it switches to Chase mode and runs at the player!
/// </summary>

// This enum defines ALL the possible states the enemy can be in.
// Right now there are only 2, but we could add more later (Flee, Attack, Dead...)
public enum EnemyState
{
    Patrol,   // Walk between Point A and Point B
    Chase     // Run directly at the player!
}
```

> 💡 **Ask the student:** "Why is the enum OUTSIDE the class?" Answer: We define it outside so other scripts could use it too if needed. It's like creating a new vocabulary word that the whole project can use. You COULD put it inside the class, but keeping it outside is the common convention.

> 💡 **Notice:** The enum just says what states EXIST. It doesn't say what happens in each state — that's the switch statement's job (coming later).

---

**🎓 Section 2: Class declaration and fields (explain → type)**

Right after the enum, type the class:

```csharp
public class EnemyAI : MonoBehaviour
{
    [Header("Patrol Points")]
    [Tooltip("First patrol destination (drag an Empty GameObject here)")]
    public Transform pointA;

    [Tooltip("Second patrol destination (drag an Empty GameObject here)")]
    public Transform pointB;

    [Header("Movement Speeds")]
    [Tooltip("How fast the enemy walks while patrolling")]
    public float patrolSpeed = 2f;

    [Tooltip("How fast the enemy runs while chasing — should be faster than patrol!")]
    public float chaseSpeed = 4.5f;
```

> 💡 **Ask the student:** "Why is `chaseSpeed` bigger than `patrolSpeed`?" Because when the enemy spots you, it should be MORE dangerous! If it chased you at the same slow patrol speed, you could just walk away. The chase needs to feel threatening!
>
> 💡 **Important design question:** "Should `chaseSpeed` be faster than the PLAYER's walk speed?" YES! Otherwise the enemy could never catch you. Our player's walk speed is 3 and sprint is 6. So a chaseSpeed of 4.5 means:
> - If you WALK, the enemy catches you! 😱
> - If you SPRINT, you can escape! 😅
> This creates an exciting gameplay dynamic where you NEED to sprint to survive.

---

**🎓 Section 3: Detection settings (explain → type)**

Continue typing inside the class:

```csharp
    [Header("Detection")]
    [Tooltip("How close the player must be for the enemy to notice them")]
    public float detectionRange = 8f;

    [Tooltip("How far the player must run before the enemy gives up chasing")]
    public float loseRange = 12f;

    [Tooltip("How close the enemy needs to get to a patrol point before turning around")]
    public float arrivalDistance = 0.5f;
```

> 💡 **Ask the student:** "Remember why `loseRange` is bigger than `detectionRange`?" (Let them explain the flickering problem from the concept section. If they can explain it, they really understand it!)

---

**🎓 Section 4: Private variables (explain → type)**

These are things the script tracks internally that don't show up in the Inspector:

```csharp
    // === Private variables (tracked internally) ===

    // The current state of the enemy — starts in Patrol mode
    private EnemyState currentState = EnemyState.Patrol;

    // Which patrol point we're currently walking toward
    private Transform currentPatrolTarget;

    // Reference to the Animator component (for animations)
    private Animator animator;

    // Reference to the player's Transform (so we know where the player is)
    private Transform player;
```

> 💡 **Ask the student:** "Why is `currentState` private?" Because nothing OUTSIDE this script should change the enemy's state directly. The enemy decides for itself when to switch states based on the player's distance. Making it private means other scripts can't accidentally set it to Chase when it shouldn't be.

---

**🎓 Section 5: Start — initialize everything (explain → type)**

```csharp
    void Start()
    {
        // Start by walking toward Point B
        currentPatrolTarget = pointB;

        // Find the Animator on the character model (child object)
        // IMPORTANT: Use GetComponentInChildren, NOT GetComponent!
        // The Animator is on the character model CHILD, not the NPC parent.
        // GetComponent only checks THIS object.
        // GetComponentInChildren checks this object AND all children.
        animator = GetComponentInChildren<Animator>();
        if (animator is null)
            Debug.LogWarning("EnemyAI: No Animator component found on " + gameObject.name);

        // Find the player in the scene
        // GameObject.FindWithTag looks for any object tagged "Player"
        // This is a Unity built-in tag — we'll set it up in the editor!
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
        else
            Debug.LogWarning("EnemyAI: No GameObject with tag 'Player' found in the scene.");
    }
```

> 💡 **New function: `GameObject.FindWithTag("Player")`**
>
> This searches the entire scene for a GameObject that has the **"Player"** tag. Tags are like name badges you stick on objects in Unity.
>
> ```
> How FindWithTag works:
>
>   Your scene:
>   ├── Player        tag: "Player"  ← FindWithTag("Player") finds THIS!
>   │   └── YBot
>   ├── NPC           tag: "Untagged"
>   │   └── Mutant
>   ├── Main Camera   tag: "MainCamera"
>   └── Ground        tag: "Untagged"
> ```
>
> **Why use a tag instead of the object's name?** Because names can change (you might rename "Player" to "Hero" someday), but tags are specifically designed for code to find objects. Unity even has a built-in "Player" tag ready to use — we just need to assign it.
>
> **Why find the player in `Start()` instead of every frame?** Because `FindWithTag` takes time — it has to search through EVERY object in the scene. Doing that 60 times per second (in Update) would waste performance. Since the player doesn't appear and disappear, we find it ONCE in Start and save the reference.

---

**🎓 Section 6: Update — the brain of the enemy (explain → type)**

This is the core of the AI! Every single frame, the enemy:
1. Checks if it can find the player
2. Uses a switch statement to run the right behavior for the current state
3. Updates the animation

```csharp
    void Update()
    {
        // Can't do anything without a player or patrol points
        if (player == null || pointA == null || pointB == null)
        {
            if (animator != null) animator.SetFloat("Speed", 0f);
            return;
        }

        // How far is the player right now?
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // THE STATE MACHINE — run different code depending on current state
        switch (currentState)
        {
            case EnemyState.Patrol:
                Patrol();

                // Check: should we switch to Chase?
                if (distanceToPlayer <= detectionRange)
                {
                    currentState = EnemyState.Chase;
                    Debug.Log($"{gameObject.name}: Player detected! Switching to CHASE!");
                }
                break;

            case EnemyState.Chase:
                Chase();

                // Check: should we switch back to Patrol?
                if (distanceToPlayer > loseRange)
                {
                    currentState = EnemyState.Patrol;
                    Debug.Log($"{gameObject.name}: Lost the player. Back to PATROL.");
                }
                break;
        }
    }
```

> 💡 **Walk through the switch step by step with the student:**
>
> "Let's say `currentState` is `EnemyState.Patrol`. What happens?"
> 1. C# looks at `switch (currentState)` → the value is `Patrol`
> 2. It jumps to `case EnemyState.Patrol:` → runs `Patrol()`
> 3. Then checks if the player is close enough to detect
> 4. If YES → changes `currentState` to `EnemyState.Chase`
> 5. `break;` → exits the switch
>
> "Next frame, `currentState` is now `Chase`. What happens?"
> 1. C# looks at `switch (currentState)` → the value is `Chase`
> 2. It SKIPS the Patrol case entirely
> 3. Jumps to `case EnemyState.Chase:` → runs `Chase()`
> 4. Checks if the player is far enough to lose
> 5. If YES → changes `currentState` back to `Patrol`
> 6. `break;` → exits the switch

> 💡 **Notice:** We use `<=` (less than or equal) for detection and `>` (greater than) for losing. This ensures there's no ambiguity at exact boundary distances.

---

**🎓 Section 7: The Patrol method (explain → type)**

This is almost identical to the old NPCPatrol script! We're just moving it into its own method:

```csharp
    /// <summary>
    /// PATROL state: Walk between Point A and Point B.
    /// This is the same logic from last week's NPCPatrol script!
    /// </summary>
    private void Patrol()
    {
        // Calculate direction to the current patrol target
        Vector3 direction = currentPatrolTarget.position - transform.position;
        direction.y = 0;  // Stay flat — no flying!
        direction.Normalize();  // Make it a unit vector (just direction, length = 1)

        // Move toward the patrol target
        transform.position += direction * patrolSpeed * Time.deltaTime;

        // Smoothly rotate to face the direction we're walking
        if (direction.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                5f * Time.deltaTime
            );
        }

        // Check if we arrived at the patrol point
        float distanceToTarget = Vector3.Distance(transform.position, currentPatrolTarget.position);
        if (distanceToTarget < arrivalDistance)
        {
            // Swap targets! (ternary operator from last week)
            currentPatrolTarget = (currentPatrolTarget == pointA) ? pointB : pointA;
        }

        // Update animation — walk speed
        if (animator != null)
        {
            animator.SetFloat("Speed", patrolSpeed, 0.1f, Time.deltaTime);
        }
    }
```

> 💡 **Ask the student:** "Does this look familiar?" It should! This is almost the exact same code from NPCPatrol.cs. The only differences:
> - It's inside a `private void Patrol()` method instead of Update
> - We use `patrolSpeed` instead of `walkSpeed` (better name since we now have two speeds)

---

**🎓 Section 8: The Chase method — the exciting part! (explain → type)**

This is NEW code! Instead of walking to a patrol point, the enemy runs toward the **player**:

```csharp
    /// <summary>
    /// CHASE state: Run directly at the player!
    /// Instead of following patrol points, the enemy targets the player's position.
    /// </summary>
    private void Chase()
    {
        // Calculate direction to the PLAYER (not a patrol point!)
        Vector3 direction = player.position - transform.position;
        direction.y = 0;  // Stay flat

        // Only move if we're not already on top of the player
        // Without this check, the enemy would JITTER back and forth
        // when it reaches you — overshooting your position every frame!
        float distToPlayer = direction.magnitude;
        if (distToPlayer > 0.5f)
        {
            direction.Normalize();

            // Move toward the player at chase speed (faster than patrol!)
            transform.position += direction * chaseSpeed * Time.deltaTime;

            // Smoothly rotate to face the player
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                8f * Time.deltaTime   // Turn faster during chase (8 vs 5)!
            );
        }

        // Update animation — run when chasing, idle when reached the player
        if (animator != null)
        {
            animator.SetFloat("Speed", distToPlayer > 0.5f ? chaseSpeed : 0f, 0.1f, Time.deltaTime);
        }
    }
```

> 💡 **Why the `distToPlayer > 0.5f` check?** Without it, the enemy would reach you and then **vibrate back and forth** every frame:
>
> ```
> Without stopping distance (BAD):
>   Frame 1: NPC behind you  → moves forward  → overshoots past you
>   Frame 2: NPC ahead of you → moves backward → overshoots again
>   Frame 3: repeat forever → JITTER! 😵
>
> With stopping distance of 0.5 (GOOD):
>   Frame 1: NPC approaches you... getting closer...
>   Frame 2: Distance is 0.4 → STOP! Stand still next to you.
>   Frame 3: You walk away → distance is 2.0 → resume chasing!
> ```
>
> The value `0.5f` means "stop when you're within half a unit of the player." You can tweak this — smaller means the enemy gets closer, bigger means it keeps more distance.

> 💡 **Compare Patrol vs Chase side by side:**
>
> ```
> Patrol:                              Chase:
> ┌──────────────────────────┐         ┌──────────────────────────┐
> │ Target: patrol point     │         │ Target: PLAYER           │
> │ Speed: patrolSpeed (2)   │         │ Speed: chaseSpeed (4.5)  │
> │ Turn speed: 5            │         │ Turn speed: 8 (snappier!)│
> │ Arrivals: swap points    │         │ Arrivals: stops at 0.5   │
> │ Anim: walk               │         │ Anim: run (idle if close)│
> └──────────────────────────┘         └──────────────────────────┘
> ```
>
> **Ask the student:** "What's different between Patrol and Chase?"
> 1. **Target** — patrol point vs player position
> 2. **Speed** — slow (2) vs fast (4.5)
> 3. **Turn speed** — 5 vs 8 (enemy turns to face you more aggressively)
> 4. **Stopping distance** — the enemy stops 0.5 units from the player (instead of jittering on top of them)

> 💡 **Why turn speed 8 instead of 5?** During a chase, the player might try to dodge left and right. A faster turn speed means the enemy can quickly change direction to follow you. If the turn speed were slow, you could easily juke the enemy by sidestepping. Making it faster makes the chase scarier!

---

**🎓 Section 9: Close the class (explain → type)**

One last closing brace to finish the class:

```csharp
}
```

**Save the script!** (Ctrl+S)

---

### 📋 Full Script Reference

If you want to double-check your code, here's the **complete** EnemyAI.cs all in one block. But try to NOT just copy-paste this — you learn way more by typing each section!

<details>
<summary>📋 Click to see the complete EnemyAI.cs script</summary>

```csharp
using UnityEngine;

/// <summary>
/// Week 11: Enemy AI with Patrol and Chase states.
/// The enemy patrols between two points. When the player gets close,
/// it switches to Chase mode and runs at the player!
/// </summary>

public enum EnemyState
{
    Patrol,
    Chase
}

public class EnemyAI : MonoBehaviour
{
    [Header("Patrol Points")]
    [Tooltip("First patrol destination (drag an Empty GameObject here)")]
    public Transform pointA;

    [Tooltip("Second patrol destination (drag an Empty GameObject here)")]
    public Transform pointB;

    [Header("Movement Speeds")]
    [Tooltip("How fast the enemy walks while patrolling")]
    public float patrolSpeed = 2f;

    [Tooltip("How fast the enemy runs while chasing — should be faster than patrol!")]
    public float chaseSpeed = 4.5f;

    [Header("Detection")]
    [Tooltip("How close the player must be for the enemy to notice them")]
    public float detectionRange = 8f;

    [Tooltip("How far the player must run before the enemy gives up chasing")]
    public float loseRange = 12f;

    [Tooltip("How close the enemy needs to get to a patrol point before turning around")]
    public float arrivalDistance = 0.5f;

    private EnemyState currentState = EnemyState.Patrol;
    private Transform currentPatrolTarget;
    private Animator animator;
    private Transform player;

    void Start()
    {
        currentPatrolTarget = pointB;

        animator = GetComponentInChildren<Animator>();
        if (animator is null)
            Debug.LogWarning("EnemyAI: No Animator component found on " + gameObject.name);

        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
        else
            Debug.LogWarning("EnemyAI: No GameObject with tag 'Player' found in the scene.");
    }

    void Update()
    {
        if (player == null || pointA == null || pointB == null)
        {
            if (animator != null) animator.SetFloat("Speed", 0f);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case EnemyState.Patrol:
                Patrol();
                if (distanceToPlayer <= detectionRange)
                {
                    currentState = EnemyState.Chase;
                    Debug.Log($"{gameObject.name}: Player detected! Switching to CHASE!");
                }
                break;

            case EnemyState.Chase:
                Chase();
                if (distanceToPlayer > loseRange)
                {
                    currentState = EnemyState.Patrol;
                    Debug.Log($"{gameObject.name}: Lost the player. Back to PATROL.");
                }
                break;
        }
    }

    private void Patrol()
    {
        Vector3 direction = currentPatrolTarget.position - transform.position;
        direction.y = 0;
        direction.Normalize();

        transform.position += direction * patrolSpeed * Time.deltaTime;

        if (direction.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                5f * Time.deltaTime
            );
        }

        float distanceToTarget = Vector3.Distance(transform.position, currentPatrolTarget.position);
        if (distanceToTarget < arrivalDistance)
        {
            currentPatrolTarget = (currentPatrolTarget == pointA) ? pointB : pointA;
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", patrolSpeed, 0.1f, Time.deltaTime);
        }
    }

    private void Chase()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0;

        float distToPlayer = direction.magnitude;
        if (distToPlayer > 0.5f)
        {
            direction.Normalize();

            transform.position += direction * chaseSpeed * Time.deltaTime;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                8f * Time.deltaTime
            );
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", distToPlayer > 0.5f ? chaseSpeed : 0f, 0.1f, Time.deltaTime);
        }
    }
}
```

</details>

---

### Step 3: Set Up the Player Tag (IMPORTANT!)

Our script uses `FindWithTag("Player")` to find the player. We need to actually TAG the player object!

**🖐️ Do these steps together:**

1. In the **Hierarchy**, click on your **Player** object (the parent, not the character model child)
2. In the **Inspector**, look at the very top. You'll see a field that says **"Tag: Untagged"**

   ```
   Inspector — Player object:
   ┌──────────────────────────────────────────────┐
   │ Player                                        │
   │ Tag: [Untagged ▼]  Layer: [Default ▼]        │
   │                                               │
   │ ← Click this dropdown!                        │
   └──────────────────────────────────────────────┘
   ```

3. Click the **Tag** dropdown → select **"Player"**

   ```
   Tag dropdown:
   ┌──────────────────┐
   │ Untagged         │
   │ Respawn          │
   │ Finish           │
   │ EditorOnly       │
   │ MainCamera       │
   │ ► Player ◄       │ ← Select this one!
   │ GameController   │
   └──────────────────┘
   ```

   > 💡 **"Player" is a built-in tag** — Unity already has it ready for you. You don't need to create it!

4. Make sure it now says **"Tag: Player"** at the top of the Inspector

---

### Step 4: Update the NPC's Animator (Optional but Recommended)

Right now the NPC's Blend Tree only has Idle (threshold 0) and Walk (threshold 2). When the enemy chases at speed 4.5, the Blend Tree will just play the Walk animation because 4.5 is above the highest threshold.

For the chase to look REALLY good, let's add a Run animation:

1. If you don't have a **Run animation** for the NPC yet:
   - Go to **[mixamo.com](https://www.mixamo.com)** → **Animations** → search **"Running"**
   - Check **"In Place" ☑️**
   - Download **FBX for Unity**, **Without Skin**
   - Import it into your NPC's Animations folder
   - Configure: **Rig** tab → Humanoid → Copy From Other Avatar → NPC's avatar → Apply
   - Configure: **Animation** tab → Loop Time ✅ → Apply

2. Open **NPCAnimator** (double-click it) → go inside the **Blend Tree**

3. Click **+** to add a **third** motion slot

4. Set up the Blend Tree like this:

   | Slot | Clip | Threshold |
   |------|------|-----------|
   | 1 | Idle | **0** |
   | 2 | Walking clip | **2** |
   | 3 | Running clip | **5** |

   - **Uncheck** "Automate Thresholds" if it resets them!

   > **Why threshold 5?** Our `chaseSpeed` is 4.5. A threshold of 5 means at speed 4.5, the Blend Tree will play mostly run with a little walk blended in — that's a good look for a fast chase!

```
Updated Blend Tree:

  Speed 0          Speed 2          Speed 5
    │                 │                 │
  IDLE             WALK              RUN
    └────blend────────┘────blend───────┘

  Patrol (speed 2): plays Walk animation ✅
  Chase (speed 4.5): mostly Run animation ✅
  Stopped (speed 0): Idle animation ✅
```

---

### Step 5: Replace NPCPatrol with EnemyAI

Now we swap the old script for the new one on the NPC:

**🖐️ Do these steps together:**

1. Select the **NPC** object in the Hierarchy (the parent, where NPCPatrol is attached)

2. In the Inspector, find the **NPCPatrol** component:
   - Click the **three dots ⋮** (or right-click the component header)
   - Click **"Remove Component"**

   ```
   Inspector — NPC:
   ┌──────────────────────────────────┐
   │ NPC Patrol (Script)    [⋮]      │
   │                         │        │
   │   ← Click the dots → Remove     │
   │                     Component    │
   └──────────────────────────────────┘
   ```

   > ⚠️ **Don't worry about deleting it!** The EnemyAI script does everything NPCPatrol did, plus more. You can keep the NPCPatrol.cs FILE in case you want to look at it later — just remove the COMPONENT from the NPC.

3. Click **Add Component** → search for **"EnemyAI"** → add it

4. Wire up the fields in the Inspector:
   - **Point A:** Drag `PatrolPoint_A` from the Hierarchy
   - **Point B:** Drag `PatrolPoint_B` from the Hierarchy
   - **Patrol Speed:** 2 (default)
   - **Chase Speed:** 4.5 (default)
   - **Detection Range:** 8 (default)
   - **Lose Range:** 12 (default)

   ```
   Inspector — EnemyAI:
   ┌──────────────────────────────────────────────┐
   │ Enemy AI (Script)                             │
   │                                               │
   │ ── Patrol Points ──                           │
   │ Point A:          [PatrolPoint_A           ]  │
   │ Point B:          [PatrolPoint_B           ]  │
   │                                               │
   │ ── Movement Speeds ──                         │
   │ Patrol Speed:     [2                       ]  │
   │ Chase Speed:      [4.5                     ]  │
   │                                               │
   │ ── Detection ──                               │
   │ Detection Range:  [8                       ]  │
   │ Lose Range:       [12                      ]  │
   │ Arrival Distance: [0.5                     ]  │
   └──────────────────────────────────────────────┘
   ```

---

### Step 6: Scene Positioning — CRITICAL! ⚠️

This step is VERY important. If the enemy starts too close to the player, it will immediately enter Chase mode and you'll never see the patrol behavior!

**🖐️ Do these steps together:**

1. Make sure the **Player** is near the origin:
   - Select **Player** in the Hierarchy → Position: **X: 0, Y: 0, Z: 0**

2. Move the **NPC** far away from the player:
   - Select **NPC** in the Hierarchy → Position: **X: 15, Y: 0, Z: 15**

3. Position the **patrol points** near the NPC (not near the player!):
   - **PatrolPoint_A:** X: 12, Y: 0, Z: 15
   - **PatrolPoint_B:** X: 20, Y: 0, Z: 15

   ```
   Scene layout (top view):

     Player ★ (0, 0, 0)                    A ·──── 🧟 ────· B
                                             (12,0,15)    (20,0,15)
            ← about 15+ units apart →

     Player is outside detectionRange (8) → Enemy patrols peacefully!
     Walk toward the enemy to trigger the chase.
   ```

4. **ALSO CHECK:** Click on the **character model child** inside the NPC (e.g., the Mutant or armored character). Its **local position** (in the Transform) **must be (0, 0, 0)**!

   ```
   ❌ WRONG — character child is offset:
   NPC (parent) at (15, 0, 15)
   └── Character at (5, 0.5, 5)  ← The code thinks it's at (15,0,15)
                                    but you SEE it at (20, 0.5, 20)!
                                    Distance calculations will be WRONG.

   ✅ CORRECT — character child at origin:
   NPC (parent) at (15, 0, 15)
   └── Character at (0, 0, 0)   ← Sits right on top of parent.
                                    What you see = what the code calculates. ✅
   ```

   > ⚠️ **Why this matters:** The code measures distance using the **NPC parent's position** (`transform.position`). If the visible character model is offset by (5, 0, 5) from its parent, the code thinks the enemy is in one place, but you SEE it somewhere completely different! The enemy might look far away but the code thinks it's right on top of you. Always reset the character child's local position to (0, 0, 0) — right-click the Transform → **Reset**.

---

## Part 4: Test, Debug & Tweak (10 minutes)

### 🎮 Test It!

1. **Press Play**
2. First, **stand far away** from the NPC. It should patrol between A and B normally (same as last week).
3. Now **walk your player toward the NPC**. When you get within 8 units...

```
Testing the state transitions:

  1. Far away — enemy patrols peacefully:
     A ←── 🧟 ──→ B        🏃 (you, far away)

  2. Walk closer — cross the detection range:
     🧟💨💨💨 → 🏃           "I SEE YOU!" (Console: "Player detected!")

  3. Sprint away — escape beyond the lose range:
     🧟 ...slows... A → B   🏃💨💨 (you, sprinting away)
                             (Console: "Lost the player. Back to PATROL.")

  4. Enemy resumes patrol!
     A ←── 🧟 ──→ B        🏃 (you, safe again... for now)
```

4. **Check the Console!** You should see the Debug.Log messages:
   - `"NPC: Player detected! Switching to CHASE!"` when you get close
   - `"NPC: Lost the player. Back to PATROL."` when you escape

### 🐛 Troubleshooting

#### Common Setup Issues

| Problem | Likely Cause | Fix |
|---------|-------------|-----|
| Enemy doesn't move at all | Patrol points not assigned | Drag PatrolPoint_A and PatrolPoint_B into the EnemyAI fields |
| Enemy patrols but never chases | Player tag not set | Select Player in Hierarchy → Inspector top → Tag → "Player" |
| Console says "No object with tag 'Player' found" | Player tag is wrong or missing | Same fix as above — set the Player tag! |
| Enemy chases but never stops | `loseRange` too small | Increase lose range (try 15 or 20) |
| Enemy flickers between Patrol/Chase | `loseRange` too close to `detectionRange` | Make loseRange at least 1.5× detectionRange |
| Enemy chases but slides without running animation | NPC Blend Tree doesn't have a Run clip at high threshold | Add a Run animation to the Blend Tree at threshold 5 |
| Enemy only plays Walk animation during chase | `chaseSpeed` is below the Run threshold | Increase chaseSpeed OR lower the Run threshold in the Blend Tree |
| Enemy ignores you even when standing right next to it | `detectionRange` too small | Increase detectionRange (try 10 or 12) |

#### Tricky Bugs That Are Harder to Spot

These are real bugs we hit during testing. They're sneaky because the code looks right but something in the Unity setup is wrong!

---

**🐛 Bug: Enemy jitters/vibrates back and forth when it reaches you**

The enemy catches you but instead of stopping, it **vibrates rapidly** in place — shaking back and forth every frame.

```
What's happening:
  Frame 1: NPC behind you   → moves toward you  → overshoots past you
  Frame 2: NPC now ahead     → moves back toward you → overshoots again
  Frame 3: repeat forever   → rapid jittering!
```

**Cause:** The Chase method moves the enemy toward the player every frame, but at close range it keeps overshooting the target position and snapping back. There's no "stop when close enough" logic.

**Fix:** Add a **stopping distance** check in `Chase()`. Don't move if the enemy is already within 0.5 units of the player. This is already in the script above — the `if (distToPlayer > 0.5f)` check.

---

**🐛 Bug: Enemy seems to ignore you OR chase goes wrong, but the code looks correct**

You walk right up to the enemy and nothing happens — or the Console says "Player detected! Switching to CHASE!" but the enemy doesn't visually move toward you. It might even appear to move AWAY.

**Cause:** The **character model child** has a **non-zero local position**. For example, the character's Transform shows Position (5, 0.5, 5) instead of (0, 0, 0).

```
What's happening:

  NPC (parent — code measures from HERE):   position (3, 0, 3)
  └── Character (what you SEE):             offset  +(5, 0.5, 5)
                                            appears at (8, 0.5, 8)

  The code thinks the enemy is at (3, 0, 3) — RIGHT next to you!
  But you SEE the character 5+ units away.
  Distance calculations are all wrong.
  The enemy "chases" but moves the invisible parent, not where you're looking.
```

**Fix:**
1. Click on the **character model child** (e.g., Ch44_nonPBR, Mutant, etc.) inside the NPC
2. In the Inspector → Transform → set Position to **X: 0, Y: 0, Z: 0**
3. Or right-click on the Transform header → **Reset**

---

**🐛 Bug: Enemy immediately starts chasing on Play — never patrols**

The moment you press Play, the Console immediately shows "Player detected! Switching to CHASE!" and the enemy never patrols.

**Cause:** The NPC starts too close to the player. If their starting distance is less than `detectionRange` (8 units), the very first frame triggers Chase mode.

```
Bad starting positions:
  Player at (0, 0, 0)     NPC at (5, 0, 5)
  Distance: ~7 units → LESS than detectionRange (8) → Instant Chase!

Good starting positions:
  Player at (0, 0, 0)     NPC at (15, 0, 15)
  Distance: ~21 units → MORE than detectionRange (8) → Patrol first! ✅
```

**Fix:** Move the NPC (and its patrol points) **far away** from the player. See **Step 6: Scene Positioning** above.

---

**🐛 Bug: Both `NPCPatrol` and `EnemyAI` on the same NPC — enemy jitters between patrol and chase**

The enemy detects you (Console says "Switching to CHASE!") but keeps patrolling as if nothing happened, or jitters between the two behaviors.

**Cause:** The old `NPCPatrol` script AND the new `EnemyAI` script are BOTH attached to the NPC. Both have `Update()` methods that run every frame, fighting each other:

```
Every frame (BAD — two scripts fighting):
  EnemyAI.Chase():       "Move toward the player!" → pushes NPC toward you
  NPCPatrol.Update():    "Move toward patrol point!" → pulls NPC away from you
  Result: NPC jitters, patrolling wins because NPCPatrol doesn't know about Chase
```

**Fix:**
1. Select the NPC in the Hierarchy
2. In the Inspector, look for **NPC Patrol (Script)**
3. Click the three dots ⋮ → **Remove Component**
4. Only `EnemyAI` should remain. It handles BOTH patrolling and chasing.

---

**🐛 Bug: Enemy doesn't animate — slides around or stays in idle during chase**

The enemy moves correctly (chases/patrols) but stays in the idle pose, or slides without walking/running animations.

**Cause:** The script uses `GetComponent<Animator>()` instead of `GetComponentInChildren<Animator>()`. This only checks the NPC parent object, but the Animator is on the **character model child**.

```
NPC hierarchy:
  NPC (parent)          ← GetComponent looks ONLY here. No Animator here!
  └── Character model   ← Animator is here. GetComponent can't see it!

  GetComponent<Animator>() → returns null → all SetFloat calls silently skip
  GetComponentInChildren<Animator>() → finds it on the child! ✅
```

**Fix:** Make sure `Start()` uses:
```csharp
animator = GetComponentInChildren<Animator>();  // ← InChildren!
```
NOT:
```csharp
animator = GetComponent<Animator>();  // ← WRONG! Only checks the parent!
```

> 💡 **How to know this happened:** Look at the Console when you press Play. If you see `"EnemyAI: No Animator component found on NPC"`, that warning is telling you the Animator wasn't found — you need `GetComponentInChildren`.

### 🎯 Live Debugging Tip

While the game is running, you can **select the NPC** and look at the **EnemyAI** component in the Inspector. You'll see the public fields live:

```
Inspector (during Play mode):
┌──────────────────────────────────────────┐
│ Enemy AI (Script)                         │
│                                           │
│ Detection Range: 8                        │
│ Lose Range:      12                       │
│ Chase Speed:     4.5                      │
│                                           │
│ ← You can change these values LIVE!       │
│   They reset when you stop playing.       │
└──────────────────────────────────────────┘
```

Try changing `detectionRange` to 20 while the game is running — the enemy will spot you from much farther away! Changes in Play mode **don't save** (they reset when you stop), so it's safe to experiment.

---

## Part 5: Experiments & Fun Challenges (8 minutes)

Now that the enemy works, let's play with it! Try each experiment and **predict what will happen BEFORE testing it:**

### Experiment 1: Super Detective 🔍

Change **Detection Range** to **30** and **Lose Range** to **40**.

```
What happens?
  The enemy can see you from SUPER far away!
  You can barely move before it's chasing you.
  🧟💨💨💨💨💨 → → → 🏃  "HOW DID YOU SEE ME?!"
```

> Is this fun? Not really — the player has no safe space. Games need a balance!

### Experiment 2: Lightning Enemy ⚡

Change **Chase Speed** to **8** (faster than the player's sprint of 6!)

```
What happens?
  YOU. CAN'T. ESCAPE. 🧟💨💨💨💨💨💨 → 🏃💀
  The enemy outruns you no matter what!
```

> Is this fun? Terrifying, but UNFAIR. Players need a way to escape or it's frustrating.

### Experiment 3: Lazy Enemy 😴

Change **Chase Speed** to **1** and **Patrol Speed** to **0.3**.

```
What happens?
  🧟..... ..... → 🏃
  The slowest chase ever. Not scary at all!
```

### Experiment 4: Blind Enemy 🦇

Change **Detection Range** to **2**.

```
What happens?
  You have to basically step on the enemy before it notices!
  Good for stealthy gameplay, but maybe TOO hard to trigger.
```

### 🤔 Game Design Discussion

**Ask the student:** "Which settings felt the most FUN? Why?"

The answer is usually somewhere in the middle — where the enemy is a THREAT but the player CAN escape if they react quickly. This is **game balance**, and it's one of the hardest parts of game development!

```
Game balance spectrum:

  Too Easy ←────────────── Fun! ──────────────→ Too Hard
    
  Slow enemy               Chase speed 4.5           Fast enemy
  Short detect             Detect 8, Lose 12         Giant detect
  "What enemy?"            "EXCITING!"                "UNFAIR!"
```

---

## Part 6: Preview — What's Coming Next! (2 minutes)

You built an enemy with a brain! It patrols, detects, and chases. But it can run **through walls**, it can't **hurt** you, and you can't **hurt** it. Next we fix that!

```
Coming in future weeks:
┌────────────────────────────────────────────────────┐
│ 🔫 SHOOTING — Raycasting & dealing damage!         │
│   → Point and click to shoot                       │
│   → Bullets hit things in the world                │
│                                                    │
│ ❤️ HEALTH SYSTEM — Things can take damage!          │
│   → Player has health                              │
│   → Enemy has health                               │
│   → IDamageable interface (powerful C# concept!)   │
│                                                    │
│ 🧱 NAVMESH — Enemy navigates AROUND obstacles!     │
│   → No more running through walls!                 │
│   → Unity's built-in pathfinding system            │
│   → Enemy finds the smartest path to reach you     │
│                                                    │
│ 💀 GAME OVER — Win/lose conditions!                 │
│   → Enemy reaches you = damage                     │
│   → Health drops to 0 = game over screen           │
│   → Kill all enemies = you win!                    │
└────────────────────────────────────────────────────┘
```

---

## 🧠 Review: What You Built Today

### New C# Concepts

| Concept | What It Does | Example |
|---------|-------------|---------|
| **`enum`** | Defines a custom type with a fixed list of values | `enum EnemyState { Patrol, Chase }` |
| **`switch` / `case`** | Runs different code blocks depending on a value | `switch (currentState) { case Patrol: ... break; }` |
| **`break;`** | Exits the current switch case (required!) | Used at the end of each `case` |
| **`GameObject.FindWithTag()`** | Finds an object in the scene by its tag | `GameObject.FindWithTag("Player")` |

### New Unity Concepts

| Concept | What It Is | Why It Matters |
|---------|-----------|----------------|
| **State Machine** | A system that can be in one "mode" at a time and switches between modes | The enemy is either Patrolling or Chasing — never both |
| **Detection Range** | An invisible circle around the enemy that triggers behavior changes | Determines when the enemy "sees" the player |
| **Hysteresis** (detect ≠ lose range) | Using different thresholds for entering and leaving a state | Prevents rapid flickering between states |
| **Tags** | Labels you assign to GameObjects in Unity | Used by `FindWithTag()` to locate specific objects |
| **Game Balance** | Tuning numbers to make the game fun (not too easy, not too hard) | Detection range, speeds, lose range all affect difficulty |

### What Code Changed

| File | Status | Notes |
|------|--------|-------|
| PlayerController.cs | ❌ No change | Untouched this week |
| NPCPatrol.cs | 🔄 Replaced | Removed from NPC (file kept for reference) |
| **EnemyAI.cs** | ⚙️ **NEW!** | Patrol + Chase state machine with detection |
| NPCAnimator | 🔄 Updated | Added Run clip to Blend Tree (optional) |

### How NPCPatrol → EnemyAI Evolved

```
NPCPatrol.cs (Week 10):              EnemyAI.cs (Week 11):
┌─────────────────────────┐           ┌─────────────────────────┐
│ Walk between A and B    │           │ Walk between A and B    │
│ One speed               │     →→→   │ Two speeds (patrol/chase)│
│ No awareness of player  │           │ Detects player by range │
│ No state machine        │           │ enum + switch states    │
│ Always patrols          │           │ Patrols OR chases       │
└─────────────────────────┘           └─────────────────────────┘
```

---

## ✅ Week 11 Complete!

### What You Accomplished:
- ✅ **Learned** what state machines are and why games use them everywhere
- ✅ **Learned** enums — a way to define a list of named states in C#
- ✅ **Learned** switch statements — a clean way to run code based on a state
- ✅ **Built** an enemy AI that patrols AND chases the player!
- ✅ **Learned** `FindWithTag()` to locate the player in the scene
- ✅ **Learned** about hysteresis (detect vs lose range) and why it prevents flickering
- ✅ **Experimented** with game balance and difficulty tuning

### The Before & After:

```
Start of Week 11:                    End of Week 11:
┌────────────────────────┐           ┌────────────────────────┐
│ NPC patrols A ↔ B      │           │ Enemy patrols A ↔ B    │
│ Ignores you completely │    →→→    │ DETECTS you when close! │
│ No state changes       │           │ CHASES you at speed!   │
│ Basically furniture    │           │ Gives up if you escape │
│                        │           │ Has a BRAIN! 🧠         │
│ "Not scary at all"     │           │ "RUN RUN RUN!!!"       │
└────────────────────────┘           └────────────────────────┘
```

---

## 💡 Homework Challenges (Optional)

1. **Add a THIRD state: Idle** — When the enemy reaches a patrol point, make it STOP and wait 2-3 seconds before walking to the next point. (Hint: you'll need a timer variable and a new `EnemyState.Idle` value in the enum!)

2. **Add a second enemy** — Duplicate the NPC and patrol points. Give the second enemy DIFFERENT speeds and detection ranges. Maybe one is fast but blind (small detection), and the other is slow but all-seeing (huge detection)!

3. **Sound effect hint** — Right now the only way to know the enemy spotted you is the Console log. What if the screen tinted red? Or a sound played? Think about what would make the chase FEEL scarier. (We'll build this later!)

4. **Think ahead** — The enemy chases you but... then what? It reaches you and runs past. What SHOULD happen when the enemy catches you? (Hint: Health system and damage!)
