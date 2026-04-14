# Plan: Rudimentary Shop & Currency

## Overview

A lobby-accessible shop where players spend earned coins on augments. All shop items are visible in a paginated 3-card carousel. Purchased augments are added to the level-up draw pool in gameplay (meta-progression). Currency and purchase state persist across game sessions via PlayerPrefs.

---

## Files to Create

| File | Purpose |
|------|---------|
| `Assets/Scripts/CurrencyManager.cs` | Static utility class — PlayerPrefs-backed coin storage |
| `Assets/Scripts/ShopUI.cs` | Shop panel — carousel, card building, purchase logic |

## Files to Modify

| File | Change |
|------|--------|
| `Assets/Scripts/StageData.cs` | Add `coinReward` int field |
| `Assets/Scripts/Editor/StageDataEditor.cs` | Expose `coinReward` in custom Inspector under Rewards header |
| `Assets/Scripts/ResultsScreen.cs` | Call `CurrencyManager.AddCoins(coinReward)` on win |
| `Assets/Scripts/AugmentPool.cs` | Add `AddToPool(AugmentData)` method; inject shop purchases in `Start()` |
| `Assets/Scripts/SceneFlowManager.cs` | Add `purchasedShopAugments` list; AugmentPool reads it on Start |
| LobbyScene (Editor only) | Add Shop button + ShopPanel + coin tracker label to Canvas |

---

## Architecture Notes

### Why SceneFlowManager carries purchases (not just PlayerPrefs)
`AugmentPool` lives in `Andrew_Scene` and rebuilds `availablePool` from `allAugments` every `Start()`. Shop purchases need to survive scene transitions. `SceneFlowManager` is already `DontDestroyOnLoad` and is the established cross-scene data carrier — purchases are stored there as a runtime list. PlayerPrefs backs that list so it survives full game restarts.

### Purchase flow across scenes
1. Player buys item in LobbyScene → `ShopUI` calls `SceneFlowManager.Instance.AddShopPurchase(augment)` and saves to PlayerPrefs
2. Player enters stage → `AugmentPool.Start()` calls `SceneFlowManager.Instance.GetShopPurchases()` and adds each to `availablePool`
3. First level-up filter (`firstLevelUp == true`) looks for `AugmentType.NewWeapon` in `availablePool` — purchased weapon augments ARE in the pool at this point, so they appear automatically ✓

---

## 1. CurrencyManager

Static utility, no MonoBehaviour. Same pattern as `StageProgressData`.

```csharp
public static class CurrencyManager
{
    const string KEY = "player_coins";

    public static int GetCoins() => PlayerPrefs.GetInt(KEY, 0);

    public static void AddCoins(int amount)
    {
        PlayerPrefs.SetInt(KEY, GetCoins() + amount);
        PlayerPrefs.Save();
    }

    // Returns false if insufficient — caller should check
    public static bool SpendCoins(int amount)
    {
        int current = GetCoins();
        if (current < amount) return false;
        PlayerPrefs.SetInt(KEY, current - amount);
        PlayerPrefs.Save();
        return true;
    }

    public static void Reset()   // debug / wipe
    {
        PlayerPrefs.DeleteKey(KEY);
        PlayerPrefs.Save();
    }
}
```

**Coin earning**: Players earn coins by completing stages. Each `StageData` asset has a `coinReward` field set in the editor. `ResultsScreen` calls `CurrencyManager.AddCoins(coinReward)` on win. For testing, a debug key adds coins in the lobby (see §7).

---

## 2. StageData & ResultsScreen Changes

### StageData.cs

Add under the `[Header("Difficulty")]` block (or a new `[Header("Rewards")]`):

```csharp
[Header("Rewards")]
public int coinReward = 10;
```

Default of `10` is reasonable — tune per stage in the Inspector.

### StageDataEditor.cs

In `OnInspectorGUI()`, after the Difficulty section, add:

```csharp
EditorGUILayout.Space(10);
EditorGUILayout.LabelField("Rewards", EditorStyles.boldLabel);
EditorGUILayout.PropertyField(serializedObject.FindProperty("coinReward"));
```

### ResultsScreen.cs

In the win branch (where stars and kill count are already shown), add one line after saving stage progress:

```csharp
// Grant coin reward
StageData stage = SceneFlowManager.Instance != null ? SceneFlowManager.Instance.SelectedStage : null;
if (stage != null)
    CurrencyManager.AddCoins(stage.coinReward);
```

> **Note**: Read `ResultsScreen.cs` before implementing — find the exact win handler method and insert there. Do not call `AddCoins` on retry/loss.

---

## 3. Lobby Coin Tracker

A persistent TextMeshPro label in the **top-right** of the LobbyScene Canvas showing the player's current coin balance.

### Editor Setup

