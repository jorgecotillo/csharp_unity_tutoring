# Week 12: Troubleshooting Your Enemy & Adding Health! ❤️🔧

⚠️ **IMPORTANT:** This continues directly from Weeks 10-11. You should have:
- ✅ A humanoid player character with walk/run animations (Blend Tree)
- ✅ PlayerController script working with WASD + Shift to sprint
- ✅ Third-person camera working
- ✅ An **EnemyAI script** that patrols between Point A → Point B
- ✅ The enemy **chases** when the player gets close (state machine with enums!)
- ⚠️ **Known issue:** Some students have enemies that just walk in tiny circles — we fix that first!

---

## 🎯 What You'll Build This Week

Two big wins today:

1. **Fix the "spinning enemy" bug** — We'll debug together like detectives 🔍
2. **Add a Health System** — The enemy can now HURT you when it gets close!

```
BEFORE (start of this week):           AFTER (end of this week):

  🧟 walks in a tiny circle               🧟 patrols A ──→ B ──→ A properly!
     never chases you                      🧟💨 CHASES you when close!
     "Is it broken?"                       🏃💔 Player LOSES HEALTH on contact!
                                           ❤️❤️❤️ → ❤️❤️ → ❤️ → 💀

  "Why is my enemy dumb?"              "My enemy is DANGEROUS now!"
```

### New C# Concepts Today:

1. **Debugging techniques** — How to find and fix bugs like a real developer
2. **Collider triggers** — Detecting when two objects touch (OnTriggerEnter/Stay)
3. **Health systems** — Tracking HP, taking damage, and dying

**Teaching style:** We explain a concept, then immediately code it. No long lectures — learn by doing! 🔧

---

## 📅 Week 12 Structure (60 minutes)

| Time | Part | What You'll Do |
|------|------|----------------|
| 0-20 min | **Part 1** | 🔧 Troubleshoot & fix the spinning/circling enemy |
| 20-25 min | **Part 2** | 🎓 Concept: Colliders, Triggers & Damage |
| 25-45 min | **Part 3** | 🖐️ Hands-On: Build a Health System |
| 45-55 min | **Part 4** | 🎮 Test, tweak & make it fun |
| 55-60 min | **Part 5** | 👀 Preview: What's coming next! |

---

## Part 1: Troubleshooting the Enemy (20 minutes)

### 🔧 The Problem: "My Enemy Walks in a Tiny Circle!"

Warren reported that his patrol enemy just walks in a small circle instead of patrolling between Point A and Point B, and never chases the player. Let's become **bug detectives** 🕵️ and figure out what went wrong!

```
What Warren SEES:                  What SHOULD happen:

     ╭──╮                          A ·────── 🧟 ──────· B
  🧟 │  │  (tiny circle)               patrol back and forth
     ╰──╯                               chase when player is close!

  "It's moving... but wrong"        "It should walk a LONG path!"
```

### 🎓 How Real Developers Debug (3 minutes)

Before we look at Warren's specific bug, let's talk about HOW to debug. Real game developers don't randomly change code and hope it works — they follow a process:

```
The Debugging Process:
┌──────────────────────────────────────────────────────────┐
│ 1. OBSERVE  — What exactly is happening? Be specific!    │
│ 2. THEORIZE — What COULD cause this behavior?            │
│ 3. TEST     — Check each theory one by one               │
│ 4. FIX      — Change only what's broken                  │
│ 5. VERIFY   — Does the fix actually work?                │
└──────────────────────────────────────────────────────────┘

GOLDEN RULE: Change ONE thing at a time!
If you change 5 things at once and it works,
you don't know WHICH change fixed it.
```

> 💡 **Pro tip:** The Console window (`Window → General → Console`) is your best friend. Unity prints warnings and errors there. ALWAYS check the Console first when something goes wrong!

---

### 🕵️ The Investigation: Why Does the Enemy Circle?

Let's think about what makes the enemy patrol. It needs:
1. **Point A** — a Transform in the scene
2. **Point B** — another Transform in the scene
3. **The NPC** — with the EnemyAI script that walks between them

If the enemy is walking in a **tiny circle**, that tells us something very specific about the math:

