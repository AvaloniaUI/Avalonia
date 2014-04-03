// -----------------------------------------------------------------------
// <copyright file="PerspexObject.cs" company="Tricycle">
// Copyright 2013 Tricycle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Reflection;
    using Splat;

    /// <summary>
    /// The priority of a binding.
    /// </summary>
    public enum BindingPriority
    {
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
    }

    /// <summary>
    /// An object with <see cref="PerspexProperty"/> support.
    /// </summary>
    /// <remarks>
    /// This class is analogous to DependencyObject in WPF.
    /// </remarks>
    public class PerspexObject : IEnableLogger
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
        /// Raised when a <see cref="PerspexProperty"/> value changes on this object/
        /// </summary>
        public event EventHandler<PerspexPropertyChangedEventArgs> PropertyChanged;

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
                            this.RaisePropertyChanged(i.Property, i.Value, newValue);
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
        /// Clears a binding on a <see cref="PerspexProperty"/>, leaving the last bound value in
        /// place.
        /// </summary>
        /// <param name="property">The property.</param>
        public void ClearBinding(PerspexProperty property)
        {
            throw new NotImplementedException();
            //Contract.Requires<NullReferenceException>(property != null);
            //PriorityValue value;

            //if (this.values.TryGetValue(property, out value))
            //{
            //    value.ClearLocalBinding();

            //    this.Log().Debug(string.Format(
            //        "Cleared binding on {0}.{1} (#{2:x8})",
            //        this.GetType().Name,
            //        property.Name,
            //        this.GetHashCode()));
            //}
        }

        /// <summary>
        /// Clears a <see cref="PerspexProperty"/> value, including its binding.
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
        /// <param name="property"></param>
        /// <returns></returns>
        public IObservable<object> GetObservable(PerspexProperty property)
        {
            Contract.Requires<NullReferenceException>(property != null);

            return Observable.Create<object>(observer =>
            {
                EventHandler<PerspexPropertyChangedEventArgs> handler = (s, e) =>
                {
                    if (e.Property == property)
                    {
                        observer.OnNext(e.NewValue);
                    }
                };

                this.PropertyChanged += handler;
                observer.OnNext(this.GetValue(property));

                return () =>
                {
                    this.PropertyChanged -= handler;
                };
            });
        }

        /// <summary>
        /// Gets an observable for a <see cref="PerspexProperty"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public IObservable<T> GetObservable<T>(PerspexProperty<T> property)
        {
            Contract.Requires<NullReferenceException>(property != null);

            return this.GetObservable((PerspexProperty)property).Cast<T>();
        }

        /// <summary>
        /// Gets an observable for a <see cref="ReadOnlyPerspexProperty"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public IObservable<T> GetObservable<T>(ReadOnlyPerspexProperty<T> property)
        {
            Contract.Requires<NullReferenceException>(property != null);

            return this.GetObservable((PerspexProperty<T>)property.Property);
        }

        /// <summary>
        /// Gets an observable for a <see cref="PerspexProperty"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public IObservable<Tuple<T, T>> GetObservableWithHistory<T>(PerspexProperty<T> property)
        {
            return Observable.Create<Tuple<T, T>>(observer =>
            {
                EventHandler<PerspexPropertyChangedEventArgs> handler = (s, e) =>
                {
                    if (e.Property == property)
                    {
                        observer.OnNext(Tuple.Create((T)e.OldValue, (T)e.NewValue));
                    }
                };

                this.PropertyChanged += handler;

                return () =>
                {
                    this.PropertyChanged -= handler;
                };
            });
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
        /// Gets a <see cref="ReadOnlyPerspexProperty"/> value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <returns>The value.</returns>
        public T GetValue<T>(ReadOnlyPerspexProperty<T> property)
        {
            Contract.Requires<NullReferenceException>(property != null);

            return (T)this.GetValue(property.Property);
        }

        /// <summary>
        /// Checks whether a <see cref="PerspexProperty"/> is set on this object.
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        public bool IsSet(PerspexProperty property)
        {
            Contract.Requires<NullReferenceException>(property != null);

            return this.values.ContainsKey(property);
        }

        /// <summary>
        /// Sets a <see cref="PerspexProperty"/> value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        public void SetValue(PerspexProperty property, object value)
        {
            Contract.Requires<NullReferenceException>(property != null);

            const int priority = (int)BindingPriority.LocalValue;
            PriorityValue v;

            if (!this.values.TryGetValue(property, out v))
            {
                if (value == PerspexProperty.UnsetValue)
                {
                    return;
                }

                v = this.CreatePriorityValue(property);
                this.values.Add(property, v);
            }

            this.Log().Debug(string.Format(
                "Set local value of {0}.{1} (#{2:x8}) to {3}",
                this.GetType().Name,
                property.Name,
                this.GetHashCode(),
                value));

            v.Clear(priority);
            v.Add(Observable.Never<object>().StartWith(value), priority);
        }

        /// <summary>
        /// Sets a <see cref="PerspexProperty"/> value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        public void SetValue<T>(PerspexProperty<T> property, T value)
        {
            Contract.Requires<NullReferenceException>(property != null);

            this.SetValue((PerspexProperty)property, value);
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
            IObservableDescription description = source as IObservableDescription;

            if (!this.values.TryGetValue(property, out v))
            {
                v = this.CreatePriorityValue(property);
                this.values.Add(property, v);
            }

            if (priority == BindingPriority.LocalValue)
            {
                v.Clear((int)priority);
            }

            this.Log().Debug(string.Format(
                "Bound value of {0}.{1} (#{2:x8}) to {3}",
                this.GetType().Name,
                property.Name,
                this.GetHashCode(),
                description != null ? description.Description : "[Anonymous]"));

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

            return this.Bind((PerspexProperty)property, (IObservable<object>)source, priority);
        }

        private PriorityValue CreatePriorityValue(PerspexProperty property)
        {
            PriorityValue result = new PriorityValue();

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
                    this.RaisePropertyChanged(property, oldValue, newValue);

                    this.Log().Debug(string.Format(
                        "Value of {0}.{1} (#{2:x8}) changed from {3} to {4}",
                        this.GetType().Name,
                        property.Name,
                        this.GetHashCode(),
                        oldValue,
                        newValue));
                }
            });

            return result;
        }

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
                this.RaisePropertyChanged(e.Property, e.OldValue, e.NewValue);
            }
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="property">The property that has changed.</param>
        /// <param name="oldValue">The old property value.</param>
        /// <param name="newValue">The new property value.</param>
        private void RaisePropertyChanged(PerspexProperty property, object oldValue, object newValue)
        {
            Contract.Requires<NullReferenceException>(property != null);

            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(
                    this,
                    new PerspexPropertyChangedEventArgs(property, oldValue, newValue));
            }
        }
    }
}
