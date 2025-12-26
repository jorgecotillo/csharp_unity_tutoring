# Week 2: Growing and Shrinking Objects 📏

**Today's Goal:** Make a cube grow bigger when you press the spacebar and shrink when you let go!  
**Time:** 1 hour  
**New Concepts:** Keyboard input (new Input System), Scale (size), if/else statements  
**C# Concepts:** Conditionals, Input System package, Vector3  

---

## ⚠️ IMPORTANT: Setup Required BEFORE Starting!

This week uses Unity's **new Input System** (the modern, recommended way). You must install it first:

1. In Unity, go to **Window** → **Package Manager**
2. In the top-left dropdown, select **Unity Registry**
3. Scroll down and find **Input System**
4. Click **Install** (bottom-right)
5. Unity will ask to restart → Click **Yes**
6. Wait for Unity to reopen

**✅ You're ready when Unity restarts successfully!**

---

## What You'll Build Today

A cube that you control! Press and hold the spacebar to make it **grow bigger**. Let go and it **shrinks back down**. Like a balloon inflating and deflating! 🎈

**Success = Controlling the cube's size with the spacebar!** ✨

---

## Session Structure (60 minutes)

### Part 1: Week 1 Recap & Concepts Review (10 minutes)
### Part 2: Understanding Keyboard Input (10 minutes)
### Part 3: Understanding Scale (Size) (10 minutes)
### Part 4: BREAK - Stand up and move! (5 minutes)
### Part 5: Write Your Scaling Script (20 minutes)
### Part 6: Challenges & Homework (5 minutes)

---

## Part 1: Week 1 Recap & Key Concepts (10 minutes)

### What We Learned Last Week
Last week you made a **rainbow cube** that changes colors automatically! Let's review the important concepts.

---

### 📌 Concept Review: `Time.deltaTime` (Frame-Rate Independence)

**Remember this code?**
```csharp
hueValue += colorChangeSpeed * Time.deltaTime;
```

Let's make sure you REALLY understand `Time.deltaTime`:

#### **The Problem Without It:**
Different computers run at different speeds (FPS = Frames Per Second).

**Example WITHOUT `Time.deltaTime`:**
```csharp
hueValue += colorChangeSpeed;  // BAD! Don't do this!
```

- **Fast computer (60 FPS):** `Update()` runs 60 times per second
  - Adds `colorChangeSpeed` 60 times = way too fast! 🏃‍♂️💨
  
- **Slow computer (30 FPS):** `Update()` runs 30 times per second  
  - Adds `colorChangeSpeed` 30 times = half the speed 🐌

**Not fair! The game runs at different speeds on different computers!**

---

#### **The Solution: `Time.deltaTime`**

`Time.deltaTime` = **"How much time passed since the last frame"** (in seconds)

**Fast computer (60 FPS):**
- Each frame takes: 1 second ÷ 60 frames = **0.0167 seconds** per frame
- `Time.deltaTime` = 0.0167
- Each frame adds: `1 × 0.0167 = 0.0167` to hueValue
- **After 1 second (60 frames):**
  - Frame 1: `hueValue = 0 + 0.0167 = 0.0167`
  - Frame 2: `hueValue = 0.0167 + 0.0167 = 0.0334`
  - Frame 3: `hueValue = 0.0334 + 0.0167 = 0.0501`
  - ...keep adding 60 times...
  - Frame 60: `hueValue ≈ 1.0`
  - **Total: 0.0167 × 60 = 1.0** ✅

**Slow computer (30 FPS):**
- Each frame takes: 1 second ÷ 30 frames = **0.0333 seconds** per frame
- `Time.deltaTime` = 0.0333
- Each frame adds: `1 × 0.0333 = 0.0333` to hueValue
- **After 1 second (30 frames):**
  - Frame 1: `hueValue = 0 + 0.0333 = 0.0333`
  - Frame 2: `hueValue = 0.0333 + 0.0333 = 0.0666`
  - Frame 3: `hueValue = 0.0666 + 0.0333 = 0.0999`
  - ...keep adding 30 times...
  - Frame 30: `hueValue ≈ 1.0`
  - **Total: 0.0333 × 30 = 1.0** ✅

**Both computers complete 1 rainbow cycle in exactly 1 second!** ⏱️

---

#### **Simple Way to Think About It:**

**Without `Time.deltaTime`:** "Take 1 step" every frame
- Fast computer checks more often = runs faster ❌

**With `Time.deltaTime`:** "Move at 1 meter per second"
- Fast computer takes many tiny steps
- Slow computer takes fewer big steps  
- Both travel the same distance in the same time! ✅

