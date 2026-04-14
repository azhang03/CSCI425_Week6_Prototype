# Project Context

## Overview

This is a **2D top-down bullet hell roguelite** for CSCI 426, developed as a deliverable game (no longer prototype phase). UI should be built visually in the Unity Editor with scripts handling logic via serialized references — not generated in code.

The Unity project lives in `Prototype6/`. Level system planning doc: `Prototype6/Docs/LEVEL_SYSTEM_PLAN.md`.

## Game Concept

2D top-down arena. The player is **anchored at the center** of a circular tilemap stage. Enemies spawn at the edges and walk toward the player. The player **cannot move** — instead, they press Q/E or Left/Right arrows to **rotate the entire room** (and all enemies with it). A gun auto-fires bullets in random cardinal directions on a fixed timer. Bullets do NOT rotate with the room. The core skill is rotating enemies into the bullet paths.

## Team / File Conventions

Two people work in this repo. Scripts and scenes are now fully unified — there is no longer an `A_` prefix convention. All scripts are shared and live in `Prototype6/Assets/Scripts/`. Both teammates work in `Andrew_Scene`.

## Scene Hierarchy (Andrew_Scene)

```
Main Camera          (root — static, not under Rotate)
Rotate               (root — Rotate script / WorldRotator)
  ├── Player         (SpriteRenderer, Rigidbody2D gravity=0, CircleCollider2D, Shooting, PlayerHealth)
  └── Grid           (Grid component)
        └── Stage    (Tilemap + TilemapRenderer, dark grey tint)
GameManager          (root — GameManager, XPManager, WeaponManager, AugmentPool, StageManager)
EnemySpawner         (root — EnemySpawner, parents enemies under Rotate)
ObstacleSpawner      (root — ObstacleSpawner, parents obstacles under Rotate)
Canvas               (root — UI, Render Mode = Screen Space - Overlay)
  ├── XPBar          (XPBar, anchored top-left)
  ├── AugmentPanel   (AugmentUI + HorizontalLayoutGroup + CanvasGroup, stretch-fill)
  ├── WeaponInventory (WeaponInventoryUI + TextMeshPro, anchored top-right)
  ├── HealthUI       (HealthUI + TextMeshPro, anchored bottom-left)
  └── ResultsScreen  (ResultsScreen + CanvasGroup — win/loss overlay)
EventSystem          (root — auto-created with Canvas)
```

Key parenting rules:
- Everything that rotates with the room (tilemap, enemies, obstacles) is a **child of Rotate**.
- The Player is also under Rotate, but the Rotate/WorldRotator script preserves its world position/rotation each frame so it appears stationary.
- Bullets are instantiated at world root (not under Rotate) so they travel in fixed world-space directions.
- EnemySpawner and ObstacleSpawner are at root but parent their spawned objects under Rotate.

## Sorting Layers (render order, back to front)

1. **Default** — unused
2. **Stage** — tilemap floor
3. **Entities** — Player, Enemies, Bullets

## Key Script Relationships

- `Enemy` detects projectile hits via `OnTriggerEnter2D`. Reads `Projectile.damage`, falls back to `Bullet` component (damage 1) for backward compatibility.
- `Enemy` grants XP on death via `XPManager.Instance.AddXP(xpValue)`. Calls `RegisterDeath()` BEFORE AddXP so win is detected before level-up triggers augment UI.
- `XPManager` is a singleton that tracks XP/level with an exponential curve (base 5, growth 1.5x). Fires `OnXPChanged` and `OnLevelUp` events.
- `XPBar` subscribes to `OnXPChanged` to update the fill bar and text label.
- `GameManager` handles debug key: 1 to add 1 XP.
- `EnemySpawner` spawns enemies along the circumference of the circular stage on a timer.

## Weapon / Augment System (RNG Builder)

The core progression mechanic. Each timestep, every weapon in the player's inventory has a probability of firing independently.

### Data Assets (ScriptableObjects)

- **WeaponData** (`Create > Game > WeaponData`): defines a weapon — prefab, speed, damage, fire chance, `WeaponType` (Projectile/Area/Line), and optional `duration`/`radius` for area weapons. Visual properties are baked into each weapon's prefab.
- **AugmentData** (`Create > Game > AugmentData`): defines an augment card. Types: `NewWeapon`, `ModifyWeapon`, `Tradeoff`, `ModifyAllWeapons`, `ModifyHealth`, `ModifyFireInterval`, `ModifyWeaponStat`. `isUnique` removes it from the pool after being picked.

### Runtime Singletons (all on GameManager object)

- **WeaponManager**: holds the player's weapon inventory (`List<WeaponEntry>`). `WeaponEntry` has `currentChance`, `isOnCooldown`, and bonus stats (`bonusDuration`, `bonusRadius`, `bonusDamage`). Starts with Bullet at 100%. Exposes `AddWeapon`, `ModifyChance`, `ModifyAllChances`, `SetCooldown`, `GetHighestChanceEntry`, `GetLowestChanceEntry`, `ModifyWeaponStat`, and `OnInventoryChanged` event.
- **AugmentPool**: holds the available augment pool. `GetCards(3)` draws without replacement (duplicates to fill if pool < 3). `ApplyAugment` handles all augment types and removes unique ones from the pool.

