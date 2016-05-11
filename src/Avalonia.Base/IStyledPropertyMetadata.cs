// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia
{
    /// <summary>
    /// Untyped interface to <see cref="StyledPropertyMetadata{TValue}"/>
    /// </summary>
    public interface IStyledPropertyMetadata
    {
        /// <summary>
        /// Gets the default value for the property.
        /// </summary>
        object DefaultValue { get; }

        /// <summary>
        /// Gets the property's validation function.
        /// </summary>
        Func<IAvaloniaObject, object, object> Validate { get; }
    }
}