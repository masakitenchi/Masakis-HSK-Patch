using System.Reflection.Emit;
using System.IO;
using CombatExtended;
using Verse;

namespace Core_SK_Patch;

public class TestSeparateDefComp : ThingComp
{
    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        yield return new Command_Action()
        {
            defaultDesc = "Report ID & def.label",
            defaultLabel = "Report ID & def.label",
            icon = BaseContent.BadTex,
            action = () =>
            {
                Log.Message($"ID:{this.parent.ThingID}, def.label{this.parent.def.label}");
            }
        };
        yield return new Command_Action()
        {
            defaultDesc = "Change def.label between true & false",
            defaultLabel = "Change def.label between true & false",
            icon = BaseContent.GreyTex,
            action = () =>
            {
                this.parent.def.label = this.parent.def.label == "true" ? "false" : "true";
            }
        };
    }
}

public class WeaponAutoCastComp : ThingComp
{
    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        yield return new Command_Action()
        {
            defaultDesc = "Report ID & verb.onlyManualCast",
            defaultLabel = "Report ID & verb.onlyManualCast",
            icon = BaseContent.BadTex,
            action = () =>
            {
                Log.Message($"ID: {this.parent.ThingID}, verb.onlyManualCast {this.parent.def.Verbs.First().onlyManualCast}");
            }
        };
        yield return new Command_Action()
        {
            defaultDesc = "Toggle verb.onlyManualCast",
            defaultLabel = "Toggle verb.onlyManualCast",
            icon = BaseContent.GreyTex,
            action = () =>
            {
                ThingDef def = AccessTools.MakeDeepCopy(this.parent.def, typeof(ThingDef)) as ThingDef;
                def.Verbs.First().onlyManualCast = false;
                Log.Message("DeepCopy Made!");
                this.parent.def = def;
            }
        };
    }
}

//Only on Pawn. Use with a transpiler for control each pawn's behavior
public class ManualCastOverride : ThingComp
{
    public bool AutoCastOverride;

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        ThingWithComps equip = parent is Pawn pawn && pawn.IsColonistPlayerControlled ? 
            pawn.inventory.innerContainer.FirstOrDefault(x => x.def.IsAOEWeapon()) as ThingWithComps : null;
        if (equip == null)
        {
            yield break;
        }
        yield return new Command_Toggle()
        {
            defaultDesc = "AOECastOverride",
            defaultLabel = "AOECastOverride",
            icon = TexCommand.FireAtWill,
            toggleAction = () =>
            {
                this.AutoCastOverride = !this.AutoCastOverride;
            },
            isActive = () => AutoCastOverride
        };
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref AutoCastOverride, "AOECastOverride");
    }
}

/*
 * IL_002c: ldarg.0
	IL_002d: ldfld class Verse.Pawn_EquipmentTracker Verse.Pawn::equipment
	IL_0032: callvirt instance class Verse.CompEquippable Verse.Pawn_EquipmentTracker::get_PrimaryEq()
	IL_0037: callvirt instance class Verse.Verb Verse.CompEquippable::get_PrimaryVerb()
	IL_003c: ldfld class Verse.VerbProperties Verse.Verb::verbProps
	IL_0041: ldfld bool Verse.VerbProperties::onlyManualCast
	IL_0046: brfalse.s IL_006a
    <- insert here
	IL_0048: ldarg.0
	IL_0049: call instance class Verse.AI.Job Verse.Pawn::get_CurJob()
	IL_004e: brfalse.s IL_0067
    IL_0050: ldarg.0
	IL_0051: call instance class Verse.AI.Job Verse.Pawn::get_CurJob()
	IL_0056: ldfld class Verse.JobDef Verse.AI.Job::def
	IL_005b: ldsfld class Verse.JobDef RimWorld.JobDefOf::Wait_Combat
	IL_0060: ceq
	IL_0062: ldc.i4.0
	IL_0063: ceq
	IL_0065: br.s IL_006b
	IL_0067: ldc.i4.0
	IL_0068: br.s IL_006b
	IL_006a: ldc.i4.1
	IL_006b: ldarg.2
	IL_006c: or
	IL_006d: brfalse.s IL_0080
	IL_006f: ldarg.0
	IL_0070: ldfld class Verse.Pawn_EquipmentTracker Verse.Pawn::equipment
	IL_0075: callvirt instance class Verse.CompEquippable Verse.Pawn_EquipmentTracker::get_PrimaryEq()
	IL_007a: callvirt instance class Verse.Verb Verse.CompEquippable::get_PrimaryVerb()
	IL_007f: ret
*/
[HarmonyPatch]
public static class AllowAutoCastAOEWeapon
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.TryGetAttackVerb))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        bool Found = false;
        List<CodeInstruction> ins = instructions.ToList();
        for (int i = 0; i < ins.Count; i++)
        {
            CodeInstruction inst = ins[i];
            if (!Found && inst.Is(OpCodes.Ldfld, AccessTools.Field(typeof(VerbProperties), nameof(VerbProperties.onlyManualCast))))
            {
                yield return inst;  //ldfld onlyManualCast
                ++i;
                yield return ins[i]; //brfalse.s
                Found = true;
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ThingCompUtility), nameof(ThingCompUtility.TryGetComp), generics: new Type[] { typeof(ManualCastOverride) }));
                yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ManualCastOverride), nameof(ManualCastOverride.AutoCastOverride)));
                yield return new CodeInstruction(OpCodes.Brtrue_S, ins[i].operand);
            }
            else
            {
                yield return inst;
            }
        }
    }
}

