// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex
{
    /// <summary>
    /// A styled perspex property.
    /// </summary>
    public class StyledProperty<TValue> : StyledPropertyBase<TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StyledPropertyBase{T}"/> class.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="ownerType">The type of the class that registers the property.</param>
        /// <param name="inherits">Whether the property inherits its value.</param>
        /// <param name="metadata">The property metadata.</param>
        public StyledProperty(
            string name,
            Type ownerType,
            bool inherits,
            StyledPropertyMetadata metadata)
                : base(name, ownerType, inherits, metadata)
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
        public StyledProperty<TValue> AddOwner<TOwner>()
        {
            PerspexPropertyRegistry.Instance.Register(typeof(TOwner), this);
            return this;
        }
    }
}