### Firing (Shooting)

- Each `fireInterval` (default 1.5s), iterates all weapons from `WeaponManager`.
- Skips weapons with `isOnCooldown = true` (e.g. Moat while active).
- Rolls `Random.value < currentChance` for each weapon independently.
- Fires passing weapons sequentially with a `0.15s` stagger delay (coroutine).
- Branches on `WeaponType`:
  - **Projectile**: spawns prefab with velocity in random cardinal direction. Damage = `weapon.damage + entry.bonusDamage`.
  - **Area**: spawns moat prefab at player position, passes duration/radius/damage, sets cooldown on the weapon.
  - **Line**: spawns laser prefab at player position in random cardinal direction, passes damage.
- All spawned objects are at world root (not under Rotate).

### Level-Up UI (AugmentUI + AugmentCard)

- Subscribes to `XPManager.OnLevelUp`.
- Pauses game (`Time.timeScale = 0`), shows 3 cards built dynamically.
- Uses **CanvasGroup** (alpha/blocksRaycasts) to show/hide instead of SetActive, so event subscriptions survive.
- Cards have hover glow and click to select.
- On selection: applies augment, hides cards, resumes `Time.timeScale = 1` (unless stage already ended).
- **Suppressed** when `StageManager.Result != None` — no augment popup after winning.
- Press **2** while panel is showing to reroll cards (debug).

### Weapon Inventory Display (WeaponInventoryUI)

- Top-right TextMeshPro text showing all weapons and their current % chance.
- Subscribes to `WeaponManager.OnInventoryChanged`.

### Current Weapons

| Weapon | Type | Damage | Base Chance | Notes |
|--------|------|--------|-------------|-------|
| Bullet | Projectile | 1 | 100% | |
| Fireball | Projectile | 2 | 40% | |
| Moat | Area | 1/sec | 25% | 4s duration, 2 radius, cooldown while active |
| Laser | Line | 2 | 10% | Telegraphed, hits all enemies in line |

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

- **PlayerHealth** (singleton on Player): tracks hearts (default 3). Detects enemy collision via `OnTriggerEnter2D`. Destroys the enemy on contact and deals 1 damage. Has invincibility frames (1s) with sprite flashing. Fires `OnHealthChanged(current, max)` and `OnPlayerDied` events. `AddMaxHearts(int)` for augment integration.
- **HealthUI** (on Canvas): TMP text showing filled/empty hearts. Subscribes to `PlayerHealth` events via polling pattern.

## Obstacles & Shield System

- **Obstacle** prefab (`Assets/Prefab/Obstacle.prefab`): Tagged "Obstacle". Projectiles destroy themselves on contact. `Obstacle.cs` is an empty placeholder.
- **ObstacleSpawner** (`Assets/Scripts/ObstacleSpawner.cs`): Singleton. `Configure(StageData)` reads `stageData.obstacleLayout` and instantiates obstacles as children of `rotateParent`. `clearExistingOnSpawn = true` by default.
- **ObstaclePlacement** (`Assets/Scripts/ObstaclePlacement.cs`): Serializable struct with `localPosition`, `localScale`, `localRotationZ`. Used in `StageData.obstacleLayout`.
- **Shield Enemy** variant (`Assets/Prefab/Enemies/Sheild Enemy Variant.prefab`): Has a shield child object (`Sheild.cs` — typo in filename) that orbits between the enemy and player, blocking projectiles from the front.

## Physics Setup

- Player Rigidbody2D: **Dynamic**, Gravity Scale = **0**.
- Bullet/Fireball prefab Rigidbody2D: **Kinematic**, BoxCollider2D with **Is Trigger = ON**.
- Projectile.cs: destroys itself on contact with "Obstacle" tagged objects.
- Enemy: **Kinematic** Rigidbody2D, collider with **Is Trigger = ON**.
- The stage tilemap has a `m_Color` tint of `(0.35, 0.35, 0.38)` for a dark stone look.

## Camera

- Orthographic, size 12
- Clear Flags: **Solid Color** (`0.12, 0.12, 0.18` — dark blue-grey)

## Controls

| Key | Action |
|-----|--------|
| Left Arrow / Q | Rotate room counter-clockwise |
| Right Arrow / E | Rotate room clockwise |
| R | Retry stage via SceneFlowManager (fallback: reload scene) |
| 1 | (Debug) Add 1 XP |
| 2 | (Debug) Reroll augment cards while panel is showing |
| 4 | (Debug) Force win |
| (Auto) | Player fires weapons each timestep based on probability rolls |

## Stage / Level System

The level system is fully implemented (all 7 phases complete). See `Prototype6/Docs/LEVEL_SYSTEM_PLAN.md` for the full plan and implementation notes.

### Two Scenes

