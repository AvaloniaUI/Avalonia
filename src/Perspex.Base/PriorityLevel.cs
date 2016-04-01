// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace Perspex
{
    /// <summary>
    /// Stores bindings for a priority level in a <see cref="PriorityValue"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each priority level in a <see cref="PriorityValue"/> has a current <see cref="Value"/>,
    /// a list of <see cref="Bindings"/> and a <see cref="DirectValue"/>. When there are no
    /// bindings present, or all bindings return <see cref="PerspexProperty.UnsetValue"/> then
    /// <code>Value</code> will equal <code>DirectValue</code>.
    /// </para>
    /// <para>
    /// When there are bindings present, then the latest added binding that doesn't return
    /// <code>UnsetValue</code> will take precedence. The active binding is returned by the
    /// <see cref="ActiveBindingIndex"/> property (which refers to the active binding's
    /// <see cref="PriorityBindingEntry.Index"/> property rather than the index in
    /// <code>Bindings</code>).
    /// </para>
    /// <para>
    /// If <code>DirectValue</code> is set while a binding is active, then it will replace the
    /// current value until the active binding fires again.
    /// </para>
    /// </remarks>
    internal class PriorityLevel
    {
        /// <summary>
        /// Method called when current value changes.
        /// </summary>
        private readonly Action<PriorityLevel> _changed;

        /// <summary>
        /// The current direct value.
        /// </summary>
        private object _directValue;

        /// <summary>
        /// The index of the next <see cref="PriorityBindingEntry"/>.
        /// </summary>
        private int _nextIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityLevel"/> class.
        /// </summary>
        /// <param name="priority">The priority.</param>
        /// <param name="mode">The precedence mode.</param>
        /// <param name="changed">A method to be called when the current value changes.</param>
        public PriorityLevel(
            int priority,
            Action<PriorityLevel> changed)
        {
            Contract.Requires<ArgumentNullException>(changed != null);

            _changed = changed;
            Priority = priority;
            Value = _directValue = PerspexProperty.UnsetValue;
            ActiveBindingIndex = -1;
            Bindings = new LinkedList<PriorityBindingEntry>();
        }

        /// <summary>
        /// Gets the priority of this level.
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// Gets or sets the direct value for this priority level.
        /// </summary>
        public object DirectValue
        {
            get
            {
                return _directValue;
            }

            set
            {
                Value = _directValue = value;
                _changed(this);
            }
        }

        /// <summary>
        /// Gets the current binding for the priority level.
        /// </summary>
        public object Value { get; private set; }

        /// <summary>
        /// Gets the <see cref="PriorityBindingEntry.Index"/> value of the active binding, or -1
        /// if no binding is active.
        /// </summary>
        public int ActiveBindingIndex { get; private set; }

        /// <summary>
        /// Gets the bindings for the priority level.
        /// </summary>
        public LinkedList<PriorityBindingEntry> Bindings { get; }

        /// <summary>
        /// Adds a binding.
        /// </summary>
        /// <param name="binding">The binding to add.</param>
        /// <returns>A disposable used to remove the binding.</returns>
        public IDisposable Add(IObservable<object> binding)
        {
            Contract.Requires<ArgumentNullException>(binding != null);

            var entry = new PriorityBindingEntry(_nextIndex++);
            var node = Bindings.AddFirst(entry);

            entry.Start(binding, Changed, Completed);

            return Disposable.Create(() =>
            {
                Bindings.Remove(node);
                entry.Dispose();

                if (entry.Index >= ActiveBindingIndex)
                {
                    ActivateFirstBinding();
                }
            });
        }

        /// <summary>
        /// Invoked when an entry in <see cref="Bindings"/> changes value.
        /// </summary>
        /// <param name="entry">The entry that changed.</param>
        private void Changed(PriorityBindingEntry entry)
        {
            if (entry.Index >= ActiveBindingIndex)
            {
                if (entry.Value != PerspexProperty.UnsetValue)
                {
                    Value = entry.Value;
                    ActiveBindingIndex = entry.Index;
                    _changed(this);
                }
                else
                {
                    ActivateFirstBinding();
                }
            }
        }

        /// <summary>
        /// Invoked when an entry in <see cref="Bindings"/> completes.
        /// </summary>
        /// <param name="entry">The entry that completed.</param>
        private void Completed(PriorityBindingEntry entry)
        {
            Bindings.Remove(entry);

            if (entry.Index >= ActiveBindingIndex)
            {
                ActivateFirstBinding();
            }
        }

        /// <summary>
        /// Activates the first binding that has a value.
        /// </summary>
        private void ActivateFirstBinding()
        {
            foreach (var binding in Bindings)
            {
                if (binding.Value != PerspexProperty.UnsetValue)
                {
                    Value = binding.Value;
                    ActiveBindingIndex = binding.Index;
                    _changed(this);
                    return;
                }
            }

            Value = DirectValue;
            ActiveBindingIndex = -1;
            _changed(this);
        }
    }
}