```
Normal patrol — Points are FAR APART:

  A (12, 0, 15) ─────────────────── B (20, 0, 15)
                   8 units apart
  🧟 walks a long straight path back and forth ✅


Tiny circle — Points are TOO CLOSE or ON TOP OF EACH OTHER:

  A≈B (15, 0, 15)    ← Both points at (nearly) the same position!
     ╭──╮
  🧟 │  │   The NPC arrives immediately, swaps target, arrives again...
     ╰──╯   It looks like a circle because of the smooth rotation!
```

> 💡 **The smooth rotation (`Quaternion.Slerp`) makes it circle instead of jitter.** If we didn't have smooth rotation, the NPC would just vibrate in place. The Slerp makes it arc gracefully — which looks like a tiny circle!

---

### 🔍 Debug Checklist: Walk Through These WITH the Student

Go through each check together. **Stop at the first problem you find** — that's probably the fix!

#### ✅ Check 1: Are Point A and Point B Actually ASSIGNED in the Inspector?

**What to check:** Select the **NPC** (or Enemy) object in the Hierarchy. Look at the **EnemyAI** component in the Inspector.

```
Inspector — EnemyAI component:
┌──────────────────────────────────────────────┐
│ Enemy AI (Script)                             │
│                                               │
│ ── Patrol Points ──                           │
│ Point A:          [None (Transform)       ]   │  ← ❌ PROBLEM! Says "None"!
│ Point B:          [None (Transform)       ]   │  ← ❌ PROBLEM! Says "None"!
│                                               │
│ Should look like:                             │
│ Point A:          [PatrolPoint_A          ]   │  ← ✅ Has an object assigned
│ Point B:          [PatrolPoint_B          ]   │  ← ✅ Has an object assigned
└──────────────────────────────────────────────┘
```

🐛 **If they say "None":** The patrol points aren't assigned! The script has a safety check that returns early if they're null, so the enemy might just stand still — or if only the code paths change, it might use default position (0,0,0).

**Fix:**
1. Create two **Empty GameObjects** in the Hierarchy (`Right-click → Create Empty`)
2. Name them **PatrolPoint_A** and **PatrolPoint_B**
3. **Drag them** from the Hierarchy into the Point A and Point B slots

> 💡 **Common mistake:** Creating the GameObjects but forgetting to drag them into the Inspector slots. The script can't "see" objects in your scene unless you tell it where they are!

---

#### ✅ Check 2: Are Point A and Point B in DIFFERENT Positions? (MOST LIKELY CAUSE! ⭐)

**This is the #1 reason for the tiny circle bug!**

**What to check:** Click on **PatrolPoint_A** in the Hierarchy. Look at its **Transform → Position** in the Inspector. Then click on **PatrolPoint_B** and check its position too.

```
❌ WRONG — Both points at the same (or nearly same) position:

PatrolPoint_A → Position: X: 0,  Y: 0,  Z: 0
PatrolPoint_B → Position: X: 0,  Y: 0,  Z: 0     ← Same as A!

   A≈B at origin
      ↓
   (0, 0, 0) 🧟     The NPC has nowhere to go!
   It arrives instantly, swaps, arrives again = tiny circle!


✅ CORRECT — Points are spread apart:

PatrolPoint_A → Position: X: 12, Y: 0,  Z: 15
PatrolPoint_B → Position: X: 20, Y: 0,  Z: 15    ← 8 units away!

   A ·──────── 🧟 ────────· B
   (12,0,15)              (20,0,15)
   Nice long patrol path!
```

🐛 **Why does this happen?** When you create an Empty GameObject, Unity places it at the **origin (0, 0, 0)** by default. If you create both patrol points and forget to move them, they're both at (0, 0, 0) — right on top of each other!

**Fix:**
1. Select **PatrolPoint_A** → set Position to something like **X: -5, Y: 0, Z: 0**
2. Select **PatrolPoint_B** → set Position to something like **X: 5, Y: 0, Z: 0**
3. Now they're **10 units apart** — plenty of room to patrol!

> 💡 **Pro tip:** A good patrol distance is **5-15 units**. Less than 2 units and the NPC barely moves. More than 20 and the patrol takes forever.

