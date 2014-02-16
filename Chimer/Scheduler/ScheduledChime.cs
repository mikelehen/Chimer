using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chimer.Scheduler
{
    class ScheduledChime
    {
        public DateTime Time { get; set; }
        public string Zone { get; set; }
        public string Sound { get; set; }
    }
}
