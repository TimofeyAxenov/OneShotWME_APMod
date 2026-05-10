using OneShotMG;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using HarmonyLib;

namespace OneShot.Archipelago
{
    public static class APSaveManager
    {

            private static Harmony? _harmony = null;
        public static readonly string APSaveFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OneShotWME", "archipelago"
        );

        public static readonly string VanillaSaveFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OneShotWME"
        );

        private const string SAVE_INDEX_FILE = "ap_saves.json";

        // Vanilla save filenames
        private static readonly string[] VanillaFiles = {
            "save.dat", "p-settings.dat", "desktop.dat", "filesystem.dat"
        };

        public static string? ActiveSaveKey { get; private set; }
        public static bool IsAPModeActive => ActiveSaveKey != null;

        static APSaveManager()
        {
            Directory.CreateDirectory(APSaveFolder);
        }

        public class APSaveInfo
        {
            public string Key { get; set; } = "";
            public string SlotName { get; set; } = "";
            public string Host { get; set; } = "";
            public int Port { get; set; } = 38281;
            public string? Password { get; set; }
            public string Seed { get; set; } = "";
            public DateTime LastPlayed { get; set; } = DateTime.Now;
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
            ActiveSaveKey = info.Key;
            info.LastPlayed = DateTime.Now;

            var saves = GetAllSaves();
            int idx = saves.FindIndex(s => s.Key == info.Key);
            if (idx >= 0) saves[idx] = info;
            else saves.Add(info);
            SaveIndex(saves);

            // Back up all vanilla files, then restore AP save files
            BackupVanillaFiles();
            RestoreAPFiles(info.Key);
            RestartEntireSystem();
            if (_harmony == null)
{
    _harmony = new Harmony("oneshot.archipelago");
    _harmony.PatchAll(typeof(APSaveManager).Assembly);
    Mod.Context.Logger.Log("Archipelago: Harmony patches applied.");
}
        }

        public static void DeactivateAPSave()
        {
            if (ActiveSaveKey != null)
                BackupAPFiles(ActiveSaveKey);
                if (_harmony != null)
{
    _harmony.UnpatchAll("oneshot.archipelago");
    _harmony = null;
    Mod.Context.Logger.Log("Archipelago: Harmony patches removed.");
}

            ActiveSaveKey = null;
            RestoreVanillaFiles();
            RestartEntireSystem();
        }

        public static void DeleteSave(string key)
        {
            // Delete all AP save files for this key
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

        // Back up current vanilla save files to temp location
        private static void BackupVanillaFiles()
        {
            foreach (string fileName in VanillaFiles)
            {
                string src  = Path.Combine(VanillaSaveFolder, fileName);
                string dest = Path.Combine(APSaveFolder, $"vanilla_backup_{fileName}");
                if (File.Exists(src)) File.Copy(src, dest, overwrite: true);
            }
        }

        // Restore vanilla backup files to active slot
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

        // Copy AP save files into active slot (fresh start if none exist)
        private static void RestoreAPFiles(string key)
        {
            foreach (string fileName in VanillaFiles)
            {
                string src  = Path.Combine(APSaveFolder, $"{key}_{fileName}");
                string dest = Path.Combine(VanillaSaveFolder, fileName);
                if (File.Exists(dest)) File.Delete(dest);
                if (File.Exists(src)) File.Copy(src, dest, overwrite: true);
            }
        }

        // Back up current active files to AP save slot
        public static void BackupAPFiles(string key)
        {
            foreach (string fileName in VanillaFiles)
            {
                string src  = Path.Combine(VanillaSaveFolder, fileName);
                string dest = Path.Combine(APSaveFolder, $"{key}_{fileName}");
                if (File.Exists(src)) File.Copy(src, dest, overwrite: true);
            }
        }

        private static void RestartEntireSystem()
{
    var osWindow = Game1.windowMan?.GetOneshotWindow();

    if (osWindow == null)
    {
        Mod.Context.Logger.Log("Archipelago: No OneshotWindow found, cannot restart cleanly.");
        return;
    }

    Mod.Context.Logger.Log("Archipelago: Restarting game for save switch...");

    // Save current desktop + filesystem state
    Patches.WindowManagerHelper.ReloadFilesystemAndDesktop();

    // Exit game (this is the ONLY safe reset method)
    osWindow.ExitGame();
}

    }
}
