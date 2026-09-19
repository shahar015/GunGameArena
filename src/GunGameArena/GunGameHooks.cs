using System;
using GunGame.Scripts;
using UnityEngine.SceneManagement;

namespace GunGameArena
{
    /// <summary>Bridges GunGame's static round events to our own. Everything else subscribes here,
    /// never to GunGame directly, so the ordering is controlled in one place.</summary>
    public static class GunGameHooks
    {
        public static event Action RoundStarting;  // fired on GunGame BeforeGameStartedEvent (before sosigs spawn)
        public static event Action RoundStarted;   // fired on GunGame GameStartedEvent (after initial spawns)
        public static event Action RoundEnded;     // fired on scene change
        public static bool RoundActive { get; private set; }

        public static void Install()
        {
            GameManager.BeforeGameStartedEvent += OnBefore;
            GameManager.GameStartedEvent += OnStarted;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public static void Uninstall()
        {
            GameManager.BeforeGameStartedEvent -= OnBefore;
            GameManager.GameStartedEvent -= OnStarted;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private static void OnBefore()
        {
            try
            {
                RoundActive = true;
                Plugin.Log.LogInfo("Round starting.");
                if (RoundStarting != null) RoundStarting();
            }
            catch (Exception e) { Plugin.Log.LogError("RoundStarting handler failed: " + e); }
        }

        private static void OnStarted()
        {
            try
            {
                Plugin.Log.LogInfo("Round started.");
                if (RoundStarted != null) RoundStarted();
            }
            catch (Exception e) { Plugin.Log.LogError("RoundStarted handler failed: " + e); }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try
            {
                if (!RoundActive) return;
                RoundActive = false;
                Plugin.Log.LogInfo("Scene changed, round ended.");
                if (RoundEnded != null) RoundEnded();
            }
            catch (Exception e) { Plugin.Log.LogError("RoundEnded handler failed: " + e); }
        }
    }
}
