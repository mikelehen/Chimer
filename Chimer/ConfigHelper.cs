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
        public static Config Load(string configFile)
        {
            string json = File.ReadAllText(configFile);
            Config c = JsonConvert.DeserializeObject<Config>(json);
            if (c.sounds == null)
                c.sounds = new System.Collections.Generic.Dictionary<string, string>();
            if (c.zones == null)
                c.zones = new System.Collections.Generic.Dictionary<string, int>();
            if (c.schedule == null)
                c.schedule = new System.Collections.Generic.List<ScheduleItem>();

            ValidateConfig(c);
            c.RawText = json;
            return c;
        }

        private static void ValidateConfig(Config c)
        {
            foreach (var keyValue in c.sounds)
            {
                try
                {
                    // Just see if we can load it successfully.
                    new CachedSound(keyValue.Value);
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
                foreach (string zone in chime.zones)
                {
                    if (!c.zones.Keys.Contains(zone))
                    {
                        throw new ValidationException("Invalid zone encounterd while parsing schedule item: " + zone);
                    }
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
