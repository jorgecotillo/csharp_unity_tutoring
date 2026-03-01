# Week 8: From Capsule to Character — Humanoid Setup with Mixamo 🧍

⚠️ **IMPORTANT:** This continues directly from Weeks 5-7. You should have:
- ✅ Player GameObject with Character Controller + PlayerController script
- ✅ Third-person camera working (CameraFollow / ThirdPersonCamera)
- ✅ WASD movement, sprint, gravity, camera collision all working
- ✅ A ground plane and at least one wall in your scene

If anything is missing, go back to the Week 5-7 README and finish those steps first!

---

## 🎯 What You'll Build This Week

Right now your player is a **capsule**. That's fine for testing, but it's boring. This week we're swapping it out for a real 3D humanoid character — the kind you'd see in an actual game.

**By the end of this lesson, you'll have:**
- A humanoid character from Mixamo (free!) in your project
- The character moving around with WASD, just like the capsule did
- A basic idle animation so the character isn't frozen like a mannequin
- Understanding of 3D models, rigs, and avatars

**No code changes to movement!** Your PlayerController and Camera scripts stay exactly the same. We're just swapping the visual (capsule → character model).

---

## 📅 Week 8 Structure (60 minutes)

| Time | Topic |
|------|-------|
| 0-5 min | Why Characters Matter + Motivation |
| 5-15 min | **Deep Dive: 3D Models, Meshes & Rigs** |
| 15-25 min | **Hands-On: Downloading from Mixamo** |
| 25-40 min | **Hands-On: Importing & Configuring in Unity** |
| 40-50 min | **Hands-On: Replacing the Capsule** |
| 50-55 min | Testing, Fixing & Basic Idle Animation |
| 55-60+ min | **Bonus: Drop an NPC in the Scene / Preview Walk Animations** |

---

## Part 1: Why Characters Matter (5 minutes)

### 🧠 The Psychology of Game Feel

When you play a game, you don't think "I'm moving a capsule collider with a character controller." You think **"I'm running."** The visual representation matters — a lot.

Compare:
```
Capsule:                    Humanoid:
    ___                       O
   /   \                    /|\
  |     |                   / \
  |     |                  Looks like someone!
   \___/                   
  Looks like... a pill?    
```

Professional games use placeholder capsules during development, then swap in real characters. That's exactly what we're doing today.

### 💡 Good News

Here's the cool part: **we don't need to change any of our movement code**. The CharacterController component doesn't care what the player *looks* like — it only cares about the collider shape. We're replacing the **visual**, not the **logic**.

---

## Part 2: Deep Dive — 3D Models, Meshes & Rigs (10 minutes)

### 🎓 What Is a 3D Model?

A 3D model is made up of several parts:

| Component | What It Is | Analogy |
|-----------|-----------|---------|
| **Mesh** | The 3D shape — thousands of tiny triangles | The body/skin |
| **Materials** | Colors and textures applied to the mesh | Clothing/paint |
| **Skeleton (Rig)** | Invisible bones inside the mesh | Actual skeleton |
| **Avatar** | Unity's mapping of the skeleton | "This bone = left arm" |

```
3D Character Breakdown:
┌──────────────────────────┐
│         MESH             │ ← The visible triangles (skin, clothes, hair)
│  ┌───────────────────┐   │
│  │    SKELETON (RIG)  │   │ ← Hidden bones that move the mesh
│  │    ┌─ Head         │   │
│  │    ├─ Spine        │   │
│  │    ├─ L.Arm        │   │
│  │    ├─ R.Arm        │   │
│  │    ├─ L.Leg        │   │
│  │    └─ R.Leg        │   │
│  └───────────────────┘   │
│  MATERIALS → Colors/Tex  │ ← What makes the mesh look good
└──────────────────────────┘
         ↓ Unity reads this as ↓
      AVATAR (Unity's bone map)
```

### 🎓 What Is Rigging?

**Rigging** = putting a skeleton inside a 3D model.

Think of it like a puppet:
- The **mesh** is the puppet's body (the thing you see)
- The **rig/skeleton** is the wooden frame inside (the thing that moves)
- When you move a bone, the mesh bends and follows

Without a rig, the character is just a frozen statue. With a rig, every bone can move — and that's how animations work.

### 🎓 What Is Mixamo?

