# Project Context

## Overview

This is a **2D top-down bullet hell roguelite** for CSCI 426, developed as a deliverable game (no longer prototype phase). UI should be built visually in the Unity Editor with scripts handling logic via serialized references — not generated in code.

The Unity project lives in `Prototype6/`. Level system planning doc: `Prototype6/Docs/LEVEL_SYSTEM_PLAN.md`.

## Game Concept

2D top-down arena. The player is **anchored at the center** of a circular tilemap stage. Enemies spawn at the edges and walk toward the player. The player **cannot move** — instead, they press Q/E or Left/Right arrows to **rotate the entire room** (and all enemies with it). A gun auto-fires bullets in random cardinal directions on a fixed timer. Bullets do NOT rotate with the room. The core skill is rotating enemies into the bullet paths.

## Team / File Conventions

Two people are working in this repo in parallel. To avoid merge conflicts:

- **Partner's scripts**: `Rotate.cs`, `Shooting.cs`, `Bullet.cs` — these must NOT be modified by Andrew. They live in `Prototype6/Assets/Scripts/` and are used by partner scenes.
- **Andrew's scripts**: all prefixed with `A_` (e.g. `A_Rotate.cs`, `A_Shooting.cs`, `A_Enemy.cs`). These are independent copies or new scripts used only in `Andrew_Scene`.
- **Andrew's scene**: `Prototype6/Assets/Scenes/Andrew_Scene.unity` — the only scene Andrew edits.
- Partner scenes (`SampleScene`, `TestScene`) should not be touched.

When making changes, always create or edit `A_`-prefixed scripts. Never modify the partner's original scripts.

## Scene Hierarchy (Andrew_Scene)

```
Main Camera          (root — static, not under Rotate)
Rotate               (root — A_WorldRotator2D script)
  ├── Player         (SpriteRenderer, Rigidbody2D gravity=0, CircleCollider2D, A_Shooting, A_PlayerHealth)
  └── Grid           (Grid component)
        └── Stage    (Tilemap + TilemapRenderer, dark grey tint)
GameManager          (root — A_GameManager, A_XPManager, A_WeaponManager, A_AugmentPool)
EnemySpawner         (root — A_EnemySpawner, parents enemies under Rotate)
Canvas               (root — UI, Render Mode = Screen Space - Overlay)
  ├── XPBar          (A_XPBar, anchored top-left)
  │     ├── Background (Image, dark)
  │     ├── Fill       (Image, Filled horizontal)
  │     └── Label      (TextMeshPro, "0/5 XP")
  ├── AugmentPanel   (A_AugmentUI + HorizontalLayoutGroup + CanvasGroup, stretch-fill)
  ├── WeaponInventory (A_WeaponInventoryUI + TextMeshPro, anchored top-right)
  └── HealthUI       (A_HealthUI + TextMeshPro, anchored bottom-left)
EventSystem          (root — auto-created with Canvas)
```

Key parenting rules:
- Everything that should rotate with the room (tilemap, enemies) is a **child of Rotate**.
- The Player is also under Rotate, but the A_WorldRotator2D script preserves its world position/rotation each frame so it appears stationary.
- Bullets are instantiated at world root (not under Rotate) so they travel in fixed world-space directions.
- The EnemySpawner itself is at root, but it parents spawned enemies under Rotate.

## Sorting Layers (render order, back to front)

1. **Default** — unused
2. **Stage** — tilemap floor
3. **Entities** — Player, Enemies, Bullets

## Key Script Relationships

- `A_Enemy` detects projectile hits via `OnTriggerEnter2D`. Checks for `A_Projectile` first (reads its `damage` field), falls back to `Bullet` (damage 1) for backward compatibility.
- `A_Enemy` grants XP on death via `A_XPManager.Instance.AddXP(xpValue)`.
- `A_XPManager` is a singleton that tracks XP/level with an exponential curve (base 5, growth 1.5x). Fires `OnXPChanged` and `OnLevelUp` events.
- `A_XPBar` subscribes to `OnXPChanged` to update the fill bar and text label.
- `A_GameManager` reloads the scene on R press (full reset). Also handles debug key: 1 to add 1 XP.
- `A_EnemySpawner` spawns enemies along the circumference of the circular stage on a timer.

