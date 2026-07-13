using OneShotMG;
using OneShotMG.src.TWM;
using OneShot.Archipelago.UI;
using WorldMachineLoader.API.Core;
using WorldMachineLoader.API.Events.Content;
using WorldMachineLoader.API.Events.Environment;
using WorldMachineLoader.API.Events.Gameplay;
using WorldMachineLoader.API.Events.Lifecycle;
using WorldMachineLoader.API.Interfaces;
using WorldMachineLoader.API.UI;
using HarmonyLib;

namespace OneShot.Archipelago
{
    public class Mod : IMod
    {
        public static ModContext Context = null!;
        public static Game1? Game1Instance { get; private set; }

        public static OneshotWindow? GetOneshotWindow() => Game1.windowMan?.GetOneshotWindow();

        public void OnLoad(ModContext modContext)
        {
            Context = modContext;
            Context.Logger.Log("OneShot Archipelago: Loaded!");

            // Register all windows for persistent desktop shortcuts
            WindowRegistry.Register<APSaveManagerWindow>(Context, "AP Save Manager");
            WindowRegistry.Register<APClientWindow>(Context, "AP Chat");
            WindowRegistry.Register<APLocationWindow>(Context, "AP Locations");
            WindowRegistry.Register<APHintsWindow>(Context, "AP Hints");

            // Events
            EventBus.Subscribe<Game1InitializeEvent>(OnGameInitialize);
            EventBus.Subscribe<WindowManagerInitializedEvent>(OnWindowManagerInitialized);
            EventBus.Subscribe<ItemAddedEvent>(OnItemAdded);
            EventBus.Subscribe<AchievementUnlockedEvent>(OnAchievementUnlocked);

            // Game-thread tick
            Context.Scheduler.RunEvery(
                System.TimeSpan.FromMilliseconds(500),
                GameStateManager.Tick
            );

            // Periodic backup of AP save files (every 30s while AP mode is active).
            // Does NOT trigger a game save — just copies whatever the game last wrote.
            Context.Scheduler.RunEvery(
                System.TimeSpan.FromSeconds(30),
                PeriodicBackup
            );
        }

        private static void PeriodicBackup()
        {
            if (!APSaveManager.IsAPModeActive || APSaveManager.ActiveSaveKey == null)
                return;
            APSaveManager.BackupAPFiles(APSaveManager.ActiveSaveKey);
        }

        private static void OnGameInitialize(Game1InitializeEvent e)
        {
            Game1Instance = e.Instance;
            Context.Logger.Log("Archipelago: Game1 instance cached.");
        }

        private static void OnWindowManagerInitialized(WindowManagerInitializedEvent e)
        {
            Game1.windowMan.AddWindow(new APSaveManagerWindow());
            APSaveManager.TryResumeActiveSession();

            if (APSaveManager.IsAPModeActive)
            {
                Context.Logger.Log("Archipelago: AP mode restored from saved session.");
                APClientWindow.Initialize();
                APLocationWindow.Initialize();
                APHintsWindow.Initialize();
                RegionWindow.Initialize();
            }
            else
            {
                Context.Logger.Log("Archipelago: Starting in vanilla mode.");
            }
        }

        private static void OnItemAdded(ItemAddedEvent e)
        {
            LocationTracker.OnItemAdded(e.ItemID);
        }

        private static void OnAchievementUnlocked(AchievementUnlockedEvent e)
        {
            if (e.AchievementID != null)
                LocationTracker.OnAchievementUnlocked(e.AchievementID);
        }

        public void OnShutdown()
        {
            if (APSaveManager.IsAPModeActive && APSaveManager.ActiveSaveKey != null)
                APSaveManager.BackupAPFiles(APSaveManager.ActiveSaveKey);

            EventBus.Unsubscribe<Game1InitializeEvent>(OnGameInitialize);
            EventBus.Unsubscribe<WindowManagerInitializedEvent>(OnWindowManagerInitialized);
            EventBus.Unsubscribe<ItemAddedEvent>(OnItemAdded);
            EventBus.Unsubscribe<AchievementUnlockedEvent>(OnAchievementUnlocked);

            ArchipelagoClient.Disconnect();
            Context.Logger.Log("OneShot Archipelago: Shutting down.");
        }
    }
}
