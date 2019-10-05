// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Avalonia.Data;

namespace Avalonia
{
    /// <summary>
    /// Stores bindings for a priority level in a <see cref="PriorityValue"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each priority level in a <see cref="PriorityValue"/> has a current <see cref="Value"/>,
    /// a list of <see cref="Bindings"/> and a <see cref="DirectValue"/>. When there are no
    /// bindings present, or all bindings return <see cref="AvaloniaProperty.UnsetValue"/> then
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
        private object _directValue;
        private int _nextIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityLevel"/> class.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="priority">The priority.</param>
        public PriorityLevel(
            PriorityValue owner,
            int priority)
        {
            Contract.Requires<ArgumentNullException>(owner != null);

            Owner = owner;
            Priority = priority;
            Value = _directValue = AvaloniaProperty.UnsetValue;
            ActiveBindingIndex = -1;
            Bindings = new LinkedList<PriorityBindingEntry>();
        }

        /// <summary>
        /// Gets the owner of the level.
        /// </summary>
        public PriorityValue Owner { get; }

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
                Owner.LevelValueChanged(this);
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

            var entry = new PriorityBindingEntry(this, _nextIndex++);
            var node = Bindings.AddFirst(entry);

            entry.Start(binding);

            return new RemoveBindingDisposable(node, Bindings, this);
        }

        /// <summary>
        /// Invoked when an entry in <see cref="Bindings"/> changes value.
        /// </summary>
        /// <param name="entry">The entry that changed.</param>
        public void Changed(PriorityBindingEntry entry)
        {
            if (entry.Index >= ActiveBindingIndex)
            {
                if (entry.Value != AvaloniaProperty.UnsetValue)
                {
                    Value = entry.Value;
                    ActiveBindingIndex = entry.Index;
                    Owner.LevelValueChanged(this);
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
        public void Completed(PriorityBindingEntry entry)
        {
            Bindings.Remove(entry);

            if (entry.Index >= ActiveBindingIndex)
            {
                ActivateFirstBinding();
            }
        }

        /// <summary>
        /// Invoked when an entry in <see cref="Bindings"/> encounters a recoverable error.
        /// </summary>
        /// <param name="entry">The entry that completed.</param>
        /// <param name="error">The error.</param>
        public void Error(PriorityBindingEntry entry, BindingNotification error)
        {
            Owner.LevelError(this, error);
        }

        /// <summary>
        /// Activates the first binding that has a value.
        /// </summary>
        private void ActivateFirstBinding()
        {
            foreach (var binding in Bindings)
            {
                if (binding.Value != AvaloniaProperty.UnsetValue)
                {
                    Value = binding.Value;
                    ActiveBindingIndex = binding.Index;
                    Owner.LevelValueChanged(this);
                    return;
                }
            }

            Value = DirectValue;
            ActiveBindingIndex = -1;
            Owner.LevelValueChanged(this);
        }

        private sealed class RemoveBindingDisposable : IDisposable
        {
            private readonly LinkedList<PriorityBindingEntry> _bindings;
            private readonly PriorityLevel _priorityLevel;
            private LinkedListNode<PriorityBindingEntry> _binding;

            public RemoveBindingDisposable(
                LinkedListNode<PriorityBindingEntry> binding,
                LinkedList<PriorityBindingEntry> bindings,
                PriorityLevel priorityLevel)
            {
                _binding = binding;
                _bindings = bindings;
                _priorityLevel = priorityLevel;
            }

            public void Dispose()
            {
                LinkedListNode<PriorityBindingEntry> binding = Interlocked.Exchange(ref _binding, null);

                if (binding == null)
                {
                    // Some system is trying to remove binding twice.
                    Debug.Assert(false);

                    return;
                }

                PriorityBindingEntry entry = binding.Value;

                if (!entry.HasCompleted)
                {
                    _bindings.Remove(binding);

                    entry.Dispose();

                    if (entry.Index >= _priorityLevel.ActiveBindingIndex)
                    {
                        _priorityLevel.ActivateFirstBinding();
                    }
                }
            }
        }
    }
}
