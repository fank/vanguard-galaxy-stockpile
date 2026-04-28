# VGStockpile decomp notes

Captured from the publicized `Assembly-CSharp.dll` stub at
`../vanguard-galaxy-tts/VGTTS/lib/Assembly-CSharp.dll` during the Task 1 spike.

## Galaxy POI enumeration

- Singleton: `Source.Galaxy.GalaxyMapData.current` (static).
- Direct enumerator: `GalaxyMapData.current.allPointsOfInterest` returns `IEnumerable<MapPointOfInterest>`.
  Filter to `SpaceStation` via `OfType<SpaceStation>()`.
- Alternative: `GalaxyMapData.current.allSystems` → walk each `SystemMapData.pointsOfInterest` and filter.
- `GalaxyMapData.GetSystem(string guid)` and `GetPointOfInterest(string guid)` are useful for direct lookups.

## Station / POI shape

- `Source.Galaxy.POI.SpaceStation` extends `Source.Galaxy.MapPointOfInterest` extends `Source.Galaxy.MapElement`.
- Fields available via inheritance from `MapElement`:
  - `string guid` — stable id (capitalisation: lowercase `guid`).
  - `string name` — display name. Backing field `_name`.
  - `SystemMapData system` — owning system.
  - `virtual Faction faction` — owning faction (may be null for unaligned POIs).
- On `SpaceStation` directly:
  - `Inventory materialStorage` — the per-station storage we need.
  - `static SpaceStation current` — currently-docked station (not relevant for galaxy-wide reads, but useful for testing in-game).

## Inventory shape

- `Source.Item.Inventory.items` returns `IEnumerable<InventoryItem>` — preferred (skips null/empty slots).
  - `Inventory.allItems` is a fixed-length `InventoryItem[]` with possible nulls.
  - `Inventory.count : int` — total item count.
  - `Inventory.GetItemsOfType(ItemCategory category)` returns a filtered `List<InventoryItem>`.
- `Inventory.InventoryItem` (nested) fields:
  - `readonly InventoryItemType item` — the type (note: field name is `item`, not `type`).
  - `int count { get; set; }` — quantity (this is an auto-property in the publicized stub).
  - `int slot { get; set; }`
  - `Inventory inventory` — parent.
  - `float spaceRequired`

## InventoryItemType (the material type)

- Type lives at `Behaviour.Item.InventoryItemType : MonoBehaviour`.
- Registry: `static Dictionary<string, InventoryItemType> allItems` (key: identifier).
  - Convenience: `static IEnumerable<InventoryItemType> all`.
- Fields:
  - `string identifier` — the stable id (e.g. "iron-ore", "titanium").
  - `ItemCategory itemCategory` — the enum.
  - `string displayName`.
  - `Sprite icon`.

## ItemCategory enum (Source.Item.ItemCategory)

```
Empty, Ore, Ammo, Turret, Module, Booster, Junk, UnusedMissionItem,
Drone, RefinedProduct, Torpedo, JumpgatePass, TradeGoods, Usable,
DefensiveTurret, Salvage, Currency, Crystal
```

VGStockpile mapping:
- `Ore` → `MaterialCategory.Ore` (the only ore category).
- `RefinedProduct` → `MaterialCategory.Refined`.
- `Crystal`, `TradeGoods`, `Salvage`, `Junk` → `MaterialCategory.Component` (lumped together — they're the misc tradable categories).
- everything else → `MaterialCategory.Unknown` (we don't expect these in `materialStorage` and don't want to hide them if they appear).

## Faction shape

- Type: `Source.Galaxy.Faction` (regular C# class, not MonoBehaviour).
- `string identifier` — stable id.
- `string name` (virtual) — display.
- `static Dictionary<string, Faction> allFactions`.
- No icon `Sprite` directly on `Faction`; faction visuals live in HUD/UI prefabs and are out of scope for v1 — we ship without faction icons (label is `name — station name`) and revisit if visual QA wants them.

## Vanilla "Locate" flow (galaxy map focus)

Confirmed singletons:
- `Behaviour.GalaxyMap.AbstractGalaxyMapManager.current` (static field) — base class for `GalaxyMapManager` and `GalaxyMapManagerOld`.
  - `MapPointOfInterest focusPointOfInterest` — sets the highlighted POI.
  - `void ShowSystemMap(SystemMapData)` — switches the map to a specific system.
  - `void ShowGalaxyMap(int quadrant = -1)` — show galaxy view.
  - `GalaxyMapWindow mapWindow` — the actual UI window (implements `IDraggableWindow`).

Mission "Locate" trigger details (the exact handler call from `MissionRow.OnPointerClick`) is throw-stubbed in the publicized DLL, so the precise method chain isn't directly visible. The implementation will:
1. Set `AbstractGalaxyMapManager.current.focusPointOfInterest = station`.
2. Call `ShowSystemMap(station.system)` to scope the view.
3. Activate the `mapWindow.gameObject` (in case it's not yet visible) — equivalent to opening the galaxy map UI.

Risk note: if vanilla's Locate uses a higher-level helper we missed, the map may not animate/zoom the way it does for missions. Surface during in-game QA — easy follow-up tweak.

## HUD canvas hook

- Type: `Behaviour.UI.HUD.HudManager : MonoBehaviour`.
- Singleton: `static HudManager Instance`.
- Lifecycle: has `Awake()`, `Start()`, `LateUpdate()`. Patch postfix on `Awake()` for early injection; if the canvas isn't ready until `Start()`, fall back to that.
- The HUD canvas isn't a direct field; we walk the transform via `GetComponentInChildren<Canvas>()` from the `HudManager` instance.
- Fields suggesting the HUD root: `healthBarContent : RectTransform`, `wingmanDisplayParent : Transform`, `dockButton : Button`. Any of these can be walked up to find the parent `Canvas`.

## QA findings

(To be appended during Task 17 in-game verification.)
