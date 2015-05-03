// -----------------------------------------------------------------------
// <copyright file="PriorityValue.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using Perspex.Diagnostics;
    using System;

    /// <summary>
    /// A registered binding in a <see cref="PriorityValue"/>.
    /// </summary>
    internal class PriorityBindingEntry : IDisposable
    {
        /// <summary>
        /// The binding subscription.
        /// </summary>
        private IDisposable subscription;

        public PriorityBindingEntry(int index)
        {
            this.Index = index;
        }

        /// <summary>
        /// Gets a description of the binding.
        /// </summary>
        public string Description
        {
            get;
            private set;
        }

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
        /// <param name="changed">Called when the binding changes.</param>
        /// <param name="completed">Called when the binding completes.</param>
        public void Start(
            IObservable<object> binding,
            Action<PriorityBindingEntry> changed,
            Action<PriorityBindingEntry> completed)
        {
            Contract.Requires<ArgumentNullException>(binding != null);
            Contract.Requires<ArgumentNullException>(changed != null);
            Contract.Requires<ArgumentNullException>(completed != null);

            if (this.subscription != null)
            {
                throw new Exception("PriorityValue.Entry.Start() called more than once.");
            }

            this.Value = PerspexProperty.UnsetValue;

            if (binding is IDescription)
            {
                this.Description = ((IDescription)binding).Description;
            }

            this.subscription = binding.Subscribe(
                value =>
                {
                    this.Value = value;
                    changed(this);
                },
                () => completed(this));
        }

        /// <summary>
        /// Ends the binding subscription.
        /// </summary>
        public void Dispose()
        {
            if (this.subscription != null)
            {
                this.subscription.Dispose();
            }
        }
    }
}
