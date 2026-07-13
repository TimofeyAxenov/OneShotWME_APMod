using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using OneShotMG;
using OneShotMG.src.Entities;
using OneShotMG.src.TWM;
using OneShotMG.src.TWM.Filesystem;
using WorldMachineLoader.API.Core;

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
        Mod.Context.Logger.Log($"Archipelago: Writing File to Filesystem: {file.name}");
        if (file.name == "fakesave_filename")
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
        private static string SaveFolder => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OneShotWME");

        public static void ReloadDesktop()
        {
            var wm = Game1.windowMan;
            if (wm == null) return;

            string path = Path.Combine(SaveFolder, "desktop.dat");
            if (!File.Exists(path)) return;

            try
            {
                var dtData = JsonConvert.DeserializeObject<FilesystemSaveManager.DesktopSaveData>(
                    File.ReadAllText(path));
                if (dtData == null) return;

                wm.Desktop.SetLayout(dtData);
                wm.UnlockMan?.LoadSaveData(dtData);

                if (dtData.currentTheme != null)
                {
                    var themeField = typeof(WindowManager).GetField("theme",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    var theme = wm.GetThemeById(dtData.currentTheme);
                    if (themeField != null && theme != null)
                        themeField.SetValue(wm, theme);
                }

                wm.FileSystem?.CreateWallpaperFiles(
                    wm.UnlockMan?.UnlockedWallpapers ?? new List<string>());
                wm.FileSystem?.CreateThemeFiles(
                    wm.UnlockMan?.UnlockedThemes ?? new List<string>());

                Game1.gMan.Gamma = dtData.Gamma;
                Game1.soundMan.BGMVol = dtData.BGMVolume;
                Game1.soundMan.SFXVol = dtData.SFXVolume;

                Mod.Context.Logger.Log("Archipelago: Desktop reloaded from disk.");
            }
            catch (Exception ex)
            {
                Mod.Context.Logger.Log($"Archipelago: Desktop reload failed: {ex.Message}");
            }
        }
    }
}
