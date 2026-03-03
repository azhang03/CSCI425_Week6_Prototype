# CSCI 426 — Week 6 Prototype

A 2D top-down arena shooter built in Unity for CSCI 426 Game Prototyping.

---

## Game Overview

The player is locked at the center of a circular stone arena. Enemies continuously spawn at the edges and walk inward. The player cannot move — instead, they rotate the entire room (and all enemies with it) using the arrow keys or Q/E. The player's weapons fire automatically in fixed world-space directions on a timer.

The core skill is **rotating enemies into the paths of your own bullets**. Positioning yourself isn't an option; anticipating where your shots will land is.

---

## Randomness Design

This prototype uses **both input and output randomness** in layered ways.

### Input Randomness — the Augment System

Every 5 kills, the player levels up and is presented with 3 augment cards to choose from. These cards are drawn randomly from the available pool and shape what the player's build looks like going forward.

This is input randomness: the random draw happens *before* the player makes a decision. The player sees what they got and must decide how to use it — commit to a high-damage weapon, trade off reliability for power, or shore up survivability with extra hearts.

The first level-up always presents the three new weapons (Fireball, Moat, Laser), giving players a guaranteed and meaningful first choice. Subsequent draws are random, with eligibility filtering — you won't see upgrade cards for weapons you don't own, tradeoff cards unless you have two weapons, or health-loss cards if they would kill you.

The augment pool uses **weighted-by-rarity** behavior through the `isUnique` flag. New weapon cards disappear after being picked, ensuring each weapon can only be acquired once. Repeatable cards (Lucky Round, Quickdraw, Focus Fire) remain in the pool, making them increasingly likely to appear as the unique cards thin out.

### Output Randomness — Weapons Firing

Each fire interval (default 1.5 seconds), every weapon in the player's inventory independently rolls against its fire chance. A bullet at 100% always fires. A fireball at 40% fires roughly two out of every five intervals. A laser at 10% fires rarely.

This is output randomness: the player has already built their loadout and the room rotation is already in progress — the dice rolls happen *after* the player's strategic decisions are made. 

Fire chances are **non-uniform by design**. Starting weapons are reliable (Bullet at 100%), new weapons are impactful but infrequent (Laser at 20%), and augments let the player deliberately shift these distributions toward whatever playstyle they prefer.

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
