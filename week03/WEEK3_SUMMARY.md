# Week 3 Summary - Physics & Forces

## Overview
Week 3 has been upgraded from basic movement to **Physics & Forces** - a more advanced curriculum that bridges the gap between simple transforms and realistic physics simulation.

## Why This Approach?

Based on your feedback:
- ✅ Student already understands events (keyboard events)
- ✅ Student is interested in **gravitation** and more advanced topics
- ✅ Week 2's keyboard input was valuable but potentially "too basic"
- ✅ Need something challenging but not overwhelming

## What's Covered

### Core Topics
1. **Rigidbody Fundamentals** - Unity's physics brain
2. **Update() vs FixedUpdate()** - When to use each for physics
3. **Velocity vs Forces** - Two approaches to movement
4. **ForceMode Types** - Force, Impulse, Acceleration, VelocityChange
5. **Custom Gravity** - Planetary gravity with inverse square law
6. **Orbital Mechanics** - Making objects orbit like satellites

### Scripts Created

1. **PhysicsMover.cs** (Velocity-based)
   - Direct velocity control
   - Custom gravity option
   - Good for arcade-style movement
   - Teaches: `rb.velocity`, speed clamping

2. **ForceController.cs** (Force-based)
   - Realistic momentum-based movement
   - Jumping with ground detection
   - Dynamic drag (ground vs air)
   - Teaches: `rb.AddForce()`, `ForceMode.Impulse`, raycasting

3. **CustomGravityObject.cs** (Advanced)
   - Gravitational attraction to a point
   - Inverse square law implementation
   - Orbital mechanics
   - Teaches: Real physics simulation, Vector math

4. **PhysicsDebugger.cs** (Helper)
   - Visualizes velocity and forces
   - On-screen debug information
   - Helps students "see" physics

## Progression Path

**Week 1:** Color changes (transform, Time.deltaTime, MonoBehaviour)  
**Week 2:** Scale changes (Input System, if/else, Vector3)  
**Week 3:** Physics movement (Rigidbody, velocity, forces, gravity) ← **NEW**  
**Week 4:** Character controller (combine all concepts + raycasting)

## Key Concepts Emphasized

### Not Too Basic
- Skips lengthy event explanations (student knows this)
- Jumps directly to Rigidbody physics
- Introduces real physics formulas (F = G × m₁×m₂ / r²)
- Covers advanced topics like orbital mechanics

### Not Overwhelming
- Clear step-by-step explanations
- Builds on Week 1 & 2 foundations
- Practical examples with real-world analogies
- Hands-on experiments to "feel" the physics

### Real-World Connections
- Shopping cart = applying force
- Jumping = impulse
- Ice skating = zero drag
- Planetary orbits = gravity + velocity

## Student Benefits

1. **Immediate Gratification**
   - Make objects orbit a planet (cool visual!)
   - Jump around with momentum
   - Create mini solar systems

2. **Real Physics Understanding**
   - Why inverse square law matters
   - Difference between velocity and acceleration
   - How orbital mechanics work

3. **Foundation for Advanced Topics**
   - Prepares for character controllers (Week 4)
   - Understanding needed for vehicle physics
   - Basis for all Unity physics puzzles

## Teaching Tips

### If Student Finds It Too Easy
- Challenge: Multi-planet systems with gravitational interference
- Challenge: Figure-8 orbital patterns
- Challenge: Add `rb.AddTorque()` for rotation physics
- Extension: Physics materials (bouncy, friction)

### If Student Finds It Challenging
- Focus on PhysicsMover first (simpler velocity approach)
- Use PhysicsDebugger to visualize what's happening
- Spend more time on experiments (hands-on learning)
- Skip CustomGravity initially, revisit later

## Homework Challenges

1. **Perfect Circular Orbit** - Find ideal settings for stable orbit
2. **Force-Based Platformer** - Build obstacle course, tune feel
3. **Zero-Gravity Spaceship** - Navigate with preserved momentum

Each challenge reinforces different aspects of physics understanding.

## Next Steps

**Week 4 Preview:** Character Controllers
- Combine input + physics + raycasting
- Ground detection with Physics.Raycast
- Camera following
- Smooth movement feel
- Optional: Animation system

This will tie together everything from Weeks 1-3 into a complete playable character!

---

## Quick Reference

**Key Scripts Locations:**
- `week03/Assets/Scripts/PhysicsMover.cs`
- `week03/Assets/Scripts/ForceController.cs`
- `week03/Assets/Scripts/CustomGravityObject.cs`
- `week03/Assets/Scripts/PhysicsDebugger.cs`

**README:** `week03/README.md`

**Pattern Followed:** Week 2's structure
- ✅ Recap section
- ✅ Detailed concept explanations
- ✅ Step-by-step instructions
- ✅ Experiments and challenges
- ✅ Teacher notes
- ✅ Parent summary
- ✅ Troubleshooting guide

---

**Status:** ✅ Complete, committed (390f496), pushed to origin/main
