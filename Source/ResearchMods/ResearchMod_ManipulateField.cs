using Mono.Cecil;
using System.Reflection;

namespace Core_SK_Patch;
public abstract class ResearchMod_ManipulateField : ResearchMod
{
#nullable enable
    protected object? instance;
#nullable disable
    protected Type type;
    protected FieldInfo fieldInfo;
    protected ValueType originalvalue;
    protected bool cached;

    public virtual void ResetField()
    {
        fieldInfo.SetValue(instance, originalvalue);
    }

    public abstract void CacheField();

    public override void Apply()
    {
        if (!cached) this.CacheField();
    }
}


public class ResearchMod_ChangeDefSimple : ResearchMod_ManipulateField
{
    public string defName;
    public Type defType;
    public string field;
    public float value;
    public TargetMode mode;

    private AccessTools.FieldRef<float> fieldRef;
    public override void CacheField()
    {
        instance = GenDefDatabase.GetDefSilentFail(defType, defName);
        fieldInfo = AccessTools.Field(instance.GetType(), field);
        originalvalue = fieldInfo.GetValue(instance) as ValueType;
        cached = true;
    }

    public override void Apply()
    {
        base.Apply();
        switch (mode)
        {
            case TargetMode.Add:
                fieldInfo.SetValue(instance, (originalvalue is float? Convert.ToSingle(originalvalue): Convert.ToInt32(originalvalue)) + value);
                break;
            case TargetMode.Subtract:
                fieldInfo.SetValue(instance, (originalvalue is float ? Convert.ToSingle(originalvalue) : Convert.ToInt32(originalvalue)) - value);
                break;
            case TargetMode.Multiply:
                fieldInfo.SetValue(instance, (originalvalue is float ? Convert.ToSingle(originalvalue) : Convert.ToInt32(originalvalue)) * value);
                break;
            case TargetMode.Divide:
                fieldInfo.SetValue(instance, (originalvalue is float ? Convert.ToSingle(originalvalue) : Convert.ToInt32(originalvalue)) / value);
                break;
            case TargetMode.Set:
                fieldInfo.SetValue(instance, value);
                break;
            default: throw new InvalidOperationException($"Must define a mode. Available: {string.Join("\n", Enum.GetNames(typeof(TargetMode)))}");
        }
#if DEBUG
        Log.Message($"Applied {mode} to {defName}.{field}, current value is: {fieldInfo.GetValue(instance)}");
#endif
    }
}

public class ResearchMod_ChangeStatBase : ResearchMod_ManipulateField
{
    public string defName;
    public Type defType;
    public string statName;
    public float value;
    public TargetMode mode;

    private List<StatModifier> statBases;
    private StatModifier statModifier;

    public override void ResetField()
    {
        this.statModifier.value = Convert.ToSingle(originalvalue);
    }
    public override void CacheField()
    {
        instance = GenDefDatabase.GetDefSilentFail(defType, defName);
        //List<StatModifier>
        fieldInfo = AccessTools.Field(instance.GetType(), "statBases");
        InitStatModifier();

        originalvalue = statModifier.value;
        cached = true;
    }

    private void InitStatModifier()
    {
        statBases = fieldInfo.GetValue(instance) as List<StatModifier>;
        statModifier = statBases.Find(x => x.stat.defName == statName);

        //Generate new statModifier at runtime if target uses defaultBaseValue of that StatDef
        if (statModifier is null)
        {
            StatModifier stat = new StatModifier()
            {
                stat = DefDatabase<StatDef>.AllDefs.First(x => x.defName == statName),
                value = DefDatabase<StatDef>.AllDefs.First(x => x.defName == statName).defaultBaseValue
            };
            statModifier = stat;
            statBases.Add(statModifier);
        }
    }

    public override void Apply()
    {
        base.Apply();
        switch(mode)
        {
            case TargetMode.Add:
                statModifier.value += value;
                break;
            case TargetMode.Subtract:
                statModifier.value -= value;
                break;
            case TargetMode.Multiply:
                statModifier.value *= value;
                break;
            case TargetMode.Divide:
                statModifier.value /= value;
                break;
            case TargetMode.Set:
                statModifier.value = value;
                break;
            default: throw new InvalidOperationException($"Must define a mode. Available: {string.Join("\n", Enum.GetNames(typeof(TargetMode)))}");
        }
#if DEBUG
        Log.Message($"Applied {mode} to {defName}'s {statName}, Before: {originalvalue}, after: {statModifier.value}");
#endif
    }
}

