# Week 5: Setup Instructions

## 📋 Complete Setup Checklist

### Step 1: Update Input Actions

You need to add a "Look" action to your PlayerInputActions asset from Week 4.

1. **Open PlayerInputActions Asset**
   - In Project window, find `PlayerInputActions`
   - Double-click to open Input Actions editor

2. **Add Look Action**
   - With "Player" action map selected
   - Click `+` next to "Actions"
   - Name it: `Look`
   - Set properties:
     - Action Type: `Value`
     - Control Type: `Vector2`
   
3. **Add Mouse Binding**
   - Click `+` next to "Look" action
   - Select `Add Binding`
   - Click `<No Binding>` → Path → Mouse → Delta
   - This binds to mouse movement (delta = movement, not position)

4. **Save and Regenerate**
   - Click "Save Asset"
   - Click "Apply" in Inspector to regenerate C# class

### Step 2: Create Camera Collision Layer

To prevent camera from colliding with player:

1. **Create Layer**
   - Edit → Project Settings → Tags and Layers
   - Find first empty layer slot
   - Name it: `CameraCollision`

2. **Assign to Environment**
   - Select all walls, floors, obstacles
   - In Inspector, set Layer: `CameraCollision`

3. **Configure Player Layer**
   - Select Player GameObject
   - Set Layer: `Player` (create if doesn't exist)

### Step 3: Scene Setup

1. **Setup Player**
   - GameObject → 3D Object → Capsule (name: "Player")
   - Add Component: Character Controller
   - Add Component: PlayerControllerWeek5 (Week 5 script)
   - Assign PlayerInputActions asset in Inspector

2. **Setup Camera**
   - Find Main Camera
   - Remove the old CameraFollow script
   - Add Component: ThirdPersonCamera (Week 5 script)
   - Assign settings:
     - Target: Drag Player GameObject here
     - Collision Mask: Check "CameraCollision" layer ONLY
     - Mouse Sensitivity: 2 (adjust to preference)

3. **Assign Camera Reference to Player**
   - Select Player GameObject
   - In PlayerControllerWeek5 component:
     - Camera Transform: Drag Main Camera here

4. **Create Ground**
   - GameObject → 3D Object → Plane (name: "Ground")
   - Scale: (10, 1, 10)
   - Layer: `CameraCollision`

5. **Create Some Walls** (for testing camera collision)
   - GameObject → 3D Object → Cube (name: "Wall")
   - Scale: (1, 3, 10) for a wall
   - Position: Near player
   - Layer: `CameraCollision`
   - Duplicate a few times to create test environment

### Step 4: Test!

**Press Play and test:**

✅ **Movement:**
- WASD moves player relative to camera direction
- Shift sprints
- Player rotates to face movement direction

✅ **Camera:**
- Mouse moves camera around player
- Can't flip camera upside down
- Camera doesn't go through walls
- Smooth orbiting

**Common Issues:**

❌ **Player not rotating?**
- Check Camera Transform is assigned in PlayerControllerWeek5

❌ **Camera going through walls?**
- Check walls have `CameraCollision` layer
- Check camera's Collision Mask includes `CameraCollision` layer

❌ **Moving in wrong direction?**
- Verify camera reference is Main Camera

❌ **Mouse not working?**
- Check Look action is added to Input Actions
- Check Input Actions asset is assigned
- Try pressing Alt+Escape to unlock cursor, then click game view

## 🎮 Recommended Settings

Based on testing, these values feel good:

**PlayerControllerWeek5:**
- Walk Speed: 3
- Sprint Speed: 6
- Rotation Speed: 10

**ThirdPersonCamera:**
- Distance: 5
- Height: 2
- Mouse Sensitivity: 2-3
- Min Pitch: -80
- Max Pitch: 80
- Position Smooth Speed: 10
- Rotation Smooth Speed: 15
- Min Distance: 1
- Collision Offset: 0.3
- Collision Radius: 0.2

## 🎯 What You've Accomplished!

After Week 5 setup, you have:
- ✅ Camera-relative player movement
- ✅ Mouse-controlled camera orbit
- ✅ Camera collision detection
- ✅ Smooth, professional third-person system
- ✅ Ready for Week 6 shooting mechanics!

**This is the foundation used in games like:**
- Fortnite
- Gears of War
- The Division
- Uncharted

**Next Week:** We add shooting! 🔫
