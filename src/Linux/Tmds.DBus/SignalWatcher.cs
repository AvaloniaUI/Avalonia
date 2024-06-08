using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Tmds.DBus
{
    /// <summary>
    /// Helper class for implementing D-Bus signals.
    /// </summary>
    public static class SignalWatcher
    {
        private class Disposable : IDisposable
        {
            public Disposable(Action disposeAction)
            {
                _disposeAction = disposeAction;
            }
            public void Dispose()
            {
                _disposeAction();
            }
            private Action _disposeAction;
        }

        private static IDisposable Add(object o, string eventName, object handler)
        {
            var eventInfo = o.GetType().GetEvent(eventName);
            var addMethod = eventInfo.GetAddMethod();
            var removeMethod = eventInfo.GetRemoveMethod();
            addMethod.Invoke(o, new object[] { handler });
            Action disposeAction = () => removeMethod.Invoke(o, new object[] { handler });
            return new Disposable(disposeAction);
        }

        /// <summary>
        /// Emits on the handler when the event is raised and returns an IDisposable that removes the handler.
        /// </summary>
        /// <param name="o">Object that emits events.</param>
        /// <param name="eventName">Name of the event.</param>
        /// <param name="handler">Action to be invoked when the event is raised.</param>
        /// <returns>
        /// Disposable that removes the handler from the event.
        /// </returns>
        public static Task<IDisposable> AddAsync<T>(object o, string eventName, Action<T> handler)
        {
            return Task.FromResult(Add(o, eventName, handler));
        }

        /// <summary>
        /// Emits on the handler when the event is raised and returns an IDisposable that removes the handler.
        /// </summary>
        /// <param name="o">Object that emits events.</param>
        /// <param name="eventName">Name of the event.</param>
        /// <param name="handler">Action to be invoked when the event is raised.</param>
        /// <returns>
        /// Disposable that removes the handler from the event.
        /// </returns>
        public static Task<IDisposable> AddAsync(object o, string eventName, Action handler)
        {
            return Task.FromResult(Add(o, eventName, handler));
        }
    }
}