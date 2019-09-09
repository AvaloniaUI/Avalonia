// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia
{
    /// <summary>
    /// Represents boxed value of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Type of stored value.</typeparam>
    internal readonly struct BoxedValue<T>
    {
        public BoxedValue(T value)
        {
            Boxed = value;
            Typed = value;
        }

        /// <summary>
        /// Boxed value.
        /// </summary>
        public object Boxed { get; }

        /// <summary>
        /// Typed value.
        /// </summary>
        public T Typed { get; }
    }
}
