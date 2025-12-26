# Week 1: Hello Unity - Make a Cube Rainbow! 🌈

**Today's Goal:** Create a cube that changes colors automatically like a rainbow  
**Time:** 1 hour  
**New Concepts:** Unity interface, GameObjects, Scripts, Color changes  
**C# Concepts:** Variables, functions (Start, Update), changing values

---

## What You'll Build Today

A 3D cube that cycles through rainbow colors automatically! You'll press Play and watch it change from red → orange → yellow → green → blue → purple and repeat forever!

**Success = Seeing colors change when you press Play!** ✨

---

## Session Structure (60 minutes)

### Part 1: Unity Interface Tour (10 minutes)
### Part 2: Create Your First Scene (10 minutes)
### Part 3: Write Your First Script (15 minutes)
### Part 4: BREAK - Stand up and move! (5 minutes)
### Part 5: Make It Work & Experiment (15 minutes)
### Part 6: Homework & Wrap-up (5 minutes)

---

## Part 1: Unity Interface Tour (10 minutes)

### The 5 Most Important Windows

When you open Unity, you'll see several panels. Here are the ones we care about today:

1. **Scene View** (Center-left, 3D view)
   - *What it is:* Where you build your game world
   - *What to do:* Click and drag to rotate view
   - *Like:* A 3D modeling program

2. **Game View** (Tab next to Scene)
   - *What it is:* What players will see when playing
   - *What to do:* Click Play button to test
   - *Like:* The actual game screen

3. **Hierarchy** (Left panel, list of objects)
   - *What it is:* List of everything in your scene
   - *What to do:* Click objects to select them
   - *Like:* A file folder showing what's in your scene

4. **Inspector** (Right panel, properties)
   - *What it is:* Shows details about selected object
   - *What to do:* Change values here to modify objects
   - *Like:* Settings panel for whatever you clicked

5. **Project** (Bottom panel, files)
   - *What it is:* All your game files (scripts, images, etc.)
   - *What to do:* Create and organize files here
   - *Like:* Windows File Explorer but for your game

### Quick Activity: Find These Buttons
- ▶️ **Play button** (top center) - Start/stop the game
- ⏸️ **Pause button** (next to Play) - Pause while playing
- **Console tab** (bottom, next to Project) - Shows messages and errors

---

## Part 2: Create Your First Project & Scene (10 minutes)

### Step 1: Open Unity Hub
1. Find and open **Unity Hub** on your computer
   - Look for the Unity Hub icon (not the regular Unity icon)
   - If you can't find it, search "Unity Hub" in Windows search

**What you should see:**  
Unity Hub window with "Projects" tab showing (might be empty if this is your first time)

---

### Step 2: Create a New Project
1. Click the **"New project"** button (top right, blue button)
2. Wait a moment for the project creation window to appear

**What you should see:**  
A window showing different project templates

---

### Step 3: Choose Project Template
1. Look at the templates displayed (the colorful cards/boxes)
2. Find **"3D Mobile"** template (it has a blue "Core" label and shows a simple cube icon)
   - It's in the second row on the left side
   - The description says "Creating a 3D? This template includes recommended packages and settings for 3D Mobile development"
3. Click on the **"3D Mobile"** card to select it

**Alternative:** If you want a simpler option:
- Look for **"Universal 3D"** (top left, purple card with "Core" label) 
- This is also fine for beginners!

**What is a template?**  
It's a starting point with pre-configured settings. "3D Mobile" or "Universal 3D" are both good for learning.

**Note:** Don't worry about all the other templates (Learning, Sample, etc.) - those are for advanced users!

---

### Step 4: Name and Save Your Project
1. After selecting a template (3D Mobile or Universal 3D), look at the right side of the screen
2. You should see **"Project name"** field at the top
3. Type: **RainbowCube**
4. Look at the **"Location"** field below it - this shows where Unity will save your project
   - Default location is usually fine (like Documents/Unity Projects)
   - You can click the folder icon "📁" to choose a different location if you want
   - **Remember where this is!** (You might want to write it down)
5. Click the **"Create project"** button (bottom right)

**What happens next?**  
Unity will take 30-60 seconds (or longer) to create your project and open the Unity Editor. You'll see a progress bar. Be patient - this is normal!

**What you should see:**  
The Unity Editor window opens with several panels (Scene, Game, Hierarchy, Inspector, Project)

---

### Step 5: Understand What You're Looking At
Unity Editor has just opened! You should see:
- **Scene View** (center-left) - A gray 3D space
- **Game View** (tab next to Scene) - What players will see
- **Hierarchy** (left panel) - Shows "Main Camera", "Directional Light", and "Global Volume"
- **Inspector** (right panel) - Empty or showing camera details
- **Project** (bottom) - Shows your Assets folder

