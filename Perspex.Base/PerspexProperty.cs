// -----------------------------------------------------------------------
// <copyright file="PerspexProperty.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Subjects;
    using System.Reflection;

    /// <summary>
    /// A perspex property.
    /// </summary>
    /// <remarks>
    /// This class is analogous to DependencyProperty in WPF.
    /// </remarks>
    public class PerspexProperty
    {
        /// <summary>
        /// Represents an unset property value.
        /// </summary>
        public static readonly object UnsetValue = new Unset();

        /// <summary>
        /// The default values for the property, by type.
        /// </summary>
        private Dictionary<Type, object> defaultValues = new Dictionary<Type, object>();

        /// <summary>
        /// Observable fired when this property changes on any <see cref="PerspexObject"/>.
        /// </summary>
        private Subject<PerspexPropertyChangedEventArgs> initialized = new Subject<PerspexPropertyChangedEventArgs>();

        /// <summary>
        /// Observable fired when this property changes on any <see cref="PerspexObject"/>.
        /// </summary>
        private Subject<PerspexPropertyChangedEventArgs> changed = new Subject<PerspexPropertyChangedEventArgs>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PerspexProperty"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="valueType">The type of the property's value.</param>
        /// <param name="ownerType">The type of the class that registers the property.</param>
        /// <param name="defaultValue">The default value of the property.</param>
        /// <param name="inherits">Whether the property inherits its value.</param>
        /// <param name="defaultBindingMode">The default binding mode for the property.</param>
        /// <param name="coerce">A coercion function.</param>
        public PerspexProperty(
            string name,
            Type valueType,
            Type ownerType,
            object defaultValue,
            bool inherits = false,
            BindingMode defaultBindingMode = BindingMode.Default,
            Func<PerspexObject, object, object> coerce = null,
            bool isAttached = false)
        {
            Contract.Requires<NullReferenceException>(name != null);
            Contract.Requires<NullReferenceException>(valueType != null);
            Contract.Requires<NullReferenceException>(ownerType != null);

            this.Name = name;
            this.PropertyType = valueType;
            this.OwnerType = ownerType;
            this.defaultValues.Add(ownerType, defaultValue);
            this.Inherits = inherits;
            this.DefaultBindingMode = defaultBindingMode;
            this.Coerce = coerce;
            this.IsAttached = isAttached;
        }

        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the type of the property's value.
        /// </summary>
        public Type PropertyType { get; }

        /// <summary>
        /// Gets the type of the class that registers the property.
        /// </summary>
        public Type OwnerType { get; }

        /// <summary>
        /// Gets a value indicating whether the property inherits its value.
        /// </summary>
        public bool Inherits { get; }

        /// <summary>
        /// Gets the default binding mode for the property.
        /// </summary>
        /// <returns></returns>
        public BindingMode DefaultBindingMode { get; }

        /// <summary>
        /// Gets the property's coerce function.
        /// </summary>
        public Func<PerspexObject, object, object> Coerce { get; }

        /// <summary>
        /// Gets a value indicating whether this is an attached property.
        /// </summary>
        public bool IsAttached { get; }

        /// <summary>
        /// Gets an observable that is fired when this property is initialized on a
        /// new <see cref="PerspexObject"/> instance.
        /// </summary>
        /// <remarks>
        /// This observable is fired each time a new <see cref="PerspexObject"/> is constructed
        /// for all properties registered on the object's type. The default value of the property 
        /// for the object is passed in the args' NewValue (OldValue will always be 
        /// <see cref="UnsetValue"/>.
        /// </remarks>
        public IObservable<PerspexPropertyChangedEventArgs> Initialized
        {
            get { return this.initialized; }
        }

        /// <summary>
        /// Gets an observable that is fired when this property changes on any 
        /// <see cref="PerspexObject"/> instance.
        /// </summary>
        public IObservable<PerspexPropertyChangedEventArgs> Changed
        {
            get { return this.changed; }
        }

        /// <summary>
        /// Registers a <see cref="PerspexProperty"/>.
        /// </summary>
        /// <typeparam name="TOwner">The type of the class that is registering the property.</typeparam>
        /// <typeparam name="TValue">The type of the property's value.</typeparam>
        /// <param name="name">The name of the property.</param>
        /// <param name="defaultValue">The default value of the property.</param>
        /// <param name="inherits">Whether the property inherits its value.</param>
        /// <param name="defaultBindingMode">The default binding mode for the property.</param>
        /// <param name="coerce">A coercion function.</param>
        /// <returns>A <see cref="PerspexProperty{TValue}"/></returns>
        public static PerspexProperty<TValue> Register<TOwner, TValue>(
            string name,
            TValue defaultValue = default(TValue),
            bool inherits = false,
            BindingMode defaultBindingMode = BindingMode.OneWay,
            Func<PerspexObject, TValue, TValue> coerce = null)
            where TOwner : PerspexObject
        {
            Contract.Requires<NullReferenceException>(name != null);

            PerspexProperty<TValue> result = new PerspexProperty<TValue>(
                name,
                typeof(TOwner),
                defaultValue,
                inherits,
                defaultBindingMode,
                coerce,
                false);

            PerspexObject.Register(typeof(TOwner), result);

            return result;
        }

        /// <summary>
        /// Registers an attached <see cref="PerspexProperty"/>.
        /// </summary>
        /// <typeparam name="TOwner">The type of the class that is registering the property.</typeparam>
        /// <typeparam name="THost">The type of the class that the property is to be registered on.</typeparam>
        /// <typeparam name="TValue">The type of the property's value.</typeparam>
        /// <param name="name">The name of the property.</param>
        /// <param name="defaultValue">The default value of the property.</param>
        /// <param name="inherits">Whether the property inherits its value.</param>
        /// <param name="defaultBindingMode">The default binding mode for the property.</param>
        /// <param name="coerce">A coercion function.</param>
        /// <returns>A <see cref="PerspexProperty{TValue}"/></returns>
        public static PerspexProperty<TValue> RegisterAttached<TOwner, THost, TValue>(
            string name,
            TValue defaultValue = default(TValue),
            bool inherits = false,
            BindingMode defaultBindingMode = BindingMode.OneWay,
            Func<PerspexObject, TValue, TValue> coerce = null)
        {
            Contract.Requires<NullReferenceException>(name != null);

            PerspexProperty<TValue> result = new PerspexProperty<TValue>(
                name,
                typeof(TOwner),
                defaultValue,
                inherits,
                defaultBindingMode,
                coerce,
                true);

            PerspexObject.Register(typeof(THost), result);

            return result;
        }

        /// <summary>
        /// Registers an attached <see cref="PerspexProperty"/>.
        /// </summary>
        /// <typeparam name="THost">The type of the class that the property is to be registered on.</typeparam>
        /// <typeparam name="TValue">The type of the property's value.</typeparam>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The type of the class that is registering the property.</param>
        /// <param name="defaultValue">The default value of the property.</param>
        /// <param name="inherits">Whether the property inherits its value.</param>
        /// <param name="defaultBindingMode">The default binding mode for the property.</param>
        /// <param name="coerce">A coercion function.</param>
        /// <returns>A <see cref="PerspexProperty{TValue}"/></returns>
        public static PerspexProperty<TValue> RegisterAttached<THost, TValue>(
            string name,
            Type ownerType,
            TValue defaultValue = default(TValue),
            bool inherits = false,
            BindingMode defaultBindingMode = BindingMode.OneWay,
            Func<PerspexObject, TValue, TValue> coerce = null)
        {
            Contract.Requires<NullReferenceException>(name != null);

            PerspexProperty<TValue> result = new PerspexProperty<TValue>(
                name,
                ownerType,
                defaultValue,
                inherits,
                defaultBindingMode,
                coerce,
                true);

            PerspexObject.Register(typeof(THost), result);

            return result;
        }

        /// <summary>
        /// Provides access to a property's binding via the <see cref="PerspexObject"/> 
        /// indexer.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>A <see cref="Binding"/> describing the binding.</returns>
        public static Binding operator !(PerspexProperty property)
        {
            return new Binding
            {
                Priority = BindingPriority.LocalValue,
                Property = property,
            };
        }

        /// <summary>
        /// Provides access to a property's template binding via the <see cref="PerspexObject"/>
        /// indexer.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>A <see cref="Binding"/> describing the binding.</returns>
        public static Binding operator ~(PerspexProperty property)
        {
            return new Binding
            {
                Priority = BindingPriority.TemplatedParent,
                Property = property,
            };
        }

        /// <summary>
        /// Returns a binding accessor that can be passed to <see cref="PerspexObject"/>'s [] 
        /// operator to initiate a binding.
        /// </summary>
        /// <returns>A <see cref="Binding"/>.</returns>
        /// <remarks>
        /// The ! and ~ operators are short forms of this.
        /// </remarks>
        public Binding Bind()
        {
            return new Binding
            {
                Property = this,
            };
        }

        /// <summary>
        /// Gets the default value for the property on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The default value.</returns>
        public object GetDefaultValue(Type type)
        {
            Contract.Requires<NullReferenceException>(type != null);

            while (type != null)
            {
                object result;

                if (this.defaultValues.TryGetValue(type, out result))
                {
                    return result;
                }

                type = type.GetTypeInfo().BaseType;
            }

            return this.defaultValues[this.OwnerType];
        }

        public bool IsValidValue(object value)
        {
            if (value == UnsetValue)
            {
                return true;
            }
            else if (value == null)
            {
                return !this.PropertyType.GetTypeInfo().IsValueType ||
                    Nullable.GetUnderlyingType(this.PropertyType) != null;
            }

            return this.PropertyType.GetTypeInfo().IsAssignableFrom(value.GetType().GetTypeInfo());
        }

        /// <summary>
        /// Gets the default value for the property on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="defaultValue">The default value.</param>
        public void OverrideDefaultValue(Type type, object defaultValue)
        {
            Contract.Requires<NullReferenceException>(type != null);

            // TODO: Ensure correct type.
            if (this.defaultValues.ContainsKey(type))
            {
                throw new InvalidOperationException("Default value is already set for this property.");
            }

            this.defaultValues.Add(type, defaultValue);
        }

        public override string ToString()
        {
            return this.Name;
        }

        internal void NotifyInitialized(PerspexPropertyChangedEventArgs e)
        {
            this.initialized.OnNext(e);
        }

        internal void NotifyChanged(PerspexPropertyChangedEventArgs e)
        {
            this.changed.OnNext(e);
        }

        private class Unset
        {
            public override string ToString()
            {
                return "(unset)";
            }
        }
    }

    /// <summary>
    /// A typed perspex property.
    /// </summary>
    public class PerspexProperty<TValue> : PerspexProperty
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PerspexProperty"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The type of the class that registers the property.</param>
        /// <param name="defaultValue">The default value of the property.</param>
        /// <param name="inherits">Whether the property inherits its value.</param>
        /// <param name="defaultBindingMode">The default binding mode for the property.</param>
        /// <param name="coerce">A coercion function.</param>
        /// <param name="isAttached">Whether the property is an attached property.</param>
        public PerspexProperty(
            string name,
            Type ownerType,
            TValue defaultValue = default(TValue),
            bool inherits = false,
            BindingMode defaultBindingMode = BindingMode.Default,
            Func<PerspexObject, TValue, TValue> coerce = null,
            bool isAttached = false)
            : base(
                name, 
                typeof(TValue), 
                ownerType, 
                defaultValue, 
                inherits, 
                defaultBindingMode,
                Convert(coerce),
                isAttached)
        {
            Contract.Requires<NullReferenceException>(name != null);
            Contract.Requires<NullReferenceException>(ownerType != null);
        }

        /// <summary>
        /// Registers the property on another type.
        /// </summary>
        /// <typeparam name="TOwner">The type of the additional owner.</typeparam>
        /// <returns>The property.</returns>
        public PerspexProperty<TValue> AddOwner<TOwner>()
        {
            PerspexObject.Register(typeof(TOwner), this);
            return this;
        }

        /// <summary>
        /// Gets the default value for the property on the specified type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <returns>The default value.</returns>
        public TValue GetDefaultValue<T>()
        {
            return (TValue)this.GetDefaultValue(typeof(T));
        }

        /// <summary>
        /// Converts from a typed coercion function to an untyped.
        /// </summary>
        /// <param name="f">The typed coercion function.</param>
        /// <returns>Te untyped coercion function.</returns>
        private static Func<PerspexObject, object, object> Convert(Func<PerspexObject, TValue, TValue> f)
        {
            return f != null ? (o, v) => f(o, (TValue)v) : (Func<PerspexObject, object, object>)null;
        }
    }
}
