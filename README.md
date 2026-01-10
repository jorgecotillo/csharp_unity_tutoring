# Unity & C# Game Development Tutoring Curriculum 🎮

A comprehensive, beginner-friendly Unity game development curriculum designed for an 8th-grade student. This repository contains weekly lessons that progressively build toward creating a complete 3D shooter game over 5-6 months.

---

## 👨‍🎓 About This Curriculum

**Target Student:** 8th grade  
**Session Length:** 1 hour per week  
**Session Format:**
- First 10-15 minutes: Learn new concepts
- Remaining 45-50 minutes: Hands-on implementation

**Teaching Philosophy:**
- ✅ Clear structure with visual hierarchy
- ✅ Concepts explained from scratch (no assumptions)
- ✅ Immediate hands-on practice
- ✅ Progressive difficulty with clear milestones
- ✅ Fun and engaging at every step

---

## 🎯 Final Project Goal

By the end of this curriculum, the student will have built a **complete 3D shooter game** featuring:
- Player character with smooth movement and camera controls
- Multiple weapon types with different behaviors
- Intelligent AI enemies that dodge bullets and use cover
- Health/damage systems
- Wave-based gameplay with boss fights
- Polished visual and audio effects

**See [FINAL_PROJECT_REVISED.md](FINAL_PROJECT_REVISED.md) for the complete roadmap.**

---

## 📚 Curriculum Structure

### ✅ Completed Weeks

#### **Week 1: Unity Fundamentals**
- Unity interface and Editor basics
- GameObjects and Components
- First C# script (rainbow color changer)
- Understanding Update() and Time.deltaTime

**📁 Folder:** `week01_simple/`

---

#### **Week 2: Input & Transforms**
- Unity's New Input System
- Keyboard input handling
- Scaling objects with WASD
- Vector3 manipulation
- Conditional statements

**📁 Folder:** `week02/`

---

#### **Week 3: Physics & Forces**
- Rigidbody component
- Velocity vs Forces (two approaches to movement)
- Custom gravity implementation
- FixedUpdate for physics
- Orbital mechanics

**📁 Folder:** `week03/`  
**Summary:** `week03/WEEK3_SUMMARY.md`

---

### 🚀 Current Focus: Player Controller & Camera System

#### **Week 4: Character Controller & Basic Movement**
**Goal:** Get player moving with keyboard controls

**Status:** ⚠️ Partially Complete - Input Actions setup only

**Completed Topics:**
- ✅ Input System Review
- ✅ PlayerInput component setup
- ✅ Input Actions and Action Maps configuration

**Remaining Topics (moved to Week 5):**
- CharacterController vs Rigidbody comparison
- Vector2 → Vector3 conversion for movement
- Transform.Translate vs CharacterController.Move
- Implementing WASD movement
- Sprint functionality (Shift key)
- Velocity and speed calculations

**📁 Folder:** `week04/`  
**Start Here:** `week04/README.md`

**Key Concepts Covered:**
- Input System architecture
- Action Maps for organized input
- Keyboard input handling setup

---

#### **Week 5: Complete Movement & Gravity**
**Goal:** Finish player movement system and add gravity/ground detection

**Topics to Cover:**

**Part 1: Completing Basic Movement (from Week 4)**
- CharacterController component setup
- Vector2 → Vector3 conversion (input to world direction)
- CharacterController.Move() for movement
- Calculating movement direction from input
- Speed variables (walk speed, sprint speed)
- Time.deltaTime for smooth, frame-independent movement

**Part 2: Gravity & Ground Detection**
- Gravity concepts (constant acceleration)
- CharacterController doesn't auto-handle gravity
- Raycasting for ground detection (review from Week 3)
- LayerMasks for targeted raycasting
- Vertical velocity accumulation
- Resetting velocity when grounded
- Smooth falling behavior

**📁 Folder:** `week05/`  
**Start Here:** `week05/README.md`

**Key Concepts:**
- Vector math (2D to 3D conversion)
- Movement with CharacterController
- Physics simulation (gravity)
- Ground detection with raycasts
- Velocity management

**Deliverables:**
- Player moves with WASD
- Sprint works with Shift key
- Player falls realistically with gravity
- Proper ground detection (no floating)
- Movement feels responsive and grounded

---

### 📅 Upcoming Weeks (Planned)

#### **Week 6: Mouse Look & Camera Basics**
- Quaternions and rotation basics
- Euler angles vs Quaternions
- Mouse input (delta movement)
- Sensitivity settings
- Camera rotation (horizontal player, vertical camera)
- Camera vertical clamping (prevent flipping)

#### **Week 7: Third-Person Camera**
- Camera positioning and offset
- Smooth camera following with Lerp
- Vector3.Lerp for interpolation
- Camera as separate GameObject
- Camera rig/arm concept

