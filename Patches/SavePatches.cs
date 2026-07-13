namespace OneShot.Archipelago.Patches
{
    public static class SavePatches
    {
        public static bool BlockSave() => true;

        public static void BackupPostfix()
        {
            if (APSaveManager.IsAPModeActive && APSaveManager.ActiveSaveKey != null)
                APSaveManager.BackupAPFiles(APSaveManager.ActiveSaveKey);
        }
    }
}
