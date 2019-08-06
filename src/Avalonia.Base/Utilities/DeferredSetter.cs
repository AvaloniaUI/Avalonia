// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Utilities
{
    /// <summary>
    /// Callback invoked when deferred setter wants to set a value.
    /// </summary>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <param name="property">Property being set.</param>
    /// <param name="backing">Backing field reference.</param>
    /// <param name="value">New value.</param>
    internal delegate void SetAndNotifyCallback<TValue>(AvaloniaProperty property, ref TValue backing, TValue value);

    /// <summary>
    /// A utility class to enable deferring assignment until after property-changed notifications are sent.
    /// Used to fix #855.
    /// </summary>
    /// <typeparam name="TSetRecord">The type of value with which to track the delayed assignment.</typeparam>
    internal sealed class DeferredSetter<TSetRecord>
    {
        private readonly SingleOrQueue<TSetRecord> _pendingValues;
        private bool _isNotifying;

        public DeferredSetter()
        {
            _pendingValues = new SingleOrQueue<TSetRecord>();
        }

        private static void SetAndRaisePropertyChanged(AvaloniaObject source, AvaloniaProperty<TSetRecord> property, ref TSetRecord backing, TSetRecord value)
        {
            var old = backing;

            backing = value;

            source.RaisePropertyChanged(property, old, value);
        }

        public bool SetAndNotify(
            AvaloniaObject source,
            AvaloniaProperty<TSetRecord> property,
            ref TSetRecord backing,
            TSetRecord value)
        {
            if (!_isNotifying)
            {
                using (new NotifyDisposable(this))
                {
                    SetAndRaisePropertyChanged(source, property, ref backing, value);
                }

                if (!_pendingValues.Empty)
                {
                    using (new NotifyDisposable(this))
                    {
                        while (!_pendingValues.Empty)
                        {
                            SetAndRaisePropertyChanged(source, property, ref backing, _pendingValues.Dequeue());
                        }
                    }
                }

                return true;
            }

            _pendingValues.Enqueue(value);

            return false;
        }

        public bool SetAndNotifyCallback<TValue>(AvaloniaProperty property, SetAndNotifyCallback<TValue> setAndNotifyCallback, ref TValue backing, TValue value)
            where TValue : TSetRecord
        {
            if (!_isNotifying)
            {
                using (new NotifyDisposable(this))
                {
                    setAndNotifyCallback(property, ref backing, value);
                }

                if (!_pendingValues.Empty)
                {
                    using (new NotifyDisposable(this))
                    {
                        while (!_pendingValues.Empty)
                        {
                            setAndNotifyCallback(property, ref backing, (TValue) _pendingValues.Dequeue());
                        }
                    }
                }

                return true;
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
            private readonly DeferredSetter<TSetRecord> _setter;

            internal NotifyDisposable(DeferredSetter<TSetRecord> setter)
            {
                _setter = setter;
                _setter._isNotifying = true;
            }

            public void Dispose()
            {
                _setter._isNotifying = false;
            }
        }
    }
}
