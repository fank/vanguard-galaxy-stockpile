namespace VGStockpile.Transfers;

internal sealed record TransferConfig(
    bool  Enabled,
    bool  PushEnabled,
    int   MaxConcurrent,

    int   QuantityStepSmall,
    int   QuantityStepLarge,
    int   ShiftMultiplier,

    int   FeeBase,
    int   FeePerUnit,
    int   FeePerJump,
    float FeePerUnitPerJump,

    float EtaBaseSeconds,
    float EtaPerJumpSeconds,
    float EtaMinSeconds,
    float EtaMaxSeconds,

    bool  PushRequiresPeaceful,
    bool  PushRequiresRefinery)
{
    public static TransferConfig Defaults() => new(
        Enabled: false, PushEnabled: false, MaxConcurrent: 3,
        QuantityStepSmall: 1, QuantityStepLarge: 20, ShiftMultiplier: 5,
        FeeBase: 100, FeePerUnit: 1, FeePerJump: 50, FeePerUnitPerJump: 0.5f,
        EtaBaseSeconds: 30f, EtaPerJumpSeconds: 20f,
        EtaMinSeconds: 15f, EtaMaxSeconds: 1800f,
        PushRequiresPeaceful: true, PushRequiresRefinery: true);
}