**What are these 3 objects in the Hierarchy?**

1. **Main Camera** - The "eyeball" that sees your 3D world
   - Everything this camera sees appears in the Game View (what players see)
   - Like a movie camera - if it's not in front of the camera, it won't be visible!
   - Every scene needs at least one camera

2. **Directional Light** - Acts like the sun
   - Shines light on all objects so you can see them
   - Without light, everything would be pitch black!
   - Gives your objects shadows and makes them visible

3. **Global Volume** - Controls visual effects (post-processing)
   - Makes your game look prettier with effects like bloom (glow), color adjustments, etc.
   - Think of it like Instagram filters for your game
   - **You can ignore this for now** - it's automatically created by the template
   - We won't touch it in this lesson

**Congratulations!** You've created your first Unity project!

---

### Step 6: Save Your Scene
Let's save what we have so far:
1. Click **File** (top menu) → **Save As**
2. Name it: **RainbowCube**
3. Click **Save**

**What just happened?**  
You saved your scene file. The project is the whole folder, the scene is this specific 3D world.

---

### Step 7: Create a Cube
1. Right-click in the **Hierarchy** window (left side)
2. Click **3D Object** → **Cube**
3. A cube appears in your scene!

**What you should see:**
- In **Hierarchy**: "Cube" appears in the list
- In **Scene View**: A gray 3D cube
- In **Inspector**: Details about the cube (when cube is selected)

---

### Step 8: Position the Cube So We Can See It
The cube might be hard to see. Let's move it to a good spot:

1. Click the **Cube** in the Hierarchy (if not already selected)
2. Look at the **Inspector** on the right
3. Find **Transform** at the top
4. Set **Position** to:
   - X: 0
   - Y: 0
   - Z: 0

**What is Position?**
- **X** = Left/Right (negative = left, positive = right)
- **Y** = Up/Down (negative = down, positive = up)
- **Z** = Forward/Back (negative = back, positive = forward)

---

### Step 9: Frame the Cube in View
1. Click the **Cube** in Hierarchy
2. Press **F key** on keyboard (or double-click the cube)
3. The Scene View zooms to show your cube perfectly!

**Practice:** Try rotating the view by clicking and dragging in the Scene View

---

### Step 10: Save Your Work
1. Press **Ctrl+S** on your keyboard
2. This saves any changes you've made

**Tip:** Get in the habit of pressing Ctrl+S frequently!

---

## Part 3: Write Your First Script (15 minutes)

### What is a Script?
A script is code that tells Unity what to do. We'll write one that says "change this cube's color over and over!"

---

### Step 1: Create a Scripts Folder
1. In the **Project** window (bottom), find **Assets** folder
2. Right-click **Assets**
3. Click **Create** → **Folder**
4. Name it **"Scripts"**
5. Press Enter

**Why a folder?** To stay organized! All code goes in Scripts folder.

---

### Step 2: Create the Script
1. Right-click the **Scripts** folder you just made
2. In the menu that appears, click **Create** (it has a small arrow →)
3. In the submenu, look for **Scripting** and hover over it (another arrow →)
4. In the next submenu, click **"Empty C# Script"**
   - You might also see "MonoBehaviour Script" at the top - that works too!
5. A new script file appears in the Scripts folder with a default name highlighted in blue
6. **Immediately** type: **ColorChanger** (no spaces!)
7. Press **Enter** to confirm the name

**⚠️ IMPORTANT:** The name MUST match exactly! C# is picky about names.

---

### Step 3: Open the Script
1. Double-click **ColorChanger** in the Project window
2. Visual Studio (or your code editor) will open

**What you see:**  
A code file with some stuff already in it! Unity creates a template for you.

---

### Step 4: Replace the Code
**Delete everything** in the file and replace it with this:

```csharp
using UnityEngine;

public class ColorChanger : MonoBehaviour
{
    // How fast colors change
    public float colorChangeSpeed = 1f;
    
    private float hueValue = 0f;
    private Renderer objectRenderer;

    void Start()
    {
        // Get the part that shows colors
        objectRenderer = GetComponent<Renderer>();
        Debug.Log("ColorChanger ready!");
    }

    void Update()
    {
        // Increase rainbow position
        hueValue += colorChangeSpeed * Time.deltaTime;
        
        // Reset if we go past the end
        if (hueValue > 1f)
        {
            hueValue = 0f;
        }
        
        // Create and apply the new color
        Color newColor = Color.HSVToRGB(hueValue, 1f, 1f);
        objectRenderer.material.color = newColor;
    }
}
```

