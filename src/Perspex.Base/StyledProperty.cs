// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Data;

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
        /// <param name="defaultValue">The default value of the property.</param>
        /// <param name="inherits">Whether the property inherits its value.</param>
        /// <param name="defaultBindingMode">The default binding mode for the property.</param>
        /// <param name="validate">A validation function.</param>
        /// <param name="notifying">
        /// A method that gets called before and after the property starts being notified on an
        /// object; the bool argument will be true before and false afterwards. This callback is
        /// intended to support IsDataContextChanging.
        /// </param>
        public StyledProperty(
            string name,
            Type ownerType,
            TValue defaultValue,
            bool inherits = false,
            BindingMode defaultBindingMode = BindingMode.Default,
            Func<IPerspexObject, TValue, TValue> validate = null,
            Action<IPerspexObject, bool> notifying = null)
                : base(name, ownerType, defaultValue, inherits, defaultBindingMode, validate, notifying)
        {
        }
    }
}
