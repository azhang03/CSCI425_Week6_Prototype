# CSCI 426 — Week 6 Prototype

A 2D top-down arena shooter built in Unity for CSCI 426 Game Prototyping.

---

## Game Overview

The player is locked at the center of a circular stone arena. Enemies continuously spawn at the edges and walk inward. The player cannot move — instead, they rotate the entire room (and all enemies with it) using the arrow keys or Q/E. The player's weapons fire automatically in fixed world-space directions on a timer.

The core skill is **rotating enemies into the paths of your own bullets**. Positioning yourself isn't an option; anticipating where your shots will land is.

---

## Randomness Design

This prototype uses **input randomness** throughout — randomness always produces visible state that the player then reacts to.

### Input Randomness — Weapons Firing

Each fire interval (default 1.5 seconds), every weapon in the player's inventory independently rolls against its fire chance. The results — which weapons fired and in which random cardinal directions — appear on screen *before* the player decides how to rotate. The player reads that state (laser telegraphing left, bullet going up) and rotates enemies into those paths.

The randomness is the input to the rotation decision, not a consequence of it. A well-timed laser proc or a fortunate bullet direction is something the player must act on in real time.

Fire chances are **non-uniform by design**. Starting weapons are reliable (Bullet at 100%), new weapons are impactful but infrequent (Laser at 10%), and augments let the player deliberately shift these distributions toward whatever playstyle they prefer.

### Input Randomness — the Augment System

Every 5 kills, the player levels up and is presented with 3 augment cards drawn randomly from the available pool. The player sees the options and chooses — the randomness shapes the decision space before the choice is made.

The first level-up always presents the three new weapons (Fireball, Moat, Laser), giving players a guaranteed and meaningful first choice. Subsequent draws use eligibility filtering — upgrade cards only appear if you own the weapon, tradeoff cards only appear with two or more weapons, and health-loss cards never appear if they would kill you.

The augment pool uses **weighted-by-rarity** behavior through the `isUnique` flag. New weapon cards disappear after being picked, ensuring each weapon can only be acquired once. Repeatable cards (Lucky Round, Quickdraw, Focus Fire) remain in the pool, becoming more likely to surface as unique cards are exhausted.

### Enemy Spawning — Weighted Randomness

Enemies spawn from a weighted pool containing three types: a standard enemy, a spiral-pathing enemy, and a teleporting enemy. Spawn weights are configured in the Inspector, allowing the mix to favor common enemies while making more dangerous variants rarer.

---

## Augment Cards

Cards are drawn at level-up. Unique cards are removed from the pool when selected.

**New Weapons (Unique)**
- Fireball, Moat, Laser — adds the weapon to your inventory at its base fire chance

**Tradeoff Cards**
- Focus Fire — +15% to your best weapon, -10% to your worst
- Underdog — +20% to your worst weapon, -10% to your best

**Upside Cards**
- Lucky Round — all weapons +3%
- Quickdraw — fire interval -0.15s (minimum 0.5s)

**Health Cards (Unique)**
- Glass Cannon — lose 1 heart, all weapons +8%
- Fortify — gain 1 heart, all weapons -5%

**Weapon Upgrades** (require owning the weapon)
- Extended Moat — Moat lasts 2s longer
- Wide Moat — Moat radius +1
- Lingering Laser — Laser damage +1
- Thick Laser — Laser width +0.2
- Fireball Radius — Fireball size +50%

---

## Controls

| Key | Action |
|-----|--------|
| Left Arrow / Q | Rotate room counter-clockwise |
| Right Arrow / E | Rotate room clockwise |
| Esc | Pause / Resume |
| R | Reset scene |
| 1 | (Debug) Add 1 XP |
| 2 | (Debug) Reroll augment cards |

---

## Project Structure

```
Prototype6/
  Assets/
    Scenes/
      Andrew_Scene.unity     
      SampleScene.unity       
    Scripts/
      A_*.cs               
      S_Enemy.cs, etc.        
    Prefab/
      Data/                   — ScriptableObject assets (weapons, augments)
      Enemies/                — Enemy prefab variants
```
