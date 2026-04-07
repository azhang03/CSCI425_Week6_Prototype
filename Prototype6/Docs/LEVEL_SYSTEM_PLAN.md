# Level Creation & Selection System — Planning Document

> **Last Updated:** April 2026
> **Status:** Planning — not yet implemented
> **Scope:** Level creation tooling, stage selection UI, win/loss detection, star achievements, persistent progress

---

## Overview

This doc covers the **level system** — the only system we're building right now. Everything else from the earlier design brainstorms (currency, permanent upgrades, weapon loadout, augment deck curation) is **backlogged** at the bottom of this doc.

The goal: a level designer can create stages entirely through the Unity Editor (no code), players pick a stage in a lobby, play it, win or lose, see a star rating, and move on. Progress persists between sessions.

**Who's working on this:**
- One developer implementing the level system (scripts, UI, scene flow)
- One partner creating enemy prefabs separately — their workflow is unaffected (see [Enemy Partner Integration](#enemy-partner-integration))

---

## Level Creation System

### The Core Idea

Each level (stage) is a **StageData ScriptableObject** — a data container that a level designer fills in the Unity Editor Inspector. No code needed per stage. The existing `EnemySpawner` gets a new `Configure(StageData)` method that overwrites its fields from the StageData asset at runtime. The spawner's existing logic (wave timing, weighted enemy selection, HP scaling) runs exactly as before — it just starts with different data.

### StageData Fields

To create a new stage: **Project window > right-click > Create > Game > StageData**

| Inspector Field | Type | Purpose | Example |
|----------------|------|---------|---------|
| `stageName` | string | Display name shown in UI | "The Swarm" |
| `stageNumber` | int | Determines ordering in the stage list. Stage 1, 2, 3... | 1 |
| `waveCount` | int | How many waves the player must survive to win | 4 |
| `spawnIntervals` | float[] | Spawn rate per wave (seconds between spawns). Array length should match waveCount | [4, 3, 2, 1] |
| `enemiesPerWave` | float[] | How many enemies spawn per wave. Array length should match waveCount | [3, 5, 8, 15] |
| `enemyRoster` | List\<EnemySpawner.EnemyType\> | Which enemy prefabs + spawn weights. Higher weight = more common | [DefaultEnemy w:1.0, SpiralEnemy w:0.4] |
| `enemyHPBonus` | int | Flat HP added to ALL enemies in this stage (on top of base prefab HP) | 0 for easy, 3 for hard |
| `bonusHPPerCycle` | int | If waves repeat (future), HP added per cycle | 2 |
| `bonusEnemiesPerWave` | int | If waves repeat, extra enemies per wave per cycle | 2 |
| `spawnRandomness` | float | Random variance on spawn timing | 0.5 |

The `enemyRoster` field reuses the existing `EnemySpawner.EnemyType` inner class, which has:
- `name` (string) — label for the Inspector
- `prefab` (GameObject) — drag an enemy prefab here
- `spawnWeight` (float) — higher = more likely to spawn

**Ordering:** `stageNumber` determines the order stages appear in the selection UI and the unlock chain. Stage 1 is always unlocked. Stage N unlocks when stage N-1 is cleared.

### How Waves Work (Current Behavior)

Waves are **count-based, not time-based**. Each wave defines a number of enemies to spawn and an interval between spawns. Once the last enemy in a wave spawns, the next wave starts immediately — it does NOT wait for existing enemies to die. Enemies from earlier waves can still be alive when later waves begin, which creates natural pressure buildup.

**Enemy types are roster-wide, not per-wave.** The `enemyRoster` is a single weighted random pool shared across all waves. Every individual spawn rolls from the full roster. There is no way to say "wave 1 = only DefaultEnemies, wave 3 = introduce TeleportEnemies" — every wave draws from the same pool.

Example: a stage with `spawnIntervals = [4, 2]`, `enemiesPerWave = [3, 5]`, and roster `[Default w:0.6, Spiral w:0.4]`:
- Wave 1: spawns 3 enemies, one every 4 seconds (any type from roster)
- Wave 2 starts immediately after the 3rd spawn: spawns 5 enemies, one every 2 seconds
- After the 5th spawn in wave 2, the stage is complete

This behavior is inherited from the prototype's `EnemySpawner` and works fine for now. Per-wave roster overrides or "kill all before next wave" gating can be added later if needed.

### How It Feeds Into EnemySpawner

The new `Configure(StageData)` method on `EnemySpawner` does this:

1. Overwrites `numStages` with `stageData.waveCount`
2. Overwrites `stageIntervals` with `stageData.spawnIntervals`
3. Overwrites `enemiesPerStage` with `stageData.enemiesPerWave`
4. Overwrites `enemyTypes` with `stageData.enemyRoster`
5. Recalculates `totalWeight` from the new roster
6. Sets `bonusHPPerCycle`, `bonusEnemiesPerStage`, `spawnRandmoness` (note: typo in original code, keep it for compatibility)
7. Sets a new `baseHPBonus` field from `stageData.enemyHPBonus`
8. Resets internal counters (`stageCounter`, `spawnCounter`, `cycleCount`)
9. Sets `finiteMode = true` — this is a new flag that prevents the spawner from looping back to wave 1 after all waves complete (the prototype's current behavior). Instead, it fires an `OnAllWavesComplete` event and stops spawning.

The existing `SpawnEnemy()` line that applies HP bonus changes from:
```
enemyComponent.maxHitPoints += cycleCount * bonusHPPerCycle;
```
to:
```
enemyComponent.maxHitPoints += baseHPBonus + cycleCount * bonusHPPerCycle;
```

If `Configure()` is never called, `finiteMode` stays false and the spawner loops forever — identical to the prototype. Zero risk of breaking the existing game.

---

## Level Selection UI

### Layout

The lobby has a single stage card in the center with left/right navigation:

```
┌───────────────────────────────────────────────────────────┐
│                                                           │
│                                                           │
│       ◀          ┌────────────────────┐          ▶        │
│     (left        │    STAGE 1         │       (right      │
│      arrow)      │   "The Swarm"      │        arrow)     │
│                  │                    │                   │
│                  │    ★  ★  ☆         │          🔒       │
│                  │                    │       (lock if    │
│                  │     [ PLAY ]       │        next stage │
│                  └────────────────────┘        is locked) │
│                                                           │
└───────────────────────────────────────────────────────────┘
```

### Stage Card (Center, ~350x400)

- **Stage number + name** at the top (e.g., "STAGE 1 — The Swarm")
- **3 star slots** in a row:
  - Earned stars: gold filled star (★ U+2605, gold color)
  - Empty stars: dark outline star (☆ U+2606, dark grey)
  - Filled left-to-right: if 2 stars, shows ★★☆
- **PLAY button** at the bottom of the card

### Navigation Arrows

- **Left triangle** (◀) and **right triangle** (▶) positioned to the left and right of the center card
- Both arrows cycle through **unlocked stages only**
- **Right arrow:** advances to the next unlocked stage. If currently on the last unlocked stage, wraps to Stage 1
- **Left arrow:** goes to the previous unlocked stage. If currently on Stage 1, wraps to the last unlocked stage
- **Lock indicator:** a small lock icon appears next to the right arrow when the *next sequential* stage is locked (visual cue that there's more to unlock). The arrow itself still works — it wraps to Stage 1
- If only 1 stage is unlocked, arrows are hidden or visually disabled

### Build Pattern

UI is built entirely in code (matching the existing `PauseMenu.cs` and `DeathScreen.cs` pattern — `new GameObject()`, `AddComponent<Image>()`, `AddComponent<TextMeshProUGUI>()`, etc.). No prefab needed.

---

## Win/Loss + Star System

### StageManager

A new singleton (`StageManager.Instance`) that lives on the `GameManager` GameObject in the gameplay scene. It orchestrates a stage from start to finish:

1. On `Start()`: reads current StageData (from `SceneFlowManager.Instance.SelectedStage`, falling back to a serialized field for testing). Calls `EnemySpawner.Instance.Configure(stageData)`.
2. Subscribes to `EnemySpawner.Instance.OnAllWavesComplete` → **win**
3. Subscribes to `PlayerHealth.Instance.OnPlayerDied` → **loss**
4. On either event: pauses game (`Time.timeScale = 0`), calculates star rating, fires `OnStageEnded(StageResult, int stars)` event

Uses the lazy-subscribe polling pattern from `DeathScreen.cs` (try subscribing in `Update()` until Instance is available) to handle script execution order safely.

### Star Rating

Stars = **hearts remaining at the moment of winning**.

| Hearts at Win | Stars |
|--------------|-------|
| 3 | ★★★ |
| 2 | ★★☆ |
| 1 | ★☆☆ |
| 0 (died) | ☆☆☆ (loss, 0 stars) |

Stars are **achievement only** — they do NOT gate progression. Clearing a stage (regardless of stars) unlocks the next one. Stars are saved as best-ever and never downgraded by replaying a stage with fewer hearts.

---

## Persistence

### PlayerPrefs (Simple Key-Value)

No full save system for now. Stage progress is stored in `PlayerPrefs`:

| Key | Value | Notes |
|-----|-------|-------|
| `Stage_{N}_cleared` | 1 or 0 | Whether stage N has been cleared |
| `Stage_{N}_stars` | 0–3 | Best star rating achieved on stage N |

### Static Utility: StageProgressData

A static helper class with no MonoBehaviour:

| Method | What It Does |
|--------|-------------|
| `bool IsCleared(int stageNumber)` | Returns true if stage has been cleared |
| `int GetStars(int stageNumber)` | Returns best star rating (0–3) |
| `bool IsUnlocked(int stageNumber)` | Stage 1 = always true. Stage N = IsCleared(N-1) |
| `void SaveResult(int stageNumber, int stars)` | Sets cleared=1, stars=Max(existing, new). Calls PlayerPrefs.Save() |
| `void ClearAllProgress()` | Debug: wipes all progress |

---

## Scene Flow

### Two Scenes

| Scene | Purpose |
|-------|---------|
| `LobbyScene` (new) | Stage selection UI. Contains Canvas with StageSelectUI, SceneFlowManager (DontDestroyOnLoad), EventSystem, Camera |
| `Andrew_Scene` (existing, becomes gameplay scene) | Everything the prototype already has, plus StageManager on GameManager and ResultsScreen on Canvas |

Both scenes added to **Build Settings** (File > Build Settings > Add Open Scenes).

### SceneFlowManager

A **DontDestroyOnLoad** singleton that survives scene transitions. Carries the selected StageData between scenes.

| Field/Method | Purpose |
|-------------|---------|
| `StageData SelectedStage` | Set by StageSelectUI before loading gameplay scene |
| `List<StageData> allStages` | Master ordered list of all stages, populated in Inspector |
| `GoToStage(StageData)` | Sets SelectedStage, loads gameplay scene |
| `GoToLobby()` | Resets timeScale, loads lobby scene |
| `RetryCurrentStage()` | Reloads gameplay scene (SelectedStage unchanged) |
| `GetNextStage()` | Returns next StageData by stageNumber, or null |

Lives on a persistent GameObject in the lobby scene. On first creation, calls `DontDestroyOnLoad`. The standard Awake destroy-duplicate guard prevents duplicates when the lobby scene reloads.

### ResultsScreen (Replaces DeathScreen)

Built in code on the gameplay Canvas. Subscribes to `StageManager.Instance.OnStageEnded`. Handles both win and loss:

**Win:**
```
         STAGE CLEAR!
           ★ ★ ☆
          Kills: 24
  [Next Stage]    [Lobby]
```

**Loss:**
```
          GAME OVER
          Kills: 12
    [Retry]    [Lobby]
```

- On win: calls `StageProgressData.SaveResult(stageNumber, stars)` before showing
- "Next Stage" → `SceneFlowManager.Instance.GoToStage(nextStage)` (hidden if no next stage exists)
- "Retry" → `SceneFlowManager.Instance.RetryCurrentStage()`
- "Lobby" → `SceneFlowManager.Instance.GoToLobby()`

The existing `DeathScreen` component is disabled/removed from the gameplay scene Canvas. The `.cs` file is kept (no broken references in other branches).

### Other Modifications

- **PauseMenu.cs**: Add a "Return to Lobby" button → `SceneFlowManager.Instance.GoToLobby()`
- **GameManager.cs**: R-key routes through `SceneFlowManager.Instance.RetryCurrentStage()` if available, else falls back to existing reload behavior

---

## Existing Tools — Editor Reference

Components already in the project that the level system interacts with. Written for anyone working in the Inspector.

### EnemySpawner (Component)

**Lives on:** The `EnemySpawner` GameObject (scene root)

**What it does:** Controls when and where enemies spawn. Has an internal wave/cycle system that escalates difficulty over time.

| Inspector Field | What It Does |
|----------------|-------------|
| `Audio Manager` | Reference to the AudioManager for spawn SFX |
| `Stage Tilemap` | Reference to the Stage tilemap (used to calculate spawn radius) |
| `Rotate Parent` | Reference to the Rotate transform (spawned enemies become children of this) |
| `Spawn Interval` | Base seconds between spawns (gets overridden by stage intervals) |
| `Num Stages` | Number of waves per cycle (default 4) |
| `Spawn Randomness` | Random variance added to spawn timing |
| `Stage Intervals` | Array of spawn intervals per wave (e.g., [5, 3, 2, 1]) |
| `Enemies Per Stage` | Array of enemy counts per wave (e.g., [2, 5, 8, 15]) |
| `Bonus HP Per Cycle` | HP added to all enemies each time the full cycle repeats |
| `Bonus Enemies Per Stage` | Extra enemies added per wave each cycle |
| `Enemy Types` | List of enemy prefabs with spawn weights (higher weight = more common) |

**Recent change:** The partner added a null check on `audioManager.PlayEnemySpawn()` in `SpawnEnemy()`, so the spawner no longer crashes if AudioManager is missing (e.g., in test scenes). `Shooting.cs` got the same null check on `audioManager.PlayBullet()`.

**For the level system:** The new `Configure(StageData)` method overwrites these fields from a StageData asset at runtime. The spawner's existing logic is unchanged — it just runs with different starting data.

### PlayerHealth (Component)

**Lives on:** The `Player` GameObject (child of Rotate)

| Inspector Field | What It Does |
|----------------|-------------|
| `Max Hearts` | Starting health (default 3) |
| `I-Frame Duration` | Seconds of invincibility after taking damage (default 1) |
| `Flash Interval` | How fast the sprite blinks during invincibility (default 0.1s) |

**For the level system:** `CurrentHearts` at win determines star rating. `OnPlayerDied` event triggers loss detection in StageManager.

### Enemy Prefab Variants

**Where they live:** `Assets/Prefab/Enemies/`

All enemy variants share the base **Enemy** component (maxHitPoints, xpValue, hit flash) plus a movement script:

| Variant | Movement Script | Key Fields |
|---------|----------------|------------|
| DefaultEnemy | `DefaultMove` | `moveSpeed` (default 2) — walks straight toward player |
| SpiralEnemy | `SpiralMove` | `moveSpeed` (3), `spiralStrength` (15), `spiralTightness` (1.5) — spirals inward |
| TeleportEnemy | `TeleportMove` | `teleportInterval` (2s), `shrinkFactor` (0.75), `fadeDuration` (0.4s) — blinks closer |
| ShieldEnemy | `Shield` (child object) | `distanceFromEnemy` (1.5) — has a shield child that orbits between the enemy and the player, blocking projectiles from the front |

**For the level system:** These prefabs get dragged into StageData assets' `enemyRoster` field with a spawn weight. New enemy types created by the enemy partner follow the same pattern and work automatically.

### Obstacles

**Prefab:** `Assets/Prefab/Obstacle.prefab`
**Script:** `Obstacle.cs` (empty placeholder — no logic yet)
**Tag:** "Obstacle" (added to TagManager)

Obstacles are static objects placed in the arena that block projectiles. Projectiles (`Projectile.cs`) destroy themselves on contact with anything tagged "Obstacle" via `OnTriggerEnter2D`. The partner's `Bullet.cs` has commented-out code for the same behavior (not active yet).

Obstacles are **not spawner-driven** — they're placed directly in the scene, presumably as children of the Rotate transform so they rotate with the arena. Different stages might want different obstacle layouts. Our current StageData doesn't define obstacle placement, so for now obstacles would need to be part of the scene itself or handled separately. This is something to revisit if we want per-stage obstacle configurations.

**For the level system:** Obstacles don't affect the stage system implementation. They're scene objects that exist independently of the spawner. If we later want per-stage obstacle layouts, we'd either use obstacle prefab references in StageData or use separate scene sections/prefabs per stage.

---

## Enemy Partner Integration

The enemy partner's workflow is **completely unaffected**. Here's what they do and don't need to know:

**Their workflow (unchanged):**
1. Create a new movement script (e.g., `ShieldMove.cs`)
2. Duplicate the base `Enemy.prefab`, add the movement script
3. Put the new prefab in `Assets/Prefab/Enemies/`

**What happens with their prefabs:**
- The level designer drags their prefabs into `StageData.enemyRoster` in the Inspector
- The EnemySpawner uses the roster at runtime — no code changes needed on the enemy partner's end

**The only thing they might notice:** If they look at `EnemySpawner.cs`, it has a few new fields and a new method (`Configure`, `baseHPBonus`, `finiteMode`, `OnAllWavesComplete`). These are purely additive — nothing they wrote is changed.

---

## Phased Implementation Plan

### Phase 1: StageData + EnemySpawner.Configure()

**Goal:** Make the spawner data-driven with a "stage complete" signal. Existing game still works unchanged if no StageData is provided.

**Create:**
- `Assets/Scripts/StageData.cs` — ScriptableObject with all fields from the [Level Creation System](#stagedata-fields) section above. Use `[CreateAssetMenu(fileName = "NewStage", menuName = "Game/StageData")]`.

**Modify: `Assets/Scripts/EnemySpawner.cs`**
- Add field: `public int baseHPBonus = 0;`
- Add field: `public bool finiteMode = false;`
- Add property: `public bool IsComplete { get; private set; }`
- Add event: `public event System.Action OnAllWavesComplete;`
- Add method: `public void Configure(StageData data)` — overwrites: `numStages`, `stageIntervals`, `enemiesPerStage`, `enemyTypes`, `totalWeight` (recalc), `bonusHPPerCycle`, `bonusEnemiesPerStage`, `spawnRandmoness`, `baseHPBonus`. Sets `finiteMode = true`. Resets `stageCounter = 0`, `spawnCounter = 0`, `cycleCount = 0`, `IsComplete = false`.
- In `Update()`: add `if (IsComplete) return;` guard at top
- In `SpawnEnemy()`: change HP line to `baseHPBonus + cycleCount * bonusHPPerCycle`
- In `UpdateStage()`: when `stageCounter >= numStages` and `finiteMode == true` → set `IsComplete = true`, fire `OnAllWavesComplete?.Invoke()`, return early (don't loop)

**Test:** Create a test StageData asset (2 waves, 3 enemies, DefaultEnemy). Temporarily reference it in EnemySpawner to test Configure(). Play → verify config applies → all waves complete → event fires (log it). Remove test reference after.

**Dependencies:** None. This is the starting point.

---

### Phase 2: StageManager (Win/Loss + Stars)

**Goal:** Orchestrate a stage run. Detect win and loss. Calculate star rating.

**Create: `Assets/Scripts/StageManager.cs`**
- Singleton pattern (same as all existing singletons: `public static StageManager Instance`, set in `Awake()`, destroy-duplicate guard)
- Lives on: `GameManager` GameObject in gameplay scene
- Field: `public StageData currentStage;` (serialized for testing, overridden by SceneFlowManager later)
- Enum: `public enum StageResult { None, Win, Loss }`
- Properties: `public StageResult Result`, `public int StarsEarned`
- Event: `public event Action<StageResult, int> OnStageEnded;` (result, stars)
- `Start()` or lazy-subscribe in `Update()` (matching DeathScreen pattern):
  - If `currentStage != null`: call `EnemySpawner.Instance.Configure(currentStage)`
  - Subscribe to `EnemySpawner.Instance.OnAllWavesComplete` → `HandleWin()`
  - Subscribe to `PlayerHealth.Instance.OnPlayerDied` → `HandleLoss()`
- `HandleWin()`: `StarsEarned = Mathf.Clamp(PlayerHealth.Instance.CurrentHearts, 0, 3)`. `Time.timeScale = 0f`. Fire `OnStageEnded(Win, StarsEarned)`.
- `HandleLoss()`: `StarsEarned = 0`. `Time.timeScale = 0f`. Fire `OnStageEnded(Loss, 0)`.

**Test:** Add StageManager to GameManager in Andrew_Scene. Assign a StageData. Play → survive → verify win + correct stars in console. Die → verify loss fires with 0 stars.

**Dependencies:** Phase 1 (needs `Configure()` and `OnAllWavesComplete`).

---

### Phase 3: Stage Progress Persistence

**Goal:** Save/load stage clear status and best stars via PlayerPrefs.

**Create: `Assets/Scripts/StageProgressData.cs`**
- Static class, no MonoBehaviour
- Keys: `"Stage_{stageNumber}_cleared"` (int 0/1), `"Stage_{stageNumber}_stars"` (int 0-3)
- `static bool IsCleared(int stageNumber)` → `PlayerPrefs.GetInt(key, 0) == 1`
- `static int GetStars(int stageNumber)` → `PlayerPrefs.GetInt(key, 0)`
- `static bool IsUnlocked(int stageNumber)` → `stageNumber == 1 || IsCleared(stageNumber - 1)`
- `static void SaveResult(int stageNumber, int stars)` → set cleared=1, set stars=`Mathf.Max(GetStars(stageNumber), stars)` (never downgrade), `PlayerPrefs.Save()`
- `static void ClearAllProgress()` → `PlayerPrefs.DeleteAll()`, `PlayerPrefs.Save()`

**Test:** Call SaveResult/IsCleared/GetStars from a debug script. Verify stars never downgrade. Verify IsUnlocked chain works.

**Dependencies:** None.

> **Phases 2 and 3 are independent** — they don't share any files or data. Build in either order.

---

### Phase 4: SceneFlowManager + LobbyScene Skeleton

**Goal:** Two-scene flow with data carried between scenes. Minimal lobby for testing.

**Create: `Assets/Scripts/SceneFlowManager.cs`**
- Singleton + `DontDestroyOnLoad` (first DontDestroyOnLoad in the project)
- `public static SceneFlowManager Instance`
- `public StageData SelectedStage { get; private set; }`
- `public List<StageData> allStages;` — populated in Inspector, sorted by stageNumber
- Scene name constants: `const string LOBBY_SCENE = "LobbyScene"; const string GAMEPLAY_SCENE = "Andrew_Scene";`
- `GoToStage(StageData stage)`: sets `SelectedStage = stage`, calls `SceneManager.LoadScene(GAMEPLAY_SCENE)`
- `GoToLobby()`: `Time.timeScale = 1f`, calls `SceneManager.LoadScene(LOBBY_SCENE)`
- `RetryCurrentStage()`: `Time.timeScale = 1f`, calls `SceneManager.LoadScene(GAMEPLAY_SCENE)` (SelectedStage unchanged)
- `GetNextStage()`: find current stage index in allStages, return next or null

**Create: `Assets/Scenes/LobbyScene.unity`**
- Camera, Canvas, EventSystem
- SceneFlowManager GameObject (DontDestroyOnLoad) with allStages populated
- Temporary "Play Stage 1" button for testing

**Modify: `Assets/Scripts/StageManager.cs`** (small delta from Phase 2)
- In Start(), before calling Configure: `if (SceneFlowManager.Instance != null && SceneFlowManager.Instance.SelectedStage != null) currentStage = SceneFlowManager.Instance.SelectedStage;` — falls back to serialized field for testing

**Add both scenes to Build Settings.**

**Test:** Start from LobbyScene → click button → gameplay loads with correct StageData → survive → verify win detection still works. Add temp debug key for `GoToLobby()` to test return.

**Dependencies:** Phases 1, 2, 3 (needs StageManager to read SelectedStage, needs StageProgressData for lobby UI later).

---

### Phase 5: StageSelectUI (Full Lobby UI)

**Goal:** Build the stage selection card + arrow navigation described in [Level Selection UI](#level-selection-ui).

**Create: `Assets/Scripts/StageSelectUI.cs`**
- MonoBehaviour on a Canvas GameObject in LobbyScene
- `BuildUI()` in `Start()` — constructs everything in code (matching PauseMenu/DeathScreen pattern)
- State: `List<StageData> allStages` (from `SceneFlowManager.Instance.allStages`, sorted by stageNumber), `int currentIndex = 0`
- `NavigateRight()`: find next unlocked index wrapping (if at last unlocked, go to index 0). Update display.
- `NavigateLeft()`: find previous unlocked index wrapping (if at index 0, go to last unlocked). Update display.
- `RefreshDisplay()`: update stage name text, update 3 star slots from `StageProgressData.GetStars()`, update lock indicator (visible when next sequential stage is locked), update arrow visibility (hide if only 1 unlocked stage)
- `OnPlayClicked()`: `SceneFlowManager.Instance.GoToStage(allStages[currentIndex])`
- Stars: use TextMeshProUGUI with `★` (U+2605, gold Color) for earned, `☆` (U+2606, dark grey) for empty
- Card: Image component with dark background, Outline component for border
- Arrows: TextMeshProUGUI `◀` and `▶` on Button components
- Lock: TextMeshProUGUI `🔒` next to right arrow, shown/hidden via SetActive

**Replace the temporary "Play Stage 1" button from Phase 4 with this.**

**Test:** Clear all progress → only Stage 1 navigable. Manually SaveResult(1, 2) → Stage 2 navigable, Stage 1 shows 2 gold stars. Verify wrapping in both directions. Click Play → gameplay loads.

**Dependencies:** Phases 3 (StageProgressData) and 4 (SceneFlowManager + LobbyScene).

---

### Phase 6: ResultsScreen (Replaces DeathScreen)

**Goal:** Post-stage screen for win and loss. Saves progress. Provides Retry/Next/Lobby navigation.

**Create: `Assets/Scripts/ResultsScreen.cs`**
- MonoBehaviour on gameplay Canvas
- Built in code following DeathScreen pattern
- Subscribes to `StageManager.Instance.OnStageEnded` (lazy-subscribe in Update if needed)
- Uses CanvasGroup for show/hide (alpha/blocksRaycasts/interactable), same as A_AugmentUI
- `OnStageEnded(StageResult result, int stars)`:
  - If win: call `StageProgressData.SaveResult(stageNumber, stars)`, then build win UI
  - If loss: build loss UI
  - Both: `Time.timeScale = 0f` (StageManager already does this, but be safe)
- Win UI: "STAGE CLEAR!" title, star display (same 3-star pattern as lobby), kill count from `A_ScoreManager.Instance.KillCount`, "Next Stage" button (hidden if `SceneFlowManager.Instance.GetNextStage()` is null), "Lobby" button
- Loss UI: "GAME OVER" title, kill count, "Retry" button, "Lobby" button
- "Next Stage" → `SceneFlowManager.Instance.GoToStage(nextStage)`
- "Retry" → `SceneFlowManager.Instance.RetryCurrentStage()`
- "Lobby" → `SceneFlowManager.Instance.GoToLobby()`

**Disable DeathScreen:** Remove the DeathScreen component from the gameplay scene Canvas GameObject. Keep the `.cs` file in the project (don't delete it — avoids broken refs in partner branches).

**Test:** Win → stars shown and saved, Next Stage loads correct stage. Lose → Retry reloads same stage, Lobby returns. Return to lobby → stars updated.

**Dependencies:** Phases 2 (StageManager), 3 (StageProgressData), 4 (SceneFlowManager).

> **Phases 5 and 6 are independent** — StageSelectUI is lobby-side, ResultsScreen is gameplay-side. No shared files. Build in either order.

---

### Phase 7: Integration + Polish

**Goal:** Wire everything end-to-end, create real stage assets, clean up loose ends.

**Modify: `Assets/Scripts/PauseMenu.cs`**
- In `BuildUI()`: add "Return to Lobby" button → calls `Resume()` then `SceneFlowManager.Instance.GoToLobby()`. Shift existing buttons down to make room.

**Modify: `Assets/Scripts/GameManager.cs`**
- R-key handler: if `SceneFlowManager.Instance != null`, call `RetryCurrentStage()`. Else fall back to existing `scoreManager.ResetGame()`.

**Create StageData assets in `Assets/Prefab/Data/Stages/`:**

| Asset | Name | Waves | Intervals | Enemies/Wave | Roster | HP Bonus |
|-------|------|-------|-----------|-------------|--------|----------|
| Stage_1.asset | "The Swarm" | 3 | [4, 3, 2] | [3, 5, 8] | DefaultEnemy (1.0) | 0 |
| Stage_2.asset | "Spiral Assault" | 4 | [3, 2.5, 2, 1.5] | [4, 7, 10, 14] | Default (0.6), Spiral (0.4) | 1 |
| Stage_3.asset | "Teleport Chaos" | 5 | [3, 2, 1.5, 1, 0.8] | [5, 8, 12, 16, 20] | Default (0.3), Spiral (0.3), Teleport (0.4) | 3 |

**Populate `SceneFlowManager.allStages`** with all stage assets in the Inspector.

**End-to-end test checklist:**
- [ ] Launch from LobbyScene → only Stage 1 available
- [ ] Play Stage 1, win with 3 hearts → 3 stars shown, Stage 2 unlocks
- [ ] Return to lobby → Stage 1 shows 3 gold stars, can navigate to Stage 2
- [ ] Play Stage 2, win with 1 heart → 1 star
- [ ] Replay Stage 1 → 3-star record NOT downgraded
- [ ] Die during a stage → Retry reloads same stage, Lobby returns to lobby
- [ ] Pause during gameplay → "Return to Lobby" button works
- [ ] Navigate arrows: wrapping works correctly (last→first, first→last unlocked)
- [ ] Lock icon visible when next sequential stage is locked
- [ ] Close game, reopen → all progress persists (PlayerPrefs)

**Dependencies:** All previous phases.

---

## Phase Dependency Summary

```
Phase 1 ──────────────────────────────
  StageData + EnemySpawner.Configure()
  │
  ▼
Phase 2              Phase 3
  StageManager         StageProgressData
  (win/loss/stars)     (PlayerPrefs save/load)
  │                    │
  │  INDEPENDENT ←──→  │  INDEPENDENT
  │                    │
  └────────┬───────────┘
           ▼
         Phase 4
         SceneFlowManager + LobbyScene
           │
      ┌────┴────┐
      ▼         ▼
  Phase 5     Phase 6
  StageSelectUI  ResultsScreen
  (lobby)        (gameplay)
  INDEPENDENT ←→ INDEPENDENT
      │         │
      └────┬────┘
           ▼
         Phase 7
         Integration + Polish
```

- **Phases 2 & 3** are fully independent — no shared files, build in either order
- **Phases 5 & 6** are fully independent — no shared files, build in either order
- Everything else is sequential

---

## New File Summary

| File | Type | Phase |
|------|------|-------|
| `Assets/Scripts/StageData.cs` | ScriptableObject definition | 1 |
| `Assets/Scripts/StageManager.cs` | Singleton MonoBehaviour | 2 |
| `Assets/Scripts/StageProgressData.cs` | Static utility class | 3 |
| `Assets/Scripts/SceneFlowManager.cs` | DontDestroyOnLoad singleton | 4 |
| `Assets/Scripts/StageSelectUI.cs` | MonoBehaviour (lobby) | 5 |
| `Assets/Scripts/ResultsScreen.cs` | MonoBehaviour (gameplay) | 6 |
| `Assets/Scenes/LobbyScene.unity` | Scene | 4 |
| `Assets/Prefab/Data/Stages/*.asset` | StageData instances | 7 |

## Modified File Summary

| File | Change | Phase |
|------|--------|-------|
| `Assets/Scripts/EnemySpawner.cs` | +Configure(), +baseHPBonus, +finiteMode, +OnAllWavesComplete, tweak SpawnEnemy HP line | 1 |
| `Assets/Scripts/StageManager.cs` | Read SelectedStage from SceneFlowManager | 4 |
| `Assets/Scripts/PauseMenu.cs` | +"Return to Lobby" button | 7 |
| `Assets/Scripts/GameManager.cs` | R-key routes through SceneFlowManager | 7 |
| DeathScreen component | Removed from scene (file kept) | 6 |

---

## Backlog

Everything below was discussed in earlier design sessions but is **not being implemented now**. Preserved here so nothing is lost.

### Currency System
- **Decision:** Single currency (Coins/Gold/Essence) for everything
- **Earning:** Enemy kills (1-3 per kill scaling with difficulty), stage clear bonus (50-200), first-clear bonus (100-300 one-time), partial rewards on defeat (~30%)
- **Spending:** Permanent upgrades, weapon unlocks, augment unlocks
- **Why single:** Simpler mental model, no conversion confusion, can add a second currency later if needed
- **Implementation notes:** Add `currencyValue` field to Enemy.cs, create CurrencyManager singleton, integrate with ResultsScreen

### Permanent Stat Upgrades (Lobby)
- **Decision:** Tiered upgrades bought with currency, persist across all runs
- **Stats:** Max Health (+hearts), Fire Rate (-interval), Base Damage (+dmg), XP Gain (+%), Starting Chance Bonus (+%)
- **Implementation notes:** PermanentUpgradeData ScriptableObject, UpgradeShopUI in lobby, PlayerProgression script applies at scene start
- **Maps to:** PlayerHealth.maxHearts, Shooting.fireInterval, XPManager, WeaponManager

### Weapon Loadout
- **Decision:** Start with 1 weapon (Bullet). Pick 4 unlocked weapons for loadout slots. Loadout weapons appear as NewWeapon augment cards mid-run (not starting weapons)
- **Why:** Preserves mid-run "new weapon!" excitement while giving pre-run agency
- **Characters (possible):** Different characters = different starting weapon. Natural extension of loadout system

### Augment Deck Curation (Deck Building)
- **Decision:** Option 3b — unlock augments, curate 8-10 per run
- **Why not 3a (global pool):** Pool dilution punishes unlocking more augments
- **Implementation notes:** AugmentDeckUI in lobby, AugmentPool.InitializeFromDeck() method

### Save System Upgrade
- **Current:** PlayerPrefs (simple, works for level progress)
- **Future:** JSON file at Application.persistentDataPath for full save (currency, unlocks, upgrade tiers, loadout, deck). Unity's JsonUtility doesn't serialize Dictionary — use List wrapper or Newtonsoft JSON

### Future Game Ideas (Not Designed)
- Boss encounters (special final wave in certain stages)
- Arena modifiers (auto-rotation, shrinking, 8-directional bullets, cluster spawns)
- New enemy types (splitting, armored, speed-scaling)
- Prestige / New Game+ system
- Multiplayer considerations
