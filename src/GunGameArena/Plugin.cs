using BepInEx;
using BepInEx.Logging;

namespace GunGameArena
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency("Kodeman.GunGame", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "shaha.GunGameArena";
        public const string Name = "GunGame Arena";
        public const string Version = "0.1.0";

        public static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo(Name + " " + Version + " loaded (scaffold).");
        }
    }
}
