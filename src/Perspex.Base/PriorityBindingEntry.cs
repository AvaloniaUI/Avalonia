// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Data;

namespace Perspex
{
    /// <summary>
    /// A registered binding in a <see cref="PriorityValue"/>.
    /// </summary>
    internal class PriorityBindingEntry : IDisposable
    {
        private PriorityLevel _owner;
        private IDisposable _subscription;

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityBindingEntry"/> class.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="index">
        /// The binding index. Later bindings should have higher indexes.
        /// </param>
        /// <param name="validation">The validation settings for the binding.</param>
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
            Value = PerspexProperty.UnsetValue;

            if (binding is IDescription)
            {
                Description = ((IDescription)binding).Description;
            }

            _subscription = binding.Subscribe(ValueChanged, Completed);
        }

        /// <summary>
        /// Ends the binding subscription.
        /// </summary>
        public void Dispose()
        {
            _subscription?.Dispose();
        }

        private void ValueChanged(object value)
        {
            var bindingError = value as BindingError;

            if (bindingError != null)
            {
                _owner.Error(this, bindingError);
            }

            var validationStatus = value as IValidationStatus;

            if (validationStatus != null)
            {
                _owner.Validation(this, validationStatus);
            }
            else if (bindingError == null || bindingError.UseFallbackValue)
            {
                Value = bindingError == null ? value : bindingError.FallbackValue;
                _owner.Changed(this);
            }
        }

        private void Completed()
        {
            _owner.Completed(this);
        }
    }
}
