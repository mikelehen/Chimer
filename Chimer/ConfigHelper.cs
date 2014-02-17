using System;
using System.Reflection;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Chimer.Audio;

namespace Chimer
{
    class ConfigHelper
    {
        private static readonly string CONFIG_FILE = "config.json";

        private static string BaseDir
        {
            get
            {
                return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }
        }

        public static string ConfigFile
        {
            get
            {
                return BaseDir + "\\" + CONFIG_FILE;
            }
        }

        public static Config Load()
        {
            string json = File.ReadAllText(ConfigFile);
            Config c = JsonConvert.DeserializeObject<Config>(json);
            if (c.sounds == null)
                c.sounds = new System.Collections.Generic.Dictionary<string, SoundConfig>();
            if (c.zones == null)
                c.zones = new System.Collections.Generic.Dictionary<string, int>();
            if (c.schedule == null)
                c.schedule = new System.Collections.Generic.List<ScheduleItem>();

            ValidateConfig(c);
            c.RawText = json;
            return c;
        }

        public static void InitializeIfNecessary()
        {
            if (!File.Exists(ConfigFile))
            {
                InitializeConfigFile(ConfigFile);
            }

            string chimeFile = BaseDir + "\\chime.wav";
            if (!File.Exists(chimeFile))
            {
                InitializeChimeWav(chimeFile);
            }
        }

        private static void InitializeConfigFile(string configFile) {
            string json = GetDefaultConfigContents();
            File.WriteAllText(configFile, json);
        }

        private static void InitializeChimeWav(string chimeFile)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Chimer.chime.wav";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (FileStream writer = File.Open(chimeFile, FileMode.Create)) {
                    stream.CopyTo(writer);
                }
            }
        }

        private static string GetDefaultConfigContents() 
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Chimer.example_config.json";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        private static void ValidateConfig(Config c)
        {
            foreach (var keyValue in c.sounds)
            {
                try
                {
                    // Just see if we can load it successfully.
                    new CachedSound(keyValue.Value.file);
                }
                catch(Exception e) {
                    throw new ValidationException("Could not load sound file " + keyValue.Value, e);
                }
            }

            foreach (var keyValue in c.zones)
            {
                if (keyValue.Value < 0 || keyValue.Value > c.channels)
                {
                    throw new ValidationException("Invalid channel specified for zone " + keyValue.Key + ": " + keyValue.Value);
                }
            }

            foreach (ScheduleItem chime in c.schedule)
            {
                if (!c.zones.Keys.Contains(chime.zone))
                {
                    throw new ValidationException("Invalid zone encounterd while parsing schedule item: " + chime.zone);
                }
                foreach (string time in chime.times)
                {
                    try
                    {
                        DateTime test = DateTime.Parse(time);
                    } catch {
                        throw new ValidationException("Couldn't parse time: " + time);
                    }
                }

                if (!c.sounds.Keys.Contains(chime.sound))
                {
                    throw new ValidationException("Invalid sound encountered while parsing schedule item: " + chime.sound);
                }
            }
        }
    }

    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) {}
        public ValidationException(string message, Exception source) : base(message, source) { }
    }
}
