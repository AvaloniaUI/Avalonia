// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia
{
    /// <summary>
    /// An attached avalonia property.
    /// </summary>
    /// <typeparam name="TValue">The type of the property's value.</typeparam>
    public class AttachedProperty<TValue> : StyledPropertyBase<TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AttachedProperty{TValue}"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The class that is registering the property.</param>
        /// <param name="metadata">The property metadata.</param>
        /// <param name="inherits">Whether the property inherits its value.</param>
        public AttachedProperty(
            string name,
            Type ownerType,            
            StyledPropertyMetadata<TValue> metadata,
            bool inherits = false)
            : base(name, ownerType, metadata, inherits)
        {
        }

        /// <inheritdoc/>
        public override bool IsAttached => true;

        /// <summary>
        /// Attaches the property as a non-attached property on the specified type.
        /// </summary>
        /// <typeparam name="TOwner">The owner type.</typeparam>
        /// <returns>The property.</returns>
        public StyledProperty<TValue> AddOwner<TOwner>() where TOwner : IAvaloniaObject
        {
            var result = new StyledProperty<TValue>(this, typeof(TOwner));
            AvaloniaPropertyRegistry.Instance.Register(typeof(TOwner), result);
            return result;
        }
    }
}
