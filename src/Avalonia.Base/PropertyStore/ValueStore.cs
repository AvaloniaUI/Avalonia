using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Collections.Pooled;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Logging;
using Avalonia.Utilities;
using JetBrains.Annotations;

namespace Avalonia.PropertyStore
{
    internal class ValueStore
    {
        private readonly List<ValueFrame> _frames = new();
        private Dictionary<int, IDisposable>? _localValueBindings;
        private AvaloniaPropertyDictionary<EffectiveValue> _effectiveValues;
        private int _inheritedValueCount;
        private int _frameGeneration;
        private int _styling;

        public ValueStore(AvaloniaObject owner) => Owner = owner;

        public AvaloniaObject Owner { get; }
        public ValueStore? InheritanceAncestor { get; private set; }
        public IReadOnlyList<ValueFrame> Frames => _frames;

        public void BeginStyling() => ++_styling;

        public void EndStyling()
        {
            if (--_styling == 0)
                ReevaluateEffectiveValues();
        }

        public void AddFrame(ValueFrame style)
        {
            InsertFrame(style);
            ReevaluateEffectiveValues();
        }

        public IDisposable AddBinding<T>(
            StyledPropertyBase<T> property,
            IObservable<BindingValue<T>> source,
            BindingPriority priority)
        {
            if (priority == BindingPriority.LocalValue)
            {
                var observer = new LocalValueBindingObserver<T>(this, property);
                DisposeExistingLocalValueBinding(property);
                _localValueBindings ??= new();
                _localValueBindings[property.Id] = observer;
                observer.Start(source);
                return observer;
            }
            else
            {
                var effective = GetEffectiveValue(property);
                var frame = GetOrCreateImmediateValueFrame(property, priority, out var frameIndex);
                var result = frame.AddBinding(property, source);

                if (effective is null || priority <= effective.Priority)
                {
                    result.Start();
                    UnsubscribeInactiveValues(frameIndex, property);
                }

                return result;
            }
        }

        public IDisposable AddBinding<T>(
            StyledPropertyBase<T> property,
            IObservable<T> source,
            BindingPriority priority)
        {
            if (priority == BindingPriority.LocalValue)
            {
                var observer = new LocalValueBindingObserver<T>(this, property);
                DisposeExistingLocalValueBinding(property);
                _localValueBindings ??= new();
                _localValueBindings[property.Id] = observer;
                observer.Start(source);
                return observer;
            }
            else
            {
                var effective = GetEffectiveValue(property);
                var frame = GetOrCreateImmediateValueFrame(property, priority, out var frameIndex);
                var result = frame.AddBinding(property, source);

                if (effective is null || priority <= effective.Priority)
                {
                    result.Start();
                    UnsubscribeInactiveValues(frameIndex, property);
                }

                return result;
            }
        }

        public IDisposable AddBinding<T>(
            StyledPropertyBase<T> property,
            IObservable<object?> source,
            BindingPriority priority)
        {
            if (priority == BindingPriority.LocalValue)
            {
                var observer = new LocalValueUntypedBindingObserver<T>(this, property);
                DisposeExistingLocalValueBinding(property);
                _localValueBindings ??= new();
                _localValueBindings[property.Id] = observer;
                observer.Start(source);
                return observer;
            }
            else
            {
                var effective = GetEffectiveValue(property);
                var frame = GetOrCreateImmediateValueFrame(property, priority, out var frameIndex);
                var result = frame.AddBinding(property, source);

                if (effective is null || priority <= effective.Priority)
                {
                    result.Start();
                    UnsubscribeInactiveValues(frameIndex, property);
                }

                return result;
            }
        }

        public IDisposable AddBinding<T>(DirectPropertyBase<T> property, IObservable<BindingValue<T>> source)
        {
            var observer = new DirectBindingObserver<T>(this, property);
            DisposeExistingLocalValueBinding(property);
            _localValueBindings ??= new();
            _localValueBindings[property.Id] = observer;
            observer.Start(source);
            return observer;
        }

        public IDisposable AddBinding<T>(DirectPropertyBase<T> property, IObservable<T> source)
        {
            var observer = new DirectBindingObserver<T>(this, property);
            DisposeExistingLocalValueBinding(property);
            _localValueBindings ??= new();
            _localValueBindings[property.Id] = observer;
            observer.Start(source);
            return observer;
        }

