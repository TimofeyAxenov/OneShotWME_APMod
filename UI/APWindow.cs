// APWindow.cs is now split into APSaveManagerWindow.cs and APClientWindow.cs
// This file is kept as a compatibility shim so existing references compile.
// AddMessage routes to APClientWindow.

namespace OneShot.Archipelago.UI
{
    public static class APWindow
    {
        public static void Initialize() => APClientWindow.Initialize();
        public static void AddMessage(string msg) => APClientWindow.AddMessage(msg);
//        public static void AddLocationSent(string loc) => APClientWindow.AddLocationSent(loc);
    }
}
