using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Logging;
using Avalonia.PropertyStore;
using Avalonia.Reactive;
using Avalonia.Threading;

#nullable enable

namespace Avalonia
{
    /// <summary>
    /// An object with <see cref="AvaloniaProperty"/> support.
    /// </summary>
    /// <remarks>
    /// This class is analogous to DependencyObject in WPF.
    /// </remarks>
    public class AvaloniaObject : IAvaloniaObject, IAvaloniaObjectDebug, INotifyPropertyChanged
    {
        private List<IDisposable>? _directBindings;
        private PropertyChangedEventHandler? _inpcChanged;
        private EventHandler<AvaloniaPropertyChangedEventArgs>? _propertyChanged;
        private Dictionary<AvaloniaProperty, AvaloniaPropertyObservable>? _observables;
        private ValueStore _values;

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaObject"/> class.
        /// </summary>
        public AvaloniaObject()
        {
            VerifyAccess();
            _values = new ValueStore(this);
        }

        /// <summary>
        /// Raised when a <see cref="AvaloniaProperty"/> value changes on this object.
        /// </summary>
        public event EventHandler<AvaloniaPropertyChangedEventArgs>? PropertyChanged
        {
            add { _propertyChanged += value; }
            remove { _propertyChanged -= value; }
        }

        /// <summary>
        /// Raised when a <see cref="AvaloniaProperty"/> value changes on this object.
        /// </summary>
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { _inpcChanged += value; }
            remove { _inpcChanged -= value; }
        }

        /// <summary>
        /// Gets or sets the value of a <see cref="AvaloniaProperty"/>.
        /// </summary>
        /// <param name="property">The property.</param>
        public object? this[AvaloniaProperty property]
        {
            get { return GetValue(property); }
            set { SetValue(property, value); }
        }

        /// <summary>
        /// Gets or sets a binding for a <see cref="AvaloniaProperty"/>.
        /// </summary>
        /// <param name="binding">The binding information.</param>
        public IBinding this[IndexerDescriptor binding]
        {
            get { return new IndexerBinding(this, binding.Property, binding.Mode); }
            set { this.Bind(binding.Property, value); }
        }

        /// <summary>
        /// Returns a value indicating whether the current thread is the UI thread.
        /// </summary>
        /// <returns>true if the current thread is the UI thread; otherwise false.</returns>
        public bool CheckAccess() => Dispatcher.UIThread.CheckAccess();

        /// <summary>
        /// Checks that the current thread is the UI thread and throws if not.
        /// </summary>
        public void VerifyAccess() => Dispatcher.UIThread.VerifyAccess();

        /// <summary>
        /// Clears a <see cref="AvaloniaProperty"/>'s local value.
        /// </summary>
        /// <param name="property">The property.</param>
        public void ClearValue(AvaloniaProperty property)
        {
            _ = property ?? throw new ArgumentNullException(nameof(property));
            VerifyAccess();
            _values.ClearLocalValue(property);
        }

        /// <summary>
        /// Clears a <see cref="AvaloniaProperty"/>'s local value.
        /// </summary>
        /// <param name="property">The property.</param>
        public void ClearValue<T>(AvaloniaProperty<T> property)
        {
            property = property ?? throw new ArgumentNullException(nameof(property));
            VerifyAccess();

            switch (property)
            {
                case StyledPropertyBase<T> styled:
                    _values.ClearLocalValue(styled);
                    break;
                case DirectPropertyBase<T> direct:
                    ClearValue(direct);
                    break;
                default:
                    throw new NotSupportedException("Unsupported AvaloniaProperty type.");
            }
        }

        /// <summary>
        /// Clears a <see cref="AvaloniaProperty"/>'s local value.
        /// </summary>
        /// <param name="property">The property.</param>
        public void ClearValue<T>(StyledPropertyBase<T> property)
        {
            property = property ?? throw new ArgumentNullException(nameof(property));
            VerifyAccess();

            _values?.ClearLocalValue(property);
        }