---

### Step 5: Understand the Code (Statement-by-Statement Explanation)

Let's understand WHY each statement exists and what it accomplishes:

---

#### **Statement 1: `using UnityEngine;`**
**Why:** Gives us access to Unity's built-in classes like `MonoBehaviour`, `Color`, `Debug`, `Time`, etc.  
**Without it:** C# wouldn't recognize any Unity-specific code

---

#### **Statement 2: `public class ColorChanger : MonoBehaviour`**
**Why:** Creates a script that Unity can attach to GameObjects. The `: MonoBehaviour` is required for Unity to recognize it as a usable script.  
**Result:** We can now drag this script onto our cube

---

#### **Statement 3: `public float colorChangeSpeed = 1f;`**
**Why we need this:** We want to control HOW FAST the colors change  
**Why it's public:** So we can adjust the speed in Unity's Inspector without editing code  
**Why it's a float:** We need decimal precision (0.5, 1.5, 2.75, etc.)  
**Why default is 1:** One full rainbow cycle per second - a good starting speed (not too fast, not too slow)  
**What the unit means:** "Rainbow cycles per second" - 1 = full rainbow in 1 second, 2 = full rainbow in 0.5 seconds

---

#### **Statement 4: `private float hueValue = 0f;`**
**Why we need this:** We need to track WHERE we are in the rainbow (our current color position)  
**What is rainbow position:** A number from 0 to 1 representing position on the color wheel:
- 0.0 = Red
- 0.33 = Green  
- 0.66 = Blue
- 1.0 = Back to Red (full circle)

**Why start at 0:** Begin at red (start of rainbow)  
**Why it's private:** Internal tracking variable - no need to expose it in Inspector  
**How it works:** This number slowly increases from 0→1, creating the rainbow effect

---

#### **Statement 5: `private Renderer objectRenderer;`**
**Why we need this:** We need a way to access the cube's visual appearance so we can change its color  
**What is a Renderer:** The component that makes the cube visible and controls its material/color  
**Why store it:** So we can reuse it every frame without searching for it repeatedly (efficient)  
**Why it's private:** Internal reference - no need to show in Inspector

---

#### **Statement 6: `void Start() { ... }`**
**Why we need this:** One-time setup code that runs when the game starts  
**What it does:** Prepares everything before the color-changing begins  
**When it runs:** Once, immediately when you press Play

---

#### **Statement 7: `objectRenderer = GetComponent<Renderer>();`**
**Why:** Find and save the Renderer component attached to this GameObject  
**Purpose:** We need to grab the cube's Renderer so we can change its color later  
**Why in Start():** Only need to find it once, not every frame (saves performance)  
**What happens:** Searches the cube for its Renderer component and stores a reference to it

---

#### **Statement 8: `Debug.Log("ColorChanger ready!");`**
**Why:** Confirmation message that the script initialized successfully  
**Purpose:** Debugging/testing - lets you know the script started without errors  
**Where you see it:** Unity's Console window  
**Optional:** You can remove this line if you want - it's just for confirmation

---

#### **Statement 9: `void Update() { ... }`**
**Why we need this:** Code that runs continuously while the game is playing  
**What it does:** Contains the logic that changes colors every frame  
**When it runs:** Every frame (60+ times per second typically)  
**Purpose:** This is where the magic happens - the continuous color changing

---

#### **Statement 10: `hueValue += colorChangeSpeed * Time.deltaTime;`**
**Why:** Smoothly increase our position in the rainbow  
**What it does:** 
- Takes the current rainbow position value
- Adds a small increment to move forward in the rainbow
- The increment = speed × time since last frame

**Why `Time.deltaTime`:** Makes the speed consistent on all computers regardless of frame rate
- Without it: Fast PC = super fast colors, slow PC = slow colors
- With it: Same speed everywhere!

**Example math (60 FPS):**
- `Time.deltaTime` ≈ 0.0167 seconds (1/60th second)
- `colorChangeSpeed` = 1
- Each frame adds: 1 × 0.0167 = 0.0167 to hueValue
- After 60 frames: hueValue increased by ~1.0 (one full rainbow!)

---

#### **Statement 11: `if (hueValue > 1f) { hueValue = 0f; }`**
**Why:** Reset the rainbow position back to the start when we reach the end of the rainbow  
**Purpose:** Create a continuous loop - after purple, go back to red  
**What it does:** 
- Checks if hueValue exceeded 1.0 (end of color wheel)
- If yes, reset to 0.0 (beginning of color wheel)

**Result:** Red→Orange→Yellow→Green→Blue→Purple→back to Red→repeat forever