**Mixamo** (mixamo.com) is a free Adobe website that gives you:
- **Characters** — Ready-to-use 3D humanoid models with rigs
- **Animations** — Thousands of motion-captured animations (walk, run, jump, shoot, dance, etc.)

It's free, no credit card needed, just an Adobe account. Professional indie developers use Mixamo all the time for prototyping.

### 🎓 FBX File Format

3D models are stored in **FBX files** (`.fbx`). Think of FBX like a ZIP file for 3D — it can contain:
- The mesh (shape)
- The rig (skeleton)
- Materials (colors)
- Animations (movement)

When you download from Mixamo, you get `.fbx` files. Unity knows how to read these natively.

### 🎓 Unity's Humanoid Avatar System

Unity has a special system called **Humanoid Avatar**. When you tell Unity "this model is a humanoid," it:

1. **Maps every bone** to Unity's standard skeleton (LeftUpperArm, RightFoot, Spine, etc.)
2. **Enables animation retargeting** — any humanoid animation can play on any humanoid character
3. **Enables IK (Inverse Kinematics)** — feet plant on the ground, hands reach for objects

This is why Mixamo animations "just work" — they're all humanoid, so Unity can map them to any humanoid character.

---

## Part 3: Downloading from Mixamo (10 minutes)

### Step 1: Create an Adobe Account (if you don't have one)

