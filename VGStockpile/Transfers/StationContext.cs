namespace VGStockpile.Transfers;

internal readonly record struct StationContext(
    string Guid,
    bool HasBeenVisited,
    bool HasAnyStoredMaterials,
    bool HasRefinery,
    bool IsPeaceful,
    bool IsCurrent,
    bool IsPlayerDocked);
