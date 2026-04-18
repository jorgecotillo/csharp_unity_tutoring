# Week 10: Fixing Animations & Your First Enemy 🧟

⚠️ **IMPORTANT:** This continues directly from Weeks 8-9. You should have:
- ✅ A humanoid character from Mixamo with Animator Controller
- ✅ A Blend Tree set up with Idle, Walk, and Run animations
- ✅ PlayerController script with `UpdateAnimator()` sending Speed to the Animator
- ✅ Third-person camera working
- ⚠️ **Known issue from last week:** Character may still be "sliding" — we fix that first!

---

## 🎯 What You'll Build This Week

Two big wins today:

1. **Fix the sliding character** — We'll debug together like real game devs
2. **Add your first ENEMY** — An enemy that patrols around AND chases you when you get close!

```
BEFORE (this week starts):           AFTER (this week ends):

    O                                     O
   /|\  ← slides around                 /|\  ← walks & runs properly!
   / \    no animation change            / \
                                    
   Empty, boring world               🧟 Enemy patrols...
                                     🧟💨 ...then CHASES you!
                                    
   "Why is this broken?"             "This feels like a real game!"
```

**Teaching style today:** We explain a concept, then immediately code it. No long lectures — learn by doing! 🔧

---

## 📅 Week 10 Structure (60 minutes)

| Time | Part | What You'll Do |
|------|------|----------------|
| 0-15 min | **Part 1** | 🔧 Troubleshoot & fix the sliding animation |
| 15-20 min | **Part 2** | 🎓 Quick concept: What makes an enemy? (State machines) |
| 20-35 min | **Part 3** | 🖐️ Hands-On: Download & set up the enemy character |
| 35-50 min | **Part 4** | 🖐️ Hands-On: Write the EnemyAI script (patrol + chase!) |
| 50-55 min | **Part 5** | 🎮 Test & tweak the enemy |
| 55-60 min | **Part 6** | 🔫 Equip a REAL gun from the Low Poly Guns asset pack! |

---

## Part 1: Troubleshooting the Sliding Animation (15 minutes)

### 🔧 The Problem

Your character moves with WASD, but instead of playing walk/run animations, it **slides around in the idle pose** like a mannequin on a skateboard. Let's fix it!

### 🕵️ What We Already Know

We tested the Animator: when we **manually change the Speed parameter** in the Animator window, the preview correctly shows Idle → Walk → Run. That tells us a LOT:

```
┌─────────────────────────────────────────────────────────────┐
│  What we tested:                                            │
│                                                             │
│  Animator window → Parameters → Speed → type 0, 3, 6       │
│  Preview shows: Idle ✅  Walk ✅  Run ✅                    │
│                                                             │
│  ✅ Blend Tree is set up correctly                          │
│  ✅ Animation clips are assigned                            │
│  ✅ Thresholds are correct                                  │
│  ✅ Parameter name "Speed" exists                           │
│                                                             │
│  ❌ But during GAMEPLAY, character still slides...          │
│                                                             │
│  CONCLUSION: The Animator setup is FINE.                    │
│  The problem is on the CODE → ANIMATOR connection!          │
│  Something prevents the Speed value from reaching the       │
│  Animator when the game is actually running.                │
└─────────────────────────────────────────────────────────────┘
```

This is how real debugging works — **eliminate what IS working** to narrow down what's broken. Since the Animator itself is fine, there are only a few things left to check.

### 🎓 How Debugging Works (2 minutes)

Real game developers spend a LOT of time debugging. The trick is to work **backwards from what you see** to find what's broken. We already ruled out the Animator, so our suspects are:

```
What you SEE:                    Remaining suspects:
┌──────────────────┐             ┌──────────────────────────────┐
│ Character slides │             │ 1. Code not sending Speed    │
│ in idle pose     │  ←── ?! ── │    (UpdateAnimator missing)  │
│ when moving      │             │ 2. animator variable is null │
│                  │             │    (can't find Animator)     │
│ BUT preview      │             │ 3. Apply Root Motion is ON  │
│ works fine!      │             │ 4. Wrong Animator Controller │
│                  │             │    on the scene object       │
└──────────────────┘             └──────────────────────────────┘
```

We'll check each one together. **Open your Unity project — let's debug!**

---

### 🔍 Debug Checklist (Work through these WITH the student)

Go through these checks one by one. **Stop at the first problem you find** — that's probably the fix!

#### ✅ Check 1: Is `UpdateAnimator()` Being Called?

**What to check:** Open `PlayerController.cs` and look at the `Update()` method.

```csharp
void Update()
{
    HandleMovement();
    HandleGravity();
    ApplyMovement();
    UpdateAnimator();  // ← IS THIS LINE HERE?
    
    if (showDebugInfo) { DebugInfo(); }
}
```

**Also check:** Does the `UpdateAnimator()` method exist in the script?

```csharp
private void UpdateAnimator()
{
    if (animator == null) return;
    animator.SetFloat("Speed", CurrentSpeed, 0.15f, Time.deltaTime);
}
```

🐛 **Common mistake:** The method exists but isn't called from `Update()`. Or it's called but the method body is missing/empty.

