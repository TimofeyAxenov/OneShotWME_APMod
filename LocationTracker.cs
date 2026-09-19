using System.Collections.Generic;
using OneShot.Archipelago.UI;
using OneShotMG;
using OneShotMG.src.TWM;
using OneShotMG.src.TWM.Filesystem;

namespace OneShot.Archipelago
{
    public static class LocationTracker
    {
        public const long LOC_ID_BASE  = 7_771_000;
        public const long ITEM_ID_BASE = 7_770_000;

        public static List<int>? InterceptedItems = null;

        public static Dictionary<int, long>? FlagToLocation = null;

        public static Dictionary<string, long>? AchievementToLocation = null;

        public static Dictionary<string, long>? ScriptToLocation = null;

        public static Dictionary<string, long>? TWMFileToLocation = null;

        public static Dictionary<string, long>? WallpaperToLocation = null;

        public static Dictionary<string, long>? ThemeToLocation = null;

        public static Dictionary<string, long>? ProfileToLocation = null;

        public static Dictionary<string, long>? BadgeToLocation = null;


        public const int FLAG_NORMAL_ENDING   = 147;
        public const int FLAG_LEAVE_ENDING    = 153;
        public const int FLAG_SOLSTICE_ENDING = 160;

        private static readonly System.Collections.Generic.HashSet<long> SentLocations =
            new System.Collections.Generic.HashSet<long>();

        public static void Reset() => SentLocations.Clear();

        private static void SendCheck(long locationId)
        {
            if (!ArchipelagoClient.Connected) return;
            if (SentLocations.Contains(locationId)) return;
            SentLocations.Add(locationId);
            Mod.Context.Logger.Log($"Archipelago: Sending location check {locationId}");
            ArchipelagoClient.SendLocationCheck(locationId);
            APClientWindow.AddMessage($"[Check] Sent location {locationId}");
            
        }

        public static void OnFlagSet(int flagIndex)
        {
            if (!ArchipelagoClient.Connected) return;
            if (ArchipelagoClient.ReceivingAPItem) return;

            if (FlagToLocation == null) return;
            if (FlagToLocation.TryGetValue(flagIndex, out long locId))
            {
                SendCheck(locId);
                return;
            }

            var slotData = ArchipelagoClient.SlotData;
            if (slotData == null) return;

            object goalVal;
            int goal = 1;
            if (slotData.TryGetValue("Goal", out goalVal))
            {
                if (goalVal is Newtonsoft.Json.Linq.JValue jv)
                    goal = jv.ToObject<int>();
                else if (goalVal is long l)
                    goal = (int)l;
                else if (goalVal is int i)
                    goal = i;
            }

            if (goal == 1 && flagIndex == FLAG_NORMAL_ENDING)
                ArchipelagoClient.SendGoalCompletion();
        }

        public static void OnLeaveEnding()
        {
            if (!ArchipelagoClient.Connected) return;

            var slotData = ArchipelagoClient.SlotData;
            if (slotData == null) return;

            object goalVal;
            int goal = 1;
            if (slotData.TryGetValue("Goal", out goalVal))
            {
                if (goalVal is Newtonsoft.Json.Linq.JValue jv)
                    goal = jv.ToObject<int>();
                else if (goalVal is long l)
                    goal = (int)l;
                else if (goalVal is int i)
                    goal = i;
            }

            if (goal == 0)
                ArchipelagoClient.SendGoalCompletion();
        }

        // Maps game item ID → AP location ID for base item pickup locations
        // These match the location names and IDs in Locations.py
        public static Dictionary<int, long>? ItemPickupToLocation = null;

        public static void OnItemAdded(int gameItemId)
        {
                EnsureInitialized();
            if (!ArchipelagoClient.Connected) return;
            if (!APSaveManager.IsAPModeActive) return;
            if (ArchipelagoClient.ReceivingAPItem) return;
            if (ItemPickupToLocation != null && ItemPickupToLocation.TryGetValue(gameItemId, out long locId))
                SendCheck(locId);
        }

        public static void OnScript(string script)
        {
                EnsureInitialized();
            if (!ArchipelagoClient.Connected) return;
            // Custom script handling for region selection and teleportation
            if (ScriptToLocation == null) return;
            if (ScriptToLocation.TryGetValue(script, out long locId))
                SendCheck(locId);
        }