        public IDisposable AddBinding<T>(DirectPropertyBase<T> property, IObservable<object?> source)
        {
            var observer = new DirectUntypedBindingObserver<T>(this, property);
            DisposeExistingLocalValueBinding(property);
            _localValueBindings ??= new();
            _localValueBindings[property.Id] = observer;
            observer.Start(source);
            return observer;
        }

        public void ClearLocalValue(AvaloniaProperty property)
        {
            if (TryGetEffectiveValue(property, out var effective) &&
                effective.Priority == BindingPriority.LocalValue)
            {
                ReevaluateEffectiveValue(property, effective, ignoreLocalValue: true);
            }
        }

        public IDisposable? SetValue<T>(StyledPropertyBase<T> property, T value, BindingPriority priority)
        {
            if (property.ValidateValue?.Invoke(value) == false)
            {
                throw new ArgumentException($"{value} is not a valid value for '{property.Name}.");
            }

            IDisposable? result = null;

            if (priority != BindingPriority.LocalValue)
            {
                var frame = GetOrCreateImmediateValueFrame(property, priority, out var frameIndex);
                result = frame.AddValue(property, value);
                UnsubscribeInactiveValues(frameIndex, property);
            }

            if (TryGetEffectiveValue(property, out var existing))
            {
                var effective = (EffectiveValue<T>)existing;
                effective.SetAndRaise(this, property, value, priority);
            }
            else
            {
                AddEffectiveValueAndRaise(property, value, priority);
            }

            return result;
        }

        public object? GetValue(AvaloniaProperty property)
        {
            if (_effectiveValues.TryGetValue(property, out var v))
                return v.Value;
            if (property.Inherits && TryGetInheritedValue(property, out v))
                return v.Value;

            return GetDefaultValue(property);
        }

        public T GetValue<T>(StyledPropertyBase<T> property)
        {
            if (_effectiveValues.TryGetValue(property, out var v))
                return ((EffectiveValue<T>)v).Value;
            if (property.Inherits && TryGetInheritedValue(property, out v))
                return ((EffectiveValue<T>)v).Value;
            return property.GetDefaultValue(Owner.GetType());
        }

        public bool IsAnimating(AvaloniaProperty property)
        {
            if (_effectiveValues.TryGetValue(property, out var v))
                return v.Priority <= BindingPriority.Animation;
            return false;
        }

        public bool IsSet(AvaloniaProperty property)
        {
            if (_effectiveValues.TryGetValue(property, out var v))
                return v.Priority < BindingPriority.Inherited;
            return false;
        }

        public void CoerceValue(AvaloniaProperty property)
        {
            if (_effectiveValues.TryGetValue(property, out var v))
                v.CoerceValue(this, property);
        }

        public Optional<T> GetBaseValue<T>(StyledPropertyBase<T> property)
        {
            if (TryGetEffectiveValue(property, out var v) &&
                ((EffectiveValue<T>)v).TryGetBaseValue(out var baseValue))
            {
                return baseValue;
            }

            return default;
        }

        public bool TryGetInheritedValue(
            AvaloniaProperty property,
            [NotNullWhen(true)] out EffectiveValue? result)
        {
            Debug.Assert(property.Inherits);

            var i = InheritanceAncestor;

            while (i is not null)
            {
                if (i.TryGetEffectiveValue(property, out result))
                    return true;
                i = i.InheritanceAncestor;
            }

            result = null;
            return false;
        }