**🖐️ Fun exercise:** Move Point A and Point B around in the Scene view while the game is running! You'll see the NPC update its path in real-time. This is a great way to understand how the patrol works.

---

#### ✅ Check 3: Are the Patrol Points CHILDREN of the NPC? (Sneaky Bug! 🐛)

**What to check:** Look at the Hierarchy. Are the patrol points nested INSIDE the NPC?

```
❌ WRONG — Patrol points are CHILDREN of the NPC:

Hierarchy:
├── Player
└── NPC                    ← NPC moves around
    ├── Character Model
    ├── PatrolPoint_A      ← Moves WITH the NPC! 😱
    └── PatrolPoint_B      ← Moves WITH the NPC! 😱

The NPC walks toward Point B... but Point B moves with it!
The distance between NPC and Point B NEVER CHANGES!
Result: NPC walks forever in one direction or spins.


✅ CORRECT — Patrol points are at the ROOT level (siblings of NPC):

Hierarchy:
├── Player
├── NPC                    ← NPC moves around
│   └── Character Model
├── PatrolPoint_A          ← Stays put! NPC walks toward it.
└── PatrolPoint_B          ← Stays put! NPC walks toward it.
```

🐛 **Why does this happen?** When dragging GameObjects in the Hierarchy, it's easy to accidentally drop them INSIDE another object (making them a child). When a patrol point is a child of the NPC, it moves WITH the NPC — so the NPC is chasing a target that's always the same distance away. It's like a dog chasing its own tail! 🐕

**Fix:** Drag PatrolPoint_A and PatrolPoint_B OUT of the NPC in the Hierarchy. They should be at the same level as the NPC, NOT inside it.

> 💡 **Think of it this way:**
> - **Child objects** = things attached TO the NPC (like its body, weapons, hat)
> - **World objects** = things that exist independently (like landmarks, waypoints)
> - Patrol points are LANDMARKS in the world — they shouldn't move when the NPC moves!

---

#### ✅ Check 4: Is the NPC Starting Too Close to the Player?

**What to check:** If the NPC **does** patrol but never seems to reach the points, or immediately starts chasing, check the starting positions.

```
❌ WRONG — NPC starts inside the detection range:

  🧟 (15, 0, 0)    🏃 (18, 0, 0)     distance = 3 units
                                        detectionRange = 8

  3 < 8 → Enemy IMMEDIATELY chases! You never see it patrol!


✅ CORRECT — NPC starts FAR from the player:

  🧟 (15, 0, 15)    🏃 (0, 0, 0)      distance = ~21 units
                                        detectionRange = 8

  21 > 8 → Enemy patrols peacefully until you approach!
```

**Fix:** Move the NPC farther from the player, or move the Player to the origin (0, 0, 0) and put the NPC at least **10+ units** away.

---

#### ✅ Check 5: Is the Player Tagged as "Player"?

**What to check:** If the enemy patrols fine but **never chases**, the script might not be finding the player.

1. Select your **Player** object in the Hierarchy
2. Look at the very top of the Inspector
3. Check the **Tag** field

```
Inspector — Player object:
┌──────────────────────────────────────────────┐
│ Player                                        │
│ Tag: [Untagged ▼]  ← ❌ WRONG!              │
│                                               │
│ Should be:                                    │
│ Tag: [Player ▼]    ← ✅ CORRECT!             │
└──────────────────────────────────────────────┘
```

🐛 **If it says "Untagged":** The EnemyAI script uses `FindWithTag("Player")` to locate the player. If no object has the "Player" tag, the script can't find anyone to chase!

**Fix:** Click the Tag dropdown → select **"Player"**. It's a built-in Unity tag — no need to create it.

**Quick Console check:** Open the Console (Window → General → Console). If you see the warning `"No GameObject with tag 'Player' found in the scene."` — that confirms this is the problem!

---

#### ✅ Check 6: Did You Forget to REPLACE NPCPatrol with EnemyAI?

**What to check:** Select the NPC and look at the Inspector. Do you see **BOTH** scripts?