        public static void OnAchievementUnlocked(string achievementId)
        {
                EnsureInitialized();
            if (!ArchipelagoClient.Connected) return;
            if (!APSaveManager.IsAPModeActive) return;
            if (ArchipelagoClient.ReceivingAPItem) return;
            if (AchievementToLocation == null) return;
            if (AchievementToLocation.TryGetValue(achievementId, out long locId))
            {
                if (ArchipelagoClient.IsLocationInSeed(locId)) {
                    SendCheck(locId);
                    if (GameStateManager.ReceivedButUnsent.Contains(locId)) {
                        Game1.windowMan.UnlockMan.UnlockAchievement(achievementId);

                    Game1.windowMan.SaveDesktopAndFileSystem();

                    APClientWindow.AddMessage($"[Badge] Unlocked badge '{achievementId}'");
                    } else {
                        GameStateManager.SentButUnreceived.Add(locId);    
                    }
                }
            }
        }

        public static void OnTWMFileWritten(string fileName)
        {
                EnsureInitialized();
            if (!ArchipelagoClient.Connected) return;
           if (TWMFileToLocation != null && TWMFileToLocation.TryGetValue(fileName, out long locId))
                 SendCheck(locId);
          }

        public static bool OnWallpaperUnlocked(string id)
        {
            EnsureInitialized();
            if (!APSaveManager.IsAPModeActive) return false;
            if (ArchipelagoClient.ReceivingAPItem) return false;
            if (WallpaperToLocation == null) return false;
            if (WallpaperToLocation.TryGetValue(id, out long locId))
            {
                if (ArchipelagoClient.IsLocationInSeed(locId))
                {
                    SendCheck(locId);
                    if (GameStateManager.ReceivedButUnsent.Contains(locId)) {
                        Game1.windowMan.UnlockMan.UnlockWallpaper(id);

                    Game1.windowMan.FileSystem.CreateWallpaperFile(id);

                    Game1.windowMan.SaveDesktopAndFileSystem();

                    APClientWindow.AddMessage($"[Wallpaper] Unlocked wallpaper '{id}'");
                    } else {
                        GameStateManager.SentButUnreceived.Add(locId);
                    }
                    return true;
                }
            }
            return false;
        }

        public static bool OnThemeUnlocked(string id)
        {
            EnsureInitialized();
            if (!APSaveManager.IsAPModeActive) return false;
            if (ArchipelagoClient.ReceivingAPItem) return false;
            if (ThemeToLocation == null) return false;
            if (ThemeToLocation.TryGetValue(id, out long locId))
            {
                if (ArchipelagoClient.IsLocationInSeed(locId)) {
                    SendCheck(locId);
                    if (GameStateManager.ReceivedButUnsent.Contains(locId)) {
                        Game1.windowMan.UnlockMan.UnlockTheme(id);

                        Game1.windowMan.FileSystem.CreateThemeFile(id);

                        Game1.windowMan.SaveDesktopAndFileSystem();

                        APClientWindow.AddMessage($"[Theme] Unlocked theme '{id}'");
                    } else {
                        GameStateManager.SentButUnreceived.Add(locId);
                    }
                    return true;
                }
            }
            return false;
        }

        public static bool OnProfileUnlocked(string id)
        {
            EnsureInitialized();
            if (!APSaveManager.IsAPModeActive) return false;
            if (ArchipelagoClient.ReceivingAPItem) return false;
            if (ProfileToLocation == null) return false;
            if (ProfileToLocation.TryGetValue(id, out long locId))
            {
                if (ArchipelagoClient.IsLocationInSeed(locId))
                {
                    SendCheck(locId);
                    if (GameStateManager.ReceivedButUnsent.Contains(locId)) {
                        Game1.windowMan.UnlockMan.UnlockProfile(id);

                        Game1.windowMan.SaveDesktopAndFileSystem();

                        APClientWindow.AddMessage($"[Profile] Unlocked profile '{id}'");
                    } else {
                        GameStateManager.SentButUnreceived.Add(locId);
                    }
                    return true;
                }
            }
            return false;
        }

