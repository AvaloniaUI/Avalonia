// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Data;

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
        /// <param name="defaultValue">The default value of the property.</param>
        /// <param name="inherits">Whether the property inherits its value.</param>
        /// <param name="defaultBindingMode">The default binding mode for the property.</param>
        /// <param name="validate">A validation function.</param>
        public AttachedProperty(
            string name,
            Type ownerType,
            TValue defaultValue = default(TValue),
            bool inherits = false,
            BindingMode defaultBindingMode = BindingMode.Default,
            Func<IPerspexObject, TValue, TValue> validate = null)
            : base(name, ownerType, defaultValue, inherits, defaultBindingMode, validate)
        {
        }

        /// <inheritdoc/>
        public override string FullName => OwnerType + "." + Name;

        /// <inheritdoc/>
        public override bool IsAttached => true;
    }
}