```
❌ WRONG — Both scripts are on the NPC:

Inspector:
┌──────────────────────────────────────────────┐
│ NPC Patrol (Script)      ← Old script!       │
│   Point A: PatrolPoint_A                      │
│   Point B: PatrolPoint_B                      │
│                                               │
│ Enemy AI (Script)        ← New script!        │
│   Point A: None          ← Not assigned!      │
│   Point B: None                               │
└──────────────────────────────────────────────┘

Two scripts fighting over the same NPC = chaos!
```

**Fix:**
1. Remove the **NPCPatrol** component (three dots ⋮ → Remove Component)
2. Make sure **EnemyAI** has Point A and Point B assigned
3. You only need ONE movement script on the NPC!

---

#### ✅ Check 7: Is the Character Model Offset from Its Parent?

**What to check:** Click on the **character model child** inside the NPC (e.g., the Mutant). Check its **local position**.

```
❌ WRONG — Character child is offset:
NPC (parent) at (15, 0, 15)
└── Character at (5, 0.5, 5)    ← The code thinks the enemy is at (15,0,15)
                                    but you SEE it at (20, 0.5, 20)!
                                    Distance calculations = WRONG.

✅ CORRECT — Character child at (0, 0, 0):
NPC (parent) at (15, 0, 15)
└── Character at (0, 0, 0)      ← Sits right on top of parent.
                                    What you see = what the code calculates. ✅
```

**Fix:** Select the character model child → right-click the Transform component → **Reset**. This sets its local position back to (0, 0, 0).

---

### 🏆 Quick Reference: Common Problems & Fixes

| Symptom | Most Likely Cause | Fix |
|---------|-------------------|-----|
| 🔄 Enemy walks in tiny circle | Point A and B at same position | Move them apart (5-15 units) |
| 🔄 Enemy circles but never arrives | Points are children of the NPC | Drag points OUT of the NPC hierarchy |
| 🧍 Enemy stands completely still | Point A or B not assigned (null) | Drag patrol points into Inspector slots |
| 🏃 Enemy chases immediately | NPC too close to player at start | Move NPC farther away (10+ units) |
| 🧟 Enemy patrols but never chases | Player not tagged "Player" | Tag dropdown → select "Player" |
| 🤪 Enemy moves but looks wrong | Character model offset from parent | Reset child's local position to (0,0,0) |
| ⚡ Two scripts fighting | Both NPCPatrol and EnemyAI active | Remove NPCPatrol, keep only EnemyAI |

> 💡 **Warren's bug was most likely Check 2** — Point A and Point B were both at (0, 0, 0) because they were created but never repositioned in the scene. This is the #1 mistake students make!

---

### 🖐️ Let's Fix It Together! (5 minutes)

**Do these steps with the student:**

1. Open the Unity project
2. In the Hierarchy, find the patrol point objects
3. Check their positions — are they different?
4. If not, reposition them:
   - **PatrolPoint_A:** X: -5, Y: 0, Z: 0
   - **PatrolPoint_B:** X: 5, Y: 0, Z: 0
5. Make sure they're NOT children of the NPC
6. Press Play and verify the NPC patrols correctly!

```
After the fix:

  A ·──────── 🧟 ────────· B         🏃 Player (far away)
  (-5,0,0)              (5,0,0)       (0,0,15)

  ✅ NPC patrols back and forth!
  ✅ Walk toward NPC → it should chase!
  ✅ Sprint away → it goes back to patrol!
```

**Celebrate the fix!** 🎉 Debugging is a skill — the more you do it, the faster you get. Real game developers spend 30-50% of their time debugging!

---

## Part 2: Concept — Colliders, Triggers & Damage (5 minutes)

### 🎓 How Do Objects "Touch" Each Other in Unity?

Right now our enemy chases the player, but nothing happens when it catches you. You just stand there while the enemy runs into you. Boring! Let's make the enemy **dangerous**.

In Unity, objects detect contact through **Colliders**. You've already used them — the CharacterController has a built-in capsule collider that stops you from walking through walls.