        public static bool OnBadgeUnlocked(string id)
        {
            EnsureInitialized();
            if (!APSaveManager.IsAPModeActive) return false;
            if (ArchipelagoClient.ReceivingAPItem) return false;
            if (BadgeToLocation == null) return false;
            if (BadgeToLocation.TryGetValue(id, out long locId))
            {
                if (ArchipelagoClient.IsLocationInSeed(locId))
                {
                    SendCheck(locId);
                    if (GameStateManager.ReceivedButUnsent.Contains(locId)) {
                        Game1.windowMan.UnlockMan.UnlockAchievement(id);

                    Game1.windowMan.SaveDesktopAndFileSystem();

                    APClientWindow.AddMessage($"[Badge] Unlocked badge '{id}'");
                    } else {
                        GameStateManager.SentButUnreceived.Add(locId);    
                    }
                    return true;
                }
            }
            return false;
        }

        public static bool OnFileWritten(string fileName)
        {
            EnsureInitialized();
            if (!APSaveManager.IsAPModeActive) return false;
            if (ArchipelagoClient.ReceivingAPItem) return false;
            if (TWMFileToLocation == null) return false;
            if (TWMFileToLocation.TryGetValue(fileName, out long locId))
            {
                if (ArchipelagoClient.IsLocationInSeed(locId))
                {
                    SendCheck(locId);
                    return true;
                }
            }
            return false;
        }

        public static long GetItemLocationId(int itemId)
{
    if (ItemPickupToLocation != null && ItemPickupToLocation.TryGetValue(itemId, out var id))
        return id;

    return -1;
}