        /// <summary>
        /// Clears a <see cref="AvaloniaProperty"/>'s local value.
        /// </summary>
        /// <param name="property">The property.</param>
        public void ClearValue<T>(DirectPropertyBase<T> property)
        {
            property = property ?? throw new ArgumentNullException(nameof(property));
            VerifyAccess();

            var p = AvaloniaPropertyRegistry.Instance.GetRegisteredDirect(this, property);
            p.InvokeSetter(this, p.GetUnsetValue(GetType()));
        }

        /// <summary>
        /// Compares two objects using reference equality.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <remarks>
        /// Overriding Equals and GetHashCode on an AvaloniaObject is disallowed for two reasons:
        /// 
        /// - AvaloniaObjects are by their nature mutable
        /// - The presence of attached properties means that the semantics of equality are
        ///   difficult to define
        /// 
        /// See https://github.com/AvaloniaUI/Avalonia/pull/2747 for the discussion that prompted
        /// this.
        /// </remarks>
        public sealed override bool Equals(object obj) => base.Equals(obj);

        /// <summary>
        /// Gets the hash code for the object.
        /// </summary>
        /// <remarks>
        /// Overriding Equals and GetHashCode on an AvaloniaObject is disallowed for two reasons:
        /// 
        /// - AvaloniaObjects are by their nature mutable
        /// - The presence of attached properties means that the semantics of equality are
        ///   difficult to define
        /// 
        /// See https://github.com/AvaloniaUI/Avalonia/pull/2747 for the discussion that prompted
        /// this.
        /// </remarks>
        public sealed override int GetHashCode() => base.GetHashCode();

        /// <summary>
        /// Gets an observable for an <see cref="AvaloniaProperty"/>.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// An observable which fires immediately with the current value of the property on the
        /// object and subsequently each time the property value changes.
        /// </returns>
        public IObservable<object?> GetObservable(AvaloniaProperty property)
        {
            _ = property ?? throw new ArgumentNullException(nameof(property));
            VerifyAccess();

            if (_observables is object && _observables.TryGetValue(property, out var o))
                return o;
            return property.GetObservable(this);
        }

        /// <summary>
        /// Gets an observable for an <see cref="AvaloniaProperty"/>.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>
        /// An observable which fires immediately with the current value of the property on the
        /// object and subsequently each time the property value changes.
        /// </returns>
        public IObservable<T?> GetObservable<T>(AvaloniaProperty<T> property)
        {
            _ = property ?? throw new ArgumentNullException(nameof(property));
            VerifyAccess();

            _observables ??= new();

            if (_observables.TryGetValue(property, out var o))
                return (AvaloniaPropertyObservable<T>)o;
            else
            {
                AvaloniaPropertyObservable<T> result = property switch
                {
                    StyledPropertyBase<T> styled => new StyledPropertyObservable<T>(this, styled),
                    DirectPropertyBase<T> direct => new DirectPropertyObservable<T>(this, direct),
                    _ => throw new NotSupportedException("Unsupported property type."),
                };

                _observables.Add(property, result);
                return result;
            }
        }

        /// <summary>
        /// Gets a <see cref="AvaloniaProperty"/> value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>The value.</returns>
        public object? GetValue(AvaloniaProperty property) => property.GetValue(this);

        /// <summary>
        /// Gets a <see cref="AvaloniaProperty"/> value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <returns>The value.</returns>
        public T? GetValue<T>(StyledPropertyBase<T> property)
        {
            _ = property ?? throw new ArgumentNullException(nameof(property));
            VerifyAccess();
            return _values.GetValue(property);
        }

