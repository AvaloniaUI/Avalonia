// -----------------------------------------------------------------------
// <copyright file="PriorityValueTests.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Disposables;

    internal class PriorityLevel
    {
        private Action<PriorityLevel> changed;

        private object directValue;

        private int nextIndex;

        public PriorityLevel(
            int priority, 
            Action<PriorityLevel> changed)
        {
            Contract.Requires<ArgumentNullException>(changed != null);

            this.changed = changed;
            this.Priority = priority;
            this.Value = this.directValue = PerspexProperty.UnsetValue;
            this.ActiveBindingIndex = -1;
            this.Bindings = new LinkedList<PriorityBindingEntry>();
        }

        public int Priority { get; }

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

        public object Value { get; private set; }

        public int ActiveBindingIndex { get; private set; }

        public LinkedList<PriorityBindingEntry> Bindings { get; }

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

        private void Changed(PriorityBindingEntry entry)
        {
            if (entry.Index >= this.ActiveBindingIndex)
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

        private void Completed(PriorityBindingEntry entry)
        {
            this.Bindings.Remove(entry);

            if (entry.Index >= this.ActiveBindingIndex)
            {
                this.ActivateFirstBinding();
            }
        }

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
