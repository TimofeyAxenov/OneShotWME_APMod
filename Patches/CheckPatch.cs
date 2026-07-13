using HarmonyLib;
using OneShotMG.src.Entities;
using System.Globalization;

namespace OneShot.Archipelago.Patches
{
    [HarmonyPatch(typeof(EventRunner), "commandChangeItems")]
    public static class CheckPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(EventCommand command)
        {
            Mod.Context.Logger.Log("Patch triggered");

            // Parse item ID
            if (!int.TryParse(command.parameters[0], NumberStyles.Any, CultureInfo.InvariantCulture, out int itemId))
            {
                Mod.Context.Logger.Log("Failed to parse itemId");
                return true;
            }

            // Determine operation (add/remove)
            int operateValue = GetOperateValue(command);

            // Only handle item ADD
            if (operateValue <= 0)
                return true;

            Mod.Context.Logger.Log($"Intercepted item: {itemId}");

            if (ArchipelagoClient.ReceivingAPItem)
                {
                    Mod.Context.Logger.Log($"[AP] Ignoring AP-granted item {itemId}");
                    return true;
                }

            if (itemId == 2)
            {
                    Mod.Context.Logger.Log("Allowing TV Remote for access");
                    return true;
            }

            // If player already has this item (received from AP), let it through silently — no check needed
            var osWindow = GameStateManager.GetWindow();
            if (osWindow != null && osWindow.menuMan.ItemMan.HasItem(itemId))
            {
                    Mod.Context.Logger.Log($"Archipelago: Player already has item {itemId}, letting pickup through");
                    return true;
            }

            LocationTracker.OnItemAdded(itemId);

            return false;
        }

        private static int GetOperateValue(EventCommand command)
        {
            try
            {
                int operation = int.Parse(command.parameters[1]);
                int operand = int.Parse(command.parameters[3]);

                return operation == 1 ? -operand : operand;
            }
            catch
            {
                return 0;
            }
        }
    }
}
