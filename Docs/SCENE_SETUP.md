# Scene Setup Instructions

This document describes how to set up the scene in Unity for v0.1.

## 1. Grid Setup

1. In Hierarchy, right-click → Create Empty
2. Name it "Grid"
3. Select it, then in Inspector: Add Component → Grid (from UnityEngine.Tilemaps)
4. Set Cell Size to:
   - X: 0.16
   - Y: 0.16
   - Z: 0

## 2. Camera Setup

1. Select Main Camera in Hierarchy
2. In Inspector, Camera component:
   - Set Projection to Orthographic
   - Set Size to 2 or 2.5 (to zoom in for 0.16 unit grid cells)

## 3. Player Setup

1. Create Empty GameObject, name it "Player"
2. Add components:
   - SpriteRenderer (assign a sprite from Assets/Sprites, e.g., sprZinkWalkS.png)
   - PlayerController script
3. In PlayerController component:
   - Assign Grid reference (drag Grid GameObject)
   - Assign UIManager reference (will be created in step 5)
   - Set Move Speed (default 2 is fine)
4. Add a Collider2D (BoxCollider2D or CircleCollider2D) to Player for interaction detection

## 4. Baby Setup

1. Create Empty GameObject, name it "Baby1"
2. Add components:
   - SpriteRenderer (assign a baby sprite from Assets/Sprites)
   - Baby script (extends HoldableItem)
   - Collider2D (BoxCollider2D recommended) - needed for interaction detection
3. Position Baby1 at a grid-aligned position (e.g., (0.16, 0, 0) or use Grid snapping)
4. Repeat for "Baby2" at a different position

## 5. UI Setup

1. Create UI → Canvas (if not already present)
2. Create UI → Image as child of Canvas, name it "LeftHandSlot"
   - Set color to Green
   - Position in bottom-left corner
   - Add HandSlotUI component
   - Check "Is Left Hand" checkbox
3. Create UI → Image as child of Canvas, name it "RightHandSlot"
   - Set color to Blue
   - Position next to LeftHandSlot
   - Add HandSlotUI component
   - Uncheck "Is Left Hand" checkbox
4. Create Empty GameObject as child of Canvas, name it "UIManager"
   - Add UIManager component
   - Assign LeftHandSlot and RightHandSlot references

## 6. Input System Setup

The InputSystem_Actions.inputactions file is already configured. Make sure:
1. Input System package is installed (already in project)
2. The .inputactions file is assigned in Project Settings → Input System Package → Input Actions Asset

## Notes

- Grid cell size is 0.16 units (16 pixels at 100 pixels per unit)
- Player moves continuously but interactions are grid-aligned
- Babies need Collider2D components for interaction detection
- Hand slots will automatically update when items are picked up/put down
