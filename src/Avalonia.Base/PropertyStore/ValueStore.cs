using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Collections.Pooled;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Logging;

namespace Avalonia.PropertyStore
{
    internal class ValueStore
    {
        private readonly List<IValueFrame> _frames = new();
        private Dictionary<int, IDisposable>? _localValueBindings;
        private Dictionary<AvaloniaProperty, EffectiveValue>? _effectiveValues;
        private int _inheritedValueCount;
        private int _frameGeneration;
        private int _styling;

        public ValueStore(AvaloniaObject owner) => Owner = owner;

        public AvaloniaObject Owner { get; }
        public ValueStore? InheritanceAncestor { get; private set; }
        public IReadOnlyList<IValueFrame> Frames => _frames;

        public void BeginStyling() => ++_styling;

        public void EndStyling()
        {
            if (--_styling == 0)
                ReevaluateEffectiveValues();
        }

        public void AddFrame(IValueFrame style)
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
                var frame = GetOrCreateImmediateValueFrame(property, priority);
                var result = frame.AddBinding(property, source);

                if (effective is null || priority <= effective.Priority)
                    result.Start();

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
                var frame = GetOrCreateImmediateValueFrame(property, priority);
                var result = frame.AddBinding(property, source);

                if (effective is null || priority <= effective.Priority)
                    result.Start();

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
                var frame = GetOrCreateImmediateValueFrame(property, priority);
                var result = frame.AddBinding(property, source);

                if (effective is null || priority <= effective.Priority)
                    result.Start();

                return result;
            }
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
                var frame = GetOrCreateImmediateValueFrame(property, priority);
                result = frame.AddValue(property, value);
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
            if (_effectiveValues is not null && _effectiveValues.TryGetValue(property, out var v))
                return v.Value;
            if (property.Inherits && TryGetInheritedValue(property, out v))
                return v.Value;

