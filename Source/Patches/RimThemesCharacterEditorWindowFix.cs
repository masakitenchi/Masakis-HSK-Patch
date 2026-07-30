using System.Reflection;
using HarmonyLib;
using Verse;

namespace Core_SK_Patch;

/// <summary>
/// RimThemes' non-stacking mode can leave an inspect-tab marker at the top of
/// its private window list. Its WindowOnGUI prefix then suppresses every dialog,
/// including Character Editor's cached editor window, until that marker is
/// removed. Character Editor remains open in WindowStack but is never drawn.
/// </summary>
[StaticConstructorOnStartup]
public static class RimThemesCharacterEditorWindowFix
{
    private const string CharacterEditorPackageId = "void.charactereditor";
    private const string CharacterEditorWindowType = "CharacterEditor.CEditor+EditorUI";
    private const string RimThemesPrefix =
        "aRandomKiwi.RimThemes.WindowStackOnGUI_Patch:Prefix";

    static RimThemesCharacterEditorWindowFix()
    {
        if (!ModsConfig.IsActive(CharacterEditorPackageId) || !RimThemesIsActive())
        {
            return;
        }

        MethodInfo target = AccessTools.Method(RimThemesPrefix);
        MethodInfo prefix = AccessTools.Method(
            typeof(RimThemesCharacterEditorWindowFix),
            nameof(AllowCharacterEditorWindow));

        if (target == null || prefix == null)
        {
            Log.Warning("[Core SK Patch] Could not install the RimThemes/Character Editor window fix.");
            return;
        }

        new Harmony("com.reggex.HSKPatch.RimThemesCharacterEditorWindowFix")
            .Patch(target, prefix: new HarmonyMethod(prefix));

        Log.Message("[Core SK Patch] Enabled the RimThemes/Character Editor window fix.");
    }

    private static bool RimThemesIsActive()
    {
        return ModsConfig.IsActive("skyarkhangel.rimthemeslite") ||
               ModsConfig.IsActive("arandomkiwi.rimthemes") ||
               ModsConfig.IsActive("arandomkiwi.rimthemes_steam");
    }

    private static bool AllowCharacterEditorWindow(
        [HarmonyArgument(0)] Window window,
        ref bool __result)
    {
        if (window?.GetType().FullName != CharacterEditorWindowType)
        {
            return true;
        }

        // Skip RimThemes' filtering prefix and tell Window.WindowOnGUI to run.
        __result = true;
        return false;
    }
}
