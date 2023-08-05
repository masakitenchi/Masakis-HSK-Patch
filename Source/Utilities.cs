using SK;
using System.Reflection;
using System.Runtime.CompilerServices;
using Verse.Profile;

namespace Core_SK_Patch;

public enum TargetMode
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Set
};

public static class Utilities
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Invert_Bool(ref this bool a)
    {
        a = !a;
    }
}

[HarmonyPatch]
public static class ResearchScriber
{
    //Every ResearchProjectDef has a HashSet of ResearchMods to apply
    public static Dictionary<ResearchProjectDef, HashSet<ResearchMod>> modLister = new();

    [HarmonyPatch(typeof(ResearchProjectDef),nameof(ResearchProjectDef.ReapplyAllMods))]
    [HarmonyPrefix]
    public static bool DoNotApplyModMoreThanOnce(ResearchProjectDef __instance)
    {
        return modLister.TryAdd(__instance, __instance.researchMods?.ToHashSet());
    }

    [HarmonyPatch(typeof(MemoryUtility), nameof(MemoryUtility.ClearAllMapsAndWorld))]
    [HarmonyPrefix]
    public static void ResetAllFieldsAndCleanCache()
    {
        modLister.Values.Do(x =>
            {
                foreach (var mod in x)
                {
                    switch(mod)
                    {
                        case ResearchMod_ManipulateField a:
                            a.ResetField();
                            break;
                    }
                }
            });
        modLister.Clear();
    }
}

/*public abstract class ResearchMod_ManipulateStaticField : ResearchMod
{
    protected Type type;
    protected FieldInfo fieldInfo;
    protected float originalvalue;

    [RestoreData]
    public virtual void ResetField() => fieldInfo.SetValue(null, originalvalue);
    public abstract void CacheField();
}
*/
public abstract class ResearchMod_ManipulateField : ResearchMod
{
#nullable enable
    protected object? instance;
#nullable disable
    protected Type type;
    protected FieldInfo fieldInfo;
    protected float originalvalue;

    public virtual void ResetField() => fieldInfo.SetValue(instance, originalvalue);

    public abstract void CacheField();
}


public class ResearchMod_ChangeDef : ResearchMod_ManipulateField
{
    public string defName;
    public Type defType;
    public string field;
    public float value;
    public TargetMode mode;

    public override void CacheField()
    {
        instance = GenDefDatabase.GetDefSilentFail(defType, defName);
        fieldInfo = AccessTools.Field(instance.GetType(), field);
        originalvalue = (float)fieldInfo.GetValue(instance);
    }

    public override void Apply()
    {
        this.CacheField();
        switch (mode)
        {
            case TargetMode.Add:
                fieldInfo.SetValue(instance, originalvalue + value);
                break;
            case TargetMode.Subtract:
                fieldInfo.SetValue(instance, originalvalue - value);
                break;
            case TargetMode.Multiply:
                fieldInfo.SetValue(instance, originalvalue * value);
                break;
            case TargetMode.Divide:
                fieldInfo.SetValue(instance, originalvalue / value);
                break;
            case TargetMode.Set:
                fieldInfo.SetValue(instance, value); 
                break;
            default: throw new InvalidOperationException($"Must define a mode. Available: {string.Join("\n",Enum.GetNames(typeof(TargetMode)))}");
        }
        Log.Message($"Applied {mode} to {defName}.{field}, current value is: {fieldInfo.GetValue(instance)}");
    }
}