            return GetDefaultValue(property);
        }

        public T GetValue<T>(StyledPropertyBase<T> property)
        {
            if (_effectiveValues is not null && _effectiveValues.TryGetValue(property, out var v))
                return ((EffectiveValue<T>)v).Value;
            if (property.Inherits && TryGetInheritedValue(property, out v))
                return ((EffectiveValue<T>)v).Value;
            return property.GetDefaultValue(Owner.GetType());
        }

        public bool IsAnimating(AvaloniaProperty property)
        {
            if (_effectiveValues is not null && _effectiveValues.TryGetValue(property, out var v))
                return v.Priority <= BindingPriority.Animation;
            return false;
        }

        public bool IsSet(AvaloniaProperty property)
        {
            if (_effectiveValues is not null && _effectiveValues.TryGetValue(property, out var v))
                return v.Priority < BindingPriority.Inherited;
            return false;
        }

        public void CoerceValue(AvaloniaProperty property)
        {
            if (_effectiveValues is not null && _effectiveValues.TryGetValue(property, out var v))
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

        public void SetInheritanceParent(AvaloniaObject? oldParent, AvaloniaObject? newParent)
        {
            var values = DictionaryPool<AvaloniaProperty, OldNewValue>.Get();
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
                Debug.Assert(f._effectiveValues is not null);

                if (f._effectiveValues is not null)
                {
                    foreach (var i in f._effectiveValues)
                    {
                        if (i.Key.Inherits)
                            values.TryAdd(i.Key, new(i.Value));
                    }
                }

                f = f.InheritanceAncestor;
            }

            f = newAncestor;

            // Get the new values from the new inheritance ancestor.
            while (f is not null)
            {
                Debug.Assert(f._effectiveValues is not null);

                foreach (var i in f._effectiveValues)
                {
                    if (i.Key.Inherits)
                    {
                        if (values.TryGetValue(i.Key, out var existing))
                            values[i.Key] = existing.WithNewValue(i.Value);
                        else
                            values.Add(i.Key, new(null, i.Value));
                    }
                }

                f = f.InheritanceAncestor;
            }

            OnInheritanceAncestorChanged(newAncestor);

            // Raise PropertyChanged events where necessary on this object and inheritance children.
            foreach (var i in values)
            {
                var oldValue = i.Value.OldValue;
                var newValue = i.Value.NewValue;

                if (oldValue != newValue)
                    InheritedValueChanged(i.Key, oldValue, newValue);
            }

            DictionaryPool<AvaloniaProperty, OldNewValue>.Release(values);
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
        public void OnBindingCompleted(AvaloniaProperty property, IValueFrame frame)
        {
            var priority = frame.Priority;

            if (TryGetEffectiveValue(property, out var existing))
            {
                if (priority <= existing.Priority)
                    ReevaluateEffectiveValue(property, existing);
            }
        }

        /// <summary>
        /// Called by an <see cref="IValueFrame"/> when its <see cref="IValueFrame.IsActive"/>
        /// state changes.
        /// </summary>
        /// <param name="frame">The frame which produced the change.</param>
        public void OnFrameActivationChanged(IValueFrame frame)
        {
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
        /// Called when a <see cref="LocalValueBindingObserver{T}"/> completes.
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
            if (_effectiveValues is not null && _effectiveValues.ContainsKey(property))
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
        /// Called by an <see cref="IValueFrame"/> to re-evaluate the effective value when a value
        /// is removed.
        /// </summary>
        /// <param name="frame">The frame on which the change occurred.</param>
        /// <param name="property">The property whose value was removed.</param>
        public void OnValueEntryRemoved(IValueFrame frame, AvaloniaProperty property)
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

        public bool RemoveFrame(IValueFrame frame)
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

        private void InsertFrame(IValueFrame frame)
        {
            Debug.Assert(!_frames.Contains(frame));

            var index = BinarySearchFrame(frame.Priority);
            _frames.Insert(index, frame);
            ++_frameGeneration;
            frame.SetOwner(this);
        }

        private ImmediateValueFrame GetOrCreateImmediateValueFrame(
            AvaloniaProperty property, 
            BindingPriority priority)
        {
            Debug.Assert(priority != BindingPriority.LocalValue);

            var index = BinarySearchFrame(priority);

            if (index > 0 && _frames[index - 1] is ImmediateValueFrame f &&
                f.Priority == priority &&
                !f.Contains(property))
                return f;

            var result = new ImmediateValueFrame(priority);
            InsertFrame(result);
            return result;
        }

        private void ReevaluateEffectiveValue(
            AvaloniaProperty property,
            EffectiveValue current,
            bool ignoreLocalValue = false)
        {
            if (EvaluateEffectiveValue(
                property, 
                !ignoreLocalValue ? current : null,
                out var value, 
                out var priority,
                out var baseValue,
                out var basePriority))
            {
                if (basePriority != BindingPriority.Unset)
                    current.SetAndRaise(this, property, value, priority, baseValue, basePriority);
                else
                    current.SetAndRaise(this, property, value, priority);
            }
            else
            {
                RemoveEffectiveValue(property);
                current.DisposeAndRaiseUnset(this, property);
            }
        }

        private void AddEffectiveValue(AvaloniaProperty property, EffectiveValue effectiveValue)
        {
            _effectiveValues ??= new();
            _effectiveValues.Add(property, effectiveValue);

            if (property.Inherits && _inheritedValueCount++ == 0)
                OnInheritanceAncestorChanged(this);
        }

        /// <summary>
        /// Adds a new effective value, raises the initial <see cref="AvaloniaObject.PropertyChanged"/>
        /// event and notifies inheritance children if necessary .
        /// </summary>
        /// <typeparam name="T">The property type.</typeparam>
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
            if (_effectiveValues is not null && _effectiveValues.Remove(property))
            {
                if (property.Inherits && --_inheritedValueCount == 0)
                    OnInheritanceAncestorChanged(InheritanceAncestor?.InheritanceAncestor);
                return true;
            }

            return false;
        }

        private bool RemoveEffectiveValue(AvaloniaProperty property, [NotNullWhen(true)] out EffectiveValue? result)
        {
            if (_effectiveValues is not null && _effectiveValues.Remove(property, out result))
            {
                if (property.Inherits && --_inheritedValueCount == 0)
                    OnInheritanceAncestorChanged(InheritanceAncestor?.InheritanceAncestor);
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Evaluates the current value and base value for a property based on the current frames and optionally
        /// local values. Does not evaluate inherited values.
        /// </summary>
        /// <param name="property">The property to evaluation</param>
        /// <param name="current">The current effective value if the local value is to be considered.</param>
        /// <param name="value">When the method exits will contain the current value if it exists.</param>
        /// <param name="priority">When the method exits will contain the current value priority.</param>
        /// <param name="baseValue">>When the method exits will contain the current base value if it exists.</param>
        /// <param name="basePriority">When the method exits will contain the current base value priority.</param>
        /// <returns>
        /// True if a value was found, otherwise false.
        /// </returns>
        private bool EvaluateEffectiveValue(
            AvaloniaProperty property,
            EffectiveValue? current,
            out object? value,
            out BindingPriority priority,
            out object? baseValue,
            out BindingPriority basePriority)
        {
            var i = _frames.Count - 1;

            value = baseValue = AvaloniaProperty.UnsetValue;
            priority = basePriority = BindingPriority.Unset;

            // First try to find an animation value.
            for (; i >= 0; --i)
            {
                var frame = _frames[i];

                if (frame.Priority > BindingPriority.Animation)
                    break;

                if (frame.IsActive && 
                    frame.TryGetEntry(property, out var entry) && 
                    entry.TryGetValue(out value))
                {
                    priority = frame.Priority;
                    --i;
                    break;
                }
            }

            // Local values come from the current EffectiveValue.
            if (current?.Priority == BindingPriority.LocalValue)
            {
                // If there's a current effective local value and no animated value then we use the
                // effective local value.
                if (priority == BindingPriority.Unset)
                {
                    value = current.Value;
                    priority = BindingPriority.LocalValue;
                }

                // The local value is always the base value.
                baseValue = current.Value;
                basePriority = BindingPriority.LocalValue;
                return true;
            }

            // Or the current effective base value if there's no longer an animated value.
            if (priority == BindingPriority.Unset && current?.BasePriority == BindingPriority.LocalValue)
            {
                value = baseValue = current.BaseValue;
                priority = basePriority = BindingPriority.LocalValue;
                return true;
            }

            // Now try the rest of the frames.
            for (; i >= 0; --i)
            {
                var frame = _frames[i];

                if (frame.IsActive &&
                    frame.TryGetEntry(property, out var entry) && 
                    entry.TryGetValue(out var v))
                {
                    if (priority == BindingPriority.Unset)
                    {
                        value = v;
                        priority = frame.Priority;
                    }

                    baseValue = v;
                    basePriority = frame.Priority;
                    return true;
                }
            }

            return priority != BindingPriority.Unset;
        }

        private void InheritedValueChanged(
            AvaloniaProperty property,
            EffectiveValue? oldValue,
            EffectiveValue? newValue)
        {
            Debug.Assert(oldValue != newValue);
            Debug.Assert(oldValue is not null || newValue is not null);

            // If the value is set locally, propagaton ends here.
            if (_effectiveValues?.ContainsKey(property) == true)
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

        private void ReevaluateEffectiveValues()
        {
        restart:
            // Don't reevaluate if a styling pass is in effect, reevaluation will be done when
            // it has finished.
            if (_styling > 0)
                return;

            var generation = _frameGeneration;
            
            // Reset all non-LocalValue effective values to Unset priority.
            if (_effectiveValues is not null)
            {
                foreach (var v in _effectiveValues)
                {
                    var e = v.Value;

                    if (e.Priority != BindingPriority.LocalValue)
                        e.SetPriority(BindingPriority.Unset);
                    if (e.BasePriority != BindingPriority.LocalValue)
                        e.SetBasePriority(BindingPriority.Unset);
                }
            }

            // Iterate the frames, setting and creating effective values.
            for (var i = _frames.Count - 1; i >= 0; --i)
            {
                var frame = _frames[i];

                if (!frame.IsActive)
                    continue;

                var priority = frame.Priority;
                var count = frame.EntryCount;

                for (var j = 0; j < count; ++j)
                {
                    var entry = frame.GetEntry(j);
                    var property = entry.Property;
                    EffectiveValue? effectiveValue = null;

                    // Skip if we already have a value/base value for this property.
                    if (_effectiveValues?.TryGetValue(property, out effectiveValue) == true &&
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
            if (_effectiveValues is not null)
            {
                PooledList<AvaloniaProperty>? remove = null;

                foreach (var v in _effectiveValues)
                {
                    var e = v.Value;

                    if (e.Priority == BindingPriority.Unset)
                    {
                        remove ??= new();
                        remove.Add(v.Key);
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
        }

        [MemberNotNullWhen(true, nameof(_effectiveValues))]
        private bool TryGetEffectiveValue(
            AvaloniaProperty property, 
            [NotNullWhen(true)] out EffectiveValue? value)
        {
            if (_effectiveValues is not null && _effectiveValues.TryGetValue(property, out value))
                return true;
            value = null;
            return false;
        }

        private bool TryGetInheritedValue(
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

        private EffectiveValue? GetEffectiveValue(AvaloniaProperty property)
        {
            if (_effectiveValues is not null && _effectiveValues.TryGetValue(property, out var value))
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
