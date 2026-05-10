using System.Collections.Generic;
using OneShotMG;
using OneShotMG.src.Util;
using WorldMachineLoader.API.Core;
using WorldMachineLoader.API.UI;
using WorldMachineLoader.API.UI.Controls;

namespace OneShot.Archipelago.UI
{
    public class APLocationWindow : ModWindow
    {
        private static APLocationWindow? _instance;

        private const int W       = 300;
        private const int H       = 220;
        private const int PAD     = 6;
        private const int ROW_H   = 12;
        private const int VISIBLE = 13;
        private const int MAX_NAME = 38;

        private List<string> _locations = new List<string>();
        private int _scroll = 0;

        private Label _countLabel;
        private readonly Label[] _rowLabels = new Label[VISIBLE];

        public static void Initialize()
        {
            if (_instance != null) return;
            _instance = new APLocationWindow();
            Game1.windowMan.AddWindow(_instance);
            Refresh();
        }

        public static void Close() { _instance = null; }

        public static void Refresh()
        {
            if (_instance == null) return;
            _instance._locations = ArchipelagoClient.GetMissingLocationNames();
            _instance._scroll    = 0;
            _instance.RedrawRows();
        }

        public APLocationWindow()
            : base("AP Locations", "oneshot", W, H, addCloseButton: true, addMinimizeButton: true)
        {
            int y = PAD;

            _countLabel = new Label("Missing: 0", new Vec2(PAD, y));
            AddControl(_countLabel);

            AddControl(new Button("▲", new Vec2(W - 22, y),                    OnScrollUp));
            AddControl(new Button("▼", new Vec2(W - 22, y + VISIBLE * ROW_H),  OnScrollDown));

            y += 14;
            for (int i = 0; i < VISIBLE; i++)
            {
                _rowLabels[i] = new Label("", new Vec2(PAD, y + i * ROW_H));
                AddControl(_rowLabels[i]);
            }
        }

        private void OnScrollUp()
        {
            if (_scroll > 0) { _scroll--; RedrawRows(); }
        }

        private void OnScrollDown()
        {
            int max = System.Math.Max(0, _locations.Count - VISIBLE);
            if (_scroll < max) { _scroll++; RedrawRows(); }
        }

        private void RedrawRows()
        {
            _countLabel.Text = $"Missing: {_locations.Count}";
            for (int i = 0; i < VISIBLE; i++)
            {
                int idx = _scroll + i;
                if (idx < _locations.Count)
                {
                    string name = _locations[idx];
                    if (name.Length > MAX_NAME)
                        name = name.Substring(0, MAX_NAME - 1) + "…";
                    _rowLabels[i].Text = name;
                }
                else
                {
                    _rowLabels[i].Text = "";
                }
            }
        }
    }
}