## Weapon / Augment System (RNG Builder)

The core progression mechanic. Each timestep, every weapon in the player's inventory has a probability of firing independently.

### Data Assets (ScriptableObjects)

- **A_WeaponData** (`Create > Game > WeaponData`): defines a weapon — prefab, speed, damage, fire chance, `WeaponType` (Projectile/Area/Line), and optional `duration`/`radius` for area weapons. Visual properties are baked into each weapon's prefab.
- **A_AugmentData** (`Create > Game > AugmentData`): defines an augment card. Types: `NewWeapon`, `ModifyWeapon`, `Tradeoff`, `ModifyAllWeapons`, `ModifyHealth`, `ModifyFireInterval`, `ModifyWeaponStat`. `isUnique` removes it from the pool after being picked.

### Runtime Singletons (all on GameManager object)

- **A_WeaponManager**: holds the player's weapon inventory (`List<WeaponEntry>`). `WeaponEntry` has `currentChance`, `isOnCooldown`, and bonus stats (`bonusDuration`, `bonusRadius`, `bonusDamage`). Starts with Bullet at 100%. Exposes `AddWeapon`, `ModifyChance`, `ModifyAllChances`, `SetCooldown`, `GetHighestChanceEntry`, `GetLowestChanceEntry`, `ModifyWeaponStat`, and `OnInventoryChanged` event.
- **A_AugmentPool**: holds the available augment pool. `GetCards(3)` draws without replacement (duplicates to fill if pool < 3). `ApplyAugment` handles all augment types (weapons, tradeoffs, health, fire interval, weapon stats) and removes unique ones from the pool.

### Firing (A_Shooting)

- Each `fireInterval` (default 1.5s), iterates all weapons from `A_WeaponManager`.
- Skips weapons with `isOnCooldown = true` (e.g. Moat while active).
- Rolls `Random.value < currentChance` for each weapon independently.
- Fires passing weapons sequentially with a `0.15s` stagger delay (coroutine).
- Branches on `WeaponType`:
  - **Projectile**: spawns prefab with velocity in random cardinal direction. Damage = `weapon.damage + entry.bonusDamage`.
  - **Area**: spawns moat prefab at player position, passes duration/radius/damage, sets cooldown on the weapon.
  - **Line**: spawns laser prefab at player position in random cardinal direction, passes damage.
- All spawned objects are at world root (not under Rotate).

### Level-Up UI (A_AugmentUI + A_AugmentCard)

- Subscribes to `A_XPManager.OnLevelUp`.
- Pauses game (`Time.timeScale = 0`), shows 3 cards built dynamically.
- Uses **CanvasGroup** (alpha/blocksRaycasts) to show/hide instead of SetActive, so event subscriptions survive.
- Cards have hover glow (gold outline via `IPointerEnterHandler`) and click to select (`IPointerClickHandler`).
- On selection: applies augment, hides cards, resumes `Time.timeScale = 1`.
- Press **2** while panel is showing to reroll cards (debug feature).

### Weapon Inventory Display (A_WeaponInventoryUI)

- Top-right TextMeshPro text showing all weapons and their current % chance.
- Subscribes to `A_WeaponManager.OnInventoryChanged`.

### Prefabs

