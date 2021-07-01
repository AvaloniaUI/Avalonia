using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using Avalonia.Collections.Pooled;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Logging;

#nullable enable

namespace Avalonia.PropertyStore
{
    internal class ValueStore
    {
        private int _styling;
        private readonly List<IValueFrame> _frames = new();
        private InheritanceFrame? _inheritanceFrame;
        private LocalValueFrame? _localValues;
        private Dictionary<int, EffectiveValue>? _effectiveValues;
        private Dictionary<int, EffectiveValue>? _nonAnimatedValues;

        public ValueStore(AvaloniaObject owner) => Owner = owner;

        public AvaloniaObject Owner { get; }
        public IReadOnlyList<IValueFrame> Frames => _frames;
        public InheritanceFrame? InheritanceFrame => _inheritanceFrame;

        public void BeginStyling() => ++_styling;

        public void EndStyling()
        {
            if (--_styling == 0)
                ReevaluateEffectiveValues();
        }

        public void AddFrame(IValueFrame style)
        {
            InsertFrame(style);

            if (_styling == 0)
                ReevaluateEffectiveValues();
        }

        public IDisposable AddBinding<T>(
            StyledPropertyBase<T> property,
            IObservable<BindingValue<T>> source,
            BindingPriority priority)
        {
            if (priority == BindingPriority.LocalValue)
            {
                if (_localValues is null)
                {
                    _localValues = new LocalValueFrame(this);
                    InsertFrame(_localValues);
                }

                // LocalValue bindings are subscribed immediately in LocalValueEntry so no need to
                // re-evaluate the effective value here.
                return _localValues.AddBinding(property, source);
            }
            else
            {
                var effectiveValue = GetEffectiveValue(property);
                var entry = new BindingEntry<T>(property, source, priority);
                InsertFrame(entry);

                if (priority <= effectiveValue.Priority)
                {
                    var oldValue = effectiveValue.Entry is object ?
                        effectiveValue.GetValue<T>() :
                        property.GetDefaultValue(Owner.GetType());
                    ReevaluateEffectiveValue<T>(property, oldValue);
                }
                else if (effectiveValue.Priority <= BindingPriority.Animation)
                {
                    ReevaluateNonAnimatedValue(property, entry);
                }

                return entry;
            }
        }

        public IDisposable AddBinding<T>(
            StyledPropertyBase<T> property,
            IObservable<T?> source,
            BindingPriority priority)
        {
            if (priority == BindingPriority.LocalValue)
            {
                if (_localValues is null)
                {
                    _localValues = new LocalValueFrame(this);
                    InsertFrame(_localValues);
                }

                // LocalValue bindings are subscribed immediately in LocalValueEntry so no need to
                // re-evaluate the effective value here.
                return _localValues.AddBinding(property, source);
            }
            else
            {
                var effectiveValue = GetEffectiveValue(property);
                var entry = new BindingEntry<T>(property, source, priority);
                InsertFrame(entry);

                if (priority <= effectiveValue.Priority)
                {
                    var oldValue = effectiveValue.Entry is object ?
                        effectiveValue.GetValue<T>() :
                        property.GetDefaultValue(Owner.GetType());
                    ReevaluateEffectiveValue<T>(property, oldValue);
                }
                else if (effectiveValue.Priority <= BindingPriority.Animation)
                {
                    ReevaluateNonAnimatedValue(property, entry);
                }

                return entry;
            }
        }

        public IDisposable AddBinding<T>(
            StyledPropertyBase<T> property,
            IObservable<object?> source,
            BindingPriority priority)
        {
            if (priority == BindingPriority.LocalValue)
            {
                if (_localValues is null)
                {
                    _localValues = new LocalValueFrame(this);
                    InsertFrame(_localValues);
                }

                // LocalValue bindings are subscribed immediately in LocalValueEntry so no need to
                // re-evaluate the effective value here.
                return _localValues.AddBinding(property, source);
            }
            else
            {
                var effectiveValue = GetEffectiveValue(property);
                var entry = new BindingEntry(property, source, priority);
                InsertFrame(entry);

                if (priority <= effectiveValue.Priority)
                {
                    var oldValue = effectiveValue.Entry is object ?
                        effectiveValue.GetValue() :
                        GetDefaultValue(property);
                    ReevaluateEffectiveValue(property, oldValue);
                }
                else if (effectiveValue.Priority <= BindingPriority.Animation)
                {
                    ReevaluateNonAnimatedValue(property, entry);
                }

                return entry;
            }
        }