#### **Week 8: Camera Collision Prevention**
- Camera clipping through walls problem
- Raycasting for camera collision
- Adjusting camera distance dynamically
- LayerMask advanced usage
- Smooth transition when camera moves

#### **Week 9: Raycasting for Shooting (Hitscan)**
- Hitscan vs Projectile weapons
- Shooting raycast from camera
- RaycastHit information
- Mouse button input for firing
- Visual feedback with Debug.DrawRay

#### **Week 10: Simple Crosshair UI**
- Unity UI basics (Canvas, EventSystem)
- Screen Space UI
- UI Image component
- Anchors and pivots
- Centering crosshair on screen

#### **Week 11: IDamageable Interface**
- Interfaces in C#
- Polymorphism basics
- Health system design
- TakeDamage() method
- Interface implementation

#### **Week 12: Health System**
- Current HP vs Max HP
- Properties in C# (getters/setters)
- Events introduction (UnityEvent)
- Death when health reaches 0
- Destroying objects on death

#### **Week 13: Simple Enemy Target**
- Enemy GameObject setup
- Visual feedback on hit
- Enemy prefabs
- Material color changes
- Tagging as "Enemy"

#### **Week 14-20: Character Animation**
- Humanoid character setup (Mixamo)
- Animation clips import
- Animator Controller basics
- Animation parameters and transitions
- Blend trees for smooth movement
- Shooting animation integration
- Enemy animation setup

#### **Week 21-24: Enemy AI**
- NavMesh basics
- NavMeshAgent component
- Enemy state machine (Idle, Patrol, Chase, Attack)
- Line of sight detection
- Waypoint system
- Attack range and cooldown

#### **Week 25-26: UI Systems**
- Player health UI
- Enemy health bars (world space)
- Billboard effect for UI
- Canvas rendering modes
- UI Image fill amount

#### **Week 27-30: Advanced Weapons**
- ScriptableObjects for weapon data
- Weapon switching system
- Ammo system and reloading
- Projectile weapon type
- Physical bullets with Rigidbody

#### **Week 31-32: Polish & Testing**
- Bug fixing and balancing
- Game feel and juice
- Camera shake
- Particle effects
- Playtesting and iteration

**Full roadmap:** See [FINAL_PROJECT_REVISED.md](FINAL_PROJECT_REVISED.md)

---

## 🎓 Learning Path & Prerequisites

### Prerequisites by Week

**Week 1:** None - complete beginner friendly

