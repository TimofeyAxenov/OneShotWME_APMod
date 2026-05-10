using System.Collections.Generic;
using OneShotMG;
using OneShotMG.src.Util;
using WorldMachineLoader.API.Core;
using WorldMachineLoader.API.UI;
using WorldMachineLoader.API.UI.Controls;

namespace OneShot.Archipelago.UI
{
    public class APClientWindow : ModWindow
    {
        private static APClientWindow? _instance;

        private const int W        = 340;
        private const int H        = 260;
        private const int PAD      = 6;
        private const int ROW_H    = 12;  // pixels per text row
        private const int VISIBLE  = 9;   // visible chat rows
        private const int MAX_MSGS = 200;
        private const int MAX_LINE = 46;

        private readonly List<string> _lines = new List<string>();
        private int _scroll = 0;

        // One Label per visible row — fixed set, text swapped on scroll
        private readonly Label[] _rowLabels = new Label[VISIBLE];
        private InputBox _inputBox;

        public static void Initialize()
        {
            if (_instance != null) return;
            _instance = new APClientWindow();
            Game1.windowMan.AddWindow(_instance);
        }

        public static void Close() { _instance = null; }

        public static void AddMessage(string message)
        {
            Mod.Context.Logger.Log($"[AP] {message}");
            if (_instance == null) return;
            _instance.AppendWrapped(message);
        }

        public APClientWindow()
            : base("AP Chat", "oneshot", W, H, addCloseButton: true, addMinimizeButton: true)
        {
            int y = PAD;

            // Row labels — one per visible line
            for (int i = 0; i < VISIBLE; i++)
            {
                _rowLabels[i] = new Label("", new Vec2(PAD, y + i * ROW_H));
                AddControl(_rowLabels[i]);
            }

            // Scroll buttons
            AddControl(new Button("▲", new Vec2(W - 22, PAD),              OnScrollUp));
            AddControl(new Button("▼", new Vec2(W - 22, PAD + VISIBLE * ROW_H - 14), OnScrollDown));

            // Chat input
            y = H - 50;
            AddControl(new Label("Say:", new Vec2(PAD, y)));
            _inputBox = new InputBox(new Vec2(PAD + 30, y), W - PAD - 34, 200);
            _inputBox.Placeholder = "Type a message...";
            AddControl(_inputBox);

            y += 22;
            AddControl(new Button("Send", new Vec2(PAD, y), OnSend));
        }

        protected override void OnUpdate()
        {
            if (!Game1.windowMan.IsWindowFocused(this))
                _inputBox.IsFocused = false;
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

        private void OnSend()
        {
            string text = _inputBox.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;
            ArchipelagoClient.SendChatMessage(text);
            _inputBox.Text = "";
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

            while (_lines.Count > MAX_MSGS)
                _lines.RemoveAt(0);

            // Auto-scroll to bottom
            _scroll = System.Math.Max(0, _lines.Count - VISIBLE);
            RedrawRows();
        }

        private void RedrawRows()
        {
            for (int i = 0; i < VISIBLE; i++)
            {
                int lineIdx = _scroll + i;
                _rowLabels[i].Text = lineIdx < _lines.Count ? _lines[lineIdx] : "";
            }
        }
    }
}
