// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex
{
    /// <summary>
    /// Untyped interface to <see cref="DirectPropertyMetadata{TValue}"/>
    /// </summary>
    public interface IDirectPropertyMetadata
    {
        /// <summary>
        /// Gets the to use when the property is set to <see cref="PerspexProperty.UnsetValue"/>.
        /// </summary>
        object UnsetValue { get; }
    }
}