        public void ClearLocalValue(AvaloniaProperty property)
        {
            _localValues?.ClearValue(property);
        }

        public void SetLocalValue<T>(StyledPropertyBase<T> property, T? value)
        {
            if (property.ValidateValue?.Invoke(value) == false)
            {
                throw new ArgumentException($"{value} is not a valid value for '{property.Name}.");
            }

            if (_localValues is null)
            {
                _localValues = new LocalValueFrame(this);
                InsertFrame(_localValues);
            }

            _localValues.SetValue(property, value);
        }

        public object? GetValue(AvaloniaProperty property)
        {
            if (_effectiveValues is object && _effectiveValues.TryGetValue(property.Id, out var value))
                return value.GetValue();
            return GetDefaultValue(property);
        }

        public T? GetValue<T>(StyledPropertyBase<T> property)
        {
            if (_effectiveValues is object && _effectiveValues.TryGetValue(property.Id, out var value))
                return value.GetValue<T>();
            return property.GetDefaultValue(Owner.GetType());
        }

        public bool IsAnimating(AvaloniaProperty property)
        {
            if (_effectiveValues is object && _effectiveValues.TryGetValue(property.Id, out var v))
                return v.Priority <= BindingPriority.Animation;
            return false;
        }

        public bool IsSet(AvaloniaProperty property)
        {
            if (_effectiveValues is object && _effectiveValues.TryGetValue(property.Id, out var v))
                return v.Priority < BindingPriority.Inherited;
            return false;
        }

        public Optional<T> GetBaseValue<T>(
            StyledPropertyBase<T> property,
            BindingPriority minPriority,
            BindingPriority maxPriority)
        {
            var frames = _frames;

            for (var i = frames.Count - 1; i >= 0; --i)
            {
                var frame = frames[i];

                if (frame.Priority < maxPriority || frame.Priority > minPriority)
                    continue;

                var values = frame.Values;

                for (var j = 0; j < values.Count; ++j)
                {
                    var value = values[j];

                    if (value.Property == property &&
                        value is IValueEntry<T> typed &&
                        typed.TryGetValue(out var result))
                    {
                        return result;
                    }
                }
            }

            return default;
        }

        public void CoerceValue<T>(StyledPropertyBase<T> property)
        {
            if (!property.HasCoercion)
                return;

            if (_effectiveValues is object && _effectiveValues.TryGetValue(property.Id, out var v))
                SetEffectiveValue(v.Entry!, v.Priority);
        }

        public void InheritanceParentChanged(ValueStore? newParent)
        {
            var newParentFrame = newParent?.GetInheritanceFrame();

            // If we don't have an inheritance frame, or we're not the owner of the inheritance
            // frame we can directly use the parent inheritance frame. Otherwise we need to
            // reparent the existing inheritance frame.
            if (_inheritanceFrame is null || _inheritanceFrame.Owner != this)
                SetInheritanceFrame(newParentFrame);
            else
                _inheritanceFrame.SetParent(newParentFrame);

            ReevaluateEffectiveValues();
        }

        /// <summary>
        /// Called by an <see cref="IValueEntry"/> to notify the value store that its value has changed.
        /// </summary>
        /// <param name="frame">The frame that the value belongs to.</param>
        /// <param name="value">The value entry.</param>
        /// <param name="oldValue">The old value of the value entry.</param>
        public void ValueChanged(
            IValueFrame frame,
            IValueEntry value,
            object? oldValue)
        {
            var property = value.Property;
            var effective = GetEffectiveValue(property);

            // Check if the changed value has higher or equal priority to the effective value.
            if (frame.Priority <= effective.Priority)
            {
                // If the changed value is not the effective value then the oldValue passed to us is of
                // no interest; we need to use the current effective value as the old value.
                if (effective.Entry != value)
                    oldValue = effective.Entry is object ?
                        effective.GetValue() :
                        GetDefaultValue(property);

                // Reevaluate the effective value.
                ReevaluateEffectiveValue(property, oldValue);
            }
            else if (effective.Priority == BindingPriority.Animation)
            {
                // The changed value is lower priority than the effective value but the effective value
                // is an animation: in this case we need to raise a non-effective value change
                // notification in order for transitions to work.
                ReevaluateNonAnimatedValue(property, value);
            }
        }

