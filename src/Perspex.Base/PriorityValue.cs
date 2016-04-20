// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Perspex.Data;
using Perspex.Logging;
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
    /// has a higher priority. Each time the value changes, the 
    /// <see cref="IPriorityValueOwner.Changed(PriorityValue, object, object)"/> method on the 
    /// owner object is fired with the old and new values.
    /// </remarks>
    internal class PriorityValue
    {
        private readonly IPriorityValueOwner _owner;
        private readonly Type _valueType;
        private readonly Dictionary<int, PriorityLevel> _levels = new Dictionary<int, PriorityLevel>();
        private object _value;
        private readonly Func<object, object> _validate;

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityValue"/> class.
        /// </summary>
        /// <param name="owner">The owner of the object.</param>
        /// <param name="property">The property that the value represents.</param>
        /// <param name="valueType">The value type.</param>
        /// <param name="validate">An optional validation function.</param>
        public PriorityValue(
            IPriorityValueOwner owner,
            PerspexProperty property, 
            Type valueType,
            Func<object, object> validate = null)
        {
            _owner = owner;
            Property = property;
            _valueType = valueType;
            _value = PerspexProperty.UnsetValue;
            ValuePriority = int.MaxValue;
            _validate = validate;
        }

        /// <summary>
        /// Gets the property that the value represents.
        /// </summary>
        public PerspexProperty Property { get; }

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
        /// <param name="validation">Validation settings for the binding.</param>
        /// <returns>
        /// A disposable that will remove the binding.
        /// </returns>
        public IDisposable Add(IObservable<object> binding, int priority)
        {
            return GetLevel(priority).Add(binding);
        }

        /// <summary>
        /// Sets the value for a specified priority.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="priority">The priority</param>
        public void SetValue(object value, int priority)
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
        /// Called when the value for a priority level changes.
        /// </summary>
        /// <param name="level">The priority level of the changed entry.</param>
        public void LevelValueChanged(PriorityLevel level)
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

        /// <summary>
        /// Called whenever a priority level validation state changes.
        /// </summary>
        /// <param name="priorityLevel">The priority level of the changed entry.</param>
        /// <param name="validationStatus">The validation status.</param>
        public void LevelValidation(PriorityLevel priorityLevel, IValidationStatus validationStatus)
        {
            _owner.DataValidationChanged(this, validationStatus);
        }

        /// <summary>
        /// Called when a priority level encounters an error.
        /// </summary>
        /// <param name="level">The priority level of the changed entry.</param>
        /// <param name="error">The binding error.</param>
        public void LevelError(PriorityLevel level, BindingError error)
        {
            Logger.Log(
                LogEventLevel.Error,
                LogArea.Binding,
                _owner,
                "Error binding to {Target}.{Property}: {Message}",
                _owner,
                Property,
                error.Exception.Message);
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
                result = new PriorityLevel(this, priority);
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
            object castValue;

            if (TypeUtilities.TryCast(_valueType, value, out castValue))
            {
                var old = _value;

                if (_validate != null && castValue != PerspexProperty.UnsetValue)
                {
                    castValue = _validate(castValue);
                }

                ValuePriority = priority;
                _value = castValue;
                _owner?.Changed(this, old, _value);
            }
            else
            {
                Logger.Error(
                    LogArea.Binding, 
                    _owner,
                    "Binding produced invalid value for {$Property} ({$PropertyType}): {$Value} ({$ValueType})",
                    Property.Name, 
                    _valueType, 
                    value,
                    value.GetType());
            }
        }
    }
}