        public void SetInheritanceParent(AvaloniaObject? oldParent, AvaloniaObject? newParent)
        {
            var values = AvaloniaPropertyDictionaryPool<OldNewValue>.Get();
            var oldAncestor = InheritanceAncestor;
            var newAncestor = newParent?.GetValueStore();

            if (newAncestor?._inheritedValueCount == 0)
                newAncestor = newAncestor.InheritanceAncestor;

            // The old and new inheritance ancestors are the same, nothing to do here.
            if (oldAncestor == newAncestor)
                return;

            // First get the old values from the old inheritance ancestor.
            var f = oldAncestor;

            while (f is not null)
            {
                var count = f._effectiveValues.Count;

                for (var i = 0; i < count; ++i)
                {
                    f._effectiveValues.GetKeyValue(i, out var key, out var value);
                    if (key.Inherits)
                        values.TryAdd(key, new(value));
                }

                f = f.InheritanceAncestor;
            }

            f = newAncestor;

            // Get the new values from the new inheritance ancestor.
            while (f is not null)
            {
                var count = f._effectiveValues.Count;

                for (var i = 0; i < count; ++i)
                {
                    f._effectiveValues.GetKeyValue(i, out var key, out var value);

                    if (!key.Inherits)
                        continue;

                    if (values.TryGetValue(key, out var existing))
                        values[key] = existing.WithNewValue(value);
                    else
                        values.Add(key, new(null, value));
                }

                f = f.InheritanceAncestor;
            }

            OnInheritanceAncestorChanged(newAncestor);

            // Raise PropertyChanged events where necessary on this object and inheritance children.
            {
                var count = values.Count;
                for (var i = 0; i < count; ++i)
                {
                    values.GetKeyValue(i, out var key, out var v);
                    var oldValue = v.OldValue;
                    var newValue = v.NewValue;

                    if (oldValue != newValue)
                        InheritedValueChanged(key, oldValue, newValue);
                }
            }

            AvaloniaPropertyDictionaryPool<OldNewValue>.Release(values);
        }

        /// <summary>
        /// Called by non-LocalValue binding entries to re-evaluate the effective value when the
        /// binding produces a new value.
        /// </summary>
        /// <param name="property">The bound property.</param>
        /// <param name="priority">The priority of binding which produced a new value.</param>
        /// <param name="value">The new value.</param>
        public void OnBindingValueChanged(
            AvaloniaProperty property, 
            BindingPriority priority,
            object? value)
        {
            Debug.Assert(priority != BindingPriority.LocalValue);

            if (TryGetEffectiveValue(property, out var existing))
            {
                if (priority <= existing.Priority)
                    ReevaluateEffectiveValue(property, existing);
            }
            else
            {
                AddEffectiveValueAndRaise(property, value, priority);
            }
        }

        /// <summary>
        /// Called by non-LocalValue binding entries to re-evaluate the effective value when the
        /// binding produces a new value.
        /// </summary>
        /// <param name="property">The bound property.</param>
        /// <param name="priority">The priority of binding which produced a new value.</param>
        /// <param name="value">The new value.</param>
        public void OnBindingValueChanged<T>(
            StyledPropertyBase<T> property,
            BindingPriority priority,
            T value)
        {
            Debug.Assert(priority != BindingPriority.LocalValue);

            if (TryGetEffectiveValue(property, out var existing))
            {
                if (priority <= existing.Priority)
                    ReevaluateEffectiveValue(property, existing);
            }
            else
            {
                AddEffectiveValueAndRaise(property, value, priority);
            }
        }

        /// <summary>
        /// Called by non-LocalValue binding entries to re-evaluate the effective value when the
        /// binding produces an unset value.
        /// </summary>
        /// <param name="property">The bound property.</param>
        /// <param name="priority">The priority of binding which produced a new value.</param>
        public void OnBindingValueCleared(AvaloniaProperty property, BindingPriority priority)
        {
            Debug.Assert(priority != BindingPriority.LocalValue);

            if (TryGetEffectiveValue(property, out var existing))
            {
                if (priority <= existing.Priority)
                    ReevaluateEffectiveValue(property, existing);
            }
        }

        /// <summary>
        /// Called by a <see cref="BindingEntry{T}"/> to re-evaluate the effective value when the
        /// binding completes or terminates on error.
        /// </summary>
        /// <param name="property">The previously bound property.</param>
        /// <param name="frame">The frame which contained the binding.</param>
        public void OnBindingCompleted(AvaloniaProperty property, ValueFrame frame)
        {
            var priority = frame.Priority;

            if (TryGetEffectiveValue(property, out var existing))
            {
                if (priority <= existing.Priority)
                    ReevaluateEffectiveValue(property, existing);
            }
        }