**Quick test:** Add a temporary Debug.Log to verify it's running:
```csharp
private void UpdateAnimator()
{
    if (animator == null) return;
    Debug.Log($"Speed being sent to animator: {CurrentSpeed}");  // TEMPORARY!
    animator.SetFloat("Speed", CurrentSpeed, 0.15f, Time.deltaTime);
}
```

Press Play, move around. Do you see speed values in the Console?
- **YES, numbers appear** → Code is fine, problem is in the Animator (skip to Check 3)
- **NO output at all** → The method isn't being called (fix `Update()`)
- **Speed is always 0** → Movement isn't calculating speed properly (check `CurrentSpeed`)

> **🧹 Remember:** Remove the Debug.Log after you find the problem! Debug logs in Update() run 60+ times per second and can slow things down.

---

#### ✅ Check 2: Is the Animator Reference Found?

**What to check:** In `Awake()`, is the animator being found?

```csharp
void Awake()
{
    // ... other code ...
    
    animator = GetComponentInChildren<Animator>();  // ← IS THIS HERE?
    
    if (animator == null)
    {
        Debug.LogWarning("No Animator found! Animations won't play.");
    }
}
```

**Press Play** and look at the Console. Do you see the warning "No Animator found"?

🐛 **If you see the warning**, the Animator component can't be found. Check:

```
Your hierarchy should look like:
   Player (this script is here)        ← GetComponentInChildren searches here...
   └── YBot (Animator is here)         ← ...AND here! ✅

NOT like this:
   Player (this script is here)        ← Script is here
   YBot (Animator is here)             ← But character is a SIBLING, not a child! ❌
```

**Fix:** Make sure the character model is a **child** of the Player object. Drag it onto the Player in the Hierarchy.

🐛 **Another common cause:** `GetComponent<Animator>()` (without `InChildren`) only searches the Player object itself, NOT children. Make sure it says `GetComponentInChildren<Animator>()`.

---

#### ✅ Check 3: Is the Animator Controller & Avatar Assigned on the SCENE Object?

> ⚠️ **This is subtle and easy to miss!** The Blend Tree preview you tested works because you're looking at the **Animator Controller asset file** directly. But the character **in your actual scene** might have a DIFFERENT controller assigned — or none at all!

**What to check:** Select the **character model child** (e.g., YBot) **in the Hierarchy** (not in the Project panel!). Look at the **Animator** component in the Inspector.

```
Inspector — Animator Component (on the SCENE object):
┌──────────────────────────────┐
│ Animator                     │
│                              │
│ Controller: [PlayerAnimator] │ ← Is THIS the same controller you previewed?
│                              │     Or does it say "None"?
│                              │     Or is it a different one?
│ Avatar:     [YBotAvatar    ] │ ← Is something here? Or "None"?
│ Apply Root Motion: ☐         │ ← We already unchecked this ✅
│                              │
└──────────────────────────────┘
```

🐛 **Controller is "None":** The scene character has no controller! Drag your `PlayerAnimator` asset into this field.

🐛 **It's a different controller:** Maybe there are two Animator Controllers in your project and the wrong one is assigned. Make sure it's the one with your Blend Tree.

🐛 **Avatar is "None":** The character doesn't know its own skeleton. Click ⊙ and select the avatar that matches your character (e.g., `YBotAvatar`).

---

#### ✅ Check 4: Is "In Place" Enabled on the Animations?

If you downloaded animations from Mixamo WITHOUT checking "In Place," the animation itself tries to move the character forward. Combined with our code also moving the character, this causes weird sliding or double-speed movement.

**How to tell:** Preview the animation clip. If the character walks forward and leaves the preview area, "In Place" was NOT checked.

**Fix:** Re-download the animation from Mixamo with **"In Place" ☑️ checked**. Or, in the Animation tab of the FBX:
- Check **"Root Transform Position (Y)"** → Bake Into Pose ✅
- Check **"Root Transform Position (XZ)"** → Bake Into Pose ✅
- Click **Apply**

---

#### ✅ Check 5: Live Debugging in Play Mode (The Silver Bullet)

This is the **most powerful** debugging technique. We'll watch the Animator update in real-time while the game runs.

> ⚠️ **TWO GOTCHAS you MUST know:**
>
> 1. You must select the **character model child** (e.g., YBot), NOT the Player parent! The Animator window only shows live data for the selected object that has the Animator component.
>
> 2. **Keyboard input only works when the Game view has focus!** When you click on the Hierarchy or Animator window, the Game view loses focus and WASD stops working. Your character stops moving, so Speed goes back to 0. This is normal — you need to set up your view FIRST, then click back on the Game tab to move.

**Step by step:**

1. **Set up your layout BEFORE pressing Play:**
   - Open the **Animator window** (top menu: **Window → Animation → Animator**)
   - **Dock the Animator window** next to the Game tab so you can see BOTH at the same time
   
   ```
   Arrange your editor like this:
   ┌──────────────────────┬──────────────────────┐
   │                      │                      │
   │    GAME VIEW         │  ANIMATOR WINDOW     │
   │    (play here)       │  (watch Speed here)  │
   │                      │                      │
   └──────────────────────┴──────────────────────┘
   
   Drag the Animator tab next to the Game tab, then drag it
   to the side to split the view. This lets you see both!
   ```

2. **Press Play** in Unity

