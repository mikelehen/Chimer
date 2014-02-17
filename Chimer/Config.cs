using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Chimer
{
#pragma warning disable 649 // never assigned fields.
    class Config
    {
        [JsonIgnore]
        public string RawText;

        public int channels;
        public Dictionary<string, SoundConfig> sounds;
        public Dictionary<string, int> zones;
        public List<ScheduleItem> schedule;
    }
    
    class ScheduleItem
    {
        public string zone;
        public string sound;
        public List<DayOfWeek> days;
        public List<string> times;
    }

    class SoundConfig
    {
        public string file;
        public float volume;
    }
#pragma warning restore 649
}