```
Two types of collider interaction:

  COLLISION (solid):                TRIGGER (pass-through):
  ┌──────┐  🧱  ┌──────┐           ┌──────┐  ~~~~  ┌──────┐
  │Player│ BUMP │ Wall │           │Player│ enter │Danger│
  │      │ ←──→ │      │           │      │ ──→   │ Zone │
  └──────┘      └──────┘           └──────┘       └──────┘
  Objects STOP each other.          Objects PASS THROUGH but
  Can't overlap.                    Unity tells you it happened!

  Used for: walls, floors,          Used for: damage zones, pickups,
  physical obstacles                detection areas, checkpoints
```

> 💡 **Key concept:** A **trigger collider** doesn't block movement — objects pass right through it. But Unity fires special methods when something enters the trigger, letting you run code (like dealing damage!).

### 🎓 The Three Trigger Methods

Unity gives you three methods for trigger interactions:

```csharp
// Called ONCE when something first enters the trigger
void OnTriggerEnter(Collider other) { }

// Called EVERY FRAME while something stays inside the trigger
void OnTriggerStay(Collider other) { }

// Called ONCE when something leaves the trigger
void OnTriggerExit(Collider other) { }
```

```
Timeline of a trigger interaction:

  🏃 approaches →  🏃 enters zone  →  🏃 stays inside  →  🏃 leaves
                        │                    │ │ │              │
                   OnTriggerEnter     OnTriggerStay (×3)  OnTriggerExit
                   (called once!)    (called every frame!) (called once!)
```

**For our damage system, we'll use `OnTriggerStay`** — the enemy damages you every frame you're touching it. The longer you stay near it, the more health you lose!

### 🎓 What's a Collider Component?

```
Collider types in Unity:

  Box Collider         Sphere Collider      Capsule Collider
  ┌──────────┐         ╭──────────╮         ╭──────╮
  │          │        ╱            ╲        │      │
  │  📦      │       │    ⚽        │       │  🧍  │
  │          │        ╲            ╱        │      │
  └──────────┘         ╰──────────╯         ╰──────╯
  Good for:            Good for:             Good for:
  Crates, walls        Pickups, range        Characters!
```

**For our enemy's damage zone, we'll add a Sphere Collider** set as a trigger. It'll act like an invisible bubble around the enemy — when the player steps inside, they take damage!

---

## Part 3: Hands-On — Build the Health System (20 minutes)

We're going to:
1. Create a **PlayerHealth** script (tracks HP, takes damage)
2. Create an **EnemyDamage** script on the enemy (deals damage on contact)
3. Add a trigger collider to the enemy

### Step 1: Create the PlayerHealth Script

**🖐️ Do these steps together:**

1. In your Project panel, navigate to `Assets/Scripts/`
2. Right-click → **Create → C# Script**
3. Name it exactly **`PlayerHealth`**
4. Double-click to open it, **select all → delete** the template code
5. Type this together, section by section:

---

**🎓 Section 1: The class header and fields (explain → type)**

```csharp
using UnityEngine;

/// <summary>
/// Week 12: Player Health System.
/// Tracks the player's HP. Other scripts call TakeDamage() to hurt the player.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Maximum health the player starts with")]
    public float maxHealth = 100f;

    [Header("Debug")]
    [Tooltip("Show health changes in the Console")]
    public bool showDebugInfo = true;

    // Current health — starts at maxHealth
    private float currentHealth;

    // Is the player still alive?
    private bool isDead = false;
```

> 💡 **Why `private float currentHealth` instead of public?** Because we don't want other scripts to directly SET the player's health to whatever they want (imagine a bug that sets health to -999!). Instead, we'll provide a `TakeDamage()` method — a controlled way to reduce health. This is called **encapsulation** — controlling how other code interacts with your data.

---

**🎓 Section 2: Start — initialize health (explain → type)**

```csharp
    void Start()
    {
        // Start at full health!
        currentHealth = maxHealth;

        if (showDebugInfo)
            Debug.Log($"Player health initialized: {currentHealth}/{maxHealth}");
    }
```

---

**🎓 Section 3: TakeDamage — the public method (explain → type)**

This is the most important part! Other scripts call this to hurt the player:

