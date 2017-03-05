// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Logging;
using Avalonia.Threading;
using Avalonia.Utilities;
using System.Reactive.Concurrency;

namespace Avalonia
{
    /// <summary>
    /// An object with <see cref="AvaloniaProperty"/> support.
    /// </summary>
    /// <remarks>
    /// This class is analogous to DependencyObject in WPF.
    /// </remarks>
    public class AvaloniaObject : IAvaloniaObject, IAvaloniaObjectDebug, INotifyPropertyChanged, IPriorityValueOwner
    {
        /// <summary>
        /// The parent object that inherited values are inherited from.
        /// </summary>
        private IAvaloniaObject _inheritanceParent;

        /// <summary>
        /// The set values/bindings on this object.
        /// </summary>
        private readonly Dictionary<AvaloniaProperty, PriorityValue> _values =
            new Dictionary<AvaloniaProperty, PriorityValue>();

        /// <summary>
        /// Maintains a list of direct property binding subscriptions so that the binding source
        /// doesn't get collected.
        /// </summary>
        private List<IDisposable> _directBindings;

        /// <summary>
        /// Event handler for <see cref="INotifyPropertyChanged"/> implementation.
        /// </summary>
        private PropertyChangedEventHandler _inpcChanged;

        /// <summary>
        /// Event handler for <see cref="PropertyChanged"/> implementation.
        /// </summary>
        private EventHandler<AvaloniaPropertyChangedEventArgs> _propertyChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaObject"/> class.
        /// </summary>
        public AvaloniaObject()
        {
            foreach (var property in AvaloniaPropertyRegistry.Instance.GetRegistered(this))
            {
                object value = property.IsDirect ?
                    ((IDirectPropertyAccessor)property).GetValue(this) :
                    ((IStyledPropertyAccessor)property).GetDefaultValue(GetType());

                var e = new AvaloniaPropertyChangedEventArgs(
                    this,
                    property,
                    AvaloniaProperty.UnsetValue,
                    value,
                    BindingPriority.Unset);

                property.NotifyInitialized(e);
            }
        }

