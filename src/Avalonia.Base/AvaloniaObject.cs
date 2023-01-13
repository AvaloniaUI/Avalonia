using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Logging;
using Avalonia.PropertyStore;
using Avalonia.Threading;

namespace Avalonia
{
    /// <summary>
    /// An object with <see cref="AvaloniaProperty"/> support.
    /// </summary>
    /// <remarks>
    /// This class is analogous to DependencyObject in WPF.
    /// </remarks>
    public class AvaloniaObject : IAvaloniaObjectDebug, INotifyPropertyChanged
    {
        private readonly ValueStore _values;
        private AvaloniaObject? _inheritanceParent;
        private PropertyChangedEventHandler? _inpcChanged;
        private EventHandler<AvaloniaPropertyChangedEventArgs>? _propertyChanged;
        private List<AvaloniaObject>? _inheritanceChildren;

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
        event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
        {
            add { _inpcChanged += value; }
            remove { _inpcChanged -= value; }
        }

        /// <summary>
        /// Gets or sets the parent object that inherited <see cref="AvaloniaProperty"/> values
        /// are inherited from.
        /// </summary>
        /// <value>
        /// The inheritance parent.
        /// </value>
        protected internal AvaloniaObject? InheritanceParent
        {
            get
            {
                return _inheritanceParent;
            }

            set
            {
                VerifyAccess();

                if (_inheritanceParent != value)
                {
                    _inheritanceParent?.RemoveInheritanceChild(this);
                    _inheritanceParent = value;
                    _inheritanceParent?.AddInheritanceChild(this);
                    _values.SetInheritanceParent(value);
                }
            }
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
            get { return new IndexerBinding(this, binding.Property!, binding.Mode); }
            set { this.Bind(binding.Property!, value); }
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
                    ClearValue(styled);
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
        public sealed override bool Equals(object? obj) => base.Equals(obj);

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
        /// Gets a <see cref="AvaloniaProperty"/> value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>The value.</returns>
        public object? GetValue(AvaloniaProperty property)
        {
            _ = property ?? throw new ArgumentNullException(nameof(property));

            if (property.IsDirect)
                return property.RouteGetValue(this);
            else
                return _values.GetValue(property);
        }

        /// <summary>
        /// Gets a <see cref="AvaloniaProperty"/> value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <returns>The value.</returns>
        public T GetValue<T>(StyledPropertyBase<T> property)
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
        public T GetValue<T>(DirectPropertyBase<T> property)
        {
            property = property ?? throw new ArgumentNullException(nameof(property));
            VerifyAccess();

            var registered = AvaloniaPropertyRegistry.Instance.GetRegisteredDirect(this, property);
            return registered.InvokeGetter(this);
        }

        /// <inheritdoc/>
        public Optional<T> GetBaseValue<T>(StyledPropertyBase<T> property)
        {
            _ = property ?? throw new ArgumentNullException(nameof(property));
            VerifyAccess();
            return _values.GetBaseValue(property);
        }

        /// <summary>
        /// Checks whether a <see cref="AvaloniaProperty"/> is animating.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>True if the property is animating, otherwise false.</returns>
        public bool IsAnimating(AvaloniaProperty property)
        {
            property = property ?? throw new ArgumentNullException(nameof(property));

            VerifyAccess();

            return _values?.IsAnimating(property) ?? false;
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
            property = property ?? throw new ArgumentNullException(nameof(property));

            VerifyAccess();

            return _values?.IsSet(property) ?? false;
        }

        /// <summary>
        /// Sets a <see cref="AvaloniaProperty"/> value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <param name="priority">The priority of the value.</param>
        public IDisposable? SetValue(
            AvaloniaProperty property,
            object? value,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            property = property ?? throw new ArgumentNullException(nameof(property));

            return property.RouteSetValue(this, value, priority);
        }

        /// <summary>
        /// Sets a <see cref="AvaloniaProperty"/> value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <param name="priority">The priority of the value.</param>
        /// <returns>
        /// An <see cref="IDisposable"/> if setting the property can be undone, otherwise null.
        /// </returns>
        public IDisposable? SetValue<T>(
            StyledPropertyBase<T> property,
            T value,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            _ = property ?? throw new ArgumentNullException(nameof(property));
            VerifyAccess();
            ValidatePriority(priority);

            LogPropertySet(property, value, BindingPriority.LocalValue);

            if (value is UnsetValueType)
            {
                if (priority == BindingPriority.LocalValue)
                    _values.ClearLocalValue(property);
            }
            else if (value is not DoNothingType)
            {
                return _values.SetValue(property, value, priority);
            }

            return null;
        }

        /// <summary>
        /// Sets a <see cref="AvaloniaProperty"/> value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        public void SetValue<T>(DirectPropertyBase<T> property, T value)
        {
            property = property ?? throw new ArgumentNullException(nameof(property));
            VerifyAccess();

            property = AvaloniaPropertyRegistry.Instance.GetRegisteredDirect(this, property);
            LogPropertySet(property, value, BindingPriority.LocalValue);
            SetDirectValueUnchecked(property, value);
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
            BindingPriority priority = BindingPriority.LocalValue) => property.RouteBind(this, source, priority);

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
            ValidatePriority(priority);

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
            IObservable<T> source,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            property = property ?? throw new ArgumentNullException(nameof(property));
            source = source ?? throw new ArgumentNullException(nameof(source));
            VerifyAccess();
            ValidatePriority(priority);

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
            ValidatePriority(priority);

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
            VerifyAccess();

            property = AvaloniaPropertyRegistry.Instance.GetRegisteredDirect(this, property);

            if (property.IsReadOnly)
            {
                throw new ArgumentException($"The property {property.Name} is readonly.");
            }

            return _values.AddBinding(property, source);
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
            VerifyAccess();

            property = AvaloniaPropertyRegistry.Instance.GetRegisteredDirect(this, property);

            if (property.IsReadOnly)
            {
                throw new ArgumentException($"The property {property.Name} is readonly.");
            }

            return _values.AddBinding(property, source);
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
            VerifyAccess();

            property = AvaloniaPropertyRegistry.Instance.GetRegisteredDirect(this, property);

            if (property.IsReadOnly)
            {
                throw new ArgumentException($"The property {property.Name} is readonly.");
            }

            return _values.AddBinding(property, source);
        }