```csharp
    /// <summary>
    /// Call this from any script to deal damage to the player.
    /// Example: playerHealth.TakeDamage(10f);
    /// </summary>
    public void TakeDamage(float damage)
    {
        // Can't damage a dead player!
        if (isDead) return;

        // Reduce health
        currentHealth -= damage;

        if (showDebugInfo)
            Debug.Log($"Player took {damage} damage! Health: {currentHealth}/{maxHealth}");

        // Clamp health to 0 (can't go negative)
        // Mathf.Max returns the LARGER value — so Max(health, 0) ensures we never go below 0
        currentHealth = Mathf.Max(currentHealth, 0f);

        // Check for death
        if (currentHealth <= 0f)
        {
            Die();
        }
    }
```

> 💡 **`public void TakeDamage(float damage)`** — let's break this down:
> - `public` — any script can call this (the enemy needs to!)
> - `void` — doesn't return anything
> - `TakeDamage` — descriptive name (reads like English: "player.TakeDamage(10)")
> - `float damage` — how much damage to deal (a parameter!)
>
> This is a great example of **methods with parameters** — the same method works for different damage amounts. A weak enemy might call `TakeDamage(5)`, a boss might call `TakeDamage(50)`!

---

**🎓 Section 4: Die and public getters (explain → type)**

```csharp
    /// <summary>
    /// Called when health reaches 0. For now, just logs a message.
    /// Later we could add death animation, respawn, game over screen, etc.
    /// </summary>
    private void Die()
    {
        isDead = true;
        Debug.Log("☠️ PLAYER DIED! Game Over!");

        // For now, just stop the player from moving
        // We disable the PlayerController script so WASD stops working
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.enabled = false;
        }
    }

    // =========================================================
    // PUBLIC GETTERS — Let other scripts READ our health
    // =========================================================
    // These use "=>" (expression body) — a short way to write read-only properties
    // Other scripts can CHECK health but can't SET it directly!

    public float Health => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;
}
```

> 💡 **Ask the student:** "Why is `Die()` private but `TakeDamage()` is public?"
>
> Because nothing outside this script should be able to kill the player directly! Death should only happen as a RESULT of taking enough damage. It's like real life — you can't just "decide" someone is dead. They have to take enough damage first. `TakeDamage` is the controlled entry point; `Die` is the internal consequence.

---

### Step 2: Create the EnemyDamage Script

This script goes on the **enemy** and deals damage when touching the player.

1. Create a new C# script called **`EnemyDamage`**
2. Delete the template, type this together:

```csharp
using UnityEngine;

/// <summary>
/// Week 12: Enemy Damage Dealer.
/// Attach to the enemy. When the player enters the trigger collider,
/// this script finds the player's PlayerHealth and calls TakeDamage().
/// </summary>
public class EnemyDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Damage dealt PER SECOND while the player is touching the enemy")]
    public float damagePerSecond = 20f;

    /// <summary>
    /// OnTriggerStay is called EVERY FRAME while another collider stays inside our trigger.
    /// We use this instead of OnTriggerEnter because we want CONTINUOUS damage —
    /// the longer you stay near the enemy, the more health you lose!
    /// </summary>
    private void OnTriggerStay(Collider other)
    {
        // Only damage the player, not random objects!
        // We check the tag to make sure it's actually the player.
        if (!other.CompareTag("Player")) return;

        // Try to find the PlayerHealth script on the object (or its parent)
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null) return;

        // Deal damage scaled by time
        // damagePerSecond * Time.deltaTime = damage THIS FRAME
        //
        // Example at 60 FPS:
        //   20 damage/sec * 0.016 sec/frame = 0.33 damage per frame
        //   0.33 × 60 frames = ~20 damage per second ✅
        //
        // This makes damage consistent regardless of frame rate!
        playerHealth.TakeDamage(damagePerSecond * Time.deltaTime);
    }
}
```

> 💡 **`other.CompareTag("Player")`** — why not `other.tag == "Player"`?
>
> Both work, but `CompareTag` is slightly faster AND throws an error if the tag doesn't exist (helping you catch typos). `tag == "Player"` would silently fail with a wrong tag name. Always prefer `CompareTag` in Unity!

