using System.Collections.Generic;
using OneShotMG;
using OneShotMG.src.TWM;
using OneShotMG.src.Util;
using WorldMachineLoader.API.Core;
using WorldMachineLoader.API.UI;
using WorldMachineLoader.API.UI.Controls;

namespace OneShot.Archipelago.UI
{
    public class APSaveManagerWindow : ModWindow
    {
        private const int W   = 340;
        private const int H   = 324;
        private const int PAD = 6;

        private InputBox _hostBox;
        private InputBox _portBox;
        private InputBox _saveNameBox;
        private InputBox _slotBox;
        private InputBox _passBox;
        private Label    _saveListLabel;
        private Label    _statusLabel;

        private int _selectedIndex = -1;
        private List<APSaveManager.APSaveInfo> _saves = new List<APSaveManager.APSaveInfo>();

        public APSaveManagerWindow()
            : base("AP Save Manager", "oneshot", W, H, addCloseButton: true, addMinimizeButton: true)
        {
            int y = PAD;

            // ── Connection form ───────────────────────────────────────────────
            AddControl(new Label("Host:", new Vec2(PAD, y)));
            _hostBox = new InputBox(new Vec2(PAD + 40, y), 148);
            _hostBox.Text = "archipelago.gg";
            AddControl(_hostBox);

            AddControl(new Label("Port:", new Vec2(PAD + 198, y)));
            _portBox = new InputBox(new Vec2(PAD + 232, y), 66, 5);
            _portBox.Text = "";
            AddControl(_portBox);

            y += 24;
            AddControl(new Label("Name:", new Vec2(PAD, y)));
            _saveNameBox = new InputBox(new Vec2(PAD + 40, y), 258, 64);
            _saveNameBox.Placeholder = "(save label)";
            AddControl(_saveNameBox);

            y += 24;
            AddControl(new Label("Slot:", new Vec2(PAD, y)));
            _slotBox = new InputBox(new Vec2(PAD + 40, y), 258, 64);
            AddControl(_slotBox);

            y += 24;
            AddControl(new Label("Pass:", new Vec2(PAD, y)));
            _passBox = new InputBox(new Vec2(PAD + 40, y), 258, 64);
            _passBox.Placeholder = "(optional)";
            AddControl(_passBox);

            y += 24;
            AddControl(new Button("Connect & Play", new Vec2(PAD, y),       OnConnectClicked));
            AddControl(new Button("Return to Vanilla", new Vec2(PAD + 114, y), OnReturnToVanillaClicked));

            y += 22;
            _statusLabel = new Label("", new Vec2(PAD, y));
            AddControl(_statusLabel);

            // ── Save list ─────────────────────────────────────────────────────
            y += 20;
            AddControl(new Label("--- Saved Sessions ---------------", new Vec2(PAD, y)));

            y += 14;
            _saveListLabel = new Label("No AP saves found.", new Vec2(PAD, y));
            AddControl(_saveListLabel);

            y += 110;
            AddControl(new Button("▲", new Vec2(PAD, y),         OnSelectUp));
            AddControl(new Button("▼", new Vec2(PAD + 70, y),    OnSelectDown));
            AddControl(new Button("Load",   new Vec2(PAD + 140, y),   OnLoadSelectedClicked));
            AddControl(new Button("Delete", new Vec2(PAD + 210, y),  OnDeleteClicked));

            RefreshSaveList();
        }

        protected override void OnUpdate()
        {
            bool focused = Game1.windowMan.IsWindowFocused(this);
            if (!focused)
            {
                _hostBox.IsFocused = false;
                _portBox.IsFocused = false;
                _saveNameBox.IsFocused = false;
                _slotBox.IsFocused = false;
                _passBox.IsFocused = false;
            }

            if (APSaveManager.IsAPModeActive)
                _statusLabel.Text = $"Active: {APSaveManager.GetActiveSaveDisplayName()}";
            else
                _statusLabel.Text = ArchipelagoClient.Connected ? "Connected" : "";
        }

