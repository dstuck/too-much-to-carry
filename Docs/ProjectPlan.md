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

- [ ] pan that can be picked up and put on stove
- [ ] stove heats up food then starts to burn
- [ ] add counters and rooms