3. **In the Hierarchy, click on the character model CHILD** (e.g., YBot) to select it — this makes the Animator window show your controller

   ```
   Hierarchy (while game is running):
   
   ├── Player               ← ❌ DON'T select this!
   │   └── YBot             ← ✅ SELECT THIS ONE! (has the Animator)
   ├── Main Camera
   └── Ground
   ```

4. **Now click on the Game tab** to give it focus again — this is the key step!

   > ⚠️ After selecting YBot, the Game view lost focus. You MUST click back on the Game view (or the Game tab) before WASD will work again. The Animator window will keep showing YBot's data even after you click away from it.

5. **Press WASD to move** and **look at the Animator window at the same time:**

```
┌──────────────────────┬──────────────────────┐
│                      │ Parameters:          │
│   GAME VIEW          │ Speed: [3.0       ]  │
│   (you're pressing   │        ↑              │
│    W to move)        │  This number changes │
│                      │  as you move!        │
│   Character is       │                      │
│   walking! 🚶        │ Blend Tree state:    │
│                      │ ┌────────────────┐   │
│                      │ │ (progress bar  │   │
│                      │ │  loops — that's│   │
│                      │ │  normal)       │   │
│                      │ └────────────────┘   │
└──────────────────────┴──────────────────────┘
```

   You should see:
   - **Stand still** → Speed: **~0** (might flicker slightly — see note below)
   - **Walk (WASD)** → Speed: **~3**
   - **Sprint (Shift+WASD)** → Speed: **~6**

> 💡 **The progress bar on the Blend Tree state is NOT the blend indicator!** It just shows the animation clip looping through its frames. It filling up is normal. The **Speed number** in the Parameters tab is what matters.

> 💡 **Speed flickers/bounces slightly when standing still?** That's normal! Our gravity code constantly pushes the character down with a small force (`groundedGravity = -2`), which causes tiny micro-movements. The damping on `SetFloat` smooths values over time, but these micro-fluctuations prevent it from settling to a perfect 0. As long as the number stays **very small** (below ~0.5) and the character plays the **Idle animation**, everything is working fine. If it bothers you, the fix is to add a small threshold in `UpdateAnimator()`:
>
> ```csharp
> float speed = CurrentSpeed < 0.1f ? 0f : CurrentSpeed;
> animator.SetFloat("Speed", speed, 0.15f, Time.deltaTime);
> ```
>
> This snaps tiny values to exactly 0. But it's purely cosmetic — the Blend Tree already handles near-zero values as Idle.

6. **Optional deeper look:** While the game is running (and YBot is still selected from step 3), **double-click** the Blend Tree state in the Animator window to go inside it. You'll see the blend diagram with a **blue diamond marker** showing where on the Idle→Walk→Run spectrum you currently are. Click back on the Game tab and move — the marker should slide along as your speed changes!

**What the Speed parameter tells you:**

| Speed Value When Moving | Diagnosis |
|------------------------|-----------|
| **Stays at 0** even when pressing WASD | Code isn't sending speed to the Animator → go to **Check 6** |
| **Changes (0→3→6)** but character still slides | Blend Tree parameter might not be connected → re-check Check 3 |
| **Changes correctly** AND character animates | Everything works! ✅ |

---

#### ✅ Check 6: Deep Dive — Why Is Speed Stuck at Zero? (Pin It Down)

We've confirmed: the Blend Tree preview works, Apply Root Motion is off, the code has `UpdateAnimator()`, but Speed stays at 0 during gameplay. Time to add **targeted Debug.Log lines** to find exactly where the chain breaks.

**🖐️ Open `PlayerController.cs` and add these TEMPORARY debug lines:**

```csharp
private void UpdateAnimator()
{
    // DEBUG LINE 1: Is this method even running?
    Debug.Log(">>> UpdateAnimator is running!");
    
    if (animator == null)
    {
        // DEBUG LINE 2: Is the animator reference missing?
        Debug.Log(">>> PROBLEM: animator is NULL!");
        return;
    }
    
    // DEBUG LINE 3: What speed value are we sending?
    Debug.Log($">>> Sending Speed = {CurrentSpeed} to Animator");
    
    animator.SetFloat("Speed", CurrentSpeed, 0.15f, Time.deltaTime);
}
```

**Save the script, Press Play, move with WASD, then check the Console:**

| What You See in Console | What It Means | Fix |
|------------------------|---------------|-----|
| **Nothing at all** (no `>>>` messages) | `UpdateAnimator()` isn't running | See **6A** below |
| `>>> PROBLEM: animator is NULL!` | Script can't find the Animator | See **6B** below |
| `>>> Sending Speed = 0` (always 0 even when moving) | `CurrentSpeed` is always 0 | See **6C** below |
| `>>> Sending Speed = 3` (changes correctly) | Code is fine! Animator isn't receiving it | See **6D** below |

---

**6A: No debug output at all**

The `UpdateAnimator()` method isn't running. This means either:

