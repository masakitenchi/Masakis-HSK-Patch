using RimWorld;
using Verse;

namespace Core_SK_Patch;

public class Settings : ModSettings
{
    public static float InfestationPreventionRadius = 10f;
    public static bool BetterInfestation;
    public static bool EnableBulkRecipe;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref BetterInfestation, "BetterInfestation");
        Scribe_Values.Look(ref EnableBulkRecipe, "EnableBulkRecipe", false);
        Scribe_Values.Look(ref InfestationPreventionRadius, "InfestationPreventionRadius");
    }
}