1. In LobbyScene Canvas, create a **TextMeshPro - Text (UI)** GameObject named `CoinTracker`
2. **RectTransform**: anchor **top-right**, position `(-20, -20)` from corner, size `~160 × 40`
3. **Text alignment**: right-aligned
4. **Font size**: 22, bold
5. **Color**: `(1.0, 0.85, 0.3)` — gold, matches the game's accent color
6. **Initial text**: `"⬡ 0"` or `"Coins: 0"` — update at runtime

### Script

Add a tiny `CoinTrackerUI` MonoBehaviour (or handle in `StageSelectUI`):

```csharp
public class CoinTrackerUI : MonoBehaviour
{
    public TextMeshProUGUI label;

    void OnEnable() => Refresh();

    void Refresh() => label.text = $"⬡ {CurrencyManager.GetCoins()}";
}
```

`OnEnable` fires every time the lobby is entered (scene load), so the display is always current. `ShopUI` should also call `label.text = ...` (or `CoinTrackerUI.Refresh()`) after each purchase so it updates while the shop is open.

> **Wiring**: Drag the `CoinTracker` TextMeshPro into the `label` field on `CoinTrackerUI`. Alternatively, expose a static `Refresh()` on `CoinTrackerUI` and call it from `ShopUI.TryPurchase()`.

---

## 5. ShopItem (Serializable Class)

Defined at the top of `ShopUI.cs` (or its own file). The `purchased` field is runtime state only — persistence uses PlayerPrefs keyed by augment asset name.

```csharp
[System.Serializable]
public class ShopItem
{
    public AugmentData augment;
    public int price;
    [HideInInspector] public bool purchased;   // set at runtime from PlayerPrefs
}
```

---

## 6. SceneFlowManager Changes

Add to the existing singleton (after the `stageVariants` block):

```csharp
// ── Shop purchases ────────────────────────────────────────────────────────
private List<AugmentData> _shopPurchases = new List<AugmentData>();

public void AddShopPurchase(AugmentData augment)
{
    if (!_shopPurchases.Contains(augment))
        _shopPurchases.Add(augment);
}

public List<AugmentData> GetShopPurchases() => _shopPurchases;

// Called once at startup from ShopUI to restore purchases saved in PlayerPrefs.
// ShopUI passes every AugmentData it knows about so we can match by name.
public void RestoreShopPurchases(List<ShopItem> allItems)
{
    foreach (var item in allItems)
    {
        if (item.augment == null) continue;
        if (PlayerPrefs.GetInt("shop_" + item.augment.augmentName, 0) == 1)
        {
            item.purchased = true;
            AddShopPurchase(item.augment);
        }
    }
}
```

---

## 7. AugmentPool Changes

### Add `AddToPool` method

```csharp
public void AddToPool(AugmentData augment)
{
    if (augment != null && !availablePool.Contains(augment))
        availablePool.Add(augment);
}
```

### Inject shop purchases in `Start()`

In `AugmentPool.Start()`, after `availablePool = new List<AugmentData>(allAugments);`:

```csharp
if (SceneFlowManager.Instance != null)
{
    foreach (var aug in SceneFlowManager.Instance.GetShopPurchases())
        AddToPool(aug);
}
```

**First-level-up behavior**: `GetCards()` already filters for `AugmentType.NewWeapon` on the first call. Purchased weapon augments injected via `AddToPool` before that call will appear in the first level-up draw — no additional changes needed.

---

## 8. ShopUI

### Inspector Fields

```
[Header("Shop Items")]
List<ShopItem> shopItems          // drag AugmentData assets + set price per item

[Header("Navigation")]
Button prevButton                 // left red triangle (duplicate from StageSelectUI)
Button nextButton                 // right red triangle
int cardsPerPage = 3              // shown at once

[Header("References")]
TextMeshProUGUI coinDisplay       // "Coins: 42" — top of panel
GameObject dimOverlay             // semi-transparent full-screen bg behind panel
```

### Card Colors

The key difference from `AugmentUI`: the card BACKGROUND is the highlight color (not grey), and hover dims it slightly.

| Condition | Background | Cost text |
|-----------|-----------|-----------|
| Affordable — `NewWeapon` | `(1.00, 0.85, 0.30)` yellow | white |
| Affordable — `ModifyHealth` | `(1.00, 0.30, 0.30)` red | white |
| Affordable — all others | `(0.30, 0.75, 1.00)` blue | white |
| **Unaffordable** (any type) | `(0.12, 0.12, 0.16)` grey | **red** — unclickable |

### Hover Effect (Affordable Cards Only)

Add a child Image on each card, full-card size, color `(0, 0, 0, 0)`. The card script implements `IPointerEnterHandler` / `IPointerExitHandler`: on enter set color to `(0, 0, 0, 0.25)` (dark overlay dimming the background); on exit set back to transparent. Unaffordable cards skip this entirely and have no `Button` component wired.

### ShopCard script (inner component on each card)