1. **Compilation errors** — Check the Console for **red error messages**. If the script has ANY errors, Unity uses the LAST working version of the code, ignoring your changes!

   ```
   Console:
   ┌─────────────────────────────────────────────────┐
   │ 🔴 Assets/Scripts/PlayerController.cs(47,12):   │
   │    error CS1002: ; expected                     │
   │                                                 │
   │ ← THIS means your changes aren't being used!   │
   └─────────────────────────────────────────────────┘
   ```
   
   **Fix:** Read the error, fix the typo, save again. Common culprits: missing semicolons, mismatched braces, typos in method names.

2. **Script isn't attached to the Player** — Select the **Player** object in the Hierarchy. Is `PlayerController` listed as a component in the Inspector?

   ```
   Inspector — Player object:
   ┌────────────────────────────┐
   │ ✅ Transform               │
   │ ✅ Character Controller    │
   │ ✅ Player Controller       │ ← Is this here?
   │    (Script)                │
   └────────────────────────────┘
   ```

   If it's missing, click **Add Component** → search "PlayerController" → add it.

3. **Script is disabled** — Is there a **checkbox** next to "Player Controller" in the Inspector? Make sure it's ✅ checked.

---

**6B: `animator is NULL`**

The script runs but can't find the Animator component. Check:

1. **Is the character model a CHILD of the Player?**

   ```
   ✅ CORRECT:                      ❌ WRONG:
   Player                           Player
   └── YBot (child)                 YBot (sibling — not a child!)
   ```

   **Fix:** In the Hierarchy, drag YBot **onto** Player to make it a child.

2. **Does the character model have an Animator component?**
   
   Click on the character child (YBot) → look for **Animator** in the Inspector. If it's not there, click **Add Component → Animator**.

3. **Are there TWO character models?** Maybe there's an old capsule AND the Mixamo character. The script might find the wrong one.

---

**6C: Speed is always 0**

The script runs, finds the Animator, but `CurrentSpeed` is always 0. Look at how `CurrentSpeed` is defined:

```csharp
public float CurrentSpeed => moveDirection.magnitude;
```

If `moveDirection` is always `Vector3.zero`, speed will be 0. This means `HandleMovement()` isn't setting it. Add one more debug line:

```csharp
private void HandleMovement()
{
    Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
    Debug.Log($">>> Raw input: {input}");  // ADD THIS
    
    // ... rest of method ...
}
```

| Console Shows | Meaning | Fix |
|--------------|---------|-----|
| `Raw input: (0.0, 0.0)` even when pressing WASD | Input System isn't working | Check that `inputActions.Player.Enable()` is called in `OnEnable()`. Also check that the **PlayerInputActions** asset has WASD mapped to the Move action |
| `Raw input: (1.0, 0.0)` (values change with WASD) | Input works! But moveDirection gets zeroed out later | Check for a `moveDirection = Vector3.zero` that runs when it shouldn't. Maybe `cameraTransform` is null? |

---

**6D: Speed value is correct but Animator doesn't change**

This is rare but possible. The code sends the right Speed value, but the Animator ignores it. Check:

1. **Parameter name mismatch:** In the code it says `SetFloat("Speed", ...)`. In the Animator Parameters tab, is it **exactly** `Speed` (capital S)? Even an invisible space character will break it!

   **Fix:** Delete the parameter in the Animator and recreate it. Type `Speed` carefully.

2. **The scene might be using a DIFFERENT PlayerController script.** If you have multiple copies of `PlayerController.cs` in different folders, Unity might be using the wrong one.

   **Fix:** In the Inspector, look at the PlayerController component on your Player. Click the gear ⚙️ → "Edit Script" — does it open the right file?

---

**🧹 After fixing: REMOVE ALL Debug.Log LINES!**

Debug.Log in Update() runs 60+ times per second and will slow your game down. Once the animation works, delete every line that starts with `Debug.Log(">>>`).

---

### 🎯 Quick Reference: Most Likely Causes

Since the Blend Tree preview works, Apply Root Motion is off, AND the Speed parameter stays at 0 during gameplay:

| # | Problem | Fix | Likelihood |
|---|---------|-----|-----------|
| 1 | **Compilation errors** hiding in Console | Fix the red errors — Unity ignores code changes until ALL errors are fixed | ⭐⭐⭐⭐⭐ |
| 2 | `animator` is null (hierarchy issue) | Character model must be a **child** of Player, use `GetComponentInChildren` | ⭐⭐⭐⭐⭐ |
| 3 | `UpdateAnimator()` not called in `Update()` | Add the call in your Update() method | ⭐⭐⭐⭐ |
| 4 | Animator Controller not assigned **on the scene object** | Drag PlayerAnimator into Controller field on the character in the Hierarchy | ⭐⭐⭐ |
| 5 | Input System not enabled / not mapped | Check `inputActions.Player.Enable()` in `OnEnable()`, verify WASD bindings | ⭐⭐⭐ |
| 6 | `CurrentSpeed` always 0 (`cameraTransform` is null) | Assign the camera in Inspector, or verify auto-find in `Awake()` | ⭐⭐ |

> ✅ **Already ruled out:** Apply Root Motion (unchecked, still slides) and Blend Tree setup (preview works perfectly).

**Use the Debug.Log technique from Check 6 to pinpoint exactly where the chain breaks!**

---

### ✅ Animation Fixed? Let's Move On!

Once your character properly transitions between Idle → Walk → Run, take a moment to appreciate what you built:

