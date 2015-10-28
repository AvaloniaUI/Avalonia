// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using Perspex.Utilities;

namespace Perspex
{
    /// <summary>
    /// A perspex property.
    /// </summary>
    /// <remarks>
    /// This class is analogous to DependencyProperty in WPF.
    /// </remarks>
    public class PerspexProperty : IEquatable<PerspexProperty>
    {
        /// <summary>
        /// Represents an unset property value.
        /// </summary>
        public static readonly object UnsetValue = new Unset();

        /// <summary>
        /// Gets the next ID that will be allocated to a property.
        /// </summary>
        private static int s_nextId = 1;

        /// <summary>
        /// The default value provided when the property was first registered.
        /// </summary>
        private readonly object _defaultValue;

        /// <summary>
        /// The overridden default values for the property, by type.
        /// </summary>
        private readonly Dictionary<Type, object> _defaultValues;

        /// <summary>
        /// Observable fired when this property changes on any <see cref="PerspexObject"/>.
        /// </summary>
        private readonly Subject<PerspexPropertyChangedEventArgs> _initialized;

        /// <summary>
        /// Observable fired when this property changes on any <see cref="PerspexObject"/>.
        /// </summary>
        private readonly Subject<PerspexPropertyChangedEventArgs> _changed;

        /// <summary>
        /// The validation functions for the property, by type.
        /// </summary>
        private readonly Dictionary<Type, Func<PerspexObject, object, object>> _validation;

