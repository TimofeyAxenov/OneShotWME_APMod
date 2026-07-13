using OneShotMG.src.TWM;
using WorldMachineLoader.API.Core;

namespace OneShot.Archipelago.Patches
{
    public class UnlockPatches
    {
        [GamePatch(typeof(UnlockManager), "UnlockWallpaper", PatchType.Prefix, typeof(string))]
        public static bool UnlockWallpaper_Prefix(string id)
        {
            if (!APSaveManager.IsAPModeActive) return true;

            if (LocationTracker.OnWallpaperUnlocked(id))
            {
                Mod.Context.Logger.Log($"Archipelago: Intercepted wallpaper unlock '{id}' — sending check");
                return false;
            }

            return true;
        }

        [GamePatch(typeof(UnlockManager), "UnlockTheme", PatchType.Prefix, typeof(string))]
        public static bool UnlockTheme_Prefix(string id)
        {
            if (!APSaveManager.IsAPModeActive) return true;

            if (LocationTracker.OnThemeUnlocked(id))
            {
                Mod.Context.Logger.Log($"Archipelago: Intercepted theme unlock '{id}' — sending check");
                return false;
            }

            return true;
        }

        [GamePatch(typeof(UnlockManager), "UnlockProfile", PatchType.Prefix, typeof(string))]
        public static bool UnlockProfile_Prefix(string id)
        {
            if (!APSaveManager.IsAPModeActive) return true;

            if (LocationTracker.OnProfileUnlocked(id))
            {
                Mod.Context.Logger.Log($"Archipelago: Intercepted profile unlock '{id}' — sending check");
                return false;
            }

            return true;
        }

        [GamePatch(typeof(UnlockManager), "UnlockAchievement", PatchType.Prefix, typeof(string))]
        public static bool UnlockAchievement_Prefix(string id)
        {
            if (!APSaveManager.IsAPModeActive) return true;

            LocationTracker.OnAchievementUnlocked(id);

            if (LocationTracker.OnBadgeUnlocked(id))
            {
                Mod.Context.Logger.Log($"Archipelago: Intercepted badge unlock '{id}' — blocking, check sent");
                return false;
            }

            return true;
        }
    }
}
