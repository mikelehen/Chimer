﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Chimer
{
#pragma warning disable 649 // never assigned fields.
    class Config
    {
        [JsonIgnore]
        public string RawText;

        public string inputDevice;
        public string outputDevice;
        public int inputLatency;
        public int outputLatency;
        public int inputVolume;
        public int inputThreshold;

        public Dictionary<string, string> sounds;
        public Dictionary<string, int> zones;
        public List<ScheduleItem> schedule;
    }
    
    class ScheduleItem
    {
        public List<string> zones;
        public string sound;
        public List<DayOfWeek> days;
        public List<string> times;
    }

#pragma warning restore 649
}
