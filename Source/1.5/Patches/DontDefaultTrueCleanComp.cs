using System.Reflection;

namespace Core_SK_Patch;

[HarmonyPatch]
public static class DontDefaultTrueCleanComp
{
	public static bool Prepare()
	{
		return ModsConfig.IsActive("avilmask.commonsense");
	}
	public static MethodBase TargetMethod()
	{
		return AccessTools.Constructor(AccessTools.TypeByName("CommonSense.DoCleanComp"), new Type[] {});
	}

	public static void Postfix(ref bool ___active)
	{
		___active = false;
	}
}