---

#### **Statement 12: `Color newColor = Color.HSVToRGB(hueValue, 1f, 1f);`**
**Why:** Convert our rainbow position value (0-1 number) into an actual color Unity can display  
**What is HSV:** A color system using Rainbow Position (which color), Saturation (how vivid), Value (how bright)
- First parameter `hueValue` = position in rainbow (0-1)
- Second parameter `1f` = full saturation (vivid colors, not washed out)
- Third parameter `1f` = full brightness (bright, not dark)

**Why HSV instead of RGB:** Way easier to cycle through rainbow colors - just change one number (rainbow position) instead of calculating red, green, and blue separately!

**What it returns:** A Color object that Unity understands

---

#### **Statement 13: `objectRenderer.material.color = newColor;`**
**Why:** Actually apply the color to the cube (make it visible!)  
**What it does:** 
- Takes the Renderer we found earlier
- Accesses its material (surface appearance)
- Sets the color property to our new color

**This is the payoff:** All the math above was calculation - this line makes the cube actually change color on screen!

---

### **The Big Picture - How It All Flows:**

1. **Setup Phase (Start):**
   - Find the cube's Renderer
   - We're ready to change colors!

2. **Every Frame (Update - 60+ times/second):**
   - Move a tiny bit forward in the rainbow (hueValue increases)
   - If we reached the end, loop back to start
   - Convert the rainbow position number to an actual color
   - Apply that color to the cube

3. **Result:**  
   Smooth, continuous rainbow that loops forever!

---

### **Key Design Decisions:**

- **Why `colorChangeSpeed`?** Gives us control without editing code
- **Why `hueValue`?** Tracks our position in the rainbow
- **Why HSV?** Easier rainbow math than RGB
- **Why `Time.deltaTime`?** Consistent speed on all computers
- **Why the if-statement?** Loop the rainbow infinitely

---

### Step 6: Save the Script
1. In your code editor, click **File** → **Save** (or press Ctrl+S)
2. Go back to Unity (click the Unity window)

**What you should see:** Unity will process the script (takes a few seconds)

---

## Part 4: BREAK TIME! (5 minutes) 🧘

Stand up! Look away from the screen!

**Do one of these:**
- Walk around the room
- Stretch your arms
- Get a drink of water
- Look out a window (far away focus is good for eyes!)

---

## Part 5: Make It Work & Experiment (15 minutes)

### Step 1: Attach Script to Cube
**The script exists, but it's not connected to anything yet!**

1. In **Hierarchy**, click your **Cube**
2. In **Inspector**, click **Add Component** button (bottom of Inspector)
3. Type **"ColorChanger"** in the search box
4. Click **ColorChanger** when it appears

**What you should see:**  
In the Inspector, a new section appears called "Color Changer (Script)" with a "Color Change Speed" field!

---

### Step 2: Press Play!
1. Click the **▶️ Play button** at the top center
2. Watch your cube!

**What should happen:**  
The cube slowly cycles through rainbow colors! 🌈

**If it's not working, check:**
- [ ] Script is attached to Cube (check Inspector)
- [ ] No errors in Console (check Console tab at bottom)
- [ ] Cube is visible in Game View (might need to adjust camera)

---

### Step 3: Change the Speed
**While the game is still running:**

1. Look at **Inspector** with Cube selected
2. Find **Color Change Speed** field
3. Change the number to **5**
4. Watch the colors change faster!

**Try different values:**
- 0.5 = Slow, relaxing
- 1 = Normal (default)
- 5 = Fast!
- 20 = Super fast (disco mode!)

---

### Step 4: Stop the Game
1. Click the **▶️ Play button** again to stop
2. The colors will go back to white

**⚠️ IMPORTANT:** Changes made during Play mode DON'T SAVE!  
Always stop the game before making permanent changes.

---

### Step 5: Set Your Favorite Speed Permanently
1. Make sure game is **stopped** (not playing)
2. In Inspector, set **Color Change Speed** to your favorite number
3. Press Play to test
4. Press Ctrl+S to save your scene

---

### Challenge Activities (If Time Remains):

#### Challenge 1: Add a Second Cube
1. Create another cube (**Hierarchy** → right-click → **3D Object** → **Cube**)
2. Move it to the side (change its Position X to **3**)
3. Attach the **ColorChanger** script to it too
4. Give it a **different speed** than the first cube
5. Press Play - two cubes changing at different speeds!

#### Challenge 2: Change the Size
1. Select a cube
2. In Inspector, find **Transform → Scale**
3. Change all three numbers (X, Y, Z) to **2**
4. The cube gets bigger!
5. Try different sizes: 0.5 = tiny, 5 = huge!

