using System;

namespace Avalonia
{
    /// <summary>
    /// An attached avalonia property.
    /// </summary>
    /// <typeparam name="TValue">The type of the property's value.</typeparam>
    public class AttachedProperty<TValue> : StyledProperty<TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AttachedProperty{TValue}"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The class that is registering the property.</param>
        /// <param name="metadata">The property metadata.</param>
        /// <param name="inherits">Whether the property inherits its value.</param>
        /// <param name="validate">A value validation callback.</param>
        public AttachedProperty(
            string name,
            Type ownerType,
            StyledPropertyMetadata<TValue> metadata,
            bool inherits = false,
            Func<TValue, bool>? validate = null)
            : base(name, ownerType, metadata, inherits, validate)
        {
            IsAttached = true;
        }

        /// <summary>
        /// Attaches the property as a non-attached property on the specified type.
        /// </summary>
        /// <typeparam name="TOwner">The owner type.</typeparam>
        /// <returns>The property.</returns>
        public new AttachedProperty<TValue> AddOwner<TOwner>() where TOwner : AvaloniaObject
        {
            AvaloniaPropertyRegistry.Instance.Register(typeof(TOwner), this);
            return this;
        }
    }
}