- **A_Bullet**: Andrew's bullet prefab (duplicate of partner's Bullet, uses `A_Projectile` instead of `Bullet`). Partner's `Bullet` prefab is untouched.
- **Fireball**: Larger orange projectile, `A_Projectile` component, 2 damage.
- **A_Moat**: Dark green semi-transparent circle. `A_Moat` script handles tick damage (1 HP/sec per enemy via `OnTriggerStay2D` with per-enemy timers), lifetime, and cooldown clearing on destroy. CircleCollider2D trigger. No Rigidbody.
- **A_Laser**: Red-orange narrow rectangle. `A_Laser` script runs telegraph sequence (3 blinks at 35% alpha, then solid fire with `Physics2D.OverlapBoxAll` damage, then fade out). No Rigidbody or collider.

### Current Weapons

| Weapon | Type | Damage | Base Chance | Prefab | Notes |
|--------|------|--------|-------------|--------|-------|
| Bullet | Projectile | 1 | 100% | A_Bullet | |
| Fireball | Projectile | 2 | 40% | Fireball | |
| Moat | Area | 1/sec | 25% | A_Moat | 4s duration, 2 radius, cooldown while active |
| Laser | Line | 2 | 10% | A_Laser | Telegraphed, hits all enemies in line |

### Current Augments

| Augment | Type | Effect | Unique |
|---------|------|--------|--------|
| Fireball | NewWeapon | Adds Fireball at 40% | Yes |
| Moat | NewWeapon | Adds Moat at 25% | Yes |
| Laser | NewWeapon | Adds Laser at 10% | Yes |
| Focus Fire | Tradeoff | +15% highest, -10% lowest | No |
| Underdog | Tradeoff | +20% lowest, -10% highest | No |
| Glass Cannon | ModifyHealth | -1 heart, all weapons +8% | Yes |
| Fortify | ModifyHealth | +1 heart, all weapons -5% | Yes |
| Lucky Round | ModifyAllWeapons | All weapons +3% | No |
| Quickdraw | ModifyFireInterval | Fire interval -0.15s (min 0.5s) | No |
| Extended Moat | ModifyWeaponStat | Moat duration +2s | No |
| Wide Moat | ModifyWeaponStat | Moat radius +1 | No |
| Lingering Laser | ModifyWeaponStat | Laser damage +1 | No |

## Player Health System

- **A_PlayerHealth** (singleton on Player): tracks hearts (default 3). Detects enemy collision via `OnTriggerEnter2D` (checks for `A_Enemy` and `S_Enemy` components). Destroys the enemy on contact and deals 1 damage. Has invincibility frames (1s) with sprite flashing after each hit. Fires `OnHealthChanged(current, max)` and `OnPlayerDied` events. `AddMaxHearts(int)` method for augment integration (positive = heal new hearts, negative = reduce max).
- **A_HealthUI** (on Canvas): TMP text showing filled red hearts (♥) and grey empty hearts. Subscribes to `A_PlayerHealth` events via polling pattern (same as A_WeaponInventoryUI). Anchored bottom-left.

## Obstacles & Shield System

- **Obstacle** prefab (`Assets/Prefab/Obstacle.prefab`): Static arena objects tagged "Obstacle" that block projectiles. `Obstacle.cs` is an empty placeholder (no logic). Projectiles (`Projectile.cs`) destroy themselves on contact via `OnTriggerEnter2D` checking for the "Obstacle" tag. Obstacles are placed directly in the scene (not spawner-driven), presumably as children of Rotate so they rotate with the arena.
- **Shield Enemy** variant (`Assets/Prefab/Enemies/Sheild Enemy Variant.prefab`): An enemy with a shield child object. `Shield.cs` (`Sheild.cs` filename — typo) positions the shield between the enemy and the player at `distanceFromEnemy` (default 1.5), rotating to face the player. The shield blocks projectiles from the front, forcing the player to rotate enemies to hit them from behind.

## Physics Setup

