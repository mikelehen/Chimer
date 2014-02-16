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
        public Dictionary<string, string> sounds;
        public Dictionary<string, int> zones;
        public List<ChimeConfig> chimes;
    }
    
    class ChimeConfig
    {
        public string zone;
        public string sound;
        public List<DayOfWeek> days;
        public List<string> times;
    }
#pragma warning restore 649
}
