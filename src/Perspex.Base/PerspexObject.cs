// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using Perspex.Reactive;
using Perspex.Utilities;
using Serilog;
using Serilog.Core.Enrichers;

namespace Perspex
{
    /// <summary>
    /// The priority of a binding.
    /// </summary>
    public enum BindingPriority
    {
        /// <summary>
        /// A value that comes from an animation.
        /// </summary>
        Animation = -1,

        /// <summary>
        /// A local value.
        /// </summary>
        LocalValue = 0,

        /// <summary>
        /// A triggered style binding.
        /// </summary>
        /// <remarks>
        /// A style trigger is a selector such as .class which overrides a
        /// <see cref="TemplatedParent"/> binding. In this way, a basic control can have
        /// for example a Background from the templated parent which changes when the
        /// control has the :pointerover class.
        /// </remarks>
        StyleTrigger,

        /// <summary>
        /// A binding to a property on the templated parent.
        /// </summary>
        TemplatedParent,

        /// <summary>
        /// A style binding.
        /// </summary>
        Style,

        /// <summary>
        /// The binding is uninitialized.
        /// </summary>
        Unset = int.MaxValue,
    }

    /// <summary>
    /// An object with <see cref="PerspexProperty"/> support.
    /// </summary>
    /// <remarks>
    /// This class is analogous to DependencyObject in WPF.
    /// </remarks>
    public class PerspexObject : IObservablePropertyBag, INotifyPropertyChanged
    {
        /// <summary>
        /// The registered properties by type.
        /// </summary>
        private static readonly Dictionary<Type, List<PerspexProperty>> s_registered =
            new Dictionary<Type, List<PerspexProperty>>();

        /// <summary>
        /// The registered attached properties by owner type.
        /// </summary>
        private static readonly Dictionary<Type, List<PerspexProperty>> s_attached =
            new Dictionary<Type, List<PerspexProperty>>();

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

