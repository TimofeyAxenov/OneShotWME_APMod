using System.Collections.Generic;
using OneShotMG;
using OneShotMG.src.TWM;
using OneShotMG.src.Util;
using WorldMachineLoader.API.Core;
using WorldMachineLoader.API.UI;
using WorldMachineLoader.API.UI.Controls;

namespace OneShot.Archipelago.UI
{
        public class RegionWindow : ModWindow
        {
                private const int W = 340;
                private const int H = 100;
                private const int PAD = 6;

                private static RegionWindow? _instance;

                public static void Initialize()
                {
                        if (_instance != null) return;
                        _instance = new RegionWindow();
                        Game1.windowMan.AddWindow(_instance);
                }

                public static void Close() { _instance = null; }

                public RegionWindow() : base("Region Selection Window", "", W, H, addCloseButton: true, addMinimizeButton: true)
                {
                        int y = PAD;
                        y += 24;
                        AddControl(new Button("Go Home", new Vec2(150, y), GoHome));
                        
                        y += 24;
                        AddControl(new Button("Go to Barrens", new Vec2(PAD, y), GoToBarrens));
                        AddControl(new Button("Go to Glen", new Vec2(PAD + 130, y), GoToGlen));
                        AddControl(new Button("Go to Refuge", new Vec2(PAD + 260, y), GoToRefuge));

                        y += 24;
                        AddControl(new Button("Solstice", new Vec2(150, y), InitiateSolstice));
                }

                private void GoHome()
                {}

                private void GoToBarrens()
                {}

                private void GoToGlen()
                {}

                private void GoToRefuge()
                {}

                private void InitiateSolstice()
                {}
        }
}
