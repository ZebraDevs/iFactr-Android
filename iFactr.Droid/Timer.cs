using System;
using Android.Runtime;
using iFactr.UI;

namespace iFactr.Droid
{
    public class Timer : System.Timers.Timer, ITimer
    {
        public new event EventHandler Elapsed;

        public bool IsEnabled
        {
            get { return Enabled; }
            set { Enabled = value; }
        }

        [Preserve]
        public Timer()
        {
            base.Elapsed += OnElapsed;
        }

        protected virtual void OnElapsed(object sender, EventArgs e)
        {
            var timer = sender as Timer;
            if (timer == null) return;
            lock (timer)
            {
                if (!timer.IsEnabled) return;
                timer.IsEnabled = false;
                timer.Elapsed?.Invoke(timer, e);
            }
        }
    }
}