using RimWorld;
using Verse;

namespace Core_SK_Patch;

[HotSwappable]
public static class WorldPawnCleanupDebugAction
{
    [DebugAction(
        "Pawns",
        "Run WorldPawns cleanup",
        actionType = DebugActionType.Action,
        allowedGameStates = AllowedGameStates.Playing)]
    public static void RunWorldPawnCleanup()
    {
        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
            "Run the vanilla WorldPawn garbage collector now? Pawns required by factions, quests, relations and other game systems will be retained.",
            () =>
            {
                int before = Find.WorldPawns.AllPawnsAliveOrDead.Count;
                Find.WorldPawns.gc.RunGC();
                Messages.Message(
                    $"WorldPawn cleanup started ({before} world pawns before cleanup).",
                    MessageTypeDefOf.NeutralEvent,
                    historical: false);
            },
            destructive: true));
    }
}