- Player Rigidbody2D: **Dynamic**, Gravity Scale = **0** (top-down, no falling).
- A_Bullet / Fireball prefab Rigidbody2D: **Kinematic**, BoxCollider2D with **Is Trigger = ON**. Partner's original Bullet prefab is untouched.
- Projectile.cs: `OnTriggerEnter2D` destroys projectile on contact with "Obstacle" tagged objects.
- Enemy: **Kinematic** Rigidbody2D, collider with **Is Trigger = ON**.
- Obstacle: Tagged "Obstacle". Projectiles self-destruct on contact.
- The stage tilemap has a `m_Color` tint of `(0.35, 0.35, 0.38)` for a dark stone look.

## Camera

- Orthographic, size 12
- Clear Flags: **Solid Color** (`0.12, 0.12, 0.18` — dark blue-grey)
- To swap background for an image later, add a large SpriteRenderer as a child of the camera at a far Z position.

## Controls

| Key | Action |
|-----|--------|
| Left Arrow / Q | Rotate room counter-clockwise (continuous) |
| Right Arrow / E | Rotate room clockwise (continuous) |
| R | Reset / reload scene |
| 1 | (Debug) Add 1 XP |
| 2 | (Debug) Reroll augment cards while panel is showing |
| (Auto) | Player fires weapons each timestep based on probability rolls |

## Stage / Level System

The level system is fully implemented (all 7 phases complete). See `Prototype6/Docs/LEVEL_SYSTEM_PLAN.md` for the full plan and implementation notes.

### Two Scenes

| Scene | Purpose |
|-------|---------|
| `LobbyScene` (index 0 in Build Settings) | Stage selection UI. Contains Canvas with StageSelectUI, SceneFlowManager (DontDestroyOnLoad), EventSystem, Camera |
| `Andrew_Scene` (index 1) | Gameplay. Everything the prototype has, plus StageManager on GameManager |

### Key Scripts

- **StageData** (`Assets/Scripts/StageData.cs`): ScriptableObject defining a stage — name, number, wave structure (spawn intervals + enemy counts kept in sync), enemy roster with weights, HP bonus, difficulty scaling. `waveCount` is a derived property from array length. Has a custom Inspector editor (`Assets/Scripts/Editor/StageDataEditor.cs`). Assets live in `Assets/Prefab/StageData/`.
- **EnemySpawner** (`Assets/Scripts/EnemySpawner.cs`): `Configure(StageData)` overwrites all spawner fields from a StageData asset and sets `finiteMode = true`. In finite mode, spawning stops after all waves complete (`doneSpawning`), and `OnAllWavesComplete` fires only when `activeEnemyCount` reaches 0 (all enemies killed). Tracks live enemies via `activeEnemyCount` (incremented on spawn, decremented by `RegisterEnemyDeath()`).
- **StageManager** (`Assets/Scripts/StageManager.cs`): Singleton on GameManager. Lazy-subscribes to `EnemySpawner.OnAllWavesComplete` (win) and `PlayerHealth.OnPlayerDied` (loss). Reads `SceneFlowManager.SelectedStage` if available, falls back to serialized `currentStage` field. Stars = hearts remaining at win (clamped 1-3). Fires `OnStageEnded(StageResult, int stars)`.
- **StageProgressData** (`Assets/Scripts/StageProgressData.cs`): Static utility class. Saves/loads stage clear status and best stars via PlayerPrefs. `SaveResult` never downgrades stars. `IsUnlocked(n)` = stage 1 always true, else previous stage cleared.
- **SceneFlowManager** (`Assets/Scripts/SceneFlowManager.cs`): DontDestroyOnLoad singleton. Carries `SelectedStage` between scenes. `allStages` list populated in Inspector. Methods: `GoToStage()`, `GoToLobby()`, `RetryCurrentStage()`, `GetNextStage()`.
- **StageSelectUI** (`Assets/Scripts/StageSelectUI.cs`): Lobby UI logic. References editor-built UI elements via serialized fields (title text, 3 star Images, play/arrow buttons, lock icon). Stars tinted gold/black. Arrows greyed out when adjacent stage is locked. Debug keys: P = 3-star clear current stage, O = wipe progress.

