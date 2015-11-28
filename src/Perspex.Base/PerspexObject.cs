// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using Perspex.Reactive;
using Perspex.Threading;
using Perspex.Utilities;
using Serilog;
using Serilog.Core.Enrichers;

namespace Perspex
{
    /// <summary>
    /// An object with <see cref="PerspexProperty"/> support.
    /// </summary>
    /// <remarks>
    /// This class is analogous to DependencyObject in WPF.
    /// </remarks>
    public class PerspexObject : IObservablePropertyBag, INotifyPropertyChanged
    {
        /// <summary>
        /// The parent object that inherited values are inherited from.
        /// </summary>
        private PerspexObject _inheritanceParent;

        /// <summary>
        /// The set values/bindings on this object.
        /// </summary>
        private readonly Dictionary<PerspexProperty, PriorityValue> _values =
            new Dictionary<PerspexProperty, PriorityValue>();

        /// <summary>
        /// Event handler for <see cref="INotifyPropertyChanged"/> implementation.
        /// </summary>
        private PropertyChangedEventHandler _inpcChanged;

        /// <summary>
        /// A serilog logger for logging property events.
        /// </summary>
        private readonly ILogger _propertyLog;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerspexObject"/> class.
        /// </summary>
        public PerspexObject()
        {
            _propertyLog = Log.ForContext(new[]
            {
                new PropertyEnricher("Area", "Property"),
                new PropertyEnricher("SourceContext", GetType()),
                new PropertyEnricher("Id", GetHashCode()),
            });

            foreach (var property in PerspexPropertyRegistry.Instance.GetRegistered(this))
            {
                object value = property.IsDirect ? 
                    property.Getter(this) : 
                    property.GetDefaultValue(GetType());

                var e = new PerspexPropertyChangedEventArgs(
                    this,
                    property,
                    PerspexProperty.UnsetValue,
                    value,
                    BindingPriority.Unset);

                property.NotifyInitialized(e);
            }
        }

        /// <summary>
        /// Raised when a <see cref="PerspexProperty"/> value changes on this object.
        /// </summary>
        public event EventHandler<PerspexPropertyChangedEventArgs> PropertyChanged;

        /// <summary>
        /// Raised when a <see cref="PerspexProperty"/> value changes on this object.
        /// </summary>
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { _inpcChanged += value; }
            remove { _inpcChanged -= value; }
        }

        /// <summary>
        /// Gets the object that inherited <see cref="PerspexProperty"/> values are inherited from.
        /// </summary>
        IPropertyBag IPropertyBag.InheritanceParent => InheritanceParent;

        /// <summary>
        /// Gets or sets the parent object that inherited <see cref="PerspexProperty"/> values
        /// are inherited from.
        /// </summary>
        /// <value>
        /// The inheritance parent.
        /// </value>
        protected PerspexObject InheritanceParent
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

