using OneShotMG;
using OneShotMG.src.Entities;
using OneShotMG.src.TWM;
using OneShotMG.src.TWM.Filesystem;
using WorldMachineLoader.API.Core;
//using OneShot.Archipelago;

namespace OneShot.Archipelago.Patches
{
    public class ScriptPatches
    {
        [GamePatch(typeof(ScriptParser), "HandleScript", PatchType.Postfix,
            typeof(OneshotWindow), typeof(string), typeof(EventRunner), typeof(int))]
        public static void HandleScript_Postfix(string script)
        {
            LocationTracker.OnScript(script);
        }

        [GamePatch(typeof(UnlockManager), "UnlockAchievement", PatchType.Postfix, typeof(string))]
        public static void UnlockAchievement_Postfix(string id)
        {
            LocationTracker.OnAchievementUnlocked(id);
        }

        [GamePatch(typeof(TWMFilesystem), "WriteFile", PatchType.Postfix,
            typeof(string), typeof(TWMFile))]
        public static void WriteFile_Postfix(TWMFile file)
        {

                if (!IsGameReady())
                        return;
            LocationTracker.OnTWMFileWritten(file.name);
        }

        private static bool IsGameReady()
{
    return Game1.windowMan != null
        && Game1.windowMan.FileSystem != null
        && Game1.windowMan.Desktop != null;
}
    }

    public static class WindowManagerHelper
    {
        public static void ReloadFilesystemAndDesktop()
        {
            if (Game1.windowMan == null) return;
            var method = typeof(OneShotMG.src.TWM.WindowManager)
                .GetMethod("LoadFilesystemAndDesktop",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
            method?.Invoke(Game1.windowMan, null);
        }
    }
}