        /// <summary>
        /// Called by a <see cref="ValueFrame"/> when its <see cref="ValueFrame.IsActive"/>
        /// state changes.
        /// </summary>
        /// <param name="frame">The frame which produced the change.</param>
        public void OnFrameActivationChanged(ValueFrame frame)
        {
            if (frame.EntryCount == 0)
                return;
            else if (frame.EntryCount == 1)
            {
                var property = frame.GetEntry(0).Property;
                _effectiveValues.TryGetValue(property, out var current);
                ReevaluateEffectiveValue(property, current);
            }
            else
                ReevaluateEffectiveValues();
        }

        /// <summary>
        /// Called by the parent value store when its inheritance ancestor changes.
        /// </summary>
        /// <param name="ancestor">The new inheritance ancestor.</param>
        public void OnInheritanceAncestorChanged(ValueStore? ancestor)
        {
            if (ancestor != this)
            {
                InheritanceAncestor = ancestor;
                if (_inheritedValueCount > 0)
                    return;
            }

            var children = Owner.GetInheritanceChildren();

            if (children is null)
                return;

            var count = children.Count;

            for (var i = 0; i < count; ++i)
            {
                children[i].GetValueStore().OnInheritanceAncestorChanged(ancestor);
            }
        }

        /// <summary>
        /// Called by <see cref="EffectiveValue{T}"/> when an property with inheritance enabled
        /// changes its value on this value store.
        /// </summary>
        /// <param name="property">The property whose value changed.</param>
        /// <param name="oldValue">The old value of the property.</param>
        /// <param name="value">The effective value instance.</param>
        public void OnInheritedEffectiveValueChanged<T>(
            StyledPropertyBase<T> property,
            T oldValue,
            EffectiveValue<T> value)
        {
            Debug.Assert(property.Inherits);

            var children = Owner.GetInheritanceChildren();

            if (children is null)
                return;

            var count = children.Count;

            for (var i = 0; i < count; ++i)
            {
                children[i].GetValueStore().OnAncestorInheritedValueChanged(property, oldValue, value.Value);
            }
        }

        /// <summary>
        /// Called by <see cref="EffectiveValue{T}"/> when an property with inheritance enabled
        /// is removed from the effective values.
        /// </summary>
        /// <param name="property">The property whose value changed.</param>
        /// <param name="oldValue">The old value of the property.</param>
        public void OnInheritedEffectiveValueDisposed<T>(StyledPropertyBase<T> property, T oldValue)
        {
            Debug.Assert(property.Inherits);

            var children = Owner.GetInheritanceChildren();

            if (children is not null)
            {
                var defaultValue = property.GetDefaultValue(Owner.GetType());
                var count = children.Count;

                for (var i = 0; i < count; ++i)
                {
                    children[i].GetValueStore().OnAncestorInheritedValueChanged(property, oldValue, defaultValue);
                }
            }
        }

        /// <summary>
        /// Called when a <see cref="LocalValueBindingObserver{T}"/> or
        /// <see cref="DirectBindingObserver{T}"/> completes.
        /// </summary>
        /// <param name="property">The previously bound property.</param>
        /// <param name="observer">The observer.</param>
        public void OnLocalValueBindingCompleted(AvaloniaProperty property, IDisposable observer)
        {
            if (_localValueBindings is not null &&
                _localValueBindings.TryGetValue(property.Id, out var existing))
            {
                if (existing == observer)
                {
                    _localValueBindings?.Remove(property.Id);
                    ClearLocalValue(property);
                }
            }
        }

        /// <summary>
        /// Called when an inherited property changes on the value store of the inheritance ancestor.
        /// </summary>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="oldValue">The old value of the property.</param>
        /// <param name="newValue">The new value of the property.</param>
        public void OnAncestorInheritedValueChanged<T>(
            StyledPropertyBase<T> property, 
            T oldValue,
            T newValue)
        {
            Debug.Assert(property.Inherits);

            // If the inherited value is set locally, propagation stops here.
            if (_effectiveValues.ContainsKey(property))
                return;

            using var notifying = PropertyNotifying.Start(Owner, property);

            Owner.RaisePropertyChanged(
                property,
                oldValue,
                newValue,
                BindingPriority.Inherited,
                true);

            var children = Owner.GetInheritanceChildren();

            if (children is null)
                return;

            var count = children.Count;

            for (var i = 0; i < count; ++i)
            {
                children[i].GetValueStore().OnAncestorInheritedValueChanged(property, oldValue, newValue);
            }
        }