                    var inherited = (from property in PerspexPropertyRegistry.Instance.GetRegistered(this)
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
        /// Gets or sets the value of a <see cref="PerspexProperty"/>.
        /// </summary>
        /// <param name="property">The property.</param>
        public object this[PerspexProperty property]
        {
            get { return GetValue(property); }
            set { SetValue(property, value); }
        }

        /// <summary>
        /// Gets or sets a binding for a <see cref="PerspexProperty"/>.
        /// </summary>
        /// <param name="binding">The binding information.</param>
        public IObservable<object> this[BindingDescriptor binding]
        {
            get
            {
                return new BindingDescriptor
                {
                    Mode = binding.Mode,
                    Priority = binding.Priority,
                    Property = binding.Property,
                    Source = this,
                };
            }

            set
            {
                var mode = (binding.Mode == BindingMode.Default) ?
                    binding.Property.DefaultBindingMode :
                    binding.Mode;
                var sourceBinding = value as BindingDescriptor;

                if (sourceBinding == null && mode > BindingMode.OneWay)
                {
                    mode = BindingMode.OneWay;
                }

                switch (mode)
                {
                    case BindingMode.Default:
                    case BindingMode.OneWay:
                        Bind(binding.Property, value, binding.Priority);
                        break;
                    case BindingMode.OneTime:
                        SetValue(binding.Property, sourceBinding.Source.GetValue(sourceBinding.Property), binding.Priority);
                        break;
                    case BindingMode.OneWayToSource:
                        sourceBinding.Source.Bind(sourceBinding.Property, GetObservable(binding.Property), binding.Priority);
                        break;
                    case BindingMode.TwoWay:
                        BindTwoWay(binding.Property, sourceBinding.Source, sourceBinding.Property);
                        break;
                }
            }
        }

        public bool CheckAccess() => Dispatcher.UIThread.CheckAccess();

        public void VerifyAccess() => Dispatcher.UIThread.VerifyAccess();

        /// <summary>
        /// Clears a <see cref="PerspexProperty"/>'s local value.
        /// </summary>
        /// <param name="property">The property.</param>
        public void ClearValue(PerspexProperty property)
        {
            Contract.Requires<ArgumentNullException>(property != null);

            SetValue(property, PerspexProperty.UnsetValue);
        }

        /// <summary>
        /// Gets an observable for a <see cref="PerspexProperty"/>.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>An observable.</returns>
        public IObservable<object> GetObservable(PerspexProperty property)
        {
            Contract.Requires<ArgumentNullException>(property != null);

            return new PerspexObservable<object>(
                observer =>
                {
                    EventHandler<PerspexPropertyChangedEventArgs> handler = (s, e) =>
                    {
                        if (e.Property == property)
                        {
                            observer.OnNext(e.NewValue);
                        }
                    };

                    observer.OnNext(GetValue(property));

                    PropertyChanged += handler;

                    return Disposable.Create(() =>
                    {
                        PropertyChanged -= handler;
                    });
                },
                GetDescription(property));
        }

        /// <summary>
        /// Gets an observable for a <see cref="PerspexProperty"/>.
        /// </summary>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="property">The property.</param>
        /// <returns>An observable.</returns>
        public IObservable<T> GetObservable<T>(PerspexProperty<T> property)
        {
            Contract.Requires<ArgumentNullException>(property != null);

            return GetObservable((PerspexProperty)property).Cast<T>();
        }

        /// <summary>
        /// Gets an observable for a <see cref="PerspexProperty"/>.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <returns>An observable which when subscribed pushes the old and new values of the
        /// property each time it is changed.</returns>
        public IObservable<Tuple<T, T>> GetObservableWithHistory<T>(PerspexProperty<T> property)
        {
            return new PerspexObservable<Tuple<T, T>>(
                observer =>
                {
                    EventHandler<PerspexPropertyChangedEventArgs> handler = (s, e) =>
                    {
                        if (e.Property == property)
                        {
                            observer.OnNext(Tuple.Create((T)e.OldValue, (T)e.NewValue));
                        }
                    };

                    PropertyChanged += handler;

                    return Disposable.Create(() =>
                    {
                        PropertyChanged -= handler;
                    });
                },
                GetDescription(property));
        }

        /// <summary>
        /// Gets a <see cref="PerspexProperty"/> value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>The value.</returns>
        public object GetValue(PerspexProperty property)
        {
            Contract.Requires<ArgumentNullException>(property != null);

            if (property.IsDirect)
            {
                return GetRegistered(property).Getter(this);
            }
            else
            {
                object result = PerspexProperty.UnsetValue;
                PriorityValue value;

                if (!PerspexPropertyRegistry.Instance.IsRegistered(this, property))
                {
                    ThrowNotRegistered(property);
                }

                if (_values.TryGetValue(property, out value))
                {
                    result = value.Value;
                }

                if (result == PerspexProperty.UnsetValue)
                {
                    result = GetDefaultValue(property);
                }

                return result;
            }
        }

        /// <summary>
        /// Gets a <see cref="PerspexProperty"/> value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <returns>The value.</returns>
        public T GetValue<T>(PerspexProperty<T> property)
        {
            Contract.Requires<ArgumentNullException>(property != null);

            if (property.IsDirect)
            {
                return ((PerspexProperty<T>)GetRegistered(property)).Getter(this);
            }
            else
            {
                return (T)GetValue((PerspexProperty)property);
            }
        }

        /// <summary>
        /// Checks whether a <see cref="PerspexProperty"/> is set on this object.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>True if the property is set, otherwise false.</returns>
        public bool IsSet(PerspexProperty property)
        {
            Contract.Requires<ArgumentNullException>(property != null);
            
            PriorityValue value;

            if (_values.TryGetValue(property, out value))
            {
                return value.Value != PerspexProperty.UnsetValue;
            }

            return false;
        }

        /// <summary>
        /// Sets a <see cref="PerspexProperty"/> value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <param name="priority">The priority of the value.</param>
        public void SetValue(
            PerspexProperty property,
            object value,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            Contract.Requires<ArgumentNullException>(property != null);
            VerifyAccess();

            if (property.IsDirect)
            {
                property = GetRegistered(property);

                if (property.Setter == null)
                {
                    throw new ArgumentException($"The property {property.Name} is readonly.");
                }

                LogPropertySet(property, value, priority);
                property.Setter(this, UnsetToDefault(value, property));
            }
            else
            {
                PriorityValue v;
                var originalValue = value;

                if (!PerspexPropertyRegistry.Instance.IsRegistered(this, property))
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

                if (!_values.TryGetValue(property, out v))
                {
                    if (value == PerspexProperty.UnsetValue)
                    {
                        return;
                    }

                    v = CreatePriorityValue(property);
                    _values.Add(property, v);
                }

                LogPropertySet(property, value, priority);
                v.SetValue(value, (int)priority);
            }
        }

        /// <summary>
        /// Sets a <see cref="PerspexProperty"/> value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <param name="priority">The priority of the value.</param>
        public void SetValue<T>(
            PerspexProperty<T> property,
            T value,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            Contract.Requires<ArgumentNullException>(property != null);
            VerifyAccess();
            if (property.IsDirect)
            {
                property = (PerspexProperty<T>)GetRegistered(property);

                if (property.Setter == null)
                {
                    throw new ArgumentException($"The property {property.Name} is readonly.");
                }

                LogPropertySet(property, value, priority);
                property.Setter(this, value);
            }
            else
            {
                SetValue((PerspexProperty)property, value, priority);
            }
        }

        /// <summary>
        /// Binds a <see cref="PerspexProperty"/> to an observable.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="source">The observable.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        public IDisposable Bind(
            PerspexProperty property,
            IObservable<object> source,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            Contract.Requires<ArgumentNullException>(property != null);
            VerifyAccess();

            if (property.IsDirect)
            {
                property = GetRegistered(property);

                if (property.Setter == null)
                {
                    throw new ArgumentException($"The property {property.Name} is readonly.");
                }

                _propertyLog.Verbose(
                    "Bound {Property} to {Binding} with priority LocalValue",
                    property,
                    GetDescription(source));

                return source
                    .Select(x => TypeUtilities.CastOrDefault(x, property.PropertyType))
                    .Subscribe(x => SetValue(property, x));
            }
            else
            {
                PriorityValue v;

                if (!PerspexPropertyRegistry.Instance.IsRegistered(this, property))
                {
                    ThrowNotRegistered(property);
                }

                if (!_values.TryGetValue(property, out v))
                {
                    v = CreatePriorityValue(property);
                    _values.Add(property, v);
                }

                _propertyLog.Verbose(
                    "Bound {Property} to {Binding} with priority {Priority}",
                    property,
                    GetDescription(source),
                    priority);

                return v.Add(source, (int)priority);
            }
        }