| Scene | Purpose |
|-------|---------|
| `LobbyScene` (index 0 in Build Settings) | Stage selection UI. Contains Canvas with StageSelectUI, SceneFlowManager (DontDestroyOnLoad), EventSystem, Camera |
| `Andrew_Scene` (index 1) | Gameplay. Everything the prototype has, plus StageManager on GameManager |

### Key Scripts

- **StageData** (`Assets/Scripts/StageData.cs`): ScriptableObject defining a stage — name, number, wave structure (spawn intervals + enemy counts kept in sync), enemy roster with weights, obstacle layout, HP bonus, difficulty scaling. `waveCount` is a derived property from array length. Has a custom Inspector editor (`Assets/Scripts/Editor/StageDataEditor.cs`). Assets live in `Assets/Stages/`.
- **EnemySpawner** (`Assets/Scripts/EnemySpawner.cs`): `Configure(StageData)` overwrites all spawner fields and sets `finiteMode = true`. Fires `OnAllWavesComplete` only when all waves are done spawning AND `activeEnemyCount == 0`. Tracks live enemies via `RegisterEnemyDeath()`.
- **StageManager** (`Assets/Scripts/StageManager.cs`): Singleton on GameManager. Lazy-subscribes to `EnemySpawner.OnAllWavesComplete` (win) and `PlayerHealth.OnPlayerDied` (loss). Also calls `ObstacleSpawner.Instance.Configure(currentStage)`. Reads `SceneFlowManager.SelectedStage` if available, falls back to serialized `currentStage`. Stars = hearts remaining at win (clamped 1-3). Fires `OnStageEnded(StageResult, int stars)`. Has `ForceWin()` for debug.
- **StageProgressData** (`Assets/Scripts/StageProgressData.cs`): Static utility class. Saves/loads via PlayerPrefs. `SaveResult` never downgrades stars. `IsUnlocked(n)` = stage 1 always, else previous cleared.
- **SceneFlowManager** (`Assets/Scripts/SceneFlowManager.cs`): DontDestroyOnLoad singleton. Carries `SelectedStage` between scenes. `allStages` list populated in Inspector. Methods: `GoToStage()`, `GoToLobby()`, `RetryCurrentStage()`, `GetNextStage()`.
- **StageSelectUI** (`Assets/Scripts/StageSelectUI.cs`): Lobby UI logic via serialized Inspector fields. Stars tinted gold/black. Arrows greyed out when adjacent stage is locked. `debugMode = true` starts on last stage. Debug keys: P = 3-star clear, O = wipe progress.
- **ResultsScreen** (`Assets/Scripts/ResultsScreen.cs`): Code-generated win/loss overlay. Subscribes to `StageManager.OnStageEnded`. On win: saves progress, shows stars + kill count, Next Stage / Lobby buttons. On loss: Retry / Lobby buttons.

### Enemy Death → Win Ordering (important)

When an enemy dies in `Enemy.TakeDamage()`:
1. `RegisterDeath()` → decrements `activeEnemyCount` → may trigger win → sets `StageManager.Result`
2. `AddXP()` → may trigger level-up → `AugmentUI.ShowAugmentSelection` checks `StageManager.Result != None` → suppressed if already won
3. `Destroy(gameObject)` → `OnDestroy()` calls `RegisterDeath()` again (guarded by `deathRegistered` flag)

### Debug Keys

| Key | Where | Action |
|-----|-------|--------|
| P | Lobby | 3-star clear current stage |
| O | Lobby | Wipe all progress |
| 4 | Gameplay | Force win |
| R | Gameplay | Retry via SceneFlowManager |

### Stage Assets

3 stages in `Assets/Stages/`: Stage_1, Stage_2, Stage_3. Create more via right-click > Create > Game > StageData, add to SceneFlowManager.allStages in LobbyScene.

## Design Notes / Gotchas

- **Script execution order matters**: Singletons set their `Instance` in `Awake()`. Dependent scripts subscribe in `Start()` or poll in `Update()` (lazy-subscribe pattern).
- **AugmentUI uses CanvasGroup, not SetActive**: must stay enabled for event subscriptions to survive.
- **WeaponInventoryUI polls for WeaponManager**: subscribes on first `Update()` frame where Instance is available.
- **`fireInterval` lives on Shooting** (on Player). `ModifyFireInterval` augment finds it via `FindAnyObjectByType` and modifies it directly, floored at 0.5s.
- **Projectile.damage is hidden in Inspector** (`[HideInInspector]`) — always set at spawn time by Shooting from WeaponData.
- **Augment pool draw**: without replacement per level-up. Unique augments removed after picked.
- **Moat cooldown**: `isOnCooldown` true while a Moat instance is alive. `Moat.OnDestroy` clears it.
- **Laser damage is instant**: `Physics2D.OverlapBoxAll` hits all enemies in box once on fire. Does not persist.
- **Tradeoff augments**: `TargetMode.Highest/Lowest` picks at selection time based on current inventory state.
- **StageSelectUI debugMode**: when true, starts lobby showing the last stage. Set to false before shipping.
- **Input system**: uses `UnityEngine.InputSystem` (new Input System) — NOT legacy `UnityEngine.Input`.
