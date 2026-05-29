namespace VGStockpile.Transfers;

internal static class EligibilityRules
{
    public static bool CanPullFrom(StationContext s) =>
        s.IsPlayerDocked
        && s.HasBeenVisited
        && s.HasAnyStoredMaterials
        && !s.IsCurrent;

    public static bool CanPushTo(StationContext s, TransferConfig cfg) =>
        s.IsPlayerDocked
        && s.HasBeenVisited
        && !s.IsCurrent
        && (!cfg.PushRequiresRefinery || s.HasRefinery)
        && (!cfg.PushRequiresPeaceful || s.IsPeaceful);
}
