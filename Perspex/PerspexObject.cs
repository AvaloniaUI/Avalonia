// -----------------------------------------------------------------------
// <copyright file="PerspexObject.cs" company="Tricycle">
// Copyright 2013 Tricycle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Reflection;

    /// <summary>
    /// An object with <see cref="PerspexProperty"/> support.
    /// </summary>
    /// <remarks>
    /// This class is analogous to DependencyObject in WPF.
    /// </remarks>
    public class PerspexObject
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
        /// The set values on this object.
        /// </summary>
        private Dictionary<PerspexProperty, object> values =
            new Dictionary<PerspexProperty, object>();

        /// <summary>
        /// The current bindings on this object.
        /// </summary>
        private Dictionary<PerspexProperty, IDisposable> bindings =
            new Dictionary<PerspexProperty, IDisposable>();

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
        /// Binds a property on this object to an observable.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="target">The target property.</param>
        /// <param name="source">The observable.</param>
        /// <returns>A disposable binding.</returns>
        public void Bind<T>(PerspexProperty<T> target, IObservable<T> source)
        {
            Contract.Requires<NullReferenceException>(target != null);
            Contract.Requires<NullReferenceException>(source != null);

            this.ClearBinding(target);

            IDisposable binding = source.Subscribe(value =>
            {
                this.SetValueImpl(target, value);
            });

            this.bindings.Add(target, binding);
        }

        /// <summary>
        /// Clears a binding on a <see cref="PerspexProperty"/>, leaving the last bound value in
        /// place.
        /// </summary>
        /// <param name="property">The property.</param>
        public void ClearBinding(PerspexProperty property)
        {
            Contract.Requires<NullReferenceException>(property != null);
            IDisposable binding;

            if (this.bindings.TryGetValue(property, out binding))
            {
                binding.Dispose();
                this.bindings.Remove(property);
            }
        }

        /// <summary>
        /// Clears a <see cref="PerspexProperty"/> value, including its bindings.
        /// </summary>
        /// <param name="property">The property.</param>
        public void ClearValue(PerspexProperty property)
        {
            Contract.Requires<NullReferenceException>(property != null);
            this.ClearBinding(property);
            this.values.Remove(property);
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

            object value;

            if (!this.values.TryGetValue(property, out value))
            {
                if (property.Inherits && this.inheritanceParent != null)
                {
                    value = this.inheritanceParent.GetValue(property);
                }
                else
                {
                    value = property.GetDefaultValue(this.GetType());
                }
            }

            return value;
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

            this.ClearBinding(property);
            this.SetValueImpl(property, value);
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
        /// Called when a property is changed on the current <see cref="InheritanceParent"/>.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        /// <remarks>
        /// Checks for changes in an inherited property value.
        /// </remarks>
        private void ParentPropertyChanged(object sender, PerspexPropertyChangedEventArgs e)
        {
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

        public void SetValueImpl(PerspexProperty property, object value)
        {
            Contract.Requires<NullReferenceException>(property != null);

            object oldValue = this.GetValue(property);

            if (!object.Equals(oldValue, value))
            {
                this.values[property] = value;
                this.RaisePropertyChanged(property, oldValue, value);
            }
        }
    }
}
