namespace Core_SK_Patch;

// SOS2 ca94d598 (V49), ShipCombatManager.SalvageThing/SalvageEverything.
internal static class LegacySalvageRules
{
    internal const int MassPerBay = 1000;
    internal static int Refund(int count, bool plasteel) => (int)(count * (plasteel ? 0.25f : 0.5f));
    internal static int SteelSlag(int steel) => steel / 20;
    internal static int SalvageChunks(int spacerComponents) => spacerComponents / 10;
}
