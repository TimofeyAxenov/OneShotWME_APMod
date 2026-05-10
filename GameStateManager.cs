using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using OneShot.Archipelago.UI;
using OneShotMG;
using OneShotMG.src.TWM;
using OneShotMG.src.TWM.Filesystem;

namespace OneShot.Archipelago
{
    public static class GameStateManager
    {
        public static readonly ConcurrentQueue<int>    PendingItems = new ConcurrentQueue<int>();
        public static readonly ConcurrentQueue<string> PendingTraps = new ConcurrentQueue<string>();
        public static readonly ConcurrentQueue<string> PendingChat  = new ConcurrentQueue<string>();
        public static volatile bool PendingLocationRefresh = false;

        public static List<int> PhotoIds = new List<int> {61, 62, 63, 64, 65, 67, 68, 69, 70};

        public static OneshotWindow? GetWindow() => Game1.windowMan?.GetOneshotWindow();

        public static bool SuppressItemIntercept = false;

        public static Random random = new Random();

        public static void Tick()
        {
            var osWindow = GetWindow();
            if (osWindow == null) return;

            // Give items
            while (PendingItems.TryDequeue(out int itemId))
            {
                var ItemInterceptedBefore = LocationTracker.InterceptedItems.Contains(itemId);
                if (!ItemInterceptedBefore) {
                Mod.Context.Logger.Log($"Archipelago: Applying item ID {itemId}");
                if (itemId >= 1 && itemId <= 78 && itemId != 2)
                {
                        SuppressItemIntercept = true;
                        if (itemId == 45) {
                                int PhotoIdIndex = random.Next(PhotoIds.Count);
                                var PhotoId = PhotoIds[PhotoIdIndex];
                                osWindow.menuMan.ItemMan.AddItem(PhotoId);
                                PhotoIds.RemoveAt(PhotoIdIndex);
                        }
                        else {
                                osWindow.menuMan.ItemMan.AddItem(itemId);
                        }
                        SuppressItemIntercept = false;
//                        SuppressItemIntercept = true;
//                       osWindow.menuMan.ItemMan.AddItem(itemId);
//                    SuppressItemIntercept = false;
                }
                else if ((itemId >= 401 && itemId <= 416) ||
                         (itemId >= 426 && itemId <= 451) ||
                         (itemId >= 461 && itemId <= 469))
                {
                    osWindow.flagMan.SetFlag(itemId);
                }
                else if (itemId >= 1000 && itemId < 1100) // example: FILE items
                {
                        HandleFileItem(itemId, osWindow);
                }
                else if (itemId >= 1100 && itemId < 1200) // wallpapers
                {
                        HandleWallpaperItem(itemId, osWindow);
                }
                else if (itemId >= 1200 && itemId < 1300) // themes
                {
                        HandleThemeItem(itemId, osWindow);
                }
                else if (itemId >= 1300 && itemId < 1400) // friends/messages
                {
                        HandleFriendItem(itemId, osWindow);
                }
                }
            }

            // Execute traps
            while (PendingTraps.TryDequeue(out string? trapType))
                ExecuteTrap(trapType!, osWindow);

            // Forward chat — route hints to APHintsWindow too
            while (PendingChat.TryDequeue(out string? msg))
            {
                APClientWindow.AddMessage(msg!);
                // AP hint messages contain "Hint:" prefix
                if (msg!.Contains("Hint:") || msg.Contains("[Hint]"))
                    APHintsWindow.AddHint(msg);
            }

            // Refresh location list when AP server confirms checks
            if (PendingLocationRefresh)
            {
                PendingLocationRefresh = false;
                APLocationWindow.Refresh();
            }
        }

private static void HandleFileItem(int itemId, OneshotWindow osWindow)
{
    var fs = Game1.windowMan.FileSystem;

    string id = itemId.ToString();

    var file = new TWMFile(
        $"ap_file_{id}",         // internal name
        $"AP File {id}",         // display name
        LaunchableWindowType.GALLERY   // or another type if needed
    );

    fs.WriteFile("/docs_foldername/", file);

    Game1.windowMan.SaveDesktopAndFileSystem();

    APWindow.AddMessage($"[File] Created AP file {id}");
}

private static void HandleWallpaperItem(int itemId, OneshotWindow osWindow)
{
    string id = itemId.ToString();

    Game1.windowMan.UnlockMan.UnlockWallpaper(id);
    Game1.windowMan.FileSystem.CreateWallpaperFile(id);

    Game1.windowMan.SaveDesktopAndFileSystem();

    APWindow.AddMessage($"[Wallpaper] Unlocked wallpaper {id}");
}

private static void HandleThemeItem(int itemId, OneshotWindow osWindow)
{

        string id = itemId.ToString();
    Game1.windowMan.UnlockMan.UnlockTheme(id);

    Game1.windowMan.FileSystem.CreateThemeFile(id);

    Game1.windowMan.SaveDesktopAndFileSystem();

    APWindow.AddMessage($"[Theme] Unlocked theme {id}");
}

private static void HandleFriendItem(int itemId, OneshotWindow osWindow)
{

        string id = itemId.ToString();
    var fs = Game1.windowMan.FileSystem;

    var file = new TWMFile(
        $"ap_contact_{id}",
        $"Contact {id}",
        LaunchableWindowType.CONTACTS
    );

    fs.WriteFile("/", file);

    Game1.windowMan.SaveDesktopAndFileSystem();

    APWindow.AddMessage($"[Friend] New contact unlocked {id}");
}

        private static void ExecuteTrap(string trapType, OneshotWindow osWindow)
        {
            Mod.Context.Logger.Log($"Archipelago: Trap — {trapType}");
            switch (trapType)
            {
                case "Spooky Popup Trap":
                    osWindow.ShowModalWindow(
                        ModalWindow.ModalType.Info,
                        "( ._.) ʕ•ᴥ•ʔ boo.",
                        null, playModalNoise: true, canAutomash: true);
                    break;
                case "Crash Trap":
                    APClientWindow.AddMessage("[Trap] Saving and closing...");
                    osWindow.gameSaveMan.MakeSave();
                    osWindow.ExitGame();
                    break;
            }
        }
    }
}