        /// <summary>
        /// Called by an <see cref="IValueEntry{T}"/> to notify the value store that its value has changed.
        /// </summary>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="frame">The frame that the value belongs to.</param>
        /// <param name="value">The value entry.</param>
        /// <param name="oldValue">The old value of the value entry.</param>
        public void ValueChanged<T>(
            IValueFrame frame,
            IValueEntry<T> value,
            Optional<T> oldValue)
        {
            var property = value.Property;
            var effective = GetEffectiveValue(property);

            // Check if the changed value has higher or equal priority than the effective value.
            if (frame.Priority <= effective.Priority)
            {
                // If the changed value is not the effective value then the oldValue passed to us is of
                // no interest; we need to use the current effective value as the old value.
                if (effective.Entry != value)
                    oldValue = effective.Entry is object ?
                        effective.GetValue<T>() :
                        property.GetDefaultValue(Owner.GetType());

                // Reevaluate the effective value.
                ReevaluateEffectiveValue(property, oldValue);
            }
            else if (effective.Priority == BindingPriority.Animation)
            {
                // The changed value is lower priority than the effective value but the effective value
                // is an animation: in this case we need to raise a non-effective value change
                // notification in order for transitions to work.
                ReevaluateNonAnimatedValue(property, value);
            }
        }

        public void FrameActivationChanged(IValueFrame frame)
        {
            ReevaluateEffectiveValues();
        }

        public void RemoveBindingEntry<T>(BindingEntry<T> entry, in Optional<T> oldValue)
        {
            _frames.Remove(entry);
            ReevaluateEffectiveValue(entry.Property, oldValue);
        }

        public void RemoveBindingEntry(BindingEntry entry, object? oldValue)
        {
            _frames.Remove(entry);
            ReevaluateEffectiveValue(entry.Property, oldValue);
        }

        private void InsertFrame(IValueFrame frame)
        {
            var index = _frames.BinarySearch(frame, FrameInsertionComparer.Instance);
            if (index < 0)
                index = ~index;
            _frames.Insert(index, frame);
            frame.SetOwner(this);
        }

        public void RemoveFrame(IValueFrame frame)
        {
            _frames.Remove(frame);
            if (_styling == 0)
                ReevaluateEffectiveValues();
        }

        public void InheritedValueChanged<T>(StyledPropertyBase<T> property, Optional<T> oldValue)
        {
            // The inheritance frame may not have been set up yet, if so exit: it should be set up
            // subsequently.
            if (_inheritanceFrame is null)
                return;

            // If the inherited value is set locally, propagation stops here.
            if (_inheritanceFrame!.Owner == this && _inheritanceFrame.TryGet(property, out _))
                return;

            ReevaluateEffectiveValue<T>(property, oldValue);
        }

        public AvaloniaPropertyValue GetDiagnostic(AvaloniaProperty property)
        {
            var effective = GetEffectiveValue(property);

            if (effective.Entry is object)
                return new AvaloniaPropertyValue(
                    property,
                    effective.GetValue(),
                    effective.Entry is object ? effective.Priority : BindingPriority.Unset);
            else
                return new AvaloniaPropertyValue(
                    property,
                    null,
                    BindingPriority.Unset);
        }

        private InheritanceFrame GetInheritanceFrame()
        {
            if (_inheritanceFrame is null)
            {
                _inheritanceFrame = new(this);

                if (_effectiveValues is object)
                {
                    foreach (var (_, v) in _effectiveValues)
                    {
                        if (v.Entry!.Property.Inherits)
                            _inheritanceFrame.SetValue(v.Entry);
                    }
                }

                InsertFrame(_inheritanceFrame);
            }

            return _inheritanceFrame;
        }

        private void ReevaluateEffectiveValue(AvaloniaProperty property, object? oldValue)
        {
            var newValue = AvaloniaProperty.UnsetValue;

            if (EvaluateEffectiveValue(property, out var value, out var priority))
                newValue = SetEffectiveValue(value, priority);
            else
                ClearEffectiveValue(property);

            RaisePropertyChanged(property, value, oldValue, newValue, priority, true);
        }

