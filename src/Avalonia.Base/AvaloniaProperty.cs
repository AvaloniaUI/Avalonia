using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.PropertyStore;
using Avalonia.Utilities;

namespace Avalonia
{
    /// <summary>
    /// Base class for avalonia properties.
    /// </summary>
    public abstract class AvaloniaProperty : IEquatable<AvaloniaProperty>, IPropertyInfo
    {
        /// <summary>
        /// Represents an unset property value.
        /// </summary>
        public static readonly object UnsetValue = new UnsetValueType();

        private static int s_nextId;

        /// <summary>
        /// Provides a metadata object for types which have no metadata of their own.
        /// </summary>
        private readonly AvaloniaPropertyMetadata _defaultMetadata;

        /// <summary>
        /// Provides a fast path when the property has no metadata overrides.
        /// </summary>
        private KeyValuePair<Type, AvaloniaPropertyMetadata>? _singleMetadata;

        private readonly Dictionary<Type, AvaloniaPropertyMetadata> _metadata;
        private readonly Dictionary<Type, AvaloniaPropertyMetadata> _metadataCache = new Dictionary<Type, AvaloniaPropertyMetadata>();

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaProperty"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="valueType">The type of the property's value.</param>
        /// <param name="ownerType">The type of the class that registers the property.</param>
        /// <param name="hostType">The class that the property being is registered on.</param>
        /// <param name="metadata">The property metadata.</param>
        /// <param name="notifying">A <see cref="Notifying"/> callback.</param>
        private protected AvaloniaProperty(
            string name,
            Type valueType,
            Type ownerType,
            Type hostType,
            AvaloniaPropertyMetadata metadata,
            Action<AvaloniaObject, bool>? notifying = null)
        {
            _ = name ?? throw new ArgumentNullException(nameof(name));

            if (name.Contains('.'))
            {
                throw new ArgumentException("'name' may not contain periods.");
            }

            _metadata = new Dictionary<Type, AvaloniaPropertyMetadata>();

            Name = name;
            PropertyType = valueType ?? throw new ArgumentNullException(nameof(valueType));
            OwnerType = ownerType ?? throw new ArgumentNullException(nameof(ownerType));
            Notifying = notifying;
            Id = s_nextId++;

            _metadata.Add(hostType, metadata ?? throw new ArgumentNullException(nameof(metadata)));
            _defaultMetadata = metadata.GenerateTypeSafeMetadata();
            _singleMetadata = new(hostType, metadata);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaProperty"/> class.
        /// </summary>
        /// <param name="source">The direct property to copy.</param>
        /// <param name="ownerType">The new owner type.</param>
        /// <param name="metadata">Optional overridden metadata.</param>
        private protected AvaloniaProperty(
            AvaloniaProperty source,
            Type ownerType,
            AvaloniaPropertyMetadata? metadata)
        {
            _metadata = new Dictionary<Type, AvaloniaPropertyMetadata>();

            Name = source?.Name ?? throw new ArgumentNullException(nameof(source));
            PropertyType = source.PropertyType;
            OwnerType = ownerType ?? throw new ArgumentNullException(nameof(ownerType));
            Notifying = source.Notifying;
            Id = source.Id;
            _defaultMetadata = source._defaultMetadata;

            if (metadata != null)
            {
                _metadata.Add(ownerType, metadata);
            }
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
        /// Gets the type of the class that registered the property.
        /// </summary>
        public Type OwnerType { get; }

        /// <summary>
        /// Gets a value indicating whether the property inherits its value.
        /// </summary>
        public bool Inherits { get; private protected set; }

        /// <summary>
        /// Gets a value indicating whether this is an attached property.
        /// </summary>
        public bool IsAttached { get; private protected set; }

        /// <summary>
        /// Gets a value indicating whether this is a direct property.
        /// </summary>
        public bool IsDirect { get; private protected set; }

        /// <summary>
        /// Gets a value indicating whether this is a readonly property.
        /// </summary>
        public bool IsReadOnly { get; private protected set; }

        /// <summary>
        /// Gets an observable that is fired when this property changes on any
        /// <see cref="AvaloniaObject"/> instance.
        /// </summary>
        /// <value>
        /// An observable that is fired when this property changes on any
        /// <see cref="AvaloniaObject"/> instance.
        /// </value>
        public IObservable<AvaloniaPropertyChangedEventArgs> Changed => GetChanged();

        /// <summary>
        /// Gets a method that gets called before and after the property starts being notified on an
        /// object.
        /// </summary>
        /// <remarks>
        /// When a property changes, change notifications are sent to all property subscribers;
        /// for example via the <see cref="AvaloniaProperty.Changed"/> observable and and the
        /// <see cref="AvaloniaObject.PropertyChanged"/> event. If this callback is set for a property,
        /// then it will be called before and after these notifications take place. The bool argument
        /// will be true before the property change notifications are sent and false afterwards. This
        /// callback is intended to support Control.IsDataContextChanging.
        /// </remarks>
        internal Action<AvaloniaObject, bool>? Notifying { get; }

        /// <summary>
        /// Gets the integer ID that represents this property.
        /// </summary>
        internal int Id { get; }

        /// <summary>
        /// Provides access to a property's binding via the <see cref="AvaloniaObject"/>
        /// indexer.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>A <see cref="IndexerDescriptor"/> describing the binding.</returns>
        public static IndexerDescriptor operator !(AvaloniaProperty property)
        {
            return new IndexerDescriptor
            {
                Priority = BindingPriority.LocalValue,
                Property = property,
            };
        }

        /// <summary>
        /// Provides access to a property's template binding via the <see cref="AvaloniaObject"/>
        /// indexer.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>A <see cref="IndexerDescriptor"/> describing the binding.</returns>
        public static IndexerDescriptor operator ~(AvaloniaProperty property)
        {
            return new IndexerDescriptor
            {
                Priority = BindingPriority.Template,
                Property = property,
            };
        }

        /// <summary>
        /// Tests two <see cref="AvaloniaProperty"/>s for equality.
        /// </summary>
        /// <param name="a">The first property.</param>
        /// <param name="b">The second property.</param>
        /// <returns>True if the properties are equal, otherwise false.</returns>
        public static bool operator ==(AvaloniaProperty? a, AvaloniaProperty? b)
        {
            if (object.ReferenceEquals(a, b))
            {
                return true;
            }
            else if (a is null || b is null)
            {
                return false;
            }
            else
            {
                return a.Equals(b);
            }
        }

        /// <summary>
        /// Tests two <see cref="AvaloniaProperty"/>s for inequality.
        /// </summary>
        /// <param name="a">The first property.</param>
        /// <param name="b">The second property.</param>
        /// <returns>True if the properties are equal, otherwise false.</returns>
        public static bool operator !=(AvaloniaProperty? a, AvaloniaProperty? b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Registers a <see cref="AvaloniaProperty"/>.
        /// </summary>
        /// <typeparam name="TOwner">The type of the class that is registering the property.</typeparam>
        /// <typeparam name="TValue">The type of the property's value.</typeparam>
        /// <param name="name">The name of the property.</param>
        /// <param name="defaultValue">The default value of the property.</param>
        /// <param name="inherits">Whether the property inherits its value.</param>
        /// <param name="defaultBindingMode">The default binding mode for the property.</param>
        /// <param name="validate">A value validation callback.</param>
        /// <param name="coerce">A value coercion callback.</param>
        /// <param name="enableDataValidation">Whether the property is interested in data validation.</param>
        /// <returns>A <see cref="StyledProperty{TValue}"/></returns>
        public static StyledProperty<TValue> Register<TOwner, TValue>(
            string name,
            TValue defaultValue = default!,
            bool inherits = false,
            BindingMode defaultBindingMode = BindingMode.OneWay,
            Func<TValue, bool>? validate = null,
            Func<AvaloniaObject, TValue, TValue>? coerce = null,
            bool enableDataValidation = false)
                where TOwner : AvaloniaObject
        {
            _ = name ?? throw new ArgumentNullException(nameof(name));

            var metadata = new StyledPropertyMetadata<TValue>(
                defaultValue,
                defaultBindingMode: defaultBindingMode,
                coerce: coerce,
                enableDataValidation: enableDataValidation);

            var result = new StyledProperty<TValue>(
                name,
                typeof(TOwner),
                typeof(TOwner),
                metadata,
                inherits,
                validate);
            AvaloniaPropertyRegistry.Instance.Register(typeof(TOwner), result);
            return result;
        }

        /// <summary>
        /// Registers an attached <see cref="AvaloniaProperty"/>.
        /// </summary>
        /// <typeparam name="TOwner">The type of the class that is registering the property.</typeparam>
        /// <typeparam name="TValue">The type of the property's value.</typeparam>
        /// <param name="name">The name of the property.</param>
        /// <param name="defaultValue">The default value of the property.</param>
        /// <param name="inherits">Whether the property inherits its value.</param>
        /// <param name="defaultBindingMode">The default binding mode for the property.</param>
        /// <param name="validate">A value validation callback.</param>
        /// <param name="coerce">A value coercion callback.</param>
        /// <param name="enableDataValidation">if is set to true enable data validation.</param>
        /// <param name="notifying">
        /// A method that gets called before and after the property starts being notified on an
        /// object; the bool argument will be true before and false afterwards. This callback is
        /// intended to support IsDataContextChanging.
        /// </param>
        internal static StyledProperty<TValue> Register<TOwner, TValue>(
            string name,
            TValue defaultValue,
            bool inherits,
            BindingMode defaultBindingMode,
            Func<TValue, bool>? validate,
            Func<AvaloniaObject, TValue, TValue>? coerce,
            bool enableDataValidation,
            Action<AvaloniaObject, bool>? notifying)
                where TOwner : AvaloniaObject
        {
            _ = name ?? throw new ArgumentNullException(nameof(name));

            var metadata = new StyledPropertyMetadata<TValue>(
                defaultValue,
                defaultBindingMode: defaultBindingMode,
                coerce: coerce,
                enableDataValidation: enableDataValidation);

            var result = new StyledProperty<TValue>(
                name,
                typeof(TOwner),
                typeof(TOwner),
                metadata,
                inherits,
                validate,
                notifying);
            AvaloniaPropertyRegistry.Instance.Register(typeof(TOwner), result);
            return result;
        }

        /// <summary>
        /// Registers an attached <see cref="AvaloniaProperty"/>.
        /// </summary>
        /// <typeparam name="TOwner">The type of the class that is registering the property.</typeparam>
        /// <typeparam name="THost">The type of the class that the property is to be registered on.</typeparam>
        /// <typeparam name="TValue">The type of the property's value.</typeparam>
        /// <param name="name">The name of the property.</param>
        /// <param name="defaultValue">The default value of the property.</param>
        /// <param name="inherits">Whether the property inherits its value.</param>
        /// <param name="defaultBindingMode">The default binding mode for the property.</param>
        /// <param name="validate">A value validation callback.</param>
        /// <param name="coerce">A value coercion callback.</param>
        /// <returns>A <see cref="AvaloniaProperty{TValue}"/></returns>
        public static AttachedProperty<TValue> RegisterAttached<TOwner, THost, TValue>(
            string name,
            TValue defaultValue = default!,
            bool inherits = false,
            BindingMode defaultBindingMode = BindingMode.OneWay,
            Func<TValue, bool>? validate = null,
            Func<AvaloniaObject, TValue, TValue>? coerce = null)
                where THost : AvaloniaObject
        {
            _ = name ?? throw new ArgumentNullException(nameof(name));

            var metadata = new StyledPropertyMetadata<TValue>(
                defaultValue,
                defaultBindingMode: defaultBindingMode,
                coerce: coerce);

            var result = new AttachedProperty<TValue>(name, typeof(TOwner), typeof(THost), metadata, inherits, validate);
            var registry = AvaloniaPropertyRegistry.Instance;
            registry.Register(typeof(TOwner), result);
            registry.RegisterAttached(typeof(THost), result);
            return result;
        }

        /// <summary>
        /// Registers an attached <see cref="AvaloniaProperty"/>.
        /// </summary>
        /// <typeparam name="THost">The type of the class that the property is to be registered on.</typeparam>
        /// <typeparam name="TValue">The type of the property's value.</typeparam>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The type of the class that is registering the property.</param>
        /// <param name="defaultValue">The default value of the property.</param>
        /// <param name="inherits">Whether the property inherits its value.</param>
        /// <param name="defaultBindingMode">The default binding mode for the property.</param>
        /// <param name="validate">A value validation callback.</param>
        /// <param name="coerce">A value coercion callback.</param>
        /// <returns>A <see cref="AvaloniaProperty{TValue}"/></returns>
        public static AttachedProperty<TValue> RegisterAttached<THost, TValue>(
            string name,
            Type ownerType,
            TValue defaultValue = default!,
            bool inherits = false,
            BindingMode defaultBindingMode = BindingMode.OneWay,
            Func<TValue, bool>? validate = null,
            Func<AvaloniaObject, TValue, TValue>? coerce = null)
                where THost : AvaloniaObject
        {
            _ = name ?? throw new ArgumentNullException(nameof(name));

            var metadata = new StyledPropertyMetadata<TValue>(
                defaultValue,
                defaultBindingMode: defaultBindingMode,
                coerce: coerce);

            var result = new AttachedProperty<TValue>(name, ownerType, typeof(THost), metadata, inherits, validate);
            var registry = AvaloniaPropertyRegistry.Instance;
            registry.Register(ownerType, result);
            registry.RegisterAttached(typeof(THost), result);
            return result;
        }

        /// <summary>
        /// Registers a direct <see cref="AvaloniaProperty"/>.
        /// </summary>
        /// <typeparam name="TOwner">The type of the class that is registering the property.</typeparam>
        /// <typeparam name="TValue">The type of the property's value.</typeparam>
        /// <param name="name">The name of the property.</param>
        /// <param name="getter">Gets the current value of the property.</param>
        /// <param name="setter">Sets the value of the property.</param>
        /// <param name="unsetValue">The value to use when the property is cleared.</param>
        /// <param name="defaultBindingMode">The default binding mode for the property.</param>
        /// <param name="enableDataValidation">
        /// Whether the property is interested in data validation.
        /// </param>
        /// <returns>A <see cref="AvaloniaProperty{TValue}"/></returns>
        public static DirectProperty<TOwner, TValue> RegisterDirect<TOwner, TValue>(
            string name,
            Func<TOwner, TValue> getter,
            Action<TOwner, TValue>? setter = null,
            TValue unsetValue = default!,
            BindingMode defaultBindingMode = BindingMode.OneWay,
            bool enableDataValidation = false)
                where TOwner : AvaloniaObject
        {
            _ = name ?? throw new ArgumentNullException(nameof(name));
            _ = getter ?? throw new ArgumentNullException(nameof(getter));

            var metadata = new DirectPropertyMetadata<TValue>(
                unsetValue: unsetValue,
                defaultBindingMode: defaultBindingMode,
                enableDataValidation: enableDataValidation);

            var result = new DirectProperty<TOwner, TValue>(
                name,
                getter,
                setter,
                metadata);
            AvaloniaPropertyRegistry.Instance.Register(typeof(TOwner), result);
            return result;
        }

        /// <summary>
        /// Returns a binding accessor that can be passed to <see cref="AvaloniaObject"/>'s []
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

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            var p = obj as AvaloniaProperty;
            return p is not null && Equals(p);
        }

        /// <inheritdoc/>
        public bool Equals(AvaloniaProperty? other)
        {
            return Id == other?.Id;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Id;
        }

        /// <summary>
        /// Gets the <see cref="AvaloniaPropertyMetadata"/> which applies to this property when it is used with the specified type.
        /// </summary>
        /// <typeparam name="T">The type for which to retrieve metadata.</typeparam>
        public AvaloniaPropertyMetadata GetMetadata<T>() where T : AvaloniaObject => GetMetadata(typeof(T));

        /// <inheritdoc cref="GetMetadata{T}"/>
        /// <param name="type">The type for which to retrieve metadata.</param>
        public AvaloniaPropertyMetadata GetMetadata(Type type) => GetMetadataWithOverrides(type);

        /// <summary>
        /// Checks whether the <paramref name="value"/> is valid for the property.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>True if the value is valid, otherwise false.</returns>
        [RequiresUnreferencedCode(TrimmingMessages.ImplicitTypeConversionRequiresUnreferencedCodeMessage)]
        public bool IsValidValue(object? value)
        {
            return TypeUtilities.TryConvertImplicit(PropertyType, value, out _);
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
        /// Creates an effective value for the property.
        /// </summary>
        /// <param name="o">The effective value owner.</param>
        internal abstract EffectiveValue CreateEffectiveValue(AvaloniaObject o);

        /// <summary>
        /// Routes an untyped ClearValue call to a typed call.
        /// </summary>
        /// <param name="o">The object instance.</param>
        internal abstract void RouteClearValue(AvaloniaObject o);

        /// <summary>
        /// Routes an untyped CoerceValue call on a property with its default value to a typed call.
        /// </summary>
        /// <param name="o">The object instance.</param>
        internal abstract void RouteCoerceDefaultValue(AvaloniaObject o);

        /// <summary>
        /// Routes an untyped GetValue call to a typed call.
        /// </summary>
        /// <param name="o">The object instance.</param>
        internal abstract object? RouteGetValue(AvaloniaObject o);

        /// <summary>
        /// Routes an untyped GetBaseValue call to a typed call.
        /// </summary>
        /// <param name="o">The object instance.</param>
        internal abstract object? RouteGetBaseValue(AvaloniaObject o);

        /// <summary>
        /// Routes an untyped SetValue call to a typed call.
        /// </summary>
        /// <param name="o">The object instance.</param>
        /// <param name="value">The value.</param>
        /// <param name="priority">The priority.</param>
        /// <returns>
        /// An <see cref="IDisposable"/> if setting the property can be undone, otherwise null.
        /// </returns>
        internal abstract IDisposable? RouteSetValue(
            AvaloniaObject o,
            object? value,
            BindingPriority priority);

        /// <summary>
        /// Routes an untyped SetCurrentValue call to a typed call.
        /// </summary>
        /// <param name="o">The object instance.</param>
        /// <param name="value">The value.</param>
        internal abstract void RouteSetCurrentValue(AvaloniaObject o, object? value);

        /// <summary>
        /// Routes an untyped SetDirectValueUnchecked call to a typed call.
        /// </summary>
        /// <param name="o">The object instance.</param>
        /// <param name="value">The value.</param>
        internal virtual void RouteSetDirectValueUnchecked(AvaloniaObject o, object? value) =>
            throw new NotSupportedException();

        /// <summary>
        /// Routes an untyped Bind call to a typed call.
        /// </summary>
        /// <param name="o">The object instance.</param>
        /// <param name="source">The binding source.</param>
        /// <param name="priority">The priority.</param>
        internal abstract IDisposable RouteBind(
            AvaloniaObject o,
            IObservable<object?> source,
            BindingPriority priority);

        /// <summary>
        /// Overrides the metadata for the property on the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="metadata">The metadata.</param>
        private protected void OverrideMetadata(Type type, AvaloniaPropertyMetadata metadata)
        {
            _ = type ?? throw new ArgumentNullException(nameof(type));
            _ = metadata ?? throw new ArgumentNullException(nameof(metadata));

            if (_metadata.ContainsKey(type))
            {
                throw new InvalidOperationException(
                    $"Metadata is already set for {Name} on {type}.");
            }

            var baseMetadata = GetMetadata(type);
            metadata.Merge(baseMetadata, this);
            _metadata.Add(type, metadata);
            _metadataCache.Clear();

            _singleMetadata = null;
        }

        private protected abstract IObservable<AvaloniaPropertyChangedEventArgs> GetChanged();

        private AvaloniaPropertyMetadata GetMetadataWithOverrides(Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (_metadataCache.TryGetValue(type, out var result))
            {
                return result;
            }

            if (_singleMetadata is { } singleMetadata)
            {
                return _metadataCache[type] = singleMetadata.Key.IsAssignableFrom(type) ? singleMetadata.Value : _defaultMetadata;
            }

            var currentType = type;

            while (currentType != null)
            {
                if (_metadata.TryGetValue(currentType, out result))
                {
                    _metadataCache[type] = result;

                    return result;
                }

                currentType = currentType.BaseType;
            }

            return _metadataCache[type] = _defaultMetadata;
        }

        bool IPropertyInfo.CanGet => true;
        bool IPropertyInfo.CanSet => !IsReadOnly;
        object? IPropertyInfo.Get(object target) => ((AvaloniaObject)target).GetValue(this);
        void IPropertyInfo.Set(object target, object? value) => ((AvaloniaObject)target).SetValue(this, value);
    }

    /// <summary>
    /// Class representing the <see cref="AvaloniaProperty.UnsetValue"/>.
    /// </summary>
    public sealed class UnsetValueType
    {
        internal UnsetValueType() { }

        /// <summary>
        /// Returns the string representation of the <see cref="AvaloniaProperty.UnsetValue"/>.
        /// </summary>
        /// <returns>The string "(unset)".</returns>
        public override string ToString() => "(unset)";
    }
}