**Week 2:** 
- Week 1 concepts (Unity basics, C# fundamentals)
- Input System package installed

**Week 3:**
- Weeks 1-2 (Unity basics, Input System)
- Understanding of Vector3 and basic math

**Week 4:**
- Weeks 1-3 (Physics basics, raycasting, Vector math)
- Input System installed

**Week 5:**
- Week 4 (Player movement must be completed first)

---

## 🎯 Key Milestones Achieved So Far

### 🏆 Week 1: "Hello Unity!"
- ✅ Created first Unity project
- ✅ Wrote first C# script
- ✅ Made object change colors automatically

### 🏆 Week 2: "I Can Control It!"
- ✅ Used Input System
- ✅ Made object grow/shrink with keyboard
- ✅ Understood Vector3 and clamping

### 🏆 Week 3: "Physics Are Real!"
- ✅ Implemented Rigidbody movement
- ✅ Created custom gravity system
- ✅ Made objects orbit like planets

### 🏆 Week 4: "Input System Mastery!"
- ✅ Input System setup complete
- ✅ PlayerInput component configured
- ✅ Action Maps created
- ⏳ CharacterController movement (in progress)

### 🎯 Week 5: "Complete Movement System!" (Next Session)
- ⏳ Finish WASD movement implementation
- ⏳ Add sprint functionality
- ⏳ Implement gravity and ground detection
- ⏳ Create responsive player controls

---

## 📖 How to Use This Repository

### For the Student:
1. Start with `week01_simple/README.md`
2. Complete each week in order
3. Each README has step-by-step instructions
4. Build the demos, then apply to final project
5. Reference summary files for review

### For the Tutor:
1. Review the week's README before session
2. First 10-15 min: Explain concepts using README
3. Remaining time: Guide student through implementation
4. Use "gotchas" sections to anticipate issues
5. Reference FINAL_PROJECT.md to show how it all connects

### Folder Structure:
```
unity_tutoring/
├── README.md (this file)
├── FINAL_PROJECT.md (complete project roadmap)
│
├── week01_simple/
│   ├── README.md (lesson plan)
│   └── (scripts)
│
├── week02/
│   ├── README.md
│   └── Assets/Scripts/
│
├── week03/
│   ├── README.md
│   ├── WEEK3_SUMMARY.md
│   └── Assets/Scripts/
│
├── week04/
│   ├── README.md (start here for Week 4)
│   ├── INPUT_ACTIONS_SETUP.md
│   └── Assets/Scripts/
│
├── week05/
│   ├── README.md (start here for Week 5)
│   ├── SETUP_INSTRUCTIONS.md
│   ├── COMPLETE_SUMMARY.md (review after Weeks 4-5)
│   ├── QUICK_START_GUIDE.md (quick reference)
│   └── Assets/Scripts/
│
└── (future weeks...)
```

---

## 🛠️ Technical Setup

### Required Software:
- **Unity Hub** (latest version)
- **Unity Editor** 2022.3 LTS or newer
- **Visual Studio** or **VS Code** (for C# editing)
- **Input System Package** (installed via Package Manager)

### Unity Packages Required:
- Input System (Week 2+)
- TextMeshPro (Week 6+ for UI)

### Setup Guides:
- Input System setup: `week04/INPUT_ACTIONS_SETUP.md`
- Complete Week 4-5 setup: `week05/QUICK_START_GUIDE.md`

---

## 🎮 Core Concepts Covered

### Programming Concepts:
- C# basics (variables, functions, conditionals)
- Object-Oriented Programming (classes, inheritance)
- Component architecture
- Input handling
- Physics simulation
- Vector math (directions, normalization)
- Quaternion rotations
- Raycasting and collision
- State machines (upcoming)
- Interfaces (upcoming)
- Object pooling (upcoming)

### Unity Systems:
- Editor interface and workflow
- GameObjects and Components
- Transform system
- Physics (Rigidbody, Character Controller)
- Input System (old and new)
- Camera systems
- NavMesh AI (upcoming)
- UI System (upcoming)
- Particle Systems (upcoming)

### Game Design:
- Frame-rate independence (Time.deltaTime)
- Player feel and responsiveness
- Camera controls and collision
- Game architecture patterns

---

## 📊 Progress Tracking

**Total Planned Duration:** 32 weeks (8 months)  
**Current Progress:** Week 4 (12.5% complete)  
**Current Status:** Input System setup complete, movement implementation in progress

```
Weeks 1-3:  ████████████████████ Fundamentals Complete ✅
Week 4:     ██████████░░░░░░░░░░ Input System Setup Complete (50%)
Week 5:     ░░░░░░░░░░░░░░░░░░░░ Movement & Gravity (Next)
Weeks 6-8:  ░░░░░░░░░░░░░░░░░░░░ Camera System
Weeks 9-13: ░░░░░░░░░░░░░░░░░░░░ Combat Basics
Weeks 14-24:░░░░░░░░░░░░░░░░░░░░ Animation & AI
Weeks 25-32:░░░░░░░░░░░░░░░░░░░░ Advanced Features & Polish
```

---

## 💡 Teaching Tips

### What Works Well:
- ✅ Visual progress every session (see something new work!)
- ✅ Clear explanations with real-world analogies
- ✅ "Why this matters" sections keep motivation high
- ✅ Gotchas sections prevent frustration
- ✅ Code examples with extensive comments

### Learning Support Features:
- ✅ Clear visual hierarchy with emojis
- ✅ Breaking complex topics into small steps
- ✅ Multiple explanations (text + code + examples)
- ✅ Frequent breaks built into structure
- ✅ Hands-on practice immediately after theory

### Pacing:
- Don't rush through concepts
- Let student experiment and break things
- Encourage questions
- Celebrate small wins
- Reference previous weeks often

---

## 🎯 Learning Outcomes

By completing this curriculum, the student will be able to:

1. **Navigate Unity confidently**
   - Create and manage projects
   - Use the Editor effectively
   - Understand the component system

2. **Write C# code for games**
   - Understand OOP concepts
   - Implement game mechanics
   - Debug common issues

3. **Implement game systems**
   - Player controls (movement, camera)
   - Combat systems (shooting, damage)
   - AI behaviors (enemies, pathfinding)
   - Game management (UI, states, progression)

4. **Think like a game developer**
   - Break problems into smaller parts
   - Design systems that are extensible
   - Balance gameplay and performance
   - Polish and iterate

5. **Build a complete game**
   - From concept to finished product
   - With professional-quality systems
   - Portfolio-ready project

---

## 🤝 Contributing

This is a private tutoring curriculum, but suggestions for improvements are welcome!

---

## 🎊 Let's Build Something Amazing!

Game development is a journey. Every professional started exactly where you are now. 

**Remember:**
- Make mistakes - that's how you learn!
- Experiment with the code
- Ask questions
- Have fun!

**The best way to learn game development is to BUILD GAMES.** Let's do this! 🚀

---

**Last Updated:** January 2026  
**Current Week:** Week 4 (Input System Setup Complete)  
**Next Session:** Week 5 - Complete Movement & Gravity  
**Next Milestone:** Functional player movement with physics
