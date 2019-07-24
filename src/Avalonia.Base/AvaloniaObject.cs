// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Logging;
using Avalonia.Threading;
using Avalonia.Utilities;

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
        private IAvaloniaObject _inheritanceParent;
        private List<DirectBindingSubscription> _directBindings;
        private PropertyChangedEventHandler _inpcChanged;
        private EventHandler<AvaloniaPropertyChangedEventArgs> _propertyChanged;
        private EventHandler<AvaloniaPropertyChangedEventArgs> _inheritablePropertyChanged;
        private ValueStore _values;
        private ValueStore Values => _values ?? (_values = new ValueStore(this));

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaObject"/> class.
        /// </summary>
        public AvaloniaObject()
        {
            VerifyAccess();
            AvaloniaPropertyRegistry.Instance.NotifyInitialized(this);
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
        /// Raised when an inheritable <see cref="AvaloniaProperty"/> value changes on this object.
        /// </summary>
        event EventHandler<AvaloniaPropertyChangedEventArgs> IAvaloniaObject.InheritablePropertyChanged
        {
            add { _inheritablePropertyChanged += value; }
            remove { _inheritablePropertyChanged -= value; }
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
                        _inheritanceParent.InheritablePropertyChanged -= ParentPropertyChanged;
                    }

                    var properties = AvaloniaPropertyRegistry.Instance.GetRegistered(this)
                        .Concat(AvaloniaPropertyRegistry.Instance.GetRegisteredAttached(this.GetType()));
                    var inherited = (from property in properties
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
                        _inheritanceParent.InheritablePropertyChanged += ParentPropertyChanged;
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
            else if (_values != null)
            {
                var result = Values.GetValue(property);

                if (result == AvaloniaProperty.UnsetValue)
                {
                    result = GetDefaultValue(property);
                }

                return result;
            }
            else
            {
                return GetDefaultValue(property);
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
        /// Checks whether a <see cref="AvaloniaProperty"/> is animating.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>True if the property is animating, otherwise false.</returns>
        public bool IsAnimating(AvaloniaProperty property)
        {
            Contract.Requires<ArgumentNullException>(property != null);
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
            Contract.Requires<ArgumentNullException>(property != null);
            VerifyAccess();

            return _values?.IsSet(property) ?? false;
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

                if (_directBindings == null)
                {
                    _directBindings = new List<DirectBindingSubscription>();
                }

                return new DirectBindingSubscription(this, property, source);
            }
            else
            {
                Logger.Verbose(
                    LogArea.Property,
                    this,
                    "Bound {Property} to {Binding} with priority {Priority}",
                    property,
                    description,
                    priority);

                return Values.AddBinding(property, source, priority);
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
            _values?.Revalidate(property);
        }
        
        internal void PriorityValueChanged(AvaloniaProperty property, int priority, object oldValue, object newValue)
        {
            oldValue = (oldValue == AvaloniaProperty.UnsetValue) ?
                GetDefaultValue(property) :
                oldValue;
            newValue = (newValue == AvaloniaProperty.UnsetValue) ?
                GetDefaultValue(property) :
                newValue;

            if (!Equals(oldValue, newValue))
            {
                RaisePropertyChanged(property, oldValue, newValue, (BindingPriority)priority);

                Logger.Verbose(
                    LogArea.Property,
                    this,
                    "{Property} changed from {$Old} to {$Value} with priority {Priority}",
                    property,
                    oldValue,
                    newValue,
                    (BindingPriority)priority);
            }
        }
        
        internal void BindingNotificationReceived(AvaloniaProperty property, BindingNotification notification)
        {
            LogIfError(property, notification);
            UpdateDataValidation(property, notification);
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
        internal IDictionary<AvaloniaProperty, object> GetSetValues() => Values?.GetSetValues();

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
        /// Logs a binding error for a property.
        /// </summary>
        /// <param name="property">The property that the error occurred on.</param>
        /// <param name="e">The binding error.</param>
        protected internal virtual void LogBindingError(AvaloniaProperty property, Exception e)
        {
            Logger.Log(
                LogEventLevel.Warning,
                LogArea.Binding,
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
        protected internal void RaisePropertyChanged(
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

                if (property.Inherits)
                {
                    _inheritablePropertyChanged?.Invoke(this, e);
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

            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            DeferredSetter<T> setter = Values.GetDirectDeferredSetter(property);

            return setter.SetAndNotify(this, property, ref field, value);
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
                return TypeUtilities.ConvertImplicitOrDefault(value, type);
            }
            else
            {
                if (notification.HasValue)
                {
                    notification.SetValue(TypeUtilities.ConvertImplicitOrDefault(notification.Value, type));
                }

                return notification;
            }
        }

        /// <summary>
        /// Gets the default value for a property.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>The default value.</returns>
        private object GetDefaultValue(AvaloniaProperty property)
        {
            if (property.Inherits && InheritanceParent is AvaloniaObject aobj)
                return aobj.GetValue(property);
            return ((IStyledPropertyAccessor) property).GetDefaultValue(GetType());
        }

        /// <summary>
        /// Sets the value of a direct property.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        private void SetDirectValue(AvaloniaProperty property, object value)
        {
            void Set()
            {
                var notification = value as BindingNotification;

                if (notification != null)
                {
                    LogIfError(property, notification);
                    value = notification.Value;
                }

                if (notification == null || notification.ErrorType == BindingErrorType.Error || notification.HasValue)
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

            if (Dispatcher.UIThread.CheckAccess())
            {
                Set();
            }
            else
            {
                Dispatcher.UIThread.Post(Set);
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

            if (!TypeUtilities.TryConvertImplicit(property.PropertyType, value, out value))
            {
                throw new ArgumentException(string.Format(
                    "Invalid value for Property '{0}': '{1}' ({2})",
                    property.Name,
                    originalValue,
                    originalValue?.GetType().FullName ?? "(null)"));
            }

            LogPropertySet(property, value, priority);
            Values.AddValue(property, value, (int)priority);
        }

        /// <summary>
        /// Given a direct property, returns a registered avalonia property that is equivalent or
        /// throws if not found.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>The registered property.</returns>
        private AvaloniaProperty GetRegistered(AvaloniaProperty property)
        {
            var direct = property as IDirectPropertyAccessor;

            if (direct == null)
            {
                throw new AvaloniaInternalException(
                    "AvaloniaObject.GetRegistered should only be called for direct properties");
            }

            if (property.OwnerType.IsAssignableFrom(GetType()))
            {
                return property;
            }

            var result =  AvaloniaPropertyRegistry.Instance.GetRegistered(this)
                .FirstOrDefault(x => x == property);

            if (result == null)
            {
                throw new ArgumentException($"Property '{property.Name} not registered on '{this.GetType()}");
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
        /// Logs a mesage if the notification represents a binding error.
        /// </summary>
        /// <param name="property">The property being bound.</param>
        /// <param name="notification">The binding notification.</param>
        private void LogIfError(AvaloniaProperty property, BindingNotification notification)
        {
            if (notification.ErrorType == BindingErrorType.Error)
            {
                if (notification.Error is AggregateException aggregate)
                {
                    foreach (var inner in aggregate.InnerExceptions)
                    {
                        LogBindingError(property, inner);
                    }
                }
                else
                {
                    LogBindingError(property, notification.Error);
                }
            }
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

        private class DirectBindingSubscription : IObserver<object>, IDisposable
        {
            readonly AvaloniaObject _owner;
            readonly AvaloniaProperty _property;
            IDisposable _subscription;

            public DirectBindingSubscription(
                AvaloniaObject owner,
                AvaloniaProperty property,
                IObservable<object> source)
            {
                _owner = owner;
                _property = property;
                _owner._directBindings.Add(this);
                _subscription = source.Subscribe(this);
            }

            public void Dispose()
            {
                _subscription.Dispose();
                _owner._directBindings.Remove(this);
            }

            public void OnCompleted() => Dispose();
            public void OnError(Exception error) => Dispose();

            public void OnNext(object value)
            {
                var castValue = CastOrDefault(value, _property.PropertyType);
                _owner.SetDirectValue(_property, castValue);
            }
        }
    }
}
