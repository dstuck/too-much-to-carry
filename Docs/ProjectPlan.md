# Too Much To Carry

A game about figuring out how to do eight hands worth of tasks with just two. Like overcooked but your teamates keep pooping their diapers and crawling off of counter tops.

# Gameplay

You must make it through a fixed amount of time while trying to accomplish multiple household tasks

1. Keep the babies happy, clean, and safe
2. Prepare some food
3. Do the laundry

The challenge will be in alternating your limited focus between the babies (who will require both hands to be held at once) and placing them down to accomplish other tasks that may require immediate response like a burning pot of food.

## Controls

Layout is grid based, but character moves continuously with simple movements and two interaction keys, one for each hand. Eventually, maybe hold an empty hand button to move an object. A tile will be highlighted to show which tile is in front of the character.

Each hand will contain a single item or be empty for picking up a new one. Different actions can be performed on various objects based on what is in the hand that interaction.

## UI

There will be a green and a blue square representing left and right hands and display the currently held item. There will be a space in the top of the screen where hint text and warnings can be displayed. The outside of the scren can turn red when babies get mad or a stove is burning.

## Objects

- *Baby: will crawl around and possibly fall off of things if not contained in a crib or basket. Can be picked up, can be changed with diaper.
- *Basket: Keeps babies contained, clean or dirty laundry may be stored in it
- *Clean diaper: can be picked up and put on baby to change it
- Crib: Keeps babies contained and happy for a bit of time
- Counters: high space to place objects on (food, laundry). Unsafe for babies who can crawl off
- Hamper: Can store a queue of objects including laundry and babies. Placed babies can't wander and are happier longer
- Stove: a counter but it heats up and eventually burns food. Very unsafe for babies
- Clean laundry (unfolded): if unfolded, action will fold it
- Clean laundry (folded): If placed in the 

## Equiped Items

- Empty hand: generally pick up or operate on the object in front of you
- Baby: place down on the item in front of you
- Dirty laundry
- Clean laundry (unfolded): can be picked up and placed on a counter to fold


# Implementation Plan

v0.1

- [x] movable character (with stand in sprite)
- [x] two babies that can be picked up and put down
- [x] item slots showing current held item

v0.2
- [x] add baby crying when mad (object and item)
- [x] add baby crawling
- [x] babies get mad after sitting on ground for more than 3 seconds
- [x] add crib that baby can be put into
- [x] babies get mad after sitting in crib

v0.3
- [x] babies poo after random time
- [x] baby cries when dirty diaper
- [x] add changing table to put baby on
- [x] add diapers and diaper stack
- [x] diapers on baby cleans diaper

v0.4
- [x] pan on the stove
- [x] stove heats up food then starts to burn
- [x] add counters and rooms

v0.5
- [x] add laundry object that can be folded or unfolded
- [x] folded and unfolded have different sprites that must appear in game and in UI
    - [ ] if unfolded and on a counter, interacting will fold it rather than pick it up
    - [ ] if unfolded and not on a counter, it will be picked up
    - [ ] if folded, it will always just be picked up
- [x] hamper object that folded clothes will live in
    no script, but we will need to score at end based on folded clothes in hamper
- [x] similarly a refrigerator object that cooked meat will go onto

v0.6 - highlighting
- [x] create HighlightManager system to manage sparkle effects
    - [x] create HighlightableObject component that can be attached to objects
    - [x] create sparkle particle effect prefab (subtle, colored)
    - [x] highlight color: green for left hand, blue for right hand
- [x] implement highlighting rules based on held items:
    - [x] uncooked meat (RawMeat with Raw state) → highlights objects with "Stove" tag
    - [x] cooked/burnt meat (RawMeat with Cooked or Burnt state) → highlights objects with "Refrigerator" tag or Refrigerator component
    - [x] poopy baby (Baby with isDirty=true) → highlights:
        - objects with "Crib" tag AND name contains "Changing" (changing table)
        - objects with Diaper component (diaper stack)
    - [x] unfolded laundry (Laundry with Unfolded state) → highlights objects with "Crib" tag AND name does NOT contain "Changing" (counters, lightly)
    - [x] folded laundry (Laundry with Folded state) → highlights objects with "Hamper" tag
- [x] update highlighting in real-time as items are picked up/put down
- [x] ensure sparkle effect is subtle and doesn't obstruct gameplay
- [x] create Refrigerator script (similar to Hamper) for cooked/burnt meat storage