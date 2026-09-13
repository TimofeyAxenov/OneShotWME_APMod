using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using OneShotMG;
using OneShotMG.src;
using OneShotMG.src.Entities;
using OneShot.Archipelago.UI;
using OneShotMG.src.Map;
using OneShotMG.src.TWM;

namespace OneShot.Archipelago.Patches
{
    [HarmonyPatch(typeof(EventRunner), "commandTransferPlayer")]
    public static class TransferPatches
    {
        private static readonly HashSet<int> RefugeMaps = new HashSet<int>
        {
            47, 48, 49, 52, 54,
            84, 85, 87, 90, 91,
            116, 120,
            213, 214, 215, 216, 219, 225
        };

        [HarmonyPrefix]
        public static bool Prefix(EventCommand command)
        {
            if (!APSaveManager.IsAPModeActive)
                return true;

            if (!int.TryParse(command.parameters[1], NumberStyles.Any, CultureInfo.InvariantCulture, out int destMapId))
                return true;

            if (!RefugeMaps.Contains(destMapId))
                return true;

            if (ArchipelagoClient.Connected && ArchipelagoClient.OwnedLogicalKeys.Contains(RegionWindow.KEY_REFUGE))
                return true;

            Mod.Context.Logger.Log($"Archipelago: Blocked transfer to Refuge (map {destMapId}) — player lacks Refuge Key, redirecting to home");

            command.parameters[1] = "4";
            command.parameters[2] = "21";
            command.parameters[3] = "10";

            return true;
        }
    }

    [HarmonyPatch(typeof(TileMapManager), "StartEvent")]
    public static class RowbotPatches
    {
        // EventCommandCode enum values (all public int fields on EventCommand)
        private const int CodeShowText = 101;        // 0x65  ShowText
        private const int CodeExit     = 115;        // 0x73  ExitEventProcessing

        // The marker dialogue where we truncate the departure.
        private const string CutMarker = "SETTING COURSE";

        // Cache the pristine page list so rebuilds stay idempotent.
        private static EventCommand[]? _originalList;

        [HarmonyPrefix]
        public static bool Prefix(Entity triggeringEntity)
        {
            if (!APSaveManager.IsAPModeActive)
                return true;

            var osWindow = GameStateManager.GetWindow();
            if (osWindow == null) return true;

            if (GetCurrentMapId(osWindow) != 19)   // Docks
                return true;

            var list = triggeringEntity?.list;
            if (list == null || list.Length == 0)
                return true;
            if (triggeringEntity == null)
                return true;

            // Only run for the rowbot's scripted page (contains our marker).
            int cutIndex = FindMarkerIndex(list);
            if (cutIndex < 0)
                return true;

            // Already rewritten (exit command inserted on a previous interaction).
            if (list.Any(c => c.code == CodeExit))
                return true;

            // First time: snapshot the original for clean rebuilds.
            if (_originalList == null)
                _originalList = (EventCommand[])list.Clone();

            // Build: [0 .. SETTING COURSE] + replacement lines + Exit
            var baseCommands = _originalList.Take(cutIndex + 1).ToArray();

            var replacement = new[]
            {
                NewText("@rowbot WAIT. THE LAKE'S WATER IS UNSTABLE. WE CANNOT DEPART."),
                NewText("@niko_speak Um... How am I going to get to the Tower then?"),
                NewText("@rowbot PERHAPS OUR GOD COULD FIND A DIFFERENT WAY?"),
                NewExit(),
            };

            var rebuilt = baseCommands.Concat(replacement).ToArray();

            SetEntityList(triggeringEntity, rebuilt);

            return true; // let StartEvent build the runner from the new list
        }

        // ---- helpers ----

        private static int FindMarkerIndex(EventCommand[] list)
        {
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i].code == CodeShowText &&
                    list[i].parameters != null &&
                    list[i].parameters.Length > 0 &&
                    list[i].parameters[0] != null &&
                    list[i].parameters[0].Contains(CutMarker))
                {
                    return i;
                }
            }
            return -1;
        }

        private static EventCommand NewText(string text)
        {
            return new EventCommand
            {
                code = CodeShowText,
                indent = 3,
                parameters = new[] { text },
            };
        }

        private static EventCommand NewExit()
        {
            return new EventCommand
            {
                code = CodeExit,
                indent = 3,
                parameters = new string[0],
            };
        }

        private static int GetCurrentMapId(OneshotWindow osWindow)
        {
            var field = osWindow.tileMapMan.GetType().GetField("currentMapId",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field == null) return -1;
            return (int)field.GetValue(osWindow.tileMapMan);
        }

        // Entity.list has a public getter but a PRIVATE setter — set the backing field via reflection.
        private static void SetEntityList(Entity entity, EventCommand[] list)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var field = typeof(Entity).GetField("<list>k__BackingField", flags)
                ?? typeof(Entity).GetField("list", flags);
            if (field == null)
            {
                Mod.Context.Logger.Log("Archipelago: Could not find Entity list backing field.");
                return;
            }
            field.SetValue(entity, list);
        }
    }
}