```
Your character now:
  Stand still  → 🧍 Idle (breathing/swaying)
  WASD         → 🚶 Walk (legs moving!)
  Shift + WASD → 🏃 Run  (full sprint!)
  
  All transitions are SMOOTH thanks to the Blend Tree! 🎉
```

**Now let's make the world more interesting — time for an NPC!** 🧍

---

## Part 2: What Is an NPC? (5 minutes)

### 🎓 Explain → Code Pattern

Before we write any code, let's understand the concept. Then we'll build it immediately!

### 🎓 What Is an NPC?

**NPC = Non-Player Character.** Any character in a game that isn't controlled by a human player.

Think about games you've played:

```
Examples of NPCs:
┌──────────────────────────────────────────────────────────┐
│ Minecraft:   Villagers that walk around and trade        │
│ Zelda:       Townspeople that give you quests            │
│ Fortnite:    AI enemies that patrol and shoot            │
│ GTA:         Pedestrians walking on sidewalks            │
│ Pokémon:     Trainers that challenge you when you pass   │
└──────────────────────────────────────────────────────────┘
```

NPCs make the world feel **alive**. Without them, your game is just an empty space with a player running around.

### 🎓 Our NPC: A Simple Patroller

Today we'll build the simplest type of NPC — one that **walks back and forth between two points**, forever.

```
Scene (top view):

     A ·──────────────· B
          NPC walks
          back and forth
          between A and B

     🏃 Player watches...     "Hey, there's someone in my game world!"
```

The NPC uses the **exact same animation system** as your player:
- Same Blend Tree concept (Speed parameter)
- Same `SetFloat("Speed", ...)` call
- But instead of reading keyboard input, the NPC follows a **script that tells it where to walk**

```
Player:                              NPC:
┌──────────────────┐                ┌──────────────────┐
│ Input: Keyboard  │                │ Input: Script     │
│ Speed: 0-6       │                │ Speed: 0-2        │
│ SetFloat("Speed")│                │ SetFloat("Speed") │
│ Blend Tree picks │                │ Blend Tree picks  │
│ the animation!   │                │ the animation!    │
└──────────────────┘                └──────────────────┘
     Same system, different input source!
```

**Got the concept? Let's build it!** 🔧

---

## Part 3: Hands-On — Set Up the NPC Character (15 minutes)

### Step 1: Download an NPC Character from Mixamo (3 min)

1. Go to **[mixamo.com](https://www.mixamo.com)** → **Characters** tab
2. Pick a **DIFFERENT** character than your player. Good picks:
   - **Mutant** — big, scary creature
   - **X Bot** — robot (if your player is Y Bot)
   - **Peasant Girl** — medieval character
   - **Zombie** — classic creepy NPC
3. Download: **FBX for Unity**, **T-Pose**

4. Also download a **Walking** animation for the NPC:
   - Click **Animations** tab → search **"Walking"**
   - Check **"In Place" ☑️** (very important!)
   - Download: **FBX for Unity**, **Without Skin**

   > We already have Idle from our player — humanoid animations work on ANY humanoid character (animation retargeting)!

### Step 2: Import & Configure the NPC (5 min)

This process should feel familiar from Week 8!

**🖐️ Do each step together:**

1. Create folder structure in Unity:
   ```
   Assets/Characters/NPC_Name/
   Assets/Characters/NPC_Name/Animations/
   ```
   (Replace NPC_Name with your character, e.g., `Mutant`)

2. **Drag** the NPC character FBX into `Assets/Characters/NPC_Name/`

3. **Click** the NPC FBX → **Rig** tab:
   - Animation Type: **Humanoid**
   - Click **Apply**

4. **Drag** the Walk animation FBX into the `Animations/` subfolder

5. **Click** the Walk animation FBX and configure:
   
   **Rig tab:**
   - Animation Type: **Humanoid**
   - Avatar Definition: **Copy From Other Avatar**
   - Source: Pick the **NPC's avatar** (not the player's!)
   - Click **Apply**
   
   **Animation tab:**
   - Loop Time: ✅ **Checked**
   - Click **Apply**

### Step 3: Create the NPC's Animator Controller (4 min)

Same process as the player — but let's do it together to reinforce the learning!

**🖐️ Do each step together:**

1. Right-click in the NPC's folder → **Create → Animator Controller** → name it **`NPCAnimator`**

2. **Double-click** to open the Animator window

3. Create a **Speed** parameter:
   - Click **Parameters** tab → **+** → **Float** → name it exactly **`Speed`**

4. Create the **Blend Tree**:
   - Right-click empty space → **Create State → From New Blend Tree**
   - Double-click the Blend Tree to go inside