        /// <summary>
        /// Gets the ID of the property.
        /// </summary>
        private int _id;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerspexProperty"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="valueType">The type of the property's value.</param>
        /// <param name="ownerType">The type of the class that registers the property.</param>
        /// <param name="defaultValue">The default value of the property.</param>
        /// <param name="inherits">Whether the property inherits its value.</param>
        /// <param name="defaultBindingMode">The default binding mode for the property.</param>
        /// <param name="validate">A validation function.</param>
        /// <param name="notifying">
        /// A method that gets called before and after the property starts being notified on an
        /// object; the bool argument will be true before and false afterwards. This callback is
        /// intended to support IsDataContextChanging.
        /// </param>
        /// <param name="isAttached">Whether the property is an attached property.</param>
        public PerspexProperty(
            string name,
            Type valueType,
            Type ownerType,
            object defaultValue,
            bool inherits = false,
            BindingMode defaultBindingMode = BindingMode.Default,
            Func<PerspexObject, object, object> validate = null,
            Action<PerspexObject, bool> notifying = null,
            bool isAttached = false)
        {
            Contract.Requires<ArgumentNullException>(name != null);
            Contract.Requires<ArgumentNullException>(valueType != null);
            Contract.Requires<ArgumentNullException>(ownerType != null);

            if (name.Contains("."))
            {
                throw new ArgumentException("'name' may not contain periods.");
            }

            _defaultValues = new Dictionary<Type, object>();
            _initialized = new Subject<PerspexPropertyChangedEventArgs>();
            _changed = new Subject<PerspexPropertyChangedEventArgs>();
            _validation = new Dictionary<Type, Func<PerspexObject, object, object>>();

            Name = name;
            PropertyType = valueType;
            OwnerType = ownerType;
            _defaultValue = defaultValue;
            Inherits = inherits;
            DefaultBindingMode = defaultBindingMode;
            IsAttached = isAttached;
            Notifying = notifying;
            _id = s_nextId++;

            if (validate != null)
            {
                _validation.Add(ownerType, validate);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PerspexProperty"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="valueType">The type of the property's value.</param>
        /// <param name="ownerType">The type of the class that registers the property.</param>
        /// <param name="getter">Gets the current value of the property.</param>
        /// <param name="setter">Sets the value of the property.</param>
        public PerspexProperty(
            string name,
            Type valueType,
            Type ownerType,
            Func<PerspexObject, object> getter,
            Action<PerspexObject, object> setter)
        {
            Contract.Requires<ArgumentNullException>(name != null);
            Contract.Requires<ArgumentNullException>(valueType != null);
            Contract.Requires<ArgumentNullException>(ownerType != null);
            Contract.Requires<ArgumentNullException>(getter != null);

            if (name.Contains("."))
            {
                throw new ArgumentException("'name' may not contain periods.");
            }

            _defaultValues = new Dictionary<Type, object>();
            _initialized = new Subject<PerspexPropertyChangedEventArgs>();
            _changed = new Subject<PerspexPropertyChangedEventArgs>();
            _validation = new Dictionary<Type, Func<PerspexObject, object, object>>();

            Name = name;
            PropertyType = valueType;
            OwnerType = ownerType;
            Getter = getter;
            Setter = setter;
            IsDirect = true;
            _id = s_nextId++;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PerspexProperty"/> class.
        /// </summary>
        /// <param name="source">The direct property to copy.</param>
        /// <param name="ownerType">The new owner type.</param>
        protected PerspexProperty(PerspexProperty source, Type ownerType)
        {
            Contract.Requires<ArgumentNullException>(source != null);
            Contract.Requires<ArgumentNullException>(ownerType != null);

            if (source.IsDirect)
            {
                throw new InvalidOperationException(
                    "This method cannot be called on direct PerspexProperties.");
            }

            _defaultValues = source._defaultValues;
            _initialized = source._initialized;
            _changed = source._changed;
            _validation = source._validation;

            Name = source.Name;
            PropertyType = source.PropertyType;
            OwnerType = ownerType;
            _defaultValue = source._defaultValue;
            Inherits = source.Inherits;
            DefaultBindingMode = source.DefaultBindingMode;
            IsAttached = false;
            Notifying = Notifying;
            _validation = source._validation;
            _id = source._id;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PerspexProperty"/> class.
        /// </summary>
        /// <param name="source">The direct property to copy.</param>
        /// <param name="ownerType">The new owner type.</param>
        /// <param name="getter">A new getter.</param>
        /// <param name="setter">A new setter.</param>
        protected PerspexProperty(
            PerspexProperty source,
            Type ownerType,
            Func<PerspexObject, object> getter,
            Action<PerspexObject, object> setter)
        {
            Contract.Requires<ArgumentNullException>(source != null);
            Contract.Requires<ArgumentNullException>(ownerType != null);
            Contract.Requires<ArgumentNullException>(getter != null);

            if (!source.IsDirect)
            {
                throw new InvalidOperationException(
                    "This method can only be called on direct PerspexProperties.");
            }

            _defaultValues = source._defaultValues;
            _initialized = source._initialized;
            _changed = source._changed;
            _validation = source._validation;

            Name = source.Name;
            PropertyType = source.PropertyType;
            OwnerType = ownerType;
            Getter = getter;
            Setter = setter;
            IsDirect = true;
            _id = source._id;
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
        /// Gets a value indicating whether this is an attached property.
        /// </summary>
        /// <value>
        /// A value indicating whether this is an attached property.
        /// </value>
        public bool IsAttached { get; }

        /// <summary>
        /// Gets a value indicating whether this is a direct property.
        /// </summary>
        public bool IsDirect { get; }

        /// <summary>
        /// Gets a value indicating whether this is a readonly property.
        /// </summary>
        public bool IsReadOnly => IsDirect && Setter == null;

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
        public IObservable<PerspexPropertyChangedEventArgs> Initialized => _initialized;

        /// <summary>
        /// Gets an observable that is fired when this property changes on any
        /// <see cref="PerspexObject"/> instance.
        /// </summary>
        /// <value>
        /// An observable that is fired when this property changes on any
        /// <see cref="PerspexObject"/> instance.
        /// </value>
        public IObservable<PerspexPropertyChangedEventArgs> Changed => _changed;

        /// <summary>
        /// The notifying callback.
        /// </summary>
        /// <remarks>
        /// This is a method that gets called before and after the property starts being notified
        /// on an object; the bool argument will be true before and false afterwards. This 
        /// callback is intended to support IsDataContextChanging.
        /// </remarks>
        public Action<PerspexObject, bool> Notifying { get; }

        /// <summary>
        /// Provides access to a property's binding via the <see cref="PerspexObject"/>
        /// indexer.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>A <see cref="BindingDescriptor"/> describing the binding.</returns>
        public static BindingDescriptor operator !(PerspexProperty property)
        {
            return new BindingDescriptor
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
        /// <returns>A <see cref="BindingDescriptor"/> describing the binding.</returns>
        public static BindingDescriptor operator ~(PerspexProperty property)
        {
            return new BindingDescriptor
            {
                Priority = BindingPriority.TemplatedParent,
                Property = property,
            };
        }

        /// <summary>
        /// Gets the getter function for direct properties.
        /// </summary>
        internal Func<PerspexObject, object> Getter { get; }

        /// <summary>
        /// Gets the etter function for direct properties.
        /// </summary>
        internal Action<PerspexObject, object> Setter { get; }

        /// <summary>
        /// Tests two <see cref="PerspexProperty"/>s for equality.
        /// </summary>
        /// <param name="a">The first property.</param>
        /// <param name="b">The second property.</param>
        /// <returns>True if the properties are equal, otherwise false.</returns>
        public static bool operator ==(PerspexProperty a, PerspexProperty b)
        {
            if (object.ReferenceEquals(a, b))
            {
                return true;
            }
            else if (((object)a == null) || ((object)b == null))
            {
                return false;
            }
            else
            {
                return a.Equals(b);
            }
        }

        /// <summary>
        /// Tests two <see cref="PerspexProperty"/>s for unequality.
        /// </summary>
        /// <param name="a">The first property.</param>
        /// <param name="b">The second property.</param>
        /// <returns>True if the properties are equal, otherwise false.</returns>
        public static bool operator !=(PerspexProperty a, PerspexProperty b)
        {
            return !(a == b);
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
        /// <param name="validate">A validation function.</param>
        /// <param name="notifying">
        /// A method that gets called before and after the property starts being notified on an
        /// object; the bool argument will be true before and false afterwards. This callback is
        /// intended to support IsDataContextChanging.
        /// </param>
        /// <returns>A <see cref="PerspexProperty{TValue}"/></returns>
        public static PerspexProperty<TValue> Register<TOwner, TValue>(
            string name,
            TValue defaultValue = default(TValue),
            bool inherits = false,
            BindingMode defaultBindingMode = BindingMode.OneWay,
            Func<TOwner, TValue, TValue> validate = null,
            Action<PerspexObject, bool> notifying = null)
            where TOwner : PerspexObject
        {
            Contract.Requires<ArgumentNullException>(name != null);

            PerspexProperty<TValue> result = new PerspexProperty<TValue>(
                name,
                typeof(TOwner),
                defaultValue,
                inherits,
                defaultBindingMode,
                Cast(validate),
                notifying,
                false);

            PerspexPropertyRegistry.Instance.Register(typeof(TOwner), result);

            return result;
        }

        /// <summary>
        /// Registers a direct <see cref="PerspexProperty"/>.
        /// </summary>
        /// <typeparam name="TOwner">The type of the class that is registering the property.</typeparam>
        /// <typeparam name="TValue">The type of the property's value.</typeparam>
        /// <param name="name">The name of the property.</param>
        /// <param name="getter">Gets the current value of the property.</param>
        /// <param name="setter">Sets the value of the property.</param>
        /// <returns>A <see cref="PerspexProperty{TValue}"/></returns>
        public static PerspexProperty<TValue> RegisterDirect<TOwner, TValue>(
            string name,
            Func<TOwner, TValue> getter,
            Action<TOwner, TValue> setter = null)
                where TOwner : PerspexObject
        {
            Contract.Requires<ArgumentNullException>(name != null);

            PerspexProperty<TValue> result = new PerspexProperty<TValue>(
                name,
                typeof(TOwner),
                Cast(getter),
                Cast(setter));

            PerspexPropertyRegistry.Instance.Register(typeof(TOwner), result);

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
        /// <param name="validate">A validation function.</param>
        /// <returns>A <see cref="PerspexProperty{TValue}"/></returns>
        public static PerspexProperty<TValue> RegisterAttached<TOwner, THost, TValue>(
            string name,
            TValue defaultValue = default(TValue),
            bool inherits = false,
            BindingMode defaultBindingMode = BindingMode.OneWay,
            Func<PerspexObject, TValue, TValue> validate = null)
        {
            Contract.Requires<ArgumentNullException>(name != null);

            PerspexProperty<TValue> result = new PerspexProperty<TValue>(
                name,
                typeof(TOwner),
                defaultValue,
                inherits,
                defaultBindingMode,
                validate,
                null,
                true);

            PerspexPropertyRegistry.Instance.Register(typeof(THost), result);

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
        /// <param name="validate">A validation function.</param>
        /// <returns>A <see cref="PerspexProperty{TValue}"/></returns>
        public static PerspexProperty<TValue> RegisterAttached<THost, TValue>(
            string name,
            Type ownerType,
            TValue defaultValue = default(TValue),
            bool inherits = false,
            BindingMode defaultBindingMode = BindingMode.OneWay,
            Func<PerspexObject, TValue, TValue> validate = null)
        {
            Contract.Requires<ArgumentNullException>(name != null);

            PerspexProperty<TValue> result = new PerspexProperty<TValue>(
                name,
                ownerType,
                defaultValue,
                inherits,
                defaultBindingMode,
                validate,
                null,
                true);

            PerspexPropertyRegistry.Instance.Register(typeof(THost), result);

            return result;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            var p = obj as PerspexProperty;
            return p != null ? Equals(p) : false;
        }

        /// <inheritdoc/>
        public bool Equals(PerspexProperty other)
        {
            return other != null && _id == other._id;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return _id;
        }

        /// <summary>
        /// Returns a binding accessor that can be passed to <see cref="PerspexObject"/>'s []
        /// operator to initiate a binding.
        /// </summary>
        /// <returns>A <see cref="BindingDescriptor"/>.</returns>
        /// <remarks>
        /// The ! and ~ operators are short forms of this.
        /// </remarks>
        public BindingDescriptor Bind()
        {
            return new BindingDescriptor
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
            Contract.Requires<ArgumentNullException>(type != null);

            while (type != null)
            {
                object result;

                if (_defaultValues.TryGetValue(type, out result))
                {
                    return result;
                }

                type = type.GetTypeInfo().BaseType;
            }

            return _defaultValue;
        }

        /// <summary>
        /// Gets the validation function for the property on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// The validation function, or null if no validation function registered for this type.
        /// </returns>
        public Func<PerspexObject, object, object> GetValidationFunc(Type type)
        {
            Contract.Requires<ArgumentNullException>(type != null);

            while (type != null)
            {
                Func<PerspexObject, object, object> result;

                if (_validation.TryGetValue(type, out result))
                {
                    return result;
                }

                type = type.GetTypeInfo().BaseType;
            }

            return null;
        }

        /// <summary>
        /// Checks whether the <paramref name="value"/> is valid for the property.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>True if the value is valid, otherwise false.</returns>
        public bool IsValidValue(object value)
        {
            return TypeUtilities.TryCast(PropertyType, value, out value);
        }

        /// <summary>
        /// Overrides the default value for the property on the specified type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="defaultValue">The default value.</param>
        public void OverrideDefaultValue<T>(object defaultValue)
        {
            OverrideDefaultValue(typeof(T), defaultValue);
        }

        /// <summary>
        /// Overrides the default value for the property on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="defaultValue">The default value.</param>
        public void OverrideDefaultValue(Type type, object defaultValue)
        {
            Contract.Requires<ArgumentNullException>(type != null);

            if (!TypeUtilities.TryCast(PropertyType, defaultValue, out defaultValue))
            {
                throw new InvalidOperationException(string.Format(
                    "Invalid value for Property '{0}': {1} ({2})",
                    Name,
                    defaultValue,
                    defaultValue.GetType().FullName));
            }

            if (_defaultValues.ContainsKey(type))
            {
                throw new InvalidOperationException("Default value is already set for this property.");
            }

            _defaultValues.Add(type, defaultValue);
        }

        /// <summary>
        /// Overrides the validation function for the property on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="validation">The validation function.</param>
        public void OverrideValidation(Type type, Func<PerspexObject, object, object> validation)
        {
            Contract.Requires<ArgumentNullException>(type != null);

            if (_validation.ContainsKey(type))
            {
                throw new InvalidOperationException("Validation is already set for this property.");
            }

            _validation.Add(type, validation);
        }

        /// <summary>
        /// Gets the string representation of the property.
        /// </summary>
        /// <returns>The property's string representation.</returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Notifies the <see cref="Initialized"/> observable.
        /// </summary>
        /// <param name="e">The observable arguments.</param>
        internal void NotifyInitialized(PerspexPropertyChangedEventArgs e)
        {
            _initialized.OnNext(e);
        }

        /// <summary>
        /// Notifies the <see cref="Changed"/> observable.
        /// </summary>
        /// <param name="e">The observable arguments.</param>
        internal void NotifyChanged(PerspexPropertyChangedEventArgs e)
        {
            _changed.OnNext(e);
        }

        /// <summary>
        /// Casts a getter function accepting a typed owner to one accepting a
        /// <see cref="PerspexObject"/>.
        /// </summary>
        /// <typeparam name="TOwner">The owner type.</typeparam>
        /// <typeparam name="TValue">The property value type.</typeparam>
        /// <param name="f">The typed function.</param>
        /// <returns>The untyped function.</returns>
        private static Func<PerspexObject, TValue> Cast<TOwner, TValue>(Func<TOwner, TValue> f)
            where TOwner : PerspexObject
        {
            return (f != null) ? o => f((TOwner)o) : (Func<PerspexObject, TValue >)null;
        }

        /// <summary>
        /// Casts a setter action accepting a typed owner to one accepting a
        /// <see cref="PerspexObject"/>.
        /// </summary>
        /// <typeparam name="TOwner">The owner type.</typeparam>
        /// <typeparam name="TValue">The property value type.</typeparam>
        /// <param name="f">The typed action.</param>
        /// <returns>The untyped action.</returns>
        private static Action<PerspexObject, TValue> Cast<TOwner, TValue>(Action<TOwner, TValue> f)
            where TOwner : PerspexObject
        {
            return f != null ? (o, v) => f((TOwner)o, v) : (Action<PerspexObject, TValue>)null;
        }

        /// <summary>
        /// Casts a validation function accepting a typed owner to one accepting a
        /// <see cref="PerspexObject"/>.
        /// </summary>
        /// <typeparam name="TOwner">The owner type.</typeparam>
        /// <typeparam name="TValue">The property value type.</typeparam>
        /// <param name="f">The typed function.</param>
        /// <returns>The untyped function.</returns>
        private static Func<PerspexObject, TValue, TValue> Cast<TOwner, TValue>(Func<TOwner, TValue, TValue> f)
            where TOwner : PerspexObject
        {
            return f != null ? (o, v) => f((TOwner)o, v) : (Func<PerspexObject, TValue, TValue>)null;
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
