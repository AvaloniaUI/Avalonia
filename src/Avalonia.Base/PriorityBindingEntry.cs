// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Runtime.ExceptionServices;
using Avalonia.Data;
using Avalonia.Threading;

namespace Avalonia
{
    /// <summary>
    /// A registered binding in a <see cref="PriorityValue"/>.
    /// </summary>
    internal class PriorityBindingEntry : IDisposable, IObserver<object>
    {
        private readonly PriorityLevel _owner;
        private IDisposable _subscription;

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityBindingEntry"/> class.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="index">
        /// The binding index. Later bindings should have higher indexes.
        /// </param>
        public PriorityBindingEntry(PriorityLevel owner, int index)
        {
            _owner = owner;
            Index = index;
        }

        /// <summary>
        /// Gets the observable associated with the entry.
        /// </summary>
        public IObservable<object> Observable { get; private set; }

        /// <summary>
        /// Gets a description of the binding.
        /// </summary>
        public string Description
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the binding entry index. Later bindings will have higher indexes.
        /// </summary>
        public int Index
        {
            get;
        }

        /// <summary>
        /// Gets a value indicating whether the binding has completed.
        /// </summary>
        public bool HasCompleted { get; private set; }

        /// <summary>
        /// The current value of the binding.
        /// </summary>
        public object Value
        {
            get;
            private set;
        }

        /// <summary>
        /// Starts listening to the binding.
        /// </summary>
        /// <param name="binding">The binding.</param>
        public void Start(IObservable<object> binding)
        {
            Contract.Requires<ArgumentNullException>(binding != null);

            if (_subscription != null)
            {
                throw new Exception("PriorityValue.Entry.Start() called more than once.");
            }

            Observable = binding;
            Value = AvaloniaProperty.UnsetValue;

            if (binding is IDescription)
            {
                Description = ((IDescription)binding).Description;
            }

            _subscription = binding.Subscribe(this);
        }

        /// <summary>
        /// Ends the binding subscription.
        /// </summary>
        public void Dispose()
        {
            _subscription?.Dispose();
        }

        void IObserver<object>.OnNext(object value)
        {
            void Signal(PriorityBindingEntry instance, object newValue)
            {
                var notification = newValue as BindingNotification;

                if (notification != null)
                {
                    if (notification.HasValue || notification.ErrorType == BindingErrorType.Error)
                    {
                        instance.Value = notification.Value;
                        instance._owner.Changed(instance);
                    }

                    if (notification.ErrorType != BindingErrorType.None)
                    {
                        instance._owner.Error(instance, notification);
                    }
                }
                else
                {
                    instance.Value = newValue;
                    instance._owner.Changed(instance);
                }
            }

            if (Dispatcher.UIThread.CheckAccess())
            {
                Signal(this, value);
            }
            else
            {
                // To avoid allocating closure in the outer scope we need to capture variables
                // locally. This allows us to skip most of the allocations when on UI thread.
                var instance = this;
                var newValue = value;

                Dispatcher.UIThread.Post(() => Signal(instance, newValue));
            }
        }

        void IObserver<object>.OnCompleted()
        {
            HasCompleted = true;

            if (Dispatcher.UIThread.CheckAccess())
            {
                _owner.Completed(this);
            }
            else
            {
                Dispatcher.UIThread.Post(() => _owner.Completed(this));
            }
        }

        void IObserver<object>.OnError(Exception error)
        {
            ExceptionDispatchInfo.Capture(error).Throw();
        }
    }
}
