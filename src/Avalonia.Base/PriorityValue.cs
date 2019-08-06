// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Data;
using Avalonia.Logging;
using Avalonia.Utilities;

namespace Avalonia
{
    /// <summary>
    /// Maintains a list of prioritized bindings together with a current value.
    /// </summary>
    /// <remarks>
    /// Bindings, in the form of <see cref="IObservable{Object}"/>s are added to the object using
    /// the <see cref="Add"/> method. With the observable is passed a priority, where lower values
    /// represent higher priorities. The current <see cref="Value"/> is selected from the highest
    /// priority binding that doesn't return <see cref="AvaloniaProperty.UnsetValue"/>. Where there
    /// are multiple bindings registered with the same priority, the most recently added binding
    /// has a higher priority. Each time the value changes, the 
    /// <see cref="IPriorityValueOwner.Changed"/> method on the 
    /// owner object is fired with the old and new values.
    /// </remarks>
    internal class PriorityValue
    {
        private readonly Type _valueType;
        private readonly SingleOrDictionary<int, PriorityLevel> _levels = new SingleOrDictionary<int, PriorityLevel>();

        private readonly Func<object, object> _validate;
        private readonly SetAndNotifyCallback<(object, int)> _setAndNotifyCallback;
        private (object value, int priority) _value;
        private DeferredSetter<object> _setter;

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityValue"/> class.
        /// </summary>
        /// <param name="owner">The owner of the object.</param>
        /// <param name="property">The property that the value represents.</param>
        /// <param name="valueType">The value type.</param>
        /// <param name="validate">An optional validation function.</param>
        public PriorityValue(
            IPriorityValueOwner owner,
            AvaloniaProperty property, 
            Type valueType,
            Func<object, object> validate = null)
        {
            Owner = owner;
            Property = property;
            _valueType = valueType;
            _value = (AvaloniaProperty.UnsetValue, int.MaxValue);
            _validate = validate;
            _setAndNotifyCallback = SetAndNotify;
        }

        /// <summary>
        /// Gets a value indicating whether the property is animating.
        /// </summary>
        public bool IsAnimating
        {
            get
            {
                return ValuePriority <= (int)BindingPriority.Animation && 
                    GetLevel(ValuePriority).ActiveBindingIndex != -1;
            }
        }

        /// <summary>
        /// Gets the owner of the value.
        /// </summary>
        public IPriorityValueOwner Owner { get; }

        /// <summary>
        /// Gets the property that the value represents.
        /// </summary>
        public AvaloniaProperty Property { get; }

        /// <summary>
        /// Gets the current value.
        /// </summary>
        public object Value => _value.value;

        /// <summary>
        /// Gets the priority of the binding that is currently active.
        /// </summary>
        public int ValuePriority => _value.priority;

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
                if (level.Value != AvaloniaProperty.UnsetValue)
                {
                    UpdateValue(level.Value, level.Priority);
                }
                else
                {
                    foreach (var i in _levels.Values.OrderBy(x => x.Priority))
                    {
                        if (i.Value != AvaloniaProperty.UnsetValue)
                        {
                            UpdateValue(i.Value, i.Priority);
                            return;
                        }
                    }

                    UpdateValue(AvaloniaProperty.UnsetValue, int.MaxValue);
                }
            }
        }

        /// <summary>
        /// Called when a priority level encounters an error.
        /// </summary>
        /// <param name="level">The priority level of the changed entry.</param>
        /// <param name="error">The binding error.</param>
        public void LevelError(PriorityLevel level, BindingNotification error)
        {
            Owner.LogError(Property, error.Error);
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
        /// Updates the current <see cref="Value"/> and notifies all subscribers.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <param name="priority">The priority level that the value came from.</param>
        private void UpdateValue(object value, int priority)
        {
            var newValue = (value, priority);

            if (newValue == _value)
            {
                return;
            }

            if (_setter == null)
            {
                _setter = Owner.GetNonDirectDeferredSetter(Property);
            }

            _setter.SetAndNotifyCallback(Property, _setAndNotifyCallback, ref _value, newValue);
        }

        private void SetAndNotify(AvaloniaProperty property, ref (object value, int priority) backing, (object value, int priority) update)
        {
            var val = update.value;
            var notification = val as BindingNotification;
            object castValue;

            if (notification != null)
            {
                val = (notification.HasValue) ? notification.Value : null;
            }

            if (TypeUtilities.TryConvertImplicit(_valueType, val, out castValue))
            {
                var old = backing.value;

                if (_validate != null && castValue != AvaloniaProperty.UnsetValue)
                {
                    castValue = _validate(castValue);
                }

                backing = (castValue, update.priority);

                if (notification?.HasValue == true)
                {
                    notification.SetValue(castValue);
                }

                if (notification == null || notification.HasValue)
                {
                    Owner?.Changed(Property, ValuePriority, old, Value);
                }

                if (notification != null)
                {
                    Owner?.BindingNotificationReceived(Property, notification);
                }
            }
            else
            {
                Logger.Error(
                    LogArea.Binding,
                    Owner,
                    "Binding produced invalid value for {$Property} ({$PropertyType}): {$Value} ({$ValueType})",
                    Property.Name,
                    _valueType,
                    val,
                    val?.GetType());
            }
        }
    }
}
