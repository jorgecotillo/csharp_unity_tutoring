# Week 9: From Sliding to Striding — Walk & Run Animations 🏃

⚠️ **IMPORTANT:** This continues directly from Week 8. You should have:
- ✅ A humanoid character from Mixamo (replacing the old capsule)
- ✅ An Animator Controller with a basic Idle animation
- ✅ PlayerController script with WASD movement + sprint
- ✅ Third-person camera working
- ✅ Character "slides" around in idle pose when you move (that's the problem we're fixing!)

If anything is missing, go back to the Week 8 README and finish those steps first!

---

## � Fix: Character Looks White / No Textures (Read This First!)

If your character imported from Mixamo but looks **solid white** with no colors or detail, the textures are trapped inside the FBX file. Here's how to fix it:

### Step 1: Extract Materials

1. Click on your **character FBX file** in the Project panel (e.g., "Vanguard By T. Choonyung")
2. In the Inspector, click the **"Materials"** tab
3. Click **"Extract Materials..."** → save to your character's folder (e.g., `Assets/Characters/Vanguard/`)
4. Click **Apply**

### Step 2: Extract Textures

1. Same FBX still selected, same **"Materials"** tab
2. Click **"Extract Textures..."** → save to the same folder
3. Click **Apply**

You should now see texture files in your folder (like `vanguard_diffuse1`, `vanguard_normal`, `vanguard_specular`).

### 🎓 What Are These Texture Files? (The Paint Job Analogy)

Think of a 3D character like a **blank white action figure**. Textures are like **wrapping paper** that goes around it to make it look real. But there are different TYPES of wrapping — each one does a different job:

```
🎨 DIFFUSE (the color photo)          🧱 NORMAL (the bump faker)         ✨ SPECULAR (the shine map)
┌──────────────────┐                  ┌──────────────────┐              ┌──────────────────┐
│                  │                  │                  │              │                  │
│  The actual      │                  │  Fakes tiny      │              │  Controls what's │
│  colors & image  │                  │  bumps & dents   │              │  shiny vs dull   │
│  you see         │                  │  WITHOUT adding  │              │                  │
│                  │                  │  real geometry   │              │  Metal = bright  │
│  Like a photo    │                  │                  │              │  Cloth = dark    │
│  printed on the  │                  │  Like embossed   │              │  Skin = medium   │
│  character       │                  │  wallpaper — it  │              │                  │
│                  │                  │  LOOKS bumpy but │              │  Like choosing   │
│  🟢 MUST HAVE    │                  │  it's flat       │              │  glossy vs matte │
│                  │                  │                  │              │  wrapping paper   │
│                  │                  │  🟡 NICE TO HAVE │              │                  │
│                  │                  │                  │              │  🔵 OPTIONAL      │
└──────────────────┘                  └──────────────────┘              └──────────────────┘
Goes in: ALBEDO slot                  Goes in: NORMAL MAP slot          Goes in: METALLIC/SPECULAR slot
```

**Real-world comparison:**

| Texture | Real-World Example | Without It | With It |
|---------|-------------------|------------|---------|
| **Diffuse** | The paint on a car | White/blank character | Character has colors, camo, skin, clothes |
| **Normal** | Textured phone case — looks bumpy but is actually flat | Smooth, plastic-looking | Visible fabric, wrinkles, stitches, scars |
| **Specular** | Glossy vs matte sticker | Everything same shininess | Metal parts gleam, cloth stays dull |

**Bottom line:**
- **Diffuse = REQUIRED** (without it, everything is white)
- **Normal = recommended** (adds a LOT of visual detail for free)
- **Specular = optional** (subtle difference, skip if short on time)

### Why isn't this automatic?

