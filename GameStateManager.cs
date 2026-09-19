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

        public static List<long> ReceivedButUnsent = new List<long>();
        public static List<long>? SentButUnreceived = new List<long>();

        public static List<int> PhotoIds = new List<int> {61, 62, 63, 64, 65, 67, 68, 69, 70};

        public static OneshotWindow? GetWindow() => Game1.windowMan?.GetOneshotWindow();

        public static Random random = new Random();

        public static void Tick()
        {
                try {
//                        Mod.Context.Logger.Log("[AP DEBUG] Tick entered");

        if (ArchipelagoClient.Session == null)
        {
//            Mod.Context.Logger.Log("[AP DEBUG] AP client null");
            return;
        }

        if (Game1.windowMan == null)
        {
//            Mod.Context.Logger.Log("[AP DEBUG] windowMan null");
            return;
        }

            var osWindow = GetWindow();
            if (osWindow == null) return;

            // Give items
            while (PendingItems.TryDequeue(out int itemId))
            {
                var ItemInterceptedBefore = LocationTracker.InterceptedItems?.Contains(itemId) ?? false;
                if (!ItemInterceptedBefore) {
                Mod.Context.Logger.Log($"Archipelago: Applying item ID {itemId}");
                ArchipelagoClient.ReceivingAPItem = true;
                if (itemId >= 1 && itemId <= 78 && itemId != 2)
                {
                        if (itemId == 45) {
                                int PhotoIdIndex = random.Next(PhotoIds.Count);
                                var PhotoId = PhotoIds[PhotoIdIndex];
                                osWindow.menuMan.ItemMan.AddItem(PhotoId);
                                PhotoIds.RemoveAt(PhotoIdIndex);
                        }
                        else {
                                osWindow.menuMan.ItemMan.AddItem(itemId);
                        }
                }
                else if (itemId >= 1000 && itemId < 1100) // FILE items
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
                else if (itemId >= 1400 && itemId < 1500) // badges
                {
                        HandleBadgeItem(itemId, osWindow);
                }
                ArchipelagoClient.ReceivingAPItem = false;
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
            }}
        catch (Exception ex)
    {
        Mod.Context.Logger.Log($"[AP ERROR] Tick crash: {ex}");
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

    APClientWindow.AddMessage($"[File] Created AP file {id}");
}

private static readonly Dictionary<int, string> WallpaperItemMap = new Dictionary<int, string>
{
    { 1100, "lamp" },
    { 1101, "factory" },
    { 1102, "navigate" },
    { 1103, "courtyard" },
    { 1104, "ruins" },
    { 1105, "catwalks" },
    { 1106, "library" },
    { 1107, "secret" },
    { 1108, "lamplighter" },
    { 1109, "cafe" },
    { 1110, "plant" },
    { 1111, "tower" },
};

private static readonly Dictionary<int, string> ThemeItemMap = new Dictionary<int, string>
{
    { 1200, "blue" },
    { 1201, "teal" },
    { 1202, "green" },
    { 1203, "yellow" },
    { 1204, "red" },
    { 1205, "pink" },
    { 1206, "orange" },
    { 1207, "white" },
    { 1208, "rainbow" },
};

private static readonly Dictionary<int, string> ProfileItemMap = new Dictionary<int, string>
{
    { 1300, "prophetbot" },
    { 1301, "silver" },
    { 1302, "rowbot" },
    { 1303, "shepherd" },
    { 1304, "magpie" },
    { 1305, "calamus" },
    { 1306, "alula" },
    { 1307, "maize" },
    { 1308, "ling" },
    { 1309, "watcher" },
    { 1310, "mason" },
    { 1311, "lamplighter" },
    { 1312, "kelvin" },
    { 1313, "kip" },
    { 1314, "george1" },
};

private static string MapItemId(int itemId, Dictionary<int, string> map, string label)
{
    if (map.TryGetValue(itemId, out string? gameId))
        return gameId;
    APClientWindow.AddMessage($"[{label}] Warning: no game string mapping for item {itemId}");
    return itemId.ToString();
}

private static void HandleWallpaperItem(int itemId, OneshotWindow osWindow)
{

    string gameId = MapItemId(itemId, WallpaperItemMap, "Wallpaper");

    long locId = LocationTracker.WallpaperToLocation[gameId];

    if (SentButUnreceived.Contains(locId)) {
        Game1.windowMan.UnlockMan.UnlockWallpaper(gameId);

        Game1.windowMan.FileSystem.CreateWallpaperFile(gameId);

        Game1.windowMan.SaveDesktopAndFileSystem();

        APClientWindow.AddMessage($"[Wallpaper] Unlocked wallpaper '{gameId}'");

        SentButUnreceived.Remove(locId);
    } else {
        ReceivedButUnsent.Add(locId);
    }

    
}

private static void HandleThemeItem(int itemId, OneshotWindow osWindow)
{
    string gameId = MapItemId(itemId, ThemeItemMap, "Theme");

    long locId = LocationTracker.ThemeToLocation[gameId];

    if (SentButUnreceived.Contains(locId)) {
        Game1.windowMan.UnlockMan.UnlockTheme(gameId);

        Game1.windowMan.FileSystem.CreateThemeFile(gameId);

        Game1.windowMan.SaveDesktopAndFileSystem();

        APClientWindow.AddMessage($"[Theme] Unlocked theme '{gameId}'");

        SentButUnreceived.Remove(locId);
    } else {
        ReceivedButUnsent.Add(locId);
    }

    
}

private static void HandleFriendItem(int itemId, OneshotWindow osWindow)
{
    string gameId = MapItemId(itemId, ProfileItemMap, "Profile");

    long locId = LocationTracker.ProfileToLocation[gameId];

    if (SentButUnreceived.Contains(locId)) {

        Game1.windowMan.UnlockMan.UnlockProfile(gameId);

        Game1.windowMan.SaveDesktopAndFileSystem();

        APClientWindow.AddMessage($"[Profile] Unlocked profile '{gameId}'");
        
        SentButUnreceived.Remove(locId);
    } else {
        ReceivedButUnsent.Add(locId);
    }
}

private static readonly Dictionary<int, string> BadgeItemMap = new Dictionary<int, string>
{
    { 1400, "CHAOTIC_EVIL" },
    { 1401, "SHOCK" },
    { 1402, "EXTREME_BARTERING" },
    { 1403, "RAM_WHISPERER" },
    { 1404, "WE_RIDE_AT_DAWN" },
    { 1405, "SECRET" },
    { 1406, "BOOKWORM" },
    { 1407, "PANCAKES" },
    { 1408, "REBIRTH" },
    { 1409, "ONESHOT" },
};

private static void HandleBadgeItem(int itemId, OneshotWindow osWindow)
{
    string badgeId = itemId.ToString();
    if (BadgeItemMap.TryGetValue(itemId, out string? mappedId))
        badgeId = mappedId;

    long locId = LocationTracker.BadgeToLocation[badgeId];

    if (SentButUnreceived.Contains(locId)) {
        Game1.windowMan.UnlockMan.UnlockAchievement(badgeId);

        Game1.windowMan.SaveDesktopAndFileSystem();

        APClientWindow.AddMessage($"[Badge] Unlocked badge '{badgeId}'");

        SentButUnreceived.Remove(locId);
    } else {
        ReceivedButUnsent.Add(locId);
    }
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
