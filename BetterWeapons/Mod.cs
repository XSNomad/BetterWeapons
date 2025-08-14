using fastJSON;
using HBS.Util;
using System;
using System.IO;

namespace BetterWeapons
{
    public class Mod
    {
        public Mod(string directory)
        {
            Directory = directory;
            Name = Path.GetFileName(directory);
        }

        public string Name { get; }
        public string Directory { get; }

        public string SettingsPath => Path.Combine(Directory, "Settings.json");
        public string SettingsDefaultsPath => Path.Combine(Directory, "Settings.defaults.json");
        public string SettingsLastPath => Path.Combine(Directory, "Settings.last.json");

        public void LoadSettings(object settings)
        {
            if (!File.Exists(SettingsPath))
            {
                return;
            }

            try
            {
                using var reader = new StreamReader(SettingsPath);
                var json = reader.ReadToEnd();
                JSONSerializationUtility.FromJSON(settings, json);
            }
            catch (Exception e)
            {
                WriteStartupError(e);
            }
        }

        public string StartupErrorLogPath => Path.Combine(Directory, "log.txt");

        public void ResetStartupErrorLog()
        {
            if (!File.Exists(StartupErrorLogPath))
            {
                return;
            }

            try
            {
                using var writer = new StreamWriter(StartupErrorLogPath);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public void WriteStartupError(object o)
        {
            using var s = new StreamWriter(StartupErrorLogPath, true);
            s.WriteLine(o);
        }

        public void SaveSettings(object settings, string path)
        {
            using var writer = new StreamWriter(path);
            var p = new JSONParameters
            {
                EnableAnonymousTypes = true,
                SerializeToLowerCaseNames = false,
                UseFastGuid = false,
                KVStyleStringDictionary = false,
                SerializeNullValues = true
            };

            var json = JSON.ToNiceJSON(settings, p);
            writer.Write(json);
        }

        public override string ToString()
        {
            return $"{Name} ({Directory})";
        }
    }
}