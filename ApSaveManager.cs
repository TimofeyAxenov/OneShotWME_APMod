using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using OneShotMG;
using OneShotMG.src.TWM;
using OneShotMG.src.TWM.Filesystem;

namespace OneShot.Archipelago
{
    public static class APSaveManager
    {
        public static readonly string APSaveFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OneShotWME", "archipelago"
        );

        public static readonly string VanillaSaveFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OneShotWME"
        );

        private const string SAVE_INDEX_FILE = "ap_saves.json";
        private const string ACTIVE_KEY_FILE = "ap_active_key.txt";

        private static readonly string[] VanillaFiles = {
            "save.dat", "p-settings.dat", "desktop.dat", "fs.dat"
        };

        public static string? ActiveSaveKey { get; private set; }
        public static bool IsAPModeActive => ActiveSaveKey != null;

        private static string ActiveKeyFilePath => Path.Combine(APSaveFolder, ACTIVE_KEY_FILE);
        static APSaveManager()
        {
            Directory.CreateDirectory(APSaveFolder);
        }

        public class APSaveInfo
        {
            public string Key { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string slotName { get; set; } = "";
            public string Host { get; set; } = "";
            public int Port { get; set; } = 38281;
            public string? Password { get; set; }
            public string Seed { get; set; } = "";
            public DateTime LastPlayed { get; set; } = DateTime.Now;

            public string GetDisplayName() => string.IsNullOrEmpty(DisplayName) ? slotName : DisplayName;
        }

        public static List<APSaveInfo> GetAllSaves()
        {
            string indexPath = Path.Combine(APSaveFolder, SAVE_INDEX_FILE);
            if (!File.Exists(indexPath)) return new List<APSaveInfo>();
            try
            {
                return JsonConvert.DeserializeObject<List<APSaveInfo>>(
                    File.ReadAllText(indexPath)) ?? new List<APSaveInfo>();
            }
            catch { return new List<APSaveInfo>(); }
        }

        private static void SaveIndex(List<APSaveInfo> saves)
        {
            File.WriteAllText(
                Path.Combine(APSaveFolder, SAVE_INDEX_FILE),
                JsonConvert.SerializeObject(saves, Formatting.Indented)
            );
        }

        public static string MakeSaveKey(string seed, string slotName)
            => $"{seed}_{slotName}".Replace(" ", "_");

        public static void ActivateAPSave(APSaveInfo info)
        {
            string? prevKey = ActiveSaveKey;

            if (prevKey != null)
                BackupAPFiles(prevKey);

            if (prevKey == null)
                BackupVanillaFiles();

            ActiveSaveKey = info.Key;
            info.LastPlayed = DateTime.Now;

            var saves = GetAllSaves();
            int idx = saves.FindIndex(s => s.Key == info.Key);
            if (idx >= 0) saves[idx] = info;
            else saves.Add(info);

            SaveIndex(saves);

            File.WriteAllText(ActiveKeyFilePath, info.Key);

            RestoreAPFiles(info.Key);
            RestartEntireSystem();
        }

        public static void DeactivateAPSave()
        {
            if (ActiveSaveKey != null)
                BackupAPFiles(ActiveSaveKey);

            ActiveSaveKey = null;
            if (File.Exists(ActiveKeyFilePath))
                File.Delete(ActiveKeyFilePath);

            RestoreVanillaFiles();
            RestartEntireSystem();
        }

        public static bool IsOneShotRunning()
        {
            var osWindow = Game1.windowMan?.GetOneshotWindow();
            return osWindow != null && !osWindow.titleScreenMan!.IsOpen();
        }

        private static void RestartEntireSystem()
        {
            var osWindow = Game1.windowMan?.GetOneshotWindow();
            if (osWindow != null)
            {
                if (!osWindow.titleScreenMan!.IsOpen())
                    Mod.Context.Logger.Log("Archipelago: Closing in-game OneShot for save switch.");

                Game1.windowMan!.RemoveWindow(osWindow);
            }

            Patches.WindowManagerHelper.ReloadDesktop();
        }

        public static string? GetActiveSaveDisplayName()
        {
            if (ActiveSaveKey == null) return null;
            var saves = GetAllSaves();
            var info = saves.Find(s => s.Key == ActiveSaveKey);
            return info?.GetDisplayName();
        }

        public static void TryResumeActiveSession()
        {
            if (!File.Exists(ActiveKeyFilePath)) return;
            string key = File.ReadAllText(ActiveKeyFilePath).Trim();
            if (string.IsNullOrEmpty(key)) return;

            var saves = GetAllSaves();
            if (saves.Exists(s => s.Key == key))
            {
                ActiveSaveKey = key;
                Mod.Context.Logger.Log($"Archipelago: Resumed active session {key}");
            }
            else
            {
                File.Delete(ActiveKeyFilePath);
                Mod.Context.Logger.Log($"Archipelago: Stale active key deleted ({key})");
            }
        }

        public static void DeleteSave(string key)
        {
            foreach (string fileName in VanillaFiles)
            {
                string apFile = Path.Combine(APSaveFolder, $"{key}_{fileName}");
                if (File.Exists(apFile)) File.Delete(apFile);
            }

            var saves = GetAllSaves();
            saves.RemoveAll(s => s.Key == key);
            SaveIndex(saves);
            Mod.Context.Logger.Log($"Archipelago: Deleted save {key}");
        }

        private static void BackupVanillaFiles()
        {
            foreach (string fileName in VanillaFiles)
            {
                string src  = Path.Combine(VanillaSaveFolder, fileName);
                string dest = Path.Combine(APSaveFolder, $"vanilla_backup_{fileName}");
                if (File.Exists(src)) File.Copy(src, dest, overwrite: true);
            }
        }

        private static void RestoreVanillaFiles()
        {
            foreach (string fileName in VanillaFiles)
            {
                string src  = Path.Combine(APSaveFolder, $"vanilla_backup_{fileName}");
                string dest = Path.Combine(VanillaSaveFolder, fileName);
                if (File.Exists(dest)) File.Delete(dest);
                if (File.Exists(src)) File.Copy(src, dest, overwrite: true);
            }
        }

        private static void RestoreAPFiles(string key)
        {
            foreach (string fileName in VanillaFiles)
            {
                string dest = Path.Combine(VanillaSaveFolder, fileName);
                if (File.Exists(dest)) File.Delete(dest);

                string src = Path.Combine(APSaveFolder, $"{key}_{fileName}");

                if (fileName == "desktop.dat" && !File.Exists(src))
                {
                    CreateDefaultDesktop(dest);
                    continue;
                }

                if (File.Exists(src))
                {
                    File.Copy(src, dest, overwrite: true);
                }
                else
                {
                    string fallback = Path.Combine(APSaveFolder, $"vanilla_backup_{fileName}");
                    if (File.Exists(fallback))
                        File.Copy(fallback, dest, overwrite: true);
                }
            }
        }

        private static void CreateDefaultDesktop(string dest)
        {
            var dt = new FilesystemSaveManager.DesktopSaveData
            {
                iconPositions = new Dictionary<string, Vec2>(),
                currentWallpaper = "default",
                currentTheme = "purple",
                unlockedWallpapers = new List<string>
                    { "default", "barrens", "glen", "refuge", "niko", "title", "planet", "black" },
                unlockedThemes = new List<string> { "purple" },
                unlockedMusicTracks = new List<string>(),
                unlockedCgs = new List<string>(),
                unlockedAchievements = new List<string>(),
                unlockedProfiles = new List<string>(),
                tutorialCompleted = true,
                Gamma = 100,
                BGMVolume = 100,
                SFXVolume = 100,
                inSolstice = false,
            };
            string json = JsonConvert.SerializeObject(dt, Formatting.None);
            File.WriteAllText(dest, json);
        }

        public static void BackupAPFiles(string key)
        {
            foreach (string fileName in VanillaFiles)
            {
                string src  = Path.Combine(VanillaSaveFolder, fileName);
                string dest = Path.Combine(APSaveFolder, $"{key}_{fileName}");
                if (File.Exists(src)) File.Copy(src, dest, overwrite: true);
            }

            CaptureDesktopState(key);
        }

        private static void CaptureDesktopState(string key)
        {
            var wm = Game1.windowMan;
            if (wm?.Desktop == null) return;

            var layout = wm.Desktop.GetLayout();
            wm.UnlockMan?.PopulateSaveData(layout);

            var themeField = typeof(WindowManager).GetField("theme",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var theme = themeField?.GetValue(wm) as TWMTheme;
            if (theme != null)
                layout.currentTheme = theme.id;

            layout.Gamma = Game1.gMan.Gamma;
            layout.BGMVolume = Game1.soundMan.BGMVol;
            layout.SFXVolume = Game1.soundMan.SFXVol;
            layout.tutorialCompleted = wm.TutorialStep == TutorialStep.COMPLETE;
            layout.inSolstice = wm.Desktop.inSolstice;

            string dest = Path.Combine(APSaveFolder, $"{key}_desktop.dat");
            File.WriteAllText(dest, JsonConvert.SerializeObject(layout, Formatting.None));
        }
    }
}