            foreach (var property in GetRegisteredProperties())
            {
                var e = new PerspexPropertyChangedEventArgs(
                    this,
                    property,
                    PerspexProperty.UnsetValue,
                    property.GetDefaultValue(GetType()),
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

                    var inherited = (from property in GetRegisteredProperties(GetType())
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

                if (sourceBinding == null && mode != BindingMode.OneWay)
                {
                    throw new InvalidOperationException("Can only bind OneWay to plain IObservable.");
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

        /// <summary>
        /// Gets all <see cref="PerspexProperty"/>s registered on a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A collection of <see cref="PerspexProperty"/> definitions.</returns>
        public static IEnumerable<PerspexProperty> GetRegisteredProperties(Type type)
        {
            Contract.Requires<NullReferenceException>(type != null);

            TypeInfo i = type.GetTypeInfo();

            while (type != null)
            {
                List<PerspexProperty> list;

                if (s_registered.TryGetValue(type, out list))
                {
                    foreach (PerspexProperty p in list)
                    {
                        yield return p;
                    }
                }

                type = type.GetTypeInfo().BaseType;
            }
        }

        /// <summary>
        /// Gets all attached <see cref="PerspexProperty"/>s registered by an owner.
        /// </summary>
        /// <param name="ownerType">The owner type.</param>
        /// <returns>A collection of <see cref="PerspexProperty"/> definitions.</returns>
        public static IEnumerable<PerspexProperty> GetAttachedProperties(Type ownerType)
        {
            List<PerspexProperty> list;

            if (s_attached.TryGetValue(ownerType, out list))
            {
                return list;
            }

            return Enumerable.Empty<PerspexProperty>();
        }

        /// <summary>
        /// Registers a <see cref="PerspexProperty"/> on a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="property">The property.</param>
        /// <remarks>
        /// You won't usually want to call this method directly, instead use the
        /// <see cref="PerspexProperty.Register"/> method.
        /// </remarks>
        public static void Register(Type type, PerspexProperty property)
        {
            Contract.Requires<NullReferenceException>(type != null);
            Contract.Requires<NullReferenceException>(property != null);

            List<PerspexProperty> list;

            if (!s_registered.TryGetValue(type, out list))
            {
                list = new List<PerspexProperty>();
                s_registered.Add(type, list);
            }

            if (!list.Contains(property))
            {
                list.Add(property);
            }

            if (property.IsAttached)
            {
                if (!s_attached.TryGetValue(property.OwnerType, out list))
                {
                    list = new List<PerspexProperty>();
                    s_attached.Add(property.OwnerType, list);
                }

                if (!list.Contains(property))
                {
                    list.Add(property);
                }
            }
        }

        /// <summary>
        /// Clears a <see cref="PerspexProperty"/>'s local value.
        /// </summary>
        /// <param name="property">The property.</param>
        public void ClearValue(PerspexProperty property)
        {
            Contract.Requires<NullReferenceException>(property != null);

            SetValue(property, PerspexProperty.UnsetValue);
        }

        /// <summary>
        /// Gets an observable for a <see cref="PerspexProperty"/>.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>An observable.</returns>
        public IObservable<object> GetObservable(PerspexProperty property)
        {
            Contract.Requires<NullReferenceException>(property != null);

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
                GetObservableDescription(property));
        }

        /// <summary>
        /// Gets an observable for a <see cref="PerspexProperty"/>.
        /// </summary>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="property">The property.</param>
        /// <returns>An observable.</returns>
        public IObservable<T> GetObservable<T>(PerspexProperty<T> property)
        {
            Contract.Requires<NullReferenceException>(property != null);

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
                GetObservableDescription(property));
        }

        /// <summary>
        /// Gets a <see cref="PerspexProperty"/> value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>The value.</returns>
        public object GetValue(PerspexProperty property)
        {
            Contract.Requires<NullReferenceException>(property != null);

            object result;

            PriorityValue value;

            if (_values.TryGetValue(property, out value))
            {
                result = value.Value;
            }
            else
            {
                result = PerspexProperty.UnsetValue;
            }

            if (result == PerspexProperty.UnsetValue)
            {
                result = GetDefaultValue(property);
            }

            return result;
        }

        /// <summary>
        /// Gets a <see cref="PerspexProperty"/> value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <returns>The value.</returns>
        public T GetValue<T>(PerspexProperty<T> property)
        {
            Contract.Requires<NullReferenceException>(property != null);

            return (T)GetValue((PerspexProperty)property);
        }

        /// <summary>
        /// Gets all properties that are registered on this object.
        /// </summary>
        /// <returns>
        /// A collection of <see cref="PerspexProperty"/> objects.
        /// </returns>
        public IEnumerable<PerspexProperty> GetRegisteredProperties()
        {
            return GetRegisteredProperties(GetType());
        }

        /// <summary>
        /// Checks whether a <see cref="PerspexProperty"/> is set on this object.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>True if the property is set, otherwise false.</returns>
        public bool IsSet(PerspexProperty property)
        {
            Contract.Requires<NullReferenceException>(property != null);

            return _values.ContainsKey(property);
        }

        /// <summary>
        /// Checks whether a <see cref="PerspexProperty"/> is registered on this class.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>True if the property is registered, otherwise false.</returns>
        public bool IsRegistered(PerspexProperty property)
        {
            Type type = GetType();

            while (type != null)
            {
                List<PerspexProperty> list;

                if (s_registered.TryGetValue(type, out list))
                {
                    if (list.Contains(property))
                    {
                        return true;
                    }
                }

                type = type.GetTypeInfo().BaseType;
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
            Contract.Requires<NullReferenceException>(property != null);

            PriorityValue v;
            var originalValue = value;

            if (!IsRegistered(property))
            {
                throw new InvalidOperationException(string.Format(
                    "Property '{0}' not registered on '{1}'",
                    property.Name,
                    GetType()));
            }

            if (!TypeUtilities.TryCast(property.PropertyType, value, out value))
            {
                throw new InvalidOperationException(string.Format(
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

            _propertyLog.Verbose(
                "Set {Property} to {$Value} with priority {Priority}",
                property,
                value,
                priority);
            v.SetDirectValue(value, (int)priority);
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
            Contract.Requires<NullReferenceException>(property != null);

            SetValue((PerspexProperty)property, value, priority);
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
            Contract.Requires<NullReferenceException>(property != null);

            PriorityValue v;
            IDescription description = source as IDescription;

            if (!IsRegistered(property))
            {
                throw new InvalidOperationException(string.Format(
                    "Property '{0}' not registered on '{1}'",
                    property.Name,
                    GetType()));
            }

            if (!_values.TryGetValue(property, out v))
            {
                v = CreatePriorityValue(property);
                _values.Add(property, v);
            }

            _propertyLog.Verbose(
                "Bound {Property} to {Binding} with priority {Priority}",
                property,
                source,
                priority);

            return v.Add(source, (int)priority);
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
            Contract.Requires<NullReferenceException>(property != null);

            return Bind((PerspexProperty)property, source.Select(x => (object)x), priority);
        }

        /// <summary>
        /// Initialites a two-way bind between <see cref="PerspexProperty"/>s.
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
            return new CompositeDisposable(
                Bind(property, source.GetObservable(sourceProperty)),
                source.Bind(sourceProperty, GetObservable(property)));
        }

        /// <summary>
        /// Forces the specified property to be revalidated.
        /// </summary>
        /// <param name="property">The property.</param>
        public void Revalidate(PerspexProperty property)
        {
            PriorityValue value;

            if (_values.TryGetValue(property, out value))
            {
                value.Revalidate();
            }
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
        private string GetObservableDescription(PerspexProperty property)
        {
            return string.Format("{0}.{1}", GetType().Name, property.Name);
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="property">The property that has changed.</param>
        /// <param name="oldValue">The old property value.</param>
        /// <param name="newValue">The new property value.</param>
        /// <param name="priority">The priority of the binding that produced the value.</param>
        private void RaisePropertyChanged(
            PerspexProperty property,
            object oldValue,
            object newValue,
            BindingPriority priority)
        {
            Contract.Requires<NullReferenceException>(property != null);

            PerspexPropertyChangedEventArgs e = new PerspexPropertyChangedEventArgs(
                this,
                property,
                oldValue,
                newValue,
                priority);

            OnPropertyChanged(e);
            property.NotifyChanged(e);

            if (PropertyChanged != null)
            {
                PropertyChanged(this, e);
            }

            if (_inpcChanged != null)
            {
                PropertyChangedEventArgs e2 = new PropertyChangedEventArgs(property.Name);
                _inpcChanged(this, e2);
            }
        }
    }
}
