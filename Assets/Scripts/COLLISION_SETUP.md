# Collision Setup Guide

## How Collisions Work in Unity

**Important**: Colliders alone do NOT block movement in Unity. You need:
- **Rigidbody2D** on the moving object (the player)
- **Collider2D** on both objects
- Colliders must NOT be triggers (unless you want them to pass through)

## Setup for This Game

### Player (Should Block Movement)
- Add **Rigidbody2D** component
  - Set Body Type to "Kinematic" (we're moving via transform, not physics)
  - Or use "Dynamic" if you want physics-based movement
- Add **Collider2D** (BoxCollider2D or CircleCollider2D)
  - Make sure "Is Trigger" is **UNCHECKED**

### Babies (Should NOT Block Movement)
- Add **Collider2D** (BoxCollider2D or CircleCollider2D)
  - Make sure "Is Trigger" is **CHECKED** ✓
  - This allows interaction detection but doesn't block movement
  - No Rigidbody2D needed

### Future: Walls/Counters (Should Block Movement)
- Add **Collider2D** (BoxCollider2D)
  - Make sure "Is Trigger" is **UNCHECKED**
  - No Rigidbody2D needed (static colliders)
  - Player's Rigidbody2D will collide with these automatically

## Physics Layers (Optional but Recommended)

For better control, set up Physics Layers:
1. Edit → Project Settings → Tags and Layers
2. Create layers:
   - "Player" (layer 8)
   - "Items" (layer 9) - for babies and pickable items
   - "Environment" (layer 10) - for walls/counters
3. Set Physics2D collision matrix:
   - Player collides with: Environment (walls/counters)
   - Player does NOT collide with: Items (babies pass through)
   - Items don't collide with anything (they're triggers)

## Current Code Notes

The interaction detection code checks both:
- Regular colliders (non-triggers) - for walls/counters
- Trigger colliders - for babies and items

This allows babies to have trigger colliders for detection without blocking movement.
