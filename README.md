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

**See [FINAL_PROJECT.md](FINAL_PROJECT.md) for the complete roadmap.**

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

#### **Week 4: Player Movement with Character Controller**
Building the foundation for player control:
- Character Controller vs Rigidbody (when to use each)
- Input Actions system (action maps)
- WASD movement with sprint
- Manual gravity implementation
- Basic camera follow system

**📁 Folder:** `week04/`  
**Start Here:** `week04/README.md`

**Key Concepts:**
- Component architecture
- Input Actions (better than checking keys directly)
- Vector2 → Vector3 conversion
- Normalizing diagonal movement
- Understanding why Character Controller is better for player characters

---

#### **Week 5: Mouse Look & Camera Polish**
Adding professional camera controls:
- Euler angles vs Quaternions
- Mouse-controlled camera orbit
- Vertical angle clamping (prevent flipping)
- Camera collision detection with raycasting
- Camera-relative player movement

**📁 Folder:** `week05/`  
**Start Here:** `week05/README.md`

**Key Concepts:**
- 3D rotations (pitch, yaw, roll)
- Quaternion.Euler for rotation conversion
- Layer masks for selective collision
- Sphere casting for smooth collision
- LateUpdate() for camera timing

**After completing both weeks:** See `week05/COMPLETE_SUMMARY.md` for comprehensive overview

---

### 📅 Upcoming Weeks (Planned)

#### **Week 6-7: Shooting Mechanics**
- Raycasting for hitscan weapons
- Projectile physics for bullets
- Weapon base class architecture
- Bullet spawning and ammunition
- Crosshair system

#### **Week 8: Weapon System Architecture**
- Scriptable Objects for weapon data
- Weapon switching system
- Inheritance and polymorphism
- Multiple weapon types (Pistol, Rifle, Shotgun)

#### **Week 9-10: Enemy AI & NavMesh**
- NavMesh AI navigation
- State machines (Idle, Patrol, Chase, Attack)
- Line of sight detection
- Enemy spawn system

#### **Week 11-12: Health & Damage System**
- Health management for player and enemies
- IDamageable interface
- Hit feedback and visual effects
- Death and respawn mechanics

#### **Week 13+: Advanced Features**
- Object pooling for performance
- Enemy dodge AI (projectile prediction!)
- Cover system
- Team AI tactics
- Boss fights
- Polish and effects

**Full roadmap:** See [FINAL_PROJECT.md](FINAL_PROJECT.md)

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

### 🏆 Week 4: "I Can Move!"
- ✅ Player moves with WASD
- ✅ Sprint functionality
- ✅ Gravity and ground detection
- ✅ Basic camera follow

### 🏆 Week 5: "Professional Camera!"
- ✅ Mouse-controlled camera orbit
- ✅ Camera collision detection
- ✅ Camera-relative movement
- ✅ AAA-game quality controls

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

**Total Planned Duration:** 20-24 weeks (5-6 months)  
**Current Progress:** Week 5 (25% complete)

```
Weeks 1-3:  ████████░░░░░░░░░░░░ Fundamentals Complete
Weeks 4-5:  ████████░░░░░░░░░░░░ Player System Complete
Weeks 6-13: ░░░░░░░░░░░░░░░░░░░░ Core Gameplay (In Progress)
Weeks 14-24:░░░░░░░░░░░░░░░░░░░░ Advanced Features
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

**Last Updated:** December 2025  
**Current Week:** Week 5 - Mouse Look & Camera Polish  
**Next Milestone:** Week 6-7 - Shooting Mechanics