        /// <summary>
        /// Gets a <see cref="AvaloniaProperty"/> value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <returns>The value.</returns>
        public T? GetValue<T>(DirectPropertyBase<T> property)
        {
            _ = property ?? throw new ArgumentNullException(nameof(property));
            VerifyAccess();

            var registered = AvaloniaPropertyRegistry.Instance.FindRegisteredDirect(this, property);

            if (registered is object)
            {
                return registered.InvokeGetter(this);
            }
            else
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.Property)?.Log(
                    "The direct property {Property} is not registered on {Type} and will always return the default value.",
                    property.Name,
                    GetType());
                return property.GetUnsetValue(GetType());
            }
        }

        /// <summary>
        /// Gets an <see cref="AvaloniaProperty"/> value with the specified binding priority.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="minPriority">The minimum priority for the value.</param>
        /// <param name="maxPriority">The maximum priority for the value.</param>
        /// <remarks>
        /// Gets the value of the property, if set on this object with a priority between
        /// <paramref name="minPriority"/> and <paramref name="maxPriority"/> (inclusive),
        /// otherwise <see cref="AvaloniaProperty.UnsetValue"/>. Note that this method does not
        /// return property values that come from inherited or default values.
        /// </remarks>
        public object? GetValueByPriority(
            AvaloniaProperty property,
            BindingPriority minPriority,
            BindingPriority maxPriority)
        {
            _ = property ?? throw new ArgumentNullException(nameof(property));
            return property.GetValueByPriority(this, minPriority, maxPriority);
        }

        /// <summary>
        /// Gets an <see cref="AvaloniaProperty"/> value with the specified binding priority.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="minPriority">The minimum priority for the value.</param>
        /// <param name="maxPriority">The maximum priority for the value.</param>
        /// <remarks>
        /// Gets the value of the property, if set on this object with a priority between
        /// <paramref name="minPriority"/> and <paramref name="maxPriority"/> (inclusive),
        /// otherwise <see cref="Optional{T}.Empty"/>. Note that this method does not return
        /// property values that come from inherited or default values.
        /// </remarks>
        public Optional<T> GetValueByPriority<T>(
            StyledPropertyBase<T> property,
            BindingPriority minPriority,
            BindingPriority maxPriority)
        {
            _ = property ?? throw new ArgumentNullException(nameof(property));
            VerifyAccess();
            return _values.GetBaseValue(property, minPriority, maxPriority);
        }

        /// <summary>
        /// Sets an <see cref="AvaloniaProperty"/> local value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <param name="priority">The priority of the value.</param>
        public void SetValue(AvaloniaProperty property, object? value) => property.SetValue(this, value);

        /// <summary>
        /// Sets a <see cref="AvaloniaProperty"/> value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// An <see cref="IDisposable"/> if setting the property can be undone, otherwise null.
        /// </returns>
        public void SetValue<T>(StyledPropertyBase<T> property, T? value)
        {
            _ = property ?? throw new ArgumentNullException(nameof(property));
            VerifyAccess();

            LogPropertySet(property, value, BindingPriority.LocalValue);

            if (value is UnsetValueType)
                _values.ClearLocalValue(property);
            else if (!(value is DoNothingType))
                _values.SetLocalValue(property, value);
        }

        /// <summary>
        /// Sets a <see cref="AvaloniaProperty"/> value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        public void SetValue<T>(DirectPropertyBase<T> property, T? value)
        {
            property = property ?? throw new ArgumentNullException(nameof(property));
            VerifyAccess();

            LogPropertySet(property, value, BindingPriority.LocalValue);
            var p = AvaloniaPropertyRegistry.Instance.GetRegisteredDirect(this, property);
            SetDirectValueUnchecked(p, value);
        }

        /// <summary>
        /// Binds a <see cref="AvaloniaProperty"/> to an observable.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="source">The observable.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        public IDisposable Bind(
            AvaloniaProperty property,
            IObservable<object?> source,
            BindingPriority priority = BindingPriority.LocalValue) => property.Bind(this, source, priority);

        /// <summary>
        /// Binds a <see cref="AvaloniaProperty"/> to an observable.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="source">The observable.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        public IDisposable Bind<T>(
            StyledPropertyBase<T> property,
            IObservable<object?> source,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            property = property ?? throw new ArgumentNullException(nameof(property));
            source = source ?? throw new ArgumentNullException(nameof(source));
            VerifyAccess();

            return _values.AddBinding(property, source, priority);
        }

        /// <summary>
        /// Binds a <see cref="AvaloniaProperty"/> to an observable.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="source">The observable.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        public IDisposable Bind<T>(
            StyledPropertyBase<T> property,
            IObservable<BindingValue<T>> source,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            property = property ?? throw new ArgumentNullException(nameof(property));
            source = source ?? throw new ArgumentNullException(nameof(source));
            VerifyAccess();

            return _values.AddBinding(property, source, priority);
        }

        /// <summary>
        /// Binds a <see cref="AvaloniaProperty"/> to an observable.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="source">The observable.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        public IDisposable Bind<T>(
            StyledPropertyBase<T> property,
            IObservable<T?> source,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            property = property ?? throw new ArgumentNullException(nameof(property));
            source = source ?? throw new ArgumentNullException(nameof(source));
            VerifyAccess();

            return _values.AddBinding(property, source, priority);
        }

        /// <summary>
        /// Binds a <see cref="AvaloniaProperty"/> to an observable.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="source">The observable.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        public IDisposable Bind<T>(
            DirectPropertyBase<T> property,
            IObservable<object?> source)
        {
            property = property ?? throw new ArgumentNullException(nameof(property));
            source = source ?? throw new ArgumentNullException(nameof(source));
            VerifyAccess();

            property = AvaloniaPropertyRegistry.Instance.GetRegisteredDirect(this, property);

            if (property.IsReadOnly)
            {
                throw new ArgumentException($"The property {property.Name} is readonly.");
            }

            Logger.TryGet(LogEventLevel.Verbose, LogArea.Property)?.Log(
                this,
                "Bound {Property} to {Binding} with priority LocalValue",
                property,
                GetDescription(source));

            _directBindings ??= new List<IDisposable>();

            return new DirectBindingUntyped<T>(this, property, source);
        }

        /// <summary>
        /// Binds a <see cref="AvaloniaProperty"/> to an observable.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="source">The observable.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        public IDisposable Bind<T>(
            DirectPropertyBase<T> property,
            IObservable<BindingValue<T>> source)
        {
            property = property ?? throw new ArgumentNullException(nameof(property));
            source = source ?? throw new ArgumentNullException(nameof(source));
            VerifyAccess();

            property = AvaloniaPropertyRegistry.Instance.GetRegisteredDirect(this, property);

            if (property.IsReadOnly)
            {
                throw new ArgumentException($"The property {property.Name} is readonly.");
            }

            Logger.TryGet(LogEventLevel.Verbose, LogArea.Property)?.Log(
                this,
                "Bound {Property} to {Binding} with priority LocalValue",
                property,
                GetDescription(source));

            return new DirectBinding<T>(this, property, source);
        }

        /// <summary>
        /// Binds a <see cref="AvaloniaProperty"/> to an observable.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="source">The observable.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        public IDisposable Bind<T>(
            DirectPropertyBase<T> property,
            IObservable<T> source)
        {
            property = property ?? throw new ArgumentNullException(nameof(property));
            source = source ?? throw new ArgumentNullException(nameof(source));
            VerifyAccess();

            property = AvaloniaPropertyRegistry.Instance.GetRegisteredDirect(this, property);

            if (property.IsReadOnly)
            {
                throw new ArgumentException($"The property {property.Name} is readonly.");
            }

            Logger.TryGet(LogEventLevel.Verbose, LogArea.Property)?.Log(
                this,
                "Bound {Property} to {Binding} with priority LocalValue",
                property,
                GetDescription(source));

            return new DirectBinding<T>(this, property, source);
        }

        /// <summary>
        /// Checks whether a <see cref="AvaloniaProperty"/> is animating.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>True if the property is animating, otherwise false.</returns>
        public bool IsAnimating(AvaloniaProperty property)
        {
            _ = property ?? throw new ArgumentNullException(nameof(property));
            VerifyAccess();
            return _values.IsAnimating(property);
        }

        /// <summary>
        /// Checks whether a <see cref="AvaloniaProperty"/> is set on this object.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>True if the property is set, otherwise false.</returns>
        /// <remarks>
        /// Checks whether a value is assigned to the property, or that there is a binding to the
        /// property that is producing a value other than <see cref="AvaloniaProperty.UnsetValue"/>.
        /// </remarks>
        public bool IsSet(AvaloniaProperty property)
        {
            _ = property ?? throw new ArgumentNullException(nameof(property));
            VerifyAccess();
            return _values.IsSet(property);
        }

        /// <summary>
        /// Coerces the specified <see cref="AvaloniaProperty"/>.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        public void CoerceValue<T>(StyledPropertyBase<T> property)
        {
            throw new NotImplementedException();
            ////_values?.CoerceValue(property);
        }

        /// <inheritdoc/>
        Delegate[]? IAvaloniaObjectDebug.GetPropertyChangedSubscribers()
        {
            return _propertyChanged?.GetInvocationList();
        }

        internal AvaloniaPropertyValue GetDiagnosticInternal(AvaloniaProperty property)
        {
            if (property.IsDirect)
            {
                return new AvaloniaPropertyValue(
                    property,
                    GetValue(property),
                    BindingPriority.Unset);
            }
            else if (_values != null)
            {
                var result = _values.GetDiagnostic(property);

                if (result != null)
                {
                    return result;
                }
            }

            return new AvaloniaPropertyValue(
                property,
                GetValue(property),
                BindingPriority.Unset);
        }

        /// <summary>
        /// Logs a binding error for a property.
        /// </summary>
        /// <param name="property">The property that the error occurred on.</param>
        /// <param name="e">The binding error.</param>
        protected internal virtual void LogBindingError(AvaloniaProperty property, Exception e)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Binding)?.Log(
                this,
                "Error in binding to {Target}.{Property}: {Message}",
                this,
                property,
                e.Message);
        }

        /// <summary>
        /// Called to update the validation state for properties for which data validation is
        /// enabled.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The new binding value for the property.</param>
        protected virtual void UpdateDataValidation<T>(
            AvaloniaProperty<T> property,
            BindingValue<T> value)
        {
        }

        /// <summary>
        /// Called when a avalonia property changes on the object.
        /// </summary>
        /// <param name="change">The property change details.</param>
        protected virtual void OnPropertyChangedCore<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            if (change.IsEffectiveValueChange)
                OnPropertyChanged(change);
        }

        /// <summary>
        /// Called when a avalonia property changes on the object.
        /// </summary>
        /// <param name="change">The property change details.</param>
        protected virtual void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for a direct property.
        /// </summary>
        /// <param name="property">The property that has changed.</param>
        /// <param name="oldValue">The old property value.</param>
        /// <param name="newValue">The new property value.</param>
        /// <param name="priority">The priority of the binding that produced the value.</param>
        protected void RaisePropertyChanged<T>(
            DirectPropertyBase<T> property,
            Optional<T> oldValue,
            BindingValue<T> newValue,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            RaisePropertyChanged(property, oldValue, newValue, priority, true);
        }

        /// <summary>
        /// Sets the backing field for a direct avalonia property, raising the 
        /// <see cref="PropertyChanged"/> event if the value has changed.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="field">The backing field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// True if the value changed, otherwise false.
        /// </returns>
        protected bool SetAndRaise<T>(DirectPropertyBase<T> property, ref T field, T value)
        {
            VerifyAccess();

            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            var old = field;
            field = value;
            RaisePropertyChanged(property, old, value, BindingPriority.LocalValue, true);
            return true;
        }

        /// <summary>
        /// Notifies the object of a change to the value of <see cref="GetInheritanceParent"/>.
        /// </summary>
        protected void InheritanceParentChanged()
        {
            _values.InheritanceParentChanged(GetInheritanceParent()?._values);
        }

        protected internal virtual int GetInheritanceChildCount() => 0;
        protected internal virtual AvaloniaObject GetInheritanceChild(int index) => throw new IndexOutOfRangeException();
        protected internal virtual AvaloniaObject? GetInheritanceParent() => null;

        internal void AddDirectBinding(IDisposable binding) => (_directBindings ??= new()).Add(binding);
        internal void RemoveDirectBinding(IDisposable binding) => _directBindings?.Remove(binding);
        internal ValueStore GetValueStore() => _values;

        internal void RaisePropertyChanged<T>(
            AvaloniaProperty<T> property,
            Optional<T> oldValue,
            BindingValue<T> newValue,
            BindingPriority priority,
            bool isEffectiveValue)
        {
            if (isEffectiveValue)
                property.Notifying?.Invoke(this, true);

            try
            {
                var e = AvaloniaPropertyChangedEventArgsPool<T>.Get(
                    this,
                    property,
                    oldValue,
                    newValue,
                    priority,
                    isEffectiveValue);

                OnPropertyChangedCore(e);

                if (isEffectiveValue)
                {
                    property.NotifyChanged(e);
                    _propertyChanged?.Invoke(this, e);
                    _inpcChanged?.Invoke(this, new PropertyChangedEventArgs(property.Name));

                    // Release the event args here so they can be recycled if raising the change on the
                    // observable causes a cascading change.
                    AvaloniaPropertyChangedEventArgsPool<T>.Release(e);
                    e = null;

                    if (_observables is object && _observables.TryGetValue(property, out var o))
                        ((AvaloniaPropertyObservable<T?>)o).PublishNext();
                }
                
                if (e is object)
                {
                    AvaloniaPropertyChangedEventArgsPool<T>.Release(e);
                }

                if (property.Inherits)
                {
                    var childCount = GetInheritanceChildCount();

                    for (var i = 0; i < childCount; ++i)
                    {
                        var child = GetInheritanceChild(i);
                        child.GetValueStore().InheritedValueChanged((StyledPropertyBase<T>)property, oldValue);
                    }
                }
            }
            finally
            {
                if (isEffectiveValue)
                    property.Notifying?.Invoke(this, false);
            }
        }

        internal void SetDirectValueUnchecked<T>(DirectPropertyBase<T> property, T? value)
        {
            if (value is UnsetValueType)
            {
                property.InvokeSetter(this, property.GetUnsetValue(GetType()));
            }
            else if (!(value is DoNothingType))
            {
                property.InvokeSetter(this, value);
            }
        }

        internal void SetDirectValueUnchecked<T>(DirectPropertyBase<T> property, BindingValue<T> value)
        {
            LogIfError(property, value);

            switch (value.Type)
            {
                case BindingValueType.UnsetValue:
                case BindingValueType.BindingError:
                    var fallback = value.HasValue ? value : value.WithValue(property.GetUnsetValue(GetType()));
                    property.InvokeSetter(this, fallback);
                    break;
                case BindingValueType.DataValidationError:
                    property.InvokeSetter(this, value);
                    break;
                case BindingValueType.Value:
                case BindingValueType.BindingErrorWithFallback:
                case BindingValueType.DataValidationErrorWithFallback:
                    property.InvokeSetter(this, value);
                    break;
            }

            var metadata = property.GetMetadata(GetType());

            if (metadata.EnableDataValidation == true)
            {
                UpdateDataValidation(property, value);
            }
        }

        /// <summary>
        /// Gets a description of an observable that van be used in logs.
        /// </summary>
        /// <param name="o">The observable.</param>
        /// <returns>The description.</returns>
        private string GetDescription(object o)
        {
            var description = o as IDescription;
            return description?.Description ?? o.ToString();
        }

        /// <summary>
        /// Logs a message if the notification represents a binding error.
        /// </summary>
        /// <param name="property">The property being bound.</param>
        /// <param name="value">The binding notification.</param>
        private void LogIfError<T>(AvaloniaProperty property, BindingValue<T> value)
        {
            if (value.HasError)
            {
                if (value.Error is AggregateException aggregate)
                {
                    foreach (var inner in aggregate.InnerExceptions)
                    {
                        LogBindingError(property, inner);
                    }
                }
                else
                {
                    LogBindingError(property, value.Error!);
                }
            }
        }

        /// <summary>
        /// Logs a property set message.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The new value.</param>
        /// <param name="priority">The priority.</param>
        private void LogPropertySet<T>(AvaloniaProperty property, T value, BindingPriority priority)
        {
            Logger.TryGet(LogEventLevel.Verbose, LogArea.Property)?.Log(
                this,
                "Set {Property} to {$Value} with priority {Priority}",
                property,
                value,
                priority);
        }
    }
}
