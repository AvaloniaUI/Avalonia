using System;
using System.Collections.Generic;

namespace Perspex.Android.Platform.Specific
{
    public class CommonUITimer : IDisposable
    {
        private System.Timers.Timer _timer;
        private TimeSpan _interval;
        private Dictionary<Action, DateTime> _actions = new Dictionary<Action, DateTime>();

        public CommonUITimer(TimeSpan interval)
        {
            _timer = new System.Timers.Timer(interval.TotalMilliseconds);
            _interval = interval;
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
        }

        private void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            DateTime now = DateTime.Now;
            foreach (var kvp in _actions)
            {
                if ((now - kvp.Value) >= _interval)
                {
                    kvp.Key();
                }
            }
        }

        public void AddAction(Action action)
        {
            _actions[action] = DateTime.Now;
        }

        public void Dispose()
        {
            _timer.Elapsed -= _timer_Elapsed;
            _timer?.Stop();
        }

        public void RemoveAction(Action action)
        {
            _actions.Remove(action);
        }

        public int ActiveActionsCount => _actions.Count;
    }
}