        private void ReevaluateEffectiveValue<T>(StyledPropertyBase<T> property, in Optional<T> oldValue)
        {
            BindingValue<T> newValue = default;

            if (EvaluateEffectiveValue(property, out var value, out var priority))
            {
                newValue = SetEffectiveValue(property, value, priority);
            }
            else
                ClearEffectiveValue(property);

            RaisePropertyChanged(property, value, oldValue, newValue, priority, true);
        }

        private void ReevaluateNonAnimatedValue(AvaloniaProperty property, IValueEntry changed)
        {
            if (EvaluateEffectiveValue(property, out var effective, out var priority, BindingPriority.LocalValue))
            {
                if (effective == changed)
                {
                    _nonAnimatedValues ??= new Dictionary<int, EffectiveValue>();
                    _nonAnimatedValues.Add(property.Id, EffectiveValue.Create(this, effective, priority));
                    effective.TryGetValue(out var newValue);
                    RaisePropertyChanged(property, changed, AvaloniaProperty.UnsetValue, newValue, priority, false);
                }
            }
            else
            {
                _nonAnimatedValues?.Remove(property.Id);
                var newValue = GetDefaultValue(property);
                RaisePropertyChanged(property, changed, AvaloniaProperty.UnsetValue, newValue, priority, false);
            }
        }

        private void ReevaluateNonAnimatedValue<T>(StyledPropertyBase<T> property, IValueEntry changed)
        {
            if (EvaluateEffectiveValue(property, out var effective, out var priority, BindingPriority.LocalValue))
            {
                if (effective == changed)
                {
                    _nonAnimatedValues ??= new Dictionary<int, EffectiveValue>();
                    _nonAnimatedValues[property.Id] = EffectiveValue.Create(this, property, effective, priority);

                    BindingValue<T> newValue;

                    if (effective is IValueEntry<T> typed)
                        newValue = typed.TryGetValue(out var value) ? value : BindingValue<T>.Unset;
                    else
                        newValue = effective.TryGetValue(out var value) ? (T?)value : BindingValue<T>.Unset;

                    RaisePropertyChanged<T>(property, changed, default, newValue, priority, false);
                }
            }
            else
            {
                _nonAnimatedValues?.Remove(property.Id);
                var newValue = property.GetDefaultValue(Owner.GetType());
                RaisePropertyChanged<T>(property, changed, default, newValue, priority, false);
            }
        }

        private bool EvaluateEffectiveValue(
            AvaloniaProperty property,
            [NotNullWhen(true)] out IValueEntry? result,
            out BindingPriority priority,
            BindingPriority maxPriority = BindingPriority.Animation)
        {
            var frames = _frames;

            for (var i = frames.Count - 1; i >= 0; --i)
            {
                var frame = frames[i];

                if (!frame.IsActive || frame.Priority < maxPriority)
                    continue;

                var values = frame.Values;

                for (var j = 0; j < values.Count; ++j)
                {
                    var value = values[j];

                    if (value.Property == property && value.HasValue)
                    {
                        priority = frame.Priority;
                        result = value;
                        return true;
                    }
                }
            }

            if (property.Inherits)
            {
                var frame = _inheritanceFrame;

                if (frame?.Owner == this)
                    frame = frame.Parent;

                while (frame is object)
                {
                    var values = frame.Values;

                    for (var j = 0; j < values.Count; ++j)
                    {
                        var value = values[j];

                        if (value.Property == property && value.HasValue)
                        {
                            priority = frame.Priority;
                            result = value;
                            return true;
                        }
                    }

                    frame = frame.Parent;
                }
            }

            result = default;
            priority = BindingPriority.Unset;
            return false;
        }

