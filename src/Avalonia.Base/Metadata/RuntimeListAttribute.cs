// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;

namespace Avalonia.Metadata
{
    /// <summary>
    /// Declares that at runtime the markup engine should treat this property as having a type of
    /// <see cref="IList"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class RuntimeListAttribute : Attribute
    {
    }
}
