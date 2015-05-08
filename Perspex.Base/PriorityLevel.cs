// -----------------------------------------------------------------------
// <copyright file="PriorityLevel.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Disposables;

    /// <summary>
    /// Determines how the current binding is selected for a <see cref="PriorityLevel"/>.
    /// </summary>
    internal enum LevelPrecedenceMode
    {
        /// <summary>
        /// The latest fired binding is used as the current value.
        /// </summary>
        Latest,

        /// <summary>
        /// The latest added binding is used as the current value.
        /// </summary>
        Newest,
    }

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
    /// current value until the active binding fires again/
    /// </para>
    /// </remarks>
    internal class PriorityLevel
    {
        /// <summary>
        /// Method called when current value changes.
        /// </summary>
        private Action<PriorityLevel> changed;

        /// <summary>
        /// The current direct value.
        /// </summary>
        private object directValue;

        /// <summary>
        /// The index of the next <see cref="PriorityBindingEntry"/>.
        /// </summary>
        private int nextIndex;

        private LevelPrecedenceMode mode;

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityLevel"/> class.
        /// </summary>
        /// <param name="priority">The priority.</param>
        /// <param name="changed">A method to be called when the current value changes.</param>
        public PriorityLevel(
            int priority, 
            LevelPrecedenceMode mode,
            Action<PriorityLevel> changed)
        {
            Contract.Requires<ArgumentNullException>(changed != null);

            this.mode = mode;
            this.changed = changed;
            this.Priority = priority;
            this.Value = this.directValue = PerspexProperty.UnsetValue;
            this.ActiveBindingIndex = -1;
            this.Bindings = new LinkedList<PriorityBindingEntry>();
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
                return this.directValue;
            }

            set
            {
                this.Value = this.directValue = value;
                this.changed(this);
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

            var entry = new PriorityBindingEntry(this.nextIndex++);
            var node = this.Bindings.AddFirst(entry);

            entry.Start(binding, this.Changed, this.Completed);

            return Disposable.Create(() =>
            {
                this.Bindings.Remove(node);

                if (entry.Index >= this.ActiveBindingIndex)
                {
                    this.ActivateFirstBinding();
                }
            });
        }

        /// <summary> 
        /// Invoked when an entry in <see cref="Bindings"/> changes value.
        /// </summary>
        /// <param name="entry">The entry that changed.</param>
        private void Changed(PriorityBindingEntry entry)
        {
            if (mode == LevelPrecedenceMode.Latest || entry.Index >= this.ActiveBindingIndex)
            {
                if (entry.Value != PerspexProperty.UnsetValue)
                {
                    this.Value = entry.Value;
                    this.ActiveBindingIndex = entry.Index;
                    this.changed(this);
                }
                else
                {
                    this.ActivateFirstBinding();
                }
            }
        }

        /// <summary> 
        /// Invoked when an entry in <see cref="Bindings"/> completes.
        /// </summary>
        /// <param name="entry">The entry that completed.</param>
        private void Completed(PriorityBindingEntry entry)
        {
            this.Bindings.Remove(entry);

            if (entry.Index >= this.ActiveBindingIndex)
            {
                this.ActivateFirstBinding();
            }
        }

        /// <summary> 
        /// Activates the first binding that has a value.
        /// </summary>
        private void ActivateFirstBinding()
        {
            foreach (var binding in this.Bindings)
            {
                if (binding.Value != PerspexProperty.UnsetValue)
                {
                    this.Value = binding.Value;
                    this.ActiveBindingIndex = binding.Index;
                    this.changed(this);
                    return;
                }
            }

            this.Value = this.DirectValue;
            this.ActiveBindingIndex = -1;
            this.changed(this);
        }
    }
}