### Enemy Death → Win Ordering (important)

When an enemy dies in `Enemy.TakeDamage()`:
1. `RegisterDeath()` → decrements `activeEnemyCount` → may trigger win
2. `AddXP()` → may trigger level-up → augment UI checks `StageManager.Result`
3. `Destroy(gameObject)` → `OnDestroy()` calls `RegisterDeath()` again (guarded by `deathRegistered` flag)

This ordering ensures the win condition is set BEFORE the augment UI checks whether to show. `AugmentUI.ShowAugmentSelection` is suppressed when `StageManager.Result != None`, and card selection doesn't resume `timeScale` if the stage already ended.

### ResultsScreen

- **ResultsScreen** (`Assets/Scripts/ResultsScreen.cs`): Code-generated win/loss overlay on gameplay Canvas. Subscribes to `StageManager.OnStageEnded`. On win: calls `StageProgressData.SaveResult()`, shows stars + kill count, Next Stage / Lobby buttons. On loss: shows kill count, Retry / Lobby buttons. Has serialized `starSprite` field (assign in Inspector for proper star visuals). DeathScreen.cs still exists as fallback for non-staged play.

### Debug Keys

| Key | Where | Action |
|-----|-------|--------|
| P | Lobby | 3-star clear current stage |
| O | Lobby | Wipe all progress |
| 4 | Gameplay | Force win (StageManager.ForceWin) |
| R | Gameplay | Retry via SceneFlowManager (or fallback reload) |

### Stage Assets

3 stages in `Assets/Stages/`: Stage_1, Stage_2, Stage_3. Create more via Project > right-click > Create > Game > StageData, then add to SceneFlowManager.allStages in LobbyScene.

## Design Notes / Gotchas

- **Script execution order matters**: Singletons (A_WeaponManager, A_XPManager, A_AugmentPool) set their `Instance` in `Awake()`. Other scripts that depend on them subscribe in `Start()` or poll in `Update()` to handle timing safely.
- **A_AugmentUI uses CanvasGroup, not SetActive**: It must stay enabled for event subscriptions to survive. Visibility is toggled via `alpha`/`blocksRaycasts`/`interactable`.
- **A_WeaponInventoryUI polls for A_WeaponManager**: It subscribes on the first `Update()` frame where `Instance` is available, to avoid execution-order issues.
- **`fireInterval` lives on A_Shooting** (on the Player object). The `ModifyFireInterval` augment type finds the `A_Shooting` component via `FindAnyObjectByType` and modifies it directly, floored at 0.5s.
- **A_PlayerHealth uses the same polling pattern** as A_WeaponInventoryUI: A_HealthUI subscribes on the first `Update()` frame where `A_PlayerHealth.Instance` is available.
- **A_Projectile.damage is hidden in Inspector** (`[HideInInspector]`) because it's always set at spawn time by A_Shooting from A_WeaponData. The field exists only as a runtime data carrier that A_Enemy reads on collision.
- **Augment pool draw**: draws without replacement per level-up. If the pool has fewer augments than cards requested, duplicates fill the remaining slots. Unique augments (like NewWeapon) are removed from the pool after being picked.
- **Moat cooldown**: While a Moat instance is alive, `isOnCooldown` is true on its `WeaponEntry`, preventing overlapping moats. `A_Moat.OnDestroy` clears the cooldown.
- **Laser damage is instant**: `Physics2D.OverlapBoxAll` hits all enemies in the box once when the laser fires (after telegraph). It does not persist.
- **WeaponEntry bonus stats**: `bonusDuration`, `bonusRadius`, `bonusDamage` are added to base weapon values at fire time. Modified by `ModifyWeaponStat` augments.
- **Tradeoff augments use dynamic targeting**: `TargetMode.Highest`/`Lowest` picks the weapon with the highest/lowest `currentChance` at the time of selection, so the same card has different effects depending on the player's inventory state.