        private void ReevaluateEffectiveValues()
        {
            var foundValues = DictionaryPool<int, FoundValue>.Get();

            void ReevaluateFrame(IValueFrame frame)
            {
                if (!frame.IsActive)
                    return;

                var values = frame.Values;

                for (var j = 0; j < values.Count; ++j)
                {
                    var entry = values[j];
                    var property = entry.Property;

                    if (foundValues.TryGetValue(property.Id, out var found) && found == FoundValue.NonAnimated)
                        continue;
                    if (found == FoundValue.Animated && frame.Priority < BindingPriority.LocalValue)
                        continue;
                    if (!entry.HasValue)
                        continue;

                    entry.TryGetValue(out var newValue);

                    var oldValue = AvaloniaProperty.UnsetValue;
                    var oldValues = found != FoundValue.Animated ? _effectiveValues : _nonAnimatedValues;

                    if (oldValues is object && oldValues.TryGetValue(property.Id, out var oldValueEntry))
                    {
                        oldValue = oldValueEntry.GetValue();
                    }

                    if (found == FoundValue.None)
                    {
                        _effectiveValues ??= new();
                        _effectiveValues[property.Id] = EffectiveValue.Create(this, entry, frame.Priority);
                    }

                    RaisePropertyChanged(
                        property,
                        entry,
                        oldValue,
                        newValue,
                        frame.Priority,
                        found != FoundValue.Animated);

                    foundValues[property.Id] = frame.Priority >= BindingPriority.LocalValue ?
                        FoundValue.NonAnimated : FoundValue.Animated;
                }
            }

            var frames = _frames;
            for (var i = frames.Count - 1; i >= 0; --i)
            {
                ReevaluateFrame(frames[i]);
            }

            var inheritanceFrame = _inheritanceFrame;

            while (inheritanceFrame is object)
            {
                ReevaluateFrame(inheritanceFrame);
                inheritanceFrame = inheritanceFrame.Parent;
            }

            if (_effectiveValues is null)
                return;

            PooledList<int>? removedValues = null;

            foreach (var i in _effectiveValues)
            {
                if (!foundValues.ContainsKey(i.Key))
                {
                    removedValues ??= new PooledList<int>();
                    removedValues.Add(i.Key);
                }
            }

            if (removedValues is null)
                return;

            foreach (var id in removedValues)
            {
                var entry = _effectiveValues[id];
                var oldValue = entry.GetValue();

                _effectiveValues.Remove(id);
                _nonAnimatedValues?.Remove(id);

                RaisePropertyChanged(
                    entry.Entry!.Property,
                    null,
                    oldValue,
                    AvaloniaProperty.UnsetValue,
                    BindingPriority.Unset,
                    true);
            }

            removedValues.Dispose();
        }

        private EffectiveValue GetEffectiveValue(AvaloniaProperty property)
        {
            if (_effectiveValues is object && _effectiveValues.TryGetValue(property.Id, out var value))
                return value;
            return EffectiveValue.Unset;
        }

        private object? SetEffectiveValue(IValueEntry value, BindingPriority priority)
        {
            var propertyId = value.Property.Id;

            _effectiveValues ??= new();
            _effectiveValues[propertyId] = EffectiveValue.Create(this, value, priority);

            if (priority > BindingPriority.Animation)
                _nonAnimatedValues?.Remove(propertyId);

            value.TryGetValue(out var result);
            return result;
        }

        private T? SetEffectiveValue<T>(StyledPropertyBase<T> property, IValueEntry value, BindingPriority priority)
        {
            _effectiveValues ??= new();
            _effectiveValues[property.Id] = EffectiveValue.Create(this, property, value, priority);

            if (priority > BindingPriority.Animation)
                _nonAnimatedValues?.Remove(property.Id);

            if (value is IValueEntry<T> typed)
            {
                typed.TryGetValue(out var result);
                return result;
            }
            else
            {
                value.TryGetValue(out var result);
                return (T?)result;
            }
        }

        private void ClearEffectiveValue(AvaloniaProperty property)
        {
            _effectiveValues?.Remove(property.Id);
            _nonAnimatedValues?.Remove(property.Id);
        }

        private void SetInheritanceFrame(InheritanceFrame? frame)
        {
            _inheritanceFrame = frame;

            var childCount = Owner.GetInheritanceChildCount();

            for (var i = 0; i < childCount; ++i)
            {
                var child = Owner.GetInheritanceChild(i);
                child.GetValueStore().ParentInheritanceFrameChanged(frame);
            }
        }

        private void SetInheritanceFrameValue(IValueEntry entry)
        {
            var frame = _inheritanceFrame!;

            if (frame.Owner != this)
                frame = new InheritanceFrame(this, _inheritanceFrame);

            frame.SetValue(entry);
            SetInheritanceFrame(frame);
        }

