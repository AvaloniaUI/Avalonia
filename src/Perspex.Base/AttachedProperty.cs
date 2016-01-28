// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex
{
    /// <summary>
    /// An attached perspex property.
    /// </summary>
    /// <typeparam name="TValue">The type of the property's value.</typeparam>
    public class AttachedProperty<TValue> : StyledPropertyBase<TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AttachedProperty{TValue}"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The class that is registering the property.</param>
        /// <param name="inherits">Whether the property inherits its value.</param>
        /// <param name="metadata">The property metadata.</param>
        public AttachedProperty(
            string name,
            Type ownerType,
            bool inherits,
            StyledPropertyMetadata metadata)
            : base(name, ownerType, inherits, metadata)
        {
        }

        /// <inheritdoc/>
        public override bool IsAttached => true;

        /// <summary>
        /// Attaches the property as a non-attached property on the specified type.
        /// </summary>
        /// <typeparam name="TOwner">The owner type.</typeparam>
        /// <returns>The property.</returns>
        public StyledProperty<TValue> AddOwner<TOwner>()
        {
            var result = new StyledProperty<TValue>(this, typeof(TOwner));
            PerspexPropertyRegistry.Instance.Register(typeof(TOwner), result);
            return result;
        }
    }
}