#### Challenge 3: Make It Spin
Want to add rotation? Ask me and I'll show you how to add one line!

---

## Part 6: Homework & Wrap-Up (5 minutes)

### What You Accomplished Today! 🎉
- ✅ Learned Unity interface
- ✅ Created your first scene
- ✅ Created a 3D object
- ✅ Wrote your first C# script
- ✅ Made something visual happen!

### Show & Tell
Take a **screenshot** of your rainbow cube:
1. Make sure Game View is showing your colorful cube
2. Press **Print Screen** key
3. Paste into Paint or similar (Ctrl+V)
4. Save as "Week1_RainbowCube.png"

### Homework (Optional - 15-20 minutes)
See **HOMEWORK.md** for details, but quick version:

**Create a sphere that changes colors differently:**
1. Create a Sphere instead of Cube
2. Attach the same ColorChanger script
3. Experiment with different speeds
4. Try making 3-4 spheres with different speeds
5. Screenshot your creation!

**Why this homework?**  
Practice the steps you learned today. Repetition helps it stick!

### Next Week Preview
Next week we'll make objects **grow and shrink** when you press keys! You'll control the size with the spacebar. 🎮

---

## Parent Summary

**Send this to parents:**

Today your student:
- Learned the Unity interface (game development software)
- Created their first 3D scene with a cube
- Wrote their first C# script to make colors change
- Saw immediate visual results (color-changing cube)

**Concepts learned:**
- What GameObjects are
- How scripts control objects
- Basic C# syntax (variables, functions)
- The game loop concept (Update function)

**Homework:** Create spheres that change colors (15-20 min, optional)

**Next week:** Making objects grow/shrink with keyboard input

---

## Teacher Notes: Session Reflection

After the session, quickly answer these:

1. **What went well?**
   - 
   
2. **What confused the student?**
   - 
   
3. **What to adjust next time?**
   - 
   
4. **Student engagement level (1-10):**
   - 
   
5. **Did we finish on time?**
   - 
   
6. **Topics to review next week:**
   - 

---

## Troubleshooting Guide

### "I don't see any colors changing!"
**Check these in order:**
1. Is the game playing? (Play button should be blue/highlighted)
2. Is the script attached to the Cube? (Check Inspector)
3. Is the Cube visible in Game View? (Switch from Scene to Game tab)
4. Any errors in Console? (Click Console tab and check)

### "I get an error when I press Play!"
**Most common errors:**

**Error:** "The type or namespace name 'ColorChanger' could not be found"  
**Fix:** Make sure you saved the script file (Ctrl+S in code editor)

**Error:** "NullReferenceException: Object reference not set"  
**Fix:** The Cube doesn't have a Renderer. Make sure you used a Cube, not an Empty GameObject.

### "The colors are changing but I can't see the cube!"
**Fix:**
1. Click the Cube in Hierarchy
2. Press F key to frame it in Scene View
3. In Hierarchy, click Main Camera
4. In Scene View, see where the camera is looking
5. Move the cube to Position (0, 0, 5) to be in front of camera

### "Unity is slow/laggy!"
**Quick fixes:**
1. Close other programs
2. In Game View, click "Stats" button to hide statistics
3. Make sure only one Scene View is open
4. Restart Unity if needed

---

## Extension: Understanding HSV Colors

**If student asks "What is HSV?"**

HSV stands for:
- **H**ue = Which color (0-1, like 0°-360° on a color wheel)
  - 0.0 = Red
  - 0.33 = Green
  - 0.66 = Blue
  - 1.0 = Back to Red
  
- **S**aturation = How colorful (0 = gray, 1 = vivid color)
  
- **V**alue = How bright (0 = black, 1 = full brightness)

Our code changes **H** from 0→1, keeping S and V at 1 (full color, full brightness).

**Try this:** Change `Color.HSVToRGB(hueValue, 1f, 1f)` to:
- `Color.HSVToRGB(hueValue, 0.5f, 1f)` → Pastel colors!
- `Color.HSVToRGB(hueValue, 1f, 0.5f)` → Darker colors!

---

## Resources for You (Teacher)

### Before Next Session:
- [ ] Complete this lesson yourself (do all steps)
- [ ] Test the script and make sure it works
- [ ] Prepare backup project in case student's breaks
- [ ] Print homework checklist

### Learn More:
- Unity Manual: GameObjects & Components
- Unity Learn: Beginner Scripting
- C# Basics: Variables and Functions

### Keep Handy:
- Unity keyboard shortcuts cheat sheet
- Common Unity errors and fixes
- Visual diagram of Unity interface (take screenshot)
