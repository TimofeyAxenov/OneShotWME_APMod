using OneShotMG.src;
using WorldMachineLoader.API.Core;

namespace OneShot.Archipelago.Patches
{
    public class FlagPatches
    {
        [GamePatch(typeof(FlagManager), "SetFlag", PatchType.Postfix, typeof(int))]
        public static void SetFlag_Postfix(int flagIndex)
        {
            if (!ArchipelagoClient.Connected) return;
            if (!APSaveManager.IsAPModeActive) return;
            LocationTracker.OnFlagSet(flagIndex);
        }
    }
}