5. Configure the Blend Tree:
   - **Parameter:** select `Speed`
   - Click **+** twice to add 2 motion slots
   - Assign:

   | Slot | Clip | Threshold |
   |------|------|-----------|
   | 1 | Idle (can use player's Idle — retargeting!) | **0** |
   | 2 | NPC's Walking clip | **2** |

   - **Uncheck** "Automate Thresholds"

   > **Why 2?** Our NPC will patrol at speed 2. The threshold must match the speed in the script!

   > **No Run needed** — the NPC is just patrolling, not sprinting. We only need Idle and Walk. You CAN add Run later if you want!

6. Go back to **Base Layer** (breadcrumb at top)

### Step 4: Place the NPC in the Scene (3 min)

**🖐️ Do each step together:**

1. In Hierarchy → right-click → **Create Empty** → name it **`NPC`**

2. **Drag** the NPC character FBX from the Project panel **onto** the NPC object (makes it a child)

   ```
   Hierarchy:
   ├── Player
   │   └── YBot
   ├── NPC              ← new empty parent
   │   └── Mutant       ← character model as child
   ├── Main Camera
   └── Ground
   ```

3. Select the **NPC character child** (e.g., Mutant) → Inspector → **Animator** component:
   - **Controller:** Drag in `NPCAnimator`
   - **Apply Root Motion:** ☐ **OFF** (our code handles movement!)

4. Position the NPC somewhere in your scene:
   - Try **X: 5, Y: 0, Z: 5** (near the player but not on top)

---

## Part 4: Hands-On — Write the NPCPatrol Script (15 minutes)

Now for the fun part — making the NPC walk on its own! We'll explain each piece, then code it together.

### 🎓 The Big Idea (1 minute)

The NPC needs to:
1. Pick a destination (Point B)
2. Walk toward it
3. When it arrives, turn around and walk to the other point (Point A)
4. Repeat forever!

```
   Point A              Point B
     ·──── walk ────→ ·
     · ←── walk ────── ·
     ·──── walk ────→ ·
     ... forever!
```

How does "walk toward a point" work in code? **Subtraction!**

```
target position - my position = direction to walk

Example: I'm at (2,0,2) and target is at (8,0,8):
  direction = (8,0,8) - (2,0,2) = (6,0,6)
  Normalize → (0.707, 0, 0.707)  ← unit vector pointing at target
  Multiply by speed × time       ← move that direction!
```

### Step 1: Create the Script

1. In `Assets/Scripts/`, right-click → **Create → C# Script** → name it **`NPCPatrol`**
2. Double-click to open it. **Replace everything** with the code below.

**We'll go section by section. Explain each block, then type it together:**

---

**🎓 First: The class and fields (explain → type)**

```csharp
using UnityEngine;

/// <summary>
/// Week 10: Simple NPC patrol between two points.
/// The NPC walks from Point A to Point B and back, forever.
/// Uses the same Blend Tree animation system as the Player!
/// </summary>
public class NPCPatrol : MonoBehaviour
{
    [Header("Patrol Points")]
    [Tooltip("First patrol destination (drag an Empty GameObject here)")]
    public Transform pointA;

    [Tooltip("Second patrol destination (drag an Empty GameObject here)")]
    public Transform pointB;

    [Header("Movement Settings")]
    [Tooltip("How fast the NPC walks (units per second)")]
    public float walkSpeed = 2f;

    [Tooltip("How close the NPC needs to get before turning around")]
    public float arrivalDistance = 0.5f;
```

> 💡 **Ask the student:** "What type is `pointA`?" Answer: `Transform` — it holds a position in the world. We'll create empty GameObjects as markers and drag them in.

> 💡 **Ask the student:** "Why is `arrivalDistance` 0.5 and not 0?" This is a great teaching moment about how games work:
>
> Games don't move objects in smooth continuous motion — they move in **discrete steps**, once per frame. Each frame, the NPC jumps forward a small amount:
>
> ```
> Target is at position 8.0
> 
> Frame 30: NPC at 7.96
> Frame 31: NPC at 7.992
> Frame 32: NPC at 8.024  ← OVERSHOT! Jumped right past 8.0!
> ```
>
> The NPC goes from 7.992 to 8.024 — it **never lands exactly on 8.0**. If we checked `position == target`, that would NEVER be true, and the NPC would walk past the point and keep going forever!
>
> `arrivalDistance = 0.5` is a **tolerance buffer** — it means "if you're within 0.5 units of the target, that's close enough — turn around." This is a common pattern in game development: **never check for exact equality with positions**, always use a tolerance.

---

**🎓 Next: Private variables and Start (explain → type)**

```csharp
    // Private variables
    private Transform currentTarget;   // Which point we're walking toward right now
    private Animator animator;          // Controls the NPC's animations (same concept as Player!)

    void Start()
    {
        // Start by walking toward Point B
        // (We assume the NPC starts near Point A)
        currentTarget = pointB;

        // Find the Animator on the character model (child object)
        // Same technique as Player — GetComponentInChildren!
        //
        // NPC hierarchy:
        //   NPC (this script)
        //   └── Character Model (Animator is here)
        animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogWarning($"{gameObject.name}: No Animator found! NPC won't animate.");
        }
    }
```

> 💡 **Recognition check:** "Have we seen `GetComponentInChildren` before?" Yes! The Player uses it too — same concept, same reason (Animator is on the child, not the parent).

---

**🎓 Next: Update — the main loop (explain → type)**

This is where the NPC does its thing every frame:

```csharp
    void Update()
    {
        // Safety check: can't patrol without both points!
        if (pointA == null || pointB == null)
        {
            if (animator != null) animator.SetFloat("Speed", 0f);
            return;
        }

        // STEP 1: Calculate direction to walk
        // (target position - my position) = direction vector pointing at target
        Vector3 direction = (currentTarget.position - transform.position);
        direction.y = 0;  // Keep movement flat (no flying NPCs!)
        direction.Normalize();  // Make it length 1 (just direction, no speed yet)
```

> 💡 **Ask the student:** "Why `direction.y = 0`?" Because the patrol points might be at slightly different heights. Without this, the NPC might try to fly up or burrow down! We only want horizontal movement.

> 💡 **Ask the student:** "What does `Normalize()` do?" Makes the vector length exactly 1. It keeps the direction but removes the magnitude — so distance to the target doesn't affect speed.

---

**🎓 Continue: Move, rotate, and check arrival (explain → type)**

```csharp
        // STEP 2: Move toward the target
        // position += direction * speed * time
        // direction = WHERE to go (unit vector)
        // walkSpeed = HOW FAST to go (units per second)
        // Time.deltaTime = makes it frame-rate independent
        transform.position += direction * walkSpeed * Time.deltaTime;

        // STEP 3: Face the direction we're walking
        if (direction.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                5f * Time.deltaTime
            );
        }
```

> 💡 **Recognition check:** "Have we seen `Quaternion.LookRotation` before?" Yes! The Player uses this exact same code for rotation. Same function, same Slerp for smooth turning.

---

**🎓 Continue: Arrival check and target swap (explain → type)**

```csharp
        // STEP 4: Check if we arrived at the target
        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
        if (distanceToTarget < arrivalDistance)
        {
            // Swap targets! If we were going to B, now go to A (and vice versa)
            // This is a TERNARY OPERATOR — same one from PlayerController!
            //   condition ? valueIfTrue : valueIfFalse
            currentTarget = (currentTarget == pointA) ? pointB : pointA;
        }
```

> 💡 **Ask the student:** "Can you read the ternary out loud in English?" Answer: "If currentTarget equals pointA, switch to pointB. Otherwise, switch to pointA."

> 💡 **New function: `Vector3.Distance()`** — Measures the straight-line distance between two points. Like using a ruler between two dots on paper!

---

**🎓 Finally: Update the Animator (explain → type)**

```csharp
        // STEP 5: Update the Animator (same concept as Player!)
        if (animator != null)
        {
            // Send walkSpeed to the Animator's "Speed" parameter
            // The NPC is always walking, so speed is always walkSpeed
            // The Blend Tree picks the Walk animation!
            animator.SetFloat("Speed", walkSpeed, 0.1f, Time.deltaTime);
        }
    }
}
```

> 💡 **Ask the student:** "Why do we send `walkSpeed` instead of calculating speed like the Player does?" Because the NPC is simple — it's ALWAYS walking at the same speed. No sprinting, no stopping (unless patrol points are missing). The Player has variable speed from input, but the NPC doesn't.

3. **Save** the script (Ctrl+S)

---

### Step 2: Create the Patrol Points

The NPC needs two points to walk between. We'll use Empty GameObjects as invisible markers:

**🖐️ Do each step together:**

1. In Hierarchy, right-click → **Create Empty** → name it **`PatrolPoint_A`**
2. Right-click → **Create Empty** → name it **`PatrolPoint_B`**
3. Position them in the scene:
   - **PatrolPoint_A:** X: 2, Y: 0, Z: 2
   - **PatrolPoint_B:** X: 8, Y: 0, Z: 8
   - (Or wherever you want — just spread them apart!)

   ```
   Scene (top view):
   
        A ·─────────── · B
              NPC walks
              back and forth
              between A and B
   
   Player spawns here: ★
   ```

> 💡 **Empty GameObjects are invisible** in the game but show up as icons in the Scene view. They're great for marking positions, spawn points, waypoints, etc.

### Step 3: Wire It All Up

1. Select the **NPC** parent object in the Hierarchy
2. Click **Add Component** → search for **NPCPatrol** → add it
3. In the Inspector, you'll see the NPCPatrol script with empty fields:
   - **Point A:** Drag `PatrolPoint_A` from the Hierarchy into this field
   - **Point B:** Drag `PatrolPoint_B` from the Hierarchy into this field
   - **Walk Speed:** 2 (default is fine)

   ```
   Inspector — NPCPatrol:
   ┌──────────────────────────────┐
   │ Point A: [PatrolPoint_A    ] │
   │ Point B: [PatrolPoint_B    ] │
   │ Walk Speed: [2             ] │
   │ Arrival Distance: [0.5    ] │
   └──────────────────────────────┘
   ```

---

## Part 5: Test & Tweak (8 minutes)

### 🎮 Test It!

1. **Press Play**
2. **Watch the NPC** — it should walk from Point A toward Point B!
3. When it arrives at Point B, it should **turn around** and walk back to Point A
4. **Walk your player over** to the NPC — two animated characters in the same world!

```
🎮 Your scene now:

  Player 🏃 ← you control with WASD
  
  NPC 🚶 →→→ · · · · · →→→ 🚶 →→→ · · · · · →→→  (patrols back and forth)
  
  Both animated! Both moving! Feels like a real game! 🎉
```

### 🐛 NPC Troubleshooting

| Problem | Fix |
|---------|-----|
| NPC doesn't move | Check that `PatrolPoint_A` and `PatrolPoint_B` are dragged into the script fields |
| NPC slides without walking animation | Check that NPCAnimator is assigned to the character child's Animator component |
| NPC walks but doesn't animate | Check that the NPC's Blend Tree has a Walk clip at threshold 2 |
| NPC walks through walls/ground | Expected! This basic script doesn't handle collision. That's OK for now! |
| NPC spins in circles | PatrolPoint_A and PatrolPoint_B might be at the same position — spread them apart! |

### 🎯 Tweaking Challenge

Try changing these values in the Inspector (NO code changes!) and see what happens:

| Experiment | What to Change | What Happens |
|------------|---------------|-------------|
| Speedy NPC | Walk Speed → 5 | NPC power-walks! |
| Lazy NPC | Walk Speed → 0.5 | Slow stroll |
| Short patrol | Move patrol points close together | NPC turns around quickly |
| Long patrol | Move patrol points far apart | NPC walks a long distance |
| Move patrol points while playing | Drag patrol points in Scene view | NPC changes path in real-time! |

---

## Part 6: Preview — What's Next! (2 minutes)

You built an NPC that walks around your world. But right now it **ignores the player completely** — it's basically animated furniture. Next week, we turn it into a real threat!

```
Coming in Week 11:
┌────────────────────────────────────────────────┐
│ 🧟 ENEMIES — Upgrade NPC to chase the player! │
│   → State Machines (Patrol ↔ Chase)            │
│   → Detection ranges (enemy spots you!)        │
│   → New C#: enums, switch statements           │
│                                                │
│ 🔫 EQUIP A GUN — Low Poly Guns asset pack!     │
│   → Attach a real weapon to your hand bone     │
│   → Your character looks dangerous!            │
│                                                │
│ Later:                                         │
│   → Shooting mechanics (raycasting)            │
│   → Health & damage systems                    │
│   → Win/lose conditions                        │
└────────────────────────────────────────────────┘
```

---

## 🧠 Review: What You Built Today

### New C# Concepts

| Concept | What It Does | Example |
|---------|-------------|---------|
| **`Vector3.Distance()`** | Measures distance between two points | `float dist = Vector3.Distance(a, b)` |
| **`transform.position +=`** | Move an object by adding to its position | `transform.position += direction * speed * Time.deltaTime` |
| **Ternary operator** (review) | One-line if/else | `target = (target == a) ? b : a` |
| **`GetComponentInChildren<T>()`** (review) | Find component on child objects | `animator = GetComponentInChildren<Animator>()` |
| **`animator.SetFloat()`** (review) | Send a number to the Animator | `animator.SetFloat("Speed", walkSpeed)` |

### New Unity Concepts

| Concept | What It Is | Why It Matters |
|---------|-----------|----------------|
| **NPC (Non-Player Character)** | A character controlled by code, not a human | Makes the game world feel alive |
| **Patrol Points** | Empty GameObjects used as invisible markers | Tell the NPC where to walk |
| **Empty GameObjects** | Invisible objects used for organization and markers | Useful for spawn points, waypoints, targets |
| **Animation Retargeting** (review) | Using one character's animations on a different character | Player's Idle works on the NPC too! |

### What Changed vs What Stayed the Same

| Component | Changed? | Notes |
|-----------|----------|-------|
| PlayerController.cs | ❌ No change | Fixed setup issues from previous weeks |
| Animator Controllers | ❌ No change | Fixed configuration issues |
| **NPCPatrol.cs** | ⚙️ **NEW!** | Simple patrol between two points |
| **NPCAnimator** | ⚙️ **NEW!** | Blend Tree for NPC (Idle/Walk) |
| **Patrol Points** | ⚙️ **NEW!** | Two empty GameObjects as markers |

---

## ✅ Week 10 Complete!

### What You Accomplished:
- ✅ **Debugged** the sliding animation issue like a real game developer
- ✅ **Learned** what NPCs are and why games need them
- ✅ **Built** an NPC that patrols back and forth with walking animation
- ✅ **Reinforced** Vector3 math, Quaternion rotation, Blend Trees, and GetComponentInChildren
- ✅ **Learned** Vector3.Distance for measuring distances between objects

### The Before & After:

```
Start of Week 10:                    End of Week 10:
┌────────────────────────┐           ┌────────────────────────┐
│ Character slides       │           │ Character walks & runs  │
│ around in idle pose    │    →→→    │ with smooth animations! │
│                        │           │                        │
│ Empty world            │           │ NPC patrols around     │
│ No other characters    │           │ World feels ALIVE!     │
│                        │           │                        │
│ "Is this even a game?" │           │ "This is getting real!" │
└────────────────────────┘           └────────────────────────┘
```

---

## 💡 Homework Challenges (Optional)

1. **Add a second NPC** — Duplicate the NPC and patrol points. Place two NPCs walking different paths around your scene!

2. **Adjust the feel** — Can you make one NPC walk fast and patrol a short path, and another walk slowly on a long path?

3. **Think ahead** — Right now the NPC ignores you completely. What SHOULD happen when you walk up to it? (Hint: Next week we add detection ranges and chase behavior!)

4. **Experiment** — What happens if you move the patrol points to the same spot? What if you set Walk Speed to 0? What if you set Arrival Distance to 5? Try weird things and see what breaks!
