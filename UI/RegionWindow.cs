using OneShotMG;
using OneShotMG.src;
using OneShotMG.src.TWM;
using OneShot.Archipelago.Patches;
using WorldMachineLoader.API.Core;
using WorldMachineLoader.API.UI;
using WorldMachineLoader.API.UI.Controls;

namespace OneShot.Archipelago.UI
{
        public class RegionWindow : ModWindow
        {
                private const int W = 340;
                private const int H = 80;
                private const int PAD = 6;

                private static RegionWindow? _instance;

                public const int KEY_BARRENS = 700;
                public const int KEY_GLEN    = 701;
                public const int KEY_REFUGE  = 702;

                public static void Initialize()
                {
                        if (_instance != null) return;
                        _instance = new RegionWindow();
                        Game1.windowMan.AddWindow(_instance);
                }

                public static void Close() { _instance = null; }

                public RegionWindow() : base("Region Selection Window", "", W, H, addCloseButton: true, addMinimizeButton: true)
                {
                        int y = PAD;
                        y += 24;
                        AddControl(new Button("Go Home", new Vec2(150, y), GoHome));
                        
                        y += 24;
                        AddControl(new Button("Go to Barrens", new Vec2(PAD, y), GoToBarrens));
                        AddControl(new Button("Go to Glen", new Vec2(PAD + 130, y), GoToGlen));
                        AddControl(new Button("Go to Refuge", new Vec2(PAD + 260, y), GoToRefuge));
                }

                private static bool HasLightbulb()
                {
                        var osWindow = GameStateManager.GetWindow();
                        if (osWindow == null) return false;
                        return osWindow.menuMan.ItemMan.HasItem(1);
                }

                private void GoHome()
                {
                        TeleportToMap(4, 21, 10, "Home");
                }

                private void GoToBarrens()
                {
                        if (!HasLightbulb())
                        {
                                var osWindow = GameStateManager.GetWindow();
                                if (osWindow == null) return;
                                osWindow.ShowModalWindow(
                                        ModalWindow.ModalType.Info,
                                        "You cannot leave yet.\nThe Lightbulb is needed to power the teleportation system.",
                                        null, playModalNoise: true);
                                return;
                        }
                        TeleportToRegion(KEY_BARRENS, "Barrens", 12, 23, 49);
                }

                private void GoToGlen()
                {
                        if (!HasLightbulb())
                        {
                                var osWindow = GameStateManager.GetWindow();
                                if (osWindow == null) return;
                                osWindow.ShowModalWindow(
                                        ModalWindow.ModalType.Info,
                                        "You cannot leave yet.\nThe Lightbulb is needed to power the teleportation system.",
                                        null, playModalNoise: true);
                                return;
                        }
                        TeleportToRegion(KEY_GLEN, "Glen", 27, 20, 41);
                }

                private void GoToRefuge()
                {
                        if (!HasLightbulb())
                        {
                                var osWindow = GameStateManager.GetWindow();
                                if (osWindow == null) return;
                                osWindow.ShowModalWindow(
                                        ModalWindow.ModalType.Info,
                                        "You cannot leave yet.\nThe Lightbulb is needed to power the teleportation system.",
                                        null, playModalNoise: true);
                                return;
                        }
                        TeleportToRegion(KEY_REFUGE, "Refuge", 47, 94, 45);
                }

                private static bool HasKey(int keyId)
                {
                        return ArchipelagoClient.Connected
                                && ArchipelagoClient.OwnedLogicalKeys.Contains(keyId);
                }

                private static void TeleportToRegion(int keyId, string regionName, int mapId, int tileX, int tileY)
                {
                        if (!HasKey(keyId))
                        {
                                var osWindow = GameStateManager.GetWindow();
                                if (osWindow == null) return;
                                osWindow.ShowModalWindow(
                                        ModalWindow.ModalType.Info,
                                        $"Teleportation to {regionName} is impossible.\nYou do not have the required key.",
                                        null, playModalNoise: true);
                                return;
                        }
                        TeleportToMap(mapId, tileX, tileY, regionName);
                }

                private static void TeleportToMap(int mapId, int tileX, int tileY, string label)
                {
                        var osWindow = GameStateManager.GetWindow();
                        if (osWindow == null)
                        {
                                Mod.Context.Logger.Log($"Archipelago: Cannot teleport to {label} - game window not open.");
                                return;
                        }
                        WindowManagerHelper.TeleportTo(mapId, tileX, tileY);
                        Mod.Context.Logger.Log($"Archipelago: Teleported to {label} (map {mapId}, {tileX}, {tileY})");
                }
        }
}