        /// <summary>
        /// Coerces the specified <see cref="AvaloniaProperty"/>.
        /// </summary>
        /// <param name="property">The property.</param>
        public void CoerceValue(AvaloniaProperty property) => _values.CoerceValue(property);

        /// <inheritdoc/>
        internal void AddInheritanceChild(AvaloniaObject child)
        {
            _inheritanceChildren ??= new List<AvaloniaObject>();
            _inheritanceChildren.Add(child);
        }
        
        /// <inheritdoc/>
        internal void RemoveInheritanceChild(AvaloniaObject child)
        {
            _inheritanceChildren?.Remove(child);
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
                    BindingPriority.Unset,
                    "Local Value");
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
                BindingPriority.Unset,
                "Unset");
        }

        internal ValueStore GetValueStore() => _values;
        internal IReadOnlyList<AvaloniaObject>? GetInheritanceChildren() => _inheritanceChildren;

        /// <summary>
        /// Gets a logger to which a binding warning may be written.
        /// </summary>
        /// <param name="property">The property that the error occurred on.</param>
        /// <param name="e">The binding exception, if any.</param>
        /// <remarks>
        /// This is overridden in <see cref="Visual"/> to prevent logging binding errors when a
        /// control is not attached to the visual tree.
        /// </remarks>
        internal virtual ParametrizedLogger? GetBindingWarningLogger(
            AvaloniaProperty property,
            Exception? e)
        {
            return Logger.TryGet(LogEventLevel.Warning, LogArea.Binding);
        }

        /// <summary>
        /// Called to update the validation state for properties for which data validation is
        /// enabled.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="state">The current data binding state.</param>
        /// <param name="error">The current data binding error, if any.</param>
        protected virtual void UpdateDataValidation(
            AvaloniaProperty property,
            BindingValueType state,
            Exception? error)
        {
        }

        /// <summary>
        /// Called when a avalonia property changes on the object.
        /// </summary>
        /// <param name="change">The property change details.</param>
        protected virtual void OnPropertyChangedCore(AvaloniaPropertyChangedEventArgs change)
        {
            if (change.IsEffectiveValueChange)
            {
                OnPropertyChanged(change);
            }
        }

        /// <summary>
        /// Called when a avalonia property changes on the object.
        /// </summary>
        /// <param name="change">The property change details.</param>
        protected virtual void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
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
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="property">The property that has changed.</param>
        /// <param name="oldValue">The old property value.</param>
        /// <param name="newValue">The new property value.</param>
        /// <param name="priority">The priority of the binding that produced the value.</param>
        /// <param name="isEffectiveValue">
        /// Whether the notification represents a change to the effective value of the property.
        /// </param>
        internal void RaisePropertyChanged<T>(
            AvaloniaProperty<T> property,
            Optional<T> oldValue,
            BindingValue<T> newValue,
            BindingPriority priority,
            bool isEffectiveValue)
        {
            var e = new AvaloniaPropertyChangedEventArgs<T>(
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
            }
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
        protected bool SetAndRaise<T>(AvaloniaProperty<T> property, ref T field, T value)
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
        /// Sets the value of a direct property.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        internal void SetDirectValueUnchecked<T>(DirectPropertyBase<T> property, T value)
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

        /// <summary>
        /// Sets the value of a direct property.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        internal void SetDirectValueUnchecked<T>(DirectPropertyBase<T> property, BindingValue<T> value)
        {
            LoggingUtils.LogIfNecessary(this, property, value);

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
                UpdateDataValidation(property, value.Type, value.Error);
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
            return description?.Description ?? o.ToString() ?? o.GetType().Name;
        }

        /// <summary>
        /// Logs a property set message.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The new value.</param>
        /// <param name="priority">The priority.</param>
        private void LogPropertySet<T>(AvaloniaProperty<T> property, T value, BindingPriority priority)
        {
            Logger.TryGet(LogEventLevel.Verbose, LogArea.Property)?.Log(
                this,
                "Set {Property} to {$Value} with priority {Priority}",
                property,
                value,
                priority);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ValidatePriority(BindingPriority priority)
        {
            if (priority < BindingPriority.Animation || priority >= BindingPriority.Inherited)
                ThrowInvalidPriority(priority);
        }

        private static void ThrowInvalidPriority(BindingPriority priority)
        {
            throw new ArgumentException($"Invalid priority ${priority}", nameof(priority));
        }
    }
}
