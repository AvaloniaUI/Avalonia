using System;

namespace Avalonia
{
    /// <summary>
    /// A styled avalonia property.
    /// </summary>
    public class StyledProperty<TValue> : StyledPropertyBase<TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StyledPropertyBase{T}"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The type of the class that registers the property.</param>
        /// <param name="metadata">The property metadata.</param>
        /// <param name="inherits">Whether the property inherits its value.</param>
        /// <param name="validate">A value validation callback.</param>
        /// <param name="notifying">A <see cref="AvaloniaProperty.Notifying"/> callback.</param>
        public StyledProperty(
            string name,
            Type ownerType,
            StyledPropertyMetadata<TValue> metadata,
            bool inherits = false,
            Func<TValue, bool>? validate = null,
            Action<AvaloniaObject, bool>? notifying = null)
            : base(name, ownerType, metadata, inherits, validate, notifying)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StyledPropertyBase{T}"/> class.
        /// </summary>
        /// <param name="source">The property to add the owner to.</param>
        /// <param name="ownerType">The type of the class that registers the property.</param>
        internal StyledProperty(StyledPropertyBase<TValue> source, Type ownerType)
            : base(source, ownerType)
        {
        }
        
        /// <summary>
        /// Registers the property on another type.
        /// </summary>
        /// <typeparam name="TOwner">The type of the additional owner.</typeparam>
        /// <returns>The property.</returns>        
        public StyledProperty<TValue> AddOwner<TOwner>() where TOwner : AvaloniaObject
        {
            AvaloniaPropertyRegistry.Instance.Register(typeof(TOwner), this);
            return this;
        }
    }
}
