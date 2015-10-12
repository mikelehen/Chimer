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

            timer.AutoReset = false;
            timer.Elapsed += (s, e) =>
            {
                Console.WriteLine("Chiming at " + DateTime.Now.ToShortTimeString());
                targetDispatcher.InvokeAsync(() =>
                {
                    fireChimesAndReschedule();
                });
            };

            // Do initial scheduling.
            fireChimesAndReschedule();
        }

        public event EventHandler<ScheduledChime> Chime;

        private void makeEnumerators(Config c)
        {
            foreach (ScheduleItem chime in c.schedule)
            {
                foreach (DayOfWeek day in chime.days)
                {
                    foreach (string zone in chime.zones)
                    {
                        foreach (string time in chime.times)
                        {
                            var enumerator = makeChimeEnumerator(day, time, zone, chime.sound);
                            enumerator.MoveNext();
                            chimeEnumerators.Add(enumerator);
                        }
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
                timer.Interval = (nextChimeTime - now).TotalMilliseconds;
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

        private static DateTime getFirstInstanceInTheFuture(DayOfWeek day, string timeString)
        {
            DateTime target = DateTime.Parse(timeString);
            target = target.AddDays(daysBetween(target.DayOfWeek, day));
            if (target < DateTime.Now)
            {
                // It must have been today and the time is already passed.  Skip to next week.
                target = target.AddDays(7);
            }
            return target;
        }

        private static int daysBetween(DayOfWeek from, DayOfWeek to)
        {
            int days = to - from;
            if (days < 0)
            {
                days += 7;
            }
            return days;
        }

        public void Dispose()
        {
            timer.Stop();
            timer.Close();
        }
    }
}
