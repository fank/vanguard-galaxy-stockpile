# VGStockpile — Galaxy-wide station stockpile overview for Vanguard Galaxy

![Stockpile window](https://raw.githubusercontent.com/fankserver/vanguard-galaxy-stockpile/main/docs/screenshots/stockpile-window.jpg)

A BepInEx 5 plugin that adds a HUD button (top-right) which opens a single window showing every station's stored materials in one grid. Stations as rows, materials as columns. The overview is read-only; optional transfers reserve/move materials, debit credits and persist a pending-job sidecar.

## Features

- **Galaxy-wide grid.** Every station with ≥1 stored material appears as a row; every material that exists across visible stations gets a column.
- **Vanilla "Sort by Type" column order.** Columns cluster the same way the cargo inventory's *Sort by Type* button orders items — `(itemCategory, gameplayType, name)`, lifted verbatim from the game's `Inventory.SortByCategory`.
- **Category filters.** Six toggle buttons in the header — Ores, Refined Canisters, Refined Products, Crystals, Trade Goods, Salvage. Click to hide / show. State persists across sessions.
- **Vanilla item tooltips on hover.** Header icons and quantity cells use `ItemTooltipSource`, the same tooltip component vanilla inventory slots use.
- **Click a station label to "Locate"** — opens the galaxy map and focuses the station, mirroring the mission UI's Locate button (calls `SidePanel.OpenMapAndFocusPoi`).
- **Live read on open.** Reopening the overview refreshes station data.
- **Optional transfers.** Pending jobs persist alongside successful vanilla saves; transfer operations do not independently write ahead of the saved inventory/credits.

## Install

1. Install BepInEx 5.x in your Vanguard Galaxy folder.
2. Install [VGModAPI 0.1.2+ within 0.1.x](https://github.com/fankserver/vanguard-galaxy-api). Keep one canonical API copy; do not duplicate its Abstractions DLL in consumer folders.
3. Drop the `VGStockpile/` folder from the release zip into `BepInEx/plugins/`, including Newtonsoft.Json and notices.
4. Launch the game. Missing/unsupported API or unavailable lifecycle/save capabilities disable Stockpile before sidecar operations.

## Configuration

`BepInEx/config/vgstockpile.cfg` exposes:

| Key | Default | Purpose |
|---|---|---|
| `UI.ActiveCategories` | `RefinedCanister,RefinedGoods,Crystal,TradeGoods,Salvage,Other` | Comma-separated list of visible categories. Toggling a filter button updates this. |
| `UI.IconRightPadding` / `UI.IconTopPadding` | `128` / `12` | HUD icon position from the top-right corner. |
| `UI.CloseWindowOnLocate` | `true` | Auto-close the window when a station label is clicked. |
| `Transfers.Enabled` | `false` | Enable inventory/credit-changing transfers. |

## Build (for contributors)

Build the sibling API Release package first (or set `VGAPI_DLL`). Game/Unity metadata stays owner-local; it is no longer tracked. Public CI validates archive layout only and never builds with game assets.

```sh
make refresh-asm # current owner-installed game; requires assembly-publicizer
make build
make test
make package CONFIG=Release # inspect and attach dist/VGStockpile.zip to a release
make deploy     # copies into <GAME_DIR>/BepInEx/plugins/VGStockpile/
```

`<GAME_DIR>` is hard-coded to a WSL Steam path in the Makefile — adjust locally for non-WSL setups, but don't commit the change.

## Architecture

Three internal areas:

- **`Data/`** — pure read side. `StationStorageReader` walks the galaxy POIs, reads each `SpaceStation.materialStorage`, returns immutable `StationStorageSnapshot` records. `MaterialCatalog` resolves `InventoryItemType` references and classifies materials into the `MaterialCategory` enum.
- **`UI/`** — UGUI rendering. `StorageGridBuilder` (pure, unit-tested) computes columns + sorted rows. `StationStorageWindow` and `StationStorageIcon` are the Unity-touching layer.
- **`Locate/`** — `IStationLocator` + production `StationLocator` that invokes vanilla's `SidePanel.OpenMapAndFocusPoi` coroutine via reflection (publicized stub vs runtime privacy).

## Transfer lifecycle boundaries

Queue restoration waits for PlayerReady; mutations/ticks wait for the matching GameplayInitialized session and run outside API callback delivery or in-flight saves. Session replacement clears pending memory without returning old-world inventory into a new world. HUD attachment still uses SidePanel.Start—not a global readiness guess.

SaveStarted captures the queue without changing vanilla data; only its matching SaveSucceeded writes that snapshot to the reported destination. It is after vanilla's caller snapshot construction, not a pre-serialization hook. Failed/skipped saves leave sidecars unchanged. Sidecar write failures pause mutations (jobs stay visible) until a later successful save retries persistence. Corrupt, unreadable or newer-version sidecars disable restoration for that attempt and are never intentionally overwritten. Empty queues do not create new sidecars. There is no cross-file transaction or rollback guarantee. Unsaved transfer progress is lost with unsaved vanilla changes.

### Experimental coordinated storage (0.7)

Default persistence remains the legacy sidecar path described above. After enabling VGModAPI persistence, separately set `[Persistence] UseCoordinatedPersistence = true` in `vgstockpile.cfg`. Unavailable service fails closed, with no legacy fallback. Coordinated mode stores transfer JSON in the API owner envelope (1 MiB limit), never writes legacy sidecars, and gates both queries and mutations on registration readiness. Capture bypasses public query gates without changing reservation, fee, cancellation or delivery logic. Disabled transfers retain loaded jobs for subsequent saves; warning delivery still waits for the HUD.

`ImportLegacySidecars = true` explicitly permits read-only adoption when this owner has no coordinated data. It does not prove that historical sidecar bytes match the vanilla snapshot. Corrupt, future-version, unreadable, malformed or oversized inputs block restoration without overwrite/quarantine. Existing logical transfer schema is retained, including the legacy reader's version-0 compatibility. Registration disposal pauses coordinated persistence until another load; status changes are logged. Native two-consumer/recovery qualification and full owner acceptance remain outstanding.

Schema is unchanged. With transfers disabled, pending-job warnings wait for an actual HUD. Tests cover pure overview/transfer logic and lifecycle guards. Controlled native coverage includes save/reload, real transfer inventory/credit changes, journal coexistence, refusal and teardown. Full owner acceptance remains separate; API RuntimeQualified remains false.

## License

MIT — see `LICENSE`.
