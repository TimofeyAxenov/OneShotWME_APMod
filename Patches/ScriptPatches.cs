using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using OneShotMG;
using OneShotMG.src.Entities;
using OneShotMG.src.TWM;
using OneShotMG.src.TWM.Filesystem;
using WorldMachineLoader.API;
using WorldMachineLoader.API.Core;

namespace OneShot.Archipelago.Patches
{
    public class ScriptPatches
    {
        [GamePatch(typeof(ScriptParser), "HandleScript", PatchType.Prefix,
            typeof(OneshotWindow), typeof(string), typeof(EventRunner), typeof(int))]
        public static bool HandleScript_Prefix(string script)
        {
            if (!APSaveManager.IsAPModeActive)
                return true;

            if (script.StartsWith("Script.put_key_in_box") ||
                script == "Script.create_boxes" ||
                script == "Script.clear_boxes")
            {
                Mod.Context.Logger.Log($"Archipelago: Blocked classical portal script: {script}");
                return false;
            }

            if (script == "quit_game_bed" || script == "quit_game_no_save")
            {
                var osWindow = GameStateManager.GetWindow();
                if (osWindow?.tileMapMan != null)
                {
                    var mapIdField = osWindow.tileMapMan.GetType().GetField("currentMapId",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                    if (mapIdField != null)
                    {
                        int mapId = (int)mapIdField.GetValue(osWindow.tileMapMan);
                        if (mapId == 63)
                        {
                            Mod.Context.Logger.Log("Archipelago: Map 63 quit detected — sending leave ending goal");
                            LocationTracker.OnLeaveEnding();
                        }
                    }
                }
            }

            return true;
        }

        [GamePatch(typeof(ScriptParser), "HandleScript", PatchType.Postfix,
            typeof(OneshotWindow), typeof(string), typeof(EventRunner), typeof(int))]
        public static void HandleScript_Postfix(string script)
        {
            LocationTracker.OnScript(script);
        }

        [GamePatch(typeof(TWMFilesystem), "WriteFile", PatchType.Prefix,
            typeof(string), typeof(TWMFile))]
        public static bool WriteFile_Prefix(string path, TWMFile file)
        {
            if (!APSaveManager.IsAPModeActive) return true;

            if (file.name == "fakesave_filename") return true;

            if (LocationTracker.OnFileWritten(file.name))
            {
                Mod.Context.Logger.Log($"Archipelago: Intercepted file write '{file.name}' — sending check");
                return false;
            }

            return true;
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

        public static void TeleportTo(int mapId, int tileX, int tileY)
        {
            var osWindow = Game1.windowMan?.GetOneshotWindow();
            if (osWindow == null) return;
            osWindow.tileMapMan.ChangeMap(mapId, tileX, tileY, 0.5f, (OneShotMG.src.Entity.Direction)2);
        }

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
