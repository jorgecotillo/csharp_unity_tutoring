# Week 4: Input Actions Setup Guide

## Creating PlayerInputActions Asset

Since Input Actions assets are binary files that Unity generates, you'll need to create them in Unity Editor. Follow these steps:

### Step-by-Step Instructions

1. **Open your Unity project**

2. **Create the Input Actions Asset**
   - In Project window, navigate to `Assets/` folder
   - Right-click → Create → Input Actions
   - Name it: `PlayerInputActions`

3. **Open the Input Actions Editor**
   - Double-click the `PlayerInputActions` asset
   - A new window will open showing the Input Actions editor

4. **Create Action Map**
   - Click the `+` button next to "Action Maps" (left panel)
   - Rename it to: `Player`

5. **Create "Move" Action**
   - With "Player" action map selected, click `+` next to "Actions" (middle panel)
   - Name it: `Move`
   - In the right panel, set:
     - Action Type: `Value`
     - Control Type: `Vector2`
   
   - Click `+` next to the Move action to add a binding
   - From the dropdown, select: `Add Up/Down/Left/Right Composite`
   - Click `⌄` to expand the composite
   - Configure each direction (you're setting which key controls each direction):
     - **Up**: 
       1. Click on `Up: <No Binding>` (it will highlight blue)
       2. On the right side, find "Path" dropdown
       3. Click the Path dropdown → Type "W" in search → Select "W [Keyboard]"
     - **Down**: Same steps, but select "S [Keyboard]"
     - **Left**: Same steps, but select "A [Keyboard]"
     - **Right**: Same steps, but select "D [Keyboard]"
   
   **Tip:** You can also click "Listen" button next to Path and just press the key you want!

6. **Create "Sprint" Action**
   - Click `+` next to "Actions" again
   - Name it: `Sprint`
   - In the right panel, set:
     - Action Type: `Button`
   - Click `+` next to Sprint action to add a binding
   - Click `<No Binding>` → Listen → Press `Left Shift` key

7. **Generate C# Class**
   - Click "Save Asset" button at the top
   - In the Inspector (with PlayerInputActions selected), check:
     - ☑ Generate C# Class
   - Click "Apply" button
   - Unity will generate a `PlayerInputActions.cs` file

8. **Verify**
   - You should now see `PlayerInputActions.cs` in your Project window
   - This file contains the C# API for accessing your input actions

## Alternative: Quick Setup (If the Above Doesn't Work)

If you're having trouble, you can use the old Input System for Week 4:

Replace the input reading in `PlayerController.cs`:

```csharp
// Instead of:
Vector2 input = inputActions.Player.Move.ReadValue<Vector2>();
bool isSprinting = inputActions.Player.Sprint.IsPressed();

// Use:
float horizontal = Input.GetAxisRaw("Horizontal"); // A/D keys
float vertical = Input.GetAxisRaw("Vertical");     // W/S keys
Vector2 input = new Vector2(horizontal, vertical);
bool isSprinting = Input.GetKey(KeyCode.LeftShift);
```

## Testing Your Input

Once set up, test by:
1. Adding `PlayerController` to your player GameObject
2. Assigning the PlayerInputActions asset in the Inspector (if using new Input System)
3. Pressing Play and using WASD + Shift keys
