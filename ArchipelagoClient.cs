using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using APMessageLog = Archipelago.MultiClient.Net.MessageLog.Messages.LogMessage;
using OneShot.Archipelago.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using OneShotMG;
using OneShotMG.src.TWM;

namespace OneShot.Archipelago
{
    public static class ArchipelagoClient
    {
        public static ArchipelagoSession? Session { get; private set; }
        public static bool Connected => Session?.Socket.Connected ?? false;
        public static Dictionary<string, object>? SlotData { get; private set; }
        // Track which logical key items (IDs 700‑704) the player currently owns.
        public static readonly System.Collections.Generic.HashSet<int> OwnedLogicalKeys = new System.Collections.Generic.HashSet<int>();

        // Tracks how many items we've already applied so we don't re-apply on reconnect
        private static int _appliedItemCount = 0;
        private static int _receivedItemIndex = 0;

        private static readonly HashSet<long> CheckedLocations = new();

        public static HashSet<int> PendingAPItems = new HashSet<int>();

        public static bool ReceivingAPItem = false;

        public static bool IsLocationChecked(long locationId)
{
    return CheckedLocations.Contains(locationId);
}


        public static void MarkLocationChecked(long locationId)
{
    CheckedLocations.Add(locationId);
}

public static bool IsLocationInSeed(long locationId)
{
    return Session?.Locations.AllLocations.Contains(locationId) ?? false;
}

        public static bool Connect(string host, int port, string slotName, string? password = null)
        {
            try
            {
                Session = ArchipelagoSessionFactory.CreateSession(host, port);
                Session.Items.ItemReceived += OnItemReceived;
                Session.MessageLog.OnMessageReceived += OnMessageReceived;
                Session.Locations.CheckedLocationsUpdated += OnCheckedLocationsUpdated;

                var result = Session.TryConnectAndLogin(
                    game: "OneShot World Machine Edition",
                    name: slotName,
                    itemsHandlingFlags: ItemsHandlingFlags.AllItems,
                    password: password
                );

                switch (result)
                {
                    case LoginSuccessful success:
                    {
                        SlotData = success.SlotData;
                        object optionsObj;
                        if (SlotData != null && SlotData.TryGetValue("options", out optionsObj)
                                             && optionsObj is JObject optionsJson)
                        {
                            foreach (var kvp in optionsJson)
                                SlotData[kvp.Key] = kvp.Value!;
                        }
                        object goalVal;
                        string goalStr = SlotData != null && SlotData.TryGetValue("Goal", out goalVal)
                            ? goalVal.ToString() : "unknown";
                        Mod.Context.Logger.Log($"Archipelago: Connected as {slotName}! Goal = {goalStr}");
                        APClientWindow.AddMessage($"Connected as {slotName}!");
                        return true;
                    }
                    case LoginFailure failure:
                    {
                        if (failure.ErrorCodes.Contains(ConnectionRefusedError.InvalidSlot))
                        {
                            GameStateManager.GetWindow()?.ShowModalWindow(ModalWindow.ModalType.Error, "Check the slot name and try again.");
                        }
                        else if (failure.ErrorCodes.Contains(ConnectionRefusedError.InvalidGame))
                        {
                            GameStateManager.GetWindow()?.ShowModalWindow(ModalWindow.ModalType.Error, "This slot is not configured for OneShot: World Machine Edition.");
                        }
                        else if (failure.ErrorCodes.Contains(ConnectionRefusedError.InvalidPassword))
                        {
                            GameStateManager.GetWindow()?.ShowModalWindow(ModalWindow.ModalType.Error, "Check the password and try again.");
                        }
                        else
                        {
                            GameStateManager.GetWindow()?.ShowModalWindow(ModalWindow.ModalType.Error, "Unable to connect to Archipelago.");
                        }
                        Mod.Context.Logger.Log($"Archipelago: Connection failed — {string.Join(", ", failure.Errors)}");
                        Session = null;
                        return false;
                    }
                    default:
                        // Ideally we'd throw UnreachableException but this isn't available in the current version of .NET
                        throw new InvalidOperationException();
                }
            }
            catch (Exception ex)
            {
                Mod.Context.Logger.Log($"Archipelago: Exception during connect — {ex.Message}");
                return false;
            }
        }

