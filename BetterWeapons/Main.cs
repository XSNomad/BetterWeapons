using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Harmony;

namespace BetterWeapons
{
    public static class Main
    {
        internal static Settings settings = new();
        public const string ModName = "BetterWeapons";
        public const string ModId = "Weapons.Better.BTN";

        public static BetterLogger Logger;
        public static Mod mod;

        public static readonly Dictionary<string, bool> IsClustered = new();

        public static void Init(string modDirectory)
        {
            mod = new Mod(modDirectory);

            try
            {
                FileUtils.SetReadonly(mod.SettingsDefaultsPath, false);
                FileUtils.SetReadonly(mod.SettingsLastPath, false);

                mod.SaveSettings(settings, mod.SettingsDefaultsPath);
                mod.LoadSettings(settings);
                mod.SaveSettings(settings, mod.SettingsLastPath);

                if (settings.GeneratedSettingsFilesReadonly)
                {
                    FileUtils.SetReadonly(mod.SettingsDefaultsPath, true);
                    FileUtils.SetReadonly(mod.SettingsLastPath, true);
                }

                Logger = BetterLog.SetupModLog(Path.Combine(modDirectory, "BetterWeapons.txt"), nameof(BetterWeapons), settings.BetterLog);
            }
            catch (Exception e)
            {
                mod.WriteStartupError(e);
                throw;
            }

            try
            {
                Logger.Info?.Log($"version {Assembly.GetExecutingAssembly().GetInformationalVersion()}");
                Logger.Info?.Log("settings loaded");
                Logger.Debug?.Log("debugging enabled");
                Logger.Info?.Log("started");
            }
            catch (Exception e)
            {
                Logger.Error.Log("error starting", e);
                throw;
            }
            // Safe attempts — will no-op if DataManager not ready yet
            try { Calculator.SimpleVariance.PrecomputeIfReady(); } catch (Exception ex) { Logger.Warning.Log($"SimpleVariance precompute skipped: {ex.Message}"); }
            try { if (settings.Jamming) JammingEnabler.Precompute(); } catch (Exception ex) { Logger.Warning.Log($"Jamming precompute skipped: {ex.Message}"); }
            var harmony = HarmonyInstance.Create(ModId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Logger.Info.Log($"{ModName} initialized.");
        }
        public static string GetInformationalVersion(this Assembly assembly)
        {
            var attributes = assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);

            if (attributes.Length == 0)
            {
                return "";
            }
            return ((AssemblyInformationalVersionAttribute)attributes[0]).InformationalVersion;
        }
    }
}
public static class FileUtils
{
    public static void SetReadonly(string path, bool ro)
    {
        if (!File.Exists(path))
        {
            return;
        }
        var attributes = File.GetAttributes(path);
        if (ro)
        {
            attributes |= FileAttributes.ReadOnly;
        }
        else
        {
            attributes &= ~FileAttributes.ReadOnly;
        }
        File.SetAttributes(path, attributes);
    }
}
