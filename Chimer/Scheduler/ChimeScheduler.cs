using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Threading;

namespace Chimer.Scheduler
{
    class ChimeScheduler: IDisposable
    {
        private List<IEnumerator<ScheduledChime>> chimeEnumerators = new List<IEnumerator<ScheduledChime>>();
        private Dispatcher targetDispatcher;
        private Timer timer = new Timer();
        public ObservableCollection<ScheduledChime> UpcomingChimes = new ObservableCollection<ScheduledChime>();

        public ChimeScheduler(Config c)
        {
            targetDispatcher = Dispatcher.CurrentDispatcher;
            makeEnumerators(c);

            fireChimesAndReschedule();
        }

        public event EventHandler<ScheduledChime> Chime;

        private void makeEnumerators(Config c)
        {
            foreach (ChimeConfig chime in c.chimes)
            {
                foreach (DayOfWeek day in chime.days)
                {
                    foreach (string time in chime.times)
                    {
                        var enumerator = makeChimeEnumerator(day, time, chime.zone, chime.sound);
                        enumerator.MoveNext();
                        chimeEnumerators.Add(enumerator);
                    }
                }
            }
        }

        private void fireChimesAndReschedule()
        {
            DateTime now = DateTime.Now;
            DateTime nextChimeTime = DateTime.MaxValue;
            foreach (var chimeEnumerator in chimeEnumerators)
            {
                if (chimeEnumerator.Current.Time <= now)
                {
                    Chime(this, chimeEnumerator.Current);
                    chimeEnumerator.MoveNext();
                }

                if (chimeEnumerator.Current.Time <= nextChimeTime)
                {
                    nextChimeTime = chimeEnumerator.Current.Time;
                }
            }

            timer.Stop();
            if (nextChimeTime != DateTime.MaxValue)
            {
                timer.AutoReset = false;
                timer.Interval = (nextChimeTime - now).TotalMilliseconds;
                timer.Elapsed += (s, e) =>
                {
                    targetDispatcher.InvokeAsync(() =>
                    {
                        fireChimesAndReschedule();
                    });
                };
                timer.Start();
            }

            // Update upcoming chimes.
            UpcomingChimes.Clear();
            var orderedChimes = chimeEnumerators
                .Select(enumerator => enumerator.Current)
                .OrderBy(scheduledChime => scheduledChime.Time);
            foreach(var chime in orderedChimes) {
                UpcomingChimes.Add(chime);
            }
        }

        private IEnumerator<ScheduledChime> makeChimeEnumerator(DayOfWeek day, string time, string zone, string sound)
        {
            DateTime t = getFirstInstanceInTheFuture(day, time);

            while (true)
            {
                yield return new ScheduledChime
                {
                    Zone = zone,
                    Sound = sound,
                    Time = t
                };
                t = t.AddDays(7);
            }
        }

        private DateTime getFirstInstanceInTheFuture(DayOfWeek day, string timeString)
        {
            DateTime d = DateTime.Today;
            while (d.DayOfWeek != day)
            {
                d = d.Subtract(TimeSpan.FromDays(1));
            }

            DateTime time = DateTime.Parse(timeString);
            d = d.AddHours(time.Hour);
            d = d.AddMinutes(time.Minute);
            d = d.AddSeconds(time.Second);

            // Now fast-forward to the future!
            while (d < DateTime.Now)
            {
                d = d.AddDays(7);
            }
            return d;
        }

        struct GeneratorState
        {
            public IEnumerator<ScheduledChime> Enumerator;
            public DateTime Next;
        }

        public void Dispose()
        {
            timer.Stop();
        }
    }
}