        private static void OnItemReceived(ReceivedItemsHelper helper)
        {
            // Skip items already applied in a previous session
            if (_receivedItemIndex < _appliedItemCount)
            {
                helper.DequeueItem();
                _receivedItemIndex++;
                return;
            }

            ItemInfo item = helper.DequeueItem();
            _receivedItemIndex++;

            // Calculate game item ID — AP item ID minus our base
            long gameItemId = item.ItemId - LocationTracker.ITEM_ID_BASE;
            Mod.Context.Logger.Log(
                $"Archipelago: Received '{item.ItemName}' AP={item.ItemId} gameId={gameItemId}");

            // Traps
//            if (item.ItemName == "Spooky Popup Trap" || item.ItemName == "Crash Trap")
//            {
//                GameStateManager.PendingTraps.Enqueue(item.ItemName);
//                GameStateManager.PendingChat.Enqueue($"[Trap] {item.ItemName}!");
//                _appliedItemCount++;
//                return;
//            }

            // Logical keys (700-704) — no game action, just notify and track ownership
            if (gameItemId >= 700 && gameItemId <= 704)
            {
                GameStateManager.PendingChat.Enqueue($"[Key] Received: {item.ItemName}");
                // Record owned logical key for region selection
                OwnedLogicalKeys.Add((int)gameItemId);
                _appliedItemCount++;
                return;
            }

            // Filler
            if (gameItemId == 900)
            {
                GameStateManager.PendingChat.Enqueue($"[Filler] Received: {item.ItemName}");
                _appliedItemCount++;
                return;
            }

            // Game items (IDs 1-80 in game = ITEM_ID_BASE+1 to ITEM_ID_BASE+80 in AP)
            if (gameItemId >= 1 && gameItemId <= 80)
            {
                GameStateManager.PendingItems.Enqueue((int)gameItemId);
                GameStateManager.PendingChat.Enqueue($"[Item] {item.ItemName}");
                _appliedItemCount++;
                return;
            }

            // Collectibles: wallpapers (401-416), profiles (426-451), themes (461-469)
            if ((gameItemId >= 401 && gameItemId <= 416) ||
                (gameItemId >= 426 && gameItemId <= 451) ||
                (gameItemId >= 461 && gameItemId <= 469))
            {
                GameStateManager.PendingItems.Enqueue((int)gameItemId);
                GameStateManager.PendingChat.Enqueue($"[Collectible] {item.ItemName}");
                _appliedItemCount++;
                return;
            }

            // Wallpapers received as AP items (1100-1199)
            if (gameItemId >= 1100 && gameItemId < 1200)
            {
                GameStateManager.PendingItems.Enqueue((int)gameItemId);
                GameStateManager.PendingChat.Enqueue($"[Wallpaper] {item.ItemName}");
                _appliedItemCount++;
                return;
            }

            // Themes received as AP items (1200-1299)
            if (gameItemId >= 1200 && gameItemId < 1300)
            {
                GameStateManager.PendingItems.Enqueue((int)gameItemId);
                GameStateManager.PendingChat.Enqueue($"[Theme] {item.ItemName}");
                _appliedItemCount++;
                return;
            }

            // Profiles received as AP items (1300-1399)
            if (gameItemId >= 1300 && gameItemId < 1400)
            {
                GameStateManager.PendingItems.Enqueue((int)gameItemId);
                GameStateManager.PendingChat.Enqueue($"[Profile] {item.ItemName}");
                _appliedItemCount++;
                return;
            }

            // Badges received from AP (1400-1499)
            if (gameItemId >= 1400 && gameItemId < 1500)
            {
                GameStateManager.PendingItems.Enqueue((int)gameItemId);
                GameStateManager.PendingChat.Enqueue($"[Badge] {item.ItemName}");
                _appliedItemCount++;
                return;
            }

            // Files received from AP (1000-1099)
            if (gameItemId >= 1000 && gameItemId < 1100)
            {
                GameStateManager.PendingItems.Enqueue((int)gameItemId);
                GameStateManager.PendingChat.Enqueue($"[File] {item.ItemName}");
                _appliedItemCount++;
                return;
            }

            Mod.Context.Logger.Log(
                $"Archipelago: Unhandled item '{item.ItemName}' gameId={gameItemId}");
        }

        private static void OnMessageReceived(APMessageLog message)
        {
            GameStateManager.PendingChat.Enqueue(message.ToString());
        }

        private static void OnCheckedLocationsUpdated(
            System.Collections.ObjectModel.ReadOnlyCollection<long> newChecked)
        {
            GameStateManager.PendingLocationRefresh = true;
        }

        public static void SendLocationCheck(long locationId)
        {
            if (!Connected) return;
            Session!.Locations.CompleteLocationChecks(locationId);
        }

        public static void SendGoalCompletion()
        {
            if (!Connected) return;
            Mod.Context.Logger.Log("Archipelago: Sending goal completion!");
            APClientWindow.AddMessage("[Goal] Completed!");
            Session!.Socket.SendPacket(new StatusUpdatePacket
            {
                Status = ArchipelagoClientState.ClientGoal
            });
        }

        /// <summary>Returns all missing (unchecked) location names sorted alphabetically.</summary>
        public static List<string> GetMissingLocationNames()
        {
            if (Session == null) return new List<string>();
            var result = new List<string>();
            foreach (long locId in Session.Locations.AllMissingLocations)
            {
                string name = Session.Locations.GetLocationNameFromId(locId)
                              ?? $"Location {locId}";
                result.Add(name);
            }
            result.Sort();
            return result;
        }

        /// <summary>Send a chat message to the AP server.</summary>
        public static void SendChatMessage(string message)
        {
            if (!Connected || string.IsNullOrWhiteSpace(message)) return;
            Session!.Socket.SendPacket(new SayPacket { Text = message });
        }

        public static void ResetItemTracking()
        {
            _appliedItemCount = 0;
            _receivedItemIndex = 0;
        }

        public static void Disconnect()
        {
            if (Session != null)
            {
                Session.Items.ItemReceived -= OnItemReceived;
                Session.MessageLog.OnMessageReceived -= OnMessageReceived;
                Session.Locations.CheckedLocationsUpdated -= OnCheckedLocationsUpdated;
                if (Connected) Session.Socket.DisconnectAsync();
            }
            Session = null;
            SlotData = null;
        }
    }
}
