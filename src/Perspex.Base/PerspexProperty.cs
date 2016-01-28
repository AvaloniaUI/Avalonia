// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Subjects;
using Perspex.Data;
using Perspex.Utilities;

namespace Perspex
{
    /// <summary>
    /// Base class for perspex property metadata.
    /// </summary>
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
        /// Observable fired when this property changes on any <see cref="PerspexObject"/>.
        /// </summary>
        private readonly Subject<PerspexPropertyChangedEventArgs> _initialized;

        /// <summary>
        /// Observable fired when this property changes on any <see cref="PerspexObject"/>.
        /// </summary>
        private readonly Subject<PerspexPropertyChangedEventArgs> _changed;

        /// <summary>
        /// Gets the ID of the property.
        /// </summary>
        private readonly int _id;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerspexProperty"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="valueType">The type of the property's value.</param>
        /// <param name="ownerType">The type of the class that registers the property.</param>
        /// <param name="defaultBindingMode">The default binding mode for the property.</param>
        /// <param name="notifying">
        /// A method that gets called before and after the property starts being notified on an
        /// object; the bool argument will be true before and false afterwards. This callback is
        /// intended to support IsDataContextChanging.
        /// </param>
        protected PerspexProperty(
            string name,
            Type valueType,
            Type ownerType,
            BindingMode defaultBindingMode = BindingMode.Default,
            Action<PerspexObject, bool> notifying = null)
        {
            Contract.Requires<ArgumentNullException>(name != null);
            Contract.Requires<ArgumentNullException>(valueType != null);
            Contract.Requires<ArgumentNullException>(ownerType != null);

            if (name.Contains("."))
            {
                throw new ArgumentException("'name' may not contain periods.");
            }

            _initialized = new Subject<PerspexPropertyChangedEventArgs>();
            _changed = new Subject<PerspexPropertyChangedEventArgs>();

            Name = name;
            PropertyType = valueType;
            OwnerType = ownerType;
            DefaultBindingMode = defaultBindingMode;
            Notifying = notifying;
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

            _initialized = source._initialized;
            _changed = source._changed;

            Name = source.Name;
            PropertyType = source.PropertyType;
            OwnerType = ownerType;
            DefaultBindingMode = source.DefaultBindingMode;
            Notifying = Notifying;
            _id = source._id;
        }

        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the full name of the property, wich includes the owner type in the case of
        /// attached properties.
        /// </summary>
        public virtual string FullName => Name;

        /// <summary>
        /// Gets the type of the property's value.
        /// </summary>
        public Type PropertyType { get; }

        /// <summary>
        /// Gets the type of the class that registered the property.
        /// </summary>
        public Type OwnerType { get; }

        /// <summary>
        /// Gets a value indicating whether the property inherits its value.
        /// </summary>
        public virtual bool Inherits => false;

        /// <summary>
        /// Gets the default binding mode for the property.
        /// </summary>
        public BindingMode DefaultBindingMode { get; }

        /// <summary>
        /// Gets a value indicating whether this is an attached property.
        /// </summary>
        public virtual bool IsAttached => false;

        /// <summary>
        /// Gets a value indicating whether this is a direct property.
        /// </summary>
        public virtual bool IsDirect => false;

        /// <summary>
        /// Gets a value indicating whether this is a readonly property.
        /// </summary>
        public virtual bool IsReadOnly => false;

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
        /// <returns>A <see cref="IndexerDescriptor"/> describing the binding.</returns>
        public static IndexerDescriptor operator !(PerspexProperty property)
        {
            return new IndexerDescriptor
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
        /// <returns>A <see cref="IndexerDescriptor"/> describing the binding.</returns>
        public static IndexerDescriptor operator ~(PerspexProperty property)
        {
            return new IndexerDescriptor
            {
                Priority = BindingPriority.TemplatedParent,
                Property = property,
            };
        }

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
        /// <returns>A <see cref="StyledProperty{TValue}"/></returns>
        public static StyledProperty<TValue> Register<TOwner, TValue>(
            string name,
            TValue defaultValue = default(TValue),
            bool inherits = false,
            BindingMode defaultBindingMode = BindingMode.OneWay,
            Func<TOwner, TValue, TValue> validate = null,
            Action<IPerspexObject, bool> notifying = null)
            where TOwner : IPerspexObject
        {
            Contract.Requires<ArgumentNullException>(name != null);

            var result = new StyledProperty<TValue>(
                name,
                typeof(TOwner),
                defaultValue,
                inherits,
                defaultBindingMode,
                Cast(validate),
                notifying);

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
        public static AttachedProperty<TValue> RegisterAttached<TOwner, THost, TValue>(
            string name,
            TValue defaultValue = default(TValue),
            bool inherits = false,
            BindingMode defaultBindingMode = BindingMode.OneWay,
            Func<THost, TValue, TValue> validate = null)
                where THost : IPerspexObject
        {
            Contract.Requires<ArgumentNullException>(name != null);

            var result = new AttachedProperty<TValue>(
                name,
                typeof(TOwner),
                defaultValue,
                inherits,
                defaultBindingMode,
                Cast(validate));

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
            Func<THost, TValue, TValue> validate = null)
                where THost : IPerspexObject
        {
            Contract.Requires<ArgumentNullException>(name != null);

            var result = new AttachedProperty<TValue>(
                name,
                ownerType,
                defaultValue,
                inherits,
                defaultBindingMode,
                Cast(validate));

            PerspexPropertyRegistry.Instance.Register(typeof(THost), result);

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
        public static DirectProperty<TOwner, TValue> RegisterDirect<TOwner, TValue>(
            string name,
            Func<TOwner, TValue> getter,
            Action<TOwner, TValue> setter = null)
                where TOwner : IPerspexObject
        {
            Contract.Requires<ArgumentNullException>(name != null);

            var result = new DirectProperty<TOwner, TValue>(name, getter, setter);
            PerspexPropertyRegistry.Instance.Register(typeof(TOwner), result);
            return result;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            var p = obj as PerspexProperty;
            return p != null && Equals(p);
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
        /// <returns>A <see cref="IndexerDescriptor"/>.</returns>
        /// <remarks>
        /// The ! and ~ operators are short forms of this.
        /// </remarks>
        public IndexerDescriptor Bind()
        {
            return new IndexerDescriptor
            {
                Property = this,
            };
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
        /// Casts a validation function accepting a typed owner to one accepting an
        /// <see cref="IPerspexObject"/>.
        /// </summary>
        /// <typeparam name="TOwner">The owner type.</typeparam>
        /// <typeparam name="TValue">The property value type.</typeparam>
        /// <param name="f">The typed function.</param>
        /// <returns>The untyped function.</returns>
        protected static Func<IPerspexObject, TValue, TValue> Cast<TOwner, TValue>(Func<TOwner, TValue, TValue> f)
            where TOwner : IPerspexObject
        {
            if (f == null)
            {
                return null;
            }
            else
            {
                return (o, v) => f((TOwner)o, v);
            }
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
            public override string ToString() => "(unset)";
        }
    }
}
