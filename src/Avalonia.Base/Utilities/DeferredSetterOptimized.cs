using System;

namespace Avalonia.Utilities
{
    /// <summary>
    /// A utility class to enable deferring assignment until after property-changed notifications are sent.
    /// Used to fix #855.
    /// </summary>
    /// <typeparam name="TSetRecord">The type of value with which to track the delayed assignment.</typeparam>
    internal sealed class DeferredSetterOptimized<TSetRecord>
    {
        private bool _isNotifying;
        private readonly SingleOrQueue<TSetRecord> _pendingValues;

        public DeferredSetterOptimized()
        {
            _pendingValues = new SingleOrQueue<TSetRecord>();
        }

        public bool SetAndNotify(
            AvaloniaObject source,
            AvaloniaProperty<TSetRecord> property,
            ISetterHandler handler,
            ref TSetRecord backing,
            TSetRecord value)
        {
            if (!_isNotifying)
            {
                bool updated;

                using (new NotifyDisposable(this))
                {
                    updated = handler.Update(source, property, ref backing, value);
                }

                if (!_pendingValues.Empty)
                {
                    using (new NotifyDisposable(this))
                    {
                        while (!_pendingValues.Empty)
                        {
                            updated = handler.Update(source, property, ref backing, _pendingValues.Dequeue());
                        }
                    }
                }

                return updated;
            }

            _pendingValues.Enqueue(value);

            return false;
        }

        /// <summary>
        /// Disposable that marks the property as currently notifying.
        /// When disposed, marks the property as done notifying.
        /// </summary>
        private readonly struct NotifyDisposable : IDisposable
        {
            private readonly DeferredSetterOptimized<TSetRecord> _setter;

            internal NotifyDisposable(DeferredSetterOptimized<TSetRecord> setter)
            {
                _setter = setter;
                _setter._isNotifying = true;
            }

            public void Dispose()
            {
                _setter._isNotifying = false;
            }
        }

        public interface ISetterHandler
        {
            bool Update(
                AvaloniaObject source, 
                AvaloniaProperty<TSetRecord> property, 
                ref TSetRecord backing,
                TSetRecord value);
        }
    }
}
