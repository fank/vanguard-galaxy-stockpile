namespace VGStockpile.UI.Refinery;

/// <summary>
/// Pure timing/progress math for refinery jobs, derived from the vanilla
/// model: each tick does <c>progress += deltaTime</c>; one unit completes when
/// <c>progress >= refineTime</c>, then <c>progress -= refineTime</c> and
/// <c>remainingAmount--</c>. So progress accrues at ~1.0 per game-second and a
/// unit takes <c>refineTime</c> seconds. (Autoplay/bonus modifiers can nudge
/// the real rate, so the ETA is an estimate.)
/// </summary>
internal static class RefineryMath
{
    /// <summary>
    /// Seconds until the whole job finishes: the in-progress unit needs
    /// (refineTime − progress) more, and each other remaining unit needs a
    /// full refineTime. Equals <c>remainingAmount × refineTime − progress</c>.
    /// </summary>
    public static float EtaSeconds(float progress, float refineTime, int remainingAmount)
    {
        if (remainingAmount <= 0 || refineTime <= 0f) return 0f;
        var eta = remainingAmount * refineTime - progress;
        return eta < 0f ? 0f : eta;
    }

    /// <summary>
    /// Overall job completion as a 0–1 fraction across all units:
    /// (unitsDone + currentUnitFraction) / initialAmount.
    /// </summary>
    public static float ProgressFraction(
        int initialAmount, int remainingAmount, float progress, float refineTime)
    {
        if (initialAmount <= 0) return 0f;

        var unitsDone = initialAmount - remainingAmount;
        var current   = refineTime > 0f ? progress / refineTime : 0f;
        if (current < 0f) current = 0f;
        else if (current > 1f) current = 1f;

        var frac = (unitsDone + current) / initialAmount;
        if (frac < 0f) return 0f;
        return frac > 1f ? 1f : frac;
    }
}
