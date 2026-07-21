namespace Core_SK_Patch;

public class Settings : ModSettings
{
    public static float InfestationPreventionRadius = 10f;
    public static bool BetterInfestation;
    public static bool EnableBulkRecipe;
    public static bool NeverDieByLowHealth;
    public static bool AutoForbidSpoiled;
    public static bool LogAllPatchOperations;
    public static bool ErrorChecks;
    public static bool EnableFactionDiscovery = true;
    public static bool AllowTamingDownedAnimals;


    public override void ExposeData()
    {
        Scribe_Values.Look(ref BetterInfestation, "BetterInfestation");
        Scribe_Values.Look(ref EnableBulkRecipe, "EnableBulkRecipe", false);
        Scribe_Values.Look(ref InfestationPreventionRadius, "InfestationPreventionRadius");
        Scribe_Values.Look(ref NeverDieByLowHealth, nameof(NeverDieByLowHealth), false);
        Scribe_Values.Look(ref AutoForbidSpoiled, nameof(AutoForbidSpoiled), true);
        Scribe_Values.Look(ref LogAllPatchOperations, nameof(LogAllPatchOperations), false);
        Scribe_Values.Look(ref ErrorChecks, "CheckError");
        Scribe_Values.Look(ref EnableFactionDiscovery, nameof(EnableFactionDiscovery), true);
        Scribe_Values.Look(ref AllowTamingDownedAnimals, nameof(AllowTamingDownedAnimals), false);
    }
}