FBX files are like a **ZIP file** — they pack everything together. Unity keeps things locked inside by default because:
- You might want to use your OWN textures instead
- You might re-download/update the FBX later (Unity doesn't want to overwrite your edits)
- The textures need to go into the right slots, and Unity can't always guess correctly

Think of it like unpacking a suitcase — the clothes come out, but YOU decide which drawer to put them in!

### Step 3: Assign Textures to Materials

The extracted materials might not auto-link to the textures. Fix manually:

1. **Click on your body material** (e.g., `VanguardBodyMat`) in the Project panel
2. In Inspector, find **Albedo** under Main Maps
3. **Drag** the diffuse texture (e.g., `vanguard_diffuse1`) from the Project panel into the **small square slot to the LEFT of Albedo**
4. Your character should now show its real colors!

**Optional (extra detail):**
5. **Drag** the normal texture (e.g., `vanguard_normal`) into the **Normal Map** slot
6. If Unity says *"This texture is not marked as a normal map"* → click **"Fix Now"**
7. Repeat for any other materials (e.g., `Vanguard_VisorMat`)

> 💡 **URP Projects:** If the Shader dropdown says `Standard` and your project uses URP (Universal Render Pipeline), change the Shader to **Universal Render Pipeline → Lit**. If you don't see that option, you're on Built-in Render Pipeline and `Standard` is correct.

> 💡 **Quick Alternative:** If textures aren't cooperating, create a new Material (right-click → Create → Material), pick a color you like, and drag it onto the character. Solid colors look great for a stylized game!

---

## �🎯 What You'll Build This Week

Right now your character **slides across the ground like a mannequin on a skateboard**. That's embarrassing. Today we fix it — BIG TIME.

```
BEFORE (Week 8):                    AFTER (Week 9):

    O                                    O
   /|\  ← frozen idle pose             /|\  ← legs actually moving!
   / \                                 / \
  ~~~~~~~~>  sliding on ice          🏃💨  walking & running!

  "Why is my character                "NOW we're talking!"
   ice skating??"
```

**By the end of this lesson, you'll have:**
- 🚶 **Walk animation** that plays when you move slowly
- 🏃 **Run animation** that plays when you sprint
- 🧍 **Idle animation** that plays when you stand still
- 🔄 **Smooth blending** between all three (no sudden jumps!)
- 🧟 **An NPC** walking around your scene (looks like a real game world!)
- 🔫 **BONUS:** Your character holding a gun!

**New C# code:** Only ~10 lines added to PlayerController. The magic is in Unity's Animator!

---

## 📅 Week 9 Structure (60 minutes)

| Time | Part | What You'll Do |
|------|------|----------------|
| 0-3 min | **Part 1** | The Skating Problem — Why it happens |
| 3-10 min | **Part 2** | 🎓 Concept: Blend Trees (the "speed dial" for animations) |
| 10-20 min | **Part 3** | 🖐️ Hands-On: Download Walk & Run from Mixamo |
| 20-30 min | **Part 4** | 🖐️ Hands-On: Build your Blend Tree in the Animator |
| 30-40 min | **Part 5** | 🖐️ Hands-On: Connect your code to the Animator (~10 lines!) |
| 40-45 min | **Part 6** | 🎮 Test Drive & Tweak |
| 45-55 min | **Part 7** | 🖐️ Hands-On: NPC that walks around your scene |
| 55-60 min | **Part 8** | 🔫 BONUS: Equip a gun! |

---

## Part 1: The Skating Problem (3 minutes)

### 🤔 Why Does Our Character Slide?

Let's think about what's happening right now:

```
What our code does:              What the Animator does:
┌───────────────────┐            ┌───────────────────┐
│ PlayerController   │            │ Animator Controller │
│                   │            │                   │
│ WASD → Move()     │            │ Play: Idle        │
│ Shift → Sprint    │            │ (always idle...)  │
│ Gravity → Fall    │            │                   │
└───────────────────┘            └───────────────────┘
        ↓                                ↓
  Character MOVES                 Character looks IDLE
  (position changes)              (standing still pose)
```

**The problem:** Our code moves the character, but the Animator doesn't know we're moving! It just keeps playing the idle animation no matter what.

**The fix:** We need to **tell** the Animator how fast we're moving, so it can pick the right animation:
- Speed = 0 → Play **Idle**
- Speed = 3 → Play **Walk**
- Speed = 6 → Play **Run**

### 💡 The Solution: One Line of Code

Believe it or not, the magic line that fixes everything is:

```csharp
animator.SetFloat("Speed", CurrentSpeed);
```

That's it. One line. But we need to set up the Animator to understand what "Speed" means — that's where **Blend Trees** come in.

---

## Part 2: Blend Trees — The Speed Dial for Animations (7 minutes)

### 🎓 What Is an Animation Parameter?

An **Animation Parameter** is a variable that lives inside the Animator Controller. Your C# code can change it, and the Animator reacts.

Think of it like a **walkie-talkie** between your code and the Animator:

```
PlayerController.cs                    Animator Controller
┌──────────────────┐     "Speed=3!"    ┌──────────────────┐
│                  │ ──────────────→   │                  │
│ SetFloat("Speed",│                   │ Oh, Speed is 3?  │
│          3.0)    │                   │ Play WALK anim!  │
│                  │                   │                  │
└──────────────────┘                   └──────────────────┘
```

Parameters can be different types:

| Type | What It Stores | Example Use |
|------|---------------|-------------|
| **Float** | A decimal number | Speed (0.0 to 6.0) |
| **Int** | A whole number | Weapon type (1, 2, 3) |
| **Bool** | True or false | IsGrounded (true/false) |
| **Trigger** | A one-time signal | Jump (fires once) |

Today we'll use a **Float** called "Speed".

### 🎓 What Is a Blend Tree?

A **Blend Tree** is a special Animator state that **smoothly mixes** between multiple animations based on a parameter.

Think of it like a **volume slider** on a music app — but instead of mixing songs, it mixes animations:

```
Speed Parameter (like a slider):

  0         1.5         3         4.5         6
  |────●─────|───────────|───────────|─────────|
  Idle    (blending)    Walk    (blending)    Run

  ● = current speed value

  At 0:    100% Idle,   0% Walk,   0% Run    → Standing still
  At 1.5:   50% Idle,  50% Walk,   0% Run    → Starting to move
  At 3:      0% Idle, 100% Walk,   0% Run    → Normal walking
  At 4.5:    0% Idle,  50% Walk,  50% Run    → Speeding up
  At 6:      0% Idle,   0% Walk, 100% Run    → Full sprint!
```

**The cool part:** The blending happens automatically! You just tell it the speed number, and the Blend Tree figures out which animations to mix and by how much.

### 🎓 Why Not Just Use If/Else?

You COULD do it manually in code:

```csharp
// BAD WAY (don't do this):
if (speed == 0) PlayAnimation("Idle");
else if (speed < 4) PlayAnimation("Walk");
else PlayAnimation("Run");
```

But this has problems:
- ❌ Animations snap/jump between states (ugly!)
- ❌ No smooth blending
- ❌ Lots of messy code for complex animation setups
- ❌ Hard to tune — need to recompile to change thresholds

With Blend Trees:
- ✅ Smooth blending happens automatically
- ✅ Only ONE line of code needed
- ✅ Tune thresholds visually in Unity's Animator window
- ✅ Professional quality!

### 📝 Quick Check: Do You Understand?

Before we code, make sure you can answer:

1. **What does `animator.SetFloat("Speed", 3)` do?**
   → Sends the number 3 to the Animator's "Speed" parameter

2. **If the Blend Tree has Idle at 0, Walk at 3, Run at 6... what plays at Speed=1.5?**
   → A 50/50 blend of Idle and Walk

3. **Why is this better than if/else?**
   → Smooth blending, less code, easy to tune

Got it? Let's build it! 🔧

---

## Part 3: Hands-On — Download Walk & Run from Mixamo (10 minutes)

> 💡 **Already downloaded Walk & Run animations in Week 8's bonus section?** Skip to **Step 3** (Importing into Unity)!

### Step 1: Download the Walk Animation

1. Go to **[mixamo.com](https://www.mixamo.com)** and log in

2. Click the **"Animations"** tab at the top

3. In the search box, type **"Walking"**

4. Browse until you find a natural-looking walk. Good picks:
   - **"Walking"** — basic, clean walk cycle
   - **"Slow Walk"** — more relaxed pace
   - **"Walking With Swinging Arms"** — a bit more personality

5. **IMPORTANT:** Check the **"In Place"** checkbox! ☑️

   ```
   ☑️ In Place ← CHECK THIS!
   
   Why? Without it, the animation moves the character forward.
   But OUR SCRIPT handles movement! We just want the legs to move.
   
   "In Place" = legs move, body stays still = perfect for us!
   ```

6. Click **"Download"** with these settings:

   | Setting | Value |
   |---------|-------|
   | **Format** | **FBX for Unity (.fbx)** |
   | **Skin** | **Without Skin** |

   > **Why "Without Skin"?** We already have the character mesh from Week 8. For animations, we only need the bone movement data (which bones go where each frame). No need to download the character's body again!

7. Save the file somewhere you can find it (Desktop is fine). Name it something clear like `Walking.fbx`

### Step 2: Download the Run Animation

1. Still on Mixamo, search for **"Running"**

2. Pick a run that matches your walk's energy:
   - **"Running"** — standard jog
   - **"Fast Run"** — more intense sprint
   - **"Rifle Run"** — if you want to add a gun later!

3. **Check "In Place"** ☑️ (same reason as before)

4. Download: **FBX for Unity**, **Without Skin**

5. Save as `Running.fbx`

### Step 3: Import into Unity

1. In Unity's **Project** panel, navigate to your character's animations folder:
   ```
   Assets/Characters/YBot/Animations/    (or whatever your character is called)
   ```

2. **Drag both FBX files** (Walking.fbx and Running.fbx) into the Animations folder

   Your folder should now have:
   ```
   Assets/Characters/YBot/
   ├── YBot.fbx              (your character)
   ├── Animations/
   │   ├── Idle.fbx          (from Week 8)
   │   ├── Walking.fbx       ← NEW!
   │   └── Running.fbx       ← NEW!
   └── PlayerAnimator         (Animator Controller from Week 8)
   ```

### Step 4: Configure Each Animation

Do this for **BOTH** Walking.fbx and Running.fbx:

**A) Set the Rig (so Unity knows it's humanoid):**

1. **Click** on the animation FBX in the Project panel
2. In Inspector, click the **"Rig"** tab
3. Set:
   | Setting | Value |
   |---------|-------|
   | **Animation Type** | **Humanoid** |
   | **Avatar Definition** | **Copy From Other Avatar** |
   | **Source** | Click ⊙ → pick your character's Avatar (e.g., "YBotAvatar") |
4. Click **"Apply"**

> **Reminder:** "Copy From Other Avatar" tells this animation "use the same skeleton mapping as our character." This is animation retargeting!

**B) Enable Looping (so the animation repeats):**

1. Click the **"Animation"** tab (same FBX still selected)
2. Set:
   | Setting | Value |
   |---------|-------|
   | **Loop Time** | ✅ **Checked** |
   | **Loop Pose** | ✅ **Checked** (if available) |
3. Click **"Apply"**

> **Why loop?** Walking and running are continuous — the character should keep walking until they stop, not walk 2 steps and freeze!

### Step 5: Quick Preview Test

Want to see your animations before wiring them up?

1. Click the ▶ arrow next to Walking.fbx to expand it
2. Click the **animation clip** inside (has a play ▶ icon)
3. At the bottom of the Inspector, find the **Preview** window
4. Click the **Play** button ▶ — watch your character walk!
5. Do the same for Running.fbx

If the preview shows a generic gray model, drag YOUR character model into the preview to see it on your character.

**Animations looking good? Let's wire them up!** 🎬

---

## Part 4: Hands-On — Build Your Blend Tree (10 minutes)

This is where the magic happens. We're going to replace the single Idle state with a Blend Tree that automatically picks between Idle, Walk, and Run.

### Step 1: Open Your Animator Controller

1. Find **PlayerAnimator** in the Project panel (inside your character's folder)
2. **Double-click** it to open the **Animator** window

You should see something like this:
```
┌─────────────────────────────────────────┐
│            Animator Window               │
│                                         │
│  [Entry] ──→ [Idle]                     │
│               (orange = default state)   │
│                                         │
│  [Any State]                            │
│                                         │
└─────────────────────────────────────────┘
```

The orange `Idle` state is the only state right now. We're going to replace it.

### Step 2: Create the "Speed" Parameter

The Blend Tree needs a parameter to react to. Let's create one:

1. In the Animator window, find the **"Parameters"** tab on the left side
   - If you see "Layers" instead, click the tab that says **"Parameters"**

2. Click the **"+"** button next to the search bar

3. Select **"Float"** from the dropdown

4. Name it exactly: **`Speed`** (capital S!)

   ```
   Parameters tab:
   ┌─────────────────────┐
   │ Parameters  | Layers │
   │ + ▼                 │
   │                     │
   │ Speed    [0.0    ]  │  ← You just created this!
   │                     │
   └─────────────────────┘
   ```

> **⚠️ The name matters!** Our code will say `animator.SetFloat("Speed", ...)` — if you misspell the name, the code won't find it and nothing will work. Make sure it's exactly `Speed` with a capital S.

### Step 3: Delete the Old Idle State

1. **Right-click** on the orange `Idle` state in the Animator window
2. Click **"Delete"**
3. The state disappears. Don't worry — we'll put Idle INSIDE the new Blend Tree!

> 💡 If you're nervous about deleting it, that's OK! The Idle animation clip still exists in your Animations folder. We're just removing the state from the Animator — the animation itself isn't deleted.

### Step 4: Create a New Blend Tree

1. **Right-click** on the empty space in the Animator window
2. Click **"Create State"** → **"From New Blend Tree"**
3. A new state appears (called "Blend Tree") — it's automatically orange (default state)

   ```
   ┌─────────────────────────────────────────┐
   │            Animator Window               │
   │                                         │
   │  [Entry] ──→ [Blend Tree]               │
   │               (orange = default)         │
   │                                         │
   │  [Any State]                            │
   │                                         │
   └─────────────────────────────────────────┘
   ```

4. If it's NOT orange (not the default state):
   - Right-click on it → **"Set as Layer Default State"**

### Step 5: Open and Configure the Blend Tree

1. **Double-click** the `Blend Tree` state to go inside it

   The Animator window now shows the inside of the Blend Tree:
   ```
   ┌─────────────────────────────────────────┐
   │            Blend Tree (inside)           │
   │                                         │
   │  [Blend Tree]                           │
   │       (empty — needs motions!)          │
   │                                         │
   └─────────────────────────────────────────┘
   ```

2. **Click** on the `Blend Tree` node to select it

3. In the **Inspector** (right side), you'll see the Blend Tree settings:
   - **Blend Type:** `1D` (leave it as is — we only have one parameter: Speed)
   - **Parameter:** Click the dropdown and select **`Speed`**

   ```
   Inspector:
   ┌──────────────────────────┐
   │ Blend Tree               │
   │                          │
   │ Blend Type: [1D       ▼] │
   │ Parameter:  [Speed    ▼] │  ← Select "Speed" here!
   │                          │
   │ Motion list: (empty)     │
   │                          │
   └──────────────────────────┘
   ```

### Step 6: Add Your Three Animations

Now we add Idle, Walk, and Run to the Blend Tree:

1. In the Inspector, click the **"+"** button under the motion list → **"Add Motion Field"**
2. Click **"+"** again → **"Add Motion Field"**
3. Click **"+"** one more time → **"Add Motion Field"**

You now have 3 empty slots:
```
Motion:          Threshold:
[ None      ]    0
[ None      ]    0.5
[ None      ]    1
```

4. **Assign animations to each slot:**
   
   For each `[ None ]` field, click the **circle icon** ⊙ on the right side and search for your animation clips:
   
   | Slot | Animation Clip | How to Find It |
   |------|---------------|----------------|
   | 1st | Your **Idle** clip | Search for "idle" — pick the one from your Idle.fbx |
   | 2nd | Your **Walking** clip | Search for "walk" — pick the one from Walking.fbx |
   | 3rd | Your **Running** clip | Search for "run" — pick the one from Running.fbx |

   > **⚠️ Pick the CLIP, not the FBX!** When you expand an FBX file (click ▶), the animation clip is inside. It has a small play button icon. The clips are also searchable by name in the picker window.

### Step 7: Set the Thresholds

**Thresholds** tell the Blend Tree "at what Speed value should each animation play?"

1. **Uncheck** "Automate Thresholds" (if it's checked) — we want manual control

2. Set the thresholds to match our PlayerController's speed values:

   | Motion | Threshold | Why This Number? |
   |--------|-----------|------------------|
   | Idle | **0** | Speed = 0 when standing still |
   | Walking | **3** | `walkSpeed = 3f` in our PlayerController |
   | Running | **6** | `sprintSpeed = 6f` in our PlayerController |

   ```
   Your Blend Tree should look like this:
   
   Motion:              Threshold:
   [Idle           ]    [0  ]
   [Walking        ]    [3  ]
   [Running        ]    [6  ]
   
   Automate Thresholds: ☐ (unchecked)
   ```

> **💡 Key Insight:** The thresholds MUST match the speed values in your PlayerController script! If your `walkSpeed` is 5 instead of 3, set the Walk threshold to 5. If your `sprintSpeed` is 8, set Run to 8. They need to line up!

### Step 8: Test the Blend Tree Preview

You can preview the blending by manually changing the Speed parameter:

1. In the **Parameters** tab (left side of the Animator window), find the **Speed** field showing `0`
2. **Type a number** into the Speed field and watch the preview at the bottom of the Inspector:
   - Type **0** → Idle animation plays
   - Type **3** → Walk animation plays
   - Type **1.5** → A blend of Idle + Walk!
   - Type **6** → Run animation plays
   - Type **4.5** → A blend of Walk + Run!
3. Set it back to **0** when you're done testing

> 💡 The preview at the bottom of the Inspector shows your character performing the current blend. If you don't see a preview, click on the **Blend Tree** node in the Animator graph to select it.

**See the animations changing? THAT'S why Blend Trees are awesome!** 🎉

### Step 9: Go Back to the Base Layer

1. Click **"Base Layer"** at the top of the Animator window (breadcrumb navigation)
   ```
   Base Layer > Blend Tree     ← Click "Base Layer" to go back up
   ```
2. You're back to seeing the `Blend Tree` state in the Animator

Your Animator is now set up! But nothing happens in Play mode yet — we haven't connected the code. That's the next step!

---

## Part 5: Hands-On — Connect Your Code to the Animator (10 minutes)

This is the moment where one line of code brings everything together.

### 🎓 What We Need to Change

Our PlayerController already tracks speed — it has `CurrentSpeed` that returns how fast the player is moving. We just need to:

1. Get a reference to the Animator component
2. Each frame, tell the Animator: "Hey, the speed is [this number]"

That's it. Two additions.

### Step 1: Open PlayerController.cs

Open your `PlayerController.cs` script (should be in `Assets/Scripts/`).

### Step 2: Add the Animator Reference

Find the **private variables section** near the top of the script. Add ONE new line:

```csharp
// =========================================================
// PRIVATE VARIABLES (Only this script can access these)
// =========================================================

// Components we need to access
private CharacterController characterController;
private PlayerInputActions inputActions;
private Animator animator;  // ← ADD THIS LINE (NEW IN WEEK 9!)

// State tracking
private float verticalVelocity = 0f;
private Vector3 moveDirection = Vector3.zero;
```

### Step 3: Find the Animator in Awake()

In the `Awake()` method, **after** the existing code that gets the CharacterController, add:

```csharp
void Awake()
{
    // ... (existing code for camera, characterController, inputActions) ...
    
    // NEW IN WEEK 9: Find the Animator on our character model (child object)
    // GetComponentInChildren searches THIS object AND all children
    // Our Animator is on the YBot child, not on the Player parent!
    animator = GetComponentInChildren<Animator>();
    
    if (animator == null)
    {
        Debug.LogWarning("No Animator found! Animations won't play.");
    }
}
```

> **🎓 Why `GetComponentInChildren` instead of `GetComponent`?**
> 
> Remember our hierarchy:
> ```
> Player (parent)           ← PlayerController is HERE
> └── YBot (child)          ← Animator is HERE
> ```
> 
> `GetComponent<Animator>()` would only search the Player object — and the Animator isn't there!
> `GetComponentInChildren<Animator>()` searches the Player AND all its children — it finds the Animator on YBot!
> 
> | Method | Searches | Use When |
> |--------|----------|----------|
> | `GetComponent<T>()` | Only THIS object | Component is on same object |
> | `GetComponentInChildren<T>()` | This object + ALL children | Component is on a child |
> | `GetComponentInParent<T>()` | This object + ALL parents | Component is on a parent |

### Step 4: Create the UpdateAnimator Method

Add this NEW method at the bottom of your script (before the public getters at the very end):

```csharp
// =========================================================
// UPDATE ANIMATOR - Tell the Animator how fast we're moving
// =========================================================
// NEW IN WEEK 9!

private void UpdateAnimator()
{
    if (animator == null) return;  // Safety check
    
    // THE MAGIC LINE:
    // Send our current speed to the Animator's "Speed" parameter
    // The Blend Tree receives this number and picks the right animation!
    //
    // The 0.15f is "damping time" — it smooths the transition over 0.15 seconds
    // Without it, animations would snap instantly (ugly!)
    // With it, there's a brief smooth blend (professional!)
    animator.SetFloat("Speed", CurrentSpeed, 0.15f, Time.deltaTime);
}
```

> **🎓 What's That 0.15f Doing?**
> 
> `SetFloat` has two versions:
> ```csharp
> // Version 1: Instant (snappy, can look jarring)
> animator.SetFloat("Speed", CurrentSpeed);
> 
> // Version 2: Smoothed (adds damping for smooth transitions)
> animator.SetFloat("Speed", CurrentSpeed, dampTime, deltaTime);
> //                                        ↑          ↑
> //                                   0.15 seconds   Time.deltaTime
> //                                   to transition  (frame time)
> ```
> 
> The damping means: "Don't jump to the new value instantly — smoothly slide there over 0.15 seconds."
> 
> This prevents the animation from snapping harshly when you go from standing still to full sprint. Instead, it blends through Walk on the way to Run. Looks way better!

### Step 5: Call UpdateAnimator from Update()

Find your `Update()` method and add the call:

```csharp
void Update()
{
    HandleMovement();
    HandleGravity();
    ApplyMovement();
    UpdateAnimator();  // ← ADD THIS LINE (NEW IN WEEK 9!)
    
    if (showDebugInfo)
    {
        DebugInfo();
    }
}
```

### 📋 Complete Changes Summary

Here's EVERYTHING we changed (just 3 spots):

```
Change #1: Added ONE variable
──────────────────────────────
private Animator animator;

Change #2: Added 5 lines to Awake()
──────────────────────────────
animator = GetComponentInChildren<Animator>();
if (animator == null)
{
    Debug.LogWarning("No Animator found! Animations won't play.");
}

Change #3: Added ONE method + ONE call in Update()
──────────────────────────────
private void UpdateAnimator()
{
    if (animator == null) return;
    animator.SetFloat("Speed", CurrentSpeed, 0.15f, Time.deltaTime);
}

// And in Update():
UpdateAnimator();
```

That's it. ~10 lines of new code total. The Blend Tree does all the heavy lifting!

### Step 6: Save the Script

Press **Ctrl+S** to save. Unity will recompile.

---

## Part 6: Test Drive! (5 minutes)

### 🎮 The Moment of Truth

1. **Press Play** in Unity

2. **Stand still** — your character should be in the Idle animation (breathing/swaying)

3. **Press W** to walk forward — the character should transition to the Walk animation!

4. **Hold Shift + W** to sprint — the character should transition to Run!

5. **Let go of everything** — character blends back to Idle!

```
Your controls now:

  Stand still     → 🧍 Idle animation
  WASD            → 🚶 Walk animation (speed ~3)
  Shift + WASD    → 🏃 Run animation  (speed ~6)
  
  Transitions are SMOOTH — no more ice skating! 🎉
```

### 🐛 Troubleshooting

**Character still slides (no animation change):**
- Open the Animator window while in Play mode — is the Speed parameter changing?
- If Speed stays at 0: Check that `UpdateAnimator()` is being called in `Update()`
- If Speed changes but animation doesn't: Check that the Blend Tree's Parameter is set to "Speed"
- Make sure the parameter name is exactly `Speed` (capital S) in both the Animator AND the code

**Animation plays but looks wrong (feet sliding on ground):**
- This is called **foot skating** — the animation's step length doesn't match the movement speed
- **Fix:** Adjust the Blend Tree thresholds. If walking looks too slow, decrease the Walk threshold (try 2 instead of 3)
- Or adjust `walkSpeed` in the Inspector to match the animation better

**Character snaps between animations (no smooth blending):**
- Make sure you're using the version with damping: `SetFloat("Speed", CurrentSpeed, 0.15f, Time.deltaTime)`
- If you used `SetFloat("Speed", CurrentSpeed)` without damping, add the extra parameters

**Animations play in wrong order (running when walking, etc.):**
- Check the thresholds in the Blend Tree. They should go: Idle=0, Walk=3, Run=6
- Make sure AnimaitonIdleIdle is in the FIRST slot, Walk in the SECOND, Run in the THIRD

**Character faces backward when moving:**
- Select the character model child (YBot) → set **Rotation Y: 180**

### 🎯 Fine-Tuning Challenge (Optional)

Want it to look even better? Try adjusting:

| What to Tweak | Where | Effect |
|---------------|-------|--------|
| Walk threshold | Blend Tree | When walk animation kicks in |
| Run threshold | Blend Tree | When run animation kicks in |
| Damping time | Code (0.15f) | How fast animation transitions happen |
| walkSpeed | Inspector | How fast the player actually moves |
| sprintSpeed | Inspector | How fast sprinting is |

**Pro tip:** Open the Animator window, press Play, and watch the Blend Tree slider move in real-time as you walk/run. You can see exactly where you are in the blend!

---

## Part 7: Hands-On — NPC That Walks Around (10 minutes)

Your world looks empty with just your player running around. Let's add an NPC (Non-Player Character) that patrols back and forth — it'll make your scene feel like a real game!

> **📌 This section was originally the Week 8 bonus, now upgraded with MOVEMENT!**

### 🎓 Quick Concept: Same Tools, Different Script

The NPC uses the exact same animation system you just set up:
- Same Blend Tree concept (Idle at 0, Walk at 3)
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

### Step 1: Download an NPC Character (if you don't have one from Week 8)

1. Go to **Mixamo** → **Characters** tab
2. Pick a **DIFFERENT** character than your player
   - **Mutant** — cool zombie-like creature
   - **X Bot** — if your player is Y Bot
   - **Peasant Girl** — medieval character
3. Download: **FBX for Unity**, **T-Pose**
4. Also download a **Walking** animation for the NPC (FBX for Unity, **Without Skin**, **In Place** ☑️)

> 💡 If you already downloaded an NPC character in Week 8's bonus, use that one!

### Step 2: Import & Configure the NPC

Same process as before (this should feel familiar now!):

1. Create folder: `Assets/Characters/NPC_Name/` (e.g., `Assets/Characters/Mutant/`)
2. Create subfolder: `Animations/` inside it
3. **Drag** the NPC character FBX into the NPC folder
4. Click it → **Rig** tab → **Animation Type: Humanoid** → **Apply**
5. **Drag** the walk animation FBX into `Animations/`
6. Click it → **Rig** tab → **Humanoid** → **Copy From Other Avatar** → pick NPC's avatar → **Apply**
7. Click it → **Animation** tab → **Loop Time ✅** → **Apply**

### Step 3: Create the NPC's Animator Controller

1. Right-click in the NPC's folder → **Create → Animator Controller** → name it **"NPCAnimator"**
2. **Double-click** to open it

3. Create a **Speed** parameter (Float) — same as the player's!

4. **Right-click** → **Create State → From New Blend Tree**

5. **Double-click** the Blend Tree to go inside

6. Set **Parameter** to **Speed**

7. Add **2 Motion Fields** (+ → Add Motion Field, twice):
   - Slot 1: NPC's **Idle** clip → Threshold: **0**
   - Slot 2: NPC's **Walking** clip → Threshold: **2**

> 💡 **No Run for the NPC** — it's just patrolling, not sprinting! We only need Idle and Walk. You CAN add a Run if you want though!

> 💡 **What if you don't have an Idle animation for the NPC?** You can use the same Idle animation from your player! Humanoid animation retargeting means any humanoid animation works on any humanoid character. Just pick your player's Idle clip.

8. Go back to **Base Layer** (breadcrumb at top)

### Step 4: Place the NPC in the Scene

1. In the Hierarchy, right-click → **Create Empty** → name it **`NPC`**
2. **Drag** your NPC character FBX from the Project panel **onto** the NPC object (makes it a child)

   ```
   Hierarchy:
   ├── Player
   │   └── YBot
   ├── NPC              ← new empty parent
   │   └── Mutant       ← character model as child
   ├── Main Camera
   └── Ground
   ```

3. Select the NPC character child → Inspector → **Animator** component:
   - **Controller:** Drag in `NPCAnimator`
   - **Apply Root Motion:** ☐ **OFF**

4. Position the NPC somewhere in your scene (try X: 5, Y: 0, Z: 5 — near the player but not on top)

### Step 5: Create the Patrol Script

Now for the fun part — making the NPC actually walk!

1. In `Assets/Scripts/`, right-click → **Create → C# Script** → name it **`NPCPatrol`**
2. Double-click to open it, and **replace everything** with this:

```csharp
using UnityEngine;

/// <summary>
/// Week 9: Simple NPC patrol between two points.
/// The NPC walks from Point A to Point B and back, forever.
/// Uses the same Blend Tree animation system as the Player!
/// </summary>
public class NPCPatrol : MonoBehaviour
{
    [Header("Patrol Points")]
    [Tooltip("First patrol destination")]
    public Transform pointA;

    [Tooltip("Second patrol destination")]
    public Transform pointB;

    [Header("Movement Settings")]
    [Tooltip("How fast the NPC walks")]
    public float walkSpeed = 2f;

    // Private variables
    private Transform currentTarget;   // Which point we're walking toward
    private Animator animator;          // Same concept as the player!

    void Start()
    {
        // Start by walking toward Point B
        currentTarget = pointB;

        // Find the Animator on the character model (child object)
        // Same as Player — GetComponentInChildren!
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // Safety check: can't patrol without both points!
        if (pointA == null || pointB == null) return;

        // STEP 1: Calculate direction to walk
        // (target position - my position) = direction vector pointing at target
        Vector3 direction = (currentTarget.position - transform.position);
        direction.y = 0;  // Keep movement flat (no flying NPCs!)
        direction.Normalize();  // Make it length 1 (just direction, no magnitude)

        // STEP 2: Move toward the target
        // position += direction * speed * time = smooth movement!
        transform.position += direction * walkSpeed * Time.deltaTime;

        // STEP 3: Face the direction we're walking
        if (direction.magnitude > 0.01f)
        {
            // Quaternion.LookRotation = "make a rotation that faces this direction"
            // Same function the Player uses for rotation!
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // STEP 4: Check if we arrived at the target
        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
        if (distanceToTarget < 0.5f)
        {
            // Swap targets! If we were going to B, now go to A (and vice versa)
            // This is a TERNARY OPERATOR — same one from PlayerController!
            currentTarget = (currentTarget == pointA) ? pointB : pointA;
        }

        // STEP 5: Update the Animator (same concept as Player!)
        if (animator != null)
        {
            // NPC is always walking at walkSpeed, so just send that
            animator.SetFloat("Speed", walkSpeed, 0.1f, Time.deltaTime);
        }
    }
}
```

3. **Save** the script (Ctrl+S)

### Step 6: Set Up the Patrol Points

The NPC needs two points to walk between. We'll use Empty GameObjects as markers:

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

### Step 7: Wire It All Up

1. Select the **NPC** parent object in the Hierarchy
2. Click **Add Component** → search for **NPCPatrol** → add it
3. In the Inspector, you'll see the NPCPatrol script with empty fields:
   - **Point A:** Drag `PatrolPoint_A` from the Hierarchy into this field
   - **Point B:** Drag `PatrolPoint_B` from the Hierarchy into this field
   - **Walk Speed:** 2 (default is fine)

### Step 8: Test It!

1. **Press Play**
2. Watch the NPC walk back and forth between the two points!
3. The NPC should be playing the walk animation (not sliding!)
4. Walk your player over to the NPC — two animated characters in the same world!

```
🎮 Your scene now:

  Player 🏃 ← you control with WASD
  
  NPC 🚶 → · · · · · → 🚶 → · · · · · → 🚶  (patrols back and forth)
  
  Both animated! Both moving! Feels like a real game! 🎉
```

### 🐛 NPC Troubleshooting

**NPC doesn't move:**
- Check that `PatrolPoint_A` and `PatrolPoint_B` are dragged into the script fields
- Check that the NPCPatrol script is on the **NPC parent**, not on the character child

**NPC slides without walking animation:**
- Check that NPCAnimator is assigned to the character child's Animator component
- Check that the NPC's Blend Tree has a Walk clip at threshold 2

**NPC walks through walls/ground:**
- This basic script doesn't handle collision. That's OK for now!
- We'll add proper AI navigation (NavMesh) in a later week

---

## Part 8: BONUS — Equip a Gun! 🔫 (5 minutes)

If there's time left, let's give your player a weapon! This is a quick visual trick that makes your game look 10x cooler.

### 🎓 The Concept: Parenting to a Bone

Remember the skeleton inside your character? It has bones for every body part — including **hands**. If we make a gun object a **child** of the hand bone, the gun will move with the hand automatically!

```
Character Skeleton:
         Head
        /    \
   L.Arm      R.Arm
    |            |
  L.Hand      R.Hand
                 |
              🔫 Gun  ← Parent it here!

When the hand moves (animations), the gun follows!
```

### Step 1: Find the Right Hand Bone

1. In the **Hierarchy**, expand your character model (click all the ▶ arrows):
   ```
   Player
   └── YBot
       └── Armature (or "mixamorig:Hips")
           └── mixamorig:Spine
               └── mixamorig:Spine1
                   └── mixamorig:Spine2
                       ├── mixamorig:LeftShoulder
                       │   └── ...
                       └── mixamorig:RightShoulder
                           └── mixamorig:RightArm
                               └── mixamorig:RightForeArm
                                   └── mixamorig:RightHand  ← THIS ONE!
   ```

2. **Click on `mixamorig:RightHand`** to select it

> 💡 **The bone names might be slightly different** depending on your character. Look for anything with "Right" and "Hand" in the name.

### Step 2: Create a Simple Gun from Primitives

We'll make a quick gun from basic shapes (no need to download anything!):

1. **Right-click** on `mixamorig:RightHand` in the Hierarchy
2. Click **Create Empty** → name it **`Gun`**
3. Now add the gun parts as children of `Gun`:

**Gun Barrel (the long part):**
1. Right-click on `Gun` → **3D Object → Cube**
2. Name it `Barrel`
3. Set Transform:
   - **Position:** X: 0, Y: 0, Z: 0.15
   - **Scale:** X: 0.03, Y: 0.03, Z: 0.3

**Gun Grip (the handle):**
1. Right-click on `Gun` → **3D Object → Cube**
2. Name it `Grip`
3. Set Transform:
   - **Position:** X: 0, Y: -0.05, Z: 0
   - **Scale:** X: 0.03, Y: 0.1, Z: 0.06

Your hierarchy should look like:
```
mixamorig:RightHand
└── Gun
    ├── Barrel    (long cube)
    └── Grip      (short cube below)
```

### Step 3: Position the Gun

The gun probably looks wrong (floating, rotated weirdly). We need to adjust the `Gun` parent object:

1. Select the **`Gun`** empty object

2. Adjust the **Transform** until the gun sits nicely in the hand:
   - **Position:** Try small values like X: 0.1, Y: 0.05, Z: 0
   - **Rotation:** Try X: 0, Y: 0, Z: 90 (or experiment!)
   
   > **This takes trial and error!** Press Play, see how it looks, Stop, adjust, repeat. Every character's hand bone is oriented differently.

3. **Optional:** Create a material and make the gun dark gray or black:
   - Right-click in Project → **Create → Material** → name it `GunMetal`
   - Set Albedo/Base color to dark gray
   - Drag it onto both Barrel and Grip

### Step 4: Test It!

1. **Press Play**
2. Your character should be holding the gun!
3. When you move, the gun stays in the hand
4. When the idle animation plays, the hand (and gun) sway gently

```
  Before:                After:
    O                      O
   /|\                    /|\🔫  ← Gun in right hand!
   / \                    / \

  Empty hands...          Armed and dangerous!
```

> 💡 **Want a REAL gun model?** You can download a free weapon model from:
> - **Unity Asset Store** (search "free weapon" or "low poly gun")
> - **Sketchfab** (free section, download as FBX)
> - Import the FBX, drag it onto `mixamorig:RightHand`, and position it!
>
> For even better results, download a **"Rifle Idle"** and **"Rifle Walk"** animation from Mixamo and add them to your Blend Tree. The character will hold the gun properly in every animation!

---

## 🧠 Part 9: Understanding What We Built (Review)

### 🎓 The Full Animation Pipeline

```
┌─────────────────────────────────────────────────────────────┐
│                    THE ANIMATION PIPELINE                    │
│                                                             │
│  PlayerController.cs         Animator Controller             │
│  ┌────────────────┐          ┌────────────────────┐         │
│  │ WASD Input     │  Speed   │   Blend Tree       │         │
│  │ → Calculate    │────────→│  ┌──────────────┐  │         │
│  │   movement     │ (float) │  │  0: Idle     │  │         │
│  │ → Get speed    │         │  │  3: Walk     │  │         │
│  │ → SetFloat()   │         │  │  6: Run      │  │         │
│  └────────────────┘         │  └──────────────┘  │         │
│                             └────────────────────┘         │
│                                      │                      │
│                                      ▼                      │
│                             ┌────────────────────┐         │
│                             │  SkinnedMeshRenderer│         │
│                             │  Moves bones →      │         │
│                             │  Deforms mesh →     │         │
│                             │  You see animation! │         │
│                             └────────────────────┘         │
└─────────────────────────────────────────────────────────────┘
```

### 🎓 What Changed vs What Stayed the Same

| Component | Changed? | Notes |
|-----------|----------|-------|
| PlayerController.cs | ⚙️ **+10 lines** | Added Animator ref + `UpdateAnimator()` |
| Animator Controller | ⚙️ **Blend Tree** | Replaced single Idle state with Blend Tree |
| CharacterController | ❌ No change | Still handles physics collision |
| ThirdPersonCamera | ❌ No change | Still follows and orbits |
| Character Model | ❌ No change | Same Mixamo character from Week 8 |

### 🎓 New C# Concepts

| Concept | What It Does | Example |
|---------|-------------|---------|
| `GetComponentInChildren<T>()` | Searches this object AND all children for a component | `animator = GetComponentInChildren<Animator>()` |
| `animator.SetFloat()` | Sends a number to the Animator's parameter | `animator.SetFloat("Speed", 3.0f)` |
| Damping (smooth transitions) | Smoothly changes a value over time instead of instantly | `SetFloat("Speed", value, 0.15f, Time.deltaTime)` |

### 🎓 New Unity Concepts

| Concept | What It Is | Why It Matters |
|---------|-----------|----------------|
| **Animation Parameter** | A variable in the Animator that code can change | Bridge between code and animations |
| **Blend Tree** | Mixes animations based on a parameter value | Smooth idle→walk→run transitions |
| **Threshold** | The parameter value where an animation plays at 100% | Must match your speed values! |
| **Bone Parenting** | Making an object a child of a skeleton bone | Weapons follow hand movement |

---

## ✅ Week 9 Complete!

### What You Built:
- ✅ Walk animation that plays when moving
- ✅ Run animation that plays when sprinting
- ✅ Smooth blending between Idle, Walk, and Run via Blend Tree
- ✅ Connected PlayerController to Animator with ~10 lines of code
- ✅ NPC that patrols between two points with walking animation
- ✅ (Bonus) Gun equipped to player's hand bone!

### What You Learned:
- **Animation Parameters** — How code talks to the Animator
- **Blend Trees** — Smoothly mixing animations based on a number
- **Thresholds** — When each animation plays at full strength
- **GetComponentInChildren** — Finding components on child objects
- **SetFloat with damping** — Smooth value transitions
- **Bone parenting** — Attaching objects to skeleton bones
- **NPC patrol patterns** — Basic AI movement between waypoints

### The Before & After:

```
Week 8:                              Week 9:
┌────────────────────┐               ┌────────────────────┐
│ Character slides   │               │ Character WALKS    │
│ around in idle     │     →→→       │ and RUNS with      │
│ pose. Creepy.      │               │ smooth animations! │
│ Empty world.       │               │ NPC patrols around!│
│ No weapon.         │               │ Holding a gun!     │
└────────────────────┘               └────────────────────┘
```

---

## 💡 Homework Challenges (Optional)

1. **Tune the feel:** Adjust `walkSpeed`, `sprintSpeed`, Blend Tree thresholds, and damping until movement feels perfect to YOU. Every game feels different — find your style!

2. **Add more NPCs:** Duplicate the NPC and patrol points. Place 3-4 NPCs walking different paths around your scene.

3. **Try different animations:** Download a "Strafe" or "Walking Backward" animation from Mixamo. Can you figure out how to add it to the Blend Tree? (Hint: you'll need a 2D Blend Tree — look it up!)

4. **Dance party:** Download a dance animation from Mixamo and manually set it as the NPC's default state instead of the Blend Tree. Watch the NPC dance forever.

5. **Better gun:** Download a real gun model from the Unity Asset Store (search "free weapon"). Import it and parent it to the hand bone instead of the cube-gun.

---

## 🔮 What's Coming Next

Now that your character moves AND animates like a real game character, we're ready for:

- **Week 10:** Shooting Mechanics — Raycasting to fire bullets + muzzle flash effects
- **Week 11:** Enemy AI — NPCs that chase and attack the player using NavMesh
- **Later:** Health systems, damage, game UI, win/lose conditions!

Your character went from a sliding mannequin to an armed, animated character in a world with NPCs. That's a GAME. 🎮
