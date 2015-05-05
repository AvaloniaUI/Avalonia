// -----------------------------------------------------------------------
// <copyright file="PerspexObject.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reflection;
    using Perspex.Diagnostics;
    using Splat;
    using System.Reactive.Disposables;
    using Perspex.Reactive;

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
        LocalValue,

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
    public class PerspexObject : INotifyPropertyChanged, IEnableLogger
    {
        /// <summary>
        /// The registered properties by type.
        /// </summary>
        private static Dictionary<Type, List<PerspexProperty>> registered =
            new Dictionary<Type, List<PerspexProperty>>();

        /// <summary>
        /// The parent object that inherited values are inherited from.
        /// </summary>
        private PerspexObject inheritanceParent;

        /// <summary>
        /// The set values/bindings on this object.
        /// </summary>
        private Dictionary<PerspexProperty, PriorityValue> values =
            new Dictionary<PerspexProperty, PriorityValue>();

        /// <summary>
        /// Event handler for <see cref="INotifyPropertyChanged"/> implementation.
        /// </summary>
        private PropertyChangedEventHandler inpcChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerspexObject"/> class.
        /// </summary>
        public PerspexObject()
        {
            foreach (var p in this.GetAllValues())
            {
                var priority = p.PriorityValue != null ? 
                    (BindingPriority)p.PriorityValue.ValuePriority : 
                    BindingPriority.LocalValue;

                var e = new PerspexPropertyChangedEventArgs(
                    this, 
                    p.Property, 
                    PerspexProperty.UnsetValue, 
                    p.CurrentValue,
                    priority);

                p.Property.NotifyInitialized(e);
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
            add { this.inpcChanged += value; }
            remove { this.inpcChanged -= value; }
        }

        /// <summary>
        /// Gets or sets the parent object that inherited <see cref="PerspexProperty"/> values 
        /// are inherited from.
        /// </summary>
        protected PerspexObject InheritanceParent
        {
            get
            {
                return this.inheritanceParent;
            }

            set
            {
                if (this.inheritanceParent != value)
                {
                    if (this.inheritanceParent != null)
                    {
                        this.inheritanceParent.PropertyChanged -= this.ParentPropertyChanged;
                    }

                    var inherited = (from property in GetProperties(this.GetType())
                                     where property.Inherits
                                     select new
                                     {
                                         Property = property,
                                         Value = this.GetValue(property),
                                     }).ToList();

                    this.inheritanceParent = value;

                    foreach (var i in inherited)
                    {
                        object newValue = this.GetValue(i.Property);

                        if (!object.Equals(i.Value, newValue))
                        {
                            this.RaisePropertyChanged(i.Property, i.Value, newValue, BindingPriority.LocalValue);
                        }
                    }

                    if (this.inheritanceParent != null)
                    {
                        this.inheritanceParent.PropertyChanged += this.ParentPropertyChanged;
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
            get { return this.GetValue(property); }
            set { this.SetValue(property, value); }
        }

        /// <summary>
        /// Gets or sets a binding for a <see cref="PerspexProperty"/>.
        /// </summary>
        /// <param name="binding">The binding information.</param>
        public IObservable<object> this[Binding binding]
        {
            get
            {
                return new Binding
                {
                    Mode = binding.Mode,
                    Priority = binding.Priority,
                    Property = binding.Property,
                    Source = this,
                };
            }

            set
            {
                BindingMode mode = (binding.Mode == BindingMode.Default) ? 
                    binding.Property.DefaultBindingMode : 
                    binding.Mode;
                Binding sourceBinding = value as Binding;

                if (sourceBinding == null && mode != BindingMode.OneWay)
                {
                    throw new InvalidOperationException("Can only bind OneWay to plain IObservable.");
                }

                switch (mode)
                {
                    case BindingMode.Default:
                    case BindingMode.OneWay:
                        this.Bind(binding.Property, value, binding.Priority);
                        break;
                    case BindingMode.OneTime:
                        this.SetValue(binding.Property, sourceBinding.Source.GetValue(sourceBinding.Property), binding.Priority);
                        break;
                    case BindingMode.OneWayToSource:
                        sourceBinding.Source.Bind(sourceBinding.Property, this.GetObservable(binding.Property), binding.Priority);
                        break;
                    case BindingMode.TwoWay:
                        this.BindTwoWay(binding.Property, sourceBinding.Source, sourceBinding.Property);
                        break;
                }
            }
        }

        /// <summary>
        /// Gets all <see cref="PerspexProperty"/>s registered on a type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A collection of <see cref="PerspexProperty"/> definitions.</returns>
        public static IEnumerable<PerspexProperty> GetProperties(Type type)
        {
            Contract.Requires<NullReferenceException>(type != null);

            TypeInfo i = type.GetTypeInfo();

            while (type != null)
            {
                List<PerspexProperty> list;

                if (registered.TryGetValue(type, out list))
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

            if (!registered.TryGetValue(type, out list))
            {
                list = new List<PerspexProperty>();
                registered.Add(type, list);
            }

            if (!list.Contains(property))
            {
                list.Add(property);
            }
        }

        /// <summary>
        /// Clears a <see cref="PerspexProperty"/>'s local value.
        /// </summary>
        /// <param name="property">The property.</param>
        public void ClearValue(PerspexProperty property)
        {
            Contract.Requires<NullReferenceException>(property != null);
            
            this.SetValue(property, PerspexProperty.UnsetValue);
        }

        /// <summary>
        /// Gets an observable for a <see cref="PerspexProperty"/>.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>An observable.</returns>
        public IObservable<object> GetObservable(PerspexProperty property)
        {
            Contract.Requires<NullReferenceException>(property != null);

            return new PerspexObservable<object>(observer =>
            {
                EventHandler<PerspexPropertyChangedEventArgs> handler = (s, e) =>
                {
                    if (e.Property == property)
                    {
                        observer.OnNext(e.NewValue);
                    }
                };

                observer.OnNext(this.GetValue(property));

                this.PropertyChanged += handler;

                return Disposable.Create(() =>
                {
                    this.PropertyChanged -= handler;
                });
            }, this.GetObservableDescription(property));
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

            return this.GetObservable((PerspexProperty)property).Cast<T>();
        }

        /// <summary>
        /// Gets an observable for a <see cref="PerspexProperty"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public IObservable<Tuple<T, T>> GetObservableWithHistory<T>(PerspexProperty<T> property)
        {
            return new PerspexObservable<Tuple<T, T>>(observer =>
            {
                EventHandler<PerspexPropertyChangedEventArgs> handler = (s, e) =>
                {
                    if (e.Property == property)
                    {
                        observer.OnNext(Tuple.Create((T)e.OldValue, (T)e.NewValue));
                    }
                };

                this.PropertyChanged += handler;

                return Disposable.Create(() =>
                {
                    this.PropertyChanged -= handler;
                });
            }, this.GetObservableDescription(property));
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

            if (this.values.TryGetValue(property, out value))
            {
                result = value.Value;
            }
            else
            {
                result = PerspexProperty.UnsetValue;
            }

            if (result == PerspexProperty.UnsetValue)
            {
                result = this.GetDefaultValue(property);
            }

            return result;
        }

        /// <summary>
        /// Gets a <see cref="PerspexProperty"/> value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>The value.</returns>
        public T GetValue<T>(PerspexProperty<T> property)
        {
            Contract.Requires<NullReferenceException>(property != null);

            return (T)this.GetValue((PerspexProperty)property);
        }

        /// <summary>
        /// Gets the value of of all properties that are registered on this object.
        /// </summary>
        /// <returns>
        /// A collection of <see cref="PerspexPropertyValue"/> objects.
        /// </returns>
        public IEnumerable<PerspexPropertyValue> GetAllValues()
        {
            foreach (PerspexProperty property in this.GetRegisteredProperties())
            {
                PriorityValue value;

                if (this.values.TryGetValue(property, out value))
                {
                    yield return new PerspexPropertyValue(property, value);
                }
                else
                {
                    yield return new PerspexPropertyValue(property, this.GetValue(property));
                }
            }
        }

        /// <summary>
        /// Gets all properties that are registered on this object.
        /// </summary>
        /// <returns>
        /// A collection of <see cref="PerspexProperty"/> objects.
        /// </returns>
        public IEnumerable<PerspexProperty> GetRegisteredProperties()
        {
            Type type = this.GetType();

            while (type != null)
            {
                List<PerspexProperty> list;

                if (registered.TryGetValue(type, out list))
                {
                    foreach (var p in list)
                    {
                        yield return p;
                    }
                }

                type = type.GetTypeInfo().BaseType;
            }
        }

        /// <summary>
        /// Gets all of the <see cref="PerspexProperty"/> values explicitly set on this object.
        /// </summary>
        public IEnumerable<PerspexPropertyValue> GetSetValues()
        {
            foreach (var value in this.values)
            {
                yield return new PerspexPropertyValue(value.Key, value.Value);
            }
        }

        /// <summary>
        /// Checks whether a <see cref="PerspexProperty"/> is set on this object.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>True if the property is set, otherwise false.</returns>
        public bool IsSet(PerspexProperty property)
        {
            Contract.Requires<NullReferenceException>(property != null);

            return this.values.ContainsKey(property);
        }

        /// <summary>
        /// Checks whether a <see cref="PerspexProperty"/> is registered on this class.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>True if the property is registered, otherwise false.</returns>
        public bool IsRegistered(PerspexProperty property)
        {
            Type type = this.GetType();

            while (type != null)
            {
                List<PerspexProperty> list;

                if (registered.TryGetValue(type, out list))
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

            if (!this.IsRegistered(property))
            {
                throw new InvalidOperationException(string.Format(
                    "Property '{0}' not registered on '{1}'",
                    property.Name,
                    this.GetType()));
            }

            if (!PriorityValue.IsValidValue(value, property.PropertyType))
            {
                throw new InvalidOperationException(string.Format(
                    "Invalid value for Property '{0}': {1} ({2})",
                    property.Name,
                    value,
                    value.GetType().FullName));
            }

            if (!this.values.TryGetValue(property, out v))
            {
                if (value == PerspexProperty.UnsetValue)
                {
                    return;
                }

                v = this.CreatePriorityValue(property);
                this.values.Add(property, v);
            }
            
            this.Log().Debug(
                "Set local value of {0}.{1} (#{2:x8}) to {3}",
                this.GetType().Name,
                property.Name,
                this.GetHashCode(),
                value);

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

            this.SetValue((PerspexProperty)property, value, priority);
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
        public IDisposable Bind(
            PerspexProperty property,
            IObservable<object> source,
            BindingPriority priority = BindingPriority.LocalValue)
        {
            Contract.Requires<NullReferenceException>(property != null);

            PriorityValue v;
            IDescription description = source as IDescription;

            if (!this.values.TryGetValue(property, out v))
            {
                v = this.CreatePriorityValue(property);
                this.values.Add(property, v);
            }

            this.Log().Debug(
                "Bound value of {0}.{1} (#{2:x8}) to {3}",
                this.GetType().Name,
                property.Name,
                this.GetHashCode(),
                description != null ? description.Description : "[Anonymous]");

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

            return this.Bind((PerspexProperty)property, source.Select(x => (object)x), priority);
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
                this.Bind(property, source.GetObservable(sourceProperty)),
                source.Bind(sourceProperty, this.GetObservable(property)));
        }

        /// <summary>
        /// Forces the specified property to be re-coerced.
        /// </summary>
        /// <param name="property">The property.</param>
        public void CoerceValue(PerspexProperty property)
        {
            PriorityValue value;

            if (this.values.TryGetValue(property, out value))
            {
                value.Coerce();
            }
        }

        /// <summary>
        /// Forces re-coercion of properties when a property value changes.
        /// </summary>
        /// <param name="property">The property to that affects coercion.</param>
        /// <param name="affected">The affected properties.</param>
        protected static void AffectsCoercion(PerspexProperty property, params PerspexProperty[] affected)
        {
            property.Changed.Subscribe(e =>
            {
                foreach (var p in affected)
                {
                    e.Sender.CoerceValue(p);
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
            Func<object, object> coerce = null;

            if (property.Coerce != null)
            {
                coerce = v => property.Coerce(this, v);
            }

            PriorityValue result = new PriorityValue(property.Name, property.PropertyType, coerce);

            result.Changed.Subscribe(x =>
            {
                object oldValue = (x.Item1 == PerspexProperty.UnsetValue) ?
                    this.GetDefaultValue(property) :
                    x.Item1;
                object newValue = (x.Item2 == PerspexProperty.UnsetValue) ?
                    this.GetDefaultValue(property) :
                    x.Item2;

                if (!object.Equals(oldValue, newValue))
                {
                    this.RaisePropertyChanged(property, oldValue, newValue, (BindingPriority)result.ValuePriority);

                    this.Log().Debug(
                        "Value of {0}.{1} (#{2:x8}) changed from {3} to {4}",
                        this.GetType().Name,
                        property.Name,
                        this.GetHashCode(),
                        oldValue,
                        newValue);
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
            if (property.Inherits && this.inheritanceParent != null)
            {
                return this.inheritanceParent.GetValue(property);
            }
            else
            {
                return property.GetDefaultValue(this.GetType());
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

            if (e.Property.Inherits && !this.IsSet(e.Property))
            {
                this.RaisePropertyChanged(e.Property, e.OldValue, e.NewValue, BindingPriority.LocalValue);
            }
        }

        /// <summary>
        /// Gets a description of a property that van be used in observables.
        /// </summary>
        /// <param name="property">The property</param>
        /// <returns>The description.</returns>
        private string GetObservableDescription(PerspexProperty property)
        {
            return string.Format("{0}.{1}", this.GetType().Name, property.Name);
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

            this.OnPropertyChanged(e);
            property.NotifyChanged(e);

            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, e);
            }

            if (this.inpcChanged != null)
            {
                PropertyChangedEventArgs e2 = new PropertyChangedEventArgs(property.Name);
                this.inpcChanged(this, e2);
            }
        }
    }
}