        /// <summary>
        /// Raised when a <see cref="AvaloniaProperty"/> value changes on this object.
        /// </summary>
        public event EventHandler<AvaloniaPropertyChangedEventArgs> PropertyChanged
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
        /// Gets or sets the parent object that inherited <see cref="AvaloniaProperty"/> values
        /// are inherited from.
        /// </summary>
        /// <value>
        /// The inheritance parent.
        /// </value>
        protected IAvaloniaObject InheritanceParent
        {
            get
            {
                return _inheritanceParent;
            }

            set
            {
                if (_inheritanceParent != value)
                {
                    if (_inheritanceParent != null)
                    {
                        _inheritanceParent.PropertyChanged -= ParentPropertyChanged;
                    }

                    var inherited = (from property in AvaloniaPropertyRegistry.Instance.GetRegistered(this)
                                     where property.Inherits
                                     select new
                                     {
                                         Property = property,
                                         Value = GetValue(property),
                                     }).ToList();

                    _inheritanceParent = value;

                    foreach (var i in inherited)
                    {
                        object newValue = GetValue(i.Property);

                        if (!Equals(i.Value, newValue))
                        {
                            RaisePropertyChanged(i.Property, i.Value, newValue, BindingPriority.LocalValue);
                        }
                    }

                    if (_inheritanceParent != null)
                    {
                        _inheritanceParent.PropertyChanged += ParentPropertyChanged;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the value of a <see cref="AvaloniaProperty"/>.
        /// </summary>
        /// <param name="property">The property.</param>
        public object this[AvaloniaProperty property]
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
            get
            {
                return new IndexerBinding(this, binding.Property, binding.Mode);
            }

            set
            {
                var sourceBinding = value as IBinding;
                this.Bind(binding.Property, sourceBinding);
            }
        }

        public bool CheckAccess() => Dispatcher.UIThread.CheckAccess();

        public void VerifyAccess() => Dispatcher.UIThread.VerifyAccess();

        /// <summary>
        /// Clears a <see cref="AvaloniaProperty"/>'s local value.
        /// </summary>
        /// <param name="property">The property.</param>
        public void ClearValue(AvaloniaProperty property)
        {
            Contract.Requires<ArgumentNullException>(property != null);
            VerifyAccess();

            SetValue(property, AvaloniaProperty.UnsetValue);
        }

        /// <summary>
        /// Gets a <see cref="AvaloniaProperty"/> value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>The value.</returns>
        public object GetValue(AvaloniaProperty property)
        {
            Contract.Requires<ArgumentNullException>(property != null);
            VerifyAccess();

            if (property.IsDirect)
            {
                return ((IDirectPropertyAccessor)GetRegistered(property)).GetValue(this);
            }
            else
            {
                if (!AvaloniaPropertyRegistry.Instance.IsRegistered(this, property))
                {
                    ThrowNotRegistered(property);
                }

                return GetValueInternal(property);
            }
        }

        /// <summary>
        /// Gets a <see cref="AvaloniaProperty"/> value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <returns>The value.</returns>
        public T GetValue<T>(AvaloniaProperty<T> property)
        {
            Contract.Requires<ArgumentNullException>(property != null);

            return (T)GetValue((AvaloniaProperty)property);
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
            Contract.Requires<ArgumentNullException>(property != null);
            VerifyAccess();

            PriorityValue value;

            if (_values.TryGetValue(property, out value))
            {
                return value.Value != AvaloniaProperty.UnsetValue;
            }

            return false;
        }

        /// <summary>
        /// Sets a <see cref="AvaloniaProperty"/> value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <param name="priority">The priority of the value.</param>
        public void SetValue(
            AvaloniaProperty property,
            object value,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            Contract.Requires<ArgumentNullException>(property != null);
            VerifyAccess();

            if (property.IsDirect)
            {
                SetDirectValue(property, value);
            }
            else
            {
                SetStyledValue(property, value, priority);
            }
        }

        /// <summary>
        /// Sets a <see cref="AvaloniaProperty"/> value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <param name="priority">The priority of the value.</param>
        public void SetValue<T>(
            AvaloniaProperty<T> property,
            T value,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            Contract.Requires<ArgumentNullException>(property != null);

            SetValue((AvaloniaProperty)property, value, priority);
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
            IObservable<object> source,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            Contract.Requires<ArgumentNullException>(property != null);
            Contract.Requires<ArgumentNullException>(source != null);

            VerifyAccess();

            var description = GetDescription(source);

            var scheduler = AvaloniaLocator.Current.GetService<IScheduler>() ?? ImmediateScheduler.Instance;
            source = source.ObserveOn(scheduler); 

            if (property.IsDirect)
            {
                if (property.IsReadOnly)
                {
                    throw new ArgumentException($"The property {property.Name} is readonly.");
                }

                Logger.Verbose(
                    LogArea.Property, 
                    this,
                    "Bound {Property} to {Binding} with priority LocalValue", 
                    property, 
                    description);

                IDisposable subscription = null;

                if (_directBindings == null)
                {
                    _directBindings = new List<IDisposable>();
                }

                subscription = source
                    .Select(x => CastOrDefault(x, property.PropertyType))
                    .Do(_ => { }, () => _directBindings.Remove(subscription))
                    .Subscribe(x => SetDirectValue(property, x));

                _directBindings.Add(subscription);

                return Disposable.Create(() =>
                {
                    subscription.Dispose();
                    _directBindings.Remove(subscription);
                });
            }
            else
            {
                PriorityValue v;

                if (!AvaloniaPropertyRegistry.Instance.IsRegistered(this, property))
                {
                    ThrowNotRegistered(property);
                }

                if (!_values.TryGetValue(property, out v))
                {
                    v = CreatePriorityValue(property);
                    _values.Add(property, v);
                }

                Logger.Verbose(
                    LogArea.Property,
                    this,
                    "Bound {Property} to {Binding} with priority {Priority}",
                    property,
                    description,
                    priority);

                return v.Add(source, (int)priority);
            }
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
            AvaloniaProperty<T> property,
            IObservable<T> source,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            Contract.Requires<ArgumentNullException>(property != null);

            return Bind(property, source.Select(x => (object)x), priority);
        }

        /// <summary>
        /// Forces the specified property to be revalidated.
        /// </summary>
        /// <param name="property">The property.</param>
        public void Revalidate(AvaloniaProperty property)
        {
            VerifyAccess();
            PriorityValue value;

            if (_values.TryGetValue(property, out value))
            {
                value.Revalidate();
            }
        }

        /// <inheritdoc/>
        void IPriorityValueOwner.Changed(PriorityValue sender, object oldValue, object newValue)
        {
            var property = sender.Property;
            var priority = (BindingPriority)sender.ValuePriority;

            oldValue = (oldValue == AvaloniaProperty.UnsetValue) ?
                GetDefaultValue(property) :
                oldValue;
            newValue = (newValue == AvaloniaProperty.UnsetValue) ?
                GetDefaultValue(property) :
                newValue;

            if (!Equals(oldValue, newValue))
            {
                RaisePropertyChanged(property, oldValue, newValue, priority);

                Logger.Verbose(
                    LogArea.Property,
                    this,
                    "{Property} changed from {$Old} to {$Value} with priority {Priority}",
                    property,
                    oldValue,
                    newValue,
                    priority);
            }
        }

        /// <inheritdoc/>
        void IPriorityValueOwner.BindingNotificationReceived(PriorityValue sender, BindingNotification notification)
        {
            UpdateDataValidation(sender.Property, notification);
        }

        /// <inheritdoc/>
        Delegate[] IAvaloniaObjectDebug.GetPropertyChangedSubscribers()
        {
            return _propertyChanged?.GetInvocationList();
        }

        /// <summary>
        /// Gets all priority values set on the object.
        /// </summary>
        /// <returns>A collection of property/value tuples.</returns>
        internal IDictionary<AvaloniaProperty, PriorityValue> GetSetValues()
        {
            return _values;
        }

        /// <summary>
        /// Forces revalidation of properties when a property value changes.
        /// </summary>
        /// <param name="property">The property to that affects validation.</param>
        /// <param name="affected">The affected properties.</param>
        protected static void AffectsValidation(AvaloniaProperty property, params AvaloniaProperty[] affected)
        {
            property.Changed.Subscribe(e =>
            {
                foreach (var p in affected)
                {
                    e.Sender.Revalidate(p);
                }
            });
        }

        /// <summary>
        /// Called to update the validation state for properties for which data validation is
        /// enabled.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="status">The new validation status.</param>
        protected virtual void UpdateDataValidation(
            AvaloniaProperty property,
            BindingNotification status)
        {
        }

        /// <summary>
        /// Called when a avalonia property changes on the object.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="property">The property that has changed.</param>
        /// <param name="oldValue">The old property value.</param>
        /// <param name="newValue">The new property value.</param>
        /// <param name="priority">The priority of the binding that produced the value.</param>
        protected void RaisePropertyChanged(
            AvaloniaProperty property,
            object oldValue,
            object newValue,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            Contract.Requires<ArgumentNullException>(property != null);
            VerifyAccess();

            AvaloniaPropertyChangedEventArgs e = new AvaloniaPropertyChangedEventArgs(
                this,
                property,
                oldValue,
                newValue,
                priority);

            property.Notifying?.Invoke(this, true);

            try
            {
                OnPropertyChanged(e);
                property.NotifyChanged(e);

                _propertyChanged?.Invoke(this, e);

                if (_inpcChanged != null)
                {
                    PropertyChangedEventArgs e2 = new PropertyChangedEventArgs(property.Name);
                    _inpcChanged(this, e2);
                }
            }
            finally
            {
                property.Notifying?.Invoke(this, false);
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
            if (!object.Equals(field, value))
            {
                var old = field;
                field = value;
                RaisePropertyChanged(property, old, value, BindingPriority.LocalValue);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to cast a value to a type, taking into account that the value may be a
        /// <see cref="BindingNotification"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="type">The type.</param>
        /// <returns>The cast value, or a <see cref="BindingNotification"/>.</returns>
        private static object CastOrDefault(object value, Type type)
        {
            var notification = value as BindingNotification;

            if (notification == null)
            {
                return TypeUtilities.CastOrDefault(value, type);
            }
            else
            {
                if (notification.HasValue)
                {
                    notification.SetValue(TypeUtilities.CastOrDefault(notification.Value, type));
                }

                return notification;
            }
        }

        /// <summary>
        /// Creates a <see cref="PriorityValue"/> for a <see cref="AvaloniaProperty"/>.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>The <see cref="PriorityValue"/>.</returns>
        private PriorityValue CreatePriorityValue(AvaloniaProperty property)
        {
            var validate = ((IStyledPropertyAccessor)property).GetValidationFunc(GetType());
            Func<object, object> validate2 = null;

            if (validate != null)
            {
                validate2 = v => validate(this, v);
            }

            PriorityValue result = new PriorityValue(
                this,
                property,
                property.PropertyType, 
                validate2);

            return result;
        }

        /// <summary>
        /// Gets the default value for a property.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>The default value.</returns>
        private object GetDefaultValue(AvaloniaProperty property)
        {
            if (property.Inherits && _inheritanceParent != null)
            {
                return (_inheritanceParent as AvaloniaObject).GetValueInternal(property);
            }
            else
            {
                return ((IStyledPropertyAccessor)property).GetDefaultValue(GetType());
            }
        }

        /// <summary>
        /// Gets a <see cref="AvaloniaProperty"/> value
        /// without check for registered as this can slow getting the value
        /// this method is intended for internal usage in AvaloniaObject only
        /// it's called only after check the property is registered
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>The value.</returns>
        private object GetValueInternal(AvaloniaProperty property)
        {
            object result = AvaloniaProperty.UnsetValue;
            PriorityValue value;

            if (_values.TryGetValue(property, out value))
            {
                result = value.Value;
            }

            if (result == AvaloniaProperty.UnsetValue)
            {
                result = GetDefaultValue(property);
            }

            return result;
        }

        /// <summary>
        /// Sets the value of a direct property.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        private void SetDirectValue(AvaloniaProperty property, object value)
        {
            var notification = value as BindingNotification;

            if (notification != null)
            {
                if (notification.ErrorType == BindingErrorType.Error)
                {
                    Logger.Error(
                        LogArea.Binding,
                        this,
                        "Error in binding to {Target}.{Property}: {Message}",
                        this,
                        property,
                        ExceptionUtilities.GetMessage(notification.Error));
                }

                if (notification.HasValue)
                {
                    value = notification.Value;
                }
            }

            if (notification == null || notification.HasValue)
            {
                var metadata = (IDirectPropertyMetadata)property.GetMetadata(GetType());
                var accessor = (IDirectPropertyAccessor)GetRegistered(property);
                var finalValue = value == AvaloniaProperty.UnsetValue ? 
                    metadata.UnsetValue : value;

                LogPropertySet(property, value, BindingPriority.LocalValue);

                accessor.SetValue(this, finalValue);
            }

            if (notification != null)
            {
                UpdateDataValidation(property, notification);
            }
        }

        /// <summary>
        /// Sets the value of a styled property.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <param name="priority">The priority of the value.</param>
        private void SetStyledValue(AvaloniaProperty property, object value, BindingPriority priority)
        {
            var notification = value as BindingNotification;

            // We currently accept BindingNotifications for non-direct properties but we just
            // strip them to their underlying value.
            if (notification != null)
            {
                if (!notification.HasValue)
                {
                    return;
                }
                else
                {
                    value = notification.Value;
                }
            }

            var originalValue = value;

            if (!AvaloniaPropertyRegistry.Instance.IsRegistered(this, property))
            {
                ThrowNotRegistered(property);
            }

            if (!TypeUtilities.TryCast(property.PropertyType, value, out value))
            {
                throw new ArgumentException(string.Format(
                    "Invalid value for Property '{0}': '{1}' ({2})",
                    property.Name,
                    originalValue,
                    originalValue?.GetType().FullName ?? "(null)"));
            }

            PriorityValue v;

            if (!_values.TryGetValue(property, out v))
            {
                if (value == AvaloniaProperty.UnsetValue)
                {
                    return;
                }

                v = CreatePriorityValue(property);
                _values.Add(property, v);
            }

            LogPropertySet(property, value, priority);
            v.SetValue(value, (int)priority);
        }

        /// <summary>
        /// Given a <see cref="AvaloniaProperty"/> returns a registered avalonia property that is
        /// equal or throws if not found.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>The registered property.</returns>
        public AvaloniaProperty GetRegistered(AvaloniaProperty property)
        {
            var result = AvaloniaPropertyRegistry.Instance.FindRegistered(this, property);

            if (result == null)
            {
                ThrowNotRegistered(property);
            }

            return result;
        }

        /// <summary>
        /// Called when a property is changed on the current <see cref="InheritanceParent"/>.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        /// <remarks>
        /// Checks for changes in an inherited property value.
        /// </remarks>
        private void ParentPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            Contract.Requires<ArgumentNullException>(e != null);

            if (e.Property.Inherits && !IsSet(e.Property))
            {
                RaisePropertyChanged(e.Property, e.OldValue, e.NewValue, BindingPriority.LocalValue);
            }
        }

        /// <summary>
        /// Gets a description of an observable that van be used in logs.
        /// </summary>
        /// <param name="o">The observable.</param>
        /// <returns>The description.</returns>
        private string GetDescription(IObservable<object> o)
        {
            var description = o as IDescription;
            return description?.Description ?? o.ToString();
        }

        /// <summary>
        /// Logs a property set message.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The new value.</param>
        /// <param name="priority">The priority.</param>
        private void LogPropertySet(AvaloniaProperty property, object value, BindingPriority priority)
        {
            Logger.Verbose(
                LogArea.Property,
                this,
                "Set {Property} to {$Value} with priority {Priority}",
                property,
                value,
                priority);
        }

        /// <summary>
        /// Throws an exception indicating that the specified property is not registered on this
        /// object.
        /// </summary>
        /// <param name="p">The property</param>
        private void ThrowNotRegistered(AvaloniaProperty p)
        {
            throw new ArgumentException($"Property '{p.Name} not registered on '{this.GetType()}");
        }
    }
}