        private void OnConnectClicked()
        {
            if (APSaveManager.IsOneShotRunning())
            {
                GameStateManager.GetWindow()?.ShowModalWindow(
                    ModalWindow.ModalType.Info,
                    "Close OneShot first before switching saves.",
                    null
                );
                return;
            }

            string host    = _hostBox.Text.Trim();
            string saveName = _saveNameBox.Text.Trim();
            string slot    = _slotBox.Text.Trim();
            string passRaw = _passBox.Text.Trim();
            string? pass   = string.IsNullOrEmpty(passRaw) ? null : passRaw;

            if (!int.TryParse(_portBox.Text.Trim(), out int port))
            { _statusLabel.Text = "Invalid port!"; return; }
            if (string.IsNullOrEmpty(slot))
            { _statusLabel.Text = "Slot name required!"; return; }
            if (string.IsNullOrEmpty(saveName))
                saveName = slot;

            _statusLabel.Text = "Connecting...";

            bool ok = ArchipelagoClient.Connect(host, port, slot, pass);
            if (!ok) { _statusLabel.Text = "Connection failed! Check log."; return; }

            object seedObj;
            string seed = ArchipelagoClient.SlotData != null &&
                          ArchipelagoClient.SlotData.TryGetValue("Seed", out seedObj)
                ? seedObj.ToString() : "unknown";

            var info = new APSaveManager.APSaveInfo
            {
                Key         = APSaveManager.MakeSaveKey(seed, slot),
                DisplayName = saveName,
                slotName    = slot,
                Host        = host,
                Port        = port,
                Password    = pass,
                Seed        = seed,
            };

            _statusLabel.Text = $"Loading {info.GetDisplayName()}...";
            ArchipelagoClient.ResetItemTracking();
            LocationTracker.Reset();
            ArchipelagoClient.OwnedLogicalKeys.Clear();
            APSaveManager.ActivateAPSave(info);

            APClientWindow.Initialize();
            APLocationWindow.Initialize();
            APHintsWindow.Initialize();
            RegionWindow.Initialize();

            RefreshSaveList();
        }

        private void OnReturnToVanillaClicked()
        {
            if (!APSaveManager.IsAPModeActive)
            { _statusLabel.Text = "Already on vanilla."; return; }

            if (APSaveManager.IsOneShotRunning())
            {
                GameStateManager.GetWindow()?.ShowModalWindow(
                    ModalWindow.ModalType.Info,
                    "Close OneShot first before switching saves.",
                    null
                );
                return;
            }

            ArchipelagoClient.Disconnect();
            APSaveManager.DeactivateAPSave();
            APClientWindow.Close();
            APLocationWindow.Close();
            APHintsWindow.Close();
            RegionWindow.Close();
            RefreshSaveList();
        }

        private void OnSelectUp()
        {
            if (_selectedIndex > 0) { _selectedIndex--; RefreshSaveList(); }
        }

        private void OnSelectDown()
        {
            if (_selectedIndex < _saves.Count - 1) { _selectedIndex++; RefreshSaveList(); }
        }

        private void OnLoadSelectedClicked()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _saves.Count)
            { _statusLabel.Text = "No save selected."; return; }

            if (APSaveManager.IsOneShotRunning())
            {
                GameStateManager.GetWindow()?.ShowModalWindow(
                    ModalWindow.ModalType.Info,
                    "Close OneShot first before switching saves.",
                    null
                );
                return;
            }

            var info = _saves[_selectedIndex];
            bool ok  = ArchipelagoClient.Connect(info.Host, info.Port, info.slotName, info.Password);
            if (!ok) { _statusLabel.Text = "Reconnection failed!"; return; }

            _statusLabel.Text = $"Loading {info.GetDisplayName()}...";
            ArchipelagoClient.ResetItemTracking();
            LocationTracker.Reset();
            ArchipelagoClient.OwnedLogicalKeys.Clear();
            APSaveManager.ActivateAPSave(info);

            APClientWindow.Initialize();
            APLocationWindow.Initialize();
            APHintsWindow.Initialize();
            RegionWindow.Initialize();

            RefreshSaveList();
        }

        private void OnDeleteClicked()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _saves.Count)
            { _statusLabel.Text = "No save selected."; return; }

            var info = _saves[_selectedIndex];
            if (info.Key == APSaveManager.ActiveSaveKey)
            { _statusLabel.Text = "Cannot delete active save!"; return; }

            APSaveManager.DeleteSave(info.Key);
            _selectedIndex = -1;
            _statusLabel.Text = "Deleted.";
            RefreshSaveList();
        }

        private void RefreshSaveList()
        {
            _saves = APSaveManager.GetAllSaves();
            if (_saves.Count == 0)
            { _saveListLabel.Text = "No AP saves found."; return; }

            var lines = new List<string>();
            for (int i = 0; i < _saves.Count; i++)
            {
                var s      = _saves[i];
                string sel = (i == _selectedIndex)            ? ">" : " ";
                string act = (s.Key == APSaveManager.ActiveSaveKey) ? "[*]" : "   ";
                string nm  = s.GetDisplayName();
                if (nm.Length > 18) nm = nm.Substring(0, 18);
                lines.Add($"{sel}{act} {nm} {s.LastPlayed:MM-dd HH:mm}");
            }
            _saveListLabel.Text = string.Join("\n", lines);
        }
    }
}
