using System;
using System.IO;
using System.Reflection;
using System.Xml;

namespace Core_SK_Patch;

[HotSwappable]
public class DebugActions
{

    [DebugAction("Mods", "OutputModLoadfolders", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Entry)]
    public static void ModLoadfolders()
    {
        List<DebugMenuOption> options = new List<DebugMenuOption>();
        foreach (ModContentPack mod in LoadedModManager.RunningModsListForReading)
        {
            options.Add(new DebugMenuOption(mod.Name, DebugMenuOptionMode.Action, (Action)(() =>
            {
                ModMetaData metaData = ModLister.GetModWithIdentifier(mod.PackageId);
                if (metaData.loadFolders != null && metaData.loadFolders.DefinedVersions()?.Count != 0)
                    Find.WindowStack.Add(new Dialog_DebugOptionListLister(metaData.loadFolders.DefinedVersions().Select<string, DebugMenuOption>(ver => new DebugMenuOption(ver, DebugMenuOptionMode.Action, () => ShowTable(metaData.loadFolders.FoldersForVersion(ver).Select(f => new Pair<string, string>(Path.Combine(mod.RootDir, f.folderName), f.ShouldLoad.ToString())).ToList())))));
                else
                    ShowTable((List<Pair<string, string>>)null);
            })));
        }

        static void ShowTable(List<Pair<string, string>> FoldersAndLoaded)
        {
            DebugTables.MakeTablesDialog(FoldersAndLoaded, new List<TableDataGetter<Pair<string, string>>>()
            {
                new TableDataGetter<Pair<string, string>>("Path", f => f.First),
                new TableDataGetter<Pair<string, string>>("Loaded", f=> f.Second)
            }.ToArray());
        }
        Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
    }
    /*[DebugAction("Mods", "ShowGraphicData", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Entry)]
    public static void ShowGraphicData()
    {
        List<DebugMenuOption> options = new();
        foreach(ThingDef thing in DefDatabase<ThingDef>.AllDefs.Where(x => x.graphicData != null))
        {
            options.Add(new DebugMenuOption(thing.defName, DebugMenuOptionMode.Action, () =>
            {
                Log.Message($"defName:{thing.defName} graphic path: {thing.graphicData.texPath}");
            }));
        }
    }*/
}
