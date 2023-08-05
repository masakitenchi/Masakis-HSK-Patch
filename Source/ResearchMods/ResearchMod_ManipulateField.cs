using System.Reflection;

namespace Core_SK_Patch;
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
            default: throw new InvalidOperationException($"Must define a mode. Available: {string.Join("\n", Enum.GetNames(typeof(TargetMode)))}");
        }
        Log.Message($"Applied {mode} to {defName}.{field}, current value is: {fieldInfo.GetValue(instance)}");
    }
}

