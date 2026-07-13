using OneShotMG.src.Menus;
using WorldMachineLoader.API.Core;

namespace OneShot.Archipelago.Patches
{
    public class ItemPatches
    {
        [GamePatch(typeof(ItemManager), "AddItem", PatchType.Prefix, typeof(int))]
        public static bool AddItem_Prefix(int itemId)
        {
            if (!APSaveManager.IsAPModeActive)
                return true;

            if (ArchipelagoClient.ReceivingAPItem)
                return true;

            // If player already has this item, allow the no-op add through
            var osWindow = GameStateManager.GetWindow();
            if (osWindow != null && osWindow.menuMan.ItemMan.HasItem(itemId))
                return true;

            if (LocationTracker.ItemPickupToLocation != null &&
                LocationTracker.ItemPickupToLocation.ContainsKey(itemId))
            {
                Mod.Context.Logger.Log($"Archipelago: Blocked in-game item {itemId} from being added to inventory (AP mode)");
                return false;
            }

            return true;
        }
    }
}
