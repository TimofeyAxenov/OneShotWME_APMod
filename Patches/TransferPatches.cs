using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using OneShotMG;
using OneShotMG.src;
using OneShotMG.src.Entities;
using OneShot.Archipelago.UI;

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
}
