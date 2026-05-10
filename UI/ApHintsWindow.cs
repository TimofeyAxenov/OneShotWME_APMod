using System.Collections.Generic;
using OneShotMG;
using OneShotMG.src.Util;
using WorldMachineLoader.API.Core;
using WorldMachineLoader.API.UI;
using WorldMachineLoader.API.UI.Controls;

namespace OneShot.Archipelago.UI
{
    public class APHintsWindow : ModWindow
    {
        private static APHintsWindow? _instance;

        private const int W        = 340;
        private const int H        = 240;
        private const int PAD      = 6;
        private const int ROW_H    = 12;
        private const int VISIBLE  = 11;
        private const int MAX_LINE = 46;
        private const int MAX_HINTS = 200;

        private readonly List<string> _lines = new List<string>();
        private int _scroll = 0;

        private Label _countLabel;
        private readonly Label[] _rowLabels = new Label[VISIBLE];
        private InputBox _itemBox;

        public static void Initialize()
        {
            if (_instance != null) return;
            _instance = new APHintsWindow();
            Game1.windowMan.AddWindow(_instance);
        }

        public static void Close() { _instance = null; }

        public static void AddHint(string hintText)
        {
            if (_instance == null) return;
            _instance.AppendWrapped(hintText);
        }

        public APHintsWindow()
            : base("AP Hints", "oneshot", W, H, addCloseButton: true, addMinimizeButton: true)
        {
            int y = PAD;

            _countLabel = new Label("Hints: 0", new Vec2(PAD, y));
            AddControl(_countLabel);

            AddControl(new Button("▲", new Vec2(W - 22, y),                   OnScrollUp));
            AddControl(new Button("▼", new Vec2(W - 22, y + VISIBLE * ROW_H), OnScrollDown));

            y += 14;
            for (int i = 0; i < VISIBLE; i++)
            {
                _rowLabels[i] = new Label("", new Vec2(PAD, y + i * ROW_H));
                AddControl(_rowLabels[i]);
            }

            // Hint request
            y = H - 50;
            AddControl(new Label("Hint:", new Vec2(PAD, y)));
            _itemBox = new InputBox(new Vec2(PAD + 38, y), W - PAD - 42, 64);
            _itemBox.Placeholder = "Item name...";
            AddControl(_itemBox);

            y += 22;
            AddControl(new Button("Request Hint", new Vec2(PAD, y), OnRequestHint));
        }

        protected override void OnUpdate()
        {
            if (!Game1.windowMan.IsWindowFocused(this))
                _itemBox.IsFocused = false;
        }

        private void OnScrollUp()
        {
            if (_scroll > 0) { _scroll--; RedrawRows(); }
        }

        private void OnScrollDown()
        {
            int max = System.Math.Max(0, _lines.Count - VISIBLE);
            if (_scroll < max) { _scroll++; RedrawRows(); }
        }

        private void OnRequestHint()
        {
            string item = _itemBox.Text.Trim();
            if (string.IsNullOrEmpty(item)) return;
            ArchipelagoClient.SendChatMessage($"!hint {item}");
            _itemBox.Text = "";
        }

        private void AppendWrapped(string msg)
        {
            while (msg.Length > MAX_LINE)
            {
                int cut = msg.LastIndexOf(' ', MAX_LINE);
                if (cut <= 0) cut = MAX_LINE;
                _lines.Add(msg.Substring(0, cut));
                msg = "  " + msg.Substring(cut).TrimStart();
            }
            _lines.Add(msg);

            while (_lines.Count > MAX_HINTS)
                _lines.RemoveAt(0);

            _scroll = System.Math.Max(0, _lines.Count - VISIBLE);
            RedrawRows();
        }

        private void RedrawRows()
        {
            _countLabel.Text = $"Hints: {_lines.Count}";
            for (int i = 0; i < VISIBLE; i++)
            {
                int idx = _scroll + i;
                _rowLabels[i].Text = idx < _lines.Count ? _lines[idx] : "";
            }
        }
    }
}
