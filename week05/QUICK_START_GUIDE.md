# Quick Start Guide: Weeks 4-5 Implementation

## 🚀 Fast Track Setup (15 Minutes)

Follow this guide to get Week 4-5 working in Unity ASAP.

---

## Step 1: Create Input Actions (5 minutes)

1. **Create Asset:**
   - Right-click in Assets folder → Create → Input Actions
   - Name: `PlayerInputActions`

2. **Setup Actions:**
   - Double-click to open
   - Create Action Map: `Player`
   - Add these 3 actions:

### Action 1: Move
- Type: Value
- Control Type: Vector2
- Binding: Up/Down/Left/Right Composite
  - Up: W
  - Down: S
  - Left: A
  - Right: D

### Action 2: Sprint
- Type: Button
- Binding: Left Shift

### Action 3: Look
- Type: Value
- Control Type: Vector2
- Binding: Mouse → Delta

3. **Generate Code:**
   - Click "Save Asset"
   - Check "Generate C# Class" in Inspector
   - Click "Apply"

---

## Step 2: Create Layers (2 minutes)

1. **Edit → Project Settings → Tags and Layers**

2. **Add Layers:**
   - User Layer 6: `Player`
   - User Layer 7: `CameraCollision`

---

## Step 3: Scene Setup (8 minutes)

### Create Player

```
GameObject → 3D Object → Capsule
Name: Player
Position: (0, 1, 0)
Layer: Player

Components to add:
1. Character Controller (default settings OK)
2. PlayerControllerWeek5 script
   - Assign PlayerInputActions asset
   - Walk Speed: 3
   - Sprint Speed: 6
```

### Setup Camera

```
Find: Main Camera
Position: (0, 5, -5) (starting position, will be adjusted)

Remove: Any existing camera scripts
Add: ThirdPersonCamera script
   - Target: Drag Player here
   - Distance: 5
   - Height: 2
   - Mouse Sensitivity: 2
   - Min Pitch: -80
   - Max Pitch: 80
   - Collision Mask: Check "CameraCollision" layer ONLY
   - Min Distance: 1
   - Collision Offset: 0.3
   - Collision Radius: 0.2
```

### Link Player to Camera

```
Select: Player
In PlayerControllerWeek5:
   - Camera Transform: Drag Main Camera here
```

### Create Environment

**Ground:**
```
GameObject → 3D Object → Plane
Name: Ground
Position: (0, 0, 0)
Scale: (10, 1, 10)
Layer: CameraCollision
```

**Test Walls:**
```
GameObject → 3D Object → Cube
Name: Wall
Position: (5, 1.5, 0)
Scale: (1, 3, 10)
Layer: CameraCollision

(Duplicate a few times to create test area)
```

---

## Step 4: Test! (2 minutes)

Press Play and verify:

✅ **Movement:**
- [ ] WASD moves player
- [ ] Shift makes player sprint
- [ ] Player rotates to face movement direction

✅ **Camera:**
- [ ] Mouse moves camera around player
- [ ] Can't flip camera upside down
- [ ] Camera doesn't go through walls
- [ ] Smooth orbiting

---

## 🔧 Troubleshooting

### "Player doesn't move"
1. Check Input Actions asset is assigned to Player
2. Check "Generate C# Class" was clicked
3. Try closing/reopening Unity
4. Check Console for errors

### "Camera goes through walls"
1. Walls MUST have `CameraCollision` layer
2. Camera's Collision Mask MUST include `CameraCollision`
3. Check Collision Offset is at least 0.2

### "Moving in wrong direction"
1. Verify Camera Transform is assigned in Player
2. Use PlayerControllerWeek5, not PlayerController

### "Mouse doesn't work"
1. Click on Game view to focus
2. Check Look action is in Input Actions
3. Look action MUST be bound to Mouse Delta
4. Press Alt+Escape if cursor is locked

---

## 📋 Final Checklist

Before moving to Week 6, verify:

- [ ] Player moves with WASD
- [ ] Shift sprints
- [ ] Mouse controls camera
- [ ] Camera doesn't clip walls
- [ ] Movement is relative to camera direction
- [ ] Player rotates smoothly
- [ ] Feels responsive and smooth

---

## 🎯 You're Done!

**Congratulations!** You now have:
- Professional player movement
- AAA-quality camera system
- Foundation for the shooter game

**Next:** Week 6 - Add shooting mechanics! 🔫

---

## 💾 Save Your Work!

**Important:**
1. Save Scene: Ctrl+S
2. Commit to version control (if using)
3. Test one more time
4. Take note of your favorite settings (sensitivity, speeds, etc.)

**These scripts will be the foundation for the entire project!**