        /// <summary>
        /// Binds a <see cref="PerspexProperty"/> to an observable.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="source">The observable.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        public IDisposable Bind<T>(
            PerspexProperty<T> property,
            IObservable<T> source,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            Contract.Requires<ArgumentNullException>(property != null);
            VerifyAccess();
            if (property.IsDirect)
            {
                property = (PerspexProperty<T>)GetRegistered(property);

                if (property.Setter == null)
                {
                    throw new ArgumentException($"The property {property.Name} is readonly.");
                }

                return source.Subscribe(x => SetValue(property, x));
            }
            else
            {
                return Bind((PerspexProperty)property, source.Select(x => (object)x), priority);
            }
        }

        /// <summary>
        /// Initiates a two-way binding between <see cref="PerspexProperty"/>s.
        /// </summary>
        /// <param name="property">The property on this object.</param>
        /// <param name="source">The source object.</param>
        /// <param name="sourceProperty">The property on the source object.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        /// <remarks>
        /// The binding is first carried out from <paramref name="source"/> to this.
        /// </remarks>
        public IDisposable BindTwoWay(
            PerspexProperty property,
            PerspexObject source,
            PerspexProperty sourceProperty,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            VerifyAccess();
            _propertyLog.Verbose(
                "Bound two way {Property} to {Binding} with priority {Priority}",
                property,
                source,
                priority);

            return new CompositeDisposable(
                Bind(property, source.GetObservable(sourceProperty)),
                source.Bind(sourceProperty, GetObservable(property)));
        }

        /// <summary>
        /// Initiates a two-way binding between a <see cref="PerspexProperty"/> and an 
        /// <see cref="ISubject{Object}"/>.
        /// </summary>
        /// <param name="property">The property on this object.</param>
        /// <param name="source">The subject to bind to.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        /// <remarks>
        /// The binding is first carried out from <paramref name="source"/> to this.
        /// </remarks>
        public IDisposable BindTwoWay(
            PerspexProperty property,
            ISubject<object> source,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            VerifyAccess();
            _propertyLog.Verbose(
                "Bound two way {Property} to {Binding} with priority {Priority}",
                property,
                GetDescription(source),
                priority);

            return new CompositeDisposable(
                Bind(property, source),
                GetObservable(property).Subscribe(source));
        }

        /// <summary>
        /// Forces the specified property to be revalidated.
        /// </summary>
        /// <param name="property">The property.</param>
        public void Revalidate(PerspexProperty property)
        {
            VerifyAccess();
            PriorityValue value;

            if (_values.TryGetValue(property, out value))
            {
                value.Revalidate();
            }
        }

        /// <inheritdoc/>
        bool IPropertyBag.IsRegistered(PerspexProperty property)
        {
            return PerspexPropertyRegistry.Instance.IsRegistered(this, property);
        }

        /// <summary>
        /// Gets all priority values set on the object.
        /// </summary>
        /// <returns>A collection of property/value tuples.</returns>
        internal IDictionary<PerspexProperty, PriorityValue> GetSetValues()
        {
            return _values;
        }

        /// <summary>
        /// Forces revalidation of properties when a property value changes.
        /// </summary>
        /// <param name="property">The property to that affects validation.</param>
        /// <param name="affected">The affected properties.</param>
        protected static void AffectsValidation(PerspexProperty property, params PerspexProperty[] affected)
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
        /// Called when a perspex property changes on the object.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected virtual void OnPropertyChanged(PerspexPropertyChangedEventArgs e)
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
            PerspexProperty property,
            object oldValue,
            object newValue,
            BindingPriority priority)
        {
            Contract.Requires<ArgumentNullException>(property != null);
            VerifyAccess();

            PerspexPropertyChangedEventArgs e = new PerspexPropertyChangedEventArgs(
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

                PropertyChanged?.Invoke(this, e);

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
        /// Sets the backing field for a direct perspex property, raising the 
        /// <see cref="PropertyChanged"/> event if the value has changed.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="field">The backing field.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// True if the value changed, otherwise false.
        /// </returns>
        protected bool SetAndRaise<T>(PerspexProperty<T> property, ref T field, T value)
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
        /// Converts an unset value to the default value for a property type.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="property">The property.</param>
        /// <returns>The value.</returns>
        private static object UnsetToDefault(object value, PerspexProperty property)
        {
            return value == PerspexProperty.UnsetValue ?
                TypeUtilities.Default(property.PropertyType) :
                value;
        }

        /// <summary>
        /// Creates a <see cref="PriorityValue"/> for a <see cref="PerspexProperty"/>.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>The <see cref="PriorityValue"/>.</returns>
        private PriorityValue CreatePriorityValue(PerspexProperty property)
        {
            Func<PerspexObject, object, object> validate = property.GetValidationFunc(GetType());
            Func<object, object> validate2 = null;

            if (validate != null)
            {
                validate2 = v => validate(this, v);
            }

            PriorityValue result = new PriorityValue(property.Name, property.PropertyType, validate2);

            result.Changed.Subscribe(x =>
            {
                object oldValue = (x.Item1 == PerspexProperty.UnsetValue) ?
                    GetDefaultValue(property) :
                    x.Item1;
                object newValue = (x.Item2 == PerspexProperty.UnsetValue) ?
                    GetDefaultValue(property) :
                    x.Item2;

                if (!Equals(oldValue, newValue))
                {
                    RaisePropertyChanged(property, oldValue, newValue, (BindingPriority)result.ValuePriority);

                    _propertyLog.Verbose(
                        "{Property} changed from {$Old} to {$Value} with priority {Priority}",
                        property,
                        oldValue,
                        newValue,
                        (BindingPriority)result.ValuePriority);
                }
            });

            return result;
        }

        /// <summary>
        /// Gets the default value for a property.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>The default value.</returns>
        private object GetDefaultValue(PerspexProperty property)
        {
            if (property.Inherits && _inheritanceParent != null)
            {
                return _inheritanceParent.GetValue(property);
            }
            else
            {
                return property.GetDefaultValue(GetType());
            }
        }

        /// <summary>
        /// Given a <see cref="PerspexProperty"/> returns a registered perspex property that is
        /// equal or throws if not found.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>The registered property.</returns>
        public PerspexProperty GetRegistered(PerspexProperty property)
        {
            var result = PerspexPropertyRegistry.Instance.FindRegistered(this, property);

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
        private void ParentPropertyChanged(object sender, PerspexPropertyChangedEventArgs e)
        {
            Contract.Requires<ArgumentNullException>(e != null);

            if (e.Property.Inherits && !IsSet(e.Property))
            {
                RaisePropertyChanged(e.Property, e.OldValue, e.NewValue, BindingPriority.LocalValue);
            }
        }

        /// <summary>
        /// Gets a description of a property that van be used in observables.
        /// </summary>
        /// <param name="property">The property</param>
        /// <returns>The description.</returns>
        private string GetDescription(PerspexProperty property)
        {
            return $"{GetType().Name}.{property.Name}";
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
        private void LogPropertySet(PerspexProperty property, object value, BindingPriority priority)
        {
            _propertyLog.Verbose(
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
        private void ThrowNotRegistered(PerspexProperty p)
        {
            throw new ArgumentException($"Property '{p.Name} not registered on '{this.GetType()}");
        }
    }
}
