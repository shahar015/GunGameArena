using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

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
        public static Plugin Instance;

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            try
            {
                ArenaConfig.Bind(Config);
                _harmony = new Harmony(Guid);
                _harmony.PatchAll(typeof(Plugin).Assembly);
                GunGameHooks.Install();
                Roster.Install();
                KillTracker.Install();
                Portraits.PortraitRenderer.Install();
                Hud.LeaderboardHud.Install();
                Log.LogInfo(Name + " " + Version + " loaded. Mode=" + ArenaConfig.Mode.Value
                            + " Leaderboard=" + ArenaConfig.LeaderboardEnabled.Value);
            }
            catch (Exception e)
            {
                Log.LogError(Name + " failed to initialise and is disabled: " + e);
            }
        }

        private void OnDestroy()
        {
            GunGameHooks.Uninstall();
            if (_harmony != null) _harmony.UnpatchSelf();
        }
    }
}
