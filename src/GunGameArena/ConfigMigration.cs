using System;
using System.IO;

namespace GunGameArena
{
    /// <summary>
    /// Copies settings from the legacy shaha.GunGameArena.cfg file (used before the
    /// plugin GUID was renamed to zgames.GunGameArena) into the new config path, so
    /// existing installs keep their settings after upgrading.
    /// </summary>
    public static class ConfigMigration
    {
        public static void MigrateLegacyConfig(string newPath)
        {
            try
            {
                if (string.IsNullOrEmpty(newPath)) return;

                string directory = Path.GetDirectoryName(newPath);
                if (string.IsNullOrEmpty(directory)) return;

                string oldPath = Path.Combine(directory, "shaha.GunGameArena.cfg");

                bool newFileMissingOrEmpty = !File.Exists(newPath) || new FileInfo(newPath).Length == 0;

                if (File.Exists(oldPath) && newFileMissingOrEmpty)
                {
                    File.Copy(oldPath, newPath, true);
                    if (Plugin.Log != null)
                    {
                        Plugin.Log.LogInfo("Migrated settings from shaha.GunGameArena.cfg to zgames.GunGameArena.cfg.");
                    }
                }
            }
            catch (Exception e)
            {
                if (Plugin.Log != null)
                {
                    Plugin.Log.LogError("Config migration failed: " + e);
                }
            }
        }
    }
}