> 💡 **`GetComponentInParent<PlayerHealth>()`** — why "InParent"?
>
> The collider that touches our trigger might be on a **child object** of the Player (like the character model). `GetComponentInParent` searches the object AND all its parents — so it finds PlayerHealth even if it's on the Player parent, not the child that made contact.

---

### Step 3: Set Up the Trigger Collider in Unity

Now we need to add a **Sphere Collider** to the enemy and set it as a **trigger**.

**🖐️ Do these steps together:**

1. Select the **NPC/Enemy** object in the Hierarchy (the parent, where EnemyAI is)

2. Click **Add Component** → search for **"Sphere Collider"** → add it

3. In the Inspector, configure the Sphere Collider:
   - **Is Trigger:** ✅ **CHECK THIS BOX!** (This is the most important setting!)
   - **Radius:** 1.5 (how close the player needs to be to take damage)

   ```
   Inspector — Sphere Collider:
   ┌──────────────────────────────────────────────┐
   │ Sphere Collider                               │
   │                                               │
   │ ☑ Is Trigger    ← CHECK THIS! Critical!       │
   │ Center:  X: 0  Y: 1  Z: 0                    │
   │ Radius:  1.5                                  │
   └──────────────────────────────────────────────┘
   ```

   > ⚠️ **If you forget "Is Trigger"**, the collider will be SOLID. The player will bump into the enemy like a wall instead of passing through and taking damage!

4. Click **Add Component** → search for **"EnemyDamage"** → add it

5. Set **Damage Per Second** to 20 (default — you can tweak later!)

6. **ALSO:** The enemy needs a **Rigidbody** for triggers to work!
   - Add Component → **Rigidbody**
   - **Is Kinematic:** ✅ CHECK THIS!
   - **Use Gravity:** ❌ UNCHECK THIS!

   ```
   Inspector — Rigidbody:
   ┌──────────────────────────────────────────────┐
   │ Rigidbody                                     │
   │                                               │
   │ Mass:           1                             │
   │ Use Gravity:    ☐   ← UNCHECK!               │
   │ Is Kinematic:   ☑   ← CHECK!                 │
   └──────────────────────────────────────────────┘
   ```

   > 💡 **Why Kinematic?** A kinematic Rigidbody doesn't respond to physics (gravity, forces) but still participates in trigger detection. Without it, Unity won't fire `OnTriggerStay`. We set it to kinematic because the EnemyAI script controls the enemy's movement — we don't want physics interfering!
   >
   > **Unity's trigger rule:** For `OnTriggerStay` to fire, at least ONE of the two objects involved must have a Rigidbody!

### Step 4: Add PlayerHealth to the Player

1. Select the **Player** object in the Hierarchy
2. Click **Add Component** → search for **"PlayerHealth"** → add it
3. Set **Max Health** to 100 (default)
4. Leave **Show Debug Info** checked ✅ (we want to see damage in the Console!)

```
Inspector — Player:
┌──────────────────────────────────────────────┐
│ Player Controller (Script)    ← existing     │
│ Character Controller          ← existing     │
│ Player Health (Script)        ← NEW!         │
│   Max Health:      100                       │
│   Show Debug Info: ☑                         │
└──────────────────────────────────────────────┘
```

---

## Part 4: Test, Tweak & Make It Fun (10 minutes)

### 🎮 Test It!

1. **Press Play**
2. Walk your player toward the enemy
3. Let the enemy chase you and touch you
4. Watch the Console — you should see damage messages!

```
Testing the damage system:

  1. Walk toward the enemy:
     A ←── 🧟 ──→ B        🏃 (you approach)

  2. Enemy detects you and chases:
     🧟💨💨💨 → 🏃           Console: "Player detected! Switching to CHASE!"

  3. Enemy reaches you — DAMAGE!
     🧟🏃                    Console: "Player took 0.33 damage! Health: 99.67/100"
                              Console: "Player took 0.33 damage! Health: 99.34/100"
                              (repeating every frame while touching!)

  4. Health reaches 0:
                              Console: "☠️ PLAYER DIED! Game Over!"
                              Player can't move anymore!

  5. Sprint away before dying:
     🧟         🏃💨💨        Console: "Lost the player. Back to PATROL."
                              You survived with low health!
```

