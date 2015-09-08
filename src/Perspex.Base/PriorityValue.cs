// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using Perspex.Utilities;

namespace Perspex
{
    /// <summary>
    /// Maintains a list of prioritised bindings together with a current value.
    /// </summary>
    /// <remarks>
    /// Bindings, in the form of <see cref="IObservable{Object}"/>s are added to the object using
    /// the <see cref="Add"/> method. With the observable is passed a priority, where lower values
    /// represent higher priorites. The current <see cref="Value"/> is selected from the highest
    /// priority binding that doesn't return <see cref="PerspexProperty.UnsetValue"/>. Where there
    /// are multiple bindings registered with the same priority, the most recently added binding
    /// has a higher priority. Each time the value changes, the <see cref="Changed"/> observable is
    /// fired with the old and new values.
    /// </remarks>
    internal class PriorityValue
    {
        /// <summary>
        /// The name of the property.
        /// </summary>
        private string _name;

        /// <summary>
        /// The value type.
        /// </summary>
        private Type _valueType;

        /// <summary>
        /// The currently registered bindings organised by priority.
        /// </summary>
        private Dictionary<int, PriorityLevel> _levels = new Dictionary<int, PriorityLevel>();

        /// <summary>
        /// The changed observable.
        /// </summary>
        private Subject<Tuple<object, object>> _changed = new Subject<Tuple<object, object>>();

        /// <summary>
        /// The current value.
        /// </summary>
        private object _value;

        /// <summary>
        /// The function used to validate the value, if any.
        /// </summary>
        private Func<object, object> _validate;

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityValue"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="valueType">The value type.</param>
        /// <param name="validate">An optional validation function.</param>
        public PriorityValue(string name, Type valueType, Func<object, object> validate = null)
        {
            _name = name;
            _valueType = valueType;
            _value = PerspexProperty.UnsetValue;
            ValuePriority = int.MaxValue;
            _validate = validate;
        }

        /// <summary>
        /// Fired whenever the current <see cref="Value"/> changes.
        /// </summary>
        /// <remarks>
        /// The old and new values may be the same, this class does not check for distinct values.
        /// </remarks>
        public IObservable<Tuple<object, object>> Changed => _changed;

        /// <summary>
        /// Gets the current value.
        /// </summary>
        public object Value => _value;

        /// <summary>
        /// Gets the priority of the binding that is currently active.
        /// </summary>
        public int ValuePriority
        {
            get;
            private set;
        }

        /// <summary>
        /// Adds a new binding.
        /// </summary>
        /// <param name="binding">The binding.</param>
        /// <param name="priority">The binding priority.</param>
        /// <returns>
        /// A disposable that will remove the binding.
        /// </returns>
        public IDisposable Add(IObservable<object> binding, int priority)
        {
            return GetLevel(priority).Add(binding);
        }

        /// <summary>
        /// Sets the direct value for a specified priority.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="priority">The priority</param>
        public void SetDirectValue(object value, int priority)
        {
            GetLevel(priority).DirectValue = value;
        }

        /// <summary>
        /// Gets the currently active bindings on this object.
        /// </summary>
        /// <returns>An enumerable collection of bindings.</returns>
        public IEnumerable<PriorityBindingEntry> GetBindings()
        {
            foreach (var level in _levels)
            {
                foreach (var binding in level.Value.Bindings)
                {
                    yield return binding;
                }
            }
        }

        /// <summary>
        /// Returns diagnostic string that can help the user debug the bindings in effect on
        /// this object.
        /// </summary>
        /// <returns>A diagnostic string.</returns>
        public string GetDiagnostic()
        {
            var b = new StringBuilder();
            var first = true;

            foreach (var level in _levels)
            {
                if (!first)
                {
                    b.AppendLine();
                }

                b.Append(ValuePriority == level.Key ? "*" : string.Empty);
                b.Append("Priority ");
                b.Append(level.Key);
                b.Append(": ");
                b.AppendLine(level.Value.Value?.ToString() ?? "(null)");
                b.AppendLine("--------");
                b.Append("Direct: ");
                b.AppendLine(level.Value.DirectValue?.ToString() ?? "(null)");

                foreach (var binding in level.Value.Bindings)
                {
                    b.Append(level.Value.ActiveBindingIndex == binding.Index ? "*" : string.Empty);
                    b.Append(binding.Description ?? binding.Observable.GetType().Name);
                    b.Append(": ");
                    b.AppendLine(binding.Value?.ToString() ?? "(null)");
                }

                first = false;
            }

            return b.ToString();
        }

        /// <summary>
        /// Causes a revalidation of the value.
        /// </summary>
        public void Revalidate()
        {
            if (_validate != null)
            {
                PriorityLevel level;

                if (_levels.TryGetValue(ValuePriority, out level))
                {
                    UpdateValue(level.Value, level.Priority);
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="PriorityLevel"/> with the specified priority, creating it if it
        /// doesn't already exist.
        /// </summary>
        /// <param name="priority">The priority.</param>
        /// <returns>The priority level.</returns>
        private PriorityLevel GetLevel(int priority)
        {
            PriorityLevel result;

            if (!_levels.TryGetValue(priority, out result))
            {
                var mode = (LevelPrecedenceMode)(priority % 2);
                result = new PriorityLevel(priority, mode, ValueChanged);
                _levels.Add(priority, result);
            }

            return result;
        }

        /// <summary>
        /// Updates the current <see cref="Value"/> and notifies all subscibers.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <param name="priority">The priority level that the value came from.</param>
        private void UpdateValue(object value, int priority)
        {
            if (!TypeUtilities.TryCast(_valueType, value, out value))
            {
                throw new InvalidOperationException(string.Format(
                    "Invalid value for Property '{0}': {1} ({2})",
                    _name,
                    value,
                    value?.GetType().FullName ?? "(null)"));
            }

            var old = _value;

            if (_validate != null)
            {
                value = _validate(value);
            }

            ValuePriority = priority;
            _value = value;
            _changed.OnNext(Tuple.Create(old, _value));
        }

        /// <summary>
        /// Called when the value for a priority level changes.
        /// </summary>
        /// <param name="level">The priority level of the changed entry.</param>
        private void ValueChanged(PriorityLevel level)
        {
            if (level.Priority <= ValuePriority)
            {
                if (level.Value != PerspexProperty.UnsetValue)
                {
                    UpdateValue(level.Value, level.Priority);
                }
                else
                {
                    foreach (var i in _levels.Values.OrderBy(x => x.Priority))
                    {
                        if (i.Value != PerspexProperty.UnsetValue)
                        {
                            UpdateValue(i.Value, i.Priority);
                            return;
                        }
                    }

                    UpdateValue(PerspexProperty.UnsetValue, int.MaxValue);
                }
            }
        }
    }
}
