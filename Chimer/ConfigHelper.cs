using System;
using System.Reflection;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Chimer
{
    class ConfigHelper
    {
        private static readonly string CONFIG_FILE = "config.json";

        public static string ConfigFile
        {
            get
            {
                string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return path + "\\" + CONFIG_FILE;
            }
        }

        public static Config Load()
        {
            string json = File.ReadAllText(ConfigFile);
            Config c = JsonConvert.DeserializeObject<Config>(json);
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
        }

        private static void InitializeConfigFile(string configFile) {
            string json = GetDefaultConfigContents();
            File.WriteAllText(configFile, json);
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
                if (!File.Exists(keyValue.Value))
                {
                    throw new ValidationException("Could not load sound file " + keyValue.Value);
                }
            }

            foreach (var keyValue in c.zones)
            {
                if (keyValue.Value < 0 || keyValue.Value > c.channels)
                {
                    throw new ValidationException("Invalid channel specified for zone " + keyValue.Key + ": " + keyValue.Value);
                }
            }

            foreach (ChimeConfig chime in c.chimes)
            {
                if (!c.zones.Keys.Contains(chime.zone))
                {
                    throw new ValidationException("Invalid zone encounterd while parsing chimes: " + chime.zone);
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
                    throw new ValidationException("Invalid sound encountered while parsing chimes: " + chime.sound);
                }
            }
        }
    }

    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message)
        {

        }
    }
}