**Rule to Remember:** When changing values in `Update()`, ALWAYS multiply by `Time.deltaTime`!

---

### 📌 Concept Review: Inheritance in C# (`: MonoBehaviour`)

**Remember this line?**
```csharp
public class ColorChanger : MonoBehaviour
```

#### **What is Inheritance?**
When one class **inherits** from another, it gets all the features of that class automatically!

**Think of it like this:**
- Your parents are the "base class"
- You are the "child class"  
- You inherit their DNA (eye color, height genes, etc.)
- But you're still your own person with your own features!

---

#### **In Unity:**

```csharp
public class ColorChanger : MonoBehaviour
```

- `ColorChanger` is YOUR class (what you're creating)
- `: MonoBehaviour` means "inherit from MonoBehaviour"
- `MonoBehaviour` is Unity's base class that gives you special powers!

**What you get for free from MonoBehaviour:**
- `Start()` function - runs once when game starts
- `Update()` function - runs every frame
- `GetComponent<>()` - find other components
- `transform` - access to position, rotation, scale
- `gameObject` - the object this script is attached to
- And MANY more Unity features!

**Without `: MonoBehaviour`:**
```csharp
public class ColorChanger  // Just a regular C# class
{
    void Update()  // This won't work! Unity won't call it!
    {
        // ...
    }
}
```
Unity won't recognize it! No `Start()`, no `Update()`, can't attach to GameObjects!

**With `: MonoBehaviour`:**
```csharp
public class ColorChanger : MonoBehaviour  // Unity script!
{
    void Update()  // Unity automatically calls this every frame!
    {
        // ...
    }
}
```
Unity recognizes it as a script component you can attach to GameObjects! ✅

---

#### **Real-World Analogy:**

**MonoBehaviour = Superhero Suit**
- Wearing it gives you superpowers (Start, Update, transform, etc.)
- Without it, you're just a regular person (regular C# class)
- All Unity scripts must "wear the suit" (inherit from MonoBehaviour)

---

---

### 📌 Concept Review: `public` vs `[SerializeField]`

**Remember these variables from Week 1?**
```csharp
public float colorChangeSpeed = 0.1f;
```

#### **Why `public` Instead of `[SerializeField]`?**

In Unity, there are **two ways** to make variables show up in the Inspector:

**Option 1: `public` (What we're using):**
```csharp
public float colorChangeSpeed = 0.1f;
```
- ✅ Shows in Inspector automatically
- ✅ Simple and beginner-friendly
- ✅ Other scripts can read AND change this value
- ⚠️ Less protected (anyone can modify it)

**Option 2: `[SerializeField] private` (More advanced):**
```csharp
[SerializeField] private float colorChangeSpeed = 0.1f;
```
- ✅ Shows in Inspector
- ✅ **Hidden** from other scripts (better protection)
- ✅ Professional approach
- 📚 We'll learn this in later weeks!

---

#### **When to Use Each:**

**Use `public` (like we do now):**
- Learning/prototyping
- Simple projects
- When other scripts need to access the value

**Use `[SerializeField] private` (later):**
- Bigger projects
- Want to protect values from accidental changes
- Following professional C# practices

**For now, we use `public` because it's simpler to understand!**

---

### Quick Quiz (Ask Student):
1. **What does `Time.deltaTime` give us?** 
   - Answer: Time since last frame in seconds
   
2. **Why do we use `Time.deltaTime`?**
   - Answer: So the game runs at the same speed on all computers
   
3. **What does `: MonoBehaviour` mean?**
   - Answer: Our class inherits Unity's special features like Start() and Update()

4. **Why do we use `public` instead of `[SerializeField]`?**
   - Answer: It's simpler and shows in Inspector automatically

**Great! Now let's learn something new!**

---

## Part 2: Understanding Keyboard Input (10 minutes)

### What is Input?
**Input** = Getting information from the player (keyboard, mouse, gamepad, etc.)

Today we'll use Unity's **new Input System** (the modern way) to read the **keyboard**!

---

### Why the "New" Input System?

Unity has TWO ways to handle input:

1. **Old way (legacy):** `Input.GetKey()` - simple but limited
2. **New way (recommended):** `Keyboard.current.spaceKey.isPressed` - more powerful!

We're learning the **new way** because:
- ✅ It's what Unity recommends for all new projects
- ✅ Works better with gamepads, touch screens, and custom controllers
- ✅ It's the future of Unity input!

---

### How the New Input System Works

**Step 1:** Import the package (you already did this in setup!)
```csharp
using UnityEngine.InputSystem;  // Add this at the top!
```

**Step 2:** Check if keyboard exists, then check for key presses
```csharp
if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
{
    Debug.Log("Spacebar is being held down!");
    // This runs every frame while you hold Space
}
```

**Breaking it down:**
- `Keyboard.current` - Gets the current keyboard (null if no keyboard plugged in)
- `!= null` - Checks that a keyboard exists (safety check!)
- `spaceKey.isPressed` - True while the spacebar is held down

---

### Key States in New Input System

#### **1. `isPressed`**
**When it's true:** While the key is **being held down**  
**Like:** Holding your finger on a doorbell - it rings the whole time  
**Use for:** Continuous actions (moving, growing, shooting while held)

```csharp
if (Keyboard.current.spaceKey.isPressed)
{
    Debug.Log("Spacebar is being held down!");
    // Prints every frame while you hold Space
}
```

---

#### **2. `wasPressedThisFrame`**
**When it's true:** The **moment** you press the key (only first frame)  
**Like:** Clicking a light switch ON - happens once  
**Use for:** One-time actions (jump, toggle, menu open)

```csharp
if (Keyboard.current.spaceKey.wasPressedThisFrame)
{
    Debug.Log("Spacebar was just pressed!");
    // Prints ONCE when you first press Space
}
```

**⚠️ Important Difference from `isPressed`:**

Even if you **keep holding** the space key down:
- `wasPressedThisFrame` = ✅ **only on frame 1**, then ❌ for all remaining frames
- `isPressed` = ✅ **every frame** while you're holding it down

**Example: Holding space for 3 seconds (180 frames at 60 FPS)**
```
Frame 1:    wasPressedThisFrame = ✅  |  isPressed = ✅
Frame 2:    wasPressedThisFrame = ❌  |  isPressed = ✅
Frame 3:    wasPressedThisFrame = ❌  |  isPressed = ✅
Frame 4-180: wasPressedThisFrame = ❌  |  isPressed = ✅
```

**This prevents "spam jumping"** - you must release and press again to trigger `wasPressedThisFrame` a second time!

---

#### **3. `wasReleasedThisFrame`**
**When it's true:** The **moment** you release the key  
**Like:** Clicking a light switch OFF - happens once  
**Use for:** Release actions (stop charging, let go of bow)

```csharp
if (Keyboard.current.spaceKey.wasReleasedThisFrame)
{
    Debug.Log("Spacebar was just released!");
    // This prints ONCE when you let go of Space
}
```

---

### Visual Timeline:

```
Time:                0s -------- 1s -------- 2s -------- 3s
Action:                  [Press]  [Holding]  [Release]
wasPressedThisFrame:     ✅       ❌         ❌
isPressed:               ✅       ✅         ❌
wasReleasedThisFrame:    ❌       ❌         ✅
```

---

### Quick Practice: Which Function to Use?

**Scenario 1:** Make a character run while holding W  
**Answer:** `Keyboard.current.wKey.isPressed` - continuous action

**Scenario 2:** Make a character jump when you tap Space  
**Answer:** `Keyboard.current.spaceKey.wasPressedThisFrame` - one-time action

**Scenario 3:** Stop a charging attack when you release the mouse button  
**Answer:** `Mouse.current.leftButton.wasReleasedThisFrame` - release action

**What's a charging attack?**  
Think of holding down a button to "charge up" power (like a bow pulling back, or a glowing energy ball getting bigger), then releasing the button to fire! You need `wasReleasedThisFrame` to detect the exact moment you let go.

**Simple example:**
- **While holding mouse button:** Object glows brighter, grows bigger, particles appear
- **When you release:** Fire the powered-up attack!
- **Why not use `isPressed`?** Because that would fire constantly while charging instead of firing once when you release!

---

### Common Keys in New Input System

```csharp
Keyboard.current.spaceKey       // Spacebar
Keyboard.current.wKey           // W key
Keyboard.current.aKey           // A key  
Keyboard.current.sKey           // S key
Keyboard.current.dKey           // D key
Keyboard.current.upArrowKey     // Up arrow
Keyboard.current.downArrowKey   // Down arrow
Keyboard.current.leftArrowKey   // Left arrow
Keyboard.current.rightArrowKey  // Right arrow
Keyboard.current.enterKey       // Enter key
Keyboard.current.escapeKey      // Escape key
Keyboard.current.leftShiftKey   // Left Shift
```

---

### For Today's Project:
We'll use **`Keyboard.current.spaceKey.isPressed`** because we want the cube to keep growing **while** we hold the spacebar!

---

## Part 3: Understanding Scale (Size) (10 minutes)

### What is Scale?
**Scale** = How big or small an object is

In Unity, every GameObject has a **Transform** component with three properties:
1. **Position** - Where it is (X, Y, Z location)
2. **Rotation** - Which way it's facing (angle)
3. **Scale** - How big it is (size)

---

### Transform.localScale

```csharp
transform.localScale
```

This gives you a **Vector3** with three numbers: (X, Y, Z)

**Example:**
```csharp
Vector3 currentScale = transform.localScale;
// If cube's scale is (2, 2, 2):
// currentScale.x = 2
// currentScale.y = 2  
// currentScale.z = 2
```

---

### What Do X, Y, Z Mean for Scale?

- **X** = Width (left-right size)
- **Y** = Height (up-down size)
- **Z** = Depth (forward-back size)

**Examples:**

| Scale | What It Looks Like |
|-------|-------------------|
| (1, 1, 1) | Normal size (default) |
| (2, 2, 2) | 2× bigger in all directions |
| (0.5, 0.5, 0.5) | Half the size |
| (3, 1, 1) | Wide and flat (stretched horizontally) |
| (1, 5, 1) | Tall and thin (stretched vertically) |

---

### Uniform vs Non-Uniform Scaling

**Uniform Scaling:** All dimensions change the same  
```csharp
transform.localScale = new Vector3(2, 2, 2);  // Stays proportional
```

**Non-Uniform Scaling:** Different dimensions  
```csharp
transform.localScale = new Vector3(3, 1, 1);  // Gets stretched/squashed
```

**Today we'll use uniform scaling** so our cube stays cube-shaped!

---

### Vector3.one (The Shortcut!)

Instead of writing `new Vector3(1, 1, 1)`, Unity gives us a shortcut:

```csharp
Vector3.one  // Same as new Vector3(1, 1, 1)
```

**Useful for scaling:**
```csharp
// Grow by 0.1 in all directions
transform.localScale += Vector3.one * 0.1f;
// Same as: transform.localScale += new Vector3(0.1f, 0.1f, 0.1f);
```

**Other handy shortcuts:**
```csharp
Vector3.zero   // (0, 0, 0)
Vector3.up     // (0, 1, 0)
Vector3.right  // (1, 0, 0)
Vector3.forward // (0, 0, 1)
```

---

### How to Change Scale

```csharp
// Get current scale
Vector3 currentScale = transform.localScale;

// Make it bigger
currentScale += Vector3.one * 0.5f;  // Add 0.5 to each dimension

// Make it smaller
currentScale -= Vector3.one * 0.5f;  // Subtract 0.5 from each dimension

// Apply the new scale
transform.localScale = currentScale;
```

---

## Part 4: BREAK TIME! (5 minutes) 🧘

Stand up! Stretch! Rest your eyes!

**Quick stretches:**
- Reach for the ceiling 
- Touch your toes
- Roll your shoulders
- Look far away (good for eyes!)

---

## Part 5: Write Your Scaling Script (20 minutes)

### Step 1: Create the Scene
1. **Open Unity** and create a **new scene** or use your RainbowCube scene
2. **Create a Cube** (Hierarchy → Right-click → 3D Object → Cube)
3. **Position it** at (0, 0, 0) so you can see it well
4. **Save the scene** as "ScalingCube" (Ctrl+S)

---

### Step 2: Create the Script
1. In **Project** window, go to your **Scripts** folder
2. Right-click Scripts folder → **Create** → **Scripting** → **Empty C# Script**
3. Name it **"ObjectScaler"** (no spaces!)
4. Press Enter

---

### Step 3: Write the Code

Double-click **ObjectScaler** to open it, then **replace everything** with:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class ObjectScaler : MonoBehaviour
{
    // Settings you can change in Inspector
    public float scaleSpeed = 1f;
    public float minScale = 0.5f;
    public float maxScale = 5f;
    
    void Update()
    {
        // Get the current size
        Vector3 currentScale = transform.localScale;
        
        // Check if spacebar is held down (new Input System)
        if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
        {
            // Grow bigger!
            currentScale += Vector3.one * scaleSpeed * Time.deltaTime;
        }
        else
        {
            // Shrink smaller!
            currentScale -= Vector3.one * scaleSpeed * Time.deltaTime;
        }
        
        // Make sure we don't go too big or too small
        // (Keep all dimensions the same for uniform scaling)
        float clampedScale = Mathf.Clamp(currentScale.x, minScale, maxScale);
        transform.localScale = Vector3.one * clampedScale;
    }
}
```

**Save the file!** (Ctrl+S)

---

### Step 4: Understand the Code (Statement-by-Statement)

#### **Statement 1: `using UnityEngine.InputSystem;`**
**Why:** Imports Unity's new Input System package  
**What it does:** Gives us access to `Keyboard.current` and other input features  
**Required:** Yes! Without this, the code won't recognize keyboard input  

#### **Statement 2: `public float scaleSpeed = 1f;`**
**Why:** Controls how fast the object grows/shrinks  
**Why public:** So we can adjust it in Inspector without editing code  
**Default value:** 1 = grows/shrinks at a moderate speed  
**Unit:** "Size units per second"

---

#### **Statement 2: `public float minScale = 0.5f;`**
**Why:** Prevents object from shrinking to nothing (or going negative!)  
**What it means:** Object can't get smaller than half its original size  
**Why we need it:** Without a limit, object could shrink to invisible or negative size!

---

#### **Statement 3: `public float maxScale = 5f;`**
**Why:** Prevents object from growing infinitely huge  
**What it means:** Object can't get bigger than 5× its original size  
**Why we need it:** Without a limit, object could grow so big it fills the whole screen!

---

#### **Statement 4: `Vector3 currentScale = transform.localScale;`**
**Why:** Get the object's current size so we can change it  
**What we get:** Three numbers (X, Y, Z) representing width, height, depth  
**Example:** If cube's scale is (2, 2, 2), currentScale = (2, 2, 2)

---

#### **Statement 5: `if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)`**
**Why:** Check if keyboard exists and spacebar is being held down  
**Breaking it down:**
- `Keyboard.current` = Get the current keyboard device
- `!= null` = Make sure a keyboard is actually connected (safety check!)
- `&&` = AND operator (both conditions must be true)
- `spaceKey.isPressed` = True while spacebar is held down

**When it's true:** Every frame that spacebar is pressed (and keyboard exists)  
**What happens next:** The code inside the `{ }` runs

---

#### **Statement 6: `currentScale += Vector3.one * scaleSpeed * Time.deltaTime;`**
**Why:** Make the object bigger while spacebar is held  
**Breaking it down:**
- `Vector3.one` = (1, 1, 1) - equal growth in all directions
- `* scaleSpeed` = multiply by our speed setting (default 1)
- `* Time.deltaTime` = adjust for frame rate (makes it smooth!)
- `+=` = add to the current scale

**🔥 Why multiply by Time.deltaTime? (SUPER IMPORTANT!)**

Without it, the cube would grow at **different speeds on different computers**:
- **Fast computer (60 FPS):** Adds `scaleSpeed` 60 times/second = TOO FAST! 🏃‍♂️💨
- **Slow computer (30 FPS):** Adds `scaleSpeed` 30 times/second = TOO SLOW! 🐌

**With Time.deltaTime:**
- Fast computer: Many tiny steps (0.0167 × 60 = 1.0 per second) ✅
- Slow computer: Fewer big steps (0.0333 × 30 = 1.0 per second) ✅
- **Both grow the same amount in 1 second!**

**Example math (60 FPS, scaleSpeed = 1):**
- `Time.deltaTime` ≈ 0.0167 seconds
- `Vector3.one * 1 * 0.0167` = (0.0167, 0.0167, 0.0167)
- Cube grows by 0.0167 units per frame
- After 1 second: grows by 1.0 units total!

**Golden Rule:** Always multiply by `Time.deltaTime` when changing values in `Update()`!

---

#### **Statement 7: `else { ... }`**
**Why:** What to do when spacebar is NOT pressed  
**Means:** "Otherwise" or "if the above condition was false"  
**Result:** Cube shrinks when you're not holding spacebar

---

#### **Statement 8: `currentScale -= Vector3.one * scaleSpeed * Time.deltaTime;`**
**Why:** Make the object smaller when spacebar is released  
**Same as growth, but subtraction:** `-=` instead of `+=`  
**Result:** Shrinks at the same speed it grew

---

#### **Statement 9: `float clampedScale = Mathf.Clamp(currentScale.x, minScale, maxScale);`**
**Why:** Enforce the size limits (prevent too big or too small)  
**What is `Mathf.Clamp`?** A function that keeps a number between a min and max

**How it works:**
```csharp
Mathf.Clamp(value, min, max)
```
- If `value < min` → returns min
- If `value > max` → returns max  
- Otherwise → returns value unchanged

**Examples:**
- `Mathf.Clamp(10, 0, 5)` = 5 (too big, clamped to max)
- `Mathf.Clamp(-2, 0, 5)` = 0 (too small, clamped to min)
- `Mathf.Clamp(3, 0, 5)` = 3 (within range, unchanged)

**Why `currentScale.x`?** We only need one dimension since we're scaling uniformly (X = Y = Z)

---

#### **Statement 10: `transform.localScale = Vector3.one * clampedScale;`**
**Why:** Actually apply the new size to the object!  
**Breaking it down:**
- `Vector3.one * clampedScale` = multiply (1,1,1) by our clamped value
- If clampedScale = 2, result = (2, 2, 2) - uniform scaling!
- `transform.localScale =` apply it to the object

**This is the line that makes the cube visibly change size!**

---

### Step 5: Attach the Script to the Cube
1. Click your **Cube** in the Hierarchy
2. In **Inspector**, click **Add Component**
3. Type **"ObjectScaler"**
4. Click it when it appears

**You should see:**
- Object Scaler (Script) component
- Scale Speed: 1
- Min Scale: 0.5
- Max Scale: 5

---

### Step 6: Test It!
1. Press **Play** ▶️
2. **Hold the Spacebar** - cube should grow!
3. **Let go** - cube should shrink!
4. Watch it stay between the min and max sizes

**If it's not working, check:**
- [ ] Script is attached to Cube
- [ ] No errors in Console
- [ ] Scale Speed is not 0

---

### Step 7: Experiment with Values

**While the game is running, try changing these in the Inspector:**

**Scale Speed:**
- `0.5` = Slow growth/shrink
- `2` = Fast growth/shrink
- `10` = Super fast!

**Min Scale:**
- `0.1` = Can shrink really tiny
- `1` = Can't shrink below original size

**Max Scale:**
- `2` = Can only double in size
- `10` = Can grow huge!

**Remember:** Changes during Play mode don't save! Stop the game first to make permanent changes.

---

### Step 8: Make It More Fun! (Interactive Experimentation)

Now that it works, let's make it more interesting!

#### **Experiment 1: What Happens Without Clamping?**

**Let's break it on purpose to see why clamping matters!**

1. **Comment out the clamping lines** (add `//` in front):
```csharp
// float clampedScale = Mathf.Clamp(currentScale.x, minScale, maxScale);
// transform.localScale = Vector3.one * clampedScale;

// Instead, use this temporarily:
transform.localScale = currentScale;
```

2. **Press Play** and hold Space for a long time
3. **Watch what happens!** The cube grows HUGE! 
4. **Let go** and watch it shrink past zero into **negative size** (it flips inside-out!)

**Ask the student:** "Why is clamping important?"  
**Answer:** Prevents crazy, uncontrolled behavior!

5. **Undo the changes** (remove the `//` to restore clamping)

---

#### **Experiment 2: Non-Uniform Scaling (Squash and Stretch!)**

Let's make the cube stretch like taffy!

**Replace the if/else block with this:**
```csharp
if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
{
    // Grow taller, shrink wider (like pulling taffy up)
    currentScale.y += scaleSpeed * Time.deltaTime;  // Height grows
    currentScale.x -= scaleSpeed * 0.5f * Time.deltaTime;  // Width shrinks
    currentScale.z -= scaleSpeed * 0.5f * Time.deltaTime;  // Depth shrinks
}
else
{
    // Shrink taller, grow wider (back to normal)
    currentScale.y -= scaleSpeed * Time.deltaTime;
    currentScale.x += scaleSpeed * 0.5f * Time.deltaTime;
    currentScale.z += scaleSpeed * 0.5f * Time.deltaTime;
}
```

**Press Play!** The cube stretches tall and thin, then short and fat! 🎪

**Ask:** "What's happening to the X, Y, Z values separately?"

---

#### **Experiment 3: Pulsing Effect (Automatic Growth/Shrink)**

Let's make it pulse like a heartbeat WITHOUT pressing any keys!

**Create a new variable at the top:**
```csharp
private bool growing = true;  // Are we currently growing?
```

**Replace the Update function with this:**
```csharp
void Update()
{
    Vector3 currentScale = transform.localScale;
    
    if (growing)
    {
        currentScale += Vector3.one * scaleSpeed * Time.deltaTime;
        
        // Hit max size? Start shrinking!
        if (currentScale.x >= maxScale)
        {
            growing = false;
        }
    }
    else
    {
        currentScale -= Vector3.one * scaleSpeed * Time.deltaTime;
        
        // Hit min size? Start growing!
        if (currentScale.x <= minScale)
        {
            growing = true;
        }
    }
    
    float clampedScale = Mathf.Clamp(currentScale.x, minScale, maxScale);
    transform.localScale = Vector3.one * clampedScale;
}
```

**Press Play!** The cube pulses on its own! No spacebar needed! 💓

**Ask:** "How is this different from the spacebar version?"  
**Answer:** It uses a boolean to track state and automatically reverses direction!

---

**After experimenting, revert back to the original ObjectScaler code!**

---

## Part 6: Build Something Cool Together! (10 minutes)

### Live Coding Challenge: Multi-Key Scaler

Let's make it respond to **multiple keys** with **different effects**!

**Together with the student, modify ObjectScaler.cs:**

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class MultiKeyScaler : MonoBehaviour
{
    public float scaleSpeed = 1f;
    public float minScale = 0.5f;
    public float maxScale = 5f;
    
    void Update()
    {
        // Safety check - make sure keyboard exists
        if (Keyboard.current == null) return;
        
        Vector3 currentScale = transform.localScale;
        
        // SPACE = Grow uniformly
        if (Keyboard.current.spaceKey.isPressed)
        {
            currentScale += Vector3.one * scaleSpeed * Time.deltaTime;
        }
        
        // LEFT SHIFT = Shrink uniformly
        if (Keyboard.current.leftShiftKey.isPressed)
        {
            currentScale -= Vector3.one * scaleSpeed * Time.deltaTime;
        }
        
        // W = Grow taller (Y axis only)
        if (Keyboard.current.wKey.isPressed)
        {
            currentScale.y += scaleSpeed * Time.deltaTime;
        }
        
        // S = Shrink shorter (Y axis only)
        if (Keyboard.current.sKey.isPressed)
        {
            currentScale.y -= scaleSpeed * Time.deltaTime;
        }
        
        // D = Grow wider (X axis only)
        if (Keyboard.current.dKey.isPressed)
        {
            currentScale.x += scaleSpeed * Time.deltaTime;
        }
        
        // A = Shrink thinner (X axis only)
        if (Keyboard.current.aKey.isPressed)
        {
            currentScale.x -= scaleSpeed * Time.deltaTime;
        }
        
        // Clamp each dimension separately
        currentScale.x = Mathf.Clamp(currentScale.x, minScale, maxScale);
        currentScale.y = Mathf.Clamp(currentScale.y, minScale, maxScale);
        currentScale.z = Mathf.Clamp(currentScale.z, minScale, maxScale);
        
        transform.localScale = currentScale;
    }
}
```

**Now test it!**
- **Space** = Grow bigger
- **Left Shift** = Shrink smaller
- **W/S** = Taller/Shorter
- **A/D** = Thinner/Wider

**You can make crazy shapes!** Try pressing W+D together! 🎨

---

### Quick Build Challenge: Shape Garden (5 minutes)

**Student's task:**
1. Create **5 different 3D objects** (cubes, spheres, cylinders, capsules)
2. Arrange them in a line or circle
3. Attach the **Multi-Key Scaler** script to ALL of them
4. **Press Play** and use different keys to sculpt them into interesting shapes!

**Goal:** Make a "shape garden" where each object has a unique, weird shape!

**Take a screenshot when done!** 📸

---

## Part 7: Wrap-Up & Homework (5 minutes)

### What You Accomplished Today! 🎉
- ✅ Reviewed Time.deltaTime and inheritance concepts
- ✅ Installed and learned Unity's new Input System
- ✅ Learned keyboard input (isPressed, wasPressedThisFrame, wasReleasedThisFrame)
- ✅ Understood Vector3 and scaling
- ✅ Used if/else conditional logic
- ✅ Made objects respond to keyboard input
- ✅ Created multiple interactive scripts
- ✅ Experimented with uniform and non-uniform scaling
- ✅ Built a "shape garden" with multi-key controls!

---

### Quick Homework Challenges

### Challenge 1: The Pulse Garden 💓
**Create 5 objects that pulse automatically at different speeds:**
1. Create 5 spheres in a row
2. Attach **PulsingScaler** script to each (you can find this in your Scripts folder or create it from the experiments above)
3. Give each different settings:
   - Sphere 1: Speed 0.5, Min 0.5, Max 2
   - Sphere 2: Speed 1.5, Min 1, Max 3
   - Sphere 3: Speed 3, Min 0.3, Max 4
   - Sphere 4: Speed 0.3 (very slow pulse!)
   - Sphere 5: Speed 5 (super fast!)
4. Press Play and watch them all pulse at different rhythms!
5. Screenshot it! 📸

---

### Challenge 2: Keyboard Sculptor 🎨
**Create your own custom scaler:**
1. Create a new script called **MyCustomScaler**
2. Make it respond to **different keys** with **different effects**:
   - **Up Arrow** = Grow in Y (taller)
   - **Down Arrow** = Shrink in Y (shorter)
   - **Left Arrow** = Shrink in X (thinner)
   - **Right Arrow** = Grow in X (wider)
   - **R** = Reset to size (1, 1, 1)
3. Attach to a cube and test it!
4. Can you make a really tall, thin tower? Or a flat pancake?

**Hint:** Copy ObjectScaler.cs and modify it!

---

### Challenge 3: The Color + Scale Combo! 🌈📏
**Combine Week 1 and Week 2:**
1. Create a sphere
2. Attach BOTH **ColorChanger** (from Week 1) AND **ObjectScaler** scripts
3. Press Play
4. It should change colors AND scale at the same time!
5. Try different speed combinations

**Bonus:** Can you make it so:
- Fast color change + slow scaling?
- Slow color change + fast scaling?
- What looks coolest? 😎

---

### Homework Assignment

See **HOMEWORK.md** for full details!

**Main Assignment: Shape Sculptor Scene**
1. Create a scene with at least 6 different 3D objects
2. Use a mix of scripts:
   - 2 objects with **ObjectScaler** (spacebar control)
   - 2 objects with **MultiKeyScaler** (WASD control)
   - 2 objects with **PulsingScaler** (automatic pulsing)
3. Arrange them creatively (circle, line, pyramid, etc.)
4. Press Play and interact with them!
5. Create the weirdest, coolest shapes you can!
6. Take screenshots of your favorite creations! 📸

**Bonus Challenges:**
- Combine **ColorChanger** from Week 1 with scaling scripts!
- Create a "breathing" effect (pulse slowly like breathing)
- Make objects that only scale on one axis (tall towers, flat discs)
- Try making a "growth race" - which object reaches max size first?

**Time estimate:** 20-30 minutes

---

## Troubleshooting Guide

### "Nothing happens when I press Space!"
**Check:**
1. Is the game playing? (Play button blue)
2. Is ObjectScaler attached to the cube?
3. Is Scale Speed greater than 0?
4. Click on Game window to make sure it has focus

### "The cube grows but won't stop!"
**Check:**
- Max Scale might be set too high
- Try setting Max Scale to 3 or 5

### "The cube shrinks to nothing!"
**Check:**
- Min Scale might be set too low or to 0
- Try setting Min Scale to 0.5 or 1

### "The cube grows really slow/fast!"
**Solution:**
- Adjust Scale Speed in Inspector
- Try values between 0.5 and 5

---

## Teacher Notes

### Key Teaching Points:
1. **New Input System** - Modern way to handle input in Unity
2. **Input detection** - isPressed vs wasPressedThisFrame vs wasReleasedThisFrame  
3. **If/else logic** - making decisions in code
4. **Vector3** - three numbers working together
5. **Clamping** - keeping values in a safe range
6. **Time.deltaTime** - reinforcing frame-rate independence

### Common Student Questions:

**"Why use the new Input System instead of the old way?"**
The new Input System is Unity's recommended approach and works better with multiple devices. It's what professional games use!

**"What if keyboard is null?"**
That's why we check `!= null` first - it prevents crashes if no keyboard is connected!

**"Why do we clamp?"**
Without clamping, the cube could grow infinitely or shrink to negative size! Demonstrate by temporarily commenting out the clamp line.

**"What if I want it to stay big?"**
That's what `wasPressedThisFrame` is for! We'll explore that in future weeks.

**"Can I make it grow only in one direction?"**
Yes! Instead of `Vector3.one`, use `new Vector3(1, 0, 0)` for X-only growth. Great bonus challenge!

### Session Reflection:

After the session, answer these:

1. **Did the student understand if/else?**
2. **Did they grasp the difference between GetKey and GetKeyDown?**
3. **Did they experiment with the values?**
4. **Engagement level (1-10):**
5. **What to review next week:**

---

## Next Week Preview

**Week 3:** We'll make objects **move** around the screen using WASD keys! You'll create a character that walks around! 🎮🚶‍♂️

---

## Parent Summary

**Today your student:**
- Learned keyboard input detection in Unity
- Understood if/else conditional logic  
- Made objects grow and shrink interactively
- Learned about clamping values to safe ranges
- Reviewed Time.deltaTime and inheritance concepts

**Concepts practiced:**
- User input (new Input System)
- Conditional statements (if/else)
- Vector3 and scaling
- Limiting values (Mathf.Clamp)

**Homework:** Create multiple objects with different scaling behaviors (15-20 min)

**Next week:** Physics & Forces - Learn Rigidbody, velocity, forces, and make objects orbit a planet!