        /// <summary>
        /// Called by a <see cref="ValueFrame"/> to re-evaluate the effective value when a value
        /// is removed.
        /// </summary>
        /// <param name="frame">The frame on which the change occurred.</param>
        /// <param name="property">The property whose value was removed.</param>
        public void OnValueEntryRemoved(ValueFrame frame, AvaloniaProperty property)
        {
            Debug.Assert(frame.IsActive);

            if (TryGetEffectiveValue(property, out var existing))
            {
                if (frame.Priority <= existing.Priority)
                    ReevaluateEffectiveValue(property, existing);
            }
            else
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.Property)?.Log(
                    Owner,
                    "Internal error: ValueStore.OnEntryRemoved called for {Property} " +
                    "but no effective value was found.",
                    property);
                Debug.Assert(false);
            }
        }

        public bool RemoveFrame(ValueFrame frame)
        {
            if (_frames.Remove(frame))
            {
                frame.Dispose();
                ++_frameGeneration;
                ReevaluateEffectiveValues();
            }

            return false;
        }

        public AvaloniaPropertyValue GetDiagnostic(AvaloniaProperty property)
        {
            var effective = GetEffectiveValue(property);
            return new AvaloniaPropertyValue(
                property,
                effective?.Value,
                effective?.Priority ?? BindingPriority.Unset,
                null);
        }

        private int InsertFrame(ValueFrame frame)
        {
            // Uncomment this line when #8549 is fixed.
            //Debug.Assert(!_frames.Contains(frame));

            var index = BinarySearchFrame(frame.Priority);
            _frames.Insert(index, frame);
            ++_frameGeneration;
            frame.SetOwner(this);
            return index;
        }

        private ImmediateValueFrame GetOrCreateImmediateValueFrame(
            AvaloniaProperty property, 
            BindingPriority priority,
            out int frameIndex)
        {
            Debug.Assert(priority != BindingPriority.LocalValue);

            var index = BinarySearchFrame(priority);

            if (index > 0 && _frames[index - 1] is ImmediateValueFrame f &&
                f.Priority == priority &&
                !f.Contains(property))
            {
                frameIndex = index - 1;
                return f;
            }

            var result = new ImmediateValueFrame(priority);
            frameIndex = InsertFrame(result);
            return result;
        }

        private void AddEffectiveValue(AvaloniaProperty property, EffectiveValue effectiveValue)
        {
            _effectiveValues.Add(property, effectiveValue);

            if (property.Inherits && _inheritedValueCount++ == 0)
                OnInheritanceAncestorChanged(this);
        }

        /// <summary>
        /// Adds a new effective value, raises the initial <see cref="AvaloniaObject.PropertyChanged"/>
        /// event and notifies inheritance children if necessary .
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The property value.</param>
        /// <param name="priority">The value priority.</param>
        private void AddEffectiveValueAndRaise(AvaloniaProperty property, object? value, BindingPriority priority)
        {
            Debug.Assert(priority < BindingPriority.Inherited);
            var effectiveValue = property.CreateEffectiveValue(Owner);
            AddEffectiveValue(property, effectiveValue);
            effectiveValue.SetAndRaise(this, property, value, priority);
        }

        /// <summary>
        /// Adds a new effective value, raises the initial <see cref="AvaloniaObject.PropertyChanged"/>
        /// event and notifies inheritance children if necessary .
        /// </summary>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="value">The property value.</param>
        /// <param name="priority">The value priority.</param>
        private void AddEffectiveValueAndRaise<T>(StyledPropertyBase<T> property, T value, BindingPriority priority)
        {
            Debug.Assert(priority < BindingPriority.Inherited);
            var defaultValue = property.GetDefaultValue(Owner.GetType());
            var effectiveValue = new EffectiveValue<T>(Owner, property, defaultValue, BindingPriority.Unset);
            AddEffectiveValue(property, effectiveValue);
            effectiveValue.SetAndRaise(this, property, value, priority);
        }

        private bool RemoveEffectiveValue(AvaloniaProperty property)
        {
            if (_effectiveValues.Remove(property))
            {
                if (property.Inherits && --_inheritedValueCount == 0)
                    OnInheritanceAncestorChanged(InheritanceAncestor);
                return true;
            }

            return false;
        }

        private bool RemoveEffectiveValue(AvaloniaProperty property, [NotNullWhen(true)] out EffectiveValue? result)
        {
            if (_effectiveValues.Remove(property, out result))
            {
                if (property.Inherits && --_inheritedValueCount == 0)
                    OnInheritanceAncestorChanged(InheritanceAncestor);
                return true;
            }

            result = null;
            return false;
        }

        private void InheritedValueChanged(
            AvaloniaProperty property,
            EffectiveValue? oldValue,
            EffectiveValue? newValue)
        {
            Debug.Assert(oldValue != newValue);
            Debug.Assert(oldValue is not null || newValue is not null);

            // If the value is set locally, propagaton ends here.
            if (_effectiveValues.ContainsKey(property) == true)
                return;

            using var notifying = PropertyNotifying.Start(Owner, property);

            // Raise PropertyChanged on this object if necessary.
            (oldValue ?? newValue!).RaiseInheritedValueChanged(Owner, property, oldValue, newValue);

            var children = Owner.GetInheritanceChildren();

            if (children is null)
                return;

            var count = children.Count;

            for (var i = 0; i < count; ++i)
            {
                children[i].GetValueStore().InheritedValueChanged(property, oldValue, newValue);
            }
        }

        private void ReevaluateEffectiveValue(
            AvaloniaProperty property,
            EffectiveValue? current,
            bool ignoreLocalValue = false)
        {
        restart:
            // Don't reevaluate if a styling pass is in effect, reevaluation will be done when
            // it has finished.
            if (_styling > 0)
                return;

            var generation = _frameGeneration;

            // Reset all non-LocalValue effective value to Unset priority.
            if (current is not null)
            {
                if (ignoreLocalValue || current.Priority != BindingPriority.LocalValue)
                    current.SetPriority(BindingPriority.Unset);
                if (ignoreLocalValue || current.BasePriority != BindingPriority.LocalValue)
                    current.SetBasePriority(BindingPriority.Unset);
            }

            // Iterate the frames to get the effective value.
            for (var i = _frames.Count - 1; i >= 0; --i)
            {
                var frame = _frames[i];
                var priority = frame.Priority;

                if (frame.TryGetEntry(property, out var entry) &&
                    frame.IsActive &&
                    entry.HasValue)
                {
                    if (current is not null)
                    {
                        current.SetAndRaise(this, entry, priority);
                    }
                    else
                    {
                        current = property.CreateEffectiveValue(Owner);
                        AddEffectiveValue(property, current);
                        current.SetAndRaise(this, entry, priority);
                    }
                }

                if (generation != _frameGeneration)
                    goto restart;

                if (current is not null &&
                    current.Priority < BindingPriority.Unset &&
                    current.BasePriority < BindingPriority.Unset)
                {
                    UnsubscribeInactiveValues(i, property);
                    return;
                }
            }

            if (current?.Priority == BindingPriority.Unset)
            {
                if (current.BasePriority == BindingPriority.Unset)
                {
                    RemoveEffectiveValue(property);
                    current.DisposeAndRaiseUnset(this, property);
                }
                else
                {
                    current.SetAndRaise(this, property, current.BaseValue, current.BasePriority);
                }
            }
        }

        private void ReevaluateEffectiveValues()
        {
        restart:
            // Don't reevaluate if a styling pass is in effect, reevaluation will be done when
            // it has finished.
            if (_styling > 0)
                return;

            var generation = _frameGeneration;
            var count = _effectiveValues.Count;

            // Reset all non-LocalValue effective values to Unset priority.
            for (var i = 0; i < count; ++i)
            {
                var e = _effectiveValues[i];

                if (e.Priority != BindingPriority.LocalValue)
                    e.SetPriority(BindingPriority.Unset);
                if (e.BasePriority != BindingPriority.LocalValue)
                    e.SetBasePriority(BindingPriority.Unset);
            }

            // Iterate the frames, setting and creating effective values.
            for (var i = _frames.Count - 1; i >= 0; --i)
            {
                var frame = _frames[i];

                if (!frame.IsActive)
                    continue;

                var priority = frame.Priority;
                
                count = frame.EntryCount;

                for (var j = 0; j < count; ++j)
                {
                    var entry = frame.GetEntry(j);
                    var property = entry.Property;
                    EffectiveValue? effectiveValue;

                    // Skip if we already have a value/base value for this property.
                    if (_effectiveValues.TryGetValue(property, out effectiveValue) == true &&
                        effectiveValue.BasePriority < BindingPriority.Unset)
                        continue;

                    if (!entry.HasValue)
                        continue;

                    if (effectiveValue is not null)
                    {
                        effectiveValue.SetAndRaise(this, entry, priority);
                    }
                    else
                    {
                        var v = property.CreateEffectiveValue(Owner);
                        AddEffectiveValue(property, v);
                        v.SetAndRaise(this, entry, priority);
                    }

                    if (generation != _frameGeneration)
                        goto restart;
                }
            }

            // Remove all effective values that are still unset.
            PooledList<AvaloniaProperty>? remove = null;

            count = _effectiveValues.Count;

            for (var i = 0; i < count; ++i)
            {
                _effectiveValues.GetKeyValue(i, out var key, out var e);

                if (e.Priority == BindingPriority.Unset)
                {
                    remove ??= new();
                    remove.Add(key);
                }
            }

            if (remove is not null)
            {
                foreach (var v in remove)
                {
                    if (RemoveEffectiveValue(v, out var e))
                        e.DisposeAndRaiseUnset(this, v);
                }
                remove.Dispose();
            }
        }

        private void UnsubscribeInactiveValues(int activeFrameIndex, AvaloniaProperty property)
        {
            var foundBaseValue = _frames[activeFrameIndex].Priority != BindingPriority.Animation;

            for (var i = activeFrameIndex - 1; i >= 0; --i)
            {
                var frame = _frames[i];

                if (!foundBaseValue && frame.Priority > BindingPriority.Animation)
                {
                    foundBaseValue = true;
                    continue;
                }

                if ((foundBaseValue || frame.Priority <= BindingPriority.Animation) &&
                    frame.TryGetEntry(property, out var entry) &&
                    frame.IsActive)
                {
                    entry.Unsubscribe();
                }
            }
        }

        private bool TryGetEffectiveValue(
            AvaloniaProperty property, 
            [NotNullWhen(true)] out EffectiveValue? value)
        {
            if (_effectiveValues.TryGetValue(property, out value))
                return true;
            value = null;
            return false;
        }

        private EffectiveValue? GetEffectiveValue(AvaloniaProperty property)
        {
            if (_effectiveValues.TryGetValue(property, out var value))
                return value;
            return null;
        }

        private object? GetDefaultValue(AvaloniaProperty property)
        {
            return ((IStyledPropertyAccessor)property).GetDefaultValue(Owner.GetType());
        }

        private void DisposeExistingLocalValueBinding(AvaloniaProperty property)
        {
            if (_localValueBindings is not null &&
                _localValueBindings.TryGetValue(property.Id, out var existing))
            {
                existing.Dispose();
            }
        }

        private int BinarySearchFrame(BindingPriority priority)
        {
            var lo = 0;
            var hi = _frames.Count - 1;

            // Binary search insertion point.
            while (lo <= hi)
            {
                var i = lo + ((hi - lo) >> 1);
                var order = priority - _frames[i].Priority;

                if (order <= 0)
                {
                    lo = i + 1;
                }
                else
                {
                    hi = i - 1;
                }
            }

            return lo;
        }

        private readonly struct OldNewValue
        {
            public OldNewValue(EffectiveValue? oldValue)
            {
                OldValue = oldValue;
                NewValue = null;
            }

            public OldNewValue(EffectiveValue? oldValue, EffectiveValue? newValue)
            {
                OldValue = oldValue;
                NewValue = newValue;
            }

            public readonly EffectiveValue? OldValue;
            public readonly EffectiveValue? NewValue;

            public OldNewValue WithNewValue(EffectiveValue newValue) => new(OldValue, newValue);
        }
    }
}