### 🎮 Tweak the Values!

**Challenge the student to find the "fun zone":**

| Setting | Too Low | Just Right | Too High |
|---------|---------|------------|----------|
| `damagePerSecond` | 5 (barely hurts) | 15-25 (tense!) | 100 (instant death) |
| `detectionRange` | 3 (blind enemy) | 8-10 (fair) | 30 (impossible to avoid) |
| `chaseSpeed` | 2 (easy to outrun) | 4-5 (need to sprint) | 10 (unfair!) |
| `Trigger Radius` | 0.5 (must be exact) | 1-2 (feels right) | 5 (damage from far away) |

> 💡 **The golden rule of game design:** A mechanic should be **challenging but fair**. The player should feel like they CAN survive if they play well, but they're in real danger if they mess up!

### 🧪 Fun Experiments (try any that sound interesting!)

1. **Survive for 30 seconds** — Can you stay in the enemy's patrol area without dying?
2. **The damage sprint** — Walk into the enemy, then sprint away. How much health did you lose?
3. **Multiple enemies** — Duplicate the enemy (Ctrl+D). Give them different patrol routes. Can you navigate between them?
4. **Speed demon** — Set chaseSpeed to 6 (same as your sprint). Now you can NEVER outrun it! How long can you survive? 😱

---

## Part 5: Preview — What's Coming Next! (2 minutes)

### 👀 Next Week: Fighting Back — Raycasting Weapons! 🔫

Right now you can only RUN from the enemy. Next week, we fight back!

```
Next week:

  🏃🔫 ──── laser ────→ 🧟💥    "Take THAT!"

  New concepts:
  • Raycasting — shooting an invisible laser from the camera
  • Enemy health — enemies can die too!
  • Input Actions — left-click to fire
  • Visual feedback — muzzle flash, hit effects
```

We'll learn about **Raycasting** — Unity's way of shooting an invisible line from one point in a direction and detecting what it hits. It's the foundation of EVERY shooter game!

```
How a raycast works:

  Camera 📷 ─────── RAY ──────→ hits 🧟 Enemy!
                                  │
                              "What did I hit?"
                              → It's an Enemy!
                              → Call TakeDamage(25)!
                              → Enemy health: 75/100
```

**Homework (optional but fun!):**
- Think about what should happen when an enemy's health reaches 0
- Should it disappear? Ragdoll? Play a death animation?
- How many hits should it take to kill an enemy? (hint: balance between too easy and too hard!)

---

## 📋 Week 12 Summary — What You Learned

| Concept | What It Means | Where We Used It |
|---------|---------------|------------------|
| **Debugging process** | Observe → Theorize → Test → Fix → Verify | Fixing the spinning enemy |
| **Trigger Colliders** | Detect overlap without blocking movement | Enemy damage zone |
| **OnTriggerStay** | Runs every frame while two triggers overlap | Continuous damage |
| **Encapsulation** | Controlling access with public/private | `TakeDamage()` is public, `Die()` is private |
| **GetComponentInParent** | Search up the hierarchy for a component | Finding PlayerHealth from a child collider |
| **CompareTag** | Safe, fast way to check object tags | Checking if the collider is the Player |
| **Kinematic Rigidbody** | Rigidbody that doesn't respond to physics | Required for trigger detection |

### 📂 Files Created/Modified This Week

| File | What It Does |
|------|-------------|
| `PlayerHealth.cs` | **NEW** — Tracks player HP, handles damage & death |
| `EnemyDamage.cs` | **NEW** — Deals damage when enemy touches the player |

### 🎮 Your Game So Far

```
Week 1-3:   Learned Unity basics, input, physics
Week 4-5:   Player moves with WASD + sprint + gravity ✅
Week 8:     Third-person camera ✅
Week 9:     Character animations (Blend Trees) ✅
Week 10:    NPC patrol between points ✅
Week 11:    Enemy AI with state machine (patrol + chase) ✅
Week 12:    Fixed patrol bugs + Player health system ✅  ← YOU ARE HERE!
Week 13:    🔫 Shoot the enemies! (raycasting)
```
