# NPC Shooter Game 🎮

## Quick Start Guide

### Opening the Project in Unity

1. **Install Unity Hub** (if you haven't already)
   - Download from: https://unity.com/download

2. **Install Unity Editor**
   - Version: 6000.0.23f1 (Unity 6.3 LTS) or newer
   - Make sure to include: 
     - Windows Build Support
     - Input System package

3. **Open the Project**
   - Launch Unity Hub
   - Click "Add" → "Add project from disk"
   - Navigate to: `c:\source_code\tutoring\unity\csharp_unity_tutoring\npc_shooter`
   - Select the folder and click "Open"

4. **Wait for Import**
   - Unity will import all assets (this may take a few minutes first time)

5. **Open the Game Scene**
   - In Project window, navigate to: `Assets/Scenes/GameScene.unity`
   - Double-click to open it

6. **Press Play!** ▶️
   - Click the Play button at the top center of Unity
   - Use **WASD** to move
   - Use **Mouse** to look around
   - Hold **Left Shift** to sprint
   - Press **ESC** to unlock cursor

---

## What's Included (Week 4-5: Player Movement & Camera)

### ✅ Completed Features

1. **Player Character**
   - WASD movement (relative to camera direction)
   - Sprint with Left Shift
   - Smooth acceleration/deceleration
   - Character Controller physics
   - Capsule mesh (placeholder for future model)

2. **Third-Person Camera**
   - Mouse look (360° horizontal, limited vertical)
   - Smooth camera following
   - Collision avoidance (camera won't go through walls)
   - Locked cursor during gameplay

3. **Input System**
   - New Unity Input System configured
   - Easily extendable for future features (shooting, etc.)

4. **Test Environment**
   - Large ground plane (50x50)
   - Directional light for shadows
   - Ready for obstacles and enemies

---

## Controls

| Input | Action |
|-------|--------|
| **W** | Move Forward |
| **A** | Move Left |
| **S** | Move Backward |
| **D** | Move Right |
| **Mouse** | Look Around |
| **Left Shift** | Sprint |
| **ESC** | Toggle Cursor Lock |

---

## Project Structure

```
npc_shooter/
├── Assets/
│   ├── Scenes/
│   │   └── GameScene.unity          # Main game scene
│   ├── Scripts/
│   │   ├── Player/
│   │   │   └── PlayerController.cs   # Player movement logic
│   │   └── Camera/
│   │       └── ThirdPersonCamera.cs  # Camera controller
│   ├── Prefabs/                      # (Empty - for later)
│   ├── Materials/                    # (Empty - for later)
│   └── PlayerInputActions.inputactions  # Input configuration
├── ProjectSettings/                  # Unity project settings
└── Packages/                         # Package dependencies
```

---

## Next Steps (Coming in Future Weeks)

### Week 6-7: Shooting Mechanics 🔫
- Projectile spawning
- Bullet physics
- Basic shooting
- Crosshair UI

### Week 8: Weapon System 🛠️
- Multiple weapons (Pistol, Rifle, Shotgun)
- Weapon switching
- Different fire rates and damage

### Week 9-10: Enemy AI 🤖
- Patrolling enemies
- Chase behavior
- Attack patterns

...and much more! Check `FINAL_PROJECT.md` for the complete roadmap.

---

## Troubleshooting

### Issue: Scripts won't compile
**Solution**: Make sure Unity Input System package is installed:
1. Window → Package Manager
2. Search for "Input System"
3. Click Install

### Issue: Camera doesn't follow player
**Solution**: Check the Camera's ThirdPersonCamera component:
1. Select "Main Camera" in Hierarchy
2. In Inspector, find "Third Person Camera" component
3. Make sure "Target" field is assigned to the Player object

### Issue: Player falls through ground
**Solution**: Make sure Ground has a collider:
1. Select "Ground" in Hierarchy
2. Verify it has a Box Collider component

### Issue: Can't move player
**Solution**: Check Player Input component:
1. Select "Player" in Hierarchy
2. Find "Player Input" component
3. Make sure "Actions" field references "PlayerInputActions"
4. Default Action Map should be set to "Gameplay"

---

## Customization Tips

### Adjust Movement Speed
1. Select "Player" in Hierarchy
2. Find "Player Controller" component
3. Modify:
   - **Walk Speed**: Default 5 (try 3-8)
   - **Sprint Speed**: Default 8 (try 6-12)

### Adjust Camera Settings
1. Select "Main Camera" in Hierarchy
2. Find "Third Person Camera" component
3. Modify:
   - **Mouse Sensitivity**: Default 2 (try 1-5)
   - **Offset**: Change Z value for closer/farther camera

### Change Player Appearance (Temporary)
1. Select "Player" in Hierarchy
2. Change Scale to make bigger/smaller
3. In Mesh Renderer, change Material color

---

## Learning Points (What You're Using)

### From Week 1-3 Knowledge:
- ✅ **MonoBehaviour** lifecycle (Awake, Update, LateUpdate)
- ✅ **Time.deltaTime** for frame-rate independence
- ✅ **Vector3** math for positions and directions
- ✅ **Transform** component for position/rotation
- ✅ **GetComponent<>()** to access other components

### New Concepts (Week 4-5):
- ✅ **CharacterController** for player physics
- ✅ **Input System** for modern input handling
- ✅ **Quaternions** for smooth rotation (Quaternion.Slerp)
- ✅ **Camera** following and collision detection
- ✅ **Physics.SphereCast** for camera collision avoidance
- ✅ **Vector3.Lerp** for smooth movement

---

## Have Fun! 🎉

You now have a fully functional third-person character controller! Run around, test the controls, and get ready for Week 6 when we add shooting mechanics!

**Questions or Issues?**
Check the code comments in the scripts - they explain how everything works!
