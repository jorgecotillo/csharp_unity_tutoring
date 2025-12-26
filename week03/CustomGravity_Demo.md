# Custom Gravity Demo - Step-by-Step Setup Guide

This guide walks you through creating a custom gravity demo in Unity where objects orbit around a planet!

---

## What You'll Build

By the end of this demo, you'll have:
- A planet in the center
- A small object orbiting around it
- Custom gravity simulation (not using Unity's built-in gravity)
- Ability to experiment with orbital mechanics!

**Time needed:** 10-15 minutes

---

## Prerequisites

Make sure you have:
- Unity project open (any scene)
- `CustomGravityObject.cs` script (from Week 3 materials)

---

## Step 1: Create the Planet

1. **Create a Sphere:**
   - Right-click in Hierarchy → `3D Object` → `Sphere`
   - Rename it to `"Planet"`

2. **Scale it up:**
   - Select the Planet
   - In Inspector, set Transform → Scale to `(5, 5, 5)`

3. **Position it at center:**
   - Transform → Position: `(0, 0, 0)`

4. **Optional - Make it look cool:**
   - Create a new Material (Project window → Right-click → Create → Material)
   - Name it "PlanetMaterial"
   - Change the color (e.g., blue or orange)
   - Drag the material onto the Planet sphere

**Result:** You should see a large sphere in the center of your scene!

---

## Step 2: Create the Orbiting Object

1. **Create a smaller sphere:**
   - Right-click in Hierarchy → `3D Object` → `Sphere`
   - Rename it to `"Orbiter"`

2. **Make it smaller:**
   - Transform → Scale: `(0.5, 0.5, 0.5)`

3. **Position it away from planet:**
   - Transform → Position: `(8, 0, 0)` 
   - This puts it 8 units to the right of the planet

4. **Add Rigidbody:**
   - Select Orbiter
   - In Inspector → `Add Component`
   - Search for `Rigidbody` and add it
   - ✅ **IMPORTANT:** Uncheck `Use Gravity` (we're making our own!)

**Result:** You should see a small sphere to the right of the planet!

---

## Step 3: Add Custom Gravity Script

1. **Attach the script:**
   - Select the Orbiter
   - In Inspector → `Add Component`
   - Search for `Custom Gravity Object` and add it

2. **Configure the script:**
   - **Gravity Source:** Drag the `Planet` object from Hierarchy into this field
   - **Gravitational Constant:** Set to `10`
   - **Use Inverse Square:** Check this box ✅
   - **Movement Force:** Set to `5` (we'll use this later)

**Result:** The Orbiter now has custom gravity pointing toward the planet!

---

## Step 4: Give Initial Velocity for Orbit

For an object to orbit, it needs sideways velocity. There are two ways to do this:

### Option A: Add Velocity in Code (Recommended)

1. **Open `CustomGravityObject.cs`**

2. **Find the `Start()` method** and add this line:
   ```csharp
   void Start()
   {
       rb = GetComponent<Rigidbody>();
       rb.useGravity = false;
       
       // Give initial orbital velocity
       rb.velocity = transform.right * 5f;  // ← ADD THIS LINE
   }
   ```

3. **Save the script**

### Option B: Set Velocity During Play (For Testing)

1. **Press Play**
2. **While the game is running:**
   - Select the Orbiter in Hierarchy
   - In Inspector → Rigidbody → Velocity
   - Set to: `(0, 0, 5)` or `(5, 0, 0)`
3. **Watch it start orbiting!**

**Note:** Option B doesn't save - the velocity resets when you stop playing. Use Option A for permanent setup.

---

## Step 5: Test the Orbit!

1. **Press Play ▶️**

2. **Watch the Orbiter:**
   - It should start moving sideways
   - Gravity should pull it toward the planet
   - If the speed is right, it orbits! 🛰️

3. **Adjust the Scene View:**
   - Switch to Scene view (not Game view)
   - In Scene view, rotate the camera to see the orbit from above or the side
   - You can see the path of the orbiter!

---

## Step 6: Experiment!

Now try adjusting values to see what happens:

### Experiment 1: Change Initial Velocity

In `Start()`, try different speeds:
```csharp
rb.velocity = transform.right * 3f;   // Slower - might crash into planet!
rb.velocity = transform.right * 5f;   // Medium - circular orbit?
rb.velocity = transform.right * 10f;  // Faster - might escape into space!
```

### Experiment 2: Change Gravitational Constant

- Select Orbiter → Inspector → Custom Gravity Object
- Try different values: `5`, `10`, `20`, `50`
- **Lower value** = weaker gravity = needs slower velocity to orbit
- **Higher value** = stronger gravity = needs faster velocity to orbit

### Experiment 3: Change Distance

- Select Orbiter
- Change Position to `(12, 0, 0)` (farther away)
- Notice: Gravity is weaker when farther! (inverse square law)
- You'll need different velocity to maintain orbit

### Experiment 4: Disable Inverse Square Law

- Select Orbiter → Custom Gravity Object
- **Uncheck** "Use Inverse Square"
- Notice: Gravity strength doesn't change with distance (unrealistic!)

---

## Troubleshooting

### ❌ "Orbiter crashes into planet!"

**Causes:**
- Initial velocity too slow
- Gravitational constant too high
- Starting position too close

**Solutions:**
1. Increase initial velocity: `rb.velocity = transform.right * 8f;`
2. Move starting position farther: `(10, 0, 0)` or `(12, 0, 0)`
3. Reduce gravitational constant: Try `5` or `8`

---

### ❌ "Orbiter flies away into space!"

**Causes:**
- Initial velocity too fast
- Gravitational constant too low
- Inverse square law disabled

**Solutions:**
1. Decrease initial velocity: `rb.velocity = transform.right * 3f;`
2. Increase gravitational constant: Try `15` or `20`
3. Make sure "Use Inverse Square" is checked ✅

---

### ❌ "Orbiter falls straight down!"

**Cause:** No initial sideways velocity!

**Solution:** Make sure you added `rb.velocity = transform.right * 5f;` in `Start()`

---

### ❌ "Nothing happens when I press Play!"

**Checklist:**
- Is "Gravity Source" field assigned? (drag Planet into it)
- Does Orbiter have Rigidbody component?
- Is Rigidbody "Use Gravity" unchecked?
- Is CustomGravityObject script attached to Orbiter?

---

## Understanding What's Happening

### Why Sideways Velocity?

Without sideways velocity:
```
Planet          Orbiter
   ●  ← ← ← ← ●
   
Gravity pulls it straight in → Crash! 💥
```

With sideways velocity:
```
        ↑
        ●
    ↗     ↖
  ●         ●  ← Gravity always pulls toward center
    ↘     ↙      Velocity keeps it moving sideways
        ●          Result: Circular path! ✅
        ↓
```

The orbiter is constantly "falling" toward the planet, but moving sideways fast enough to keep missing it!

### Inverse Square Law

This makes gravity behave like real physics:
```
Distance = 2m  →  Gravity = 10 / (2×2) = 2.5
Distance = 4m  →  Gravity = 10 / (4×4) = 0.625 (weaker!)
Distance = 8m  →  Gravity = 10 / (8×8) = 0.156 (much weaker!)
```

**Key insight:** Doubling the distance makes gravity **4× weaker** (not just 2×)!

This is exactly how real gravity works in space! 🌍🌙

---

## Advanced Challenges

### Challenge 1: Perfect Circular Orbit

Can you find the perfect combination of:
- Starting distance
- Gravitational constant
- Initial velocity

...to create a **perfectly circular orbit** that lasts for 60 seconds without crashing or escaping?

**Hint:** It's a delicate balance! Try:
- Distance: `8` units
- Gravity: `10`
- Velocity: `5.5 m/s`
- Then fine-tune from there!

---

### Challenge 2: Multi-Object System

1. Create 3 Orbiters at different distances:
   - Close: `(5, 0, 0)` 
   - Medium: `(8, 0, 0)`
   - Far: `(12, 0, 0)`

2. Give each a different velocity to maintain stable orbits

3. Make them different colors so you can track them

**Goal:** Create a mini "solar system" with 3 stable orbits!

---

### Challenge 3: Moon Orbiting a Planet

1. Create a second smaller sphere called "Moon"
2. Position it near one of the Orbiters
3. Set its Gravity Source to the Orbiter (not the planet!)
4. Give it sideways velocity

**Can you make the Moon orbit the Orbiter, while the Orbiter orbits the Planet?** 🌍🌙

---

### Challenge 4: Figure-8 Orbit

1. Create two "Planet" objects side by side:
   - Planet A at `(-5, 0, 0)`
   - Planet B at `(5, 0, 0)`

2. Create an Orbiter at `(0, 8, 0)` (between them, above)

3. Give it downward velocity: `rb.velocity = Vector3.down * 3f;`

4. In the script, make it pull toward **both planets**:
   ```csharp
   // You'll need to modify CustomGravityObject.cs to handle multiple sources!
   ```

**Goal:** Create a figure-8 pattern between the two planets!

---

## Next Steps

Now that you understand custom gravity, try:
- Experiment with different starting positions and velocities
- Create elliptical orbits (oval-shaped) by adjusting velocity direction
- Add player controls to the Orbiter (combine with movement code!)
- Create a "space navigation" game where you slingshot around planets

---

## Complete Scene Checklist

When finished, your scene should have:
- ✅ Planet (Sphere, scale 5,5,5, at origin)
- ✅ Orbiter (Sphere, scale 0.5,0.5,0.5, at (8,0,0))
- ✅ Orbiter has Rigidbody (Use Gravity = OFF)
- ✅ Orbiter has CustomGravityObject script
- ✅ Gravity Source field assigned to Planet
- ✅ Initial velocity set in Start() method
- ✅ When you press Play, object orbits!

**Congratulations!** You've created a custom gravity simulation! 🎉🛰️

---

## Teacher Notes

**Key teaching moments:**
1. "Why doesn't it fall straight down?" → Sideways velocity
2. "Why does it curve?" → Gravity constantly pulls toward center
3. "Why orbital velocity matters?" → Too slow = crash, too fast = escape
4. "What is inverse square law?" → Gravity gets weaker with distance squared

**Common issues to watch for:**
- Forgetting to disable "Use Gravity" on Rigidbody
- Not assigning Gravity Source in Inspector
- Missing initial velocity
- Starting too close to planet (strong forces at close range)

**Extension questions:**
- "What happens if we remove drag from the Rigidbody?" → Perfect orbit (no energy loss)
- "How do satellites maintain orbit?" → Initial launch velocity + gravity balance
- "Could we orbit in any direction?" → Yes! Try rb.velocity = transform.up * 5f