        private void ParentInheritanceFrameChanged(InheritanceFrame? frame)
        {
            if (_inheritanceFrame?.Owner == this)
                _inheritanceFrame.SetParent(frame);
            else
                SetInheritanceFrame(frame);
        }

        private void InheritedValueChanged(AvaloniaProperty property)
        {
            // If the inherited value is set locally, propagation stops here.
            if (_inheritanceFrame!.Owner == this && _inheritanceFrame.TryGet(property, out _))
                return;

            ReevaluateEffectiveValue(property, GetValue(property));
        }

        private object? GetDefaultValue(AvaloniaProperty property)
        {
            return ((IStyledPropertyAccessor)property).GetDefaultValue(Owner.GetType());
        }

        private void RaisePropertyChanged(
            AvaloniaProperty property,
            IValueEntry? entry,
            object? oldValue,
            object? newValue,
            BindingPriority priority,
            bool isEffectiveValueChange)
        {
            if (priority < BindingPriority.Inherited &&
                entry is object &&
                _inheritanceFrame is object &&
                property.Inherits)
            {
                SetInheritanceFrameValue(entry);
            }

            if (isEffectiveValueChange)
            {
                if (oldValue == AvaloniaProperty.UnsetValue)
                    oldValue = GetDefaultValue(property);
                if (newValue == AvaloniaProperty.UnsetValue)
                    newValue = GetDefaultValue(property);
            }

            if (!Equals(oldValue, newValue))
                property.RaisePropertyChanged(Owner, oldValue, newValue, priority, isEffectiveValueChange);
        }

        private void RaisePropertyChanged<T>(
            StyledPropertyBase<T> property,
            IValueEntry? entry,
            Optional<T> oldValue,
            BindingValue<T> newValue,
            BindingPriority priority,
            bool isEffectiveValueChange)
        {
            if (priority < BindingPriority.Inherited &&
                entry is object &&
                _inheritanceFrame is object &&
                property.Inherits)
            {
                SetInheritanceFrameValue(entry);
            }

            if (isEffectiveValueChange)
            {
                if (!oldValue.HasValue)
                    oldValue = property.GetDefaultValue(Owner.GetType());
                if (!newValue.HasValue)
                    newValue = property.GetDefaultValue(Owner.GetType());
            }

            if (oldValue != newValue || !isEffectiveValueChange)
                Owner.RaisePropertyChanged(property, oldValue, newValue, priority, isEffectiveValueChange);
        }

        private readonly struct EffectiveValue
        {
            private EffectiveValue(IValueEntry? entry, BindingPriority priority, object? boxedValue)
            {
                Entry = entry;
                Priority = priority;
                BoxedValue = boxedValue;
            }

            public readonly IValueEntry? Entry;
            public readonly BindingPriority Priority;
            public readonly object? BoxedValue;
            public static readonly EffectiveValue Unset = new(null, BindingPriority.Unset, AvaloniaProperty.UnsetValue);

            public static EffectiveValue Create(ValueStore store, IValueEntry entry, BindingPriority priority)
            {
                var p = (IStyledPropertyAccessor)entry.Property;
                object? value;

                entry.TryGetValue(out value);

                if (p.HasCoercion)
                    value = p.CoerceValue(store.Owner, value);

                return new EffectiveValue(entry, priority, value);
            }

            public static EffectiveValue Create<T>(
                ValueStore store,
                StyledPropertyBase<T> property,
                IValueEntry entry,
                BindingPriority priority)
            {
                object? boxed;

                if (property.HasCoercion)
                {
                    entry.TryGetValue(out var v);
                    boxed = property.CoerceValue(store.Owner, (T?)v);
                }
                else
                {
                    entry.TryGetValue(out boxed);
                }

                return new EffectiveValue(entry, priority, boxed);
            }

            public object? GetValue() => BoxedValue;
            public T? GetValue<T>() => (T?)BoxedValue;
        }

        private class FrameInsertionComparer : IComparer<IValueFrame>
        {
            public static readonly FrameInsertionComparer Instance = new FrameInsertionComparer();
            public int Compare(IValueFrame? x, IValueFrame? y)
            {
                var result = y!.Priority - x!.Priority;
                return result != 0 ? result : -1;
            }
        }

        [Flags]
        private enum FoundValue
        {
            None,
            Animated = 1,
            NonAnimated = 2,
        }
    }
}