```csharp
public class ShopCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public ShopItem item;
    public ShopUI owner;
    public Image hoverOverlay;      // full-size child Image (alpha 0 normally)
    public Button button;

    public void OnPointerEnter(PointerEventData _)
    {
        if (item == null || !CanAfford()) return;
        hoverOverlay.color = new Color(0, 0, 0, 0.25f);
    }
    public void OnPointerExit(PointerEventData _)  => hoverOverlay.color = Color.clear;
    public void OnClick()                          => owner.TryPurchase(item);
    bool CanAfford() => CurrencyManager.GetCoins() >= item.price;
}
```

### Key Methods on ShopUI

```csharp
public void Open()
// Restore purchases from SceneFlowManager, rebuild page, refresh coins, show panel

public void Close()
// Hide panel (CanvasGroup alpha = 0, blocksRaycasts = false)

void ShowPage(int page)
// Clear current cards, build cards for items on this page (skip purchased items),
// update arrows (grey out at first/last page), update coin display

public void TryPurchase(ShopItem item)
// Safety check CurrencyManager.SpendCoins(item.price)
// Mark item.purchased = true, save to PlayerPrefs ("shop_" + augment.augmentName)
// SceneFlowManager.Instance.AddShopPurchase(item.augment)
// Rebuild current page (card disappears)
// Refresh coin display
```

### Carousel Pagination

- Unpurchased items form the visible list; purchased items are filtered out
- Page = window of `cardsPerPage` into that list
- Left/right arrows step by one page
- If the current page becomes empty after a purchase, step back one page (or show "Shop empty" text)

---

## 9. Lobby Button & Panel — Editor Setup

### Shop Button

1. In LobbyScene, select the **Canvas** GameObject
2. Right-click → **UI → Button - TextMeshPro**, name it `ShopButton`
3. **RectTransform**: anchor **bottom-right**, position ~`(-90, 60)` from corner, size `~120 × 45`
4. **Button Image color**: `(1.0, 0.85, 0.3, 1)` — matches the gold/yellow accent used throughout the game
5. Child **Text**: `"Shop"`, font size 18, bold, black — good contrast on yellow
6. **OnClick()**: wire to `ShopPanel`'s `ShopUI.Open()` method

> Placement rationale: bottom-right avoids overlapping the stage selection cards (center/left). Same side as where the concept art viewer button lives, keeping secondary actions in one zone.

### ShopPanel GameObject

1. Under Canvas, create empty GameObject named `ShopPanel`
2. Add `ShopUI` component
3. Add `CanvasGroup` component (alpha/interactable/blocksRaycasts for show/hide)
4. Add child **Background** Image — full-screen, color `(0.05, 0.05, 0.08, 0.93)` dark overlay
5. Add child **CardContainer** — empty transform, centered, where ShopUI builds cards in code
6. Add child **PrevButton** and **NextButton** — duplicate the red triangle arrows from `StageSelectUI`, placed left/right of CardContainer
7. Add child **CoinDisplay** TextMeshPro — top of panel, e.g. `"Coins: 0"`
8. Add child **CloseButton** — top-right, text `"✕"` or `"Back"`, calls `ShopUI.Close()`
9. Wire all references in ShopUI Inspector fields
10. Start panel hidden: set CanvasGroup `alpha = 0`, `blocksRaycasts = false`

---

## 10. Debug Key

In `StageSelectUI.Update()` (lobby scene, always active), add:

```csharp
// Debug: 6 adds 10 coins
if (Keyboard.current.digit6Key.wasPressedThisFrame)
{
    CurrencyManager.AddCoins(10);
    CoinTrackerUI.Instance?.Refresh();  // or however you expose refresh
}
```

Remove before shipping.

---

## 11. Verification Checklist

- [ ] Each StageData asset has a `coinReward` field visible in Inspector under Rewards
- [ ] Completing a stage grants the correct coin amount (check PlayerPrefs or tracker label)
- [ ] Coin tracker label visible top-right in lobby, updates on scene load and after purchases
- [ ] Debug key `6` grants 10 coins in lobby; label updates immediately
- [ ] Shop button visible in lobby, styled yellow/gold, positioned bottom-right
- [ ] Clicking Shop opens panel; Close/Back button closes it
- [ ] All unpurchased shop items show in carousel (not random)
- [ ] Affordable cards: colored background matching type, white cost, hover dims card
- [ ] Unaffordable cards: grey background, red cost, not clickable
- [ ] Purchasing: card disappears from carousel, coin total decreases, tracker updates
- [ ] Purchased state survives re-entering lobby (PlayerPrefs persistence)
- [ ] Enter a stage → first level-up shows purchased weapon augments in draw pool
- [ ] `AugmentPool.allAugments` list in Inspector does NOT need the shop augment — it only enters the pool after purchase

---

## Open Questions

- **Wipe progress**: Should `O` (lobby wipe key) also wipe coins and shop purchases? Probably yes — add `CurrencyManager.Reset()` and loop through shop items resetting their PlayerPrefs keys.
- **Shop augments in `allAugments`**: Augments sold in the shop should NOT be in `AugmentPool.allAugments` in the Inspector — they start out of the pool and only enter it after purchase. Keep them as standalone ScriptableObject assets that you drag into `ShopUI.shopItems` only.