1. Go to **[mixamo.com](https://www.mixamo.com)**
2. Click **Log In** (top right)
3. Sign up for a free Adobe account (or use an existing one)
4. No credit card needed!

### Step 2: Choose a Character

1. Once logged in, click **"Characters"** tab at the top

2. Browse the available characters. Here are some good starter picks:
   - **Y Bot** — gray robot, clean look, great for sci-fi
   - **X Bot** — blue robot, similar to Y Bot
   - **Peasant Girl** — medieval character
   - **Mutant** — cool monster/zombie type
   - **Michelle** or **James** — realistic humans

3. **Click on a character you like** — it loads in the preview window

> 💡 **Pro tip:** Start with **Y Bot** or **X Bot** — they're simple, low-poly, and look good without textures. Realistic humans sometimes have texture issues on import.

### Step 3: Download the Character

1. After selecting your character, click **"Download"** (top right)

2. Use these settings:
   | Setting | Value |
   |---------|-------|
   | **Format** | **FBX for Unity (.fbx)** |
   | **Pose** | **T-Pose** |

   > **Why "FBX for Unity"?** Mixamo offers several FBX formats. The difference is simple:
   > - **FBX for Unity** says "1 unit = 1 meter" → character imports at the right size (1.8m tall)
   > - **FBX Binary** says "1 unit = 1 centimeter" → character imports 100x too big (180m tall giant!)
   > 
   > Same character, same bones, same everything — just a different measurement label baked in. Always pick **FBX for Unity** when working in Unity.

   > **Why T-Pose?** It's the standard starting pose for rigged characters. Arms straight out, legs straight. Unity expects this for proper bone mapping.

3. Click **"Download"**

4. Save the `.fbx` file somewhere you can find it (like Desktop or Downloads)

### Step 4: Download a Basic Idle Animation

While we're on Mixamo, let's also grab an idle animation so our character isn't frozen:

1. Click the **"Animations"** tab at the top

2. Search for **"Idle"** in the search box

3. Pick one you like — suggestions:
   - **"Happy Idle"** — slight swaying, looks natural
   - **"Idle"** — very subtle breathing animation
   - **"Rifle Idle"** — if you want to set up for the shooter game later

4. **IMPORTANT:** Make sure **"In Place"** is checked (if available). This prevents the animation from moving the character's position — we handle movement with our script!

5. Click **"Download"** with these settings:
   | Setting | Value |
   |---------|-------|
   | **Format** | **FBX for Unity (.fbx)** |
   | **Skin** | **Without Skin** |

   > **Why "Without Skin"?** We already downloaded the character mesh. For animations, we only need the bone movement data, not another copy of the mesh. This keeps file sizes small.

6. Save this file too.

---

## Part 4: Importing into Unity (15 minutes)

### Step 1: Create a Folder for Your Character

Keep your project organized:

1. In Unity's **Project** panel (bottom), right-click in `Assets`
2. **Create → Folder** → Name it `Characters`
3. Inside `Characters`, create another folder with your character's name (e.g., `YBot`)
4. Inside that, create an `Animations` folder

Your folder structure should look like:
```
Assets/
├── Characters/
│   └── YBot/           (or whatever character you picked)
│       └── Animations/
├── Scripts/
├── Scenes/
└── ...
```

### Step 2: Import the Character FBX

1. **Drag the character `.fbx`** file from your file explorer into the `Assets/Characters/YBot/` folder in Unity's Project panel

   OR

   Right-click the folder → **Import New Asset...** → Select the `.fbx` file

2. Unity will import it — you'll see it appear as an asset with a little triangle (▶) to expand it

3. **Click the ▶** to expand — you should see:
   - The mesh (character shape)
   - Materials (colors)
   - Avatar (skeleton mapping)

### Step 3: Configure the Character Rig

This is the critical step — telling Unity "this is a humanoid":

1. **Click on the imported FBX** in the Project panel to select it

2. In the **Inspector** panel (right side), you'll see import settings with tabs:
   - **Model** | **Rig** | **Animation** | **Materials**

3. Click the **"Rig"** tab

4. Change these settings:

   | Setting | Change To |
   |---------|-----------|
   | **Animation Type** | **Humanoid** (was probably "Generic" or "None") |
   | **Avatar Definition** | **Create From This Model** |

5. Click **"Apply"** (bottom of Inspector)

6. Unity will process the model and map the bones. You should see a green checkmark ✅ next to "Configure..."

### Step 4: Verify the Avatar (Optional but Recommended)

1. Click **"Configure..."** button next to Avatar Definition
2. Unity opens the Avatar Configuration window
3. You'll see a humanoid skeleton diagram with **green dots** on all the bones
   - **Green** ✅ = bone mapped correctly
   - **Red** ❌ = bone missing or wrong
4. Mixamo characters should be all green — they're designed for this
5. Click **"Done"** to close

```
Avatar Configuration:
     (O)          ← Head ✅
   --|--          ← Arms ✅
     |            ← Spine ✅
    / \           ← Legs ✅
   /   \

All green = good to go!
```

### Step 5: Import the Idle Animation

1. **Drag the idle animation `.fbx`** into `Assets/Characters/YBot/Animations/`

2. Click on the imported animation FBX to select it

3. Go to the **"Rig"** tab in Inspector:
   | Setting | Change To |
   |---------|-----------|
   | **Animation Type** | **Humanoid** |
   | **Avatar Definition** | **Copy From Other Avatar** |
   | **Source** | Select your **character's Avatar** (click the circle ⊙ and find it) |

   > **Why copy Avatar?** The animation needs to know which bone is which. By copying the avatar from our character, we're saying "use the same skeleton mapping." This is how animation retargeting works.

4. Click **"Apply"**

5. Now go to the **"Animation"** tab:
   | Setting | Change To |
   |---------|-----------|
   | **Loop Time** | ✅ **Checked** |
   | **Loop Pose** | ✅ **Checked** (if available) |

   > **Why loop?** Idle is a continuous animation — the character should breathe/sway forever, not play once and freeze.

6. Click **"Apply"**

---

## Part 5: Replacing the Capsule (10 minutes)

Now for the satisfying part — swapping the boring capsule for a real character!

### Step 1: Understand the Current Setup

Right now your Player hierarchy looks something like:
```
Player (GameObject)
├── CharacterController (component)
├── PlayerController (script)
├── Capsule Mesh (the visible capsule shape)
└── Main Camera (child or separate with CameraFollow)
```

We need to:
- **Remove** the capsule's visual mesh
- **Add** the humanoid model as a child
- **Keep** the CharacterController and scripts exactly as they are

### Step 2: Hide the Capsule Visual

We don't want to delete the whole Player GameObject — it has our scripts and CharacterController on it!

1. Select your **Player** GameObject in the Hierarchy
2. In the Inspector, find the **Mesh Renderer** component
3. **Uncheck** the checkbox next to "Mesh Renderer" to disable it (don't delete the component!)
4. Also **uncheck** the **Mesh Filter** component
   
   OR if your capsule visual is a separate child object:
   - Right-click the capsule child → **Delete**

> **💡 Why disable instead of delete?** We keep the Capsule Collider and CharacterController on the parent. We're only hiding the *visual* — the collision shape stays.

### Step 3: Add the Humanoid Model

1. In the **Project** panel, find your imported character FBX
2. **Drag it into the Hierarchy** panel and drop it **onto your Player GameObject** (it becomes a child)

   Your hierarchy should now look like:
   ```
   Player
   ├── YBot (or your character name)   ← NEW! The 3D model
   ├── Main Camera (if it's a child)
   └── (CharacterController, PlayerController, etc. as components)
   ```

3. The character appears in the scene! But it probably looks wrong — too big, wrong position, etc.

### Step 4: Position and Scale the Character

The character model needs to line up with the CharacterController collider:

1. **Select the character child** (e.g., YBot) in the Hierarchy

2. In the **Inspector**, set the **Transform**:
   | Property | Value |
   |----------|-------|
   | **Position** | X: 0, **Y: 0**, Z: 0 |
   | **Rotation** | X: 0, Y: 0, Z: 0 |
   | **Scale** | X: 1, Y: 1, Z: 1 |

   > **Adjusting Y Position:** The right Y value depends on your CharacterController's **Center Y**. The character model's origin is at their feet, so you need the feet to land at the bottom of the green wireframe capsule. Start at Y: 0 and nudge up or down until the feet are on the ground. If your CharacterController Center is Y: 1 and Height is 2, then Y: 0 puts the feet right at the bottom.

3. **Check the result visually:** Select the **Player** parent to see the green wireframe capsule, then verify:
   - **Feet** at the bottom of the CharacterController capsule (green wireframe)
   - **Head** near the top
   - **Character centered** left-right and front-back
   - If the character is sinking into the ground, increase Y. If floating, decrease Y.

4. **Enable Gizmos so you can SEE the CharacterController capsule:**
   
   By default, Unity 6 may hide the green wireframe. To turn it on:
   1. Look at the **bottom of the Scene view** — find the toolbar row with small icons
   2. Click the **4th icon from the right** (called **"View Options"**)
   3. In the popup, click the **globe/sphere icon** (next to the camera icon) — this toggles **Gizmos ON**
   4. Now select the **Player** parent object — you should see a **green wireframe capsule** in the Scene view
   
   > **💡 Can't find it?** The icons at the bottom of the Scene view look like small symbols. The View Options icon opens a panel with toggles. The Gizmos toggle looks like a small sphere/globe. Once enabled, selecting any object with a collider or CharacterController will show its wireframe shape.

### Step 5: Adjust the CharacterController Size (If Needed)

Now that you can see the green wireframe, let's make it fit the character properly.

**The Rule:** Center Y = Height ÷ 2 (this keeps the capsule centered from feet to head)

1. Select the **Player** parent
2. Find **CharacterController** in Inspector
3. Start with a **wide Radius** (0.5) so the wireframe is clearly visible outside the character body
4. Adjust these values:

   | Property | What It Does | How to Set It |
   |----------|-------------|---------------|
   | **Height** | How tall the capsule is | Increase until top dome covers the head |
   | **Center Y** | Vertical center of capsule | Always **Height ÷ 2** |
   | **Radius** | How wide the capsule is | Start 0.5 (visible), shrink to 0.3 when done |
   | **Skin Width** | Collision buffer | 0.08 (default is fine) |

5. **How to fit it — step by step:**
   - Set Height: 1.0, Center Y: 0.5 → capsule only covers legs
   - Increase Height: 1.5, Center Y: 0.75 → capsule reaches the chest
   - Increase Height: 1.8, Center Y: 0.9 → capsule almost reaches the head
   - Keep going until the head is just inside the top dome
   - The **bottom** of the capsule should sit at the character's feet

6. Once the Height is right, shrink Radius from 0.5 → 0.3 for a tighter fit (arms will stick out — that's normal, capsules can't match arm width)

```
WRONG (too short):             RIGHT (fits the body):
      ___                            ___
     /   \                          / O \     ← head inside top dome
     |   |   O  ← head outside    /--|--\
     |   |  /|\                    | /|\ |
     \___/  / \                    |/ | \|
                                   \  |  /
                                    \___/     ← feet at bottom
```

> **💡 Quick reference values for Y Bot:** Height: ~2.0, Center Y: ~1.0, Radius: 0.3. But always verify visually — different Mixamo characters have different heights!

### Step 6: Fix Camera Look-At Point

The camera's `lookAtOffset` might need adjusting since the character is a different shape:

1. Select your **Main Camera** (or the object with CameraFollow/ThirdPersonCamera)
2. Find the **Look At Offset** setting
3. Set it to roughly: **Y: 1.5** (should aim at the character's chest/head area)
4. Adjust until it looks right in Play mode

---

## Part 6: Adding a Basic Idle Animation (10 minutes)

Without an animation, the character stands in a stiff T-Pose (arms straight out). Let's fix that with a simple Animator setup.

### 🎓 Quick Animator Concepts

Unity uses an **Animator Controller** to decide which animation to play:

```
Animator Controller = State Machine
┌─────────────────────────────────┐
│                                 │
│  [Entry] ──→ [Idle Animation]   │  ← Just one state for now!
│                                 │
│  (We'll add Walk/Run later)     │
│                                 │
└─────────────────────────────────┘
```

For now, we only need **one state: Idle**. The character will play the idle animation no matter what. We'll add walk/run animations in a future week.

### Step 1: Create an Animator Controller

1. In **Project** panel, right-click inside `Assets/Characters/YBot/`
2. **Create → Animator Controller**
3. Name it **"PlayerAnimator"**

### Step 2: Set Up the Idle State

1. **Double-click** the PlayerAnimator to open the **Animator** window
2. The Animator window opens with two default states: **Entry** and **Any State**
3. Find your idle animation clip:
   - In the Project panel, expand your idle animation FBX (click ▶)
   - You'll see the animation clip inside (it has a play button ▶ icon)
4. **Drag the animation clip** from the Project panel into the **Animator window**
5. It creates a new state (orange box). This is automatically the **default** state because it's the first one you added
   - Default state has an orange color
   - The arrow from "Entry" should point to it

### Step 3: Assign the Animator Controller

1. Select your **character model** child object in the Hierarchy (e.g., YBot under Player)
2. In the Inspector, find the **Animator** component (it should already be there from the import)
   - If not, click **Add Component → Animator**
3. Drag your **PlayerAnimator** controller into the **Controller** field
4. Make sure **Apply Root Motion** is **unchecked** ☐

   > **🚨 CRITICAL: Apply Root Motion must be OFF!** 
   > 
   > Root Motion means "let the animation move the character." We do NOT want this — our PlayerController script handles all movement. If Root Motion is on, the character will drift around on its own and ignore your WASD input.
   > 
   > | Apply Root Motion | What Happens |
   > |-------------------|-------------|
   > | ✅ ON (BAD) | Animation moves the character → conflicts with our script |
   > | ☐ OFF (GOOD) | Animation only plays visually → our script controls position |

### Step 4: Test It!

1. **Press Play**
2. Your character should be standing in the idle animation (subtle breathing/swaying)
3. Press **WASD** — the character moves around just like the capsule did!
4. Press **Shift** — sprint still works
5. Camera should still follow and orbit

**The character slides around like they're on ice — no walk animation yet.** That's expected! We'll add walk/run animations in a future lesson when we learn about Blend Trees.

---

## Part 7: Understanding What We Built (5 minutes)

### 🎓 What Changed vs What Stayed the Same

| Component | Changed? | Notes |
|-----------|----------|-------|
| PlayerController.cs | ❌ No change | Still handles WASD, sprint, gravity |
| ThirdPersonCamera.cs | ❌ No change | Still follows and orbits the player |
| CharacterController | ⚙️ Resized | Adjusted height/radius to fit character |
| Capsule Mesh | 🗑️ Hidden | No longer visible |
| Character Model | ✨ NEW | Child of Player GameObject |
| Animator Controller | ✨ NEW | Plays idle animation |

**Key Insight:** The movement system doesn't care about visuals. CharacterController handles physics collision, PlayerController handles input and movement math, and the character model is just the "costume" on top.

### 🎓 Parent-Child Relationship

```
Player (parent)
├── Has: CharacterController, PlayerController
├── Moves with: CharacterController.Move()
│
└── YBot (child)
    ├── Has: Animator, SkinnedMeshRenderer
    ├── Moves with: parent (automatic!)
    └── Animates: independently via Animator
```

When the parent (Player) moves, all children move with it automatically. The child (YBot) doesn't need any movement code — it just rides along. This is **transform hierarchy** in action.

### 🎓 SkinnedMeshRenderer vs MeshRenderer

Our old capsule used **MeshRenderer** — for simple, rigid shapes.  
The humanoid uses **SkinnedMeshRenderer** — for deformable meshes with bones.

| Renderer | Used For | Bones? |
|----------|----------|--------|
| MeshRenderer | Static shapes (cubes, capsules, props) | No |
| SkinnedMeshRenderer | Characters, anything that bends | Yes |

"Skinned" means the mesh has skin that stretches over bones. When the Animator moves a bone (like bending an elbow), the SkinnedMeshRenderer deforms the mesh vertices around that joint, creating smooth bending instead of rigid rotation.

---

## Part 8: Bonus — Drop an NPC in the Scene (If Time Permits, ~15 min)

If you finished early, let's use the remaining time to set up a **second character** as a standing NPC. This previews what we'll do with enemies later — and it looks way cooler than an empty world.

### Step 1: Download a Different Character from Mixamo

1. Go back to **Mixamo** → **Characters** tab
2. Pick a **different** character than your player (variety is more fun!)
   - If your player is Y Bot, try **Mutant**, **Peasant Girl**, or **X Bot**
3. Download: **FBX for Unity**, **T-Pose**
4. Also download an idle animation for this character (search "Idle" → **Without Skin** → FBX)

### Step 2: Import & Configure (Same Steps as Before)

1. Create folder: `Assets/Characters/NPC_Name/` (e.g., `Assets/Characters/Mutant/`)
2. Drag character FBX in → **Rig tab** → **Humanoid** → **Apply**
3. Drag idle animation FBX into `Animations/` subfolder → **Rig** → **Humanoid** → **Copy From Other Avatar** (use the NPC's avatar) → **Apply**
4. Animation tab → **Loop Time** ✅ → **Apply**
5. Create a **new Animator Controller** called `NPCAnimator` → drag idle clip in as default state

> 💡 **Why a separate Animator Controller?** Different characters might have different animation setups. Keeping them separate is cleaner. Later when we add enemy AI, the NPC will have patrol/chase/attack states while the player has movement/shooting states.

### Step 3: Place the NPC in the Scene

1. Create a new **empty GameObject**: right-click Hierarchy → **Create Empty** → name it `NPC`
2. Drag the NPC character FBX onto `NPC` as a child (same as we did for the player)
3. Set the NPC child's position to **Y: 0** (NPCs don't need a CharacterController offset — they're just standing there)
4. Add the **Animator** component if it's not already on the model, assign `NPCAnimator`
5. **Apply Root Motion**: ☐ Off
6. Position the NPC somewhere interesting — maybe standing near a wall, facing the player's spawn point

```
Scene Layout:
┌─────────────────────────────────┐
│                                 │
│    NPC 🧍                       │
│         (idle animation)        │
│                                 │
│              Player 🏃          │
│              (you control)      │
│                                 │
│    Ground Plane                 │
└─────────────────────────────────┘
```

### Step 4: Walk Up to the NPC

Press **Play** and walk your character over to the NPC:
- Both characters should be animating (idle breathing/swaying)
- You can walk around the NPC and orbit the camera
- The NPC just stands there — we'll add AI later!

### 🎓 Discussion: What Would We Need to Make This NPC Do Something?

Talk through these ideas (no coding, just thinking):

| Feature | What We'd Need |
|---------|---------------|
| NPC walks around | A script that calls `CharacterController.Move()` or uses **NavMesh** |
| NPC chases the player | A way to get the player's position + NavMesh pathfinding |
| NPC has health | An `IDamageable` interface + `Health` component (coming in a few weeks) |
| NPC attacks | Animation triggers + damage dealing on a timer or range check |
| NPC dies | `Destroy(gameObject)` when health reaches 0 |

> **All of this is coming in future weeks.** For now, the NPC is just proof that you can populate your world with characters.

---

## Part 9: Extra Bonus — Preview Walk/Run Animations (~10 min)

Still have time? Let's preview what's coming next week by downloading walk/run animations — we won't wire them up yet, just see them in Unity's preview.

### Step 1: Download Walk & Run from Mixamo

1. **Animations** tab → Search **"Walking"** → pick a natural walk
   - ✅ Check **"In Place"** 
   - Download: FBX, **Without Skin**
2. Search **"Running"** → pick a matching run
   - ✅ Check **"In Place"**
   - Download: FBX, **Without Skin**

### Step 2: Import into Unity

1. Drag both into `Assets/Characters/YBot/Animations/` (your player character's animations folder)
2. For each one:
   - **Rig** tab → **Humanoid** → **Copy From Other Avatar** (use player's avatar) → **Apply**
   - **Animation** tab → **Loop Time** ✅ → **Apply**

### Step 3: Preview in Unity

1. Click on the imported animation FBX → expand the ▶
2. Click the **animation clip** inside
3. In the Inspector, at the bottom, you'll see a **Preview** window
4. Click the **Play** button ▶ in the preview
5. Watch your character walk/run in the preview window!

> 💡 You can drag a different character model into the preview to see ANY humanoid play ANY animation. This is animation retargeting in action — the walk from Mixamo works on any humanoid model.

### 🎓 Quick Question for the Student

"Right now, how does Unity know when to play Walk vs Run vs Idle?"

**Answer:** It doesn't — yet! We only have one state (Idle) in our Animator Controller. Next week, we'll learn about **Blend Trees** — a way to smoothly blend between Idle, Walk, and Run based on how fast the player is moving. The `PlayerController` already tracks speed... we just need to connect it to the Animator.

```csharp
// This line already exists in our PlayerController:
public float CurrentSpeed => moveDirection.magnitude;

// Next week we'll add something like:
animator.SetFloat("Speed", CurrentSpeed);
```

That one line is all it takes to connect movement to animation. But the Blend Tree setup is the fun part — that's next week!

---

## 🐛 Troubleshooting

### Character looks white/untextured (no color, just white mesh)

This is the **most common Mixamo import issue**. There are several fixes depending on what's happening:

#### Fix 1: Extract Materials (Most Common Fix)

By default, Unity embeds materials *inside* the FBX file and they often don't render correctly. Extracting them fixes this:

1. **Click on the character FBX** in the Project panel
2. Go to the **"Materials"** tab in Inspector
3. Click **"Extract Materials..."** button
4. Choose the same folder the FBX is in (e.g., `Assets/Characters/YBot/`)
5. Click **"Apply"** if prompted
6. Unity creates separate `.mat` files — the character should now show colors

```
Before extracting:              After extracting:
Assets/Characters/YBot/         Assets/Characters/YBot/
├── YBot.fbx                    ├── YBot.fbx
│   └── (materials trapped      ├── Alpha_Body.mat      ← extracted!
│        inside FBX)             ├── Alpha_Joints.mat    ← extracted!
│                                └── ...
White character! 😠              Colored character! 😊
```

#### Fix 2: Check Your Render Pipeline

If extracting didn't help and things still look white or pink/magenta:

1. Check your project's render pipeline:
   - **Window → Rendering → Render Pipeline Asset** (or check **Edit → Project Settings → Graphics**)
   
2. If you're using **URP (Universal Render Pipeline)**:
   - Select all the extracted `.mat` files
   - In Inspector, change **Shader** from `Standard` to `Universal Render Pipeline/Lit`
   - Or do it in bulk: **Edit → Rendering → Materials → Convert All Built-in Materials to URP**

3. If you're using **Built-in Render Pipeline** (default for new projects):
   - Materials should use `Standard` shader — this usually works out of the box
   - If white, check that textures are assigned (see Fix 3)

#### Fix 3: Manually Assign Colors (Quick & Easy)

If you just want to get moving and worry about pretty textures later:

1. **Create a new Material**: Right-click in Project → **Create → Material**
2. Name it something like `Player_Body`
3. In Inspector, click the **color square** next to **Albedo** (or Base Map in URP)
4. Pick a color you like (blue for player, red for enemy, etc.)
5. **Drag the material** onto your character in the Scene view
   - Or: Select the character → find **Skinned Mesh Renderer** → expand **Materials** → drag your material into the slot

This actually looks great for a stylized game! Solid colors are a legitimate art style.

```
Option A (Textured):    Option B (Solid Color):     Option C (Two-Tone):
┌──────────┐            ┌──────────┐                ┌──────────┐
│ Detailed │            │          │                │ ██Blue██ │ ← body
│ textures │            │  Solid   │                │          │
│ realistic│            │  Blue    │                │ ██Gray██ │ ← joints
│ skin/etc │            │          │                │          │
└──────────┘            └──────────┘                └──────────┘
  Pro look               Clean & simple              Stylized
```

#### Fix 4: Download Characters WITH Textures from Mixamo

Some Mixamo characters have textures, some don't:

| Character | Has Textures? | Notes |
|-----------|:------------:|-------|
| Y Bot | ❌ | Geometric, uses solid colors only |
| X Bot | ❌ | Same as Y Bot |
| Mutant | ✅ | Has full texture maps |
| Michelle | ✅ | Realistic skin textures |
| Peasant Girl | ✅ | Full fantasy textures |
| Vampire | ✅ | Detailed textures |

**If you want textured characters**, try Mutant, Michelle, or Peasant Girl. After importing, do Fix 1 (Extract Materials) and the textures should show up.

> 💡 **Don't stress about this!** White/solid-color characters work perfectly fine for learning. We're focused on movement and game mechanics, not art direction. You can always swap characters later.

### Character is in T-Pose (arms out, stiff)
- **Animator Controller not assigned.** Select the character child → Inspector → Animator → drag in PlayerAnimator
- **No default state.** Open Animator window, right-click your Idle state → "Set as Layer Default State"

### Character is floating/sinking
- Adjust the character child's **Y position**. Try values between -0.9 and -1.1
- Check CharacterController's **Center Y** value — should be half the Height

### Character moves but slides (no walk animation)
- **Expected!** We only set up Idle for now. Walk/Run animations come in a future week.

### Character rotates weirdly / faces wrong direction
- Select the character child and set **Rotation Y** to 0 (or 180 if backwards)
- Make sure the character's **forward direction** matches the parent's forward (blue arrow in Scene view)

### Character is HUGE or tiny
- Select the character child → adjust **Scale** (try 0.01 if imported from some sources, or 1.0 for Mixamo)
- Mixamo characters are usually correct at Scale 1

### "Apply Root Motion" warning
- Make sure it's **unchecked**. Root Motion conflicts with our PlayerController script.

### Movement feels different after swapping
- The CharacterController shape might have changed. Re-check Height, Radius, Center
- Make sure the CharacterController is still on the **parent** Player object, not on the character child

### Character's materials look pink/magenta
- This means missing shaders/materials. 
- Select the character FBX → **Materials** tab → click **"Extract Materials"**
- Or: Click **"Remapped Materials"** → Assign Unity's default material

---

## ✅ Week 8 Complete!

### What You Built:
- ✅ Downloaded a humanoid character from Mixamo
- ✅ Imported and configured FBX with Humanoid rig
- ✅ Replaced the capsule with a real 3D character
- ✅ Set up Animator Controller with idle animation
- ✅ Character moves with WASD just like before!

### What You Learned:
- **3D Model structure** — Mesh, Materials, Skeleton, Avatar
- **Rigging** — Putting bones inside a mesh so it can animate
- **FBX format** — Universal 3D file container
- **Humanoid Avatar** — Unity's bone mapping system for animation retargeting
- **Animator Controller** — State machine that decides which animation plays
- **Apply Root Motion** — When the animation vs your code controls movement
- **SkinnedMeshRenderer** — Renderer for deformable meshes with bones
- **Parent-child transforms** — Child objects move with their parent

### C# Concepts Reinforced:
- Component-based architecture (new components, same scripts)
- Separation of concerns (visual separate from logic)
- The power of abstraction (CharacterController doesn't care about visuals)

---

## 💡 Homework Challenges (Optional)

1. **Try a different character:** Go back to Mixamo and download a completely different character. Swap it in — your movement should still work!

2. **Download more animations:** Get Walk, Run, and Jump animations from Mixamo. Don't set them up yet — just import them and check the Rig tab is set to Humanoid. We'll use them soon.

3. **Experiment with scale:** Try making your character really big (Scale 3) or really small (Scale 0.5). What happens to the CharacterController? What needs adjusting?

4. **Multiple characters in scene:** Duplicate your Player and put two characters side by side. Can both move independently? (Hint: you'd need different input or AI — but it's fun to try!)

---

## 🔮 What's Coming Next

Now that we have a real character, the next steps get even more exciting:

- **Week 9:** Walk & Run Animations — Blend Trees make the character animate smoothly based on movement speed
- **Week 10:** Shooting Mechanics — Raycasting to shoot + adding a weapon model
- **Later:** Enemy AI with humanoid characters, health systems, full NPC shooter!

The capsule era is over. Welcome to the humanoid era. 🎮
