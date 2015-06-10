// -----------------------------------------------------------------------
// <copyright file="PerspexProperty.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using Perspex.Utilities;
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields must be private", Justification = "It's readonly")]
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
        /// <param name="isAttached">Whether the property is an attached property.</param>
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
        /// <value>
        /// The name of the property.
        /// </value>
        public string Name { get; }

        /// <summary>
        /// Gets the type of the property's value.
        /// </summary>
        /// <value>
        /// The type of the property's value.
        /// </value>
        public Type PropertyType { get; }

        /// <summary>
        /// Gets the type of the class that registers the property.
        /// </summary>
        /// <value>
        /// The type of the class that registers the property.
        /// </value>
        public Type OwnerType { get; }

        /// <summary>
        /// Gets a value indicating whether the property inherits its value.
        /// </summary>
        /// <value>
        /// A value indicating whether the property inherits its value.
        /// </value>
        public bool Inherits { get; }

        /// <summary>
        /// Gets the default binding mode for the property.
        /// </summary>
        /// <value>
        /// The default binding mode for the property.
        /// </value>
        public BindingMode DefaultBindingMode { get; }

        /// <summary>
        /// Gets the property's coerce function.
        /// </summary>
        /// <value>
        /// The property's coerce function.
        /// </value>
        public Func<PerspexObject, object, object> Coerce { get; }

        /// <summary>
        /// Gets a value indicating whether this is an attached property.
        /// </summary>
        /// <value>
        /// A value indicating whether this is an attached property.
        /// </value>
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
        /// <value>
        /// An observable that is fired when this property is initialized on a new
        /// <see cref="PerspexObject"/> instance.
        /// </value>
        public IObservable<PerspexPropertyChangedEventArgs> Initialized
        {
            get { return this.initialized; }
        }

        /// <summary>
        /// Gets an observable that is fired when this property changes on any
        /// <see cref="PerspexObject"/> instance.
        /// </summary>
        /// <value>
        /// An observable that is fired when this property changes on any
        /// <see cref="PerspexObject"/> instance.
        /// </value>
        public IObservable<PerspexPropertyChangedEventArgs> Changed
        {
            get { return this.changed; }
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

        /// <summary>
        /// Checks whether the <paramref name="value"/> is valid for the property.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>True if the value is valid, otherwise false.</returns>
        public bool IsValidValue(object value)
        {
            return TypeUtilities.TryCast(this.PropertyType, value, out value);
        }

        /// <summary>
        /// Overrides the default value for the property on the specified type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="defaultValue">The default value.</param>
        public void OverrideDefaultValue<T>(object defaultValue)
        {
            this.OverrideDefaultValue(typeof(T), defaultValue);
        }

        /// <summary>
        /// Overrides the default value for the property on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="defaultValue">The default value.</param>
        public void OverrideDefaultValue(Type type, object defaultValue)
        {
            Contract.Requires<NullReferenceException>(type != null);

            if (!TypeUtilities.TryCast(this.PropertyType, defaultValue, out defaultValue))
            {
                throw new InvalidOperationException(string.Format(
                    "Invalid value for Property '{0}': {1} ({2})",
                    this.Name,
                    defaultValue,
                    defaultValue.GetType().FullName));
            }

            if (this.defaultValues.ContainsKey(type))
            {
                throw new InvalidOperationException("Default value is already set for this property.");
            }

            this.defaultValues.Add(type, defaultValue);
        }

        /// <summary>
        /// Gets the string representation of the property.
        /// </summary>
        /// <returns>The property's string representation.</returns>
        public override string ToString()
        {
            return this.Name;
        }

        /// <summary>
        /// Notifies the <see cref="Initialized"/> observable.
        /// </summary>
        /// <param name="e">The observable arguments.</param>
        internal void NotifyInitialized(PerspexPropertyChangedEventArgs e)
        {
            this.initialized.OnNext(e);
        }

        /// <summary>
        /// Notifies the <see cref="Changed"/> observable.
        /// </summary>
        /// <param name="e">The observable arguments.</param>
        internal void NotifyChanged(PerspexPropertyChangedEventArgs e)
        {
            this.changed.OnNext(e);
        }

        /// <summary>
        /// Class representing the <see cref="UnsetValue"/>.
        /// </summary>
        private class Unset
        {
            /// <summary>
            /// Returns the string representation of the <see cref="UnsetValue"/>.
            /// </summary>
            /// <returns>The string "(unset)".</returns>
            public override string ToString()
            {
                return "(unset)";
            }
        }
    }
}