        private static bool _initialized = false;
private static void EnsureInitialized()
{
    if (_initialized) return;
    try {
    _initialized = true;
    InterceptedItems = new List<int> {};
    FlagToLocation = new Dictionary<int, long> {
            // Wallpaper flags (read-only proxies, never set by game events, but kept for safety)
            { 401, LOC_ID_BASE + 300 }, { 402, LOC_ID_BASE + 301 }, { 403, LOC_ID_BASE + 302 },
            { 404, LOC_ID_BASE + 303 }, { 405, LOC_ID_BASE + 304 }, { 406, LOC_ID_BASE + 305 },
            { 407, LOC_ID_BASE + 306 }, { 408, LOC_ID_BASE + 307 }, { 409, LOC_ID_BASE + 308 },
            { 410, LOC_ID_BASE + 309 }, { 411, LOC_ID_BASE + 310 }, { 412, LOC_ID_BASE + 311 },
            // Profile flags (read-only proxies, never set by game events)
            // flag -> AP location: see FlagManager constants and AP Locations.py
            { 426, LOC_ID_BASE + 400 }, { 427, LOC_ID_BASE + 401 }, { 428, LOC_ID_BASE + 402 },
            { 429, LOC_ID_BASE + 410 }, { 430, LOC_ID_BASE + 403 }, { 431, LOC_ID_BASE + 405 },
            { 432, LOC_ID_BASE + 404 }, { 433, LOC_ID_BASE + 406 }, { 435, LOC_ID_BASE + 408 },
            { 436, LOC_ID_BASE + 407 }, { 438, LOC_ID_BASE + 409 }, { 439, LOC_ID_BASE + 411 },
            { 440, LOC_ID_BASE + 412 }, { 441, LOC_ID_BASE + 412 }, { 442, LOC_ID_BASE + 412 },
            { 443, LOC_ID_BASE + 412 }, { 444, LOC_ID_BASE + 412 }, { 445, LOC_ID_BASE + 412 },
            { 434, LOC_ID_BASE + 413 },
            // Themes (read-only proxies)
            { 461, LOC_ID_BASE + 500 }, { 462, LOC_ID_BASE + 501 }, { 463, LOC_ID_BASE + 502 },
            { 464, LOC_ID_BASE + 503 }, { 465, LOC_ID_BASE + 504 }, { 466, LOC_ID_BASE + 505 },
            { 467, LOC_ID_BASE + 506 }, { 468, LOC_ID_BASE + 507 }, { 469, LOC_ID_BASE + 508 },
        };
    AchievementToLocation = new Dictionary<string, long> {
            { "CHAOTIC_EVIL",      LOC_ID_BASE + 600 },
            { "SHOCK",             LOC_ID_BASE + 601 },
            { "EXTREME_BARTERING", LOC_ID_BASE + 602 },
            { "RAM_WHISPERER",     LOC_ID_BASE + 603 },
            { "WE_RIDE_AT_DAWN",   LOC_ID_BASE + 604 },
            { "SECRET",            LOC_ID_BASE + 605 },
            { "BOOKWORM",          LOC_ID_BASE + 606 },
            { "PANCAKES",          LOC_ID_BASE + 607 },
            { "REBIRTH",           LOC_ID_BASE + 608 },
            { "ONESHOT",           LOC_ID_BASE + 609 },
        };
    ScriptToLocation = new Dictionary<string, long> {
            { "safe_puzzle_write", LOC_ID_BASE + 200 },
            { "Script.copy_journal", LOC_ID_BASE + 201 },
            { "Script.put_key_in_box(1)", LOC_ID_BASE + 202 },
            { "Script.put_key_in_box(2)", LOC_ID_BASE + 203 },
            { "Script.put_key_in_box(3)", LOC_ID_BASE + 204 },
        };
    WallpaperToLocation = new Dictionary<string, long> {
            { "lamp",        LOC_ID_BASE + 300 },
            { "factory",     LOC_ID_BASE + 301 },
            { "navigate",    LOC_ID_BASE + 302 },
            { "courtyard",   LOC_ID_BASE + 303 },
            { "ruins",       LOC_ID_BASE + 304 },
            { "catwalks",    LOC_ID_BASE + 305 },
            { "library",     LOC_ID_BASE + 306 },
            { "secret",      LOC_ID_BASE + 307 },
            { "lamplighter", LOC_ID_BASE + 308 },
            { "cafe",        LOC_ID_BASE + 309 },
            { "plant",       LOC_ID_BASE + 310 },
            { "tower",       LOC_ID_BASE + 311 },
        };
    ThemeToLocation = new Dictionary<string, long> {
            { "blue",    LOC_ID_BASE + 500 },
            { "teal",    LOC_ID_BASE + 501 },
            { "yellow",  LOC_ID_BASE + 502 },
            { "green",   LOC_ID_BASE + 503 },
            { "red",     LOC_ID_BASE + 504 },
            { "pink",    LOC_ID_BASE + 505 },
            { "orange",  LOC_ID_BASE + 506 },
            { "white",   LOC_ID_BASE + 507 },
            { "rainbow", LOC_ID_BASE + 508 },
        };
    ProfileToLocation = new Dictionary<string, long> {
            { "prophetbot",  LOC_ID_BASE + 400 },
            { "silver",      LOC_ID_BASE + 401 },
            { "rowbot",      LOC_ID_BASE + 402 },
            { "magpie",      LOC_ID_BASE + 403 },
            { "alula",       LOC_ID_BASE + 404 },
            { "calamus",     LOC_ID_BASE + 405 },
            { "maize",       LOC_ID_BASE + 406 },
            { "mason",       LOC_ID_BASE + 407 },
            { "watcher",     LOC_ID_BASE + 408 },
            { "kelvin",      LOC_ID_BASE + 409 },
            { "shepherd",    LOC_ID_BASE + 410 },
            { "kip",         LOC_ID_BASE + 411 },
            { "george1",     LOC_ID_BASE + 412 },
            { "george2",     LOC_ID_BASE + 412 },
            { "george3",     LOC_ID_BASE + 412 },
            { "george4",     LOC_ID_BASE + 412 },
            { "george5",     LOC_ID_BASE + 412 },
            { "george6",     LOC_ID_BASE + 412 },
        };
    BadgeToLocation = new Dictionary<string, long> {
            { "CHAOTIC_EVIL",      LOC_ID_BASE + 600 },
            { "SHOCK",             LOC_ID_BASE + 601 },
            { "EXTREME_BARTERING", LOC_ID_BASE + 602 },
            { "RAM_WHISPERER",     LOC_ID_BASE + 603 },
            { "WE_RIDE_AT_DAWN",   LOC_ID_BASE + 604 },
            { "SECRET",            LOC_ID_BASE + 605 },
            { "BOOKWORM",          LOC_ID_BASE + 606 },
            { "PANCAKES",          LOC_ID_BASE + 607 },
            { "REBIRTH",           LOC_ID_BASE + 608 },
            { "ONESHOT",           LOC_ID_BASE + 609 },
        };
    TWMFileToLocation = new Dictionary<string, long> {
        };
    ItemPickupToLocation = new Dictionary<int, long> {
            // Starter House
            { 4,  LOC_ID_BASE + 1  },  // Left Room (branch)
            { 7,  LOC_ID_BASE + 3  },  // Floorboards (basement key)
            { 3, LOC_ID_BASE + 2  },  // Fridge (bottle of alcohol)
            { 1,  LOC_ID_BASE + 4  },  // Basement (lightbulb)
            { 5, LOC_ID_BASE + 100 }, // Wet Branch
            { 6, LOC_ID_BASE + 101 }, // Torch
            { 11, LOC_ID_BASE + 102 }, // Empty Bottle

            // Barrens
            { 8,  LOC_ID_BASE + 13 },  // Mines (Camera)
            { 9, LOC_ID_BASE + 12 },  // Silver House (Screwdriver)
            { 12, LOC_ID_BASE + 11 },  // Box (Broken Battery)
            { 19, LOC_ID_BASE + 10 },  // House Floor (Metal Rod)
            { 20, LOC_ID_BASE + 17 },  // Sponge
            { 21, LOC_ID_BASE + 18 },  // Empty Syringe
            { 23, LOC_ID_BASE + 19 },  // Amber
            { 24, LOC_ID_BASE + 14 },  // Journal
            { 47, LOC_ID_BASE + 15 },  // Gas Mask
            { 48, LOC_ID_BASE + 16 },  // Rubber Gloves
            { 13, LOC_ID_BASE + 110 }, // Empty Battery
            { 14, LOC_ID_BASE + 111 }, // Charged Battery
            { 15, LOC_ID_BASE + 106 }, // Bottle of Smoke
            { 16, LOC_ID_BASE + 107 }, // Bottle of Acid
            { 17, LOC_ID_BASE + 108 }, // Wet sponge
            { 18, LOC_ID_BASE + 103 }, // Crowbar
            { 22, LOC_ID_BASE + 105 }, // Filled Syringe
            { 10, LOC_ID_BASE + 109 }, // Lens

            // Glen
            { 25, LOC_ID_BASE + 35 },  // Feather
            { 26, LOC_ID_BASE + 32 },  // Bottle of Dye
            { 27, LOC_ID_BASE + 34 },  // Tube of Water
            { 28, LOC_ID_BASE + 30 },  // Seed
            { 50, LOC_ID_BASE + 33 },  // Novelty T-Shirt
            { 29, LOC_ID_BASE + 31 },  // Wool
            { 30, LOC_ID_BASE + 112 }, // Feather Pen

            // Refuge
            { 31, LOC_ID_BASE + 74 }, // Die
            { 36, LOC_ID_BASE + 52 }, // Magnets
            { 37, LOC_ID_BASE + 54 }, // Metal Can
            { 38, LOC_ID_BASE + 50 }, // Scissors
            { 39, LOC_ID_BASE + 55 }, // Weird Film
            { 40, LOC_ID_BASE + 56 }, // Concave Lens
            { 41, LOC_ID_BASE + 57 }, // Convex Lens
            { 42, LOC_ID_BASE + 58 }, // Thin Lens
            { 43, LOC_ID_BASE + 59 }, // Thick Lens
            { 46, LOC_ID_BASE + 61 }, // Kips Library Card
            { 44, LOC_ID_BASE + 60 }, // Glitter Glue
            { 61, LOC_ID_BASE + 62 }, // Photo of Niko (1)
            { 62, LOC_ID_BASE + 63 }, // Photo of Niko (2)
            { 63, LOC_ID_BASE + 64 }, // Photo of Niko (3)
            { 64, LOC_ID_BASE + 65 }, // Photo of Niko (4)
            { 65, LOC_ID_BASE + 66 }, // Photo of Niko (5)
            { 66, LOC_ID_BASE + 68 }, // Photo of Niko (Blink)
            { 67, LOC_ID_BASE + 67 }, // Photo of Niko (6)
            { 68, LOC_ID_BASE + 69 }, // Photo of Niko (7)
            { 69, LOC_ID_BASE + 70 }, // Photo of Niko (8)
            { 70, LOC_ID_BASE + 71 }, // Photo of Niko (9)
            { 56, LOC_ID_BASE + 73 }, // Water Pill
            { 55, LOC_ID_BASE + 51 }, // Dirt

            // Tower, Solstice
//            { 75, LOC_ID_BASE + 80 }, // Memory Disk
//            { 76, LOC_ID_BASE + 81 }, // Memory Disk (Backup)
//            { 78, LOC_ID_BASE + 82 }, // Music Box
//            { 77, LOC_ID_BASE + 83 }, // Charged Battery (Green)
        };
    }
    catch (System.Exception ex)
    {
            Mod.Context.Logger.Log($"LocationTracker init failed: {ex.Message}\n{ex.StackTrace}");
    }
}
    }
}
